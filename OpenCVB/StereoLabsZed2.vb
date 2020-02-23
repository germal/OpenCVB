Imports System.Windows.Controls
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Module Zed2_Interface
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2Open(width As Int32, height As Int32, fps As Int32) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2SerialNumber(kc As IntPtr) As Int32
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2WaitFrame(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2Extrinsics(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2intrinsicsLeft(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2intrinsicsRight(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2PointCloud(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2LeftView(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2RightView(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2RGBADepth(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2Depth32f(kc As IntPtr) As IntPtr
    End Function
End Module
Public Class StereoLabsZed2
    Structure imuData
        Dim temperature As Single
        Dim imuAccel As cv.Point3f
        Dim accelTimeStamp As Long
        Dim imuGyro As cv.Point3f
        Dim gyroTimeStamp As Long
    End Structure
    Structure intrinsicsLeftData
        Dim fx As Single            ' Focal length x */
        Dim fy As Single            ' Focal length y */
        Dim cx As Single            ' Principal point In image, x */
        Dim cy As Single            ' Principal point In image, y */
        Dim k1 As Double ' Distortion factor :  [ k1, k2, p1, p2, k3 ]. Radial (k1,k2,k3) And Tangential (p1,p2) distortion
        Dim k2 As Double
        Dim p1 As Double
        Dim p2 As Double
        Dim k3 As Double
        Dim v_fov As Single ' vertical field of view in degrees.
        Dim h_fov As Single ' horizontal field of view in degrees.
        Dim d_fov As Single ' diagonal field of view in degrees.
        Dim width As Int64
        Dim height As Int64
    End Structure

    Dim Zed2 As IntPtr
    Public color As New cv.Mat
    Public depth16 As New cv.Mat
    Public RGBDepth As New cv.Mat
    Public deviceCount As Int32
    Public deviceName As String = "StereoLabs ZED 2"
    Public disparity As cv.Mat
    Public Extrinsics_VB As VB_Classes.ActiveClass.Extrinsics_VB
    Public h As Int32
    Public imuAccel As cv.Point3f
    Public imuGyro As cv.Point3f
    Public imuPresent As Boolean = True ' Zed2 cameras always have an IMU.
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
        Zed2 = Zed2Open(width, height, 60)
        If Zed2 <> 0 Then
            Dim serialNumber = Zed2SerialNumber(Zed2)
            Console.WriteLine("ZED 2 serial number = " + CStr(serialNumber))
            w = width
            h = height
            disparity = New cv.Mat
            leftView = New cv.Mat

            Dim ptr = Zed2Extrinsics(Zed2)
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

            ptr = Zed2intrinsicsLeft(Zed2)
            Dim intrinsics = Marshal.PtrToStructure(Of intrinsicsLeftData)(ptr)
            intrinsicsLeft_VB.ppx = intrinsics.cx
            intrinsicsLeft_VB.ppy = intrinsics.cy
            intrinsicsLeft_VB.fx = intrinsics.fx
            intrinsicsLeft_VB.fy = intrinsics.fy
            ReDim intrinsicsLeft_VB.FOV(2)
            intrinsicsLeft_VB.FOV(0) = intrinsics.v_fov
            intrinsicsLeft_VB.FOV(1) = intrinsics.h_fov
            intrinsicsLeft_VB.FOV(2) = intrinsics.d_fov
            ReDim intrinsicsLeft_VB.coeffs(5)
            intrinsicsLeft_VB.coeffs(0) = intrinsics.k1
            intrinsicsLeft_VB.coeffs(1) = intrinsics.k2
            intrinsicsLeft_VB.coeffs(2) = intrinsics.p1
            intrinsicsLeft_VB.coeffs(3) = intrinsics.p2
            intrinsicsLeft_VB.coeffs(4) = intrinsics.k3
            intrinsicsLeft_VB.width = intrinsics.width
            intrinsicsLeft_VB.height = intrinsics.height

            ptr = Zed2intrinsicsRight(Zed2)
            intrinsics = Marshal.PtrToStructure(Of intrinsicsLeftData)(ptr)
            intrinsicsRight_VB.ppx = intrinsics.cx
            intrinsicsRight_VB.ppy = intrinsics.cy
            intrinsicsRight_VB.fx = intrinsics.fx
            intrinsicsRight_VB.fy = intrinsics.fy
            ReDim intrinsicsRight_VB.FOV(2)
            intrinsicsRight_VB.FOV(0) = intrinsics.v_fov
            intrinsicsRight_VB.FOV(1) = intrinsics.h_fov
            intrinsicsRight_VB.FOV(2) = intrinsics.d_fov
            ReDim intrinsicsRight_VB.coeffs(5)
            intrinsicsRight_VB.coeffs(0) = intrinsics.k1
            intrinsicsRight_VB.coeffs(1) = intrinsics.k2
            intrinsicsRight_VB.coeffs(2) = intrinsics.p1
            intrinsicsRight_VB.coeffs(3) = intrinsics.p2
            intrinsicsRight_VB.coeffs(4) = intrinsics.k3
            intrinsicsRight_VB.width = intrinsics.width
            intrinsicsRight_VB.height = intrinsics.height
        End If
    End Sub

    Public Sub GetNextFrame()
        Dim imuFrame As IntPtr
        If Zed2 = 0 Then Return
        Dim colorPtr = Zed2WaitFrame(Zed2)
        'If imuFrame = 0 Then
        '    Console.WriteLine("Zed2WaitFrame has returned without any image.")
        '    failedImageCount += 1
        '    Exit Sub ' just process the existing images again?  
        'Else
        'Dim imuOutput = Marshal.PtrToStructure(Of imuData)(imuFrame)
        'imuGyro = imuOutput.imuGyro
        'imuAccel = imuOutput.imuAccel

        ' make the imu data consistent with the Intel IMU...
        'Dim tmpVal = imuAccel.Z
        'imuAccel.Z = imuAccel.X
        'imuAccel.X = imuAccel.Y
        'imuAccel.Y = tmpVal

        'tmpVal = imuGyro.Z
        'imuGyro.Z = -imuGyro.X
        'imuGyro.X = imuGyro.Y
        'imuGyro.Y = tmpVal

        'imuTimeStamp = imuOutput.accelTimeStamp
        'End If

        Dim colorRGBA = New cv.Mat(h, w, cv.MatType.CV_8UC4, colorPtr)
        color = colorRGBA.CvtColor(cv.ColorConversionCodes.BGRA2BGR)

        Dim RGBADepth = New cv.Mat(h, w, cv.MatType.CV_8UC4, Zed2RGBADepth(Zed2))
        RGBDepth = RGBADepth.CvtColor(cv.ColorConversionCodes.BGRA2BGR)

        Dim depth32f = New cv.Mat(h, w, cv.MatType.CV_32F, Zed2Depth32f(Zed2))
        depth32f.ConvertTo(depth16, cv.MatType.CV_16U)

        disparity = depth16.Clone()

        leftView = New cv.Mat(h, w, cv.MatType.CV_8UC1, Zed2LeftView(Zed2))
        rightView = New cv.Mat(h, w, cv.MatType.CV_8UC1, Zed2RightView(Zed2))
        pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, Zed2PointCloud(Zed2))
    End Sub
    Public Sub closePipe()
        pipelineClosed = True
    End Sub
End Class
