Imports cv = OpenCvSharp
'https://github.com/opencv/opencv/blob/master/samples/cpp/stereo_match.cpp
Public Class BlockMatching_Basics
    Inherits ocvbClass
    Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        colorizer = New Depth_Colorizer_CPP(ocvb)

        sliders.setupTrackBar1(ocvb, "Blockmatch max disparity", 2, 5, 2)
        sliders.setupTrackBar2(ocvb, "Blockmatch block size", 5, 255, 15)
        sliders.setupTrackBar3(ocvb, "Blockmatch distance factor (approx) X1000", 1, 100, 20)
        ocvb.desc = "Use OpenCV's block matching on left and right views"
        label1 = "Block matching disparity colorized like depth"
        label2 = "Right Image (used with left image)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex = Kinect4AzureCam Then
            ocvb.putText(New oTrueType("For the Kinect 4 Azure camera, the left and right views are the same.", 10, 50, RESULT1))
        End If

        Dim numDisparity = sliders.TrackBar1.Value * 16 ' must be a multiple of 16
        Dim blockSize = sliders.TrackBar2.Value
        If blockSize Mod 2 = 0 Then blockSize += 1 ' must be odd

        Static blockMatch = cv.StereoBM.Create()
        blockMatch.BlockSize = blockSize
        blockMatch.MinDisparity = 0
        blockMatch.ROI1 = New cv.Rect(0, 0, ocvb.leftView.Width, ocvb.leftView.Height)
        blockMatch.ROI2 = New cv.Rect(0, 0, ocvb.leftView.Width, ocvb.leftView.Height)
        blockMatch.PreFilterCap = 31
        blockMatch.NumDisparities = numDisparity
        blockMatch.TextureThreshold = 10
        blockMatch.UniquenessRatio = 15
        blockMatch.SpeckleWindowSize = 100
        blockMatch.SpeckleRange = 32
        blockMatch.Disp12MaxDiff = 1

        Dim disparity As New cv.Mat
        blockMatch.compute(ocvb.leftView, ocvb.rightView, disparity)
        disparity.ConvertTo(colorizer.src, cv.MatType.CV_32F, 1 / 16)
        colorizer.src = colorizer.src.Threshold(0, 0, cv.ThresholdTypes.Tozero)
        Dim topMargin = 10, sideMargin = 8
        Dim rect = New cv.Rect(numDisparity + sideMargin, topMargin, src.Width - numDisparity - sideMargin * 2, src.Height - topMargin * 2)
        Dim tmp = New cv.Mat(src.Size(), cv.MatType.CV_32F, 0)
        Dim distance = sliders.TrackBar3.Value * 1000
        cv.Cv2.Divide(distance, colorizer.src(rect), colorizer.src(rect)) ' this needs much more refinement.  The trackbar3 value is just an approximation.
        colorizer.src(rect) = colorizer.src(rect).Threshold(10000, 10000, cv.ThresholdTypes.Trunc)
        colorizer.Run(ocvb)
        dst1(rect) = colorizer.dst1(rect)
        dst2 = ocvb.rightView.Resize(src.Size())
    End Sub
End Class

