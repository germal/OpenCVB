Imports cv = OpenCvSharp

' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Features_GoodFeatures
    Inherits ocvbClass
    Public goodFeatures As New List(Of cv.Point2f)
    Public gray As cv.Mat = Nothing
        Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Number of Points", 10, 1000, 200)
        sliders.setupTrackBar2(ocvb, caller, "Quality Level", 1, 100, 1)
        sliders.setupTrackBar3(ocvb, caller, "Distance", 1, 100, 30)

        ocvb.desc = "Find good features to track in an RGB image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        if standalone Then gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim numPoints = sliders.TrackBar1.Value
        Dim quality = sliders.TrackBar2.Value / 100
        Dim minDistance = sliders.TrackBar3.Value
        Dim features = cv.Cv2.GoodFeaturesToTrack(gray, numPoints, quality, minDistance, Nothing, 7, True, 3)

        if standalone Then gray.CopyTo(ocvb.result1)
        goodFeatures.Clear()
        For i = 0 To features.Length - 1
            goodFeatures.Add(features.ElementAt(i))
            if standalone Then cv.Cv2.Circle(ocvb.result1, features(i), 3, cv.Scalar.white, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class
