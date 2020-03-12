Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports System.Threading

Module D400_Module_CPP
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400Open(w As Int32, h As Int32, IMUPresent As Boolean) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400RawWidth(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400RawHeight(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400Depth16Width(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400Depth16Height(tp As IntPtr) As Int32
    End Function

    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400WaitForFrame(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400RightRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400LeftRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400intrinsicsLeft(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400intrinsicsLeftRight(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400Extrinsics(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400PointCloud(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400RGBDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400PoseData(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400IMUTimeStamp(tp As IntPtr) As Double
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400Depth16(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400timeStampLatency(timeStamp As Double) As Single
    End Function
End Module

Structure D400IMUdata
    Public translation As cv.Point3f
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
    Public trackerConfidence As Int32
    Public mapperConfidence As Int32
End Structure
Public Class CameraD400Native
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

    Dim intrinsicsLeft As rs.Intrinsics
    Public extrinsics As rs.Extrinsics
    Public pc As New rs.PointCloud
    Public Sub New()
    End Sub
    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
#If 0 Then
        deviceName = "Intel D400"
        IMU_Present = True
        w = width
        h = height
        If OpenCVB.USBenumeration("RealSense(TM) 435 With RGB Module Depth") > 0 Then IMU_Present = True

        cPtr = D400Open(width, height, IMU_Present)

        Dim intrin = D400intrinsicsLeft(cPtr)
        intrinsicsLeft = Marshal.PtrToStructure(Of rs.Intrinsics)(intrin)
        setintrinsicsLeft(intrinsicsLeft)

        Dim extrin = D400Extrinsics(cPtr)
        extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(extrin)
        Extrinsics_VB.rotation = extrinsics.rotation
        Extrinsics_VB.translation = extrinsics.translation
#Else
        Devices = ctx.QueryDevices()
        device = Devices(0)
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
            Dim dintrinsicsLeft = pipeline_profile.GetStream(rs.Stream.Color).As(Of rs.VideoStreamProfile).GetIntrinsics
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
#End If
    End Sub
    Public Sub GetNextFrame()
        ReDim colorBytes(w * h * 3 - 1)
        ReDim depthBytes(w * h * 2 - 1)
        ReDim disparityBytes(w * h * 2 - 1)
        ReDim RGBDepthBytes(colorBytes.Length - 1)
        ReDim leftViewBytes(w * h - 1)
        ReDim rightViewBytes(w * h - 1)
        ReDim vertices(w * h * 3 * 3 - 1)

        If pipelineClosed Then Exit Sub

        Dim colorPtr = D400WaitForFrame(cPtr)

        SyncLock OpenCVB.camPic ' only really need the synclock when in callback mode but it doesn't hurt to waitforframe mode.
            IMU_TimeStamp = D400IMUTimeStamp(cPtr)
            Static imuStartTime = IMU_TimeStamp
            IMU_TimeStamp -= imuStartTime
        End SyncLock

        color = New cv.Mat(h, w, cv.MatType.CV_8UC3, colorBytes)
        RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, RGBDepthBytes)
        depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, depthBytes)
        disparity = New cv.Mat(h, w, cv.MatType.CV_16U, disparityBytes)
        leftView = New cv.Mat(h, w, cv.MatType.CV_8U, leftViewBytes)
        rightView = New cv.Mat(h, w, cv.MatType.CV_8U, rightViewBytes)
        pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, vertices)

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
End Class
