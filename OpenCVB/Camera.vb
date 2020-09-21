Imports cv = OpenCvSharp
Imports System.Numerics
Imports rs = Intel.Realsense
Public Class Camera
    Public pipelineClosed As Boolean = False
    Public transformationMatrix() As Single
    Public IMU_Barometer As Single
    Public IMU_Magnetometer As cv.Point3f
    Public IMU_Temperature As Single
    Public IMU_TimeStamp As Double
    Public IMU_Rotation As System.Numerics.Quaternion
    Public IMU_RotationMatrix(9 - 1) As Single
    Public IMU_RotationVector As cv.Point3f
    Public IMU_Translation As cv.Point3f
    Public IMU_Acceleration As cv.Point3f
    Public IMU_Velocity As cv.Point3f
    Public IMU_AngularAcceleration As cv.Point3f
    Public IMU_AngularVelocity As cv.Point3f
    Public IMU_FrameTime As Double
    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double
    Public frameCount As Integer

    Public color As New cv.Mat
    Public RGBDepth As New cv.Mat
    Public leftView As New cv.Mat
    Public rightView As New cv.Mat
    Public pointCloud As New cv.Mat
    Public depth16 As New cv.Mat
    Public width As Integer
    Public height As Integer

    Public deviceCount As integer
    Public deviceName As String = ""
    Public Extrinsics_VB As VB_Classes.ActiveTask.Extrinsics_VB
    Public IMU_Present As Boolean
    Public intrinsicsLeft_VB As VB_Classes.ActiveTask.intrinsics_VB
    Public intrinsicsRight_VB As VB_Classes.ActiveTask.intrinsics_VB
    Public colorBytes() As Byte
    Public vertices() As Byte
    Public depthBytes() As Byte
    Public RGBDepthBytes() As Byte
    Public leftViewBytes() As Byte
    Public rightViewBytes() As Byte
    Public pointCloudBytes() As Byte

    Public serialNumber As String
    Public failedImageCount As integer
    Public modelInverse As Boolean
    Public newImagesAvailable As Boolean
    Public cPtr As IntPtr
    Public Structure imuDataStruct
        Dim r00 As Single
        Dim r01 As Single
        Dim r02 As Single
        Dim tx As Single
        Dim r10 As Single
        Dim r11 As Single
        Dim r12 As Single
        Dim ty As Single
        Dim r20 As Single
        Dim r21 As Single
        Dim r22 As Single
        Dim tz As Single
        Dim m30 As Single
        Dim m31 As Single
        Dim m32 As Single
        Dim m33 As Single
    End Structure
    Structure PoseData
        Public translation As cv.Point3f
        Public velocity As cv.Point3f
        Public acceleration As cv.Point3f
        Public rotation As Quaternion
        Public angularVelocity As cv.Point3f
        Public angularAcceleration As cv.Point3f
        Public trackerConfidence As integer
        Public mapperConfidence As integer
    End Structure
    Public Function setintrinsics(intrinsicsHW As rs.Intrinsics) As VB_Classes.ActiveTask.intrinsics_VB
        Dim intrinsics As New VB_Classes.ActiveTask.intrinsics_VB
        intrinsics.ppx = intrinsicsHW.ppx
        intrinsics.ppy = intrinsicsHW.ppy
        intrinsics.fx = intrinsicsHW.fx
        intrinsics.fy = intrinsicsHW.fy
        intrinsics.FOV = intrinsicsHW.FOV
        intrinsics.coeffs = intrinsicsHW.coeffs
        Return intrinsics
    End Function
    Public Sub New()
    End Sub
    Public Sub GetNextFrameCounts(frameTime As Double)
        Static imageCounter As Integer
        Static totalMS = frameTime
        If totalMS > 1000 Then
            imageCounter = 0
            totalMS = totalMS - 1000
        End If
        imageCounter += 1
        totalMS += frameTime

        Static lastFrameTime = IMU_TimeStamp
        Static imuStartTime = IMU_TimeStamp
        IMU_FrameTime = IMU_TimeStamp - lastFrameTime - imuStartTime
        lastFrameTime = IMU_TimeStamp - imuStartTime

        Static myStopWatch As New System.Diagnostics.Stopwatch
        If frameCount = 0 Then myStopWatch.Start()
        CPU_TimeStamp = myStopWatch.ElapsedMilliseconds
        Static lastCPUTime = CPU_TimeStamp
        CPU_FrameTime = CPU_TimeStamp - lastCPUTime
        lastCPUTime = CPU_TimeStamp

        frameCount += 1
        newImagesAvailable = True
    End Sub
    Public Sub closePipe()
        pipelineClosed = True
        frameCount = 0
    End Sub
End Class
