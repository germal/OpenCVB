Imports cv = OpenCvSharp

' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Features_GoodFeatures
    Inherits VB_Class
        Public goodFeatures As New List(Of cv.Point2f)
    Public gray As cv.Mat = Nothing
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Number of Points", 10, 1000, 200)
        sliders.setupTrackBar2(ocvb, callerName, "Quality Level", 1, 100, 1)
        sliders.setupTrackBar3(ocvb, callerName,"Distance", 1, 100, 30)
        
        ocvb.desc = "Find good features to track in an RGB image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim numPoints = sliders.TrackBar1.Value
        Dim quality = sliders.TrackBar2.Value / 100
        Dim minDistance = sliders.TrackBar3.Value
        Dim features = cv.Cv2.GoodFeaturesToTrack(gray, numPoints, quality, minDistance, Nothing, 7, True, 3)

        If externalUse = False Then gray.CopyTo(ocvb.result1)
        goodFeatures.Clear()
        For i = 0 To features.Length - 1
            goodFeatures.Add(features.ElementAt(i))
            If externalUse = False Then cv.Cv2.Circle(ocvb.result1, features(i), 3, cv.Scalar.white, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub MyDispose()
            End Sub
End Class
