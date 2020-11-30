Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module OpenCVGL_Image_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OpenCVGL_Image_Open(w As integer, h As integer)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OpenCVGL_Image_Close()
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OpenCVGL_Image_Control(ppx As Single, ppy As Single, fx As Single, fy As Single, FOV As Single, zNear As Single, zFar As Single, eye As cv.Vec3f,
                                    yaw As Single, roll As Single, pitch As Single, pointSize As integer, zTrans As Single, textureWidth As integer, textureHeight As integer)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub OpenCVGL_Image_Run(rgbPtr As IntPtr, pointCloud As IntPtr, pc_rows As integer, pc_cols As integer, rgb_rows As integer, rgb_cols As integer)
    End Sub
End Module




' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class OpenCVGL_Image_CPP
    Inherits VBparent
    Dim imu As IMU_Basics
    Dim rgbData(0) As Byte
    Dim pointCloudData(0) As Byte
    Public Sub New()
        initParent()
        imu = New IMU_Basics()

        If ocvb.parms.testAllRunning = False Then
            setOpenGLsliders(caller, sliders)
            sliders.trackbar(10).Value = -10 ' eye.z
            sliders.trackbar(0).Value = 30 ' FOV
            sliders.trackbar(1).Value = 0 ' Yaw
            sliders.trackbar(2).Value = 0 ' pitch
            sliders.trackbar(3).Value = 0 ' roll

            OpenCVGL_Image_Open(ocvb.task.pointCloud.Width, ocvb.task.pointCloud.Height)
        End If
        ocvb.desc = "Use the OpenCV implementation of OpenGL to render a 3D image with depth."
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.parms.testAllRunning Then
            ' It runs fine but after several cycles, it will fail with an external exception.
            ' Only happens on 'Test All' runs.  Runs fine otherwise.
            ocvb.trueText("OpenCVGL only fails when running 'Test All'.  Can't get it to fail otherwise." + vbCrLf +
                                                  "Skipping it during a 'Test All' just so all the other tests can be exercised.")
            Exit Sub
        End If

        imu.Run()
        Dim FOV = sliders.trackbar(0).Value
        Dim yaw = sliders.trackbar(1).Value
        Dim pitch = sliders.trackbar(2).Value
        Dim roll = sliders.trackbar(3).Value
        Dim zNear = sliders.trackbar(4).Value
        Dim zFar = sliders.trackbar(5).Value
        Dim pointSize = sliders.trackbar(6).Value
        Dim eye As New cv.Vec3f(sliders.trackbar(8).Value, sliders.trackbar(9).Value, sliders.trackbar(10).Value)
        Dim zTrans = sliders.trackbar(7).Value / 100

        OpenCVGL_Image_Control(ocvb.parms.intrinsicsLeft.ppx, ocvb.parms.intrinsicsLeft.ppy, ocvb.parms.intrinsicsLeft.fx, ocvb.parms.intrinsicsLeft.fy,
                               FOV, zNear, zFar, eye, yaw, roll, pitch, pointSize, zTrans, ocvb.task.pointCloud.Width, ocvb.task.pointCloud.Height)

        Dim pcSize = ocvb.task.pointCloud.Total * ocvb.task.pointCloud.ElemSize
        If rgbData.Length <> src.Total * src.ElemSize Then ReDim rgbData(src.Total * src.ElemSize - 1)
        If pointCloudData.Length <> pcSize Then ReDim pointCloudData(pcSize - 1)
        Marshal.Copy(src.Data, rgbData, 0, rgbData.Length)
        Marshal.Copy(ocvb.task.pointCloud.Data, pointCloudData, 0, pcSize)
        Dim handleRGB = GCHandle.Alloc(rgbData, GCHandleType.Pinned)
        Dim handlePointCloud = GCHandle.Alloc(pointCloudData, GCHandleType.Pinned)
        OpenCVGL_Image_Run(handleRGB.AddrOfPinnedObject(), handlePointCloud.AddrOfPinnedObject(), ocvb.task.pointCloud.Rows, ocvb.task.pointCloud.Cols, src.Rows, src.Cols)
        handleRGB.Free()
        handlePointCloud.Free()
    End Sub
    Public Sub Close()
        OpenCVGL_Image_Close()
    End Sub
End Class


