Imports System.Windows.Controls
Imports rs = Intel.RealSense
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
    Public Function KinectWaitFrame(kc As IntPtr, color As IntPtr, depthRGB As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectExtrinsics(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectIntrinsics(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectPointCloud(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectDepthInColor(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub KinectClose(kc As IntPtr)
    End Sub
End Module
Public Class Kinect : Implements IDisposable
    Structure imuData
        Dim temperature As Single
        Dim imuAccel As cv.Point3f
        Dim accelTimeStamp As Long
        Dim imuGyro As cv.Point3f
        Dim gyroTimeStamp As Long
    End Structure
    Structure intrinsicsData
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

    Public imuPresent As Boolean = True ' kinect cameras always have an IMU.
    Public imuAccel As cv.Point3f
    Public imuGyro As cv.Point3f
    Public imuTimeStamp As Double

    Dim kc As IntPtr
    Public depthIntrinsics As rs.Intrinsics
    Public colorIntrinsics As rs.Intrinsics
    Public modelInverse As Boolean

    Public w As Int32
    Public h As Int32

    Public failedImageCount As Int32

    Public deviceName As String = "Kinect for Azure"
    Public deviceCount As Int32
    Public serialNumber As String
    Public intrinsics_VB As VB_Classes.ActiveClass.Intrinsics_VB
    Public Extrinsics_VB As VB_Classes.ActiveClass.Extrinsics_VB

    Public color As cv.Mat
    Public depth As cv.Mat
    Public depthRGB As cv.Mat
    Public pointCloud As cv.Mat
    Public disparity As cv.Mat
    Public redLeft As cv.Mat
    Public redRight As cv.Mat

    Public pcBufferSize As Int32

    Public colorBytes() As Byte
    Public depthBytes() As Byte
    Public depthRGBBytes() As Byte
    Public Sub New()
        kc = KinectOpen()
        If kc <> 0 Then
            deviceCount = KinectDeviceCount(kc)
            Dim strPtr = KinectDeviceName(kc) ' The width and height of the image are set in the constructor.
            serialNumber = Marshal.PtrToStringAnsi(strPtr)
            w = 1280
            h = 720
            disparity = New cv.Mat
            redLeft = New cv.Mat

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

            ptr = KinectIntrinsics(kc)
            Dim intrinsicsOutput = Marshal.PtrToStructure(Of intrinsicsData)(ptr)
            intrinsics_VB.ppx = intrinsicsOutput.cx
            intrinsics_VB.ppy = intrinsicsOutput.cy
            intrinsics_VB.fx = intrinsicsOutput.fx
            intrinsics_VB.fy = intrinsicsOutput.fy
            ReDim intrinsics_VB.FOV(2)
            intrinsics_VB.FOV(0) = intrinsicsOutput.fy
            intrinsics_VB.FOV(1) = intrinsicsOutput.fy
            ReDim intrinsics_VB.coeffs(5)
            intrinsics_VB.coeffs(0) = intrinsicsOutput.k1
            intrinsics_VB.coeffs(1) = intrinsicsOutput.k2
            intrinsics_VB.coeffs(2) = intrinsicsOutput.k3
            intrinsics_VB.coeffs(3) = intrinsicsOutput.k4
            intrinsics_VB.coeffs(4) = intrinsicsOutput.k5
            intrinsics_VB.coeffs(5) = intrinsicsOutput.k6

            ReDim colorBytes(w * h * 3 - 1)
            ReDim depthRGBBytes(w * h * 3 - 1)
            ReDim depthBytes(w * h * System.Runtime.InteropServices.Marshal.SizeOf(GetType(UShort)) - 1)
            pcBufferSize = w * h * System.Runtime.InteropServices.Marshal.SizeOf(GetType(Single)) * 3 ' converted from short's to float's below...
            pointCloud = New cv.Mat
        End If
    End Sub

    Public Sub GetNextFrame()
        Dim imuFrame As IntPtr
        If kc = 0 Then Return
        Dim handleColor = GCHandle.Alloc(colorBytes, GCHandleType.Pinned)
        Dim handleDepthRGB = GCHandle.Alloc(depthRGBBytes, GCHandleType.Pinned)
        Try
            imuFrame = KinectWaitFrame(kc, handleColor.AddrOfPinnedObject(), handleDepthRGB.AddrOfPinnedObject())
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

            ' make the imu consistent with the Intel IMU...
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

        handleColor.Free()
        handleDepthRGB.Free()

        color = New cv.Mat(h, w, cv.MatType.CV_8UC3, colorBytes)

        depthRGB = New cv.Mat(h, w, cv.MatType.CV_8UC3, depthRGBBytes)
        depth = New cv.Mat(h, w, cv.MatType.CV_16U, KinectDepthInColor(kc)) ' using the depth buffer right where kinect placed it.  C++ buffer

        Dim tmp As New cv.Mat
        cv.Cv2.Normalize(depth, tmp, 0, 255, cv.NormTypes.MinMax)
        tmp.ConvertTo(redLeft, cv.MatType.CV_8U)
        redRight = redLeft
        depth.ConvertTo(disparity, cv.MatType.CV_32F)

        Dim pc = New cv.Mat(h, w, cv.MatType.CV_16SC3, KinectPointCloud(kc))
        pc.ConvertTo(pointCloud, cv.MatType.CV_32FC3) ' This is less efficient than using 16-bit pixels but consistent with Intel cameras (and more widely accepted as normal.)
    End Sub
#Region "IDisposable Support"
    Private disposedValue As Boolean ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        KinectClose(kc)
        If Not disposedValue Then
            If disposing Then
                ' TODO: dispose managed state (managed objects).
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
            ' TODO: set large fields to null.
        End If
        disposedValue = True
    End Sub

    ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
    'Protected Overrides Sub Finalize()
    '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        Dispose(True)
        ' TODO: uncomment the following line if Finalize() is overridden above.
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region
End Class
