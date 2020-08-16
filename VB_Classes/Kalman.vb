Imports cv = OpenCvSharp
'http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_Basics
    Inherits ocvbClass
    Dim kalman() As Kalman_Simple
    Public input() As Single
    Public output() As Single
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Turn Kalman filtering on" + If(ocvb.caller = "", "", " in " + ocvb.caller)
        check.Box(0).Checked = True

        ocvb.desc = "Use Kalman to stabilize a set of value (such as a cv.rect.)"
        input = New Single() {0, 0, 0, 0}
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static saveDimension As Int32 = -1
        If saveDimension <> input.Length Then
            If kalman IsNot Nothing Then
                If kalman.Count > 0 Then
                    For i = 0 To kalman.Count - 1
                        kalman(i).Dispose()
                    Next
                End If
            End If
            saveDimension = input.Length
            ReDim kalman(input.Length - 1)
            For i = 0 To input.Length - 1
                kalman(i) = New Kalman_Simple()
            Next
            ReDim output(input.Count - 1)
        End If

        If check.Box(0).Checked Then
            For i = 0 To kalman.Length - 1
                kalman(i).inputReal = input(i)
                kalman(i).Run(ocvb)
                If Double.IsNaN(kalman(i).stateResult) Then kalman(i).stateResult = kalman(i).inputReal ' kalman failure...
                output(i) = kalman(i).stateResult
            Next
        Else
            output = input ' do nothing to the input.
        End If

        If standalone Then
            dst1 = src.Clone()
            Dim rect = New cv.Rect(CInt(output(0)), CInt(output(1)), CInt(output(2)), CInt(output(3)))
            rect = validateRect(rect)
            Static lastRect = rect
            If rect = lastRect Then
                Dim r = initRandomRect(src.Width, src.Height, 50)
                input = New Single() {r.X, r.Y, r.Width, r.Height}
            End If
            lastRect = rect
            dst1.Rectangle(rect, cv.Scalar.White, 6)
            dst1.Rectangle(rect, cv.Scalar.Red, 1)
        End If
    End Sub
End Class





' http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_Compare
    Inherits ocvbClass
    Dim kalman() As Kalman_Single
    Public plot As Plot_OverTime
    Public kPlot As Plot_OverTime
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
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
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
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
        Dim d As Int32 = 3
        cv.Cv2.Line(dst1, New cv.Point(center.X - d, center.Y - d), New cv.Point(center.X + d, center.Y + d), color, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.Line(dst1, New cv.Point(center.X + d, center.Y - d), New cv.Point(center.X - d, center.Y + d), color, 1, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        label1 = "Estimate Yellow < Real Red (if working)"

        cv.Cv2.Randn(kState, New cv.Scalar(0), cv.Scalar.All(0.1))
        kf.TransitionMatrix = New cv.Mat(2, 2, cv.MatType.CV_32F, New Single() {1, 1, 0, 1})

        cv.Cv2.SetIdentity(kf.MeasurementMatrix)
        cv.Cv2.SetIdentity(kf.ProcessNoiseCov, cv.Scalar.All(0.00001))
        cv.Cv2.SetIdentity(kf.MeasurementNoiseCov, cv.Scalar.All(0.1))
        cv.Cv2.SetIdentity(kf.ErrorCovPost, cv.Scalar.All(1))
        cv.Cv2.Randn(kf.StatePost, New cv.Scalar(0), cv.Scalar.All(1))
        radius = ocvb.color.Rows / 2.4 ' so we see the entire circle...
        center = New cv.Point2f(ocvb.color.cols / 2, ocvb.color.Rows / 2)
        ocvb.desc = "Track a rotating point using a Kalman filter. Yellow line (estimate) should be shorter than red (real)."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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






'http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_MousePredict
    Inherits ocvbClass
    Dim kalman As Kalman_Basics
    Dim locMultiplier = 1
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.input(1)
        ReDim kalman.output(1)

        If ocvb.parms.resolution = resHigh Then locMultiplier = 2 ' twice the size in both dimensions.
        label1 = "Red is real mouse, white is prediction"
        ocvb.desc = "Use kalman filter to predict the next mouse location."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 100 = 0 Then dst1.SetTo(0)

        Static lastRealMouse = ocvb.mousePoint
        kalman.input(0) = ocvb.mousePoint.X
        kalman.input(1) = ocvb.mousePoint.Y
        Dim lastStateResult = New cv.Point(kalman.output(0), kalman.output(1))
        kalman.Run(ocvb)
        cv.Cv2.Line(dst1, New cv.Point(lastStateResult.X, lastStateResult.Y) * locMultiplier,
                                  New cv.Point(kalman.output(0), kalman.output(1)) * locMultiplier,
                                  cv.Scalar.All(255), 1, cv.LineTypes.AntiAlias)
        cv.Cv2.Line(dst1, ocvb.mousePoint * locMultiplier, lastRealMouse * locMultiplier, New cv.Scalar(0, 0, 255), 1, cv.LineTypes.AntiAlias)
        lastRealMouse = ocvb.mousePoint
    End Sub
End Class







Public Class Kalman_CVMat
    Inherits ocvbClass
    Dim kalman() As Kalman_Simple
    Public input As cv.Mat
    Public output As cv.Mat
    Dim basics As Kalman_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        basics = New Kalman_Basics(ocvb)
        input = New cv.Mat(4, 1, cv.MatType.CV_32F, 0)
        If standalone Then label1 = "Rectangle moves smoothly to random locations"
        ocvb.desc = "Use Kalman to stabilize a set of values such as a cv.rect or cv.Mat"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static saveDimension As Int32 = -1
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
    Inherits ocvbClass
    Dim kalman As Kalman_CVMat
    Dim resize As Resize_Percentage
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        kalman = New Kalman_CVMat(ocvb)

        resize = New Resize_Percentage(ocvb)

        label1 = "The small image is processed by the Kalman filter"
        label2 = "Mask of the smoothed image minus original"
        ocvb.desc = "Resize the image to allow the Kalman filter to process the whole image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
    Dim kalman As Kalman_ImageSmall
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        kalman = New Kalman_ImageSmall(ocvb)

        label1 = "Mask of non-zero depth after Kalman smoothing"
        label2 = "Mask of the smoothed image minus original"
        ocvb.desc = "Use a resized depth Mat to find where depth is decreasing (something getting closer.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        kalman.src = ocvb.RGBDepth
        kalman.Run(ocvb)
        dst1 = kalman.dst1
        dst2 = kalman.dst2
    End Sub
End Class







Public Class Kalman_Depth32f
    Inherits ocvbClass
    Dim kalman As Kalman_CVMat
    Dim resize As Resize_Percentage
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        kalman = New Kalman_CVMat(ocvb)

        resize = New Resize_Percentage(ocvb)
        resize.sliders.trackbar(0).Value = 4

        label1 = "Mask of non-zero depth after Kalman smoothing"
        label2 = "Difference from original depth"
        ocvb.desc = "Use a resized depth Mat to find where depth is decreasing (getting closer.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
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
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        Dim tMatrix() As Single = {1, 1, 0, 1}
        kf.TransitionMatrix = New cv.Mat(2, 2, cv.MatType.CV_32F, tMatrix)
        kf.MeasurementMatrix.SetIdentity(1)
        kf.ProcessNoiseCov.SetIdentity(0.00001)
        kf.MeasurementNoiseCov.SetIdentity(0.1)
        kf.ErrorCovPost.SetIdentity(1)

        ocvb.desc = "Estimate a single value using a Kalman Filter - in the default case, the value of the mean of the grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            If ocvb.frameCount = 0 Then
                plot = New Plot_OverTime(ocvb)
                plot.maxScale = 150
                plot.minScale = 80
                plot.plotCount = 2
            End If

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
    Public Sub Run(ocvb As AlgorithmData)
        Dim prediction = kf.Predict()
        measurement.Set(Of Single)(0, 0, inputReal)
        stateResult = kf.Correct(measurement).Get(Of Single)(0, 0)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class






Public Class Kalman_Centroids
    Inherits ocvbClass
    Dim knn As KNN_CentroidsEMax
    Dim kalman(0) As Kalman_Basics
    Dim newQueries As New List(Of cv.Point2f)
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        knn = New KNN_CentroidsEMax(ocvb)

        label1 = "Centroids in yellow"
        label2 = "Original EMax output - unregistered colors"
        ocvb.desc = "Use Kalman to stabilize the EMax Centroids"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static useKalmanCheck As Windows.Forms.CheckBox
        Dim kalmanActive = useKalmanCheck?.Checked

        Dim trainingPoints = New List(Of cv.Point2f)(knn.emax.centroids)
        If trainingPoints.Count = 0 Then
            knn.Run(ocvb)
            Exit Sub ' first pass has no training data.
        End If

        ' allocate the kalman filters for each centroid with some additional filters for objects that come and go...
        If kalman.Length < trainingPoints.Count Then
            ReDim kalman(trainingPoints.Count + 10) ' pad a little to keep more info
            For i = 0 To kalman.Count - 1
                If i > 0 Then ocvb.suppressOptions = True
                kalman(i) = New Kalman_Basics(ocvb)
                If i < trainingPoints.Count Then
                    kalman(i).input = New Single() {trainingPoints(i).X, trainingPoints(i).Y}
                Else
                    kalman(i).input = New Single() {-1, -1}
                End If
            Next
            ocvb.suppressOptions = False
            useKalmanCheck = findCheckBox("Turn Kalman filtering on")
        End If

        knn.basics.trainingPoints.Clear()
        For i = 0 To kalman.Count - 1
            If kalman(i).input(0) >= 0 Then knn.basics.trainingPoints.Add(New cv.Point2f(kalman(i).input(0), kalman(i).input(1)))
        Next

        If newQueries.Count > 0 Then
            ' when the queries outnumber the trainingpoints and we are 1:1, some new queries can appear.
            Dim qIndex As Integer
            For i = knn.basics.trainingPoints.Count To kalman.Count - 1
                If qIndex >= newQueries.Count Then Exit For
                knn.basics.trainingPoints.Add(newQueries(qIndex))
                kalman(i).input = {newQueries(qIndex).X, newQueries(qIndex).Y}
                qIndex += 1
                If qIndex >= kalman.Count Then ' we don't have enough kalman filters to handle this level of queries so restart
                    ReDim kalman(0)
                    Exit Sub
                End If
            Next
        End If

        knn.Run(ocvb)
        dst1 = knn.dst1
        dst2 = knn.emax.emaxCPP.dst2

        For i = 0 To knn.basics.matchedPoints.Count - 1
            If knn.basics.matchedPoints(i).X < 0 Then
                For j = 0 To kalman.Count - 1
                    If kalman(j).input(0) < 0 Then
                        kalman(j).input = {knn.basics.queryPoints(i).X, knn.basics.queryPoints(i).Y}
                        Exit For
                    End If
                Next
            End If
        Next


        For i = 0 To knn.basics.trainingPoints.Count - 1
            Dim pt1 = knn.basics.trainingPoints(i)
            For j = 0 To knn.basics.matchedPoints.Count - 1
                Dim pt2 = knn.basics.matchedPoints(j)
                If pt1 = pt2 Then
                    kalman(i).input = {knn.basics.queryPoints(j).X, knn.basics.queryPoints(j).Y}
                    If kalmanActive Then
                        kalman(i).Run(ocvb)
                    Else
                        kalman(i).output = {knn.basics.queryPoints(j).X, knn.basics.queryPoints(j).Y}
                    End If
                    Dim pt3 = New cv.Point(kalman(i).output(0), kalman(i).output(1))
                    cv.Cv2.Circle(dst1, pt3, 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias, 0)
                    Exit For
                End If
            Next
        Next

        newQueries.Clear()
        For i = 0 To knn.basics.matchedPoints.Count - 1
            Dim pt1 = knn.basics.matchedPoints(i)
            If pt1.X = -1 And pt1.X = -1 Then newQueries.Add(pt1)
        Next
    End Sub
End Class







Public Class Kalman_PointTracker
    Inherits ocvbClass
    Dim knn As KNN_Basics
    Dim kalman(0) As Kalman_Basics
    Dim newObjects As New List(Of cv.Point2f)
    Dim topView As PointCloud_Centroids_TopView
    Dim kalmanAging() As Integer
    Dim lastMask() As cv.Mat
    Public maskAvailable As Boolean = True
    Public queryPoints As New List(Of cv.Point2f)
    Public queryRects As New List(Of cv.Rect)
    Public queryMasks As New List(Of cv.Mat)

    Public matchedMasks As New List(Of cv.Mat)
    Public matchedRects As New List(Of cv.Rect)
    Public matchedPoints As New List(Of cv.Point2f)
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        If standalone Then topView = New PointCloud_Centroids_TopView(ocvb)

        If standalone Then ocvb.suppressOptions = True
        knn = New KNN_Basics(ocvb)
        If standalone Then ocvb.suppressOptions = False

        ocvb.desc = "Use KNN to track points and Kalman to smooth the results"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            topView.Run(ocvb)
            dst1 = topView.dst1
            Exit Sub
        End If
        Static useKalmanCheck As Windows.Forms.CheckBox

        Dim trainingPoints = New List(Of cv.Point2f)(queryPoints)
        If ocvb.frameCount = 0 Then
            knn.Run(ocvb)
            Exit Sub ' first pass has no training data.
        End If

        ' allocate the kalman filters for each centroid with some additional filters for objects that come and go...
        If kalman.Length < trainingPoints.Count Then
            ReDim kalman(trainingPoints.Count + 10) ' pad a little to keep more info
            ReDim kalmanAging(kalman.Count - 1)
            ReDim lastMask(kalman.Count - 1)
            For i = 0 To kalman.Count - 1
                If i > 0 Then ocvb.suppressOptions = True
                kalman(i) = New Kalman_Basics(ocvb)
                If i < trainingPoints.Count Then
                    kalman(i).input = New Single() {trainingPoints(i).X, trainingPoints(i).Y, 0, 0, 0, 0}
                Else
                    kalman(i).input = New Single() {-1, -1, 0, 0, 0, 0}
                End If
            Next
            ocvb.suppressOptions = False
            useKalmanCheck = findCheckBox("Turn Kalman filtering on") ' we left one of these visible...
        End If
        Dim kalmanActive = useKalmanCheck?.Checked

        knn.trainingPoints.Clear()
        For i = 0 To kalman.Count - 1
            If kalman(i).input(0) >= 0 Then knn.trainingPoints.Add(New cv.Point2f(kalman(i).input(0), kalman(i).input(1)))
        Next

        If newObjects.Count > 0 Then
            ' when the queries outnumber the trainingpoints and we are 1:1, some new queries can appear.
            Dim qIndex As Integer
            For i = knn.trainingPoints.Count To kalman.Count - 1
                If qIndex >= newObjects.Count Then Exit For
                knn.trainingPoints.Add(newObjects(qIndex))
                kalman(i).input = {newObjects(qIndex).X, newObjects(qIndex).Y, 0, 0, 0, 0}
                qIndex += 1
                If qIndex >= kalman.Count Then ' we don't have enough kalman filters to handle this level of queries so restart
                    ReDim kalman(0)
                    Exit Sub
                End If
            Next
        End If

        knn.queryPoints = New List(Of cv.Point2f)(queryPoints)
        knn.Run(ocvb)

        For i = 0 To knn.matchedPoints.Count - 1
            If knn.matchedPoints(i).X < 0 Then
                For j = 0 To kalman.Count - 1
                    If kalman(j).input(0) < 0 Then
                        kalman(j).input = {knn.queryPoints(i).X, knn.queryPoints(i).Y, 0, 0, 0, 0}
                        Exit For
                    End If
                Next
            End If
        Next

        dst1.SetTo(0)
        Dim matched As Boolean
        Dim rect = New cv.Rect
        matchedMasks.Clear()
        matchedRects.Clear()
        matchedPoints.Clear()
        For i = 0 To knn.trainingPoints.Count - 1
            Dim pt1 = knn.trainingPoints(i)
            For j = 0 To knn.matchedPoints.Count - 1
                matched = False
                Dim pt2 = knn.matchedPoints(j)
                If pt1 = pt2 Then
                    matched = True
                    kalmanAging(i) = 5 ' if not found for x generations, then discard this kalman filter.
                    rect = New cv.Rect(kalman(i).input(2), kalman(i).input(3), kalman(i).input(4), kalman(i).input(5))
                    Dim qpt = knn.queryPoints(j)
                    For k = 0 To queryRects.Count - 1
                        If queryPoints(k) = qpt Then
                            rect = queryRects(k)
                            If maskAvailable Then lastMask(i) = queryMasks(k)
                            Exit For
                        End If
                    Next
                    kalman(i).input = {knn.queryPoints(j).X, knn.queryPoints(j).Y, rect.X, rect.Y, rect.Width, rect.Height}
                    If kalmanActive Then kalman(i).Run(ocvb) Else kalman(i).output = {knn.queryPoints(j).X, knn.queryPoints(j).Y, rect.X, rect.Y, rect.Width, rect.Height}
                    Exit For
                End If
            Next

            ' if the trainingpoint was not found, then just run and plot the results again...
            If matched = False Then
                If kalmanAging(i) > 0 And kalmanActive Then
                    rect = New cv.Rect(kalman(i).input(2), kalman(i).input(3), kalman(i).input(4), kalman(i).input(5))
                    matched = True
                    kalman(i).Run(ocvb)
                End If
                kalmanAging(i) -= 1
            End If

            If matched And kalman(i).output IsNot Nothing Then
                Dim pt3 = New cv.Point(kalman(i).output(0), kalman(i).output(1))
                If rect.Width = lastMask(i).Cols And rect.Height = lastMask(i).Rows Then dst1(rect).SetTo(scalarColors(i), lastMask(i))
                cv.Cv2.Circle(dst1, pt3, 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias, 0)
                rect = New cv.Rect(kalman(i).output(2), kalman(i).output(3), kalman(i).output(4), kalman(i).output(5))

                Static drawRectangleCheck = findCheckBox("Draw rectangle for each mask")
                If drawRectangleCheck.checked Then dst1.Rectangle(rect, scalarColors(i), 1)
                matchedRects.Add(rect)
                matchedMasks.Add(lastMask(i))
                matchedPoints.Add(pt3)
            End If
        Next

        newObjects.Clear()
        For i = 0 To knn.matchedPoints.Count - 1
            Dim pt1 = knn.matchedPoints(i)
            If pt1.X = -1 And pt1.X = -1 Then newObjects.Add(pt1)
        Next
    End Sub
End Class