Imports cv = OpenCvSharp
Imports CS_Classes
Public Class Blur_Gaussian
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Kernel Size", 1, 32, 5)
        ocvb.desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize As Int32 = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
        cv.Cv2.GaussianBlur(ocvb.color, dst1, New cv.Size(kernelSize, kernelSize), 0, 0)
    End Sub
End Class


Public Class Blur_Gaussian_CS
    Inherits ocvbClass
    Dim CS_BlurGaussian As New CS_BlurGaussian
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Kernel Size", 1, 32, 5)
        ocvb.desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        CS_BlurGaussian.Run(ocvb.color, dst1, sliders.TrackBar1.Value)
    End Sub
End Class



Public Class Blur_Median_CS
    Inherits ocvbClass
    Dim CS_BlurMedian As New CS_BlurMedian
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Kernel Size", 1, 32, 5)
        ocvb.desc = "Replace each pixel with the median of neighborhood of varying sizes."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        CS_BlurMedian.Run(ocvb.color, dst1, sliders.TrackBar1.Value)
    End Sub
End Class



Public Class Blur_Homogeneous
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Kernel Size", 1, 32, 5)
        ocvb.desc = "Smooth each pixel with a kernel of 1's of different sizes."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize As Int32 = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
        dst1 = ocvb.color.Blur(New cv.Size(kernelSize, kernelSize), New cv.Point(-1, -1))
        dst2 = ocvb.RGBDepth.Blur(New cv.Size(kernelSize, kernelSize), New cv.Point(-1, -1))
    End Sub
End Class



Public Class Blur_Median
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Kernel Size", 1, 32, 5)
        ocvb.desc = "Replace each pixel with the median of neighborhood of varying sizes."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize As Int32 = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
        cv.Cv2.MedianBlur(ocvb.color, dst1, kernelSize)
    End Sub
End Class



Public Class Blur_Bilateral
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Kernel Size", 1, 32, 5)
        ocvb.desc = "Smooth each pixel with a Gaussian kernel of different sizes but preserve edges"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize As Int32 = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
        If standalone Then src = ocvb.color.Clone()

        cv.Cv2.BilateralFilter(src, dst1, kernelSize, kernelSize * 2, kernelSize / 2)
    End Sub
End Class




Public Class Blur_PlusHistogram
    Inherits ocvbClass
    Dim mat2to1 As Mat_2to1
    Dim blur As Blur_Bilateral
    Dim myhist As Histogram_EqualizeGray
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        mat2to1 = New Mat_2to1(ocvb, caller)

        blur = New Blur_Bilateral(ocvb, caller)

        myhist = New Histogram_EqualizeGray(ocvb, caller)

        ocvb.desc = "Compound algorithms Blur and Histogram"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        myhist.src = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        myhist.Run(ocvb)

        mat2to1.mat(0) = myhist.dst2.Clone()

        blur.src = myhist.src
        blur.sliders.TrackBar1.Value = 15 ' kernel size is big to get a blur...
        blur.Run(ocvb)

        myhist.src = blur.dst1
        myhist.Run(ocvb)
        mat2to1.mat(1) = myhist.dst2.Clone()
        mat2to1.Run(ocvb)
        dst2 = mat2to1.dst1
        dst1 = myhist.src
        label2 = "Top is before, Bottom is after"
    End Sub
End Class

