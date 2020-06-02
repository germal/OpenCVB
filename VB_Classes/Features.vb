Imports cv = OpenCvSharp

' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Features_GoodFeatures
    Inherits ocvbClass
    Public goodFeatures As New List(Of cv.Point2f)
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Number of Points", 10, 1000, 200)
        sliders.setupTrackBar2("Quality Level", 1, 100, 1)
        sliders.setupTrackBar3("Distance", 1, 100, 30)

        ocvb.desc = "Find good features to track in an RGB image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim numPoints = sliders.TrackBar1.Value
        Dim quality = sliders.TrackBar2.Value / 100
        Dim minDistance = sliders.TrackBar3.Value
        Dim features = cv.Cv2.GoodFeaturesToTrack(src, numPoints, quality, minDistance, Nothing, 7, True, 3)

        If standalone Then src.CopyTo(dst1)
        goodFeatures.Clear()
        For i = 0 To features.Length - 1
            goodFeatures.Add(features.ElementAt(i))
            cv.Cv2.Circle(dst1, features(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class

