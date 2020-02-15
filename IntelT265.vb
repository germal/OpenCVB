Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Structure T265IMUdataOld
    Public translation As cv.Point3f
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
    Public trackerConfidence As Int32
    Public mapperConfidence As Int32
End Structure
Public Class IntelT265
#Region "T265Data"
    Dim cfg As New rs.Config
    Dim dLeft(3) As Double
    Dim dRight(3) As Double
    Dim h As Int32
    Dim intrinsicsLeft As rs.Intrinsics
    Dim intrinsicsRight As rs.Intrinsics
    Dim kLeft(8) As Double
    Dim kRight(8) As Double
    Dim leftStream As rs.VideoStreamProfile
    Dim leftViewMap1 As New cv.Mat
    Dim leftViewMap2 As New cv.Mat
    Dim lm1 As New cv.Mat
    Dim lm2 As New cv.Mat
    Dim maxDisp As Int32
    Dim minDisp = 0
    Dim numDisp As Int32
    Dim pipeline As New rs.Pipeline
    Dim pipeline_profile As rs.PipelineProfile
    Dim pLeft(11) As Double
    Dim pRight(11) As Double
    Dim rawHeight As Int32
    Dim rawWidth As Int32
    Dim rightStream As rs.VideoStreamProfile
    Dim rightViewMap1 As New cv.Mat
    Dim rightViewMap2 As New cv.Mat
    Dim rLeft(8) As Double
    Dim rm1 As New cv.Mat
    Dim rm2 As New cv.Mat
    Dim rRight(8) As Double
    Dim stereo As cv.StereoSGBM
    Dim stereo_cx As Double
    Dim stereo_cy As Double
    Dim stereo_focal_px As Double
    Dim stereo_fov_rad As Double
    Dim stereo_height_px As Double
    Dim stereo_size As cv.Size
    Dim stereo_width_px As Double
    Dim tPtr As IntPtr
    Dim validRect As cv.Rect
    Dim vertices() As Byte
    Dim w As Int32
    Public color As cv.Mat
    Public depth16 As cv.Mat
    Public deviceCount As Int32
    Public deviceName As String = "Intel T265"
    Public disparity As New cv.Mat
    Public Extrinsics_VB As VB_Classes.ActiveClass.Extrinsics_VB
    Public failedImageCount As Int32
    Public imuAccel As cv.Point3f
    Public imuGyro As cv.Point3f
    Public IMUpresent As Boolean
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
    Dim QArray(15) As Double
#End Region
    Private Sub getIntrinsics(leftStream As rs.VideoStreamProfile, rightStream As rs.VideoStreamProfile)
        intrinsicsLeft = leftStream.GetIntrinsics()
        intrinsicsRight = rightStream.GetIntrinsics()
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
    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        w = width
        h = height

        cfg.EnableStream(rs.Stream.Pose, rs.Format.SixDOF)
        cfg.EnableStream(rs.Stream.Fisheye, 1, rs.Format.Y8)
        cfg.EnableStream(rs.Stream.Fisheye, 2, rs.Format.Y8)

        pipeline_profile = pipeline.Start(cfg)

        numDisp = 112 - minDisp
        maxDisp = minDisp + numDisp
        Dim windowSize = 5
        stereo = cv.StereoSGBM.Create(minDisp, numDisp, 16, 8 * 3 * windowSize * windowSize, 32 * 3 * windowSize * windowSize, 1, 0, 10, 100, 32)

        leftStream = pipeline_profile.GetStream(Of rs.VideoStreamProfile)(rs.Stream.Fisheye, 1)
        rightStream = pipeline_profile.GetStream(Of rs.VideoStreamProfile)(rs.Stream.Fisheye, 2)

        ' Get the relative extrinsics between the left And right camera
        Dim extrinsics = leftStream.GetExtrinsicsTo(rightStream)
        Extrinsics_VB.rotation = extrinsics.rotation
        Extrinsics_VB.translation = extrinsics.translation

        getIntrinsics(leftStream, rightStream)

        rawWidth = intrinsicsLeft.width
        rawHeight = intrinsicsLeft.height

        kLeft = {intrinsicsLeft.fx, 0, intrinsicsLeft.ppx, 0, intrinsicsLeft.fy, intrinsicsLeft.ppy, 0, 0, 1}
        dLeft = {intrinsicsLeft.coeffs(0), intrinsicsLeft.coeffs(1), intrinsicsLeft.coeffs(2), intrinsicsLeft.coeffs(3)}

        kRight = {intrinsicsRight.fx, 0, intrinsicsRight.ppx, 0, intrinsicsRight.fy, intrinsicsRight.ppy, 0, 0, 1}
        dRight = {intrinsicsRight.coeffs(0), intrinsicsRight.coeffs(1), intrinsicsRight.coeffs(2), intrinsicsRight.coeffs(3)}

        ' We need To determine what focal length our undistorted images should have
        ' In order To Set up the camera matrices For initUndistortRectifyMap.  We
        ' could use stereoRectify, but here we show how To derive these projection
        ' matrices from the calibration And a desired height And field Of view      
        ' We calculate the undistorted focal length:
        '
        '         h
        ' -----------------
        '  \      |      /
        '    \    | f  /
        '     \   |   /
        '      \ fov /
        '        \|/
        stereo_fov_rad = CDbl(90 * (Math.PI / 180))  ' 90 degree desired fov
        stereo_height_px = 300            ' 300x300 pixel stereo output
        stereo_focal_px = CDbl(stereo_height_px / 2 / Math.Tan(stereo_fov_rad / 2))

        ' We Set the left rotation To identity And the right rotation
        ' the rotation between the cameras
        rLeft = {1, 0, 0, 0, 1, 0, 0, 0, 1}
        Dim r = extrinsics.rotation
        rRight = {r(0), r(1), r(2), r(3), r(4), r(5), r(6), r(7), r(8)}
        ' The stereo algorithm needs max_disp extra pixels In order To produce valid
        ' disparity On the desired output region. This changes the width, but the
        ' center Of projection should be On the center Of the cropped image
        stereo_width_px = stereo_height_px + maxDisp
        stereo_size = New cv.Size(stereo_width_px, stereo_height_px)
        stereo_cx = (stereo_height_px - 1) / 2 + maxDisp
        stereo_cy = (stereo_height_px - 1) / 2

        ' Construct the left And right projection matrices, the only difference Is
        ' that the right projection matrix should have a shift along the x axis Of
        ' baseline*focal_length
        pLeft = {stereo_focal_px, 0, stereo_cx, 0, 0, stereo_focal_px, stereo_cy, 0, 0, 0, 1, 0}
        pRight = pLeft
        pRight(3) = extrinsics.translation(0) * stereo_focal_px

        ' Construct Q For use With cv2.reprojectImageTo3D. Subtract maxDisp from x
        ' since we will crop the disparity later
        Qarray = {1, 0, 0, -(stereo_cx - maxDisp),
                  0, 1, 0, -stereo_cy,
                  0, 0, 0, stereo_focal_px,
                  0, 0, -1 / extrinsics.translation(0), 0}

        kMatleft = New cv.Mat(3, 3, cv.MatType.CV_64F, kLeft)
        dMatleft = New cv.Mat(1, 4, cv.MatType.CV_64F, dLeft)
        rMatleft = New cv.Mat(3, 3, cv.MatType.CV_64F, rLeft)
        pMatleft = New cv.Mat(3, 4, cv.MatType.CV_64F, pLeft)
        cv.Cv2.FishEye.InitUndistortRectifyMap(kMatleft, dMatleft, rMatleft, pMatleft, stereo_size,
                                               cv.MatType.CV_32FC1, lm1, lm2)
        cv.Cv2.FishEye.InitUndistortRectifyMap(kMatleft, dMatleft, rMatleft, pMatleft, New cv.Size(rawWidth, rawHeight),
                                               cv.MatType.CV_32FC1, leftViewMap1, leftViewMap2)

        kMatRight = New cv.Mat(3, 3, cv.MatType.CV_64F, kRight)
        dMatRight = New cv.Mat(1, 4, cv.MatType.CV_64F, dRight)
        rMatRight = New cv.Mat(3, 3, cv.MatType.CV_64F, rRight)
        pMatRight = New cv.Mat(3, 4, cv.MatType.CV_64F, pRight)
        cv.Cv2.FishEye.InitUndistortRectifyMap(kMatRight, dMatRight, rMatRight, pMatRight, stereo_size,
                                               cv.MatType.CV_32FC1, rm1, rm2)
        cv.Cv2.FishEye.InitUndistortRectifyMap(kMatRight, dMatRight, rMatRight, pMatRight, New cv.Size(rawWidth, rawHeight),
                                               cv.MatType.CV_32FC1, rightViewMap1, rightViewMap2)
        leftView = New cv.Mat(h, w, cv.MatType.CV_8UC1, 0)
        rightView = New cv.Mat(h, w, cv.MatType.CV_8UC1, 0)
        ReDim vertices(w * h * 4 * 3 - 1) ' 3 floats or 12 bytes per pixel.  

        IMUpresent = True
    End Sub
    Public Sub GetNextFrame()
        Static leftBytes(rawHeight * rawWidth - 1) As Byte
        Static rightBytes(leftBytes.Length - 1) As Byte

        If pipelineClosed Then Exit Sub
        Dim frames = pipeline.WaitForFrames(1000)
        Dim f = frames.FirstOrDefault(rs.Stream.Pose)

        Dim poseData = frames.FirstOrDefault(Of rs.Frame)(rs.Stream.Pose)
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

        stereo.Compute(remapLeft, remapRight, disparity) ' Works but doesn't produce the correct results.  C++ version produces correct results.
        ' re-crop just the valid part of the disparity
        validRect = New cv.Rect(maxDisp, 0, disparity.Cols - maxDisp, disparity.Rows)
        Dim disp_vis As New cv.Mat, tmpdisp As New cv.Mat
        disparity.ConvertTo(disp_vis, cv.MatType.CV_32F, CDbl(1 / 16))
        disparity.ConvertTo(tmpdisp, cv.MatType.CV_32F, CDbl(1 / 16))
        disp_vis = disp_vis(validRect)

        Dim mask = disp_vis.Threshold(1, 255, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        disp_vis *= CDbl(255 / numDisp)

        ' convert disparity To 0-255 And color it
        Dim tmpRGBDepth = New cv.Mat
        disp_vis = disp_vis.ConvertScaleAbs(1)
        cv.Cv2.ApplyColorMap(disp_vis, tmpRGBDepth, cv.ColormapTypes.Jet)
        Dim depthRect = New cv.Rect(stereo_cx, 0, tmpRGBDepth.Width, tmpRGBDepth.Height)
        tmpRGBDepth.CopyTo(RGBDepth(depthRect), mask)

        'Dim maxVal As Double, minVal As Double
        'tmpdisp.MinMaxIdx(minVal, maxVal)

        depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, 0)
        'For y = depthRect.Y To depthRect.Y + depthRect.Height - 1
        '    For x = depthRect.X To depthRect.Width - 1
        '        Dim nextVal = disparity.At(Of UShort)(y - validRect.Y, x - validRect.X) + 1
        '        If nextVal > 0 And nextVal < 65000 Then depth16.Set(Of UShort)(y, x, 1000 - nextVal)
        '    Next
        'Next
        disparity(validRect).ConvertTo(depth16(depthRect), cv.MatType.CV_16UC1)
        pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, vertices)
    End Sub
    Public Sub closePipe()
        pipelineClosed = True
    End Sub
End Class
