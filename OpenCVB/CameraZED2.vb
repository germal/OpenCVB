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
    Public Function Zed2WaitFrame(kc As IntPtr, rgba As IntPtr, depthRGBA As IntPtr, depth32f As IntPtr, left As IntPtr,
                                  right As IntPtr, pointCloud As IntPtr) As IntPtr
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
    Public Function Zed2Translation(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2RotationMatrix(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2Acceleration(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2AngularVelocity(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2IMU_Temperature(kc As IntPtr) As Single
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2IMU_TimeStamp(kc As IntPtr) As UInt64
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2IMU_Barometer(kc As IntPtr) As Single
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2Orientation(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_StereoLabsZed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2IMU_Magnetometer(kc As IntPtr) As IntPtr
    End Function
End Module
Public Class CameraZED2
    Structure imuDataStruct
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
    Structure intrinsicsLeftData
        Dim fx As Single ' Focal length x */
        Dim fy As Single ' Focal length y */
        Dim cx As Single ' Principal point In image, x */
        Dim cy As Single ' Principal point In image, y */
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
    Dim colorBytes() As Byte
    Dim depthRGBABytes() As Byte
    Dim depth32FBytes() As Byte
    Dim leftBytes() As Byte
    Dim rightBytes() As Byte
    Dim pointCloudBytes() As Byte
    Public deviceCount As Int32
    Public deviceName As String = "StereoLabs ZED 2"
    Public disparity As cv.Mat
    Public Extrinsics_VB As VB_Classes.ActiveClass.Extrinsics_VB
    Public h As Int32
    Public imuAccel As cv.Point3f
    Public imuGyro As cv.Point3f
    Public IMU_Present As Boolean = True ' Zed2 cameras always have an IMU.
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
    Public transformationMatrix() As Single
    Public IMU_Barometer As Single
    Public IMU_Magnetometer As cv.Point3f
    Public IMU_Temperature As Single
    Public IMU_TimeStamp As Double
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

            ReDim colorBytes(w * h * 4) ' rgba format coming back from driver
            ReDim depthRGBABytes(w * h * 4)
            ReDim depth32FBytes(w * h * 4)
            ReDim leftBytes(w * h)
            ReDim rightBytes(w * h)
            ReDim pointCloudBytes(w * h * 12) ' xyz + rgba
        End If
    End Sub

    Public Sub GetNextFrame()
        If Zed2 = 0 Then Return
        Dim handlecolorBytes = GCHandle.Alloc(colorBytes, GCHandleType.Pinned)
        Dim handleDepthRGBABytes = GCHandle.Alloc(depthRGBABytes, GCHandleType.Pinned)
        Dim handledepth32Fbytes = GCHandle.Alloc(depth32FBytes, GCHandleType.Pinned)
        Dim handleleftBytes = GCHandle.Alloc(leftBytes, GCHandleType.Pinned)
        Dim handlerightBytes = GCHandle.Alloc(rightBytes, GCHandleType.Pinned)
        Dim handlePCBytes = GCHandle.Alloc(pointCloudBytes, GCHandleType.Pinned)
        Dim imuFrame = Zed2WaitFrame(Zed2, handlecolorBytes.AddrOfPinnedObject(), handleDepthRGBABytes.AddrOfPinnedObject(),
                                           handledepth32Fbytes.AddrOfPinnedObject(), handleleftBytes.AddrOfPinnedObject(),
                                           handlerightBytes.AddrOfPinnedObject(), handlePCBytes.AddrOfPinnedObject())
        Dim acc = Zed2Acceleration(Zed2)
        imuAccel = Marshal.PtrToStructure(Of cv.Point3f)(acc)

        Dim ang = Zed2AngularVelocity(Zed2)
        Dim angularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(ang)

        ' there must be a bug in the structure for acceleration and velocity.  This is an attempt to make something work.
        ' Eliminate this code when the results above make sense.
        imuAccel.Z = imuAccel.X
        imuAccel.Y = angularVelocity.Z
        imuAccel.X = angularVelocity.Y

        ' roll pitch and yaw - but the values are currently incorrect.  Zed must fix this...
        ' all 3 numbers should be near zero for a stationary camera.
        imuGyro.X = angularVelocity.X
        imuGyro.Y = angularVelocity.Y
        imuGyro.Z = angularVelocity.Z

        Dim rt = Marshal.PtrToStructure(Of imuDataStruct)(imuFrame)
        Dim t = New cv.Point3f(rt.tx, rt.ty, rt.tz)
        Dim mat() As Single = {-rt.r00, rt.r01, -rt.r02, 0.0,
                               -rt.r10, rt.r11, rt.r12, 0.0,
                               -rt.r20, rt.r21, -rt.r22, 0.0,
                               t.X, t.Y, t.Z, 1.0}
        transformationMatrix = mat

        ' testing to see if we could have computed this independently...
        Dim tr = Zed2Translation(Zed2)
        Dim translation(2) As Single
        Marshal.Copy(tr, translation, 0, translation.Length)

        Dim rot = Zed2RotationMatrix(Zed2)
        Dim rotation(8) As Single
        Marshal.Copy(rot, rotation, 0, rotation.Length)

        handlecolorBytes.Free()
        handleDepthRGBABytes.Free()
        handledepth32Fbytes.Free()
        handleleftBytes.Free()
        handlerightBytes.Free()
        handlePCBytes.Free()

        IMU_Barometer = Zed2IMU_Barometer(Zed2)
        Dim mag = Zed2IMU_Magnetometer(Zed2)
        IMU_Magnetometer = Marshal.PtrToStructure(Of cv.Point3f)(mag)

        IMU_Temperature = Zed2IMU_Temperature(Zed2)

        Static startTime = Zed2IMU_TimeStamp(Zed2)
        Dim tmp = Zed2IMU_TimeStamp(Zed2)
        IMU_TimeStamp = tmp - startTime
        If IMU_TimeStamp < 0 Then
            startTime = tmp
            IMU_TimeStamp = tmp
        End If

        Dim colorRGBA = New cv.Mat(h, w, cv.MatType.CV_8UC4, colorBytes)
        color = colorRGBA.CvtColor(cv.ColorConversionCodes.BGRA2BGR)

        Dim RGBADepth = New cv.Mat(h, w, cv.MatType.CV_8UC4, depthRGBABytes)
        RGBDepth = RGBADepth.CvtColor(cv.ColorConversionCodes.BGRA2BGR)

        Dim depth32f = New cv.Mat(h, w, cv.MatType.CV_32F, depth32FBytes)
        depth32f.ConvertTo(depth16, cv.MatType.CV_16U)

        disparity = depth16.Clone()

        leftView = New cv.Mat(h, w, cv.MatType.CV_8UC1, leftBytes)
        rightView = New cv.Mat(h, w, cv.MatType.CV_8UC1, rightBytes)
        pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, pointCloudBytes)
    End Sub
    Public Sub closePipe()
        pipelineClosed = True
    End Sub
End Class
