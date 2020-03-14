Imports System.Windows.Controls
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Module Zed2_Interface
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2Open(width As Int32, height As Int32, fps As Int32) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2SerialNumber(cPtr As IntPtr) As Int32
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2WaitFrame(cPtr As IntPtr, rgba As IntPtr, depthRGBA As IntPtr, depth32f As IntPtr, left As IntPtr,
                                  right As IntPtr, pointCloud As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2Extrinsics(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2intrinsicsLeft(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2intrinsicsRight(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2Translation(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2RotationMatrix(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2Acceleration(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2AngularVelocity(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2IMU_Temperature(cPtr As IntPtr) As Single
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2IMU_TimeStamp(cPtr As IntPtr) As Double
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2IMU_Barometer(cPtr As IntPtr) As Single
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2Orientation(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Zed2.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Zed2IMU_Magnetometer(cPtr As IntPtr) As IntPtr
    End Function
End Module
Public Class CameraZED2
    Inherits Camera
    Structure intrinsicsLeftZed
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

    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        cPtr = Zed2Open(width, height, 60)
        deviceName = "StereoLabs ZED 2"
        pcMultiplier = 0.001
        If cPtr <> 0 Then
            Dim serialNumber = Zed2SerialNumber(cPtr)
            Console.WriteLine("ZED 2 serial number = " + CStr(serialNumber))
            w = width
            h = height
            leftView = New cv.Mat

            Dim ptr = Zed2Extrinsics(cPtr)
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

            ptr = Zed2intrinsicsLeft(cPtr)
            Dim intrinsics = Marshal.PtrToStructure(Of intrinsicsLeftZed)(ptr)
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

            ptr = Zed2intrinsicsRight(cPtr)
            intrinsics = Marshal.PtrToStructure(Of intrinsicsLeftZed)(ptr)
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
            ReDim RGBADepthBytes(w * h * 4)
            ReDim depth32FBytes(w * h * 4)
            ReDim leftViewBytes(w * h)
            ReDim rightViewBytes(w * h)
            ReDim pointCloudBytes(w * h * 12) ' xyz + rgba
        End If
    End Sub

    Public Sub GetNextFrame()
        If cPtr = 0 Then Return
        Dim handlecolorBytes = GCHandle.Alloc(colorBytes, GCHandleType.Pinned)
        Dim handleRGBADepthBytes = GCHandle.Alloc(RGBADepthBytes, GCHandleType.Pinned)
        Dim handledepth32Fbytes = GCHandle.Alloc(depth32FBytes, GCHandleType.Pinned)
        Dim handleLeftViewBytes = GCHandle.Alloc(leftViewBytes, GCHandleType.Pinned)
        Dim handleRightViewBytes = GCHandle.Alloc(rightViewBytes, GCHandleType.Pinned)
        Dim handlePCBytes = GCHandle.Alloc(pointCloudBytes, GCHandleType.Pinned)
        Dim imuFrame = Zed2WaitFrame(cPtr, handlecolorBytes.AddrOfPinnedObject(), handleRGBADepthBytes.AddrOfPinnedObject(),
                                           handledepth32Fbytes.AddrOfPinnedObject(), handleLeftViewBytes.AddrOfPinnedObject(),
                                           handleRightViewBytes.AddrOfPinnedObject(), handlePCBytes.AddrOfPinnedObject())

        handlecolorBytes.Free()
        handleRGBADepthBytes.Free()
        handledepth32Fbytes.Free()
        handleLeftViewBytes.Free()
        handleRightViewBytes.Free()
        handlePCBytes.Free()

        SyncLock OpenCVB.camPic
            Dim acc = Zed2Acceleration(cPtr)
            imuAccel = Marshal.PtrToStructure(Of cv.Point3f)(acc)

            Dim ang = Zed2AngularVelocity(cPtr)
            Dim angularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(ang)

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
            Dim tr = Zed2Translation(cPtr)
            Dim translation(2) As Single
            Marshal.Copy(tr, translation, 0, translation.Length)

            Dim rot = Zed2RotationMatrix(cPtr)
            Dim rotation(8) As Single
            Marshal.Copy(rot, rotation, 0, rotation.Length)

            IMU_Barometer = Zed2IMU_Barometer(cPtr)
            Dim mag = Zed2IMU_Magnetometer(cPtr)
            IMU_Magnetometer = Marshal.PtrToStructure(Of cv.Point3f)(mag)

            IMU_Temperature = Zed2IMU_Temperature(cPtr)

            IMU_TimeStamp = Zed2IMU_TimeStamp(cPtr)
            Static imuStartTime = IMU_TimeStamp
            IMU_TimeStamp -= imuStartTime

            Dim colorRGBA = New cv.Mat(h, w, cv.MatType.CV_8UC4, colorBytes)
            color = colorRGBA.CvtColor(cv.ColorConversionCodes.BGRA2BGR)

            Dim RGBADepth = New cv.Mat(h, w, cv.MatType.CV_8UC4, RGBADepthBytes)
            RGBDepth = RGBADepth.CvtColor(cv.ColorConversionCodes.BGRA2BGR)

            Dim depth32f = New cv.Mat(h, w, cv.MatType.CV_32F, depth32FBytes)
            depth32f.ConvertTo(depth16, cv.MatType.CV_16U)

            leftView = New cv.Mat(h, w, cv.MatType.CV_8UC1, leftViewBytes)
            rightView = New cv.Mat(h, w, cv.MatType.CV_8UC1, rightViewBytes)
            pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, pointCloudBytes) * pcMultiplier ' change to meters...
        End SyncLock
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
End Class
