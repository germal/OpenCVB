Imports cv = OpenCvSharp
Public Class Moments_CentroidKalman
    Inherits ocvbClass
    Dim foreground As kMeans_Depth_FG_BG
    Dim kalman As Kalman_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        kalman = New Kalman_Basics(ocvb, caller)
        ReDim kalman.input(2 - 1) ' 2 elements - cv.point

        foreground = New kMeans_Depth_FG_BG(ocvb, caller)

        ocvb.label1 = "Red dot = Kalman smoothed centroid"
        ocvb.desc = "Compute the centroid of the foreground depth and smooth with Kalman filter."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        foreground.Run(ocvb)
        Dim mask = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim m = cv.Cv2.Moments(mask, True)
        If m.M00 > 5000 Then ' if more than x pixels are present (avoiding a zero area!)
            kalman.input(0) = m.M10 / m.M00
            kalman.input(1) = m.M01 / m.M00
            kalman.Run(ocvb)
            ocvb.result1.Circle(New cv.Point(kalman.output(0), kalman.output(1)), 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        End If
    End Sub
End Class
