Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp

Module RS2_Module_CPP
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2Open(width As Int32, height As Int32, IMUPresent As Boolean, lidarCam As Boolean) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub RS2WaitForFrame(tp As IntPtr)
    End Sub
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2RightRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2Color(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2LeftRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2Disparity(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2intrinsicsLeft(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2Extrinsics(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2PointCloud(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2RGBDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2RawDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2Gyro(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2IMUTimeStamp(tp As IntPtr) As Double
    End Function
    <DllImport(("Cam_RS2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RS2Accel(tp As IntPtr) As IntPtr
    End Function
End Module

Structure RS2IMUdata
    Public translation As cv.Point3f
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
    Public trackerConfidence As Int32
    Public mapperConfidence As Int32
End Structure
Public Class CameraRS2
    Inherits Camera

    Public DecimationFilter As Boolean
    Public HoleFillingFilter As Boolean
    Public SpatialFilter As Boolean
    Public TemporalFilter As Boolean
    Public ThresholdFilter As Boolean

    Dim ctx As New rs.Context
    Public deviceNum As Integer
    Dim intrinsicsLeft As rs.Intrinsics
    Public pc As New rs.PointCloud
    Dim lidarCam As Boolean
    Dim lidarRect As New cv.Rect
    Dim lidarWidth = 1024
    Public Sub New()
    End Sub
    Public Function queryDeviceCount() As Integer
        Dim Devices = ctx.QueryDevices()
        Return ctx.QueryDevices().Count
    End Function
    Public Function queryDevice(index As Integer) As String
        Dim Devices = ctx.QueryDevices()
        Return Devices(index).Info(0)
    End Function
    Public Sub initialize(fps As Int32)
        lidarCam = If(deviceName = "Intel RealSense L515", True, False)
        cPtr = RS2Open(width, height, IMU_Present, lidarCam)

        Dim intrin = RS2intrinsicsLeft(cPtr)
        intrinsicsLeft = Marshal.PtrToStructure(Of rs.Intrinsics)(intrin)
        intrinsicsLeft_VB = setintrinsics(intrinsicsLeft)
        intrinsicsRight_VB = intrinsicsLeft_VB ' need to get the Right lens intrinsics?

        Dim extrin = RS2Extrinsics(cPtr)
        Dim extrinsics As rs.Extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(extrin) ' they are both float's
        Extrinsics_VB.rotation = extrinsics.rotation
        Extrinsics_VB.translation = extrinsics.translation
        lidarRect = New cv.Rect((width - lidarWidth) / 2, 0, lidarWidth, height)
        leftView = New cv.Mat(height, width, cv.MatType.CV_8U, 0) ' lidarCam won't have these so initialize here.
        rightView = New cv.Mat(height, width, cv.MatType.CV_8U, 0)
        cv.Cv2.PutText(rightView, "Intel RealSense L515 camera has no right view", New cv.Point(10, 200), cv.HersheyFonts.HersheyComplex, 1.5, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(leftView, "Intel RealSense L515 camera has no left view", New cv.Point(10, 200), cv.HersheyFonts.HersheyComplex, 1.5, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub GetNextFrame()
        If pipelineClosed Or cPtr = 0 Then Exit Sub

        RS2WaitForFrame(cPtr)

        SyncLock bufferLock
            color = New cv.Mat(height, width, cv.MatType.CV_8UC3, RS2Color(cPtr)).Clone()

            If lidarCam = False Then
                Dim accelFrame = RS2Accel(cPtr)
                If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame)
                IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.

                Dim gyroFrame = RS2Gyro(cPtr)
                If gyroFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame)

                Static imuStartTime = RS2IMUTimeStamp(cPtr)
                IMU_TimeStamp = RS2IMUTimeStamp(cPtr) - imuStartTime
            End If

            RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3, RS2RGBDepth(cPtr)).Clone()
            depth16 = New cv.Mat(height, width, cv.MatType.CV_16U, RS2RawDepth(cPtr)).Clone()
            If lidarCam = False Then
                leftView = New cv.Mat(height, width, cv.MatType.CV_8U, RS2LeftRaw(cPtr)).Clone()
                rightView = New cv.Mat(height, width, cv.MatType.CV_8U, RS2RightRaw(cPtr)).Clone()
            End If
            pointCloud = New cv.Mat(height, width, cv.MatType.CV_32FC3, RS2PointCloud(cPtr)).Clone()
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End SyncLock
    End Sub
End Class