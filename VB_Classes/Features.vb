Imports cv = OpenCvSharp

' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Features_GoodFeatures
    Inherits ocvbClass
    Public goodFeatures As New List(Of cv.Point2f)
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Number of Points", 10, 1000, 200)
        sliders.setupTrackBar(1, "Quality Level", 1, 100, 1)
        sliders.setupTrackBar(2, "Distance", 1, 100, 30)
        sliders.setupTrackBar(3, "Image mix percentage X100", 0, 100, 50)

        setDescription(ocvb, "Find good features to track in an RGB image.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim numPoints = sliders.trackbar(0).Value
        Dim quality = sliders.trackbar(1).Value / 100
        Dim minDistance = sliders.trackbar(2).Value
        Dim features = cv.Cv2.GoodFeaturesToTrack(src, numPoints, quality, minDistance, Nothing, 7, True, 3)

        If standalone Then src.CopyTo(dst1)
        goodFeatures.Clear()
        For i = 0 To features.Length - 1
            goodFeatures.Add(features.ElementAt(i))
            cv.Cv2.Circle(dst1, features(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class

