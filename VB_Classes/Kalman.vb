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
    Dim plot As Plot_OverTime
    Dim kPlot As Plot_OverTime
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "ProcessNoiseCov x10000", 1, 1000, 100)
        sliders.setupTrackBar2(ocvb, "MeasurementNoiseCov", 1, 100, 10)
        sliders.setupTrackBar3(ocvb, "ErrorCovPost x100", 1, 100, 10)
        If ocvb.parms.ShowOptions Then sliders.show()

        plot = New Plot_OverTime(ocvb)
        plot.externalUse = True
        plot.dst = ocvb.result2
        plot.sliders.TrackBar2.Value = 20

        kPlot = New Plot_OverTime(ocvb)
        kPlot.externalUse = True
        kPlot.dst = ocvb.result1
        kPlot.sliders.TrackBar2.Value = 20

        ocvb.label1 = "Kalman_Basics"
        ocvb.desc = "Use this kalman filter to predict the next value."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount = 0 Or restartRequested Then
            restartRequested = False
            kf = New cv.KalmanFilter(6, 3, 0)
            kf.TransitionMatrix.SetArray(0, 0, New Single() {1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1})
            kf.StatePre.SetArray(0, 0, New Single() {0, 0, 0, 0})
            Dim statePre(3) As Single ' all zeros by default
            kf.StatePre.SetArray(0, 0, statePre)

            cv.Cv2.SetIdentity(kf.MeasurementMatrix)
            cv.Cv2.SetIdentity(kf.ProcessNoiseCov, ProcessNoiseCov)
            cv.Cv2.SetIdentity(kf.MeasurementNoiseCov, MeasurementNoiseCov)
            cv.Cv2.SetIdentity(kf.ErrorCovPost, ErrorCovPost)
        End If

        If externalUse = False Then
            plot.plotData = ocvb.color.Mean()
            ocvb.label2 = "Input: x = " + Format(plot.plotData.Item(0), "#0.00") + " y = " + Format(plot.plotData.Item(1), "#0.00") + " z = " + Format(plot.plotData.Item(2), "#0.00")
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
            kPlot.plotData = New cv.Scalar(statePoint.X, statePoint.Y, statePoint.Z)
            kPlot.Run(ocvb)
            ocvb.label1 = "Kalman output: x = " + Format(statePoint.X, "#0.00") + " y = " + Format(statePoint.Y, "#0.00") + " z = " + Format(statePoint.Z, "#0.00")
        End If
        lastStatePoint = statePoint
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        plot.Dispose()
        kPlot.Dispose()
    End Sub
End Class


Public Class Kalman_kDimension_Options : Implements IDisposable
    Public sliders As New OptionsSliders
    Public radio As New OptionsRadioButtons
    Public kf As Kalman_kDimension
    Public Sub New(ocvb As AlgorithmData)
        kf = New Kalman_kDimension(ocvb)
        kf.externalUse = True

        sliders.setupTrackBar1(ocvb, "ProcessNoiseCov x10000", 1, 1000, 100)
        sliders.setupTrackBar2(ocvb, "MeasurementNoiseCov", 1, 100, 10)
        sliders.setupTrackBar3(ocvb, "ErrorCovPost x100", 1, 100, 10)
        If ocvb.parms.ShowOptions Then sliders.show()

        radio.Setup(ocvb, 7)
        radio.check(0).Text = "1,0,1,0 transition matrix"
        radio.check(1).Text = "1,1,1,0 transition matrix"
        radio.check(2).Text = "1,0,1,1 transition matrix"
        radio.check(3).Text = "0,0,1,0 transition matrix"
        radio.check(4).Text = "0,0,0,0 transition matrix" ' this produces better results...  Experiment with histogram_kalmansmoothed to verify
        radio.check(5).Text = "0,0,0,1 transition matrix"
        radio.check(6).Text = "0,1,1,0 transition matrix"
        radio.check(4).Checked = True
        If ocvb.parms.ShowOptions Then radio.show()

        ocvb.label1 = "Kalman_kDimension (no output by default)"
        ocvb.desc = "Use this kalman filter to predict the set of values."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        kf.ProcessNoiseCov = cv.Scalar.All(sliders.TrackBar1.Value / 10000)
        kf.MeasurementNoiseCov = cv.Scalar.All(sliders.TrackBar2.Value)
        kf.ErrorCovPost = cv.Scalar.All(sliders.TrackBar3.Value / 100)

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
            kf.restartRequested = True
        End If

        kf.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
    End Sub
End Class


Public Class Kalman_kDimension : Implements IDisposable
    Dim ksingle As Kalman_Single
    Public inputReal As New cv.Mat
    Public statePoint As New cv.Mat
    Public ProcessNoiseCov = cv.Scalar.All(100 / 10000)
    Public MeasurementNoiseCov = cv.Scalar.All(10)
    Public ErrorCovPost = cv.Scalar.All(10 / 100)
    Public prediction As New cv.Mat

    Public externalUse As Boolean
    Public restartRequested As Boolean = True
    Public kDimension As Int32 = 50
    Public kalman As cv.KalmanFilter
    Public measurement As cv.Mat
    Public transitionMatrix(3) As Single ' default to all zeros for the transition matrix.  Change the transition matrix externally and set restartRequested.
    Public Sub New(ocvb As AlgorithmData)
        ksingle = New Kalman_Single(ocvb)

        ocvb.label1 = "Kalman_kDimension (no output by default)"
        ocvb.desc = "Use this kalman filter to predict the set of k values."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static saveKDimension As Int32
        If restartRequested Or kDimension <> saveKDimension Then
            saveKDimension = kDimension
            restartRequested = False
            kalman = New cv.KalmanFilter(kDimension * 2, kDimension, 0)
            measurement = New cv.Mat(kDimension, 1, cv.MatType.CV_32F, 0)
            inputReal = New cv.Mat(kDimension, 1, cv.MatType.CV_32F, 0)
            kalman.TransitionMatrix.SetArray(0, 0, New Single() {transitionMatrix(0), transitionMatrix(1), transitionMatrix(2), transitionMatrix(3)})
            Dim statePre(kDimension) As Single ' all zeros by default
            kalman.StatePre.SetArray(0, 0, statePre)

            cv.Cv2.SetIdentity(kalman.MeasurementMatrix)
            cv.Cv2.SetIdentity(kalman.ProcessNoiseCov, ProcessNoiseCov)
            cv.Cv2.SetIdentity(kalman.MeasurementNoiseCov, MeasurementNoiseCov)
            cv.Cv2.SetIdentity(kalman.ErrorCovPost, ErrorCovPost)
        Else
            prediction = kalman.Predict()
            measurement = inputReal.Clone()
            statePoint = kalman.Correct(measurement).RowRange(0, kDimension).Clone()
            ksingle.inputReal = inputReal.At(Of Single)(0, 0)
            ksingle.Run(ocvb)
            statePoint.Set(Of Single)(0, 0, ksingle.stateResult) ' first value is not correct.  Not sure why...  Quick fix.  Please advise!
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        ksingle.Dispose()
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
        Dim lastStatePoint = kf.lastStatePoint
        kf.Run(ocvb)
        cv.Cv2.Line(ocvb.result1, New cv.Point(lastStatePoint.X, lastStatePoint.Y), New cv.Point(kf.statePoint.X, kf.statePoint.Y), cv.Scalar.All(255), 1, cv.LineTypes.AntiAlias)
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
        kf.TransitionMatrix.SetArray(0, 0, New Single() {1, 1, 0, 1}.ToArray)

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


Public Class Kalman_RGBGrid_MT : Implements IDisposable
    Public grid As Thread_Grid
    Dim kalman() As Kalman_kDimension
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 64
        grid.externalUse = True

        ocvb.label2 = "This algorithm is unfinished..."
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
               cv.Cv2.Subtract(gray(roi), lastGray(roi), grayDiff)
               grayDiff.ConvertTo(gray32f, cv.MatType.CV_32F)
               Dim learnInput = gray32f.Clone()
               kalman(i).inputReal = learnInput.Reshape(1, roi.Width * roi.Height)
               ' kalman(i).Run(ocvb)
               If kalman(i).statePoint.Width > 0 Then
                   learnInput = kalman(i).statePoint.Clone()
                   gray32f = learnInput.Reshape(1, roi.Height)
               End If
               gray32f.ConvertTo(grayDiff, cv.MatType.CV_8U)
               cv.Cv2.Add(gray(roi), grayDiff, ocvb.result1(roi))
           End Sub)
        End If
        lastGray = gray.Clone()
        ocvb.label1 = "Kalman stabilized grayscale image"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
        For i = 0 To kalman.Count - 1
            kalman(i).Dispose()
        Next
    End Sub
End Class


' http://opencvexamples.blogspot.com/2014/01/kalman-filter-implementation-tracking.html
Public Class Kalman_Point2f : Implements IDisposable
    Public sliders As New OptionsSliders
    Public inputReal As New cv.Point2f
    Public lastStatePoint As New cv.Point2f
    Public statePoint As New cv.Point2f
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
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.label1 = "Kalman_Basics (no output by default)"
        ocvb.desc = "Use this kalman filter to predict the next value."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount = 0 Or restartRequested Then
            restartRequested = False
            kf.TransitionMatrix.SetArray(0, 0, New Single() {1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1})
            kf.StatePre.SetArray(0, 0, New Single() {0, 0, 0, 0})

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
        statePoint = New cv.Point(estimated.At(Of Single)(0), estimated.At(Of Single)(1))
        lastStatePoint = statePoint
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class


Public Class Kalman_Image : Implements IDisposable
    Dim kalman() As Kalman_kDimension
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use Kalman to stabilize pixel values.  Shows limitations of how much data can be pushed through a Kalman Filter."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.drawRect = New cv.Rect(0, 0, 0, 0) Then ocvb.drawRect = New cv.Rect(0, 200, ocvb.color.Width, 50)
        ocvb.result1 = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray = ocvb.result1(ocvb.drawRect)

        Static saveGrayCols = gray.Cols
        If ocvb.frameCount = 0 Or saveGrayCols <> gray.Cols Then
            ReDim kalman(gray.Cols - 1)
            For i = 0 To gray.Cols - 1
                kalman(i) = New Kalman_kDimension(ocvb)
                kalman(i).kDimension = gray.Height
            Next
            ocvb.label1 = "Draw on the image - keep it small!"
        End If

        If ocvb.frameCount > 0 Then
            Parallel.For(0, gray.Cols - 1,
            Sub(i)
                If kalman(i).inputReal.Cols = 0 Then kalman(i).Run(ocvb) ' initialize on the first invocation...
                gray.Col(i).ConvertTo(kalman(i).inputReal, cv.MatType.CV_32F)
                kalman(i).Run(ocvb)
                kalman(i).statePoint.ConvertTo(ocvb.result1.Col(i), cv.MatType.CV_8U)
            End Sub)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        For i = 0 To kalman.Count - 1
            kalman(i).Dispose()
        Next
    End Sub
End Class


Public Class Kalman_Depth : Implements IDisposable
    Dim kalman() As Kalman_kDimension
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use Kalman to stabilize Depth values.  Shows limitations of how much data can be pushed through a Kalman Filter."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.drawRect = New cv.Rect(0, 0, 0, 0) Then ocvb.drawRect = New cv.Rect(0, 200, ocvb.depthRGB.Width, 50)
        ocvb.result1 = ocvb.depthRGB.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray = ocvb.result1(ocvb.drawRect)

        Static saveGrayCols = gray.Cols
        If ocvb.frameCount = 0 Or saveGrayCols <> gray.Cols Then
            ReDim kalman(gray.Cols - 1)
            For i = 0 To gray.Cols - 1
                kalman(i) = New Kalman_kDimension(ocvb)
                kalman(i).kDimension = gray.Height
            Next
            ocvb.label1 = "Draw on the image - keep it small!"
        End If

        If ocvb.frameCount > 0 Then
            Parallel.For(0, gray.Cols - 1,
            Sub(i)
                If kalman(i).inputReal.Cols = 0 Then kalman(i).Run(ocvb) ' initialize on the first invocation...
                gray.Col(i).ConvertTo(kalman(i).inputReal, cv.MatType.CV_32F)
                kalman(i).Run(ocvb)
                kalman(i).statePoint.ConvertTo(gray.Col(i), cv.MatType.CV_8U)
            End Sub)
        End If
        ocvb.result1(ocvb.drawRect) = gray
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If kalman Is Nothing Then Exit Sub
        For i = 0 To kalman.Count - 1
            kalman(i).Dispose()
        Next
    End Sub
End Class


Public Class Kalman_Single : Implements IDisposable
    Dim kf As New cv.KalmanFilter(2, 1, 0)
    Dim processNoise As New cv.Mat(2, 1, cv.MatType.CV_32F)
    Dim measurement As New cv.Mat(1, 1, cv.MatType.CV_32F, 0)
    Public inputReal As Single
    Public stateResult As Single
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "Estimate a single value (no default output)"

        kf.TransitionMatrix.SetArray(0, 0, New Single() {1, 1, 0, 1}.ToArray)

        cv.Cv2.SetIdentity(kf.MeasurementMatrix)
        cv.Cv2.SetIdentity(kf.ProcessNoiseCov, cv.Scalar.All(0.00001))
        cv.Cv2.SetIdentity(kf.MeasurementNoiseCov, cv.Scalar.All(0.1))
        cv.Cv2.SetIdentity(kf.ErrorCovPost, cv.Scalar.All(1))
        cv.Cv2.Randn(kf.StatePost, New cv.Scalar(0), cv.Scalar.All(1))
        ocvb.desc = "Estimate a single value using a Kalman Filter."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim prediction = kf.Predict()
        stateResult = prediction.At(Of Single)(0)
        measurement.Set(Of Single)(0, 0, inputReal)
        stateResult = kf.Correct(measurement).At(Of Single)(0, 0)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class