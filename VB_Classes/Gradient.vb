Imports cv = OpenCvSharp
Imports Numpy
Imports py = Python.Runtime
Imports System.Runtime.InteropServices
Public Class Gradient_Basics
    Inherits ocvbClass
    Public sobel As Edges_Sobel
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sobel = New Edges_Sobel(ocvb)
        ocvb.desc = "Use phase to compute gradient"
        label2 = "Phase Output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        sobel.src = src
        sobel.Run(ocvb)
        Dim angle = New cv.Mat
        Dim x32f As New cv.Mat
        Dim y32f As New cv.Mat
        sobel.grayX.ConvertTo(x32f, cv.MatType.CV_32F)
        sobel.grayY.ConvertTo(y32f, cv.MatType.CV_32F)
        cv.Cv2.Phase(x32f, y32f, angle)
        Dim gray = angle.Normalize(255, 0, cv.NormTypes.MinMax)
        gray.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst1 = sobel.dst1
    End Sub
End Class




Public Class Gradient_Depth
    Inherits ocvbClass
    Dim sobel As Edges_Sobel
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sobel = New Edges_Sobel(ocvb)
        ocvb.desc = "Use phase to compute gradient on depth image"
        label2 = "Phase Output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.drawRect.Width > 0 Then sobel.src = ocvb.RGBDepth(ocvb.drawRect) Else sobel.src = ocvb.RGBDepth.Clone()
        sobel.Run(ocvb)
        Dim angle = New cv.Mat
        Dim x32f As New cv.Mat
        Dim y32f As New cv.Mat
        sobel.grayX.ConvertTo(x32f, cv.MatType.CV_32F)
        sobel.grayY.ConvertTo(y32f, cv.MatType.CV_32F)
        cv.Cv2.Phase(x32f, y32f, angle)
        Dim gray = angle.Normalize(255, 0, cv.NormTypes.MinMax)
        gray.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst1 = sobel.dst1
    End Sub
End Class






Public Class Gradient_Flatland
    Inherits ocvbClass
    Dim grade As Gradient_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grade = New Gradient_Basics(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Reduction Factor", 1, 64, 16)
        ocvb.desc = "Reduced grayscale shows isobars in depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim reductionFactor = sliders.trackbar(0).Maximum - sliders.trackbar(0).Value
        dst1 = ocvb.RGBDepth.Clone()
        dst1 /= reductionFactor
        dst1 *= reductionFactor
        grade.src = src
        grade.Run(ocvb)
        dst2 = grade.dst2
    End Sub
End Class







' https://github.com/anopara/genetic-drawing
Public Class Gradient_CartToPolar
    Inherits ocvbClass
    Public basics As Gradient_Basics
    Public magnitude As New cv.Mat
    Public angle As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        basics = New Gradient_Basics(ocvb)
        basics.sobel.sliders.trackbar(0).Value = 1

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Contrast exponent to use X100", 0, 200, 30)
        label1 = "CartToPolar Magnitude Output Normalized"
        label2 = "CartToPolar Angle Output"
        ocvb.desc = "Compute the gradient and use CartToPolar to image the magnitude and angle"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        src.ConvertTo(basics.src, cv.MatType.CV_32FC3, 1 / 255)
        basics.Run(ocvb)

        basics.sobel.grayX.ConvertTo(dst1, cv.MatType.CV_32F)
        basics.sobel.grayY.ConvertTo(dst2, cv.MatType.CV_32F)

        cv.Cv2.CartToPolar(dst1, dst2, magnitude, angle, True)
        magnitude = magnitude.Normalize()
        Dim exponent = sliders.trackbar(0).Value / 100
        magnitude = magnitude.Pow(exponent)

        dst1 = magnitude
    End Sub
End Class






' https://github.com/SciSharp/Numpy.NET
Public Class Gradient_NumPy
    Inherits ocvbClass
    Public gradient As Gradient_Basics
    Public magnitude As New cv.Mat
    Public angle As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        gradient = New Gradient_Basics(ocvb)
        gradient.sobel.sliders.trackbar(0).Value = 1

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Contrast exponent to use X100", 0, 200, 30)

        label1 = "CartToPolar Magnitude Output Normalized"
        label2 = "CartToPolar Angle Output"
        ocvb.desc = "Compute the gradient and use CartToPolar to image the magnitude and angle"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        src.ConvertTo(gradient.src, cv.MatType.CV_32FC3, 1 / 255)
        gradient.Run(ocvb)

        If ocvb.parms.NumPyEnabled Then
            gradient.sobel.grayX.ConvertTo(dst1, cv.MatType.CV_32F)
            gradient.sobel.grayY.ConvertTo(dst2, cv.MatType.CV_32F)

            cv.Cv2.CartToPolar(dst1, dst2, magnitude, angle, True)
            magnitude = magnitude.Normalize()
            Dim npMag = MatToNumPyFloat(magnitude)
            Dim exponent = sliders.trackbar(0).Value / 100
            Numpy.np.power(npMag, exponent, npMag)
            NumPyFloatToMat(npMag, dst1)
        Else
            ocvb.putText(New TTtext("Enable Embedded NumPy in the OptionsDialog", 10, 60, RESULT1))
        End If
    End Sub
End Class
