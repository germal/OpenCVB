Imports cv = OpenCvSharp
#If USE_NUMPY Then
Imports py = Python.Runtime
#End If
Imports System.Runtime.InteropServices
Public Class Gradient_Basics
    Inherits VBparent
    Public sobel As Edges_Sobel
    Public Sub New()
        initParent()
        sobel = New Edges_Sobel()
        task.desc = "Use phase to compute gradient"
        label2 = "Phase Output"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        sobel.src = src
        sobel.Run()
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
    Inherits VBparent
    Dim sobel As Edges_Sobel
    Public Sub New()
        initParent()
        sobel = New Edges_Sobel()
        task.desc = "Use phase to compute gradient on depth image"
        label2 = "Phase Output"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If task.drawRect.Width > 0 Then sobel.src = task.RGBDepth(task.drawRect) Else sobel.src = task.RGBDepth.Clone()
        sobel.Run()
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
    Inherits VBparent
    Dim grade As Gradient_Basics
    Public Sub New()
        initParent()
        grade = New Gradient_Basics()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Reduction Factor", 1, 64, 16)
        End If
        task.desc = "Reduced grayscale shows isobars in depth."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim reductionFactor = sliders.trackbar(0).Maximum - sliders.trackbar(0).Value
        dst1 = task.RGBDepth.Clone()
        dst1 /= reductionFactor
        dst1 *= reductionFactor
        grade.src = src
        grade.Run()
        dst2 = grade.dst2
    End Sub
End Class






' https://github.com/anopara/genetic-drawing
Public Class Gradient_CartToPolar
    Inherits VBparent
    Public basics As Gradient_Basics
    Public magnitude As New cv.Mat
    Public angle As New cv.Mat
    Public Sub New()
        initParent()
        basics = New Gradient_Basics()

        Static ksizeSlider = findSlider("Sobel kernel Size")
        ksizeSlider.value = 1

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Contrast exponent to use X100", 0, 200, 30)
        End If
        label1 = "CartToPolar Magnitude Output Normalized"
        label2 = "CartToPolar Angle Output"
        task.desc = "Compute the gradient and use CartToPolar to image the magnitude and angle"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        src.ConvertTo(basics.src, cv.MatType.CV_32FC3, 1 / 255)
        basics.Run()

        basics.sobel.grayX.ConvertTo(dst1, cv.MatType.CV_32F)
        basics.sobel.grayX.ConvertTo(dst2, cv.MatType.CV_32F)

        cv.Cv2.CartToPolar(dst1, dst2, magnitude, angle, True)
        magnitude = magnitude.Normalize()
        Static contrastSlider = findSlider("Contrast exponent to use X100")
        Dim exponent = contrastSlider.Value / 100
        magnitude = magnitude.Pow(exponent)

        dst1 = magnitude
    End Sub
End Class






#If USE_NUMPY Then
' https://github.com/SciSharp/Numpy.NET
'Public Class Gradient_NumPy
'    Inherits VBparent
'    Public gradient As Gradient_Basics
'    Public magnitude As New cv.Mat
'    Public angle As New cv.Mat
'    Public Sub New()
'        initParent()
'        gradient = New Gradient_Basics()
'        gradient.sobel.sliders.trackbar(0).Value = 1
'        If findfrm(caller + " Slider Options") Is Nothing Then
'            sliders.Setup(caller)
'            sliders.setupTrackBar(0, "Contrast exponent to use X100", 0, 200, 30)
'        endif
'        label1 = "CartToPolar Magnitude Output Normalized"
'        label2 = "CartToPolar Angle Output"
'        task.desc = "Compute the gradient and use CartToPolar to image the magnitude and angle"
'    End Sub
'    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
'        src.ConvertTo(gradient.src, cv.MatType.CV_32FC3, 1 / 255)
'        gradient.Run()

'        If ocvb.parms.NumPyEnabled Then
'            gradient.sobel.dst1.ConvertTo(dst1, cv.MatType.CV_32F)
'            gradient.sobel.dst2.ConvertTo(dst2, cv.MatType.CV_32F)

'            cv.Cv2.CartToPolar(dst1, dst2, magnitude, angle, True)
'            magnitude = magnitude.Normalize()
'            Dim npMag = MatToNumPyFloat(magnitude)
'            Dim exponent = sliders.trackbar(0).Value / 100
'            Numpy.np.power(npMag, exponent, npMag)
'            NumPyFloatToMat(npMag, dst1)
'        Else
'            ocvb.trueText("Enable Embedded NumPy in the OptionsDialog")
'        End If
'    End Sub
'End Class
#End If
