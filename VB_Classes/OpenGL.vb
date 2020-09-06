Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes

Public Class OpenGL_Basics
    Inherits VBparent
    Dim memMapWriter As MemoryMappedViewAccessor
    Dim pipeName As String ' this is name of pipe to the OpenGL task.  It is dynamic and increments.
    Dim pipe As NamedPipeServerStream
    Dim startInfo As New ProcessStartInfo
    Dim memMapbufferSize As Int32
    Dim memMapFile As MemoryMappedFile
    Dim memMapPtr As IntPtr
    Dim rgbBuffer(0) As Byte
    Dim dataBuffer(0) As Byte
    Dim pointCloudBuffer(0) As Byte
    Public memMapValues(49) As Double ' more than needed - buffer for growth.
    Public pointSize As Int32 = 2
    Public dataInput As New cv.Mat
    Public FOV As Single = 150
    Public yaw As Single = 0
    Public pitch As Single = 0
    Public roll As Single = 0
    Public zNear As Single = 0
    Public zFar As Single = 10.0
    Public eye As New cv.Vec3f(0, 0, -40)
    Public scaleXYZ As New cv.Vec3f(10, 10, 1)
    Public zTrans As Single = 0.5
    Public OpenGLTitle As String = "OpenGL_Basics"
    Public imageLabel As String
    Public pointCloudInput As New cv.Mat
    Dim openGLHeight = 1200
    Dim openGLWidth = 1500
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        desc = "Create an OpenGL window and update it with images"
    End Sub
    Private Sub memMapUpdate(ocvb As VBocvb)
        Dim timeConversionUnits As Double = 1000
        Dim imuAlphaFactor As Double = 0.98 ' theta is a mix of acceleration data and gyro data.
        If ocvb.parms.cameraIndex <> VB_Classes.ActiveTask.algParms.D435i Then
            timeConversionUnits = 1000 * 1000
            imuAlphaFactor = 0.99
        End If
        For i = 0 To memMapValues.Length - 1
            ' only change this if you are changing the data in the OpenGL C++ code at the same time...
            memMapValues(i) = Choose(i + 1, ocvb.frameCount, ocvb.parms.intrinsicsLeft.fx, ocvb.parms.intrinsicsLeft.fy, ocvb.parms.intrinsicsLeft.ppx, ocvb.parms.intrinsicsLeft.ppy,
                                                src.Width, src.Height, src.ElemSize * src.Total,
                                                dataInput.Total * dataInput.ElemSize, FOV, yaw, pitch, roll, zNear, zFar, pointSize, dataInput.Width, dataInput.Height,
                                                ocvb.IMU_AngularVelocity.X, ocvb.IMU_AngularVelocity.Y, ocvb.IMU_AngularVelocity.Z,
                                                ocvb.IMU_Acceleration.X, ocvb.IMU_Acceleration.Y, ocvb.IMU_Acceleration.Z, ocvb.IMU_TimeStamp,
                                                If(ocvb.parms.IMU_Present, 1, 0), eye.Item0 / 100, eye.Item1 / 100, eye.Item2 / 100, zTrans,
                                                scaleXYZ.Item0 / 10, scaleXYZ.Item1 / 10, scaleXYZ.Item2 / 10, timeConversionUnits, imuAlphaFactor,
                                                imageLabel.Length)
        Next
    End Sub
    Private Sub startOpenGLWindow(ocvb As VBocvb, pcSize As Integer)
        ' first setup the named pipe that will be used to feed data to the OpenGL window
        pipeName = "OpenCVBImages" + CStr(PipeTaskIndex)
        PipeTaskIndex += 1
        pipe = New NamedPipeServerStream(pipeName, PipeDirection.InOut, 1)

        memMapbufferSize = 8 * memMapValues.Length - 1

        startInfo.FileName = OpenGLTitle + ".exe"
        startInfo.Arguments = CStr(openGLWidth) + " " + CStr(openGLHeight) + " " + CStr(memMapbufferSize) + " " + pipeName + " " +
                                  CStr(pcSize)
        If ocvb.parms.ShowConsoleLog = False Then startInfo.WindowStyle = ProcessWindowStyle.Hidden
        Process.Start(startInfo)

        memMapPtr = Marshal.AllocHGlobal(memMapbufferSize)
        memMapFile = MemoryMappedFile.CreateOrOpen("OpenCVBControl", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)

        imageLabel = OpenGLTitle ' default title - can be overridden with each image.
        pipe.WaitForConnection()
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If standalone Then pointCloudInput = ocvb.pointCloud

        Dim pcSize = pointCloudInput.Total * pointCloudInput.ElemSize
        If ocvb.frameCount = 0 Then startOpenGLWindow(ocvb, pcSize)
        Dim readPipe(4) As Byte ' we read 4 bytes because that is the signal that the other end of the named pipe wrote 4 bytes to indicate iteration complete.
        If ocvb.frameCount > 0 And pipe IsNot Nothing Then
            Dim bytesRead = pipe.Read(readPipe, 0, 4)
            If bytesRead = 0 Then
                ocvb.trueText("The OpenGL process appears to have stopped.", 20, 100)
            End If
        End If

        Dim rgb = src.CvtColor(cv.ColorConversionCodes.BGR2RGB) ' OpenGL needs RGB, not BGR
        If rgbBuffer.Length <> rgb.Total * rgb.ElemSize Then ReDim rgbBuffer(rgb.Total * rgb.ElemSize - 1)
        If dataBuffer.Length <> dataInput.Total * dataInput.ElemSize Then ReDim dataBuffer(dataInput.Total * dataInput.ElemSize - 1)
        memMapUpdate(ocvb)

        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length - 1)

        If rgb.Width > 0 Then Marshal.Copy(rgb.Data, rgbBuffer, 0, rgbBuffer.Length)
        If dataInput.Width > 0 Then Marshal.Copy(dataInput.Data, dataBuffer, 0, dataBuffer.Length)
        If pointCloudBuffer.Length <> pointCloudInput.Total * pointCloudInput.ElemSize Then ReDim pointCloudBuffer(pointCloudInput.Total * pointCloudInput.ElemSize - 1)
        If pointCloudInput.Width > 0 Then Marshal.Copy(pointCloudInput.Data, pointCloudBuffer, 0, pcSize)

        If pipe.IsConnected Then
            On Error Resume Next
            pipe.Write(rgbBuffer, 0, rgbBuffer.Length)
            pipe.Write(dataBuffer, 0, dataBuffer.Length)
            pipe.Write(pointCloudBuffer, 0, pointCloudBuffer.Length)
            Dim buff = System.Text.Encoding.UTF8.GetBytes(imageLabel)
            pipe.Write(buff, 0, imageLabel.Length)
        End If
    End Sub
    Public Sub Close()
        Dim proc = Process.GetProcessesByName(OpenGLTitle)
        For i = 0 To proc.Count - 1
            proc(i).CloseMainWindow()
        Next i
        If memMapPtr <> 0 Then Marshal.FreeHGlobal(memMapPtr)
    End Sub
End Class



Module OpenGL_Sliders_Module
    Public Sub setOpenGLsliders(ocvb As VBocvb, caller As String, sliders As OptionsSliders)
        sliders.Setup(ocvb, caller, 15)

        sliders.setupTrackBar(0, "OpenGL FOV", 1, 180, 150)
        If ocvb.parms.cameraIndex = VB_Classes.ActiveTask.algParms.D435i Then sliders.trackbar(0).Value = 135
        sliders.setupTrackBar(1, "OpenGL yaw (degrees)", -180, 180, -3)
        sliders.setupTrackBar(2, "OpenGL pitch (degrees)", -180, 180, 3)
        sliders.setupTrackBar(3, "OpenGL roll (degrees)", -180, 180, 0)

        sliders.setupTrackBar(4, "OpenGL zNear", 0, 100, 0)
        sliders.setupTrackBar(5, "OpenGL zFar", -50, 200, 20)
        sliders.setupTrackBar(6, "OpenGL Point Size", 1, 20, 2)
        sliders.setupTrackBar(7, "zTrans", -1000, 1000, 50)

        sliders.setupTrackBar(8, "OpenGL Eye X", -180, 180, 0)
        sliders.setupTrackBar(9, "OpenGL Eye Y", -180, 180, 0)
        sliders.setupTrackBar(10, "OpenGL Eye Z", -180, 180, -40)

        sliders.setupTrackBar(11, "OpenGL Scale X", 1, 100, 10)
        sliders.setupTrackBar(12, "OpenGL Scale Y", 1, 100, 10)
        sliders.setupTrackBar(13, "OpenGL Scale Z", 1, 100, 1)
    End Sub
End Module




Public Class OpenGL_Options
    Inherits VBparent
    Public OpenGL As OpenGL_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        OpenGL = New OpenGL_Basics(ocvb)
        setOpenGLsliders(ocvb, caller, sliders)
        desc = "Adjust point size and FOV in OpenGL"
        label1 = ""
    End Sub
    Public Sub Run(ocvb As VBocvb)
        OpenGL.FOV = sliders.trackbar(0).Value
        OpenGL.yaw = sliders.trackbar(1).Value
        OpenGL.pitch = sliders.trackbar(2).Value
        OpenGL.roll = sliders.trackbar(3).Value

        OpenGL.zNear = sliders.trackbar(4).Value
        OpenGL.zFar = sliders.trackbar(5).Value
        OpenGL.pointSize = sliders.trackbar(6).Value
        OpenGL.zTrans = sliders.trackbar(7).Value / 100

        OpenGL.eye.Item0 = sliders.trackbar(8).Value
        OpenGL.eye.Item1 = sliders.trackbar(9).Value
        OpenGL.eye.Item2 = sliders.trackbar(10).Value

        OpenGL.scaleXYZ.Item0 = sliders.trackbar(11).Value
        OpenGL.scaleXYZ.Item1 = sliders.trackbar(12).Value
        OpenGL.scaleXYZ.Item2 = sliders.trackbar(13).Value

        OpenGL.src = src
        OpenGL.pointCloudInput = ocvb.pointCloud
        OpenGL.Run(ocvb)
    End Sub
End Class




Public Class OpenGL_Callbacks
    Inherits VBparent
    Public ogl As OpenGL_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        ogl = New OpenGL_Basics(ocvb)
        ogl.OpenGLTitle = "OpenGL_Callbacks"
        desc = "Show the point cloud of 3D data and use callbacks to modify view."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        ogl.src = src
        ogl.pointCloudInput = ocvb.pointCloud
        ogl.Run(ocvb)
    End Sub
End Class




'https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
Public Class OpenGL_IMU
    Inherits VBparent
    Public ogl As OpenGL_Options
    Public imu As IMU_GVector

    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        imu = New IMU_GVector(ocvb)

        ogl = New OpenGL_Options(ocvb)
        ogl.OpenGL.OpenGLTitle = "OpenGL_IMU"
        ogl.sliders.trackbar(1).Value = 0 ' pitch
        ogl.sliders.trackbar(2).Value = 0 ' yaw
        ogl.sliders.trackbar(3).Value = 0 ' roll
        ocvb.pointCloud = New cv.Mat ' we are not using the point cloud in this example.
        desc = "Show how to use IMU coordinates in OpenGL"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        imu.Run(ocvb)
        ogl.OpenGL.dataInput = New cv.Mat(100, 100, cv.MatType.CV_32F, 0)
        If ocvb.parms.IMU_Present Then
            ogl.src = src
            ogl.Run(ocvb) ' we are not moving any images to OpenGL - just the IMU value which are already in the memory mapped file.
        Else
            ocvb.trueText("No IMU present on this RealSense device", 20, 100)
        End If
    End Sub
End Class




Module histogram_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Histogram_3D_RGB(rgbPtr As IntPtr, rows As Int32, cols As Int32, bins As Int32) As IntPtr
    End Function
End Module




' https://docs.opencv.org/3.4/d1/d1d/tutorial_histo3D.html
Public Class OpenGL_3Ddata
    Inherits VBparent
    Dim colors As Palette_Gradient
    Public ogl As OpenGL_Options
    Dim histInput() As Byte
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Histogram Red/Green/Blue bins", 1, 128, 32) ' why 128 and not 256? There is some limit on the max pinned memory.  Not sure...

        ogl = New OpenGL_Options(ocvb)
        ogl.OpenGL.OpenGLTitle = "OpenGL_3Ddata"
        ogl.sliders.trackbar(1).Value = -10
        ogl.sliders.trackbar(6).Value = 5
        ogl.sliders.trackbar(2).Value = 10
        ocvb.pointCloud = New cv.Mat ' we are not using the point cloud when displaying data.

        colors = New Palette_Gradient(ocvb)
        colors.color1 = cv.Scalar.Yellow
        colors.color2 = cv.Scalar.Blue
        colors.Run(ocvb)
        ogl.OpenGL.src = dst1.Clone() ' only need to set this once.

        label1 = "Input to Histogram 3D"
        desc = "Plot the results of a 3D histogram in OpenGL."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim bins = sliders.trackbar(0).Value

        If histInput Is Nothing Then ReDim histInput(src.Total * src.ElemSize - 1)
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim handleRGB = GCHandle.Alloc(histInput, GCHandleType.Pinned) ' and pin it as well
        Dim dstPtr = Histogram_3D_RGB(handleRGB.AddrOfPinnedObject(), src.Rows, src.Cols, bins)
        handleRGB.Free() ' free the pinned memory...
        Dim dstData(bins * bins * bins - 1) As Single
        Marshal.Copy(dstPtr, dstData, 0, dstData.Length)
        Dim histogram = New cv.Mat(bins, bins, cv.MatType.CV_32FC(bins), dstData)
        histogram = histogram.Normalize(255)

        ogl.OpenGL.dataInput = histogram.Clone()
        ogl.src = src
        ogl.Run(ocvb)
    End Sub
End Class




Public Class OpenGL_Draw3D
    Inherits VBparent
    Dim circle As Draw_Circles
    Public ogl As OpenGL_Options
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        circle = New Draw_Circles(ocvb)
        circle.sliders.trackbar(0).Value = 5

        ogl = New OpenGL_Options(ocvb)
        ogl.OpenGL.OpenGLTitle = "OpenGL_3DShapes"
        ogl.sliders.trackbar(0).Value = 80
        ogl.sliders.trackbar(8).Value = -140
        ogl.sliders.trackbar(9).Value = -180
        ogl.sliders.trackbar(6).Value = 16
        ogl.sliders.trackbar(10).Value = -30
        ocvb.pointCloud = New cv.Mat ' we are not using the point cloud when displaying data.
        label2 = "Grayscale image sent to OpenGL"
        desc = "Draw in an image show it in 3D in OpenGL without any explicit math"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        circle.Run(ocvb)
        dst2 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ogl.OpenGL.dataInput = dst2
        ogl.OpenGL.src = New cv.Mat(1, rColors.Length - 1, cv.MatType.CV_8UC3, rColors.ToArray)
        ogl.src = src
        ogl.Run(ocvb)
    End Sub
End Class





Public Class OpenGL_Voxels
    Inherits VBparent
    Public voxels As Voxels_Basics_MT
    Public ogl As OpenGL_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        voxels = New Voxels_Basics_MT(ocvb)
        voxels.check.Box(0).Checked = False

        ogl = New OpenGL_Basics(ocvb)
        ogl.OpenGLTitle = "OpenGL_Voxels"
        desc = "Show the voxel representation in OpenGL"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        voxels.src = src
        voxels.Run(ocvb)

        ogl.dataInput = New cv.Mat(voxels.grid.tilesPerCol, voxels.grid.tilesPerRow, cv.MatType.CV_64F, voxels.voxels)
        ogl.dataInput *= 1 / (voxels.maxDepth - voxels.minDepth)
        ogl.src = src
        ogl.Run(ocvb)
    End Sub
End Class






' https://open.gl/transformations
' https://www.codeproject.com/Articles/1247960/Learning-Basic-Math-Used-In-3D-Graphics-Engines
Public Class OpenGL_GravityTransform
    Inherits VBparent
    Public ogl As OpenGL_Basics
    Public gCloud As Depth_PointCloudInRange_IMU
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        gCloud = New Depth_PointCloudInRange_IMU(ocvb)
        gCloud.histOpts = New Histogram_ProjectionOptions(ocvb)

        ogl = New OpenGL_Basics(ocvb)
        ogl.OpenGLTitle = "OpenGL_Callbacks"

        radio.Setup(ocvb, caller, 4)
        radio.check(0).Text = "Rotate around X-axis using gravity vector"
        radio.check(1).Text = "Rotate around Z-axis using gravity vector"
        radio.check(2).Text = "Rotate around both X-axis and Z-axis using gravity vector"
        radio.check(3).Text = "No rotation"
        radio.check(2).Checked = True

        desc = "Use the IMU's acceleration values to build the transformation matrix of an OpenGL viewer"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If radio.check(0).Checked Then
            gCloud.xRotation = True
            gCloud.zRotation = False
        End If
        If radio.check(1).Checked Then
            gCloud.xRotation = False
            gCloud.zRotation = True
        End If
        If radio.check(2).Checked Then
            gCloud.xRotation = True
            gCloud.zRotation = True
        End If
        If radio.check(3).Checked Then
            gCloud.xRotation = False
            gCloud.zRotation = False
        End If

        gCloud.Run(ocvb)
        cv.Cv2.Merge(gCloud.split, ogl.pointCloudInput)
        ogl.src = src
        ogl.Run(ocvb)
    End Sub
End Class




