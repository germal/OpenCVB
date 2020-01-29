Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class IntelT265 : Implements IDisposable
    Dim cfg As New rs.Config
    Dim colorBytes() As Byte
    Dim depthBytes() As Byte
    Dim depthRGBBytes() As Byte
    Dim device As rs.Device
    Dim devices As rs.DeviceList
    Dim disparityBytes() As Byte
    Dim h As Int32
    Dim pipeline As New rs.Pipeline
    Dim pipeline_profile As rs.PipelineProfile
    Dim leftViewBytes() As Byte
    Dim rightViewBytes() As Byte
    Dim vertices() As Byte
    Dim w As Int32
    Public color As cv.Mat
    Public depth As cv.Mat
    Public depthRGB As cv.Mat
    Public disparity As cv.Mat
    Public deviceCount As Int32
    Public deviceName As String
    Public Extrinsics_VB As VB_Classes.ActiveClass.Extrinsics_VB
    Public failedImageCount As Int32
    Public imuAccel As cv.Point3f
    Public imuGyro As cv.Point3f
    Public IMUpresent As Boolean
    Public imuTimeStamp As Double
    Public intrinsics_VB As VB_Classes.ActiveClass.Intrinsics_VB
    Public modelInverse As Boolean
    Public pc As New rs.PointCloud
    Public pcMultiplier As Single = 1
    Public pointCloud As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public Sub New(fps As Int32, width As Int32, height As Int32)
        w = width
        h = height
        If OpenCVB.deviceSearch("T265") Then deviceCount = 1
        If deviceCount = 0 Then Return

        cfg.EnableStream(rs.Stream.Pose, rs.Format.SixDOF)
        cfg.EnableStream(rs.Stream.Fisheye, 1, rs.Format.Y8)
        cfg.EnableStream(rs.Stream.Fisheye, 2, rs.Format.Y8)

        pipeline_profile = pipeline.Start(cfg)
    End Sub
    Public Sub GetNextFrame()
        Dim frames = pipeline.WaitForFrames(1000)
        Dim f = frames.FirstOrDefault(rs.Stream.Pose)
        Dim pose_data = f.As(Of rs.PoseFrame).PoseData()
        Static framecount As Int32
        color = New cv.Mat(h, w, cv.MatType.CV_8UC3, framecount)
        depthRGB = New cv.Mat(h, w, cv.MatType.CV_8UC3, framecount)
        depth = New cv.Mat(h, w, cv.MatType.CV_16U, framecount)
        disparity = New cv.Mat(h, w, cv.MatType.CV_32F, 0)
        leftView = New cv.Mat(h, w, cv.MatType.CV_8U, framecount)
        rightView = New cv.Mat(h, w, cv.MatType.CV_8U, framecount)
        pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, framecount)
        framecount += 1
        If framecount = 256 Then framecount = 0

        Dim images = frames.As(Of rs.FrameSet)()
        Dim fishEyeLeft = images.FishEyeFrame
        Dim leftBytes(fishEyeLeft.Height * fishEyeLeft.Width) As Byte
        Marshal.Copy(fishEyeLeft.Data, leftBytes, 0, leftBytes.Length)

        Dim left = New cv.Mat(fishEyeLeft.Height, fishEyeLeft.Width, cv.MatType.CV_8U, leftBytes)
        cv.Cv2.ImShow("left", left)

        'If images Then
        '    Static test As Int32
        '    test += 1
        'End If
    End Sub
    Protected Overridable Sub Dispose(disposing As Boolean)
        pipeline.Stop()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
    End Sub
End Class
