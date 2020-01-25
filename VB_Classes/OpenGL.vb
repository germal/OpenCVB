Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes

Public Class OpenGL_Basics : Implements IDisposable
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
    Public Sub New(ocvb As AlgorithmData)
        Dispose() ' make sure there wasn't an old OpenGLWindow sitting around...
        ocvb.desc = "Create an OpenGL window and update it with images"
    End Sub
    Private Sub memMapUpdate(ocvb As AlgorithmData)
        Dim timeConversionUnits As Double = 1000
        Dim imuAlphaFactor As Double = 0.98 ' theta is a mix of acceleration data and gyro data.
        If ocvb.parms.UsingIntelCamera = False Then
            timeConversionUnits = 1000 * 1000
            imuAlphaFactor = 0.99
        End If
        ' setup the memory mapped area and initialize the intrinsics needed to convert imageXYZ to worldXYZ and for command/control of the interface.
        For i = 0 To memMapValues.Length - 1
            ' only change this if you are changing the data in the OpenGL C++ code at the same time...
            memMapValues(i) = Choose(i + 1, ocvb.frameCount, ocvb.parms.intrinsics.fx, ocvb.parms.intrinsics.fy, ocvb.parms.intrinsics.ppx, ocvb.parms.intrinsics.ppy,
                                            rgbInput.Width, rgbInput.Height, rgbInput.ElemSize * rgbInput.Total,
                                            dataInput.Total * dataInput.ElemSize, FOV, yaw, pitch, roll, zNear, zFar, pointSize, dataInput.Width, dataInput.Height,
                                            ocvb.parms.imuGyro.X, ocvb.parms.imuGyro.Y, ocvb.parms.imuGyro.Z,
                                            ocvb.parms.imuAccel.X, ocvb.parms.imuAccel.Y, ocvb.parms.imuAccel.Z, ocvb.parms.imuTimeStamp, If(ocvb.parms.IMUpresent, 1, 0),
                                            eye.Item0 / 100, eye.Item1 / 100, eye.Item2 / 100, zTrans, scaleXYZ.Item0 / 10, scaleXYZ.Item1 / 10, scaleXYZ.Item2 / 10,
                                            timeConversionUnits, imuAlphaFactor)
        Next
    End Sub
    Private Sub startOpenGLWindow(ocvb As AlgorithmData)
        ' first setup the named pipe that will be used to feed data to the OpenGL window
        pipeName = "OpenCVBImages" + CStr(PipeTaskIndex)
        PipeTaskIndex += 1
        pipe = New NamedPipeServerStream(pipeName, PipeDirection.InOut, 1)

        memMapbufferSize = 8 * (memMapValues.Length - 1)

        startInfo.FileName = OpenGLTitle + ".exe"
        Dim pcSize = ocvb.pointCloud.Total * ocvb.pointCloud.ElemSize
        startInfo.Arguments = CStr(ocvb.openGLWidth) + " " + CStr(ocvb.openGLHeight) + " " + CStr(memMapbufferSize) + " " + pipeName + " " +
                              CStr(pcSize)
        If ocvb.parms.ShowConsoleLog = False Then startInfo.WindowStyle = ProcessWindowStyle.Hidden
        Process.Start(startInfo)

        memMapPtr = Marshal.AllocHGlobal(memMapbufferSize)
        memMapFile = MemoryMappedFile.CreateOrOpen("OpenCVBControl", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)

        pipe.WaitForConnection()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount = 0 Then startOpenGLWindow(ocvb)
        Dim pcSize = ocvb.pointCloud.Total * ocvb.pointCloud.ElemSize

        Dim readPipe(4) As Byte ' we read 4 bytes because that is the signal that the other end of the named pipe wrote 4 bytes to indicate iteration complete.
        If ocvb.frameCount > 0 Then
            Dim bytesRead = pipe.Read(readPipe, 0, 4)
            If bytesRead = 0 Then
                ocvb.putText(New ActiveClass.TrueType("The OpenGL process appears to have stopped.", 20, 100))
            End If
        End If

        If rgbInput.Width = 0 Then rgbInput = ocvb.color

        If pointCloudBuffer.Length <> ocvb.pointCloud.Total * ocvb.pointCloud.ElemSize Then
            ReDim pointCloudBuffer(ocvb.pointCloud.Total * ocvb.pointCloud.ElemSize - 1)
        End If

        Dim rgb = rgbInput.CvtColor(cv.ColorConversionCodes.BGR2RGB) ' OpenGL needs RGB, not BGR
        If rgbBuffer.Length <> rgb.Total * rgb.ElemSize Then ReDim rgbBuffer(rgb.Total * rgb.ElemSize - 1)
        If dataBuffer.Length <> dataInput.Total * dataInput.ElemSize Then ReDim dataBuffer(dataInput.Total * dataInput.ElemSize - 1)
        memMapUpdate(ocvb)

        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length - 1)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length - 1)

        If rgb.Width > 0 Then Marshal.Copy(rgb.Data, rgbBuffer, 0, rgbBuffer.Length)
        If dataInput.Width > 0 Then Marshal.Copy(dataInput.Data, dataBuffer, 0, dataBuffer.Length)
        If ocvb.pointCloud.Width > 0 Then Marshal.Copy(ocvb.pointCloud.Data, pointCloudBuffer, 0, pcSize)

        If pipe.IsConnected Then
            On Error Resume Next
            pipe.Write(rgbBuffer, 0, rgbBuffer.Length)
            pipe.Write(dataBuffer, 0, dataBuffer.Length)
            pipe.Write(pointCloudBuffer, 0, pointCloudBuffer.Length)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If pipe IsNot Nothing Then
            If pipe.IsConnected Then
                pipe.Flush()
                pipe.WaitForPipeDrain()
                pipe.Disconnect()
            End If
        End If
        Dim proc = Process.GetProcessesByName(OpenGLTitle)
        For i = 0 To proc.Count - 1
            proc(i).CloseMainWindow()
        Next i
        If memMapPtr <> 0 Then Marshal.FreeHGlobal(memMapPtr)
    End Sub
End Class



Module OpenGL_Sliders_Module
    Public Sub setOpenGLsliders(ocvb As AlgorithmData, sliders As OptionsSliders, sliders1 As OptionsSliders, sliders2 As OptionsSliders, sliders3 As OptionsSliders)
        sliders1.setupTrackBar1(ocvb, "OpenGL zNear", 0, 100, 0)
        sliders1.setupTrackBar2(ocvb, "OpenGL zFar", -50, 200, 20)
        sliders1.setupTrackBar3(ocvb, "OpenGL Point Size", 1, 20, 2)
        sliders1.setupTrackBar4(ocvb, "zTrans", -1000, 1000, 50)
        If ocvb.parms.ShowOptions Then sliders1.Show()

        sliders2.setupTrackBar1(ocvb, "OpenGL Eye X", -180, 180, 0)
        sliders2.setupTrackBar2(ocvb, "OpenGL Eye Y", -180, 180, 0)
        sliders2.setupTrackBar3(ocvb, "OpenGL Eye Z", -180, 180, -40)
        If ocvb.parms.ShowOptions Then sliders2.Show()

        sliders3.setupTrackBar1(ocvb, "OpenGL Scale X", 1, 100, 10)
        sliders3.setupTrackBar2(ocvb, "OpenGL Scale Y", 1, 100, 10)
        sliders3.setupTrackBar3(ocvb, "OpenGL Scale Z", 1, 100, 1)
        If ocvb.parms.ShowOptions Then sliders3.Show()

        ' this is last so it shows up on top of all the others.
        sliders.setupTrackBar1(ocvb, "OpenGL FOV", 1, 180, 150)
        If ocvb.parms.UsingIntelCamera Then sliders.TrackBar1.Value = 135
        sliders.setupTrackBar2(ocvb, "OpenGL yaw (degrees)", -180, 180, -3)
        sliders.setupTrackBar3(ocvb, "OpenGL pitch (degrees)", -180, 180, 3)
        sliders.setupTrackBar4(ocvb, "OpenGL roll (degrees)", -180, 180, 0)
        If ocvb.parms.ShowOptions Then sliders.Show()
    End Sub
End Module




Public Class OpenGL_Options : Implements IDisposable
    Public sliders As New OptionsSliders
    Public sliders1 As New OptionsSliders
    Public sliders2 As New OptionsSliders
    Public sliders3 As New OptionsSliders
    Public OpenGL As OpenGL_Basics
    Public Sub New(ocvb As AlgorithmData)
        OpenGL = New OpenGL_Basics(ocvb)
        setOpenGLsliders(ocvb, sliders, sliders1, sliders2, sliders3)
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
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        sliders1.Dispose()
        sliders2.Dispose()
        sliders3.Dispose()
        OpenGL.Dispose()
    End Sub
End Class




Public Class OpenGL_Callbacks : Implements IDisposable
    Public ogl As OpenGL_Basics
    Public Sub New(ocvb As AlgorithmData)
        ogl = New OpenGL_Basics(ocvb)
        ogl.OpenGLTitle = "OpenGL_Callbacks"
        ocvb.desc = "Show the point cloud of 3D data and use callbacks to modify view."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ogl.rgbInput = ocvb.color
        ogl.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        ogl.Dispose()
    End Sub
End Class




'https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
Public Class OpenGL_IMU : Implements IDisposable
    Public ogl As OpenGL_Options
    Public Sub New(ocvb As AlgorithmData)
        ogl = New OpenGL_Options(ocvb)
        ogl.OpenGL.OpenGLTitle = "OpenGL_IMU"
        ogl.sliders.TrackBar2.Value = 0 ' pitch
        ogl.sliders.TrackBar3.Value = 0 ' yaw
        ogl.sliders.TrackBar4.Value = 0 ' roll
        ocvb.pointCloud = New cv.Mat ' we are not using the point in this example.
        ocvb.desc = "Show how to use IMU coordinates in OpenGL"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ogl.OpenGL.rgbInput = ocvb.color ' we need to send something to keep the pipe flowing and getting the ack back.
        ogl.OpenGL.dataInput = New cv.Mat(100, 100, cv.MatType.CV_32F, 0)
        If ocvb.parms.IMUpresent Then
            ogl.Run(ocvb) ' we are not moving any images to OpenGL - just the IMU value which are already in the memory mapped file.
        Else
            ocvb.putText(New ActiveClass.TrueType("No IMU present on this RealSense device", 20, 100))
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        ogl.Dispose()
    End Sub
End Class




Module histogram_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Histogram_3D_RGB(rgbPtr As IntPtr, rows As Int32, cols As Int32, bins As Int32) As IntPtr
    End Function
End Module




' https://docs.opencv.org/3.4/d1/d1d/tutorial_histo3D.html
Public Class OpenGL_3Ddata : Implements IDisposable
    Dim colors As Palette_Gradient
    Public ogl As OpenGL_Options
    Dim sliders As New OptionsSliders
    Dim histInput() As Byte
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Histogram Red/Green/Blue bins", 1, 128, 32) ' why 128 and not 256? There is some limit on the max pinned memory.  Not sure...
        If ocvb.parms.ShowOptions Then sliders.Show()

        ogl = New OpenGL_Options(ocvb)
        ogl.OpenGL.OpenGLTitle = "OpenGL_3Ddata"
        ogl.sliders.TrackBar2.Value = -10
        ogl.sliders1.TrackBar3.Value = 5
        ogl.sliders.TrackBar3.Value = 10
        ocvb.pointCloud = New cv.Mat ' we are not using the point cloud when displaying data.

        colors = New Palette_Gradient(ocvb)
        colors.externalUse = True
        colors.color1 = cv.Scalar.Yellow
        colors.color2 = cv.Scalar.Blue
        colors.Run(ocvb)
        ogl.OpenGL.rgbInput = ocvb.result1.Clone() ' only need to set this once.

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
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        ogl.Dispose()
    End Sub
End Class




Public Class OpenGL_Draw3D : Implements IDisposable
    Dim circle As Draw_Circles
    Public ogl As OpenGL_Options
    Public Sub New(ocvb As AlgorithmData)
        circle = New Draw_Circles(ocvb)
        circle.sliders.TrackBar1.Value = 5

        ogl = New OpenGL_Options(ocvb)
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
        ocvb.result2 = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ogl.OpenGL.dataInput = ocvb.result2
        ogl.OpenGL.rgbInput = New cv.Mat(1, ocvb.rColors.Length - 1, cv.MatType.CV_8UC3, ocvb.rColors.ToArray)
        ogl.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        ogl.Dispose()
        circle.Dispose()
    End Sub
End Class





Public Class OpenGL_Voxels : Implements IDisposable
    Public voxels As Voxels_Basics_MT
    Public ogl As OpenGL_Basics
    Public Sub New(ocvb As AlgorithmData)
        voxels = New Voxels_Basics_MT(ocvb)
        voxels.check.Box(0).Checked = False

        ogl = New OpenGL_Basics(ocvb)
        ogl.OpenGLTitle = "OpenGL_Voxels"
        ocvb.desc = "Show the voxel representation in OpenGL"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        voxels.Run(ocvb)

        ogl.rgbInput = ocvb.color
        ogl.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        voxels.Dispose()
        ogl.Dispose()
    End Sub
End Class
