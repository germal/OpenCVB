Imports cv = OpenCvSharp

' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Features_GoodFeatures
    Inherits VBparent
    Public goodFeatures As New List(Of cv.Point2f)
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Number of Points", 10, 1000, 200)
        sliders.setupTrackBar(1, "Quality Level", 1, 100, 1)
        sliders.setupTrackBar(2, "Distance", 1, 100, 30)

        ocvb.desc = "Find good features to track in an RGB image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim numPoints = sliders.trackbar(0).Value
        Dim quality = sliders.trackbar(1).Value / 100
        Dim minDistance = sliders.trackbar(2).Value
        Dim features = cv.Cv2.GoodFeaturesToTrack(src, numPoints, quality, minDistance, Nothing, 7, True, 3)

        src.CopyTo(dst1)
        goodFeatures.Clear()
        For i = 0 To features.Length - 1
            goodFeatures.Add(features.ElementAt(i))
            cv.Cv2.Circle(dst1, features(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class





Public Class Features_PointTracker
    Inherits VBparent
    Dim features As Features_GoodFeatures
    Dim pTrack As Kalman_PointTracker
    Dim rRadius = 10
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        features = New Features_GoodFeatures(ocvb)
        pTrack = New Kalman_PointTracker(ocvb)
        Dim drawRectCheck = findCheckBox("Draw rectangle and centroid for each mask")
        drawRectCheck.Checked = False

        label1 = "Good features without Kalman"
        label2 = "Good features with Kalman"
        ocvb.desc = "Find good features and track them"
    End Sub
    Public Sub Run(ocvb As VBocvb)

        features.src = src
        features.Run(ocvb)
        dst1 = features.dst1

        pTrack.queryPoints.Clear()
        pTrack.queryRects.Clear()
        pTrack.queryMasks.Clear()

        For i = 0 To features.goodFeatures.Count - 1
            Dim pt = features.goodFeatures(i)
            pTrack.queryPoints.Add(pt)
            Dim r = New cv.Rect(pt.X - rRadius, pt.Y - rRadius, rRadius * 2, rRadius * 2)
            pTrack.queryRects.Add(r)
            pTrack.queryMasks.Add(New cv.Mat)
        Next

        pTrack.src = src
        pTrack.Run(ocvb)

        dst2.SetTo(0)
        For Each obj In pTrack.drawRC.viewObjects
            Dim r = obj.Value.rectView
            If r.Width > 0 And r.Height > 0 Then
                If r.X + r.Width < dst2.Width And r.Y + r.Height < dst2.Height Then src(obj.Value.rectView).CopyTo(dst2(obj.Value.rectView))
            End If
        Next
    End Sub
End Class
