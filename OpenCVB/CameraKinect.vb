﻿Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Module Kinect_Interface
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectOpen() As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectDeviceCount(cPtr As IntPtr) As Int32
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectDeviceName(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectWaitFrame(cPtr As IntPtr, RGBDepth As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectExtrinsics(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectintrinsicsLeft(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectPointCloud(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectRGBA(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectDepth16(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectDepthInColor(cPtr As IntPtr) As IntPtr
    End Function
End Module
Public Class CameraKinect
    Inherits Camera
    Structure imuData
        Dim temperature As Single
        Dim imuAccel As cv.Point3f
        Dim accelTimeStamp As Long
        Dim imu_Gyro As cv.Point3f
        Dim gyroTimeStamp As Long
    End Structure

    Structure intrinsicsLeftData
        Dim cx As Single            ' Principal point In image, x */
        Dim cy As Single            ' Principal point In image, y */
        Dim fx As Single            ' Focal length x */
        Dim fy As Single            ' Focal length y */
        Dim k1 As Single            ' k1 radial distortion coefficient */
        Dim k2 As Single            ' k2 radial distortion coefficient */
        Dim k3 As Single            ' k3 radial distortion coefficient */
        Dim k4 As Single            ' k4 radial distortion coefficient */
        Dim k5 As Single            ' k5 radial distortion coefficient */
        Dim k6 As Single            ' k6 radial distortion coefficient */
        Dim codx As Single          ' Center Of distortion In Z=1 plane, x (only used For Rational6KT) */
        Dim cody As Single          ' Center Of distortion In Z=1 plane, y (only used For Rational6KT) */
        Dim p2 As Single            ' Tangential distortion coefficient 2 */
        Dim p1 As Single            ' Tangential distortion coefficient 1 */
        Dim metric_radius As Single ' Metric radius */
    End Structure

    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        cPtr = KinectOpen()
        deviceName = "Kinect for Azure"
        IMU_Present = True
        If cPtr <> 0 Then
            deviceCount = KinectDeviceCount(cPtr)
            Dim strPtr = KinectDeviceName(cPtr) ' The width and height of the image are set in the constructor.
            serialNumber = Marshal.PtrToStringAnsi(strPtr)
            w = width
            h = height
            leftView = New cv.Mat

            Dim ptr = KinectExtrinsics(cPtr)
            Dim rotationTranslation(12 - 1) As Single
            Marshal.Copy(ptr, rotationTranslation, 0, rotationTranslation.Length)
            ReDim Extrinsics_VB.rotation(9 - 1)
            ReDim Extrinsics_VB.translation(3 - 1)
            For i = 0 To Extrinsics_VB.rotation.Length - 1
                Extrinsics_VB.rotation(i) = rotationTranslation(i)
            Next
            For i = 0 To Extrinsics_VB.translation.Length - 1
                Extrinsics_VB.translation(i) = rotationTranslation(i + Extrinsics_VB.rotation.Length - 1)
            Next

            ptr = KinectintrinsicsLeft(cPtr)
            Dim intrinsicsLeftOutput = Marshal.PtrToStructure(Of intrinsicsLeftData)(ptr)
            intrinsicsLeft_VB.ppx = intrinsicsLeftOutput.cx
            intrinsicsLeft_VB.ppy = intrinsicsLeftOutput.cy
            intrinsicsLeft_VB.fx = intrinsicsLeftOutput.fx
            intrinsicsLeft_VB.fy = intrinsicsLeftOutput.fy
            ReDim intrinsicsLeft_VB.FOV(2)
            'intrinsicsLeft_VB.FOV(0) = intrinsicsLeftOutput.fx ' no FOV from Kinect?  
            'intrinsicsLeft_VB.FOV(1) = intrinsicsLeftOutput.fy
            ReDim intrinsicsLeft_VB.coeffs(5)
            intrinsicsLeft_VB.coeffs(0) = intrinsicsLeftOutput.k1
            intrinsicsLeft_VB.coeffs(1) = intrinsicsLeftOutput.k2
            intrinsicsLeft_VB.coeffs(2) = intrinsicsLeftOutput.k3
            intrinsicsLeft_VB.coeffs(3) = intrinsicsLeftOutput.k4
            intrinsicsLeft_VB.coeffs(4) = intrinsicsLeftOutput.k5
            intrinsicsLeft_VB.coeffs(5) = intrinsicsLeftOutput.k6

            intrinsicsRight_VB = intrinsicsLeft_VB ' there is no right lens - just copy for compatibility.

            ReDim RGBDepthBytes(w * h * 3 - 1)
            pointCloud = New cv.Mat()
        End If
    End Sub

    Public Sub GetNextFrame()
        Dim imuFrame As IntPtr
        If pipelineClosed Or cPtr = 0 Then Exit Sub
        Dim handleRGBDepth = GCHandle.Alloc(RGBDepthBytes, GCHandleType.Pinned)
        Try
            imuFrame = KinectWaitFrame(cPtr, handleRGBDepth.AddrOfPinnedObject())
        Catch ex As Exception
            failedImageCount += 10 ' force a restart of the camera if this happens
            Exit Sub
        End Try
        If imuFrame = 0 Then
            Console.WriteLine("KinectWaitFrame has returned without any image.")
            failedImageCount += 1
            Exit Sub ' just process the existing images again?  
        Else
            Dim imuOutput = Marshal.PtrToStructure(Of imuData)(imuFrame)
            IMU_AngularVelocity = imuOutput.imu_Gyro
            IMU_Acceleration = imuOutput.imuAccel

            ' make the imu data consistent with the Intel IMU...
            Dim tmpVal = IMU_Acceleration.Z
            IMU_Acceleration.Z = IMU_Acceleration.X
            IMU_Acceleration.X = -IMU_Acceleration.Y
            IMU_Acceleration.Y = tmpVal

            tmpVal = IMU_AngularVelocity.Z
            IMU_AngularVelocity.Z = -IMU_AngularVelocity.X
            IMU_AngularVelocity.X = -IMU_AngularVelocity.Y
            IMU_AngularVelocity.Y = tmpVal

            IMU_TimeStamp = imuOutput.accelTimeStamp / 1000
        End If

        handleRGBDepth.Free()

        Dim colorBuffer = KinectRGBA(cPtr)
        If colorBuffer <> 0 Then ' it can be zero on startup...
            Dim colorRGBA = New cv.Mat(h, w, cv.MatType.CV_8UC4, colorBuffer)
            SyncLock bufferLock
                color = colorRGBA.CvtColor(cv.ColorConversionCodes.BGRA2BGR)

                RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, RGBDepthBytes)
                depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, KinectDepth16(cPtr))

                Dim tmp As New cv.Mat
                cv.Cv2.Normalize(depth16, tmp, 0, 255, cv.NormTypes.MinMax)
                tmp.ConvertTo(leftView, cv.MatType.CV_8U)
                rightView = leftView

                Dim pc = New cv.Mat(h, w, cv.MatType.CV_16SC3, KinectPointCloud(cPtr))
                ' This is less efficient than using 16-bit pixels but consistent with the other cameras
                pc.ConvertTo(pointCloud, cv.MatType.CV_32FC3, 0.001) ' convert to meters...
                MyBase.GetNextFrameCounts(IMU_FrameTime)
            End SyncLock
        End If
    End Sub
End Class
