Imports cv = OpenCvSharp
Public Class SVM_Options
    Inherits ocvbClass
    Public kernelType = cv.ML.SVM.KernelTypes.Rbf
    Public SVMType = cv.ML.SVM.Types.CSvc
    Public points() As cv.Point2f
    Public responses() As Integer
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "SampleCount", 5, 1000, 500)
        sliders.setupTrackBar2(ocvb, caller, "Granularity", 1, 50, 5)

        radio.Setup(ocvb, caller, 4)
        radio.check(0).Text = "kernel Type = Linear"
        radio.check(1).Text = "kernel Type = Poly"
        radio.check(2).Text = "kernel Type = RBF"
        radio.check(2).Checked = True
        radio.check(3).Text = "kernel Type = Sigmoid"

        radio1.Setup(ocvb, caller, 5)
        radio1.check(0).Text = "SVM Type = CSvc"
        radio1.check(0).Checked = True
        radio1.check(1).Text = "SVM Type = EpsSvr"
        radio1.check(2).Text = "SVM Type = NuSvc"
        radio1.check(3).Text = "SVM Type = NuSvr"
        radio1.check(4).Text = "SVM Type = OneClass"
        If ocvb.parms.ShowOptions Then radio.Show()

        label1 = "SVM_Options - only options, no output"
        ocvb.desc = "SVM has many options - enough to make a class for it."
    End Sub
    Public Function createSVM() As cv.ML.SVM
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

        Dim svmx = cv.ML.SVM.Create()
        svmx.Type = SVMType
        svmx.KernelType = kernelType
        svmx.TermCriteria = cv.TermCriteria.Both(1000, 0.000001)
        svmx.Degree = 100.0
        svmx.Gamma = 100.0
        svmx.Coef0 = 1.0
        svmx.C = 1.0
        svmx.Nu = 0.5
        svmx.P = 0.1

        Return svmx
    End Function
    Public Function f(x As Double) As Double
        Return x + 50 * Math.Sin(x / 15.0)
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        ReDim points(sliders.TrackBar1.Value)
        ReDim responses(points.Length - 1)
        For i = 0 To points.Length - 1
            Dim x = msRNG.Next(0, src.Height - 1)
            Dim y = msRNG.Next(0, src.Height - 1)
            points(i) = New cv.Point2f(x, y)
            responses(i) = If(y > f(x), 1, 2)
        Next

        dst1.SetTo(0)
        For i = 0 To points.Length - 1
            Dim x = CInt(points(i).X)
            Dim y = CInt(src.Height - points(i).Y)
            Dim res = responses(i)
            Dim color As cv.Scalar = If(res = 1, cv.Scalar.Red, cv.Scalar.GreenYellow)
            Dim cSize = If(res = 1, 2, 4)
            dst1.Circle(x, y, cSize, color, -1)
        Next
    End Sub
End Class



Public Class SVM_Basics
    Inherits ocvbClass
    Dim svmOptions As SVM_Options
    Dim svm As New SVM_Simple
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        svmOptions = New SVM_Options(ocvb, caller)
        ocvb.desc = "Use SVM to classify random points.  Increase the sample count to see the value of more data."
        label1 = "SVM_Basics input data"
        label2 = "Results - line is ground truth"
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        svmOptions.Run(ocvb) ' update any options specified in the interface.
        dst1 = svmOptions.dst1

        Dim dataMat = New cv.Mat(svmOptions.points.Length - 1, 2, cv.MatType.CV_32FC1, svmOptions.points)
        Dim resMat = New cv.Mat(svmOptions.responses.Length - 1, 1, cv.MatType.CV_32SC1, svmOptions.responses)
        Dim svmx = svmOptions.createSVM()
        dataMat *= 1 / src.Height

        svmx.Train(dataMat, cv.ML.SampleTypes.RowSample, resMat)

        dst2 = svm.RunSVM(svmx, src, dataMat, resMat, svmOptions.sliders.TrackBar2.Value)

        ' draw the function in both plots to show ground truth.
        For x = 1 To src.Height - 1
            Dim y1 = CInt(src.Height - svmOptions.f(x - 1))
            Dim y2 = CInt(src.Height - svmOptions.f(x))
            dst1.Line(x - 1, y1, x, y2, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            dst2.Line(x - 1, y1, x, y2, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class




Public Class SVM_Random
    Inherits ocvbClass
    Dim svmOptions As SVM_Options
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        svmOptions = New SVM_Options(ocvb, caller)
        svmOptions.sliders.TrackBar2.Value = 15
        ocvb.drawRect = New cv.Rect(ocvb.color.Cols / 4, ocvb.color.Rows / 4, ocvb.color.Cols / 2, ocvb.color.Rows / 2)

        ocvb.label1 = "SVM Training data"
        ocvb.desc = "Use SVM to classify random points - testing if height must equal width."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        svmOptions.Run(ocvb)
        dst1.SetTo(cv.Scalar.White)
        dst2.SetTo(cv.Scalar.White)

        Dim rect = ocvb.drawRect

        Dim dataSize = svmOptions.sliders.TrackBar1.Value ' get the sample count
        Dim trainData As New cv.Mat(dataSize, 2, cv.MatType.CV_32F)
        Dim response = New cv.Mat(dataSize, 1, cv.MatType.CV_32S)
        For i = 0 To dataSize
            Dim pt = New cv.Point2f(msRNG.Next(0, src.Width - 1), msRNG.Next(0, src.Height - 1))
            If pt.X > rect.X And pt.X < rect.X + rect.Width And pt.Y > rect.Y And pt.Y < rect.Y + rect.Height Then
                response.Set(Of Integer)(i, 0, 1)
                dst1.Circle(pt, 5, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
            Else
                response.Set(Of Integer)(i, 0, -1)
                dst1.Circle(pt, 5, cv.Scalar.Green, -1, cv.LineTypes.AntiAlias)
            End If
            trainData.Set(Of Single)(i, 0, pt.X)
            trainData.Set(Of Single)(i, 1, pt.Y)
        Next

        Using svmx = cv.ML.SVM.Create()
            svmx.Train(trainData, cv.ML.SampleTypes.RowSample, response)

            Dim sampleMat As New cv.Mat(1, 2, cv.MatType.CV_32F)
            Dim granularity = svmOptions.sliders.TrackBar2.Value
            Dim blueCount As Integer = 0
            For y = 0 To dst2.Height - 1 Step granularity
                For x = 0 To dst2.Width - 1 Step granularity
                    sampleMat.Set(Of Single)(0, 0, x)
                    sampleMat.Set(Of Single)(0, 1, y)
                    If CInt(svmx.Predict(sampleMat)) >= 0 Then
                        dst2.Circle(New cv.Point(x, y), 5, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
                    Else
                        dst2.Circle(New cv.Point(x, y), 5, cv.Scalar.Green, -1, cv.LineTypes.AntiAlias)
                    End If
                Next
            Next
            label2 = "There were " + CStr(blueCount) + " blue predictions"
            dst1.Rectangle(ocvb.drawRect, cv.Scalar.Black, 2)
            dst2.Rectangle(ocvb.drawRect, cv.Scalar.Black, 2)
        End Using
    End Sub
End Class






' this class encapsulates the simplest elements of SVM and is used above.  It does not inherit from ocvbClass.
Public Class SVM_Simple
    Public Function RunSVM(svmx As cv.ML.SVM, src As cv.Mat, trainInput As cv.Mat, response As cv.Mat, granularity As Integer) As cv.Mat
        Dim dst As New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        svmx.Train(trainInput, cv.ML.SampleTypes.RowSample, response)
        Dim sampleMat As New cv.Mat(1, 2, cv.MatType.CV_32F)
        For x = 0 To src.Height - 1 Step granularity
            For y = 0 To src.Height - 1 Step granularity
                sampleMat.Set(Of Single)(0, 0, x / CSng(src.Height))
                sampleMat.Set(Of Single)(0, 1, y / CSng(src.Height))
                Dim ret = svmx.Predict(sampleMat)
                Dim plotRect = New cv.Rect(x, src.Height - 1 - y, granularity * 2, granularity * 2)
                If ret = 1 Then
                    dst.Rectangle(plotRect, cv.Scalar.Red, -1)
                ElseIf ret = 2 Then
                    dst.Rectangle(plotRect, cv.Scalar.GreenYellow, -1)
                End If
            Next
        Next
        Return dst
    End Function
End Class