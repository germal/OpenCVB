Imports cv = OpenCvSharp
'https://github.com/opencv/opencv/blob/master/samples/cpp/stereo_match.cpp
Public Class BlockMatching_Basics
    Inherits ocvbClass
    Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        colorizer = New Depth_Colorizer_CPP(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "Blockmatch scale", 1, 200, 100)
        sliders.setupTrackBar2(ocvb, caller, "Blockmatch max disparity", 1, 5, 1)
        sliders.setupTrackBar3(ocvb, caller, "Blockmatch block size", 5, 255, 15)
        ocvb.desc = "Use OpenCV's block matching on left and right views - needs more work"
        ocvb.label1 = "Block matching disparity colorized like depth"
        ocvb.label2 = "Right Image (used with left image)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1 = ocvb.color.EmptyClone.SetTo(0)
        Dim scale = sliders.TrackBar1.Value / 100
        If sliders.TrackBar1.Value <> 100 Then
            Dim method = If(scale < 1.0, cv.InterpolationFlags.Area, cv.InterpolationFlags.Cubic)
            ocvb.leftView = ocvb.leftView.Resize(New cv.Size(ocvb.leftView.Width * scale, ocvb.leftView.Height * scale), 0, 0, method)
            ocvb.rightView = ocvb.rightView.Resize(New cv.Size(ocvb.rightView.Width * scale, ocvb.rightView.Height * scale), 0, 0, method)
        End If

        Dim numDisparity = sliders.TrackBar2.Value * 16 ' must be a multiple of 16
        Dim blockSize = sliders.TrackBar3.Value
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
        disparity.ConvertTo(colorizer.src, cv.MatType.CV_32F, 1000 / 16)
        colorizer.src.Threshold(0, 0, cv.ThresholdTypes.Tozero)
        colorizer.Run(ocvb)
        Dim topMargin = 10, sideMargin = 8
        Dim rect = New cv.Rect(numDisparity + sideMargin, topMargin, ocvb.color.Width - numDisparity - sideMargin * 2, ocvb.color.Height - topMargin * 2)
        dst1(rect) = colorizer.dst1(rect)
        dst2 = ocvb.rightView.Resize(ocvb.color.Size())
    End Sub
End Class

