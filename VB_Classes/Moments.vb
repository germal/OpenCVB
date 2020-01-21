Imports cv = OpenCvSharp
Public Class Moments_CentroidKalman : Implements IDisposable
    Dim check As New OptionsCheckbox
    Dim foreground As kMeans_Depth_FG_BG
    Dim kalman As Kalman_Point2f
    Public Sub New(ocvb As AlgorithmData)
        kalman = New Kalman_Point2f(ocvb)
        foreground = New kMeans_Depth_FG_BG(ocvb)

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Turn Kalman filtering on"
        check.Box(0).Checked = True
        check.show()

        ocvb.desc = "Compute the centroid of the foreground depth and smooth with Kalman filter."
        ocvb.label1 = "Red dot = Kalman smoothed centroid"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        foreground.Run(ocvb)
        Dim mask = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim m = cv.Cv2.Moments(mask, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            If check.Box(0).Checked Then
                kalman.inputReal = New cv.Point2f(m.M10 / m.M00, m.M01 / m.M00)
                kalman.Run(ocvb)
                ocvb.result1.Circle(New cv.Point(kalman.statePoint.X, kalman.statePoint.Y), 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            Else
                ocvb.result1.Circle(New cv.Point(CInt(m.M10 / m.M00), CInt(m.M01 / m.M00)), 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        foreground.Dispose()
        kalman.Dispose()
        check.Dispose()
    End Sub
End Class