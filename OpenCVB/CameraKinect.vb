Imports System.Windows.Controls
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Module Kinect_Interface
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectOpen() As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectDeviceCount(kc As IntPtr) As Int32
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectDeviceName(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectWaitFrame(kc As IntPtr, RGBDepth As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectExtrinsics(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectintrinsicsLeft(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectPointCloud(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectRGBA(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectDepth16(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectDepthInColor(kc As IntPtr) As IntPtr
    End Function
End Module
Public Class CameraKinect
    Structure imuData
        Dim temperature As Single
        Dim imuAccel As cv.Point3f
        Dim accelTimeStamp As Long
        Dim imuGyro As cv.Point3f
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

    Dim kc As IntPtr
    Public color As cv.Mat
    Public depth16 As cv.Mat
    Public RGBDepth As cv.Mat
    Public RGBDepthBytes() As Byte
    Public deviceCount As Int32
    Public deviceName As String = "Kinect for Azure"
    Public disparity As cv.Mat
    Public Extrinsics_VB As VB_Classes.ActiveClass.Extrinsics_VB
    Public failedImageCount As Int32
    Public h As Int32
    Public imuAccel As cv.Point3f
    Public imuGyro As cv.Point3f
    Public imuPresent As Boolean = True ' kinect cameras always have an IMU.
    Public imuTimeStamp As Double
    Public intrinsicsLeft_VB As VB_Classes.ActiveClass.intrinsics_VB
    Public intrinsicsRight_VB As VB_Classes.ActiveClass.intrinsics_VB
    Public modelInverse As Boolean
    Public pointCloud As New cv.Mat
    Public pcMultiplier As Single = 0.001
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public serialNumber As String
    Public w As Int32
    Public pipelineClosed As Boolean = False
    Public transformationMatrix(15) As Single
    Public Sub New()
    End Sub
    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        kc = KinectOpen()
        If kc <> 0 Then
            deviceCount = KinectDeviceCount(kc)
            Dim strPtr = KinectDeviceName(kc) ' The width and height of the image are set in the constructor.
            serialNumber = Marshal.PtrToStringAnsi(strPtr)
            w = width
            h = height
            disparity = New cv.Mat
            leftView = New cv.Mat

            Dim ptr = KinectExtrinsics(kc)
            Dim rotationTranslation(12) As Single
            Marshal.Copy(ptr, rotationTranslation, 0, rotationTranslation.Length)
            ReDim Extrinsics_VB.rotation(8)
            ReDim Extrinsics_VB.translation(2)
            For i = 0 To Extrinsics_VB.rotation.Length - 1
                Extrinsics_VB.rotation(i) = rotationTranslation(i)
            Next
            For i = 0 To Extrinsics_VB.translation.Length - 1
                Extrinsics_VB.translation(i) = rotationTranslation(i + Extrinsics_VB.rotation.Length - 1)
            Next

            ptr = KinectintrinsicsLeft(kc)
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
        End If
    End Sub

    Public Sub GetNextFrame()
        Dim imuFrame As IntPtr
        If kc = 0 Then Return
        Dim handleRGBDepth = GCHandle.Alloc(RGBDepthBytes, GCHandleType.Pinned)
        Try
            imuFrame = KinectWaitFrame(kc, handleRGBDepth.AddrOfPinnedObject())
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
            imuGyro = imuOutput.imuGyro
            imuAccel = imuOutput.imuAccel

            ' make the imu data consistent with the Intel IMU...
            Dim tmpVal = imuAccel.Z
            imuAccel.Z = imuAccel.X
            imuAccel.X = imuAccel.Y
            imuAccel.Y = tmpVal

            tmpVal = imuGyro.Z
            imuGyro.Z = -imuGyro.X
            imuGyro.X = imuGyro.Y
            imuGyro.Y = tmpVal

            imuTimeStamp = imuOutput.accelTimeStamp
        End If

        handleRGBDepth.Free()

        Dim colorBuffer = KinectRGBA(kc)
        If colorBuffer <> 0 Then
            Dim colorRGBA = New cv.Mat(h, w, cv.MatType.CV_8UC4, colorBuffer)
            color = colorRGBA.CvtColor(cv.ColorConversionCodes.BGRA2BGR)

            RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, RGBDepthBytes)
            depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, KinectDepth16(kc))

            Dim tmp As New cv.Mat
            cv.Cv2.Normalize(depth16, tmp, 0, 255, cv.NormTypes.MinMax)
            tmp.ConvertTo(leftView, cv.MatType.CV_8U)
            rightView = leftView
            depth16.ConvertTo(disparity, cv.MatType.CV_32F)

            Dim pc = New cv.Mat(h, w, cv.MatType.CV_16SC3, KinectPointCloud(kc))
            pc.ConvertTo(pointCloud, cv.MatType.CV_32FC3) ' This is less efficient than using 16-bit pixels but consistent with Intel cameras (and more widely accepted as normal.)
        End If
    End Sub
    Public Sub closePipe()
        pipelineClosed = True
    End Sub
End Class
