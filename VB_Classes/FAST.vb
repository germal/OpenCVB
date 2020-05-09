Imports cv = OpenCvSharp
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/FASTSample.vb
Public Class FAST_Basics
    Inherits ocvbClass
    Public keypoints() As cv.KeyPoint
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Threshold", 0, 200, 15)
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
		MyBase.Finish(ocvb)
    End Sub
End Class





Public Class FAST_Centroid
    Inherits ocvbClass
    Dim fast As FAST_Basics
    Dim kalman As Kalman_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        kalman = New Kalman_Basics(ocvb, caller)
        ReDim kalman.input(1) ' 2 elements - cv.point

        fast = New FAST_Basics(ocvb, caller)
        ocvb.desc = "Find interesting points with the FAST and smooth the centroid with kalman"
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
            kalman.input(0) = m.M10 / m.M00
            kalman.input(1) = m.M01 / m.M00
            kalman.Run(ocvb)
            ocvb.result2.Circle(New cv.Point(kalman.output(0), kalman.output(1)), 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        End If
		MyBase.Finish(ocvb)
    End Sub
    Public Sub MyDispose()
        fast.Dispose()
        kalman.Dispose()
    End Sub
End Class


