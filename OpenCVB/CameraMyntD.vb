Imports System.Windows.Controls
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Module MyntD_Interface
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDOpen(width As Int32, height As Int32, fps As Int32) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDWaitFrame(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDLeftImage(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDRightImage(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDImageDepth(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDImageRGBdepth(cPtr As IntPtr) As IntPtr
    End Function


    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDExtrinsics(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDintrinsicsLeft(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDintrinsicsRight(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDTranslation(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDRotationMatrix(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDAcceleration(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDAngularVelocity(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDIMU_Temperature(cPtr As IntPtr) As Single
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDIMU_TimeStamp(cPtr As IntPtr) As Double
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDIMU_Barometer(cPtr As IntPtr) As Single
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDOrientation(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_MyntD.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function MyntDIMU_Magnetometer(cPtr As IntPtr) As IntPtr
    End Function
End Module
Public Class CameraMyntD
    Inherits Camera
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

    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        cPtr = MyntDOpen(width, height, 30)
        MyBase.deviceName = "MYNT EYE D 1000"
        If cPtr <> 0 Then
            w = width
            h = height

            'Dim ptr = MyntDExtrinsics(cPtr)
            'Dim rotationTranslation(12) As Single
            'Marshal.Copy(ptr, rotationTranslation, 0, rotationTranslation.Length)
            'ReDim Extrinsics_VB.rotation(8)
            'ReDim Extrinsics_VB.translation(2)
            'For i = 0 To Extrinsics_VB.rotation.Length - 1
            '    Extrinsics_VB.rotation(i) = rotationTranslation(i)
            'Next
            'For i = 0 To Extrinsics_VB.translation.Length - 1
            '    Extrinsics_VB.translation(i) = rotationTranslation(i + Extrinsics_VB.rotation.Length - 1)
            'Next

            'ptr = MyntDintrinsicsLeft(cPtr)
            'Dim intrinsics = Marshal.PtrToStructure(Of intrinsicsLeftData)(ptr)
            'intrinsicsLeft_VB.ppx = intrinsics.cx
            'intrinsicsLeft_VB.ppy = intrinsics.cy
            'intrinsicsLeft_VB.fx = intrinsics.fx
            'intrinsicsLeft_VB.fy = intrinsics.fy
            'ReDim intrinsicsLeft_VB.FOV(2)
            'intrinsicsLeft_VB.FOV(0) = intrinsics.v_fov
            'intrinsicsLeft_VB.FOV(1) = intrinsics.h_fov
            'intrinsicsLeft_VB.FOV(2) = intrinsics.d_fov
            'ReDim intrinsicsLeft_VB.coeffs(5)
            'intrinsicsLeft_VB.coeffs(0) = intrinsics.k1
            'intrinsicsLeft_VB.coeffs(1) = intrinsics.k2
            'intrinsicsLeft_VB.coeffs(2) = intrinsics.p1
            'intrinsicsLeft_VB.coeffs(3) = intrinsics.p2
            'intrinsicsLeft_VB.coeffs(4) = intrinsics.k3
            'intrinsicsLeft_VB.width = intrinsics.width
            'intrinsicsLeft_VB.height = intrinsics.height

            'ptr = MyntDintrinsicsRight(cPtr)
            'intrinsics = Marshal.PtrToStructure(Of intrinsicsLeftData)(ptr)
            'intrinsicsRight_VB.ppx = intrinsics.cx
            'intrinsicsRight_VB.ppy = intrinsics.cy
            'intrinsicsRight_VB.fx = intrinsics.fx
            'intrinsicsRight_VB.fy = intrinsics.fy
            'ReDim intrinsicsRight_VB.FOV(2)
            'intrinsicsRight_VB.FOV(0) = intrinsics.v_fov
            'intrinsicsRight_VB.FOV(1) = intrinsics.h_fov
            'intrinsicsRight_VB.FOV(2) = intrinsics.d_fov
            'ReDim intrinsicsRight_VB.coeffs(5)
            'intrinsicsRight_VB.coeffs(0) = intrinsics.k1
            'intrinsicsRight_VB.coeffs(1) = intrinsics.k2
            'intrinsicsRight_VB.coeffs(2) = intrinsics.p1
            'intrinsicsRight_VB.coeffs(3) = intrinsics.p2
            'intrinsicsRight_VB.coeffs(4) = intrinsics.k3
            'intrinsicsRight_VB.width = intrinsics.width
            'intrinsicsRight_VB.height = intrinsics.height
        End If
    End Sub

    Public Sub GetNextFrame()
        If pipelineClosed Or cPtr = 0 Then Exit Sub
        Dim imagePtr = MyntDWaitFrame(cPtr)
        'Dim acc = MyntDAcceleration(cPtr)
        'IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(acc)

        'Dim ang = MyntDAngularVelocity(cPtr)
        'Dim angularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(ang)

        'IMU_Acceleration.Z = IMU_Acceleration.X
        'IMU_Acceleration.Y = angularVelocity.Z
        'IMU_Acceleration.X = angularVelocity.Y

        '' all 3 numbers should be near zero for a stationary camera.
        'IMU_AngularVelocity.X = angularVelocity.X
        'IMU_AngularVelocity.Y = angularVelocity.Y
        'IMU_AngularVelocity.Z = angularVelocity.Z

        'Dim rt = Marshal.PtrToStructure(Of imuDataStruct)(imuFrame)
        'Dim t = New cv.Point3f(rt.tx, rt.ty, rt.tz)
        'Dim mat() As Single = {-rt.r00, rt.r01, -rt.r02, 0.0,
        '                       -rt.r10, rt.r11, rt.r12, 0.0,
        '                       -rt.r20, rt.r21, -rt.r22, 0.0,
        '                       t.X, t.Y, t.Z, 1.0}
        'transformationMatrix = mat

        '' testing to see if we could have computed this independently...
        'Dim tr = MyntDTranslation(cPtr)
        'Dim translation(2) As Single
        'Marshal.Copy(tr, translation, 0, translation.Length)

        'Dim rot = MyntDRotationMatrix(cPtr)
        'Dim rotation(8) As Single
        'Marshal.Copy(rot, rotation, 0, rotation.Length)

        'handlecolorBytes.Free()
        'handleRGBDepthBytes.Free()
        'handledepthBytes.Free()
        'handleLeftViewBytes.Free()
        'handleRightViewBytes.Free()
        'handlePCBytes.Free()

        'IMU_Barometer = MyntDIMU_Barometer(cPtr)
        'Dim mag = MyntDIMU_Magnetometer(cPtr)
        'IMU_Magnetometer = Marshal.PtrToStructure(Of cv.Point3f)(mag)

        'IMU_Temperature = MyntDIMU_Temperature(cPtr)

        'Static startTime = MyntDIMU_TimeStamp(cPtr)
        'IMU_TimeStamp = MyntDIMU_TimeStamp(cPtr) - startTime


        Dim depthRGBPtr = MyntDImageRGBdepth(cPtr)
        Dim depth16Ptr = MyntDImageDepth(cPtr)
        Dim rightPtr = MyntDRightImage(cPtr)
        SyncLock bufferLock
            If imagePtr <> 0 Then color = New cv.Mat(h, w, cv.MatType.CV_8UC3, imagePtr).Clone()
            leftView = color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If rightPtr <> 0 Then rightView = New cv.Mat(h, w, cv.MatType.CV_8UC3, rightPtr).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            If depthRGBPtr <> 0 Then RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, depthRGBPtr).Clone()

            If depth16Ptr <> 0 Then
                Dim depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, depth16Ptr)

                Dim pcX = New cv.Mat(h, w, cv.MatType.CV_32F, 0)
                Dim pcY = New cv.Mat(h, w, cv.MatType.CV_32F, 0)
                Dim pcZ = New cv.Mat(h, w, cv.MatType.CV_32F)
                depth16.ConvertTo(pcZ, cv.MatType.CV_32F)

                Dim split() = {pcX, pcY, (pcZ * 0.001).ToMat()}
                cv.Cv2.Merge(split, pointCloud)
            End If
        End SyncLock
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
End Class
