Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Module T265_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265timeStampLatency(timeStamp As Double) As Single
    End Function
End Module

Public Class CameraT265
    Inherits Camera
#Region "T265Data"
    Dim cfg As New rs.Config
    Dim dLeft(3) As Double
    Dim dRight(3) As Double
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
    Dim stereo_cx As Double
    Dim stereo_cy As Double
    Dim stereo_focal_px As Double
    Dim stereo_fov_rad As Double
    Dim stereo_height_px As Double
    Dim stereo_size As cv.Size
    Dim stereo_width_px As Double
    Dim tPtr As IntPtr
    Public pc As New rs.PointCloud

    Public dMatleft As cv.Mat
    Public dMatRight As cv.Mat
    Public kMatleft As cv.Mat
    Public kMatRight As cv.Mat
    Public pMatleft As cv.Mat
    Public pMatRight As cv.Mat
    Public rMatleft As cv.Mat
    Public rMatRight As cv.Mat
    Public captureTimeStamp As Double
    Dim QArray(15) As Double
#End Region
    Private Sub getIntrinsics(ByRef vb_intrin As VB_Classes.ActiveClass.intrinsics_VB, intrinsics As rs.Intrinsics)
        vb_intrin.width = intrinsics.width
        vb_intrin.height = intrinsics.height
        vb_intrin.ppx = intrinsics.ppx
        vb_intrin.ppy = intrinsics.ppy
        vb_intrin.fx = intrinsics.fx
        vb_intrin.fy = intrinsics.fy
        vb_intrin.FOV = intrinsics.FOV
        vb_intrin.coeffs = intrinsics.coeffs
    End Sub
    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        deviceName = "Intel T265"
        IMU_Present = True
        w = width
        h = height

        cfg.EnableStream(rs.Stream.Pose, rs.Format.SixDOF)
        cfg.EnableStream(rs.Stream.Gyro)
        cfg.EnableStream(rs.Stream.Accel)
        cfg.EnableStream(rs.Stream.Fisheye, 1, rs.Format.Y8)
        cfg.EnableStream(rs.Stream.Fisheye, 2, rs.Format.Y8)

        pipeline_profile = pipeline.Start(cfg)

        numDisp = 112 - minDisp
        maxDisp = minDisp + numDisp

        leftStream = pipeline_profile.GetStream(Of rs.VideoStreamProfile)(rs.Stream.Fisheye, 1)
        rightStream = pipeline_profile.GetStream(Of rs.VideoStreamProfile)(rs.Stream.Fisheye, 2)

        ' Get the relative extrinsics between the left And right camera
        Dim extrinsics = leftStream.GetExtrinsicsTo(rightStream)
        Extrinsics_VB.rotation = extrinsics.rotation
        Extrinsics_VB.translation = extrinsics.translation

        Dim intrinsics = leftStream.GetIntrinsics()
        getIntrinsics(intrinsicsLeft_VB, intrinsics)
        rawWidth = intrinsics.width
        rawHeight = intrinsics.height
        kLeft = {intrinsics.fx, 0, intrinsics.ppx, 0, intrinsics.fy, intrinsics.ppy, 0, 0, 1}
        dLeft = {intrinsics.coeffs(0), intrinsics.coeffs(1), intrinsics.coeffs(2), intrinsics.coeffs(3)}

        intrinsics = rightStream.GetIntrinsics()
        getIntrinsics(intrinsicsRight_VB, intrinsics)
        kRight = {intrinsics.fx, 0, intrinsics.ppx, 0, intrinsics.fy, intrinsics.ppy, 0, 0, 1}
        dRight = {intrinsics.coeffs(0), intrinsics.coeffs(1), intrinsics.coeffs(2), intrinsics.coeffs(3)}

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
        QArray = {1, 0, 0, -(stereo_cx - maxDisp),
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
    End Sub
    Public Sub GetNextFrame()
        If pipelineClosed Then Exit Sub
        Dim frameset = pipeline.WaitForFrames(1000)
        For i = 0 To 3
            Dim stream = Choose(i + 1, rs.Stream.Pose, rs.Stream.Gyro, rs.Stream.Accel, rs.Stream.Fisheye)
            Dim f = frameset.FirstOrDefault(stream)
            getFrames(f, frameset)
        Next
    End Sub
    Private Sub getFrames(frame As rs.Frame, frameset As rs.FrameSet)
        Static imageCounter As Integer
        Static totalMS As Double

        Select Case frame.Profile.Stream
            Case rs.Stream.Pose
                Dim poseFrame = frame.As(Of rs.PoseFrame)
                Dim poseData = poseFrame.PoseData
                SyncLock OpenCVB.camPic ' only really need the synclock when in callback mode but it doesn't hurt to waitforframe mode.
                    IMU_TimeStamp = poseFrame.Timestamp
                    Static lastFrameTime = IMU_TimeStamp
                    IMU_FrameTime = IMU_TimeStamp - lastFrameTime
                    lastFrameTime = IMU_TimeStamp
                    totalMS += IMU_FrameTime
                    Dim pose = Marshal.PtrToStructure(Of PoseData)(poseFrame.Data)
                    IMU_Rotation = pose.rotation
                    Dim q = IMU_Rotation
                    IMU_Translation = pose.translation
                    IMU_Acceleration = pose.acceleration
                    IMU_Velocity = pose.velocity
                    IMU_AngularAcceleration = pose.angularAcceleration
                    IMU_AngularVelocity = pose.angularVelocity
                    IMU_LatencyMS = T265timeStampLatency(IMU_TimeStamp)
                    Dim t = IMU_Translation
                    '  Set the matrix as column-major for convenient work with OpenGL and rotate by 180 degress (by negating 1st and 3rd columns)
                    Dim mat() As Single = {
                        -(1 - 2 * q.Y * q.Y - 2 * q.Z * q.Z), -(2 * q.X * q.Y + 2 * q.Z * q.W), -(2 * q.X * q.Z - 2 * q.Y * q.W), 0.0,
                        2 * q.X * q.Y - 2 * q.Z * q.W, 1 - 2 * q.X * q.X - 2 * q.Z * q.Z, 2 * q.Y * q.Z + 2 * q.X * q.W, 0.0,
                        -(2 * q.X * q.Z + 2 * q.Y * q.W), -(2 * q.Y * q.Z - 2 * q.X * q.W), -(1 - 2 * q.X * q.X - 2 * q.Y * q.Y), 0.0,
                        t.X, t.Y, t.Z, 1.0}
                    transformationMatrix = mat
                End SyncLock
            Case rs.Stream.Gyro
                imuGyro = Marshal.PtrToStructure(Of cv.Point3f)(frame.Data)

            Case rs.Stream.Accel
                imuAccel = Marshal.PtrToStructure(Of cv.Point3f)(frame.Data)

            Case rs.Stream.Fisheye
                frameCount += 1
                imageCounter += 1
                If frameset Is Nothing Then frameset = frame.As(Of rs.FrameSet)
                Dim firstFrame = frameset.FirstOrDefault(rs.Stream.Fisheye)
                Static leftViewBytes(rawHeight * rawWidth - 1) As Byte
                Static rightViewBytes(rawHeight * rawWidth - 1) As Byte
                Marshal.Copy(firstFrame.Data, leftViewBytes, 0, leftViewBytes.Length)
                For Each fr In frameset
                    If fr.Profile.Stream = rs.Stream.Fisheye Then
                        If fr.Profile.Index = 2 Then
                            Marshal.Copy(fr.Data, rightViewBytes, 0, rightViewBytes.Length)
                            Exit For
                        End If
                    End If
                Next
                SyncLock OpenCVB.camPic ' only really need the synclock when in callback mode but it doesn't hurt to waitforframe mode.
                    leftView = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_8U, leftViewBytes)
                    rightView = New cv.Mat(rawHeight, rawWidth, cv.MatType.CV_8U, rightViewBytes)
                    Dim tmpColor = leftView.Remap(leftViewMap1, leftViewMap2, cv.InterpolationFlags.Linear).Resize(New cv.Size(w, h))
                    color = tmpColor.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

                    Dim remapLeft = leftView.Remap(lm1, lm2, cv.InterpolationFlags.Linear)
                    Dim remapRight = rightView.Remap(rm1, rm2, cv.InterpolationFlags.Linear)

                    disparity = New cv.Mat(remapLeft.Size(), cv.MatType.CV_16SC1)
                    Dim windowSize = 5
                    Dim stereo = cv.StereoSGBM.Create(minDisp, numDisp, 16, 8 * 3 * windowSize * windowSize, 32 * 3 * windowSize * windowSize, 1, 0, 10, 100, 32)
                    stereo.Compute(remapLeft, remapRight, disparity)

                    ' re-crop just the valid part of the disparity
                    Dim validRect = New cv.Rect(maxDisp, 0, disparity.Cols - maxDisp, disparity.Rows)
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
                    RGBDepth = color.Clone()
                    tmpRGBDepth.CopyTo(RGBDepth(depthRect), mask)
                    depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, 0)
                    disparity(validRect).ConvertTo(depth16(depthRect), cv.MatType.CV_16UC1)
                    pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, vertices)
                    newImagesAvailable += 1
                End SyncLock
        End Select

        If totalMS > 1000 Then
            'Console.WriteLine("image = " + CStr(imageCounter) + " internal camera FPS.")
            imageCounter = 0
            totalMS = 0
        End If
    End Sub
End Class
