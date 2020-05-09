Imports cv = OpenCvSharp
' https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Laplacian_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Laplacian Kernel size", 1, 21, 3)
        sliders.setupTrackBar2(ocvb, caller, "Laplacian Scale", 0, 100, 100)
        sliders.setupTrackBar3(ocvb, caller, "Laplacian Delta", 0, 1000, 0)
        ocvb.desc = "Laplacian filter - the second derivative."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.TrackBar1.Value()
        If kernelSize Mod 2 = 0 Then kernelSize += 1
        Dim scale = sliders.TrackBar2.Value / 100
        Dim delta = sliders.TrackBar3.Value / 100
        Dim ddepth = cv.MatType.CV_16S

        Dim src = ocvb.color.GaussianBlur(New cv.Size(kernelSize, kernelSize), 0, 0)
        Dim srcGray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim dst = srcGray.Laplacian(ddepth, kernelSize, scale, delta)
        ocvb.result1 = dst.ConvertScaleAbs()
        ocvb.label1 = "Laplacian Filter k = " + CStr(kernelSize)
		MyBase.Finish(ocvb)
    End Sub
End Class


' https://docs.opencv.org/3.2.0/de/db2/laplace_8cpp-example.html
Public Class Laplacian_Blur
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Laplacian Kernel size", 1, 21, 3)
        sliders.setupTrackBar2(ocvb, caller, "Laplacian Scale", 0, 100, 100)
        sliders.setupTrackBar3(ocvb, caller, "Laplacian Delta", 0, 1000, 0)

        radio.Setup(ocvb, caller, 3)
        radio.check(0).Text = "Add Gaussian Blur"
        radio.check(1).Text = "Add boxfilter Blur"
        radio.check(2).Text = "Add median Blur"
        radio.check(0).Checked = True
        ocvb.desc = "Laplacian filter - the second derivative - with different bluring techniques"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.TrackBar1.Value()
        If kernelSize Mod 2 = 0 Then kernelSize += 1
        Dim scale = sliders.TrackBar2.Value / 100
        Dim delta = sliders.TrackBar3.Value / 100
        Dim ddepth = cv.MatType.CV_16S

        Dim src As cv.Mat
        Dim blurText As String
        If radio.check(0).Checked Then
            src = ocvb.color.GaussianBlur(New cv.Size(kernelSize, kernelSize), 0, 0)
            blurText = "Gaussian"
        ElseIf radio.check(1).Checked Then
            src = ocvb.color.Blur(New cv.Size(kernelSize, kernelSize))
            blurText = "boxfilter"
        Else
            src = ocvb.color.MedianBlur(kernelSize)
            blurText = "MedianBlur"
        End If
        Dim srcGray = src.CvtColor(cv.ColorConversionCodes.bgr2gray)
        Dim dst = srcGray.Laplacian(ddepth, kernelSize, scale, delta)
        ocvb.result1 = dst.ConvertScaleAbs()
        ocvb.label1 = "Laplacian+" + blurText + " k = " + CStr(kernelSize)
		MyBase.Finish(ocvb)
    End Sub
End Class

