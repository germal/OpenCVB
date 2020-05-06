Imports cv = OpenCvSharp
Imports CS_Classes
Public Class Blur_Gaussian
    Inherits VB_Class
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Kernel Size", 1, 32, 5)
                ocvb.desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize As Int32 = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
        cv.Cv2.GaussianBlur(ocvb.color, ocvb.result1, New cv.Size(kernelSize, kernelSize), 0, 0)
    End Sub
    Public Sub MyDispose()
            End Sub
End Class


Public Class Blur_Gaussian_CS
    Inherits VB_Class
        Dim CS_BlurGaussian As New CS_BlurGaussian
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Kernel Size", 1, 32, 5)
                ocvb.desc = "Smooth each pixel with a Gaussian kernel of different sizes."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        CS_BlurGaussian.Run(ocvb.color, ocvb.result1, sliders.TrackBar1.Value)
    End Sub
    Public Sub MyDispose()
            End Sub
End Class



Public Class Blur_Median_CS
    Inherits VB_Class
        Dim CS_BlurMedian As New CS_BlurMedian
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Kernel Size", 1, 32, 5)
                ocvb.desc = "Replace each pixel with the median of neighborhood of varying sizes."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        CS_BlurMedian.Run(ocvb.color, ocvb.result1, sliders.TrackBar1.Value)
    End Sub
    Public Sub MyDispose()
            End Sub
End Class



Public Class Blur_Homogeneous
    Inherits VB_Class
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Kernel Size", 1, 32, 5)
                ocvb.desc = "Smooth each pixel with a kernel of 1's of different sizes."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize As Int32 = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
        ocvb.result1 = ocvb.color.Blur(New cv.Size(kernelSize, kernelSize), New cv.Point(-1, -1))
        ocvb.result2 = ocvb.RGBDepth.Blur(New cv.Size(kernelSize, kernelSize), New cv.Point(-1, -1))
    End Sub
    Public Sub MyDispose()
            End Sub
End Class



Public Class Blur_Median
    Inherits VB_Class
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Kernel Size", 1, 32, 5)
                ocvb.desc = "Replace each pixel with the median of neighborhood of varying sizes."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize As Int32 = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
        cv.Cv2.MedianBlur(ocvb.color, ocvb.result1, kernelSize)
    End Sub
    Public Sub MyDispose()
            End Sub
End Class



Public Class Blur_Bilateral
    Inherits VB_Class
        Public src As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Kernel Size", 1, 32, 5)
                ocvb.desc = "Smooth each pixel with a Gaussian kernel of different sizes but preserve edges"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize As Int32 = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
        If externalUse = False Then src = ocvb.color.Clone()
        cv.Cv2.BilateralFilter(src, ocvb.result1, kernelSize, kernelSize * 2, kernelSize / 2)
    End Sub
    Public Sub MyDispose()
            End Sub
End Class




Public Class Blur_PlusHistogram
    Inherits VB_Class
    Dim mat2to1 As Mat_2to1
    Dim blur As Blur_Bilateral
    Dim myhist As Histogram_EqualizeGray
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        mat2to1 = New Mat_2to1(ocvb, callerName)
        mat2to1.externalUse = True

        blur = New Blur_Bilateral(ocvb, callerName)
        blur.externalUse = True

        myhist = New Histogram_EqualizeGray(ocvb, callerName)
        myhist.externalUse = True

        ocvb.desc = "Compound algorithms Blur and Histogram"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        myhist.histogram.gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        myhist.Run(ocvb)
        mat2to1.mat(0) = ocvb.result2.Clone()

        blur.src = ocvb.result1.Clone()
        blur.Run(ocvb)

        myhist.histogram.gray = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        myhist.Run(ocvb)
        mat2to1.mat(1) = ocvb.result2.Clone()
        mat2to1.Run(ocvb)
        ocvb.label2 = "Top is before, Bottom is after"
    End Sub
    Public Sub MyDispose()
        blur.Dispose()
        myhist.Dispose()
        mat2to1.Dispose()
    End Sub
End Class
