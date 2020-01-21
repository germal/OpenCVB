Imports cv = OpenCvSharp
'https://github.com/opencv/opencv/blob/master/samples/cpp/stereo_match.cpp
Public Class BlockMatching_Basics : Implements IDisposable
    Dim disp16 As Depth_Colorizer_CPP
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        disp16 = New Depth_Colorizer_CPP(ocvb)
        disp16.externalUse = True

        sliders.setupTrackBar1(ocvb, "Blockmatch scale", 1, 200, 100)
        sliders.setupTrackBar2(ocvb, "Blockmatch max disparity", 1, 8, 1)
        sliders.setupTrackBar3(ocvb, "Blockmatch block size", 5, 255, 15)
        sliders.show()
        ocvb.desc = "Use OpenCV's block matching on the Realsense infrared views."
        ocvb.label1 = "Disparity image (not depth)"
        ocvb.label2 = "Right Infrared Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim scale = sliders.TrackBar1.Value / 100
        If sliders.TrackBar1.Value <> 100 Then
            Dim method = If(scale < 1.0, cv.InterpolationFlags.Area, cv.InterpolationFlags.Cubic)
            ocvb.redLeft = ocvb.redLeft.Resize(New cv.Size(ocvb.redLeft.Width * scale, ocvb.redLeft.Height * scale), 0, 0, method)
            ocvb.redRight = ocvb.redRight.Resize(New cv.Size(ocvb.redRight.Width * scale, ocvb.redRight.Height * scale), 0, 0, method)
        End If

        Dim numDisparity = sliders.TrackBar2.Value * 16 ' must be a multiple of 16
        Dim blockSize = sliders.TrackBar3.Value
        If blockSize Mod 2 = 0 Then blockSize += 1 ' must be odd

        Static blockMatch = cv.StereoBM.Create()
        blockMatch.BlockSize = blockSize
        blockMatch.MinDisparity = 0
        blockMatch.ROI1 = New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height)
        blockMatch.ROI2 = New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height)
        blockMatch.PreFilterCap = 31
        blockMatch.NumDisparities = numDisparity
        blockMatch.TextureThreshold = 10
        blockMatch.UniquenessRatio = 15
        blockMatch.SpeckleWindowSize = 100
        blockMatch.SpeckleRange = 32
        blockMatch.Disp12MaxDiff = 1

        Dim disparity As New cv.Mat
        blockMatch.compute(ocvb.redLeft, ocvb.redRight, disparity)
        disp16.src = disparity
        disp16.Run(ocvb)
        ocvb.result1 = disp16.dst
        ocvb.result2 = ocvb.redRight
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        disp16.Dispose()
    End Sub
End Class
