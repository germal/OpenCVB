Imports cv = OpenCvSharp
Public Class Moments_Basics
    Inherits ocvbClass
    Public inputMask As cv.Mat
    Public centroid As cv.Point2f
    Dim foreground As kMeans_Depth_FG_BG
    Public scaleFactor As Integer = 1
    Public offsetPt As cv.Point
    Public kalman As Kalman_Basics
    Public useKalman As Boolean = True
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        If standalone Then foreground = New kMeans_Depth_FG_BG(ocvb)

        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.input(2 - 1) ' 2 elements - cv.point

        label1 = "Red dot = Kalman smoothed centroid"
        setDescription(ocvb, "Compute the centroid of the provided mask file.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            foreground.Run(ocvb)
            dst1 = foreground.dst1
            inputMask = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        Dim m = cv.Cv2.Moments(inputMask, True)

        Dim center As cv.Point2f
        If kalman.check.Box(0).Checked Or useKalman Then
            kalman.input(0) = m.M10 / m.M00
            kalman.input(1) = m.M01 / m.M00
            kalman.Run(ocvb)
            center = New cv.Point2f(kalman.output(0), kalman.output(1))
        Else
            center = New cv.Point2f(m.M10 / m.M00, m.M01 / m.M00)
        End If
        If standalone Then dst1.Circle(center, 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        centroid = New cv.Point2f(scaleFactor * (offsetPt.X + center.X), scaleFactor * (offsetPt.Y + center.Y))
    End Sub
End Class





Public Class Moments_CentroidKalman
    Inherits ocvbClass
    Dim foreground As kMeans_Depth_FG_BG
    Dim kalman As Kalman_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.input(2 - 1) ' 2 elements - cv.point

        foreground = New kMeans_Depth_FG_BG(ocvb)

        label1 = "Red dot = Kalman smoothed centroid"
        setDescription(ocvb, "Compute the centroid of the foreground depth and smooth with Kalman filter.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        foreground.Run(ocvb)
        dst1 = foreground.dst1
        Dim mask = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim m = cv.Cv2.Moments(mask, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.input(0) = m.M10 / m.M00
            kalman.input(1) = m.M01 / m.M00
            kalman.Run(ocvb)
            dst1.Circle(New cv.Point(kalman.output(0), kalman.output(1)), 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        End If
    End Sub
End Class
