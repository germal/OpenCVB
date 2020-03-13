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
    End Sub
    Public Sub GetNextFrame()
        If pipelineClosed Then Exit Sub

        Dim colorPtr = D400WaitForFrame(cPtr)

        SyncLock OpenCVB.camPic ' only really need the synclock when in callback mode but it doesn't hurt to waitforframe mode.
            IMU_TimeStamp = D400IMUTimeStamp(cPtr)
            Static imuStartTime = IMU_TimeStamp
            IMU_TimeStamp -= imuStartTime

            color = New cv.Mat(h, w, cv.MatType.CV_8UC3, colorPtr)
            RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, 0)
            depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, D400Depth16(cPtr))
            disparity = New cv.Mat(h, w, cv.MatType.CV_16U, 0)
            leftView = New cv.Mat(h, w, cv.MatType.CV_8U, D400LeftRaw(cPtr))
            rightView = New cv.Mat(h, w, cv.MatType.CV_8U, D400RightRaw(cPtr))
            pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, 0) ' D400PointCloud(cPtr))
        End SyncLock

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
End Class
