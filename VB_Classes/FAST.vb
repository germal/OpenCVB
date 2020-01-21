Imports cv = OpenCvSharp
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/FASTSample.vb
Public Class FAST_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public keypoints() As cv.KeyPoint
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Threshold", 0, 200, 15)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Find interesting points with the FAST (Features from Accelerated Segment Test) algorithm"
        ocvb.label1 = "FAST_Basics nonMax = true"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.color.CopyTo(ocvb.result1)
        keypoints = cv.Cv2.FAST(gray, sliders.TrackBar1.Value, True)

        For Each kp As cv.KeyPoint In keypoints
            ocvb.result1.Circle(kp.Pt, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias, 0)
        Next kp
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class





Public Class FAST_Centroid : Implements IDisposable
    Dim check As New OptionsCheckbox
    Dim fast As FAST_Basics
    Dim kalman As Kalman_Point2f
    Public Sub New(ocvb As AlgorithmData)
        kalman = New Kalman_Point2f(ocvb)
        fast = New FAST_Basics(ocvb)

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Turn Kalman filtering on"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        fast.Run(ocvb)
        ocvb.result2.SetTo(0)
        For Each kp As cv.KeyPoint In fast.keypoints
            ocvb.result2.Circle(kp.Pt, 10, cv.Scalar.White, -1, cv.LineTypes.AntiAlias, 0)
        Next kp
        Dim gray = ocvb.result2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim m = cv.Cv2.Moments(gray, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            If check.Box(0).Checked Then
                kalman.inputReal = New cv.Point2f(m.M10 / m.M00, m.M01 / m.M00)
                kalman.Run(ocvb)
                ocvb.result2.Circle(New cv.Point(kalman.statePoint.X, kalman.statePoint.Y), 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            Else
                ocvb.result2.Circle(New cv.Point2f(m.M10 / m.M00, m.M01 / m.M00), 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        fast.Dispose()
        kalman.Dispose()
        check.Dispose()
    End Sub
End Class

