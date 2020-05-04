Imports cv = OpenCvSharp
Public Class Moments_CentroidKalman : Implements IDisposable
    Dim foreground As kMeans_Depth_FG_BG
    Dim kalman As Kalman_Basics
    Public Sub New(ocvb As AlgorithmData)
        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.src(2 - 1) ' 2 elements - cv.point
        kalman.externalUse = True

        foreground = New kMeans_Depth_FG_BG(ocvb)

        ocvb.label1 = "Red dot = Kalman smoothed centroid"
        ocvb.desc = "Compute the centroid of the foreground depth and smooth with Kalman filter."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        foreground.Run(ocvb)
        Dim mask = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim m = cv.Cv2.Moments(mask, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.src(0) = m.M10 / m.M00
            kalman.src(1) = m.M01 / m.M00
            kalman.Run(ocvb)
            ocvb.result1.Circle(New cv.Point(kalman.dst(0), kalman.dst(1)), 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        foreground.Dispose()
        kalman.Dispose()
    End Sub
End Class