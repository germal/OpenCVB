Imports cv = OpenCvSharp
Imports System.Numerics
Imports rs = Intel.RealSense
Public Class Camera
    Public pipelineClosed As Boolean = False
    Public transformationMatrix() As Single
    Public IMU_Barometer As Single
    Public IMU_Magnetometer As cv.Point3f
    Public IMU_Temperature As Single
    Public IMU_TimeStamp As Double
    Public IMU_Rotation As System.Numerics.Quaternion
    Public IMU_Translation As cv.Point3f
    Public IMU_Acceleration As cv.Point3f
    Public IMU_Velocity As cv.Point3f
    Public IMU_AngularAcceleration As cv.Point3f
    Public IMU_AngularVelocity As cv.Point3f
    Public IMU_FrameTime As Double
    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double
    Public frameCount As Integer
    Public pcMultiplier As Single = 1
    Public pointCloud As cv.Mat
    Public h As Int32
    Public w As Int32
    Public color As cv.Mat
    Public depth16 As cv.Mat
    Public RGBDepth As cv.Mat
    Public deviceCount As Int32
    Public deviceName As String
    Public Extrinsics_VB As VB_Classes.ActiveClass.Extrinsics_VB
    Public imuAccel As cv.Point3f
    Public imuGyro As cv.Point3f
    Public IMU_Present As Boolean
    Public intrinsicsLeft_VB As VB_Classes.ActiveClass.intrinsics_VB
    Public intrinsicsRight_VB As VB_Classes.ActiveClass.intrinsics_VB
    Public leftView As cv.Mat
    Public rightView As cv.Mat

    Public colorBytes() As Byte
    Public vertices() As Byte
    Public depthBytes() As Byte
    Public RGBDepthBytes() As Byte
    Public RGBADepthBytes() As Byte
    Public leftViewBytes() As Byte
    Public rightViewBytes() As Byte
    Public disparityBytes() As Byte
    Public depth32FBytes() As Byte
    Public pointCloudBytes() As Byte

    Public serialNumber As String
    Public failedImageCount As Int32
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
        Public trackerConfidence As Int32
        Public mapperConfidence As Int32
    End Structure
    Public Sub setintrinsicsLeft(intrinsicsLeft As rs.Intrinsics)
        intrinsicsLeft_VB.width = intrinsicsLeft.width
        intrinsicsLeft_VB.height = intrinsicsLeft.height
        intrinsicsLeft_VB.ppx = intrinsicsLeft.ppx
        intrinsicsLeft_VB.ppy = intrinsicsLeft.ppy
        intrinsicsLeft_VB.fx = intrinsicsLeft.fx
        intrinsicsLeft_VB.fy = intrinsicsLeft.fy
        intrinsicsLeft_VB.FOV = intrinsicsLeft.FOV
        intrinsicsLeft_VB.coeffs = intrinsicsLeft.coeffs
        If intrinsicsLeft_VB.FOV Is Nothing Then ReDim intrinsicsLeft_VB.FOV(2)
        If intrinsicsLeft_VB.coeffs Is Nothing Then ReDim intrinsicsLeft_VB.coeffs(5)
    End Sub
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
