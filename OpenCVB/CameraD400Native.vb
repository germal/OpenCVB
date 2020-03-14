﻿Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports System.Threading

Module D400_Module_CPP
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400Open(w As Int32, h As Int32, IMUPresent As Boolean) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub D400WaitForFrame(tp As IntPtr)
    End Sub
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400RightRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400Color(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400LeftRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400Disparity(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400intrinsicsLeft(tp As IntPtr) As IntPtr
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
    Public Function D400Gyro(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400IMUTimeStamp(tp As IntPtr) As Double
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400Accel(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_D400.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function D400Depth16(tp As IntPtr) As IntPtr
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

    Public DecimationFilter As Boolean
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

        D400WaitForFrame(cPtr)

        SyncLock OpenCVB.camPic ' only really need the synclock when in callback mode but it doesn't hurt to waitforframe mode.
            color = New cv.Mat(h, w, cv.MatType.CV_8UC3, D400Color(cPtr)).Clone() ' must be first!  Prepares the procframes...

            Dim accelFrame = D400Accel(cPtr)
            imuAccel = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame)

            Dim gyroFrame = D400Gyro(cPtr)
            imuGyro = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame)

            Static imuStartTime = D400IMUTimeStamp(cPtr)
            IMU_TimeStamp = D400IMUTimeStamp(cPtr) - imuStartTime

            RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, D400RGBDepth(cPtr)).Clone()
            depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, D400Depth16(cPtr)).Clone()
            leftView = New cv.Mat(h, w, cv.MatType.CV_8U, D400LeftRaw(cPtr)).Clone()
            rightView = New cv.Mat(h, w, cv.MatType.CV_8U, D400RightRaw(cPtr)).Clone()
            pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, D400PointCloud(cPtr))
        End SyncLock

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
End Class