Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module undistort_Mats
    Public Sub undistortSetup(ocvb As AlgorithmData, ByRef kMatLeft As cv.Mat, ByRef dMatLeft As cv.Mat, ByRef rMatLeft As cv.Mat, ByRef pMatLeft As cv.Mat,
                       maxDisp As Int32, stereo_height_px As Int32, intrinsics As ActiveClass.intrinsics_VB)
        Dim kLeft(8) As Double
        Dim rLeft(8) As Double
        Dim dLeft(4) As Double
        Dim pLeft(11) As Double

        kLeft = {intrinsics.fx, 0, intrinsics.ppx, 0,
                 intrinsics.fy, intrinsics.ppy, 0, 0, 1}
        dLeft = {intrinsics.coeffs(0), intrinsics.coeffs(1),
                 intrinsics.coeffs(2), intrinsics.coeffs(3)}

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
        Dim stereo_fov_rad = CDbl(90 * (Math.PI / 180))  ' 90 degree desired fov
        Dim stereo_focal_px = CDbl(stereo_height_px / 2 / Math.Tan(stereo_fov_rad / 2))

        ' The stereo algorithm needs max_disp extra pixels In order To produce valid
        ' disparity On the desired output region. This changes the width, but the
        ' center Of projection should be On the center Of the cropped image
        Dim stereo_width_px = stereo_height_px + maxDisp
        Dim stereo_cx = (stereo_height_px - 1) / 2 + maxDisp
        Dim stereo_cy = (stereo_height_px - 1) / 2

        ' Construct the left And right projection matrices, the only difference Is
        ' that the right projection matrix should have a shift along the x axis Of
        ' baseline*focal_length
        pLeft = {stereo_focal_px, 0, stereo_cx, 0, 0, stereo_focal_px, stereo_cy, 0, 0, 0, 1, 0}

        kMatLeft = New cv.Mat(3, 3, cv.MatType.CV_64F, kLeft)
        dMatLeft = New cv.Mat(1, 4, cv.MatType.CV_64F, dLeft)

        rMatLeft = cv.Mat.Eye(3, 3, cv.MatType.CV_64F).ToMat() ' We Set the left rotation to identity 
        pMatLeft = New cv.Mat(3, 4, cv.MatType.CV_64F, pLeft)
    End Sub
End Module







' https://stackoverflow.com/questions/26602981/correct-barrel-distortion-in-opencv-manually-without-chessboard-image
Public Class Undistort_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim check As New OptionsCheckbox
    Dim leftViewMap1 As New cv.Mat
    Dim leftViewMap2 As New cv.Mat
    Dim saveK As Int32, saveD As Int32, saveR As Int32, saveP As Int32
    Dim maxDisp As Int32
    Dim stereo_cx As Int32
    Dim stereo_cy As Int32
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "undistort intrinsics Left", 1, 200, 100)

        If ocvb.parms.cameraIndex = T265Camera Then
            sliders.setupTrackBar2(ocvb, "undistort intrinsics coeff's", -100000, 100000, 100)
        Else
            sliders.setupTrackBar2(ocvb, "undistort intrinsics coeff's", -1000, 1000, 100)
        End If
        sliders.setupTrackBar3(ocvb, "undistort stereo height", 1, ocvb.color.Height, ocvb.color.Height)
        sliders.setupTrackBar4(ocvb, "undistort Offset left/right", 1, 200, 112)
        If ocvb.parms.ShowOptions Then sliders.Show()

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Restore Original matrices"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        ocvb.label1 = "Left Image with sliders applied"
        ocvb.desc = "Use sliders to control the undistort OpenCV API"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static kMatLeft As cv.Mat, dMatLeft As cv.Mat, rMatLeft As cv.Mat, pMatLeft As cv.Mat
        Static kMat As cv.Mat, dMat As cv.Mat
        Dim rawWidth = ocvb.leftView.Width
        Dim rawHeight = ocvb.leftView.Height
        If check.Box(0).Checked Then
            check.Box(0).Checked = False

            sliders.TrackBar1.Value = 100
            sliders.TrackBar2.Value = 100

            maxDisp = sliders.TrackBar4.Value
            Dim stereo_height_px = sliders.TrackBar3.Value
            undistortSetup(ocvb, kMatLeft, dMatLeft, rMatLeft, pMatLeft, maxDisp, stereo_height_px, ocvb.parms.intrinsicsLeft)

            ' the intrinsic coeff's on the Intel D400 series are always zero.  Here we just make up some numbers so we can show the impact.
            If ocvb.parms.cameraIndex = D400Cam Then
                Dim d() As Double = {0.5, -2, 1.5, 0.5}
                dMatLeft = New cv.Mat(1, 4, cv.MatType.CV_64F, d)
            End If
        End If
        If saveK <> sliders.TrackBar1.Value Then
            saveK = sliders.TrackBar1.Value
            kMat = kMatLeft * sliders.TrackBar1.Value / 100
        End If
        If saveD <> sliders.TrackBar2.Value Then
            saveD = sliders.TrackBar2.Value
            dMat = dMatLeft * sliders.TrackBar2.Value / 100
        End If
        If saveP <> sliders.TrackBar4.Value Or saveR <> sliders.TrackBar3.Value Then
            saveP = sliders.TrackBar4.Value
            maxDisp = saveP
            saveR = sliders.TrackBar3.Value
            Dim stereo_height_px = saveR ' heightXheight pixel stereo output
            Dim stereo_fov_rad = CDbl(90 * (Math.PI / 180))  ' 90 degree desired fov
            Dim stereo_focal_px = CDbl(stereo_height_px / 2 / Math.Tan(stereo_fov_rad / 2))
            stereo_cx = (stereo_height_px - 1) / 2 + maxDisp
            stereo_cy = (stereo_height_px - 1) / 2
            Dim pLeft = {stereo_focal_px, 0, stereo_cx, 0, 0, stereo_focal_px, stereo_cy, 0, 0, 0, 1, 0}
            pMatLeft = New cv.Mat(3, 4, cv.MatType.CV_64F, pLeft)
        End If

        cv.Cv2.FishEye.InitUndistortRectifyMap(kMat, dMat, rMatLeft, pMatLeft, New cv.Size(rawWidth, rawHeight),
                                               cv.MatType.CV_32FC1, leftViewMap1, leftViewMap2)
        ocvb.result1 = ocvb.leftView.Remap(leftViewMap1, leftViewMap2, cv.InterpolationFlags.Linear).Resize(ocvb.color.Size())
        ocvb.result2 = ocvb.color.Remap(leftViewMap1, leftViewMap2, cv.InterpolationFlags.Linear).Resize(ocvb.color.Size())
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class