Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes

Public Class OpenGL_Basics
    Inherits ocvbClass
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
    Public rgbInput As New cv.Mat
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
    Public imu As IMU_GVector
    Public pointCloudInput As New cv.Mat
        Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        imu = New IMU_GVector(ocvb, caller)
        ' Dispose() ' make sure there wasn't an old OpenGLWindow sitting around...
        ocvb.desc = "Create an OpenGL window and update it with images"
    End Sub
    Private Sub memMapUpdate(ocvb As AlgorithmData)
        Dim timeConversionUnits As Double = 1000
        Dim imuAlphaFactor As Double = 0.98 ' theta is a mix of acceleration data and gyro data.
        If ocvb.parms.cameraIndex <> D400Cam Then
            timeConversionUnits = 1000 * 1000
            imuAlphaFactor = 0.99
        End If
        ' setup the memory mapped area and initialize the intrinsicsLeft needed to convert imageXYZ to worldXYZ and for command/control of the interface.
        For i = 0 To memMapValues.Length - 1
            ' only change this if you are changing the data in the OpenGL C++ code at the same time...
            memMapValues(i) = Choose(i + 1, ocvb.frameCount, ocvb.parms.intrinsicsLeft.fx, ocvb.parms.intrinsicsLeft.fy, ocvb.parms.intrinsicsLeft.ppx, ocvb.parms.intrinsicsLeft.ppy,
                                                rgbInput.Width, rgbInput.Height, rgbInput.ElemSize * rgbInput.Total,
                                                dataInput.Total * dataInput.ElemSize, FOV, yaw, pitch, roll, zNear, zFar, pointSize, dataInput.Width, dataInput.Height,
                                                ocvb.parms.IMU_AngularVelocity.X, ocvb.parms.IMU_AngularVelocity.Y, ocvb.parms.IMU_AngularVelocity.Z,
                                                ocvb.parms.IMU_Acceleration.X, ocvb.parms.IMU_Acceleration.Y, ocvb.parms.IMU_Acceleration.Z, ocvb.parms.IMU_TimeStamp,
                                                If(ocvb.parms.IMU_Present, 1, 0), eye.Item0 / 100, eye.Item1 / 100, eye.Item2 / 100, zTrans,
                                                scaleXYZ.Item0 / 10, scaleXYZ.Item1 / 10, scaleXYZ.Item2 / 10, timeConversionUnits, imuAlphaFactor,
                                                imageLabel.Length)
        Next
    End Sub
    Private Sub startOpenGLWindow(ocvb As AlgorithmData, pcSize As Integer)
        ' first setup the named pipe that will be used to feed data to the OpenGL window
        pipeName = "OpenCVBImages" + CStr(PipeTaskIndex)
        PipeTaskIndex += 1
        pipe = New NamedPipeServerStream(pipeName, PipeDirection.InOut, 1)

        memMapbufferSize = 8 * memMapValues.Length - 1

        startInfo.FileName = OpenGLTitle + ".exe"
        startInfo.Arguments = CStr(ocvb.openGLWidth) + " " + CStr(ocvb.openGLHeight) + " " + CStr(memMapbufferSize) + " " + pipeName + " " +
                                  CStr(pcSize)
        If ocvb.parms.ShowConsoleLog = False Then startInfo.WindowStyle = ProcessWindowStyle.Hidden
        Process.Start(startInfo)

        memMapPtr = Marshal.AllocHGlobal(memMapbufferSize)
        memMapFile = MemoryMappedFile.CreateOrOpen("OpenCVBControl", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)

        imageLabel = OpenGLTitle ' default title - can be overridden with each image.
        pipe.WaitForConnection()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex = T265Camera Then
            ocvb.putText(New ActiveClass.TrueType("The T265 camera doesn't have a point cloud.", 10, 60, RESULT1))
            Exit Sub
        End If

        if standalone Then pointCloudInput = ocvb.pointCloud
        imu.Run(ocvb)

        Dim pcSize = pointCloudInput.Total * pointCloudInput.ElemSize
        If ocvb.frameCount = 0 Then startOpenGLWindow(ocvb, pcSize)
        Dim readPipe(4) As Byte ' we read 4 bytes because that is the signal that the other end of the named pipe wrote 4 bytes to indicate iteration complete.
        If ocvb.frameCount > 0 And pipe IsNot Nothing Then
            Dim bytesRead = pipe.Read(readPipe, 0, 4)
            If bytesRead = 0 Then
                ocvb.putText(New ActiveClass.TrueType("The OpenGL process appears to have stopped.", 20, 100))
            End If
        End If

        If rgbInput.Width = 0 Then rgbInput = ocvb.color
        Dim rgb = rgbInput.CvtColor(cv.ColorConversionCodes.BGR2RGB) ' OpenGL needs RGB, not BGR
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
    Public Sub setOpenGLsliders(ocvb As AlgorithmData, caller As String, sliders As OptionsSliders, sliders1 As OptionsSliders, sliders2 As OptionsSliders, sliders3 As OptionsSliders)
        sliders1.setupTrackBar1(ocvb, caller, "OpenGL zNear", 0, 100, 0)
        sliders1.setupTrackBar2(ocvb, caller, "OpenGL zFar", -50, 200, 20)
        sliders1.setupTrackBar3(ocvb, caller, "OpenGL Point Size", 1, 20, 2)
        sliders1.setupTrackBar4(ocvb, caller, "zTrans", -1000, 1000, 50)
        If ocvb.parms.ShowOptions Then sliders1.Show()

        sliders2.setupTrackBar1(ocvb, caller, "OpenGL Eye X", -180, 180, 0)
        sliders2.setupTrackBar2(ocvb, caller, "OpenGL Eye Y", -180, 180, 0)
        sliders2.setupTrackBar3(ocvb, caller, "OpenGL Eye Z", -180, 180, -40)
        If ocvb.parms.ShowOptions Then sliders2.Show()

        sliders3.setupTrackBar1(ocvb, caller, "OpenGL Scale X", 1, 100, 10)
        sliders3.setupTrackBar2(ocvb, caller, "OpenGL Scale Y", 1, 100, 10)
        sliders3.setupTrackBar3(ocvb, caller, "OpenGL Scale Z", 1, 100, 1)
        If ocvb.parms.ShowOptions Then sliders3.Show()

        ' this is last so it shows up on top of all the others.
        sliders.setupTrackBar1(ocvb, caller, "OpenGL FOV", 1, 180, 150)
        If ocvb.parms.cameraIndex = D400Cam Then sliders.TrackBar1.Value = 135
        sliders.setupTrackBar2(ocvb, caller, "OpenGL yaw (degrees)", -180, 180, -3)
        sliders.setupTrackBar3(ocvb, caller, "OpenGL pitch (degrees)", -180, 180, 3)
        sliders.setupTrackBar4(ocvb, caller, "OpenGL roll (degrees)", -180, 180, 0)
    End Sub
End Module




Public Class OpenGL_Options
    Inherits ocvbClass
    Public OpenGL As OpenGL_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        OpenGL = New OpenGL_Basics(ocvb, caller)
        setOpenGLsliders(ocvb, caller, sliders, sliders1, sliders2, sliders3)
        ocvb.desc = "Adjust point size and FOV in OpenGL"
        ocvb.label1 = ""
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        OpenGL.FOV = sliders.TrackBar1.Value
        OpenGL.yaw = sliders.TrackBar2.Value
        OpenGL.pitch = sliders.TrackBar3.Value
        OpenGL.roll = sliders.TrackBar4.Value

        OpenGL.zNear = sliders1.TrackBar1.Value
        OpenGL.zFar = sliders1.TrackBar2.Value
        OpenGL.pointSize = sliders1.TrackBar3.Value
        OpenGL.zTrans = sliders1.TrackBar4.Value / 100

        OpenGL.eye.Item0 = sliders2.TrackBar1.Value
        OpenGL.eye.Item1 = sliders2.TrackBar2.Value
        OpenGL.eye.Item2 = sliders2.TrackBar3.Value

        OpenGL.scaleXYZ.Item0 = sliders3.TrackBar1.Value
        OpenGL.scaleXYZ.Item1 = sliders3.TrackBar2.Value
        OpenGL.scaleXYZ.Item2 = sliders3.TrackBar3.Value

        OpenGL.Run(ocvb)
    End Sub
End Class




Public Class OpenGL_Callbacks
    Inherits ocvbClass
    Public ogl As OpenGL_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ogl = New OpenGL_Basics(ocvb, caller)
        ogl.OpenGLTitle = "OpenGL_Callbacks"
        ocvb.desc = "Show the point cloud of 3D data and use callbacks to modify view."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ogl.rgbInput = ocvb.color
        ogl.Run(ocvb)
    End Sub
End Class




'https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
Public Class OpenGL_IMU
    Inherits ocvbClass
    Public ogl As OpenGL_Options
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.parms.ShowOptions = False
        ogl = New OpenGL_Options(ocvb, caller)
        ogl.OpenGL.OpenGLTitle = "OpenGL_IMU"
        ogl.sliders.TrackBar2.Value = 0 ' pitch
        ogl.sliders.TrackBar3.Value = 0 ' yaw
        ogl.sliders.TrackBar4.Value = 0 ' roll
        ocvb.pointCloud = New cv.Mat ' we are not using the point cloud in this example.
        ocvb.desc = "Show how to use IMU coordinates in OpenGL"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ogl.OpenGL.dataInput = New cv.Mat(100, 100, cv.MatType.CV_32F, 0)
        If ocvb.parms.IMU_Present Then
            ogl.Run(ocvb) ' we are not moving any images to OpenGL - just the IMU value which are already in the memory mapped file.
        Else
            ocvb.putText(New ActiveClass.TrueType("No IMU present on this RealSense device", 20, 100))
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
    Inherits ocvbClass
    Dim colors As Palette_Gradient
    Public ogl As OpenGL_Options
    Dim histInput() As Byte
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Histogram Red/Green/Blue bins", 1, 128, 32) ' why 128 and not 256? There is some limit on the max pinned memory.  Not sure...

        ogl = New OpenGL_Options(ocvb, caller)
        ogl.OpenGL.OpenGLTitle = "OpenGL_3Ddata"
        ogl.sliders.TrackBar2.Value = -10
        ogl.sliders1.TrackBar3.Value = 5
        ogl.sliders.TrackBar3.Value = 10
        ocvb.pointCloud = New cv.Mat ' we are not using the point cloud when displaying data.

        colors = New Palette_Gradient(ocvb, caller)
        colors.color1 = cv.Scalar.Yellow
        colors.color2 = cv.Scalar.Blue
        colors.Run(ocvb)
        ogl.OpenGL.rgbInput = dst.Clone() ' only need to set this once.

        ocvb.label1 = "Input to Histogram 3D"
        ocvb.desc = "Plot the results of a 3D histogram in OpenGL."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim bins = sliders.TrackBar1.Value

        If histInput Is Nothing Then ReDim histInput(ocvb.color.Total * ocvb.color.ElemSize - 1)
        Marshal.Copy(ocvb.color.Data, histInput, 0, histInput.Length)

        Dim handleRGB = GCHandle.Alloc(histInput, GCHandleType.Pinned) ' and pin it as well
        Dim dstPtr = Histogram_3D_RGB(handleRGB.AddrOfPinnedObject(), ocvb.color.Rows, ocvb.color.Cols, bins)
        handleRGB.Free() ' free the pinned memory...
        Dim dstData(bins * bins * bins - 1) As Single
        Marshal.Copy(dstPtr, dstData, 0, dstData.Length)
        Dim histogram = New cv.Mat(bins, bins, cv.MatType.CV_32FC(bins), dstData)
        histogram = histogram.Normalize(255)

        ogl.OpenGL.dataInput = histogram.Clone()
        ogl.Run(ocvb)
    End Sub
End Class




Public Class OpenGL_Draw3D
    Inherits ocvbClass
    Dim circle As Draw_Circles
    Public ogl As OpenGL_Options
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        circle = New Draw_Circles(ocvb, caller)
        circle.sliders.TrackBar1.Value = 5

        ogl = New OpenGL_Options(ocvb, caller)
        ogl.OpenGL.OpenGLTitle = "OpenGL_3DShapes"
        ogl.sliders.TrackBar1.Value = 80
        ogl.sliders2.TrackBar1.Value = -140
        ogl.sliders2.TrackBar2.Value = -180
        ogl.sliders1.TrackBar3.Value = 16
        ogl.sliders2.TrackBar3.Value = -30
        ocvb.pointCloud = New cv.Mat ' we are not using the point cloud when displaying data.
        ocvb.label2 = "Grayscale image sent to OpenGL"
        ocvb.desc = "Draw in an image show it in 3D in OpenGL without any explicit math"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        circle.Run(ocvb)
        ocvb.result2 = dst.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ogl.OpenGL.dataInput = ocvb.result2
        ogl.OpenGL.rgbInput = New cv.Mat(1, ocvb.rColors.Length - 1, cv.MatType.CV_8UC3, ocvb.rColors.ToArray)
        ogl.Run(ocvb)
    End Sub
End Class





Public Class OpenGL_Voxels
    Inherits ocvbClass
    Public voxels As Voxels_Basics_MT
    Public ogl As OpenGL_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        voxels = New Voxels_Basics_MT(ocvb, caller)
        voxels.check.Box(0).Checked = False

        ogl = New OpenGL_Basics(ocvb, caller)
        ogl.OpenGLTitle = "OpenGL_Voxels"
        ocvb.desc = "Show the voxel representation in OpenGL"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        voxels.Run(ocvb)

        ogl.dataInput = New cv.Mat(voxels.grid.tilesPerCol, voxels.grid.tilesPerRow, cv.MatType.CV_64F, voxels.voxels)
        ogl.dataInput *= 1 / (voxels.maxDepth - voxels.minDepth)
        ogl.Run(ocvb)
    End Sub
End Class






' https://open.gl/transformations
' https://www.codeproject.com/Articles/1247960/Learning-Basic-Math-Used-In-3D-Graphics-Engines
Public Class OpenGL_GravityTransform
    Inherits ocvbClass
    Dim imu As IMU_GVector
    Public ogl As OpenGL_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        imu = New IMU_GVector(ocvb, caller)
        ogl = New OpenGL_Basics(ocvb, caller)
        ogl.OpenGLTitle = "OpenGL_Callbacks"
        ocvb.desc = "Use the IMU's acceleration values to build the transformation matrix of an OpenGL viewer"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex = T265Camera Then
            ocvb.putText(New ActiveClass.TrueType("The T265 camera doesn't have a point cloud.", 10, 60, RESULT1))
            Exit Sub
        End If

        imu.Run(ocvb)
        Static rotateFlag = 0
        Dim split() = cv.Cv2.Split(ocvb.pointCloud)
        Dim vertSplit = split

        Dim zCos = Math.Cos(imu.angleZ)
        Dim zSin = Math.Sin(imu.angleZ)

        Dim xCos = Math.Cos(imu.angleX)
        Dim xSin = Math.Sin(imu.angleX)

        Select Case rotateFlag Mod 4
            Case 0 ' rotate around x-axis - AKA YZ plane rotation matrix
                vertSplit(1) = zCos * split(1) - zSin * split(2)
                vertSplit(2) = zSin * split(1) + zCos * split(2)
                ogl.imageLabel = "Rotating around the x-axis"
            Case 1 ' rotate around z-axis - AKA XY plane rotation
                vertSplit(0) = xCos * split(0) - xSin * split(1)
                vertSplit(1) = xSin * split(0) + xCos * split(1)
                ogl.imageLabel = "Rotating around the z-axis"
            Case 2 ' rotate around x-axis and z-axis
                Dim xArray(,) As Single = {{1, 0, 0, 0}, {0, zCos, -zSin, 0}, {0, zSin, zCos, 0}, {0, 0, 0, 1}}
                Dim xRotate = New cv.Mat(4, 4, cv.MatType.CV_32F, xArray)

                Dim zArray(,) As Single = {{xCos, -xSin, 0, 0}, {xSin, xCos, 0, 0}, {0, 0, 1, 0}, {0, 0, 0, 1}}
                Dim zRotate = New cv.Mat(4, 4, cv.MatType.CV_32F, zArray)
                Dim yRotate = (xRotate * zRotate).ToMat

                Dim xz(4 * 4) As Single
                For j = 0 To yRotate.Rows - 1
                    For i = 0 To yRotate.Cols - 1
                        xz(i * 4 + j) = yRotate.Get(Of Single)(i, j)
                    Next
                Next

                vertSplit(0) = xz(0) * split(0) + xz(1) * split(1) + xz(2) * split(2)
                vertSplit(1) = xz(4) * split(0) + xz(5) * split(1) + xz(6) * split(2)
                vertSplit(2) = xz(8) * split(0) + xz(9) * split(1) + xz(10) * split(2)
                ogl.imageLabel = "Rotating around the x-axis and the z-axis"
            Case 3
                ogl.imageLabel = "No rotation "
        End Select
        cv.Cv2.Merge(vertSplit, ogl.pointCloudInput)

        ogl.rgbInput = ocvb.color
        ogl.Run(ocvb)
        If ocvb.frameCount Mod 30 = 0 Then rotateFlag += 1
    End Sub
End Class




