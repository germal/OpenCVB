﻿Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports System.IO

Module T265_Module_CPP
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Open(w As Int32, h As Int32) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RawWidth(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RawHeight(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub T265WaitFrame(tp As IntPtr)
    End Sub
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RightRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265LeftRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265intrinsicsLeft(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265intrinsicsRight(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Extrinsics(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265PoseData(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Disparity(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265IMUTimeStamp(tp As IntPtr) As Double
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Color(tp As IntPtr) As IntPtr
    End Function
End Module

Structure T265IMUdata
    Public translation As cv.Point3f
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
    Public trackerConfidence As Int32
    Public mapperConfidence As Int32
End Structure
Public Class CameraT265
    Inherits Camera
    Dim rawHeight As Int32
    Dim rawWidth As Int32

    Public extrinsics As rs.Extrinsics
    Public pc As New rs.PointCloud
    Dim rawSrcRect As cv.Rect
    Dim rawDstRect As cv.Rect

    Public Sub New()
    End Sub
    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        deviceName = "Intel T265"
        IMU_Present = True
        w = width
        h = height

        cPtr = T265Open(width, height)
        rawWidth = T265RawWidth(cPtr)
        rawHeight = T265RawHeight(cPtr)

        Dim intrin = T265intrinsicsLeft(cPtr)
        Dim intrinsicsLeft = Marshal.PtrToStructure(Of rs.Intrinsics)(intrin)
        intrinsicsLeft_VB = setintrinsics(intrinsicsLeft)

        intrin = T265intrinsicsRight(cPtr)
        Dim intrinsicsRight = Marshal.PtrToStructure(Of rs.Intrinsics)(intrin)
        intrinsicsRight_VB = setintrinsics(intrinsicsRight)

        Dim extrin = T265Extrinsics(cPtr)
        extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(extrin)
        Extrinsics_VB.rotation = extrinsics.rotation
        Extrinsics_VB.translation = extrinsics.translation

        rightView = New cv.Mat(h, w, cv.MatType.CV_8U, 0)
        leftView = New cv.Mat(h, w, cv.MatType.CV_8U, 0)

        rawSrcRect = New cv.Rect((rawWidth - rawWidth * h / rawHeight) / 2, 0, rawWidth * h / rawHeight, h)
        rawDstRect = New cv.Rect(0, 0, rawSrcRect.Width, rawSrcRect.Height)

        pointCloud = New cv.Mat()
    End Sub
    Public Sub GetNextFrame()
        If pipelineClosed Or cPtr = 0 Then Exit Sub

        T265WaitFrame(cPtr)

        SyncLock bufferLock ' only really need the synclock when in callback mode but it doesn't hurt to waitforframe mode.
            Dim posePtr = T265PoseData(cPtr)
            Dim pose = Marshal.PtrToStructure(Of PoseData)(posePtr)
            IMU_TimeStamp = T265IMUTimeStamp(cPtr)
            Static imuStartTime = IMU_TimeStamp
            IMU_TimeStamp -= imuStartTime

            IMU_RotationVector = New cv.Point3f(pose.rotation.X, pose.rotation.Y, pose.rotation.Z)

            ' the T265 doesn't provide the rotation matrix so we provide it with this Rodriques calculation
            Dim src As New cv.Mat(3, 1, cv.MatType.CV_32F, IMU_RotationVector)
            Dim dst As New cv.Mat(3, 3, src.Type, IMU_RotationMatrix)
            cv.Cv2.Rodrigues(src, dst)

            Dim q = IMU_Rotation
            IMU_Translation = pose.translation
            IMU_Acceleration = pose.acceleration
            IMU_Velocity = pose.velocity
            IMU_AngularAcceleration = pose.angularAcceleration
            IMU_AngularVelocity = pose.angularVelocity
            Dim t = IMU_Translation
            '  Set the matrix as column-major for convenient work with OpenGL and rotate by 180 degress (by negating 1st and 3rd columns)
            Dim mat() As Single = {
                        -(1 - 2 * q.Y * q.Y - 2 * q.Z * q.Z), -(2 * q.X * q.Y + 2 * q.Z * q.W), -(2 * q.X * q.Z - 2 * q.Y * q.W), 0.0,
                        2 * q.X * q.Y - 2 * q.Z * q.W, 1 - 2 * q.X * q.X - 2 * q.Z * q.Z, 2 * q.Y * q.Z + 2 * q.X * q.W, 0.0,
                        -(2 * q.X * q.Z + 2 * q.Y * q.W), -(2 * q.Y * q.Z - 2 * q.X * q.W), -(1 - 2 * q.X * q.X - 2 * q.Y * q.Y), 0.0,
                        t.X, t.Y, t.Z, 1.0}
            transformationMatrix = mat

            color = New cv.Mat(h, w, cv.MatType.CV_8UC3, T265Color(cPtr)).Clone()

            Dim right = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_8U, T265RightRaw(cPtr)).Clone()
            Dim left = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_8U, T265LeftRaw(cPtr)).Clone()

            rightView(rawDstRect) = right(rawSrcRect)
            leftView(rawDstRect) = left(rawSrcRect)

            Dim disparity32f = New cv.Mat(300, 300, cv.MatType.CV_32F, T265Disparity(cPtr)).Clone()
            disparity32f = disparity32f.Threshold(0, 0, cv.ThresholdTypes.Tozero)

            Dim depth32f As New cv.Mat(disparity32f.Size(), cv.MatType.CV_32F, 20000)
            cv.Cv2.Divide(depth32f, disparity32f, depth32f)
            Dim depth16 = New cv.Mat(h, w, cv.MatType.CV_16U)
            Dim rectDepth As New cv.Rect((disparity32f.Width - 1) / 2 + 112, 0, disparity32f.Width, disparity32f.Height)
            depth32f.ConvertTo(depth16(rectDepth), cv.MatType.CV_16U)

            Dim depth8u = New cv.Mat(color.Height, depth16.Width, cv.MatType.CV_8U)
            depth8u(rectDepth) = 255 - depth16(rectDepth).ConvertScaleAbs(0.03)
            RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, 0)
            cv.Cv2.ApplyColorMap(depth8u(rectDepth), RGBDepth(rectDepth), cv.ColormapTypes.Jet)

            pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, 0) ' no point cloud for T265 - just provide it for compatibility.
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End SyncLock
    End Sub
End Class
