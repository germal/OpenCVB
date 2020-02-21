Imports System.Windows.Controls
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp

Public Class StereoLabsZ3
#Region "ZedData"
    Dim cfg As New rs.Config
    Dim colorBytes() As Byte
    Dim device As rs.Device
    Dim devices As rs.DeviceList
    Dim h As Int32
    Dim kLeft(8) As Double
    Dim kRight(8) As Double
    Dim dLeft(3) As Double
    Dim dRight(3) As Double
    Dim rLeft(8) As Double
    Dim rRight(8) As Double
    Dim pLeft(8) As Double
    Dim pRight(8) As Double

    Public kMatleft As cv.Mat
    Public dMatleft As cv.Mat
    Public rMatleft As cv.Mat
    Public pMatleft As cv.Mat

    Public kMatRight As cv.Mat
    Public dMatRight As cv.Mat
    Public rMatRight As cv.Mat
    Public pMatRight As cv.Mat

    Dim pipeline As New rs.Pipeline
    Dim pipeline_profile As rs.PipelineProfile
    Dim stereo As cv.StereoSGBM
    Dim vertices() As Byte
    Dim w As Int32
    Public color As cv.Mat
    Public depth16 As cv.Mat
    Public RGBDepth As New cv.Mat
    Public disparity As New cv.Mat
    Public deviceCount As Int32
    Public deviceName As String = "Intel T265"
    Public Extrinsics_VB As VB_Classes.ActiveClass.Extrinsics_VB
    Public failedImageCount As Int32
    Public imuAccel As cv.Point3f
    Public imuGyro As cv.Point3f
    Public IMUpresent As Boolean
    Public imuTimeStamp As Double
    Public intrinsics_VB As VB_Classes.ActiveClass.intrinsics_VB
    Public modelInverse As Boolean
    Public pc As New rs.PointCloud
    Public pcMultiplier As Single = 1
    Public pointCloud As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public pipelineClosed As Boolean
    Dim stereo_fov_rad As Double
    Dim stereo_height_px As Double
    Dim stereo_focal_px As Double
    Dim stereo_width_px As Double
    Dim stereo_size As cv.Size
    Dim stereo_cx As Double
    Dim stereo_cy As Double
    Dim Qarray(15) As Double
    Dim lm1 As New cv.Mat
    Dim lm2 As New cv.Mat
    Dim rm1 As New cv.Mat
    Dim rm2 As New cv.Mat
    Dim leftViewMap1 As New cv.Mat
    Dim leftViewMap2 As New cv.Mat
    Dim rightViewMap1 As New cv.Mat
    Dim rightViewMap2 As New cv.Mat
    Dim minDisp = 0
    Dim windowSize = 5
    Dim numDisp As Int32
    Dim maxDisp As Int32
    Dim validRect As cv.Rect
    Dim tPtr As IntPtr
    Dim rawWidth As Int32
    Dim rawHeight As Int32
    Dim intrinsicsLeft As rs.Intrinsics
    Dim intrinsicsRight As rs.Intrinsics
    Dim leftStream As rs.VideoStreamProfile
    Dim rightStream As rs.VideoStreamProfile
#End Region
    Private Sub getIntrinsics(leftStream As rs.VideoStreamProfile, rightStream As rs.VideoStreamProfile)
        intrinsicsLeft = leftStream.GetIntrinsics()
        intrinsicsRight = rightStream.GetIntrinsics()
        intrinsics_VB.width = intrinsicsLeft.width
        intrinsics_VB.height = intrinsicsLeft.height
        intrinsics_VB.ppx = intrinsicsLeft.ppx
        intrinsics_VB.ppy = intrinsicsLeft.ppy
        intrinsics_VB.fx = intrinsicsLeft.fx
        intrinsics_VB.fy = intrinsicsLeft.fy
        intrinsics_VB.FOV = intrinsicsLeft.FOV
        intrinsics_VB.coeffs = intrinsicsLeft.coeffs
    End Sub
    Public Sub New()
    End Sub
    Public Sub initialize(fps As Int32, width As Int32, height As Int32)

    End Sub
    Public Sub GetNextFrame()
        Static leftBytes(rawHeight * rawWidth - 1) As Byte
        Static rightBytes(leftBytes.Length - 1) As Byte

        If pipelineClosed Then Exit Sub
        Dim frames = pipeline.WaitForFrames(1000)
        Dim f = frames.FirstOrDefault(rs.Stream.Pose)


        'Dim poseData = frames.FirstOrDefault(Of rs.Frame)(rs.Stream.Pose)
        'Dim imuStruct = Marshal.PtrToStructure(Of T265IMUdata)(poseData.Data)
        'Console.WriteLine("len = " + CStr(Marshal.SizeOf(imuStruct)))
        'imuGyro = imuStruct.translation
        'imuGyro.Y *= -1
        'imuGyro.Z *= -1
        'imuAccel = imuStruct.acceleration
        'imuTimeStamp = poseData.Timestamp
        'Console.WriteLine("translation xyz " + CStr(imuStruct.translation.X) + vbTab + CStr(imuStruct.translation.Y) + vbTab + CStr(imuStruct.translation.Z))
        'Console.WriteLine("acceleration xyz " + CStr(imuStruct.acceleration.X) + vbTab + CStr(imuStruct.acceleration.Y) + vbTab + CStr(imuStruct.acceleration.Z))
        'Console.WriteLine("velocity xyz " + CStr(imuStruct.velocity.X) + vbTab + CStr(imuStruct.velocity.Y) + vbTab + CStr(imuStruct.velocity.Z))
        'Console.WriteLine("rotation xyz " + CStr(imuStruct.rotation.X) + vbTab + CStr(imuStruct.rotation.Y) + vbTab + CStr(imuStruct.rotation.Z))
        'Console.WriteLine("angularVelocity xyz " + CStr(imuStruct.angularVelocity.X) + vbTab + CStr(imuStruct.angularVelocity.Y) + vbTab + CStr(imuStruct.angularVelocity.Z))
        'Console.WriteLine("angularAcceleration xyz " + CStr(imuStruct.angularAcceleration.X) + vbTab + CStr(imuStruct.angularAcceleration.Y) + vbTab + CStr(imuStruct.angularAcceleration.Z))
        'Console.WriteLine("trackerConfidence " + CStr(imuStruct.trackerConfidence))
        'Console.WriteLine("mapperConfidence " + CStr(imuStruct.mapperConfidence))


        Dim images = frames.As(Of rs.FrameSet)()
        Dim fishEye = images.FishEyeFrame()
        Marshal.Copy(fishEye.Data, leftBytes, 0, leftBytes.Length)
        For Each frame In images
            If frame.Profile.Stream = rs.Stream.Fisheye Then
                If frame.Profile.Index = 2 Then
                    Marshal.Copy(frame.Data, rightBytes, 0, rightBytes.Length)
                    Exit For
                End If
            End If
        Next

        leftView = New cv.Mat(fishEye.Height, fishEye.Width, cv.MatType.CV_8U, leftBytes)
        color = leftView.Remap(leftViewMap1, leftViewMap2, cv.InterpolationFlags.Linear).Resize(New cv.Size(w, h))
        color = color.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        rightView = New cv.Mat(fishEye.Height, fishEye.Width, cv.MatType.CV_8U, rightBytes)
        RGBDepth = color.Clone()

        Dim remapLeft = leftView.Remap(lm1, lm2, cv.InterpolationFlags.Linear)
        Dim remapRight = rightView.Remap(rm1, rm2, cv.InterpolationFlags.Linear)

        '#Const USE_FAILING_COMPUTE_API = 1
#If USE_FAILING_COMPUTE_API Then
        stereo.Compute(remapLeft, remapRight, disparity) ' Works but doesn't produce the correct results.  C++ version produces correct results.
        ' re-crop just the valid part of the disparity
        validRect = New cv.Rect(maxDisp, 0, disparity.Cols - maxDisp, disparity.Rows)
        disparity.ConvertTo(disparity, cv.MatType.CV_32F, CDbl(1 / 16))
        Dim disp_vis = disparity.Clone()
        disp_vis = disp_vis(validRect)
#Else
        Dim leftData(remapLeft.Total * remapLeft.ElemSize - 1) As Byte
        Marshal.Copy(remapLeft.Data, leftData, 0, leftData.Length - 1)
        Dim handleLeft = GCHandle.Alloc(leftData, GCHandleType.Pinned)

        Dim rightData(remapRight.Total * remapRight.ElemSize - 1) As Byte
        Marshal.Copy(remapRight.Data, rightData, 0, rightData.Length - 1)
        Dim handleRight = GCHandle.Alloc(rightData, GCHandleType.Pinned)

        Dim dispPtr = t265sgm_Run(tPtr, handleLeft.AddrOfPinnedObject(), handleRight.AddrOfPinnedObject(), stereo_size.Height, stereo_size.Width, maxDisp)
        handleLeft.Free()
        handleRight.Free()

        Static dstData((remapLeft.Cols - maxDisp) * remapLeft.Rows * 4 - 1) As Byte ' 32-bit float array coming back from C++
        Marshal.Copy(dispPtr, dstData, 0, dstData.Length)
        disparity = New cv.Mat(remapLeft.Cols - maxDisp, remapLeft.Rows, cv.MatType.CV_32F, dstData)
        Dim disp_vis = disparity.Clone()
#End If
        Dim mask = disp_vis.Threshold(1, 255, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        disp_vis *= CDbl(255 / numDisp)

        ' convert disparity To 0-255 And color it
        Dim tmpRGBDepth = New cv.Mat
        disp_vis = disp_vis.ConvertScaleAbs(1)
        cv.Cv2.ApplyColorMap(disp_vis, tmpRGBDepth, cv.ColormapTypes.Jet)
        Dim depthRect = New cv.Rect(stereo_cx, 0, tmpRGBDepth.Width, tmpRGBDepth.Height)
        tmpRGBDepth.CopyTo(RGBDepth(depthRect), mask)

        depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, 0)
        pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, vertices)
    End Sub
    Public Sub closePipe()
        pipelineClosed = True
    End Sub
End Class
