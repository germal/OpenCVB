﻿Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
#If 0 Then
Public Class PythonCam
    Inherits Camera
    Public cameraName As String
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

    Public Sub initialize(_width As Integer, _height As Integer, fps As Integer)
        width = _width
        height = _height
        deviceName = "OakD"
        cameraName = deviceName
        If cPtr <> 0 Then
            deviceCount = KinectDeviceCount(cPtr)
            Dim strPtr = KinectDeviceName(cPtr) ' The width and height of the image are set in the constructor.
            serialNumber = Marshal.PtrToStringAnsi(strPtr)
            leftView = New cv.Mat

            Dim ptr = KinectExtrinsics(cPtr)
            Dim extrinsics As rs.Extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(ptr)
            Extrinsics_VB.rotation = extrinsics.rotation
            Extrinsics_VB.translation = extrinsics.translation

            ptr = KinectintrinsicsLeft(cPtr)
            Dim intrinsicsLeftOutput = Marshal.PtrToStructure(Of intrinsicsLeftData)(ptr)
            intrinsicsLeft_VB.ppx = intrinsicsLeftOutput.cx
            intrinsicsLeft_VB.ppy = intrinsicsLeftOutput.cy
            intrinsicsLeft_VB.fx = intrinsicsLeftOutput.fx
            intrinsicsLeft_VB.fy = intrinsicsLeftOutput.fy
            ReDim intrinsicsLeft_VB.FOV(3 - 1)
            ReDim intrinsicsLeft_VB.coeffs(6 - 1)
            intrinsicsLeft_VB.coeffs(0) = intrinsicsLeftOutput.k1
            intrinsicsLeft_VB.coeffs(1) = intrinsicsLeftOutput.k2
            intrinsicsLeft_VB.coeffs(2) = intrinsicsLeftOutput.k3
            intrinsicsLeft_VB.coeffs(3) = intrinsicsLeftOutput.k4
            intrinsicsLeft_VB.coeffs(4) = intrinsicsLeftOutput.k5
            intrinsicsLeft_VB.coeffs(5) = intrinsicsLeftOutput.k6

            intrinsicsRight_VB = intrinsicsLeft_VB ' there is no right lens - just copy for compatibility.

            ReDim RGBDepthBytes(width * height * 3 - 1)
            pointCloud = New cv.Mat()
        End If
    End Sub

    Public Sub GetNextFrame()
        Dim imuFrame As IntPtr
        If pipelineClosed Or cPtr = 0 Then Exit Sub
        imuFrame = KinectWaitFrame(cPtr)
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

        Dim colorBuffer = KinectRGBA(cPtr)
        If colorBuffer <> 0 Then ' it can be zero on startup...
            Dim colorRGBA = New cv.Mat(height, width, cv.MatType.CV_8UC4, colorBuffer)
            SyncLock bufferLock
                color = colorRGBA.CvtColor(cv.ColorConversionCodes.BGRA2BGR)
                depth16 = New cv.Mat(height, width, cv.MatType.CV_16U, KinectRawDepth(cPtr)).Clone()
                RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3, KinectRGBdepth(cPtr)).Clone()
                ' if you normalize here instead of just a fixed multiply, the image will pulse with different brightness values.  Not pretty.
                leftView = (New cv.Mat(height, width, cv.MatType.CV_16U, KinectLeftView(cPtr)) * 0.06).ToMat.ConvertScaleAbs() ' so depth data fits into 0-255 (approximately)
                rightView = leftView

                Dim pc = New cv.Mat(height, width, cv.MatType.CV_16SC3, KinectPointCloud(cPtr))
                ' This is less efficient than using 16-bit pixels but consistent with the other cameras
                pc.ConvertTo(pointCloud, cv.MatType.CV_32FC3, 0.001) ' convert to meters...
                MyBase.GetNextFrameCounts(IMU_FrameTime)
            End SyncLock
        End If
    End Sub
    Public Sub stopCamera()
        KinectClose(cPtr)
        pipelineClosed = True
        frameCount = 0
        cPtr = 0
    End Sub
End Class
#End If