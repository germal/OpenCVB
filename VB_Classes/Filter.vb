Imports cv = OpenCvSharp
' https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Filter_Laplacian : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        ocvb.desc = "Use a filter to approximate the Laplacian derivative."
        ocvb.label1 = "Sharpened image using Filter2D output"
        ocvb.label2 = "Output of Filter2D (approximated Laplacian)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernel = New cv.Mat(3, 3, cv.MatType.CV_32FC1, New Single() {1, 1, 1, 1, -8, 1, 1, 1, 1})
        Dim imgLaplacian = ocvb.color.Filter2D(cv.MatType.CV_32F, kernel)
        Dim sharp As New cv.Mat
        ocvb.color.ConvertTo(sharp, cv.MatType.CV_32F)
        Dim imgResult As cv.Mat = sharp - imgLaplacian
        imgResult.ConvertTo(imgResult, cv.MatType.CV_8UC3)
        imgResult.ConvertTo(ocvb.result1, cv.MatType.CV_8UC3)
        imgLaplacian.ConvertTo(ocvb.result2, cv.MatType.CV_8UC3)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class


Public Class Filter_NormalizedKernel : Implements IDisposable
    Dim radio As New OptionsRadioButtons
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        radio.Setup(ocvb, 4)
        radio.check(0).Text = "INF"
        radio.check(1).Text = "L1"
        radio.check(1).Checked = True
        radio.check(2).Text = "L2"
        radio.check(3).Text = "MinMax"
        If ocvb.parms.ShowOptions Then radio.Show()
        ocvb.desc = "Create a normalized kernel and use it."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernel = New cv.Mat(1, 21, cv.MatType.CV_32FC1, New Single() {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1})
        Dim normType = cv.NormTypes.L1
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                normType = Choose(i + 1, cv.NormTypes.INF, cv.NormTypes.L1, cv.NormTypes.L2, cv.NormTypes.MinMax)
                Exit For
            End If
        Next

        kernel = kernel.Normalize(1, 0, normType)

        Dim sum As Double
        For i = 0 To kernel.Width - 1
            sum += Math.Abs(kernel.Get(Of Single)(0, i))
        Next
        ocvb.label1 = "kernel sum = " + Format(sum, "#0.000")

        Dim dst32f = ocvb.color.Filter2D(cv.MatType.CV_32FC1, kernel, anchor:=New cv.Point(0, 0))
        dst32f.ConvertTo(ocvb.result1, cv.MatType.CV_8UC3)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        radio.Dispose()
    End Sub
End Class


' https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/filter_2d/filter_2d.html
Public Class Filter_Normalized2D : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        ocvb.desc = "Create and apply a normalized kernel."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = 3 + (ocvb.frameCount Mod 20)
        Dim kernel = New cv.Mat(kernelSize, kernelSize, cv.MatType.CV_32F).SetTo(1 / (kernelSize * kernelSize))
        ocvb.result1 = ocvb.color.Filter2D(-1, kernel)
        ocvb.label1 = "Normalized KernelSize = " + CStr(kernelSize)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




'https://www.cc.gatech.edu/classes/AY2015/cs4475_summer/documents/smoothing_separable.py
Public Class Filter_SepFilter2D : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        check.Setup(ocvb, 1)
        check.Box(0).Text = "Show Difference SepFilter2D and Gaussian"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        sliders.setupTrackBar1(ocvb, "Kernel X size", 1, 21, 5)
        sliders.setupTrackBar2(ocvb, "Kernel Y size", 1, 21, 11)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.label1 = "Gaussian Blur result"
        ocvb.desc = "Apply kernel X then kernel Y with OpenCV's SepFilter2D and compare to Gaussian blur"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim xDim = sliders.TrackBar1.Value
        Dim yDim = sliders.TrackBar2.Value
        If xDim Mod 2 = 0 Then xDim += 1
        If yDim Mod 2 = 0 Then yDim += 1
        Dim kernel = cv.Cv2.GetGaussianKernel(xDim, 1.7)
        ocvb.result1 = ocvb.color.GaussianBlur(New cv.Size(xDim, yDim), 1.7)
        ocvb.result2 = ocvb.color.SepFilter2D(cv.MatType.CV_8UC3, kernel, kernel)
        If check.Box(0).Checked Then
            Dim graySep = ocvb.result2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim grayGauss = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            ocvb.result2 = (graySep - grayGauss).ToMat.Threshold(0, 255, cv.ThresholdTypes.Binary)
            ocvb.label2 = "Gaussian - SepFilter2D " + CStr(ocvb.result2.CountNonZero()) + " pixels different."
        Else
            ocvb.label2 = "SepFilter2D Result"
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        check.Dispose()
        sliders.Dispose()
    End Sub
End Class