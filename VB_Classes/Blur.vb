Imports cv = OpenCvSharp
Imports CS_Classes
Public Class Blur_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Blur Kernel Size", 0, 32, 5)
        ocvb.desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim kernelSize As integer = sliders.trackbar(0).Value
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            cv.Cv2.GaussianBlur(src, dst1, New cv.Size(kernelSize, kernelSize), 0, 0)
            If standalone Then cv.Cv2.GaussianBlur(ocvb.task.RGBDepth, dst2, New cv.Size(kernelSize, kernelSize), 0, 0)
        Else
            dst1 = src
        End If
    End Sub
End Class






Public Class Blur_Gaussian_CS
    Inherits VBparent
    Dim CS_BlurGaussian As New CS_BlurGaussian
    Dim blur As Blur_Basics
    Public Sub New()
        initParent()
        blur = New Blur_Basics()
        ocvb.desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = blurKernelSlider.Value
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            CS_BlurGaussian.Run(src, dst1, kernelSize)
            If standalone Then CS_BlurGaussian.Run(ocvb.task.RGBDepth, dst2, kernelSize)
        Else
            dst1 = src
        End If
    End Sub
End Class






Public Class Blur_Median_CS
    Inherits VBparent
    Dim CS_BlurMedian As New CS_BlurMedian
    Dim blur As Blur_Basics
    Public Sub New()
        initParent()
        blur = New Blur_Basics()
        ocvb.desc = "Replace each pixel with the median of neighborhood of varying sizes."
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = blurKernelSlider.Value
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            CS_BlurMedian.Run(src, dst1, kernelSize)
            If standalone Then CS_BlurMedian.Run(ocvb.task.RGBDepth, dst2, kernelSize)
        Else
            dst1 = src
        End If
    End Sub
End Class






Public Class Blur_Homogeneous
    Inherits VBparent
    Dim blur As Blur_Basics
    Public Sub New()
        initParent()
        blur = New Blur_Basics()
        ocvb.desc = "Smooth each pixel with a kernel of 1's of different sizes."
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = CDbl(blurKernelSlider.Value)
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            dst1 = src.Blur(New cv.Size(kernelSize, kernelSize), New cv.Point(-1, -1))
            If standalone Then dst2 = ocvb.task.RGBDepth.Blur(New cv.Size(kernelSize, kernelSize), New cv.Point(-1, -1))
        Else
            dst1 = src
            dst2 = ocvb.task.RGBDepth
        End If
    End Sub
End Class







Public Class Blur_Median
    Inherits VBparent
    Dim blur As Blur_Basics
    Public Sub New()
        initParent()
        blur = New Blur_Basics()
        ocvb.desc = "Replace each pixel with the median of neighborhood of varying sizes."
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = CDbl(blurKernelSlider.Value)
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            cv.Cv2.MedianBlur(src, dst1, kernelSize)
            If standalone Then cv.Cv2.MedianBlur(ocvb.task.RGBDepth, dst2, kernelSize)
        Else
            dst1 = src
            dst2 = ocvb.task.RGBDepth
        End If
    End Sub
End Class





' https://docs.opencv.org/2.4/modules/imgproc/doc/filtering.html?highlight=bilateralfilter
' https://www.tutorialspoint.com/opencv/opencv_bilateral_filter.htm
Public Class Blur_Bilateral
    Inherits VBparent
    Dim blur As Blur_Basics
    Public Sub New()
        initParent()
        blur = New Blur_Basics()
        ocvb.desc = "Smooth each pixel with a Gaussian kernel of different sizes but preserve edges"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static blurKernelSlider = findSlider("Blur Kernel Size")
        Dim kernelSize = CDbl(blurKernelSlider.Value)
        If kernelSize > 0 Then
            If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
            cv.Cv2.BilateralFilter(src, dst1, kernelSize, kernelSize * 2, kernelSize / 2)
        Else
            dst1 = src
        End If
    End Sub
End Class






Public Class Blur_PlusHistogram
    Inherits VBparent
    Dim mat2to1 As Mat_2to1
    Dim blur As Blur_Bilateral
    Dim myhist As Histogram_EqualizeGray
    Public Sub New()
        initParent()
        mat2to1 = New Mat_2to1()

        blur = New Blur_Bilateral()
        myhist = New Histogram_EqualizeGray()

        label1 = "Use Blur slider to see impact on histograms"
        label2 = "Top is before equalize, Bottom is after Equalize"
        ocvb.desc = "Compound algorithms Blur and Histogram"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        myhist.src = src
        myhist.Run()

        mat2to1.mat(0) = myhist.dst1.Clone()

        blur.src = myhist.src
        blur.Run()

        myhist.src = blur.dst1.Clone
        myhist.Run()

        mat2to1.mat(1) = myhist.dst2.Clone()
        mat2to1.Run()
        dst2 = mat2to1.dst1
        dst1 = blur.dst1
    End Sub
End Class






Public Class Blur_TopoMap
    Inherits VBparent
    Dim gradient As Gradient_CartToPolar
    Public Sub New()
        initParent()

        gradient = New Gradient_CartToPolar()

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Percent of Blurring", 0, 100, 20)
        sliders.setupTrackBar(1, "Reduction Factor", 2, 64, 20)
        sliders.setupTrackBar(2, "Frame Count Cycle", 1, 200, 50)

        label1 = "Image Gradient"
        ocvb.desc = "Create a topo map from the blurred image"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static savePercent As Single
        Static nextPercent As Single
        If savePercent <> sliders.trackbar(0).Value Then
            savePercent = sliders.trackbar(0).Value
            nextPercent = savePercent
        End If

        Dim kernelSize = CInt(nextPercent / 100 * src.Width)
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        gradient.src = src
        gradient.Run()
        dst1 = gradient.magnitude

        If kernelSize > 1 Then cv.Cv2.GaussianBlur(dst1, dst2, New cv.Size(kernelSize, kernelSize), 0, 0)
        dst2 = dst2.Normalize(255)
        dst2 = dst2.ConvertScaleAbs(255)

        dst2 = dst2 / sliders.trackbar(1).Value
        dst2 *= sliders.trackbar(1).Value

        label2 = "Blur = " + CStr(nextPercent) + "% Reduction Factor = " + CStr(sliders.trackbar(1).Value)
        If ocvb.frameCount Mod sliders.trackbar(2).Value = 0 Then nextPercent -= 1
        If nextPercent <= 0 Then nextPercent = savePercent
    End Sub
End Class
