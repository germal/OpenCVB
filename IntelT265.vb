Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp

Module SGBM_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function t265sgm_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub t265sgm_Close(bgfs As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function t265sgm_Run(tPtr As IntPtr, leftimg As IntPtr, rightimg As IntPtr, rows As Int32, cols As Int32, maxDisp As Int32) As IntPtr
    End Function
End Module

Structure T265IMUdata
    Public translation As cv.Point3f
    Public accelation As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAccelaration As cv.Point3f
    Public trackerConfidence As Int32
    Public mapperConfidence As Int32
End Structure
Public Class IntelT265
#Region "IntelT265Data"
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
    Dim pipeline As New rs.Pipeline
    Dim pipeline_profile As rs.PipelineProfile
    Dim stereo As cv.StereoSGBM
    Dim vertices() As Byte
    Dim w As Int32
    Public color As cv.Mat
    Public depth As cv.Mat
    Public depthRGB As New cv.Mat
    Public disparity As New cv.Mat
    Public deviceCount As Int32
    Public deviceName As String = "Intel T265"
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
    Dim rawSrc As cv.Rect
    Dim rawDst As cv.Rect
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
        pipelineClosed = False
        w = width
        h = height

        cfg.EnableStream(rs.Stream.Pose, rs.Format.SixDOF)
        cfg.EnableStream(rs.Stream.Fisheye, 1, rs.Format.Y8)
        cfg.EnableStream(rs.Stream.Fisheye, 2, rs.Format.Y8)

        pipeline_profile = pipeline.Start(cfg)

        numDisp = 112 - minDisp
        maxDisp = minDisp + numDisp
        tPtr = t265sgm_Open()
        stereo = cv.StereoSGBM.Create(minDisp, numDisp, 16, 8 * 3 * windowSize * windowSize,
                                      32 * 3 * windowSize * windowSize, 1, 0, 10, 100)

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
        ' matrices from the calibration And a desired height And field Of view      ' We calculate the undistorted focal length:
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
        'Qarray = {1, 0, 0, -(stereo_cx - maxDisp),
        '          0, 1, 0, -stereo_cy,
        '          0, 0, 0, stereo_focal_px,
        '          0, 0, -1 / extrinsics.translation(0), 0}

        Dim kMat As New cv.Mat(3, 3, cv.MatType.CV_64F, kLeft)
        Dim dMat As New cv.Mat(1, 4, cv.MatType.CV_64F, dLeft)
        Dim rMat As New cv.Mat(3, 3, cv.MatType.CV_64F, rLeft)
        Dim pMat As New cv.Mat(3, 4, cv.MatType.CV_64F, pLeft)
        cv.Cv2.FishEye.InitUndistortRectifyMap(kMat, dMat, rMat, pMat, stereo_size, cv.MatType.CV_32FC1, lm1, lm2)
        cv.Cv2.FishEye.InitUndistortRectifyMap(kMat, dMat, rMat, pMat, New cv.Size(w, h), cv.MatType.CV_32FC1, leftViewMap1, leftViewMap2)

        kMat = New cv.Mat(3, 3, cv.MatType.CV_64F, kRight)
        dMat = New cv.Mat(1, 4, cv.MatType.CV_64F, dRight)
        rMat = New cv.Mat(3, 3, cv.MatType.CV_64F, rRight)
        pMat = New cv.Mat(3, 4, cv.MatType.CV_64F, pRight)
        cv.Cv2.FishEye.InitUndistortRectifyMap(kMat, dMat, rMat, pMat, stereo_size, cv.MatType.CV_32FC1, rm1, rm2)
        cv.Cv2.FishEye.InitUndistortRectifyMap(kMat, dMat, rMat, pMat, New cv.Size(w, h), cv.MatType.CV_32FC1, rightViewMap1, rightViewMap2)
        leftView = New cv.Mat(h, w, cv.MatType.CV_8UC1, 0)
        rightView = New cv.Mat(h, w, cv.MatType.CV_8UC1, 0)
        rawSrc = New cv.Rect((rawWidth - rawWidth * h / rawHeight) / 2, 0, rawWidth * h / rawHeight, h)
        rawDst = New cv.Rect(0, 0, rawWidth * h / rawHeight, h)
        ReDim vertices(w * h * 4 * 3 - 1) ' 3 floats or 12 bytes per pixel.  

        IMUpresent = True
    End Sub
    Public Sub GetNextFrame()
#If DEBUG Then
        Static msgDelivered As Boolean
        If msgDelivered = False Then MsgBox("The T265 camera support is quite slow in Debug mode." + vbCrLf + "Use Release mode to get reasonable responsiveness.")
        msgDelivered = True
#End If
        Static leftBytes(rawHeight * rawWidth - 1) As Byte
        Static rightBytes(leftBytes.Length - 1) As Byte

        If pipelineClosed Then Exit Sub
        Dim frames = pipeline.WaitForFrames(1000)
        Dim f = frames.FirstOrDefault(rs.Stream.Pose)
        Dim pose_data = f.As(Of rs.PoseFrame).PoseData()
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

        getIntrinsics(leftStream, rightStream)

        Dim leftMat = New cv.Mat(fishEye.Height, fishEye.Width, cv.MatType.CV_8U, leftBytes)
        color = leftMat.Remap(leftViewMap1, leftViewMap2, cv.InterpolationFlags.Linear)
        color = color.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim rightMat = New cv.Mat(fishEye.Height, fishEye.Width, cv.MatType.CV_8U, rightBytes)
        depthRGB = color.Clone()

        Dim remapLeft = leftMat.Remap(lm1, lm2, cv.InterpolationFlags.Linear)
        Dim remapRight = rightMat.Remap(rm1, rm2, cv.InterpolationFlags.Linear)

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
        Dim tmpDepthRGB = New cv.Mat
        disp_vis = disp_vis.ConvertScaleAbs(1)
        cv.Cv2.ApplyColorMap(disp_vis, tmpDepthRGB, cv.ColormapTypes.Jet)
        Dim depthRect = New cv.Rect(CInt(stereo_cx / 2), 0, tmpDepthRGB.Width, tmpDepthRGB.Height)
        tmpDepthRGB.CopyTo(depthRGB(depthRect), mask)

        Dim tmp = New cv.Mat(h, w, cv.MatType.CV_8UC1)
        leftMat(rawSrc).CopyTo(leftView(rawDst))
        rightMat(rawSrc).CopyTo(rightView(rawDst))
        rightView(rawDst) = tmp(rawDst).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        depth = New cv.Mat(h, w, cv.MatType.CV_16U, 0)
        pointCloud = New cv.Mat(h, w, cv.MatType.CV_32FC3, vertices)

        Dim poseData = frames.FirstOrDefault(Of rs.Frame)(rs.Stream.Pose)
        Dim imuStruct = Marshal.PtrToStructure(Of T265IMUdata)(poseData.Data)
        imuGyro = imuStruct.translation
        imuGyro.Y *= -1
        imuGyro.Z *= -1
        imuAccel = imuStruct.angularAccelaration
        imuTimeStamp = poseData.Timestamp
    End Sub
    Public Sub closePipe()
        pipelineClosed = True
    End Sub
End Class
