Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports System.Threading
Public Class IntelD400Series : Implements IDisposable
    Dim ctx As New rs.Context
    Dim cfg As New rs.Config
    Dim devices As rs.DeviceList
    Dim device As rs.Device
    Dim pipeline As New rs.Pipeline
    Dim pipeline_profile As rs.PipelineProfile
    Dim colorizer As New rs.Colorizer
    Dim depth2Disparity As New rs.DisparityTransform
    Dim align As rs.Align
    Dim sensor As rs.Sensor
    Dim blocks As List(Of rs.ProcessingBlock)
    Public intrinsics_VB As VB_Classes.ActiveClass.Intrinsics_VB
    Public Extrinsics_VB As VB_Classes.ActiveClass.Extrinsics_VB
    Public modelInverse As Boolean
    Public IMUpresent As Boolean

    Dim w As Int32
    Dim h As Int32

    Public failedImageCount As Int32

    Public deviceCount As Int32
    Public deviceName As String

    Public color As cv.Mat
    Public depth As cv.Mat
    Public depthRGB As cv.Mat
    Public disparity As cv.Mat
    Public redLeft As cv.Mat
    Public redRight As cv.Mat

    Dim colorBytes() As Byte
    Dim depthBytes() As Byte
    Dim disparityBytes() As Byte
    Dim depthRGBBytes() As Byte
    Dim redLeftBytes() As Byte
    Dim redRightBytes() As Byte
    Public imuGyro As cv.Point3f
    Public imuAccel As cv.Point3f
    Public imuTimeStamp As Double
    Public DecimationFilter As Boolean
    Public ThresholdFilter As Boolean
    Public DepthToDisparity As Boolean
    Public SpatialFilter As Boolean
    Public TemporalFilter As Boolean
    Public HoleFillingFilter As Boolean
    Public DisparityToDepth As Boolean
    Public pc As New rs.PointCloud

    Dim vertices() As Byte
    Public pointCloud As cv.Mat

    Dim block As New rs.CustomProcessingBlock(
        Sub(f, src)
            Using varRelease = New rs.FramesReleaser
                Try
                    For Each p In blocks
                        If p.Info.Item(0) = "Decimation Filter" And DecimationFilter Then f = p.Process(f)
                        If p.Info.Item(0) = "Threshold Filter" And ThresholdFilter Then f = p.Process(f)
                        If p.Info.Item(0) = "Depth to Disparity" Then f = p.Process(f) ' always have depth to disparity
                        If p.Info.Item(0) = "Spatial Filter" And SpatialFilter Then f = p.Process(f)
                        If p.Info.Item(0) = "Temporal Filter" And TemporalFilter Then f = p.Process(f)
                        If p.Info.Item(0) = "Hole Filling Filter" And HoleFillingFilter Then f = p.Process(f)
                        If p.Info.Item(0) = "Disparity to Depth" Then f = p.Process(f) ' always have disparity to depth
                    Next

                    f = align.Process(f)
                    f = colorizer.Process(f)

                    Dim frames = f.As(Of rs.FrameSet)()
                    Dim depthFrame = frames.DepthFrame
                    Dim colorFrame = frames.ColorFrame
                    Dim disparityFrame = depth2Disparity.Process(depthFrame)
                    Dim depthRGBframe = colorizer.Process(depthFrame)
                    Dim redRight As rs.Frame = Nothing
                    Dim redLeft = frames.InfraredFrame
                    For Each frame In frames
                        If frame.Profile.Stream = rs.Stream.Infrared Then
                            If frame.Profile.Index = 2 Then
                                redRight = frame
                                Exit For
                            End If
                        End If
                    Next

                    Dim points = pc.Process(depthFrame).As(Of rs.Points)
                    Marshal.Copy(points.Data, vertices, 0, vertices.Length)

                    Marshal.Copy(colorFrame.Data, colorBytes, 0, colorBytes.Length)
                    Marshal.Copy(disparityFrame.Data, disparityBytes, 0, disparityBytes.Length)
                    Marshal.Copy(depthRGBframe.Data, depthRGBBytes, 0, depthRGBBytes.Length)
                    Marshal.Copy(depthFrame.Data, depthBytes, 0, depthBytes.Length)
                    Marshal.Copy(redLeft.Data, redLeftBytes, 0, redLeftBytes.Length)
                    Marshal.Copy(redRight.Data, redRightBytes, 0, redRightBytes.Length)

                    ' get motion data and timestamp from the gyro and accelerometer
                    If IMUpresent Then
                        Dim gyroFrame = frames.FirstOrDefault(Of rs.Frame)(rs.Stream.Gyro, rs.Format.MotionXyz32f)
                        imuGyro = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame.Data)
                        imuTimeStamp = gyroFrame.Timestamp

                        Dim accelFrame = frames.FirstOrDefault(Of rs.Frame)(rs.Stream.Accel, rs.Format.MotionXyz32f)
                        imuAccel = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame.Data)
                    End If
                Catch ex As Exception
                    Console.WriteLine("Error in CustomProcessingBlock: " + ex.Message)
                    failedImageCount += 1
                End Try
            End Using
        End Sub
    )
    Public Sub New(fps As Int32, width As Int32, height As Int32)
        devices = ctx.QueryDevices()
        deviceCount = devices.Count

        If deviceCount = 0 Then Return
        device = devices(0)
        deviceName = device.Info.Item(rs.CameraInfo.Name)
        If deviceName.EndsWith("USB2") Then MsgBox("Is the RealSense camera attached to a USB2 socket?  Are you using the Intel-provided cable?  It needs to be USB3!")

        cfg.EnableStream(rs.Stream.Color, width, height, rs.Format.Bgr8, fps)
        cfg.EnableStream(rs.Stream.Depth, width, height, rs.Format.Z16, fps)
        cfg.EnableStream(rs.Stream.Infrared, 1, width, height, rs.Format.Y8, fps) ' left
        cfg.EnableStream(rs.Stream.Infrared, 2, width, height, rs.Format.Y8, fps) ' right

        If deviceName = "Intel RealSense D435I" Then
            cfg.EnableStream(rs.Stream.Gyro)
            cfg.EnableStream(rs.Stream.Accel)
            IMUpresent = True
        End If

        pipeline = New rs.Pipeline(ctx)
        pipeline_profile = pipeline.Start(cfg)
        align = New rs.Align(rs.Stream.Color)
        sensor = pipeline_profile.Device.Sensors.First() ' This may change!  But just take the first for now.  It is the only one with 7 filters
        blocks = sensor.ProcessingBlocks.ToList()

        If deviceName <> "Intel RealSense D435" And deviceName <> "Intel RealSense D415" And deviceName <> "Intel RealSense D435I" Then
            MsgBox("We only support the D435, D415, or D435I cameras. " + vbCrLf + "Is this a new device?  It is called: " + deviceName)
            deviceCount = 0
        Else
            Dim dIntrinsics = pipeline_profile.GetStream(rs.Stream.Depth).As(Of rs.VideoStreamProfile).GetIntrinsics
            Dim cIntrinsics = pipeline_profile.GetStream(rs.Stream.Color).As(Of rs.VideoStreamProfile).GetIntrinsics
            w = dIntrinsics.width
            h = dIntrinsics.height
            intrinsics_VB.width = dIntrinsics.width
            intrinsics_VB.height = dIntrinsics.height
            intrinsics_VB.ppx = dIntrinsics.ppx
            intrinsics_VB.ppy = dIntrinsics.ppy
            intrinsics_VB.fx = dIntrinsics.fx
            intrinsics_VB.fy = dIntrinsics.fy
            intrinsics_VB.FOV = dIntrinsics.FOV
            intrinsics_VB.coeffs = dIntrinsics.coeffs
            Dim extrinsics As rs.Extrinsics = Nothing
            For Each stream In pipeline_profile.Streams
                extrinsics = stream.GetExtrinsicsTo(pipeline_profile.GetStream(rs.Stream.Infrared))
                If extrinsics.rotation(0) <> 1 Then Exit For
            Next
            Extrinsics_VB.rotation = extrinsics.rotation
            Extrinsics_VB.translation = extrinsics.translation

            ReDim colorBytes(w * h * 3 - 1)
            ReDim depthRGBBytes(w * h * 3 - 1)
            ReDim depthBytes(w * h * System.Runtime.InteropServices.Marshal.SizeOf(GetType(UShort)) - 1)
            ReDim disparityBytes(w * h * 4 - 1)
            ReDim redLeftBytes(w * h - 1)
            ReDim redRightBytes(w * h - 1)
            ReDim vertices(w * h * System.Runtime.InteropServices.Marshal.SizeOf(GetType(Single)) * 3 - 1) ' 3 floats per pixel.  
        End If
    End Sub
    Public Sub GetNextFrame()
        Dim frameSet = pipeline.WaitForFrames(1000)
        block.Process(frameSet)

        color = New cv.Mat(h, w, cv.MatType.CV_8UC3, colorBytes)
        depthRGB = New cv.Mat(h, w, cv.MatType.CV_8UC3, depthRGBBytes)
        depth = New cv.Mat(h, w, cv.MatType.CV_16U, depthBytes)
        disparity = New cv.Mat(h, w, cv.MatType.CV_32F, disparityBytes)
        redLeft = New cv.Mat(h, w, cv.MatType.CV_8U, redLeftBytes)
        redRight = New cv.Mat(h, w, cv.MatType.CV_8U, redRightBytes)
        pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, vertices)
    End Sub
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        pipeline.Stop()
        cfg.DisableAllStreams()
        ctx.Dispose()
        cfg.Dispose()
        pipeline.Dispose()
        colorizer.Dispose()
        depth2Disparity.Dispose()
        block.Dispose()
        align.Dispose()
        disposedValue = True
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
    End Sub
End Class
