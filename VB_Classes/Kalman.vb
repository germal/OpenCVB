Imports cv = OpenCvSharp
'http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_Basics
    Inherits VBparent
    Dim kalman() As Kalman_Simple
    Public kInput(4 - 1) As Single
    Public kOutput(4 - 1) As Single
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Turn Kalman filtering on"
        check.Box(0).Checked = True

        ocvb.desc = "Use Kalman to stabilize values (such as a cv.rect.)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static saveDimension As Integer = -1
        If saveDimension <> kInput.Length Then
            If kalman IsNot Nothing Then
                If kalman.Count > 0 Then
                    For i = 0 To kalman.Count - 1
                        kalman(i).Dispose()
                    Next
                End If
            End If
            saveDimension = kInput.Length
            ReDim kalman(kInput.Length - 1)
            For i = 0 To kInput.Length - 1
                kalman(i) = New Kalman_Simple()
            Next
            ReDim kOutput(kInput.Count - 1)
        End If

        Static useKalmanCheck = findCheckBox("Turn Kalman filtering on")
        If useKalmanCheck.Checked Then
            For i = 0 To kalman.Length - 1
                kalman(i).inputReal = kInput(i)
                kalman(i).Run(ocvb)
                If Double.IsNaN(kalman(i).stateResult) Then kalman(i).stateResult = kalman(i).inputReal ' kalman failure...
                kOutput(i) = kalman(i).stateResult
            Next
        Else
            kOutput = kInput ' do nothing to the input.
        End If

        If standalone Or ocvb.intermediateReview = caller Then
            dst1 = src.Clone()
            Dim rect = New cv.Rect(CInt(kOutput(0)), CInt(kOutput(1)), CInt(kOutput(2)), CInt(kOutput(3)))
            rect = validateRect(rect)
            Static lastRect = rect
            If rect = lastRect Then
                Dim r = initRandomRect(src.Width, src.Height, 50)
                kInput = New Single() {r.X, r.Y, r.Width, r.Height}
            End If
            lastRect = rect
            dst1.Rectangle(rect, cv.Scalar.White, 6)
            dst1.Rectangle(rect, cv.Scalar.Red, 1)
        End If
    End Sub
End Class





' http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_Compare
    Inherits VBparent
    Dim kalman() As Kalman_Single
    Public plot As Plot_OverTime
    Public kPlot As Plot_OverTime
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        plot = New Plot_OverTime(ocvb)
        plot.plotCount = 3
        plot.topBottomPad = 20

        kPlot = New Plot_OverTime(ocvb)
        kPlot.plotCount = 3
        kPlot.topBottomPad = 20

        label1 = "Kalman input: mean values for RGB"
        label2 = "Kalman output: smoothed mean values for RGB"
        ocvb.desc = "Use this kalman filter to predict the next value."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount = 0 Then
            If kalman IsNot Nothing Then
                If kalman.Count > 0 Then
                    For i = 0 To kalman.Count - 1
                        kalman(i).Dispose()
                    Next
                End If
            End If
            ReDim kalman(3 - 1)
            For i = 0 To kalman.Count - 1
                kalman(i) = New Kalman_Single(ocvb)
            Next
        End If

        ' if either one has triggered a reset for the scale, do them both...
        If kPlot.offChartCount >= kPlot.plotTriggerRescale Or plot.offChartCount >= plot.plotTriggerRescale Then
            kPlot.offChartCount = kPlot.plotTriggerRescale + 1
            plot.offChartCount = plot.plotTriggerRescale + 1
        End If

        plot.plotData = src.Mean()
        plot.Run(ocvb)
        dst1 = plot.dst1

        For i = 0 To kalman.Count - 1
            kalman(i).inputReal = plot.plotData.Item(i)
            kalman(i).Run(ocvb)
        Next

        kPlot.maxScale = plot.maxScale ' keep the scale the same for the side-by-side plots.
        kPlot.minScale = plot.minScale
        kPlot.plotData = New cv.Scalar(kalman(0).stateResult, kalman(1).stateResult, kalman(2).stateResult)
        kPlot.Run(ocvb)
        dst2 = kPlot.dst1
    End Sub
End Class



'https://github.com/opencv/opencv/blob/master/samples/cpp/kalman.cpp
Public Class Kalman_RotatingPoint
    Inherits VBparent
    Dim kf As New cv.KalmanFilter(2, 1, 0)
    Dim kState As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Dim processNoise As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Dim measurement As New cv.Mat(1, 1, cv.MatType.CV_32F, 0)
    Dim center As cv.Point2f, statePt As cv.Point2f
    Dim radius As Single
    Private Function calcPoint(center As cv.Point2f, R As Double, angle As Double) As cv.Point
        Return center + New cv.Point2f(Math.Cos(angle), -Math.Sin(angle)) * R
    End Function
    Private Sub drawCross(dst1 As cv.Mat, center As cv.Point, color As cv.Scalar)
        Dim d As Integer = 3
        cv.Cv2.Line(dst1, New cv.Point(center.X - d, center.Y - d), New cv.Point(center.X + d, center.Y + d), color, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.Line(dst1, New cv.Point(center.X + d, center.Y - d), New cv.Point(center.X - d, center.Y + d), color, 1, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        label1 = "Estimate Yellow < Real Red (if working)"

        cv.Cv2.Randn(kState, New cv.Scalar(0), cv.Scalar.All(0.1))
        kf.TransitionMatrix = New cv.Mat(2, 2, cv.MatType.CV_32F, New Single() {1, 1, 0, 1})

        cv.Cv2.SetIdentity(kf.MeasurementMatrix)
        cv.Cv2.SetIdentity(kf.ProcessNoiseCov, cv.Scalar.All(0.00001))
        cv.Cv2.SetIdentity(kf.MeasurementNoiseCov, cv.Scalar.All(0.1))
        cv.Cv2.SetIdentity(kf.ErrorCovPost, cv.Scalar.All(1))
        cv.Cv2.Randn(kf.StatePost, New cv.Scalar(0), cv.Scalar.All(1))
        radius = src.Rows / 2.4 ' so we see the entire circle...
        center = New cv.Point2f(src.Cols / 2, src.Rows / 2)
        ocvb.desc = "Track a rotating point using a Kalman filter. Yellow line (estimate) should be shorter than red (real)."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim stateAngle = kState.Get(Of Single)(0)

        Dim prediction = kf.Predict()
        Dim predictAngle = prediction.Get(Of Single)(0)
        Dim predictPt = calcPoint(center, radius, predictAngle)
        statePt = calcPoint(center, radius, stateAngle)

        cv.Cv2.Randn(measurement, New cv.Scalar(0), cv.Scalar.All(kf.MeasurementNoiseCov.Get(Of Single)(0)))

        measurement += kf.MeasurementMatrix * kState
        Dim measAngle = measurement.Get(Of Single)(0)
        Dim measPt = calcPoint(center, radius, measAngle)

        dst1.SetTo(0)
        drawCross(dst1, statePt, cv.Scalar.White)
        drawCross(dst1, measPt, cv.Scalar.White)
        drawCross(dst1, predictPt, cv.Scalar.White)
        cv.Cv2.Line(dst1, statePt, measPt, New cv.Scalar(0, 0, 255), 3, cv.LineTypes.AntiAlias)
        cv.Cv2.Line(dst1, statePt, predictPt, New cv.Scalar(0, 255, 255), 3, cv.LineTypes.AntiAlias)

        If msRNG.Next(0, 4) <> 0 Then kf.Correct(measurement)

        cv.Cv2.Randn(processNoise, cv.Scalar.Black, cv.Scalar.All(Math.Sqrt(kf.ProcessNoiseCov.Get(Of Single)(0, 0))))
        kState = kf.TransitionMatrix * kState + processNoise
    End Sub
End Class






' http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
' https://www.codeproject.com/Articles/865935/Object-Tracking-Kalman-Filter-with-Ease
Public Class Kalman_MousePredict
    Inherits VBparent
    Dim kalman As Kalman_Basics
    Dim lineWidth As Integer
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.kInput(2 - 1)
        ReDim kalman.kOutput(2 - 1)

        lineWidth = src.Width / 300
        label1 = "Red is real mouse, white is prediction"
        ocvb.desc = "Use kalman filter to predict the next mouse location."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount Mod 100 = 0 Then dst1.SetTo(0)

        Static lastRealMouse = ocvb.mousePoint
        kalman.kInput(0) = ocvb.mousePoint.X
        kalman.kInput(1) = ocvb.mousePoint.Y
        Dim lastStateResult = New cv.Point(kalman.kOutput(0), kalman.kOutput(1))
        kalman.Run(ocvb)
        cv.Cv2.Line(dst1, New cv.Point(kalman.kOutput(0), kalman.kOutput(1)), lastStateResult, cv.Scalar.All(255), lineWidth, cv.LineTypes.AntiAlias)
        cv.Cv2.Line(dst1, ocvb.mousePoint, lastRealMouse, New cv.Scalar(0, 0, 255), lineWidth, cv.LineTypes.AntiAlias)
        lastRealMouse = ocvb.mousePoint
    End Sub
End Class







Public Class Kalman_CVMat
    Inherits VBparent
    Dim kalman() As Kalman_Simple
    Public output As cv.Mat
    Dim basics As Kalman_Basics
    Public input As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        basics = New Kalman_Basics(ocvb)
        ReDim basics.kInput(4 - 1)
        input = New cv.Mat(4, 1, cv.MatType.CV_32F, 0)
        If standalone Then label1 = "Rectangle moves smoothly to random locations"
        ocvb.desc = "Use Kalman to stabilize a set of values such as a cv.rect or cv.Mat"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static saveDimension As Integer = -1
        If saveDimension <> input.Rows Then
            If kalman IsNot Nothing Then
                If kalman.Count > 0 Then
                    For i = 0 To kalman.Count - 1
                        kalman(i).Dispose()
                    Next
                End If
            End If
            saveDimension = input.Rows
            ReDim kalman(input.Rows - 1)
            For i = 0 To input.Rows - 1
                kalman(i) = New Kalman_Simple()
            Next
            output = New cv.Mat(input.Rows, 1, cv.MatType.CV_32F, 0)
        End If

        If basics.check.Box(0).Checked Then
            For i = 0 To kalman.Length - 1
                kalman(i).inputReal = input.Get(Of Single)(i, 0)
                kalman(i).Run(ocvb)
                output.Set(Of Single)(i, 0, kalman(i).stateResult)
            Next
        Else
            output = input ' do nothing to the input.
        End If


        If standalone Then
            Dim rx(input.Rows - 1) As Single
            Dim testrect As New cv.Rect
            For i = 0 To input.Rows - 1
                rx(i) = output.Get(Of Single)(i, 0)
            Next
            dst1 = src
            Dim rect = New cv.Rect(CInt(rx(0)), CInt(rx(1)), CInt(rx(2)), CInt(rx(3)))
            rect = validateRect(rect)

            Static lastRect As cv.Rect = rect
            If lastRect = rect Then
                Dim r = initRandomRect(src.Width, src.Height, 25)
                Dim array() As Single = {r.X, r.Y, r.Width, r.Height}
                input = New cv.Mat(4, 1, cv.MatType.CV_32F, array)
            End If
            dst1.Rectangle(rect, cv.Scalar.Red, 2)
            lastRect = rect
        End If
    End Sub
End Class







Public Class Kalman_ImageSmall
    Inherits VBparent
    Dim kalman As Kalman_CVMat
    Dim resize As Resize_Percentage
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        kalman = New Kalman_CVMat(ocvb)

        resize = New Resize_Percentage(ocvb)

        label1 = "The small image is processed by the Kalman filter"
        label2 = "Mask of the smoothed image minus original"
        ocvb.desc = "Resize the image to allow the Kalman filter to process the whole image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        resize.src = src
        resize.Run(ocvb)

        Dim saveOriginal = resize.dst1.Clone()
        Dim gray32f As New cv.Mat
        resize.dst1.ConvertTo(gray32f, cv.MatType.CV_32F)
        kalman.input = gray32f.Reshape(1, gray32f.Width * gray32f.Height)
        kalman.Run(ocvb)
        Dim tmp As New cv.Mat
        kalman.output.ConvertTo(tmp, cv.MatType.CV_8U)
        tmp = tmp.Reshape(1, gray32f.Height)
        dst1 = tmp.Resize(dst1.Size())
        cv.Cv2.Subtract(tmp, saveOriginal, dst2)
        dst2 = dst2.Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst2 = dst2.Resize(dst1.Size())
    End Sub
End Class





Public Class Kalman_DepthSmall
    Inherits VBparent
    Dim kalman As Kalman_ImageSmall
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        kalman = New Kalman_ImageSmall(ocvb)

        label1 = "Mask of non-zero depth after Kalman smoothing"
        label2 = "Mask of the smoothed image minus original"
        ocvb.desc = "Use a resized depth Mat to find where depth is decreasing (something getting closer.)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        kalman.src = ocvb.RGBDepth
        kalman.Run(ocvb)
        dst1 = kalman.dst1
        dst2 = kalman.dst2
    End Sub
End Class







Public Class Kalman_Depth32f
    Inherits VBparent
    Dim kalman As Kalman_CVMat
    Dim resize As Resize_Percentage
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        kalman = New Kalman_CVMat(ocvb)

        resize = New Resize_Percentage(ocvb)
        resize.sliders.trackbar(0).Value = 4

        label1 = "Mask of non-zero depth after Kalman smoothing"
        label2 = "Difference from original depth"
        ocvb.desc = "Use a resized depth Mat to find where depth is decreasing (getting closer.)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim depth32f = getDepth32f(ocvb)
        resize.src = depth32f
        resize.Run(ocvb)

        kalman.input = resize.dst1.Reshape(1, resize.dst1.Width * resize.dst1.Height)
        kalman.Run(ocvb)
        dst1 = kalman.output.Reshape(1, resize.dst1.Height)
        dst1 = dst1.Resize(src.Size())
        cv.Cv2.Subtract(dst1, depth32f, dst2)
        dst2 = dst2.Normalize(255)
    End Sub
End Class







Public Class Kalman_Single
    Inherits VBparent
    Dim plot As Plot_OverTime
    Dim kf As New cv.KalmanFilter(2, 1, 0)
    Dim processNoise As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Public measurement As New cv.Mat(1, 1, cv.MatType.CV_32F, 0)
    Public inputReal As Single
    Public stateResult As Single
    Public ProcessNoiseCov As Single = 0.00001
    Public MeasurementNoiseCov As Single = 0.1
    Public ErrorCovPost As Single = 1
    Public transitionMatrix() As Single = {1, 1, 0, 1} ' Change the transition matrix externally and set newTransmissionMatrix.
    Public newTransmissionMatrix As Boolean = True
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        Dim tMatrix() As Single = {1, 1, 0, 1}
        kf.TransitionMatrix = New cv.Mat(2, 2, cv.MatType.CV_32F, tMatrix)
        kf.MeasurementMatrix.SetIdentity(1)
        kf.ProcessNoiseCov.SetIdentity(0.00001)
        kf.MeasurementNoiseCov.SetIdentity(0.1)
        kf.ErrorCovPost.SetIdentity(1)
        plot = New Plot_OverTime(ocvb)
        plot.plotCount = 2 ' 2 items to plot
        ocvb.desc = "Estimate a single value using a Kalman Filter - in the default case, the value of the mean of the grayscale image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone Then
            dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            inputReal = dst1.Mean().Item(0)
        End If

        Dim prediction = kf.Predict()
        measurement.Set(Of Single)(0, 0, inputReal)
        stateResult = kf.Correct(measurement).Get(Of Single)(0, 0)
        If standalone Then
            plot.plotData = New cv.Scalar(inputReal, stateResult, 0, 0)
            plot.Run(ocvb)
            dst2 = plot.dst1
            label1 = "Mean of the grayscale image is predicted"
            label2 = "Mean (blue) = " + Format(inputReal, "0.0") + " predicted (green) = " + Format(stateResult, "0.0")
        End If
    End Sub
End Class






' This algorithm is different and does not inherit from ocvbClass.  It is the minimal work to implement kalman to allow large Kalman sets.
Public Class Kalman_Simple : Implements IDisposable
    Dim kf As New cv.KalmanFilter(2, 1, 0)
    Dim processNoise As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Public measurement As New cv.Mat(1, 1, cv.MatType.CV_32F, 0)
    Public inputReal As Single
    Public stateResult As Single
    Public ProcessNoiseCov As Single = 0.00001
    Public MeasurementNoiseCov As Single = 0.1
    Public ErrorCovPost As Single = 1
    Public transitionMatrix() As Single = {1, 1, 0, 1} ' Change the transition matrix externally and set newTransmissionMatrix.
    Public newTransmissionMatrix As Boolean = True
    Public Sub New()
        Dim tMatrix() As Single = {1, 1, 0, 1}
        kf.TransitionMatrix = New cv.Mat(2, 2, cv.MatType.CV_32F, tMatrix)
        kf.MeasurementMatrix.SetIdentity(1)
        kf.ProcessNoiseCov.SetIdentity(0.00001)
        kf.MeasurementNoiseCov.SetIdentity(0.1)
        kf.ErrorCovPost.SetIdentity(1)
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim prediction = kf.Predict()
        measurement.Set(Of Single)(0, 0, inputReal)
        stateResult = kf.Correct(measurement).Get(Of Single)(0, 0)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

