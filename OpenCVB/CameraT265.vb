Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Imports System.Numerics
Module T265_Module
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265GetEpochTime(timeStamp As Double) As Single
    End Function
End Module

Public Class CameraT265
#Region "T265Data"
    Dim cfg As New rs.Config
    Dim dLeft(3) As Double
    Dim dRight(3) As Double
    Dim h As Int32
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
    Public IMU_Present As Boolean
    Public intrinsicsLeft_VB As VB_Classes.ActiveClass.intrinsics_VB
    Public intrinsicsRight_VB As VB_Classes.ActiveClass.intrinsics_VB
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
    Public IMU_Barometer As Single
    Public IMU_Magnetometer As cv.Point3f
    Public IMU_Temperature As Single
    Public IMU_TimeStamp As Double
    Public captureTimeStamp As Double
    Dim QArray(15) As Double

    Public transformationMatrix() As Single
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
    Public Sub New()
    End Sub
    Public Sub initialize(fps As Int32, width As Int32, height As Int32)
        w = width
        h = height

        cfg.EnableStream(rs.Stream.Pose, rs.Format.SixDOF)
        cfg.EnableStream(rs.Stream.Gyro)
        cfg.EnableStream(rs.Stream.Accel)
        cfg.EnableStream(rs.Stream.Fisheye, 1, rs.Format.Y8)
        cfg.EnableStream(rs.Stream.Fisheye, 2, rs.Format.Y8)

        pipeline_profile = pipeline.Start(cfg) ', AddressOf callback)

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

        IMU_Present = True
    End Sub
    Structure PoseData
        Public translation As cv.Point3f
        Public velocity As cv.Point3f
        Public acceleration As cv.Point3f
        Public rotation As Quaternion
        Public angularVelocity As cv.Point3f
        Public angularAcceleration As cv.Point3f
        Public trackerConfidence As Int32
        Public mapperConfidence As Int32
    End Structure
    Public Sub GetNextFrame()
        Static leftBytes(rawHeight * rawWidth - 1) As Byte
        Static rightBytes(leftBytes.Length - 1) As Byte

        If pipelineClosed Then Exit Sub
        Dim frames = pipeline.WaitForFrames(1000)
        Dim f = frames.FirstOrDefault(rs.Stream.Pose)

        Dim poseData = frames.FirstOrDefault(Of rs.Frame)(rs.Stream.Pose)
        Static startingTime = poseData.Timestamp
        IMU_TimeStamp = poseData.Timestamp - startingTime
        Dim pose = Marshal.PtrToStructure(Of PoseData)(poseData.Data)
        Dim q As Quaternion = pose.rotation
        Dim t = pose.translation
        '  Set the matrix as column-major for convenient work with OpenGL and rotate by 180 degress (by negating 1st and 3rd columns)
        Dim mat() As Single = {-(1 - 2 * q.Y * q.Y - 2 * q.Z * q.Z), -(2 * q.X * q.Y + 2 * q.Z * q.W), -(2 * q.X * q.Z - 2 * q.Y * q.W), 0.0,
                               2 * q.X * q.Y - 2 * q.Z * q.W, 1 - 2 * q.X * q.X - 2 * q.Z * q.Z, 2 * q.Y * q.Z + 2 * q.X * q.W, 0.0,
                               -(2 * q.X * q.Z + 2 * q.Y * q.W), -(2 * q.Y * q.Z - 2 * q.X * q.W), -(1 - 2 * q.X * q.X - 2 * q.Y * q.Y), 0.0,
                               t.X, t.Y, t.Z, 1.0}
        transformationMatrix = mat

        Dim gyroFrame = frames.FirstOrDefault(Of rs.Frame)(rs.Stream.Gyro, rs.Format.MotionXyz32f)
        imuGyro = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame.Data)

        Dim accelFrame = frames.FirstOrDefault(Of rs.Frame)(rs.Stream.Accel, rs.Format.MotionXyz32f)
        imuAccel = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame.Data)

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
        depth16 = New cv.Mat(h, w, cv.MatType.CV_16U, 0)
        disparity(validRect).ConvertTo(depth16(depthRect), cv.MatType.CV_16UC1)
        pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, vertices)
    End Sub
    'Private Sub callback(frame As rs.Frame)
    '    If pipelineClosed Then Exit Sub
    '    Static leftBytes(rawHeight * rawWidth - 1) As Byte
    '    Static rightBytes(leftBytes.Length - 1) As Byte
    '    Static poseFrameCount As Integer
    '    Static imageFrameCount As Integer
    '    Static gyroFrameCount As Integer
    '    Static AccelFrameCount As Integer
    '    Static myStopWatch As New System.Diagnostics.Stopwatch
    '    Static lastMs As Double
    '    Static initialized As Boolean

    '    If initialized = False Then
    '        initialized = True
    '        myStopWatch.Start()
    '    End If

    '    Dim ms = myStopWatch.ElapsedMilliseconds
    '    Dim f = frame.As(Of rs.Frame)

    '    Select Case f.Profile.Stream
    '        Case rs.Stream.Pose
    '            poseFrameCount += 1
    '            Dim poseFrame = f.As(Of rs.PoseFrame)
    '            Dim poseData = poseFrame(rs.Stream.Pose)
    '            If poseData Then
    '                Dim pose = Marshal.PtrToStructure(Of PoseData)(poseData)
    '                IMU_TimeStamp = poseFrame.Timestamp
    '                Dim q As Quaternion = pose.rotation
    '                Dim t = pose.translation
    '                '  Set the matrix as column-major for convenient work with OpenGL and rotate by 180 degress (by negating 1st and 3rd columns)
    '                Dim mat() As Single = {-(1 - 2 * q.Y * q.Y - 2 * q.Z * q.Z), -(2 * q.X * q.Y + 2 * q.Z * q.W), -(2 * q.X * q.Z - 2 * q.Y * q.W), 0.0,
    '                                       2 * q.X * q.Y - 2 * q.Z * q.W, 1 - 2 * q.X * q.X - 2 * q.Z * q.Z, 2 * q.Y * q.Z + 2 * q.X * q.W, 0.0,
    '                                       -(2 * q.X * q.Z + 2 * q.Y * q.W), -(2 * q.Y * q.Z - 2 * q.X * q.W), -(1 - 2 * q.X * q.X - 2 * q.Y * q.Y), 0.0,
    '                                       t.X, t.Y, t.Z, 1.0}
    '                transformationMatrix = mat
    '            End If
    '        Case rs.Stream.Gyro
    '            gyroFrameCount += 1
    '            'imuGyro = Marshal.PtrToStructure(Of cv.Point3f)(f(rs.Stream.Gyro))

    '        Case rs.Stream.Accel
    '            AccelFrameCount += 1
    '            ' imuAccel = Marshal.PtrToStructure(Of cv.Point3f)(f(rs.Stream.Accel))

    '        Case rs.Stream.Fisheye
    '            imageFrameCount += 1

    '            Console.WriteLine("index = " + CStr(f.Profile.Index))
    '            Marshal.Copy(f.Data, leftBytes, 0, leftBytes.Length)
    '            'For Each frame In images
    '            '    If frame.Profile.Stream = rs.Stream.Fisheye Then
    '            '        If frame.Profile.Index = 2 Then
    '            '            Marshal.Copy(frame.Data, rightBytes, 0, rightBytes.Length)
    '            '            Exit For
    '            '        End If
    '            '    End If
    '            'Next
    '            color = leftView.Remap(leftViewMap1, leftViewMap2, cv.InterpolationFlags.Linear).Resize(New cv.Size(w, h))
    '            color = color.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    '            RGBDepth = color.Clone()
    '    End Select

    '    If ms - lastMs > 1000 Then
    '        Console.WriteLine("p = " + CStr(poseFrameCount) + " gyro = " + CStr(gyroFrameCount) + " accel = " + CStr(AccelFrameCount) + " image = " + CStr(imageFrameCount))
    '        poseFrameCount = 0
    '        gyroFrameCount = 0
    '        AccelFrameCount = 0
    '        imageFrameCount = 0
    '        lastMs = ms
    '    End If

    '    Dim frames = frame.As(Of rs.Frame)
    '    leftView = New cv.Mat(720, 1280, cv.MatType.CV_8U, leftbytes)
    '    rightView = New cv.Mat(720, 1280, cv.MatType.CV_8U, 0)

    '    depth16 = New cv.Mat(720, 1280, cv.MatType.CV_16U, 0)
    'End Sub
    Public Sub closePipe()
        pipelineClosed = True
    End Sub
End Class
