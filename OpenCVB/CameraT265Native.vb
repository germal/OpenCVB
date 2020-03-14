#If 0 Then
Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp

Module T265_Module_CPP
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Open(w As Int32, h As Int32) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RawWidth(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RawHeight(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Depth16Width(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Depth16Height(tp As IntPtr) As Int32
    End Function

    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265WaitFrame(tp As IntPtr) As Integer
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RightRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265LeftRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265intrinsicsLeft(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265intrinsicsLeftRight(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Extrinsics(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265PointCloud(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RGBDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Color(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265PoseData(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265IMUTimeStamp(tp As IntPtr) As Double
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Depth16(tp As IntPtr) As IntPtr
    End Function
End Module

    Structure T265IMUdata
    Public translation As cv.Point3f
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
    Public trackerConfidence As Int32
    Public mapperConfidence As Int32
End Structure
Public Class CameraT265Native
    Inherits Camera
    Dim pipeline As New rs.Pipeline ' even though this is not used, removing it makes the interface fail.  The constructor is needed.

    Dim intrinsicsLeft As rs.Intrinsics
    Dim rawHeight As Int32
    Dim rawWidth As Int32
    Dim depth16Height As Int32
    Dim depth16Width As Int32

    Public extrinsics As rs.Extrinsics
    Public pc As New rs.PointCloud

    Public Sub New()
    End Sub
    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        deviceName = "Intel T265"
        IMU_Present = True
        w = width
        h = height

        cPtr = T265Open(width, height)
        rawWidth = T265RawWidth(cPtr)
        rawHeight = T265RawHeight(cPtr)

        depth16Width = T265Depth16Width(cPtr)
        depth16Height = T265Depth16Height(cPtr)

        Dim intrin = T265intrinsicsLeft(cPtr)
        intrinsicsLeft = Marshal.PtrToStructure(Of rs.Intrinsics)(intrin)
        setintrinsicsLeft(intrinsicsLeft)

        Dim extrin = T265Extrinsics(cPtr)
        extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(extrin)
        Extrinsics_VB.rotation = extrinsics.rotation
        Extrinsics_VB.translation = extrinsics.translation

        ReDim colorBytes(w * h * 3 - 1)
        ReDim leftViewBytes(rawHeight * rawWidth - 1)
        ReDim rightViewBytes(leftViewBytes.Length - 1)
        ReDim depthBytes(depth16Width * depth16Height * 2 - 1)
        ReDim RGBDepthBytes(colorBytes.Length - 1)  ' most of the image is grayscale but the 300x300 part is RGB so the whole has to be...
    End Sub
    Public Sub GetNextFrame()
        If pipelineClosed Then Exit Sub

        Dim rc = T265WaitFrame(cPtr)
        If rc = 0 Then Exit Sub

        SyncLock OpenCVB.camPic ' only really need the synclock when in callback mode but it doesn't hurt to waitforframe mode.
            'color = New cv.Mat(h, w, cv.MatType.CV_8UC3, T265Color(cPtr)) ' must be called first to create left view as well...
            'rightView = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_8U, T265RightRaw(cPtr))
            leftView = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_8U, T265LeftRaw(cPtr))
            'RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, T265RGBDepth(cPtr))
            'depth16 = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_16U, T265Depth16(cPtr))

            'Dim posePtr = T265PoseData(cPtr)
            'Dim pose = Marshal.PtrToStructure(Of PoseData)(posePtr)
            'IMU_TimeStamp = T265IMUTimeStamp(cPtr)
            'Static imuStartTime = IMU_TimeStamp
            'IMU_TimeStamp -= imuStartTime

            'IMU_Rotation = pose.rotation
            'Dim q = IMU_Rotation
            'IMU_Translation = pose.translation
            'IMU_Acceleration = pose.acceleration
            'IMU_Velocity = pose.velocity
            'IMU_AngularAcceleration = pose.angularAcceleration
            'IMU_AngularVelocity = pose.angularVelocity
            'Dim t = IMU_Translation
            ''  Set the matrix as column-major for convenient work with OpenGL and rotate by 180 degress (by negating 1st and 3rd columns)
            'Dim mat() As Single = {
            '            -(1 - 2 * q.Y * q.Y - 2 * q.Z * q.Z), -(2 * q.X * q.Y + 2 * q.Z * q.W), -(2 * q.X * q.Z - 2 * q.Y * q.W), 0.0,
            '            2 * q.X * q.Y - 2 * q.Z * q.W, 1 - 2 * q.X * q.X - 2 * q.Z * q.Z, 2 * q.Y * q.Z + 2 * q.X * q.W, 0.0,
            '            -(2 * q.X * q.Z + 2 * q.Y * q.W), -(2 * q.Y * q.Z - 2 * q.X * q.W), -(1 - 2 * q.X * q.X - 2 * q.Y * q.Y), 0.0,
            '            t.X, t.Y, t.Z, 1.0}
            'transformationMatrix = mat
        End SyncLock

        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
End Class
#Else
Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp

Module T265_Module_CPP
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Open(w As Int32, h As Int32) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RawWidth(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RawHeight(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Depth16Width(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Depth16Height(tp As IntPtr) As Int32
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265GetData(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub T265WaitFrame(tp As IntPtr)
    End Sub
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RightRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265LeftRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265intrinsicsLeft(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265intrinsicsLeftRight(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Extrinsics(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265PointCloud(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RGBDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265PoseData(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265TimeStamp(tp As IntPtr) As Double
    End Function
    <DllImport(("Cam_T265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Depth16(tp As IntPtr) As IntPtr
    End Function
End Module

Structure T265IMUdata
    Public translation As cv.Point3f
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
    Public trackerConfidence As Int32
    Public mapperConfidence As Int32
End Structure
Public Class CameraT265Native
    Inherits Camera
#Region "T265Data"
    Dim pipeline As New rs.Pipeline ' even though this is not used, removing it makes the interface fail.  The constructor is needed.

    Dim intrinsicsLeft As rs.Intrinsics
    Dim rawHeight As Int32
    Dim rawWidth As Int32
    Dim depth16Height As Int32
    Dim depth16Width As Int32

    Public extrinsics As rs.Extrinsics
    Public IMUpresent As Boolean = True
    Public imuTimeStamp As Double
    Public pc As New rs.PointCloud

#End Region
    Public Sub New()
    End Sub
    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        deviceName = "Intel T265"
        IMU_Present = True
        w = width
        h = height

        cPtr = T265Open(width, height)
        rawWidth = T265RawWidth(cPtr)
        rawHeight = T265RawHeight(cPtr)

        depth16Width = T265Depth16Width(cPtr)
        depth16Height = T265Depth16Height(cPtr)

        Dim intrin = T265intrinsicsLeft(cPtr)
        intrinsicsLeft = Marshal.PtrToStructure(Of rs.Intrinsics)(intrin)
        setintrinsicsLeft(intrinsicsLeft)

        Dim extrin = T265Extrinsics(cPtr)
        extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(extrin)
        Extrinsics_VB.rotation = extrinsics.rotation
        Extrinsics_VB.translation = extrinsics.translation
    End Sub
    Public Sub GetNextFrame()
        Static colorBytes(w * h * 3 - 1) As Byte
        Static leftBytes(rawHeight * rawWidth - 1) As Byte
        Static rightBytes(leftBytes.Length - 1) As Byte
        Static depth16Bytes(depth16Width * depth16Height * 2 - 1) As Byte
        Static rgbdBytes(colorBytes.Length - 1) As Byte

        If pipelineClosed Then Exit Sub

        T265WaitFrame(cPtr)

        SyncLock OpenCVB.camPic
            Dim colorPtr = T265GetData(cPtr) ' this needs to be first to pull the data from the frameset.
            Dim pose = Marshal.PtrToStructure(Of PoseData)(T265PoseData(cPtr))
            IMU_TimeStamp = T265TimeStamp(cPtr)
            Static imuStartTime = IMU_TimeStamp
            IMU_TimeStamp -= imuStartTime

            IMU_Rotation = pose.rotation
            Dim q = IMU_Rotation
            IMU_Translation = pose.translation
            IMU_Acceleration = pose.acceleration
            IMU_Velocity = pose.velocity
            IMU_AngularAcceleration = pose.angularAcceleration
            IMU_AngularVelocity = pose.angularVelocity
            Dim t = IMU_Translation
            '  Set the matrix as column-major for convenient work with OpenGL and rotate by 180 degress (by negating 1st and 3rd columns)
            Dim mat() As Single = {
                        -(1 - 2 * q.Y * q.Y - 2 * q.Z * q.Z), -(2 * q.X * q.Y + 2 * q.Z * q.W), -(2 * q.X * q.Z - 2 * q.Y * q.W), 0.0,
                        2 * q.X * q.Y - 2 * q.Z * q.W, 1 - 2 * q.X * q.X - 2 * q.Z * q.Z, 2 * q.Y * q.Z + 2 * q.X * q.W, 0.0,
                        -(2 * q.X * q.Z + 2 * q.Y * q.W), -(2 * q.Y * q.Z - 2 * q.X * q.W), -(1 - 2 * q.X * q.X - 2 * q.Y * q.Y), 0.0,
                        t.X, t.Y, t.Z, 1.0}
            transformationMatrix = mat

            leftView = New cv.Mat(h, w, cv.MatType.CV_8UC3, colorPtr)
            color = leftView
            rightView = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_8U, T265RightRaw(cPtr))
            leftView = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_8U, T265LeftRaw(cPtr))
            RGBDepth = New cv.Mat(h, w, cv.MatType.CV_8UC3, T265RGBDepth(cPtr))
            depth16 = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_16U, T265Depth16(cPtr))
        End SyncLock
        MyBase.GetNextFrameCounts(IMU_FrameTime)
    End Sub
End Class

#End If