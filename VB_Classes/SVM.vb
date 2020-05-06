Imports cv = OpenCvSharp
Public Class SVM_Options : Implements IDisposable
    Public sliders As New OptionsSliders
    Public radio As New OptionsRadioButtons
    Public radio1 As New OptionsRadioButtons
    Public kernelType = cv.ML.SVM.KernelTypes.Rbf
    Public SVMType = cv.ML.SVM.Types.CSvc
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "SampleCount", 10, 1000, 500)
        sliders.setupTrackBar2(ocvb, "Granularity", 1, 50, 5)
        If ocvb.parms.ShowOptions Then sliders.Show()

        radio.Setup(ocvb, 4)
        radio.check(0).Text = "kernel Type = Linear"
        radio.check(1).Text = "kernel Type = Poly"
        radio.check(2).Text = "kernel Type = RBF"
        radio.check(2).Checked = True
        radio.check(3).Text = "kernel Type = Sigmoid"
        If ocvb.parms.ShowOptions Then radio.Show()

        radio1.Setup(ocvb, 5)
        radio1.check(0).Text = "SVM Type = CSvc"
        radio1.check(0).Checked = True
        radio1.check(1).Text = "SVM Type = EpsSvr"
        radio1.check(2).Text = "SVM Type = NuSvc"
        radio1.check(3).Text = "SVM Type = NuSvr"
        radio1.check(4).Text = "SVM Type = OneClass"
        If ocvb.parms.ShowOptions Then radio1.Show()

        ocvb.desc = "SVM has many options - enough to make a class for it."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        For i = 0 To radio.check.Length - 1
            If radio.check(i).Checked Then
                kernelType = Choose(i + 1, cv.ML.SVM.KernelTypes.Linear, cv.ML.SVM.KernelTypes.Poly, cv.ML.SVM.KernelTypes.Rbf, cv.ML.SVM.KernelTypes.Sigmoid)
                Exit For
            End If
        Next

        For i = 0 To radio.check.Length - 1
            If radio.check(i).Checked Then
                SVMType = Choose(i + 1, cv.ML.SVM.Types.CSvc, cv.ML.SVM.Types.EpsSvr, cv.ML.SVM.Types.NuSvc, cv.ML.SVM.Types.NuSvr, cv.ML.SVM.Types.OneClass)
                Exit For
            End If
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
        radio1.Dispose()
    End Sub
End Class



Public Class SVM_Basics : Implements IDisposable
    Dim svmOptions As SVM_Options
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        svmOptions = New SVM_Options(ocvb, "SVM_Basics")
        ocvb.desc = "Use SVM to classify random points.  Increase the sample count to see the value of more data."
        ocvb.label1 = "SVM_Basics input data"
        ocvb.label2 = "Results - line is ground truth"
    End Sub
    Private Function f(x As Double) As Double
        Return x + 50 * Math.Sin(x / 15.0)
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        svmOptions.Run(ocvb) ' update any options specified in the interface.
        ocvb.result1.SetTo(0)
        ocvb.result2.SetTo(0)

        Dim points(svmOptions.sliders.TrackBar1.Value) As cv.Point2f
        Dim responses(points.Length - 1) As Int32
        For i = 0 To points.Length - 1
            Dim x = ocvb.ms_rng.Next(0, ocvb.color.Height - 1)
            Dim y = ocvb.ms_rng.Next(0, ocvb.color.Height - 1)
            points(i) = New cv.Point2f(x, y)
            responses(i) = If(y > f(x), 1, 2)
        Next
        For i = 0 To points.Length - 1
            Dim x = CInt(points(i).X)
            Dim y = CInt(ocvb.color.Height - points(i).Y)
            Dim res = responses(i)
            Dim color As cv.Scalar = If(res = 1, cv.Scalar.Red, cv.Scalar.GreenYellow)
            Dim cSize = If(res = 1, 2, 4)
            ocvb.result1.Circle(x, y, cSize, color, -1)
        Next

        Dim dataMat = New cv.Mat(points.Length - 1, 2, cv.MatType.CV_32FC1, points)
        Dim resMat = New cv.Mat(responses.Length - 1, 1, cv.MatType.CV_32SC1, responses)
        Using svmx As cv.ML.SVM = cv.ML.SVM.Create()
            dataMat /= (ocvb.color.Height - 1)
            svmx.Type = svmOptions.SVMType
            svmx.KernelType = svmOptions.kernelType
            svmx.TermCriteria = cv.TermCriteria.Both(1000, 0.000001)
            svmx.Degree = 100.0
            svmx.Gamma = 100.0
            svmx.Coef0 = 1.0
            svmx.C = 1.0
            svmx.Nu = 0.5
            svmx.P = 0.1

            svmx.Train(dataMat, cv.ML.SampleTypes.RowSample, resMat)

            Dim granularity = svmOptions.sliders.TrackBar2.Value
            Dim sampleMat As New cv.Mat(1, 2, cv.MatType.CV_32F)
            For x = 0 To ocvb.color.Height - 1 Step granularity
                For y = 0 To ocvb.color.Height - 1 Step granularity
                    sampleMat.Set(Of Single)(0, 0, x / CSng(ocvb.color.Height))
                    sampleMat.Set(Of Single)(0, 1, y / CSng(ocvb.color.Height))
                    Dim ret = svmx.Predict(sampleMat)
                    Dim plotRect = New cv.Rect(x, ocvb.color.Height - 1 - y, granularity * 2, granularity * 2)
                    If ret = 1 Then
                        ocvb.result2.Rectangle(plotRect, cv.Scalar.Red, -1)
                    ElseIf ret = 2 Then
                        ocvb.result2.Rectangle(plotRect, cv.Scalar.GreenYellow, -1)
                    End If
                Next
            Next

            ' draw the function in both plots to show ground truth.
            For x = 1 To ocvb.color.Height - 1
                Dim y1 = CInt(ocvb.color.Height - f(x - 1))
                Dim y2 = CInt(ocvb.color.Height - f(x))
                ocvb.result1.Line(x - 1, y1, x, y2, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                ocvb.result2.Line(x - 1, y1, x, y2, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            Next
        End Using
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        svmOptions.Dispose()
    End Sub
End Class




Public Class SVM_Basics_MT : Implements IDisposable
    Dim grid As Thread_Grid
    Dim svmOptions As SVM_Options
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        svmOptions = New SVM_Options(ocvb, "SVM_Basics_MT")
        grid = New Thread_Grid(ocvb, "SVM_Basics_MT")
        grid.sliders.TrackBar1.Value = 100
        grid.sliders.TrackBar2.Value = 16
        grid.externalUse = True ' we don't need any results.

        ocvb.desc = "Use SVM to classify random points.  Testing the benefit of multi-threading prediction."
        ocvb.label1 = "SVM_Basics input data"
        ocvb.label2 = "Predictions - line is ground truth"
    End Sub
    Private Function f(x As Double) As Double
        Return x + 50 * Math.Sin(x / 15.0)
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        svmOptions.Run(ocvb)
        grid.Run(ocvb)
        ocvb.result1.SetTo(0)
        ocvb.result2.SetTo(0)
        Dim points(svmOptions.sliders.TrackBar1.Value) As cv.Point2f
        Dim responses(points.Length - 1) As Int32
        For i = 0 To points.Length - 1
            Dim x = ocvb.ms_rng.Next(0, ocvb.color.Height - 1)
            Dim y = ocvb.ms_rng.Next(0, ocvb.color.Height - 1)
            points(i) = New cv.Point2f(x, y)
            responses(i) = If(y > f(x), 1, 2)
        Next
        For i = 0 To points.Length - 1
            Dim x = CInt(points(i).X)
            Dim y = CInt(ocvb.color.Height - points(i).Y)
            Dim res = responses(i)
            Dim color As cv.Scalar = If(res = 1, cv.Scalar.Red, cv.Scalar.GreenYellow)
            Dim cSize = If(res = 1, 2, 4)
            ocvb.result1.Circle(x, y, cSize, color, -1)
        Next

        Dim dataMat = New cv.Mat(points.Length - 1, 2, cv.MatType.CV_32FC1, points)
        Dim resMat = New cv.Mat(responses.Length - 1, 1, cv.MatType.CV_32SC1, responses)
        Using svmx As cv.ML.SVM = cv.ML.SVM.Create()
            dataMat /= (ocvb.color.Height - 1)
            svmx.Type = svmOptions.SVMType
            svmx.KernelType = svmOptions.kernelType
            svmx.TermCriteria = cv.TermCriteria.Both(1000, 0.000001)
            svmx.Degree = 100.0
            svmx.Gamma = 100.0
            svmx.Coef0 = 1.0
            svmx.C = 1.0
            svmx.Nu = 0.5
            svmx.P = 0.1

            svmx.Train(dataMat, cv.ML.SampleTypes.RowSample, resMat)

            Dim granularity = svmOptions.sliders.TrackBar2.Value
            Parallel.ForEach(Of cv.Rect)(grid.roiList,
            Sub(roi)
                If roi.X + roi.Width > ocvb.color.Height Then
                    roi.Width = ocvb.color.Height - roi.X
                    If roi.Width <= 0 Then Exit Sub ' the prediction region must be square.  This roi is too far to the right.
                End If
                Dim sampleMat As New cv.Mat(1, 2, cv.MatType.CV_32F)

                For y = 0 To roi.Height - 1 Step granularity
                    For x = 0 To roi.Width - 1 Step granularity
                        sampleMat.Set(Of Single)(0, 0, (x + roi.X) / ocvb.color.Height)
                        sampleMat.Set(Of Single)(0, 1, (y + roi.Y) / ocvb.color.Height)
                        Dim ret = svmx.Predict(sampleMat)
                        Dim plotRect = New cv.Rect(x + roi.X, ocvb.color.Height - 1 - (y + roi.Y), granularity * 2, granularity * 2)
                        If ret = 1 Then
                            ocvb.result2.Rectangle(plotRect, cv.Scalar.Red, -1)
                        ElseIf ret = 2 Then
                            ocvb.result2.Rectangle(plotRect, cv.Scalar.GreenYellow, -1)
                        End If
                    Next
                Next
            End Sub)

            For x = 1 To ocvb.color.Height - 1
                Dim y1 = CInt(ocvb.color.Height - f(x - 1))
                Dim y2 = CInt(ocvb.color.Height - f(x))
                ocvb.result1.Line(x - 1, y1, x, y2, cv.Scalar.LightBlue, 1, cv.LineTypes.AntiAlias)
                ocvb.result2.Line(x - 1, y1, x, y2, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            Next
            ocvb.result1.SetTo(cv.Scalar.White, grid.gridMask)
        End Using
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        svmOptions.Dispose()
        grid.Dispose()
    End Sub
End Class



Public Class SVM_Simple : Implements IDisposable
    Dim svmOptions As SVM_Options
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        svmOptions = New SVM_Options(ocvb, "SVM_Simple")
        svmOptions.sliders.TrackBar1.Value = 50 ' set the samplecount 
        svmOptions.radio.check(1).Checked = True
        ocvb.desc = "Use SVM to classify random points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        svmOptions.Run(ocvb)

        Dim dataSize = svmOptions.sliders.TrackBar1.Value ' get the sample count
        Dim trainData As New cv.Mat(dataSize, 2, cv.MatType.CV_32F)
        Dim labels = New cv.Mat(dataSize, 1, cv.MatType.CV_32S)
        For i = 0 To dataSize
            labels.Set(Of Int32)(i, 0, ocvb.ms_rng.next(-1, 1))
            trainData.Set(Of Single)(i, 0, CSng(ocvb.ms_rng.next(0, ocvb.color.Width - 1)))
            trainData.Set(Of Single)(i, 1, CSng(ocvb.ms_rng.next(0, ocvb.color.Height - 1)))
        Next
        ' make sure that there always 2 classes present.
        labels.Set(Of Single)(0, 0, -1)
        labels.Set(Of Single)(dataSize - 1, 0, 1)

        ocvb.result1.SetTo(cv.Scalar.White)
        Using svmx As cv.ML.SVM = cv.ML.SVM.Create()
            svmx.Type = svmOptions.SVMType
            svmx.KernelType = svmOptions.kernelType
            svmx.TermCriteria = cv.TermCriteria.Both(1000, 0.000001)
            svmx.Degree = 100.0
            svmx.Gamma = 100.0
            svmx.Coef0 = 1.0
            svmx.C = 1.0
            svmx.Nu = 0.5
            svmx.P = 0.1

            svmx.Train(trainData, cv.ML.SampleTypes.RowSample, labels)

            Dim green = New cv.Vec3b(0, 255, 0)
            Dim yellow = New cv.Vec3b(255, 255, 0)
            Dim sampleMat As New cv.Mat(1, 2, cv.MatType.CV_32F)
            For y = 0 To ocvb.result2.Height - 1
                For x = 0 To ocvb.result2.Width - 1
                    sampleMat.Set(Of Single)(0, 0, x)
                    sampleMat.Set(Of Single)(0, 1, y)
                    'Dim ret = CInt(svmx.Predict(sampleMat))
                    'ocvb.result2.Set(Of cv.Vec3b)(y, x, If(ret = 1, green, yellow))
                Next
            Next

            ' show the training data
            For i = 0 To trainData.Rows
                Dim pt = New cv.Point(CInt(trainData.Get(Of Single)(i, 0)), CInt(trainData.Get(Of Single)(i, 1)))
                If labels.Get(of Int32)(i) >= 0 Then
                    ocvb.result1.Circle(pt, 5, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
                Else
                    ocvb.result1.Circle(pt, 5, cv.Scalar.Green, -1, cv.LineTypes.AntiAlias)
                End If
            Next

            Dim response = svmx.GetSupportVectors()
            ocvb.result2.SetTo(0)
            Dim thickness = 2
            If response.Rows > 1 Then
                For i = 0 To response.Rows
                    Dim v = response.Get(of cv.Vec2f)(i)
                    ocvb.result2.Circle(New cv.Point(v(0), v(1)), 6, cv.Scalar.Blue, thickness, cv.LineTypes.AntiAlias)
                Next
            End If
        End Using
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        svmOptions.Dispose()
    End Sub
End Class
