Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module OpenCVGL_Image_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OpenCVGL_Image_Open(w As Int32, h As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OpenCVGL_Image_Close()
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OpenCVGL_Image_Control(ppx As Single, ppy As Single, fx As Single, fy As Single, FOV As Single, zNear As Single, zFar As Single, eye As cv.Vec3f,
                                    yaw As Single, roll As Single, pitch As Single, pointSize As Int32, zTrans As Single, textureWidth As Int32, textureHeight As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OpenCVGL_Image_Run(rgbPtr As IntPtr, pointCloud As IntPtr, rows As Int32, cols As Int32)
    End Sub
End Module




' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class OpenCVGL_Image_CPP
    Inherits ocvbClass
    Dim imu As IMU_Basics
    Dim rgbData(0) As Byte
    Dim pointCloudData(0) As Byte
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        imu = New IMU_Basics(ocvb, caller)

        If ocvb.parms.testAllRunning = False Then
            setOpenGLsliders(ocvb, caller, sliders, sliders1, sliders2, sliders3)
            sliders2.TrackBar3.Value = -10 ' eye.z
            sliders.TrackBar1.Value = 30 ' FOV
            sliders.TrackBar2.Value = 0 ' Yaw
            sliders.TrackBar3.Value = 0 ' pitch
            sliders.TrackBar4.Value = 0 ' roll

            OpenCVGL_Image_Open(ocvb.color.Width, ocvb.color.Height)
        End If
        ocvb.desc = "Use the OpenCV implementation of OpenGL to render a 3D image with depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.testAllRunning Then
            ' It runs fine but after several cycles, it will fail with an external exception.
            ' Only happens on 'Test All' runs.  Runs fine otherwise.
            ocvb.putText(New ActiveClass.TrueType("OpenCVGL only fails when running 'Test All'.  Can't get it to fail otherwise." + vbCrLf +
                                                  "Skipping it during a 'Test All' just so all the other tests can be exercised.", 10, 60, RESULT1))
            Exit Sub
        End If
        If ocvb.parms.cameraIndex = T265Camera Then
            ocvb.putText(New ActiveClass.TrueType("The T265 camera doesn't have a point cloud.", 10, 60, RESULT1))
            Exit Sub
        End If
        imu.Run(ocvb)
        Dim FOV = sliders.TrackBar1.Value
        Dim yaw = sliders.TrackBar2.Value
        Dim pitch = sliders.TrackBar3.Value
        Dim roll = sliders.TrackBar4.Value
        Dim zNear = sliders1.TrackBar1.Value
        Dim zFar = sliders1.TrackBar2.Value
        Dim pointSize = sliders1.TrackBar3.Value
        Dim eye As New cv.Vec3f(sliders2.TrackBar1.Value, sliders2.TrackBar2.Value, sliders2.TrackBar3.Value)
        Dim zTrans = sliders1.TrackBar4.Value / 100

        OpenCVGL_Image_Control(ocvb.parms.intrinsicsLeft.ppx, ocvb.parms.intrinsicsLeft.ppy, ocvb.parms.intrinsicsLeft.fx, ocvb.parms.intrinsicsLeft.fy,
                               FOV, zNear, zFar, eye, yaw, roll, pitch, pointSize, zTrans, ocvb.color.Width, ocvb.color.Height)

        Dim pcSize = ocvb.pointCloud.Total * ocvb.pointCloud.ElemSize
        If rgbData.Length <> ocvb.color.Total * ocvb.color.ElemSize Then ReDim rgbData(ocvb.color.Total * ocvb.color.ElemSize - 1)
        If pointCloudData.Length <> pcSize Then ReDim pointCloudData(pcSize - 1)
        Marshal.Copy(ocvb.color.Data, rgbData, 0, rgbData.Length)
        Marshal.Copy(ocvb.pointCloud.Data, pointCloudData, 0, pcSize)
        Dim handleRGB = GCHandle.Alloc(rgbData, GCHandleType.Pinned)
        Dim handlePointCloud = GCHandle.Alloc(pointCloudData, GCHandleType.Pinned)
        OpenCVGL_Image_Run(handleRGB.AddrOfPinnedObject(), handlePointCloud.AddrOfPinnedObject(), ocvb.color.Rows, ocvb.color.Cols)
        handleRGB.Free()
        handlePointCloud.Free()
    End Sub
    Public Sub Close()
        OpenCVGL_Image_Close()
    End Sub
End Class

