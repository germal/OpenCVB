Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports System.Threading
Public Class CameraD400
    Inherits Camera

    Dim align As rs.Align
    Dim blocks As List(Of rs.ProcessingBlock)
    Dim cfg As New rs.Config
    Dim colorizer As New rs.Colorizer
    Dim ctx As New rs.Context
    Dim depth2Disparity As New rs.DisparityTransform
    Dim device As rs.Device
    Dim devices As rs.DeviceList
    Dim pipeline As New rs.Pipeline
    Dim pipeline_profile As rs.PipelineProfile
    Dim sensor As rs.Sensor

    Public DecimationFilter As Boolean
    Public DepthToDisparity As Boolean
    Public DisparityToDepth As Boolean
    Public HoleFillingFilter As Boolean
    Public SpatialFilter As Boolean
    Public TemporalFilter As Boolean
    Public ThresholdFilter As Boolean

    Public pc As New rs.PointCloud
    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        devices = ctx.QueryDevices()
        device = devices(0)
        deviceName = device.Info.Item(rs.CameraInfo.Name)
        Console.WriteLine("The current librealsense version is " + ctx.Version())

        cfg.EnableStream(rs.Stream.Color, width, height, rs.Format.Bgr8, fps)
        cfg.EnableStream(rs.Stream.Depth, width, height, rs.Format.Z16, fps)
        cfg.EnableStream(rs.Stream.Infrared, 1, width, height, rs.Format.Y8, fps) ' left
        cfg.EnableStream(rs.Stream.Infrared, 2, width, height, rs.Format.Y8, fps) ' right

        If deviceName = "Intel RealSense D435I" Then
            cfg.EnableStream(rs.Stream.Gyro)
            cfg.EnableStream(rs.Stream.Accel)
            IMU_Present = True
        End If

        pipeline_profile = pipeline.Start(cfg)

        align = New rs.Align(rs.Stream.Color)
        sensor = pipeline_profile.Device.Sensors.First() ' This may change!  But just take the first for now.  It is the only one with 7 filters
        blocks = sensor.ProcessingBlocks.ToList()

        If deviceName <> "Intel RealSense D435" And deviceName <> "Intel RealSense D415" And deviceName <> "Intel RealSense D435I" Then
            MsgBox("This driver only supports the D435, D415, or D435I cameras. " + vbCrLf + "Is this a new Intel camera?  It is called: " + deviceName)
            deviceCount = 0
        Else
            Dim dintrinsicsLeft = pipeline_profile.GetStream(rs.Stream.Depth).As(Of rs.VideoStreamProfile).GetIntrinsics
            Dim cintrinsicsLeft = pipeline_profile.GetStream(rs.Stream.Color).As(Of rs.VideoStreamProfile).GetIntrinsics
            MyBase.w = dintrinsicsLeft.width
            MyBase.h = dintrinsicsLeft.height
            intrinsicsLeft_VB.width = dintrinsicsLeft.width
            intrinsicsLeft_VB.height = dintrinsicsLeft.height
            intrinsicsLeft_VB.ppx = dintrinsicsLeft.ppx
            intrinsicsLeft_VB.ppy = dintrinsicsLeft.ppy
            intrinsicsLeft_VB.fx = dintrinsicsLeft.fx
            intrinsicsLeft_VB.fy = dintrinsicsLeft.fy
            intrinsicsLeft_VB.FOV = dintrinsicsLeft.FOV
            intrinsicsLeft_VB.coeffs = dintrinsicsLeft.coeffs
            intrinsicsRight_VB = intrinsicsLeft_VB ' How to get the right lens intrinsics?
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
        Dim frames = pipeline.WaitForFrames(1000)
        Try
            Dim procf As rs.Frame
            For Each p In blocks
                If p.Info.Item(0) = "Decimation Filter" And DecimationFilter Then procf = p.Process(frames)
                If p.Info.Item(0) = "Threshold Filter" And ThresholdFilter Then procf = p.Process(frames)
                If p.Info.Item(0) = "Depth to Disparity" Then procf = p.Process(frames) ' always have depth to disparity
                If p.Info.Item(0) = "Spatial Filter" And SpatialFilter Then procf = p.Process(frames)
                If p.Info.Item(0) = "Temporal Filter" And TemporalFilter Then procf = p.Process(frames)
                If p.Info.Item(0) = "Hole Filling Filter" And HoleFillingFilter Then procf = p.Process(frames)
                If p.Info.Item(0) = "Disparity to Depth" Then procf = p.Process(frames) ' always have disparity to depth
            Next

            procf = colorizer.Process(frames)
            procf = align.Process(frames)

            frames = procf.As(Of rs.FrameSet)()
            Dim depthFrame = frames.DepthFrame()
            Dim colorFrame = frames.ColorFrame
            Dim disparityFrame = depth2Disparity.Process(depthFrame)
            Dim RGBDepthframe = colorizer.Process(depthFrame)
            Dim rawRight As rs.Frame = Nothing
            Dim rawLeft = frames.InfraredFrame
            For Each frame In frames
                If frame.Profile.Stream = rs.Stream.Infrared Then
                    If frame.Profile.Index = 2 Then
                        rawRight = frame
                        Exit For
                    End If
                End If
            Next

            Marshal.Copy(colorFrame.Data, colorBytes, 0, colorBytes.Length)
            Marshal.Copy(RGBDepthframe.Data, RGBDepthBytes, 0, RGBDepthBytes.Length)
            Dim points = pc.Process(depthFrame).As(Of rs.Points)
            Marshal.Copy(points.Data, vertices, 0, vertices.Length)
            Marshal.Copy(disparityFrame.Data, disparityBytes, 0, disparityBytes.Length)
            Marshal.Copy(depthFrame.Data, depthBytes, 0, depthBytes.Length)
            Marshal.Copy(rawLeft.Data, leftViewBytes, 0, leftViewBytes.Length)
            Marshal.Copy(rawRight.Data, rightViewBytes, 0, rightViewBytes.Length)

            SyncLock OpenCVB.camPic
                ' get motion data and timestamp from the gyro and accelerometer
                If IMU_Present Then
                    Dim gyroFrame = frames.FirstOrDefault(Of rs.Frame)(rs.Stream.Gyro, rs.Format.MotionXyz32f)
                    imuGyro = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame.Data)

                    IMU_TimeStamp = gyroFrame.Timestamp
                    Static imuStartTime = IMU_TimeStamp
                    IMU_TimeStamp -= imuStartTime

                    Dim accelFrame = frames.FirstOrDefault(Of rs.Frame)(rs.Stream.Accel, rs.Format.MotionXyz32f)
                    imuAccel = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame.Data)
                End If

                color = New cv.Mat(h, w, cv.MatType.CV_8UC3, colorBytes)
                RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, RGBDepthBytes)
                depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, depthBytes)
                disparity = New cv.Mat(h, w, cv.MatType.CV_32F, disparityBytes)
                leftView = New cv.Mat(h, w, cv.MatType.CV_8U, leftViewBytes)
                rightView = New cv.Mat(h, w, cv.MatType.CV_8U, rightViewBytes)
                pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, vertices)
            End SyncLock

        Catch ex As Exception
            Console.WriteLine("Error in CustomProcessingBlock: " + ex.Message)
            failedImageCount += 1
        End Try
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
End Class
