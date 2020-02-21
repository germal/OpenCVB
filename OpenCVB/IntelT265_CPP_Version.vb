Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp

Module T265_Module
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Open(w As Int32, h As Int32) As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RawWidth(tp As IntPtr) As Int32
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RawHeight(tp As IntPtr) As Int32
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Depth16Width(tp As IntPtr) As Int32
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Depth16Height(tp As IntPtr) As Int32
    End Function

    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265WaitFrame(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RightRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265LeftRaw(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265intrinsicsLeft(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265intrinsicsLeftRight(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Extrinsics(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265PointCloud(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265RGBDepth(tp As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
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
Public Class IntelT265_CPP_Version
#Region "T265Data"
    Dim pipeline As New rs.Pipeline ' even though this is not used, removing it makes the interface fail.  The constructor is needed.

    Dim height As Int32
    Dim intrinsicsLeft As rs.Intrinsics
    Dim rawHeight As Int32
    Dim rawWidth As Int32
    Dim depth16Height As Int32
    Dim depth16Width As Int32
    Dim tp As IntPtr
    Dim width As Int32

    Public color As cv.Mat
    Public depth16 As cv.Mat
    Public deviceCount As Int32
    Public deviceName As String = "Intel T265"
    Public disparity As New cv.Mat
    Public Extrinsics_VB As VB_Classes.ActiveClass.Extrinsics_VB
    Public extrinsics As rs.Extrinsics
    Public failedImageCount As Int32
    Public imuAccel As cv.Point3f
    Public imuGyro As cv.Point3f
    Public IMUpresent As Boolean = True
    Public imuTimeStamp As Double
    Public intrinsicsLeft_VB As VB_Classes.ActiveClass.intrinsics_VB
    Public leftView As cv.Mat
    Public modelInverse As Boolean
    Public pc As New rs.PointCloud
    Public pcMultiplier As Single = 1
    Public pipelineClosed As Boolean = False
    Public pointCloud As cv.Mat
    Public RGBDepth As New cv.Mat
    Public rightView As cv.Mat

    Public dMatleft As cv.Mat
    Public dMatRight As cv.Mat
    Public kMatleft As cv.Mat
    Public kMatRight As cv.Mat
    Public pMatleft As cv.Mat
    Public pMatRight As cv.Mat
    Public rMatleft As cv.Mat
    Public rMatRight As cv.Mat

#End Region
    Private Sub setintrinsicsLeft(intrinsicsLeft As rs.Intrinsics)
        intrinsicsLeft_VB.width = intrinsicsLeft.width
        intrinsicsLeft_VB.height = intrinsicsLeft.height
        intrinsicsLeft_VB.ppx = intrinsicsLeft.ppx
        intrinsicsLeft_VB.ppy = intrinsicsLeft.ppy
        intrinsicsLeft_VB.fx = intrinsicsLeft.fx
        intrinsicsLeft_VB.fy = intrinsicsLeft.fy
        intrinsicsLeft_VB.FOV = intrinsicsLeft.FOV
        intrinsicsLeft_VB.coeffs = intrinsicsLeft.coeffs
    End Sub
    Public Sub New()
    End Sub
    Public Sub initialize(fps As Int32, _width As Int32, _height As Int32)
        width = _width
        height = _height
        tp = T265Open(width, height)
        rawWidth = T265RawWidth(tp)
        rawHeight = T265RawHeight(tp)

        depth16Width = T265Depth16Width(tp)
        depth16Height = T265Depth16Height(tp)

        Dim intrin = T265intrinsicsLeft(tp)
        intrinsicsLeft = Marshal.PtrToStructure(Of rs.Intrinsics)(intrin)
        setintrinsicsLeft(intrinsicsLeft)

        Dim extrin = T265Extrinsics(tp)
        extrinsics = Marshal.PtrToStructure(Of rs.Extrinsics)(extrin)
        Extrinsics_VB.rotation = extrinsics.rotation
        Extrinsics_VB.translation = extrinsics.translation
    End Sub
    Public Sub GetNextFrame()
        Static colorBytes(width * height * 3 - 1) As Byte
        Static leftBytes(rawHeight * rawWidth - 1) As Byte
        Static rightBytes(leftBytes.Length - 1) As Byte
        Static depth16Bytes(depth16Width * depth16Height * 2 - 1) As Byte
        Static rgbdBytes(colorBytes.Length - 1) As Byte

        If pipelineClosed Then Exit Sub

        Dim colorPtr = T265WaitFrame(tp)
        Marshal.Copy(colorPtr, colorBytes, 0, colorBytes.Length - 1)
        leftView = New cv.Mat(height, width, cv.MatType.CV_8UC3, colorBytes)
        color = leftView

        Dim rightPtr = T265RightRaw(tp)
        Marshal.Copy(rightPtr, rightBytes, 0, rightBytes.Length - 1)
        rightView = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_8U, rightBytes)

        Dim leftPtr = T265LeftRaw(tp)
        Marshal.Copy(leftPtr, leftBytes, 0, leftBytes.Length - 1)
        leftView = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_8U, leftBytes)

        Dim rgbdPtr = T265RGBDepth(tp)
        Marshal.Copy(rgbdPtr, rgbdBytes, 0, rgbdBytes.Length - 1)
        RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3, rgbdBytes)

        Dim depthPtr = T265Depth16(tp)
        Marshal.Copy(depthPtr, depth16Bytes, 0, depth16Bytes.Length - 1)
        depth16 = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_16U, depth16Bytes)
        disparity = depth16
    End Sub
    Public Sub closePipe()
        pipelineClosed = True
    End Sub
End Class
