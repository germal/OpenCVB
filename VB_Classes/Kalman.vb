Imports cv = OpenCvSharp
' http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_Basics : Implements IDisposable
    Public sliders As New OptionsSliders
    Public inputReal As New cv.Point3f
    Public lastStatePoint As New cv.Point3f
    Public statePoint As New cv.Point3f
    Public ProcessNoiseCov = cv.Scalar.All(100 / 10000)
    Public MeasurementNoiseCov = cv.Scalar.All(10)
    Public ErrorCovPost = cv.Scalar.All(10 / 100)
    Public prediction As cv.Mat

    Public externalUse As Boolean
    Public restartRequested As Boolean

    Public kf As cv.KalmanFilter
    Public measurement As New cv.Mat(3, 1, cv.MatType.CV_32F, 0)
    Public plot As Plot_OverTime
    Public kPlot As Plot_OverTime
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "ProcessNoiseCov x10000", 1, 1000, 100)
        sliders.setupTrackBar2(ocvb, "MeasurementNoiseCov", 1, 100, 10)
        sliders.setupTrackBar3(ocvb, "ErrorCovPost x100", 1, 100, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()

        plot = New Plot_OverTime(ocvb)
        plot.externalUse = True
        plot.dst = ocvb.result1
        plot.plotCount = 3

        kPlot = New Plot_OverTime(ocvb)
        kPlot.externalUse = True
        kPlot.dst = ocvb.result2
        kPlot.plotCount = 3

        ocvb.label1 = "Kalman input: mean values for RGB"
        ocvb.label2 = "Kalman output: smoothed mean values for RGB"
        ocvb.desc = "Use this kalman filter to predict the next value."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount = 0 Or restartRequested Then
            restartRequested = False
            kf = New cv.KalmanFilter(6, 3, 0)
#If opencvsharpOld Then
            kf.TransitionMatrix.SetArray(0, 0, New Single() {1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1})
            kf.StatePre.SetArray(0, 0, New Single() {0, 0, 0, 0})
#Else
            kf.TransitionMatrix = New cv.Mat(16, 1, cv.MatType.CV_32F, New Single() {1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1})
            kf.StatePre = New cv.Mat(4, 1, cv.MatType.CV_32F, New Single() {0, 0, 0, 0})
#End If
            cv.Cv2.SetIdentity(kf.MeasurementMatrix)
            cv.Cv2.SetIdentity(kf.ProcessNoiseCov, ProcessNoiseCov)
            cv.Cv2.SetIdentity(kf.MeasurementNoiseCov, MeasurementNoiseCov)
            cv.Cv2.SetIdentity(kf.ErrorCovPost, ErrorCovPost)
        End If

        If externalUse = False Then
            plot.plotData = ocvb.color.Mean()
            plot.Run(ocvb)
            inputReal.X = plot.plotData.Item(0)
            inputReal.Y = plot.plotData.Item(1)
            inputReal.Z = plot.plotData.Item(2)
        End If
        ProcessNoiseCov = cv.Scalar.All(sliders.TrackBar1.Value / 10000)
        MeasurementNoiseCov = cv.Scalar.All(sliders.TrackBar2.Value)
        ErrorCovPost = cv.Scalar.All(sliders.TrackBar3.Value / 100)

        prediction = kf.Predict()
        measurement.Set(Of Single)(0, 0, inputReal.X)
        measurement.Set(Of Single)(0, 1, inputReal.Y)
        measurement.Set(Of Single)(0, 2, inputReal.Z)
        Dim estimated = kf.Correct(measurement)
        statePoint = New cv.Point3f(estimated.At(Of Single)(0), estimated.At(Of Single)(1), estimated.At(Of Single)(2))
        If externalUse = False Then
            kPlot.maxScale = plot.maxScale ' keep the scale the same for the side-by-side plots.
            kPlot.minScale = plot.minScale
            kPlot.plotData = New cv.Scalar(statePoint.X, statePoint.Y, statePoint.Z)
            kPlot.Run(ocvb)
        End If
        ' if either one has triggered a reset for the scale, do them both...
        If kPlot.offChartCount = 0 Or plot.offChartCount = 0 Then
            kPlot.offChartCount = kPlot.plotTriggerRescale + 1
            plot.offChartCount = plot.plotTriggerRescale + 1
        End If
        lastStatePoint = statePoint
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        plot.Dispose()
        kPlot.Dispose()
    End Sub
End Class






'http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_MousePredict : Implements IDisposable
    Dim kf As Kalman_Point2f
    Public Sub New(ocvb As AlgorithmData)
        kf = New Kalman_Point2f(ocvb)
        kf.externalUse = True

        ocvb.label1 = "Red is real mouse, white is prediction"
        ocvb.desc = "Use kalman filter to predict the next mouse location."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 100 = 0 Then ocvb.result1.SetTo(0)

        Static lastRealMouse = ocvb.mousePoint
        kf.inputReal = New cv.Point2f(ocvb.mousePoint.X, ocvb.mousePoint.Y)
        Dim lastStateResult = kf.lastStateResult
        kf.Run(ocvb)
        cv.Cv2.Line(ocvb.result1, New cv.Point(lastStateResult.X, lastStateResult.Y), New cv.Point(kf.stateResult.X, kf.stateResult.Y), cv.Scalar.All(255), 1, cv.LineTypes.AntiAlias)
        cv.Cv2.Line(ocvb.result1, ocvb.mousePoint, lastRealMouse, New cv.Scalar(0, 0, 255), 1, cv.LineTypes.AntiAlias)
        lastRealMouse = ocvb.mousePoint
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        kf.Dispose()
    End Sub
End Class


'https://github.com/opencv/opencv/blob/master/samples/cpp/kalman.cpp
Public Class Kalman_RotatingPoint : Implements IDisposable
    Dim kf As New cv.KalmanFilter(2, 1, 0)
    Dim kState As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Dim processNoise As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Dim measurement As New cv.Mat(1, 1, cv.MatType.CV_32F, 0)
    Dim center As cv.Point2f, statePt As cv.Point2f
    Dim radius As Single
    Private Function calcPoint(center As cv.Point2f, R As Double, angle As Double) As cv.Point
        Return center + New cv.Point2f(Math.Cos(angle), -Math.Sin(angle)) * R
    End Function
    Private Sub drawCross(dst As cv.Mat, center As cv.Point, color As cv.Scalar)
        Dim d As Int32 = 3
        cv.Cv2.Line(dst, New cv.Point(center.X - d, center.Y - d), New cv.Point(center.X + d, center.Y + d), color, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.Line(dst, New cv.Point(center.X + d, center.Y - d), New cv.Point(center.X - d, center.Y + d), color, 1, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "Estimate Yellow < Real Red (if working)"

        cv.Cv2.Randn(kState, New cv.Scalar(0), cv.Scalar.All(0.1))
#If opencvsharpOld Then
        kf.TransitionMatrix.SetArray(0, 0, New Single() {1, 1, 0, 1}.ToArray)
#Else
        kf.TransitionMatrix = New cv.Mat(4, 1, cv.MatType.CV_32F, New Single() {1, 1, 0, 1})
#End If

        cv.Cv2.SetIdentity(kf.MeasurementMatrix)
        cv.Cv2.SetIdentity(kf.ProcessNoiseCov, cv.Scalar.All(0.00001))
        cv.Cv2.SetIdentity(kf.MeasurementNoiseCov, cv.Scalar.All(0.1))
        cv.Cv2.SetIdentity(kf.ErrorCovPost, cv.Scalar.All(1))
        cv.Cv2.Randn(kf.StatePost, New cv.Scalar(0), cv.Scalar.All(1))
        radius = ocvb.color.Rows / 2.4 ' so we see the entire circle...
        center = New cv.Point2f(ocvb.color.Cols / 2, ocvb.color.Height / 2)
        ocvb.desc = "Track a rotating point using a Kalman filter. Yellow line (estimate) should be shorter than red (real)."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim stateAngle = kState.At(Of Single)(0)

        Dim prediction = kf.Predict()
        Dim predictAngle = prediction.At(Of Single)(0)
        Dim predictPt = calcPoint(center, radius, predictAngle)
        statePt = calcPoint(center, radius, stateAngle)

        cv.Cv2.Randn(measurement, New cv.Scalar(0), cv.Scalar.All(kf.MeasurementNoiseCov.At(Of Single)(0)))

        measurement += kf.MeasurementMatrix * kState
        Dim measAngle = measurement.At(Of Single)(0)
        Dim measPt = calcPoint(center, radius, measAngle)

        ocvb.result1.SetTo(0)
        drawCross(ocvb.result1, statePt, cv.Scalar.White)
        drawCross(ocvb.result1, measPt, cv.Scalar.White)
        drawCross(ocvb.result1, predictPt, cv.Scalar.White)
        cv.Cv2.Line(ocvb.result1, statePt, measPt, New cv.Scalar(0, 0, 255), 3, cv.LineTypes.AntiAlias)
        cv.Cv2.Line(ocvb.result1, statePt, predictPt, New cv.Scalar(0, 255, 255), 3, cv.LineTypes.AntiAlias)

        If ocvb.ms_rng.Next(0, 4) <> 0 Then kf.Correct(measurement)

        cv.Cv2.Randn(processNoise, cv.Scalar.Black, cv.Scalar.All(Math.Sqrt(kf.ProcessNoiseCov.At(Of Single)(0, 0))))
        kState = kf.TransitionMatrix * kState + processNoise
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class


Public Class Kalman_RGBGrid_MT1 : Implements IDisposable
    Public grid As Thread_Grid
    Dim kalman() As Kalman_kDimension
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 64
        grid.externalUse = True

        ocvb.desc = "Use Kalman to stabilize pixel values."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static lastGray As New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U, 0)
        grid.Run(ocvb)
        Static gridWidth As Int32
        Static gridHeight As Int32

        If gridWidth <> grid.sliders.TrackBar1.Value Or gridHeight <> grid.sliders.TrackBar2.Value Then
            gridWidth = grid.sliders.TrackBar1.Value
            gridHeight = grid.sliders.TrackBar2.Value
            ReDim kalman(grid.roiList.Count - 1)
            For i = 0 To grid.roiList.Count - 1
                kalman(i) = New Kalman_kDimension(ocvb)
                kalman(i).kDimension = grid.roiList(i).Width * grid.roiList(i).Height
            Next
        End If

        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.result1 = gray.Clone()
        If ocvb.frameCount > 0 Then
            Parallel.For(0, grid.roiList.Count - 1,
           Sub(i)
               Dim roi = grid.roiList(i)
               Dim grayDiff As New cv.Mat, gray32f As New cv.Mat
               cv.Cv2.Absdiff(gray(roi), lastGray(roi), grayDiff)
               grayDiff.ConvertTo(gray32f, cv.MatType.CV_32F)
               Dim learnInput = gray32f.Clone()
               kalman(i).inputReal = learnInput.Reshape(1, roi.Width * roi.Height)
               kalman(i).Run(ocvb)
               If kalman(i).stateResult.Width > 0 Then
                   learnInput = kalman(i).stateResult.Clone()
                   gray32f = learnInput.Reshape(1, roi.Height)
               End If
               gray32f.ConvertTo(grayDiff, cv.MatType.CV_8U)
               cv.Cv2.Add(gray(roi), grayDiff, ocvb.result1(roi))
           End Sub)
        End If
        lastGray = gray.Clone()
        ocvb.result2 = ocvb.result1.Threshold(254, 255, cv.ThresholdTypes.Binary)
        ocvb.label1 = "Kalman stabilized grayscale image"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
        If kalman IsNot Nothing Then
            For i = 0 To kalman.Count - 1
                kalman(i).Dispose()
            Next
        End If
    End Sub
End Class


' http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_Point2f : Implements IDisposable
    Public sliders As New OptionsSliders
    Public inputReal As New cv.Point2f
    Public lastStateResult As New cv.Point2f
    Public stateResult As New cv.Point2f
    Public ProcessNoiseCov = cv.Scalar.All(100 / 10000)
    Public MeasurementNoiseCov = cv.Scalar.All(10)
    Public ErrorCovPost = cv.Scalar.All(10 / 100)
    Public prediction As cv.Mat

    Public externalUse As Boolean
    Public restartRequested As Boolean

    Public kf As New cv.KalmanFilter(4, 2, 0)
    Public measurement As New cv.Mat(2, 1, cv.MatType.CV_32F, 0)
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "ProcessNoiseCov x10000", 1, 1000, 100)
        sliders.setupTrackBar2(ocvb, "MeasurementNoiseCov", 1, 100, 10)
        sliders.setupTrackBar3(ocvb, "ErrorCovPost x100", 1, 100, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.label1 = "Kalman_Basics (no output by default)"
        ocvb.desc = "Use this kalman filter to predict the next value."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount = 0 Or restartRequested Then
            restartRequested = False
#If opencvsharpOld Then
            kf.TransitionMatrix.SetArray(0, 0, New Single() {1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1})
            kf.StatePre.SetArray(0, 0, New Single() {0, 0, 0, 0})
#Else
            kf.TransitionMatrix = New cv.Mat(16, 1, cv.MatType.CV_32F, New Single() {1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1})
            kf.StatePre = New cv.Mat(4, 1, cv.MatType.CV_32F, New Single() {0, 0, 0, 0})
#End If

            cv.Cv2.SetIdentity(kf.MeasurementMatrix)
            cv.Cv2.SetIdentity(kf.ProcessNoiseCov, ProcessNoiseCov)
            cv.Cv2.SetIdentity(kf.MeasurementNoiseCov, MeasurementNoiseCov)
            cv.Cv2.SetIdentity(kf.ErrorCovPost, ErrorCovPost)
        End If

        ProcessNoiseCov = cv.Scalar.All(sliders.TrackBar1.Value / 10000)
        MeasurementNoiseCov = cv.Scalar.All(sliders.TrackBar2.Value)
        ErrorCovPost = cv.Scalar.All(sliders.TrackBar3.Value / 100)

        prediction = kf.Predict()
        measurement.Set(Of Single)(0, 0, inputReal.X)
        measurement.Set(Of Single)(0, 1, inputReal.Y)
        Dim estimated = kf.Correct(measurement)
        stateResult = New cv.Point(estimated.At(Of Single)(0), estimated.At(Of Single)(1))
        lastStateResult = stateResult
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class







Public Class Kalman_GeneralPurpose_Options : Implements IDisposable
    Public sliders As New OptionsSliders
    Public radio As New OptionsRadioButtons
    Public kf As Kalman_Single
    Public Sub New(ocvb As AlgorithmData)
        kf = New Kalman_Single(ocvb) ' avoid using the externalUse because then it will run the mean calculation and smoothing...

        sliders.setupTrackBar1(ocvb, "ProcessNoiseCov x10000", 1, 1000, 100)
        sliders.setupTrackBar2(ocvb, "MeasurementNoiseCov x100", 1, 100, 10)
        sliders.setupTrackBar3(ocvb, "ErrorCovPost", 1, 100, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()

        radio.Setup(ocvb, 7)
        radio.check(0).Text = "1,0,1,0 transition matrix"
        radio.check(1).Text = "1,1,1,0 transition matrix"
        radio.check(2).Text = "1,0,1,1 transition matrix"
        radio.check(3).Text = "0,0,1,0 transition matrix"
        radio.check(4).Text = "1,1,0,1 transition matrix"
        radio.check(5).Text = "0,0,0,1 transition matrix"
        radio.check(6).Text = "0,1,1,0 transition matrix"
        radio.check(4).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()

        ocvb.desc = "Use this to experiment with the options for a kalman filter."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        kf.ProcessNoiseCov = cv.Scalar.All(sliders.TrackBar1.Value / 10000)
        kf.MeasurementNoiseCov = cv.Scalar.All(sliders.TrackBar2.Value / 100)
        kf.ErrorCovPost = cv.Scalar.All(sliders.TrackBar3.Value)

        Static saveTransitionMatrixIndex As Int32
        If radio.check(saveTransitionMatrixIndex).Checked = False Then
            Dim tMatrixStr As String = ""
            For i = 0 To radio.check.Length - 1
                If radio.check(i).Checked Then
                    tMatrixStr = radio.check(i).Text
                    saveTransitionMatrixIndex = i
                    Exit For
                End If
            Next
            tMatrixStr = tMatrixStr.Substring(0, InStr(tMatrixStr, " ") - 1)
            Dim tm = tMatrixStr.Split(",")
            kf.transitionMatrix = New Single() {tm(0), tm(1), tm(2), tm(3)}
            kf.newTransmissionMatrix = True
        End If

        kf.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
        kf.Dispose()
    End Sub
End Class






Public Class Kalman_kDimension : Implements IDisposable
    Public inputReal As New cv.Mat
    Public stateResult As New cv.Mat
    Public ProcessNoiseCov = cv.Scalar.All(100 / 10000)
    Public MeasurementNoiseCov = cv.Scalar.All(10)
    Public ErrorCovPost = cv.Scalar.All(10 / 100)
    Public prediction As New cv.Mat

    Public externalUse As Boolean
    Public restartRequested As Boolean = True
    Public kDimension As Int32 = 50
    Public kf As cv.KalmanFilter
    Public measurement As cv.Mat
    Public transitionMatrix(3) As Single ' default to all zeros for the transition matrix.  Change the transition matrix externally and set restartRequested.
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "Kalman_kDimension (no output by default)"
        ocvb.desc = "Use this kalman filter to predict the set of k values."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static initRun As Boolean = True
        Static saveKDimension As Int32
        If restartRequested Or kDimension <> saveKDimension Then
            saveKDimension = kDimension
            restartRequested = False
            kf = New cv.KalmanFilter(kDimension * 2, kDimension, 0)
#If opencvsharpOld Then
            kf.TransitionMatrix.SetArray(0, 0, New Single() {transitionMatrix(0), transitionMatrix(1), transitionMatrix(2), transitionMatrix(3)})
#Else
            kalman.TransitionMatrix = New cv.Mat(4, 1, cv.MatType.CV_32F, transitionMatrix)
            kalman.StatePre = New cv.Mat(4, 1, cv.MatType.CV_32F, New Single() {0, 0, 0, 0})
#End If
            cv.Cv2.SetIdentity(kf.MeasurementMatrix)
            cv.Cv2.SetIdentity(kf.ProcessNoiseCov, ProcessNoiseCov)
            cv.Cv2.SetIdentity(kf.MeasurementNoiseCov, MeasurementNoiseCov)
            cv.Cv2.SetIdentity(kf.ErrorCovPost, ErrorCovPost)
        End If

        prediction = kf.Predict()
        measurement = inputReal.Clone()
        stateResult = kf.Correct(measurement).RowRange(0, kDimension).Clone()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class








Public Class Kalman_GeneralPurpose : Implements IDisposable
    Dim kalman() As Kalman_Single
    Public src() As Single
    Public dst() As Single
    Public externalUse As Boolean
    Public ProcessNoiseCov As Single = 0.00001
    Public MeasurementNoiseCov As Single = 0.1
    Public ErrorCovPost As Single = 1

    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "Rectangle moves smoothly from random locations"
        ocvb.desc = "Use Kalman to stabilize a set of value (such as a cv.rect.)"
    End Sub
    Private Sub setValues(ocvb As AlgorithmData)
        Static autoRand As New Random()
        ReDim src(3)
        src(0) = autoRand.Next(50, ocvb.color.Width - 50)
        src(1) = autoRand.Next(50, ocvb.color.Height - 50)
        src(2) = autoRand.Next(5, ocvb.color.Width - src(0))
        src(3) = autoRand.Next(5, ocvb.color.Height - src(1))
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static saveProcessNoiseCov As Single
        Static saveMeasurementNoiseCov As Single
        Static saveErrorCovPost As Single
        If ProcessNoiseCov <> saveProcessNoiseCov Then
            saveProcessNoiseCov = ProcessNoiseCov
            saveMeasurementNoiseCov = MeasurementNoiseCov
            saveErrorCovPost = ErrorCovPost
        End If

        If src Is Nothing Then setValues(ocvb)
        Static saveDimension As Int32 = -1
        If saveDimension <> src.Length Then
            If kalman IsNot Nothing Then
                If kalman.Count > 0 Then
                    For i = 0 To kalman.Count - 1
                        kalman(i).Dispose()
                    Next
                End If
            End If
            saveDimension = src.Length
            ReDim kalman(src.Length - 1)
            For i = 0 To src.Length - 1
                kalman(i) = New Kalman_Single(ocvb)
                kalman(i).externalUse = True
                kalman(i).ProcessNoiseCov = 0.00001
                kalman(i).MeasurementNoiseCov = 0.1
                kalman(i).ErrorCovPost = 1
            Next
            ReDim dst(src.Count - 1)
        End If

        For i = 0 To kalman.Length - 1
            kalman(i).inputReal = src(i)
            kalman(i).Run(ocvb)
        Next

        For i = 0 To src.Length - 1
            dst(i) = kalman(i).stateResult
        Next

        If externalUse = False Then
            ocvb.result1 = ocvb.color
            Static rect As New cv.Rect(CInt(dst(0)), CInt(dst(1)), CInt(dst(2)), CInt(dst(3)))
            If rect.X = CInt(dst(0)) And rect.Y = CInt(dst(1)) And rect.Width = CInt(dst(2)) And rect.Height = CInt(dst(3)) Then
                setValues(ocvb)
            Else
                rect = New cv.Rect(CInt(dst(0)), CInt(dst(1)), CInt(dst(2)), CInt(dst(3)))
            End If
            ocvb.result1.Rectangle(rect, cv.Scalar.Red, 2)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If kalman IsNot Nothing Then
            For i = 0 To kalman.Count - 1
                kalman(i).Dispose()
            Next
        End If
    End Sub
End Class







Public Class Kalman_CVMat : Implements IDisposable
    Dim kalman() As Kalman_Single
    Public src As cv.Mat
    Public dst As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "Rectangle moves smoothly from random locations"
        ocvb.desc = "Use Kalman to stabilize a set of values (such as a cv.rect.)"
    End Sub
    Private Sub setValues(ocvb As AlgorithmData)
        Static autoRand As New Random()
        Dim x = autoRand.Next(50, ocvb.color.Width - 50)
        Dim y = autoRand.Next(50, ocvb.color.Height - 50)
        Dim vals() As Single = {x, y, autoRand.Next(5, ocvb.color.Width - x), autoRand.Next(5, ocvb.color.Height - y)}
        src = New cv.Mat(4, 1, cv.MatType.CV_32F, vals)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src Is Nothing Then setValues(ocvb)
        Static saveDimension As Int32 = -1
        If saveDimension <> src.Rows Then
            If kalman IsNot Nothing Then
                If kalman.Count > 0 Then
                    For i = 0 To kalman.Count - 1
                        kalman(i).Dispose()
                    Next
                End If
            End If
            saveDimension = src.Rows
            ReDim kalman(src.Rows - 1)
            For i = 0 To src.Rows - 1
                kalman(i) = New Kalman_Single(ocvb)
                kalman(i).externalUse = True
            Next
            dst = New cv.Mat(src.Rows, 1, cv.MatType.CV_32F, 0)
        End If

        For i = 0 To kalman.Length - 1
            kalman(i).inputReal = src.At(Of Single)(i, 0)
            kalman(i).Run(ocvb)
        Next

        For i = 0 To src.Rows - 1
            dst.Set(Of Single)(i, 0, kalman(i).stateResult)
        Next

        If externalUse = False Then
            Dim rx(src.Rows - 1) As Single
            Dim testrect As New cv.Rect
            For i = 0 To src.Rows - 1
                rx(i) = dst.At(Of Single)(i, 0)
            Next
            ocvb.result1 = ocvb.color
            Static rect As New cv.Rect(CInt(rx(0)), CInt(rx(1)), CInt(rx(2)), CInt(rx(3)))
            If rect.X = CInt(rx(0)) And rect.Y = CInt(rx(1)) And rect.Width = CInt(rx(2)) And rect.Height = CInt(rx(3)) Then
                setValues(ocvb)
            Else
                rect = New cv.Rect(CInt(rx(0)), CInt(rx(1)), CInt(rx(2)), CInt(rx(3)))
            End If
            ocvb.result1.Rectangle(rect, cv.Scalar.Red, 2)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If kalman IsNot Nothing Then
            For i = 0 To kalman.Count - 1
                kalman(i).Dispose()
            Next
        End If
    End Sub
End Class





Public Class Kalman_Image : Implements IDisposable
    Dim kalman As Kalman_CVMat
    Public Sub New(ocvb As AlgorithmData)
        kalman = New Kalman_CVMat(ocvb)
        kalman.externalUse = True

        ocvb.drawRect = New cv.Rect(100, 100, 100, 100) ' just do a portion of the image.
        ocvb.desc = "Use Kalman filter on a portion of a color image (too slow to run on the whole image.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray32f As New cv.Mat
        ocvb.result1(ocvb.drawRect).ConvertTo(gray32f, cv.MatType.CV_32F)
        kalman.src = gray32f.Reshape(1, gray32f.Width * gray32f.Height)
        kalman.Run(ocvb)
        Dim dst As New cv.Mat
        kalman.dst.ConvertTo(dst, cv.MatType.CV_8U)
        dst = dst.Reshape(1, ocvb.drawRect.Height)
        ocvb.result1(ocvb.drawRect) = dst
        ocvb.label1 = "Draw anywhere to apply Kalman to the image data"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        kalman.Dispose()
    End Sub
End Class






Public Class Kalman_ImageSmall : Implements IDisposable
    Dim kalman As Kalman_CVMat
    Dim resize As Resize_Percentage
    Public Sub New(ocvb As AlgorithmData)
        kalman = New Kalman_CVMat(ocvb)
        kalman.externalUse = True

        resize = New Resize_Percentage(ocvb)
        resize.externalUse = True

        ocvb.label1 = "The small image is processed by the Kalman filter"
        ocvb.label2 = "The original grayscale image"
        ocvb.desc = "Resize the image to allow the Kalman filter to process the whole image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        resize.src = gray
        resize.Run(ocvb)

        Dim saveOriginal = resize.dst.Clone()
        Dim gray32f As New cv.Mat
        resize.dst.ConvertTo(gray32f, cv.MatType.CV_32F)
        kalman.src = gray32f.Reshape(1, gray32f.Width * gray32f.Height)
        kalman.Run(ocvb)
        Dim dst As New cv.Mat
        kalman.dst.ConvertTo(dst, cv.MatType.CV_8U)
        dst = dst.Reshape(1, gray32f.Height)
        ocvb.result1 = dst.Resize(ocvb.result1.Size())
        cv.Cv2.Subtract(dst, saveOriginal, dst)
        dst = dst.Threshold(1, 255, cv.ThresholdTypes.Binary)
        ocvb.result2 = dst.Resize(ocvb.result1.Size())
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        kalman.Dispose()
        resize.Dispose()
    End Sub
End Class





Public Class Kalman_DepthSmall : Implements IDisposable
    Dim kalman As Kalman_CVMat
    Dim resize As Resize_Percentage
    Public Sub New(ocvb As AlgorithmData)
        kalman = New Kalman_CVMat(ocvb)
        kalman.externalUse = True

        resize = New Resize_Percentage(ocvb)
        resize.externalUse = True
        resize.sliders.TrackBar1.Value = 1

        ocvb.label2 = "Brighter means depth is decreasing."
        ocvb.label1 = "Mask of non-zero depth after Kalman smoothing"
        ocvb.desc = "Use a resized depth Mat to find where depth is decreasing (something getting closer.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim depth32f As New cv.Mat
        ocvb.depth16.ConvertTo(depth32f, cv.MatType.CV_32F)
        resize.src = depth32f
        resize.Run(ocvb)

        resize.dst.ConvertTo(depth32f, cv.MatType.CV_32F)
        kalman.src = depth32f.Reshape(1, depth32f.Width * depth32f.Height)
        Dim saveOriginal = kalman.src.Clone()
        kalman.Run(ocvb)
        Dim dst = kalman.dst.Reshape(1, depth32f.Height)
        ocvb.result1 = dst.Resize(ocvb.result1.Size())
        ocvb.result1 = ocvb.result1.ConvertScaleAbs()
        cv.Cv2.Subtract(kalman.dst, saveOriginal, depth32f)
        depth32f = depth32f.Threshold(0, 0, cv.ThresholdTypes.Tozero)
        depth32f.ConvertTo(dst, cv.MatType.CV_8U)
        dst = dst.Reshape(1, resize.dst.Height)
        ocvb.result2 = dst.Resize(ocvb.result1.Size())
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        kalman.Dispose()
        resize.Dispose()
    End Sub
End Class







Public Class Kalman_Single : Implements IDisposable
    Dim plot As Plot_OverTime
    Dim kf As New cv.KalmanFilter(2, 1, 0)
    Dim processNoise As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Public measurement As New cv.Mat(1, 1, cv.MatType.CV_32F, 0)
    Public inputReal As Single
    Public stateResult As Single
    Public externalUse As Boolean
    Public ProcessNoiseCov As Single = 0.00001
    Public MeasurementNoiseCov As Single = 0.1
    Public ErrorCovPost As Single = 1
    Public transitionMatrix() As Single = {1, 1, 0, 1} ' Change the transition matrix externally and set newTransmissionMatrix.
    Public newTransmissionMatrix As Boolean = True
    Public Sub New(ocvb As AlgorithmData)
#If opencvsharpOld Then
        kf.TransitionMatrix.SetArray(0, 0, New Single() {1, 1, 0, 1}.ToArray)
#Else
        kf.TransitionMatrix = New cv.Mat(4, 1, cv.MatType.CV_32F, New Single() {1, 1, 0, 1})
#End If
        cv.Cv2.SetIdentity(kf.MeasurementMatrix)
        cv.Cv2.SetIdentity(kf.ProcessNoiseCov, cv.Scalar.All(0.00001))
        cv.Cv2.SetIdentity(kf.MeasurementNoiseCov, cv.Scalar.All(0.1))
        cv.Cv2.SetIdentity(kf.ErrorCovPost, cv.Scalar.All(1))
        ocvb.label1 = "Mean of the grayscale image is predicted"
        ocvb.desc = "Estimate a single value using a Kalman Filter - in the default case, the value of the mean of the grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            If ocvb.frameCount = 0 Then
                plot = New Plot_OverTime(ocvb)
                plot.externalUse = True
                plot.dst = ocvb.result2
                plot.maxScale = 150
                plot.minScale = 80
                plot.plotCount = 2
            End If

            ocvb.result1 = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            inputReal = ocvb.result1.Mean().Item(0)
        End If

        Dim prediction = kf.Predict()
        measurement.Set(Of Single)(0, 0, inputReal)
        stateResult = kf.Correct(measurement).At(Of Single)(0, 0)
        If externalUse = False Then
            plot.plotData = New cv.Scalar(inputReal, stateResult, 0, 0)
            plot.Run(ocvb)
            ocvb.label2 = "Mean (blue) = " + Format(inputReal, "0.0") + " predicted (green) = " + Format(stateResult, "0.0")
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If plot IsNot Nothing Then plot.Dispose()
    End Sub
End Class






Public Class Kalman_SingleNew : Implements IDisposable
    Dim plot As Plot_OverTime
    Public kf As New cv.KalmanFilter(2, 1, 0)
    Dim processNoise As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Public measurement As New cv.Mat(1, 1, cv.MatType.CV_32F, 0)
    Public inputReal As Single
    Public stateResult As Single
    Public externalUse As Boolean
    Public ProcessNoiseCov As Single = 0.00001
    Public MeasurementNoiseCov As Single = 0.1
    Public ErrorCovPost As Single = 1
    Public transitionMatrix() As Single = {1, 1, 0, 1} ' Change the transition matrix externally and set newTransmissionMatrix.
    Public newTransmissionMatrix As Boolean = True
    Public Sub New(ocvb As AlgorithmData)
        cv.Cv2.SetIdentity(kf.MeasurementMatrix)
        ocvb.desc = "Estimate a single value using a Kalman Filter - in the default case, the value of the mean of the grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static saveProcessNoiseCov As Single
        Static saveMeasurementNoiseCov As Single
        Static saveErrorCovPost As Single
        If ProcessNoiseCov <> saveProcessNoiseCov Or saveMeasurementNoiseCov = MeasurementNoiseCov Or saveErrorCovPost = ErrorCovPost Then
            saveProcessNoiseCov = ProcessNoiseCov
            saveMeasurementNoiseCov = MeasurementNoiseCov
            saveErrorCovPost = ErrorCovPost
            cv.Cv2.SetIdentity(kf.ProcessNoiseCov, cv.Scalar.All(ProcessNoiseCov))
            cv.Cv2.SetIdentity(kf.MeasurementNoiseCov, cv.Scalar.All(MeasurementNoiseCov))
            cv.Cv2.SetIdentity(kf.ErrorCovPost, cv.Scalar.All(ErrorCovPost))
        End If
        If newTransmissionMatrix Then
            newTransmissionMatrix = False
#If opencvsharpOld Then
            kf.TransitionMatrix.SetArray(0, 0, transitionMatrix.ToArray)
#Else
            kf.TransitionMatrix = New cv.Mat(4, 1, cv.MatType.CV_32F,  transitionMatrix.ToArray)
#End If
        End If
        If externalUse = False Then
            If ocvb.frameCount = 0 Then
                plot = New Plot_OverTime(ocvb)
                plot.externalUse = True
                plot.dst = ocvb.result2
                plot.maxScale = 150
                plot.minScale = 80
                plot.plotCount = 2
            End If

            ocvb.label1 = "Mean of the grayscale image is predicted"
            ocvb.result1 = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            inputReal = ocvb.result1.Mean().Item(0)
        End If

        Dim prediction = kf.Predict()
        measurement.Set(Of Single)(0, 0, inputReal)
        stateResult = kf.Correct(measurement).At(Of Single)(0, 0)
        If externalUse = False Then
            plot.plotData = New cv.Scalar(inputReal, stateResult, 0, 0)
            plot.Run(ocvb)
            ocvb.label2 = "Mean (blue) = " + Format(inputReal, "0.0") + " predicted (green) = " + Format(stateResult, "0.0")
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If plot IsNot Nothing Then plot.Dispose()
    End Sub
End Class
