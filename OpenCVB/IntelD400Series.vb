Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class IntelD400Series
    Dim align As rs.Align
    Dim blocks As List(Of rs.ProcessingBlock)
    Dim cfg As New rs.Config
    Dim colorBytes() As Byte
    Dim colorizer As New rs.Colorizer
    Dim ctx As New rs.Context
    Dim depth2Disparity As New rs.DisparityTransform
    Dim depthBytes() As Byte
    Dim RGBDepthBytes() As Byte
    Dim device As rs.Device
    Dim devices As rs.DeviceList
    Dim disparityBytes() As Byte
    Dim h As Int32
    Dim pipeline As New rs.Pipeline
    Dim pipeline_profile As rs.PipelineProfile
    Dim leftViewBytes() As Byte
    Dim rightViewBytes() As Byte
    Dim sensor As rs.Sensor
    Dim vertices() As Byte
    Dim w As Int32
    Public color As cv.Mat
    Public DecimationFilter As Boolean
    Public depth16 As cv.Mat
    Public RGBDepth As cv.Mat
    Public DepthToDisparity As Boolean
    Public deviceCount As Int32
    Public deviceName As String
    Public disparity As cv.Mat
    Public DisparityToDepth As Boolean
    Public Extrinsics_VB As VB_Classes.ActiveClass.Extrinsics_VB
    Public failedImageCount As Int32
    Public HoleFillingFilter As Boolean
    Public imuAccel As cv.Point3f
    Public imuGyro As cv.Point3f
    Public IMUpresent As Boolean
    Public imuTimeStamp As Double
    Public intrinsics_VB As VB_Classes.ActiveClass.Intrinsics_VB
    Public modelInverse As Boolean
    Public pc As New rs.PointCloud
    Public pcMultiplier As Single = 1
    Public pointCloud As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public SpatialFilter As Boolean
    Public TemporalFilter As Boolean
    Public ThresholdFilter As Boolean
    Public pipelineClosed As Boolean

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
                    Dim RGBDepthframe = colorizer.Process(depthFrame)
                    Dim rightView As rs.Frame = Nothing
                    Dim leftView = frames.InfraredFrame
                    For Each frame In frames
                        If frame.Profile.Stream = rs.Stream.Infrared Then
                            If frame.Profile.Index = 2 Then
                                rightView = frame
                                Exit For
                            End If
                        End If
                    Next

                    Dim points = pc.Process(depthFrame).As(Of rs.Points)
                    Marshal.Copy(points.Data, vertices, 0, vertices.Length)

                    Marshal.Copy(colorFrame.Data, colorBytes, 0, colorBytes.Length)
                    Marshal.Copy(disparityFrame.Data, disparityBytes, 0, disparityBytes.Length)
                    Marshal.Copy(RGBDepthframe.Data, RGBDepthBytes, 0, RGBDepthBytes.Length)
                    Marshal.Copy(depthFrame.Data, depthBytes, 0, depthBytes.Length)
                    Marshal.Copy(leftView.Data, leftViewBytes, 0, leftViewBytes.Length)
                    Marshal.Copy(rightView.Data, rightViewBytes, 0, rightViewBytes.Length)

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
    Public Sub New()
        Console.WriteLine("The current librealsense version is " + ctx.Version())
    End Sub
    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        pipelineClosed = False
        devices = ctx.QueryDevices()
        device = devices(0)
        deviceName = device.Info.Item(rs.CameraInfo.Name)

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
            ReDim RGBDepthBytes(w * h * 3 - 1)
            ReDim depthBytes(w * h * 2 - 1)
            ReDim disparityBytes(w * h * 4 - 1)
            ReDim leftViewBytes(w * h - 1)
            ReDim rightViewBytes(w * h - 1)
            ReDim vertices(w * h * 4 * 3 - 1) ' 3 floats or 12 bytes per pixel.  
        End If
    End Sub
    Public Sub GetNextFrame()
        If pipelineClosed Then Exit Sub
        Dim frameSet = pipeline.WaitForFrames(1000)
        block.Process(frameSet)

        color = New cv.Mat(h, w, cv.MatType.CV_8UC3, colorBytes)
        RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, RGBDepthBytes)
        depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, depthBytes)
        disparity = New cv.Mat(h, w, cv.MatType.CV_32F, disparityBytes)
        leftView = New cv.Mat(h, w, cv.MatType.CV_8U, leftViewBytes)
        rightView = New cv.Mat(h, w, cv.MatType.CV_8U, rightViewBytes)
        pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, vertices)
    End Sub
    Public Sub closePipe()
        pipelineClosed = True
    End Sub
End Class
