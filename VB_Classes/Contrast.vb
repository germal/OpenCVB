Imports cv = OpenCvSharp
Public Class Contrast_POW
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Contrast exponent to use X100", 0, 200, 30)
        label1 = "Original Image"
        label2 = "Contrast reduced"
        ocvb.desc = "Reduce contrast with POW function"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1.ConvertTo(dst2, cv.MatType.CV_32FC3)
        dst2 = dst2.Normalize()

        Dim exponent = sliders.trackbar(0).Value / 100
        dst2 = dst2.Pow(exponent)
    End Sub
End Class






Public Class Contrast_Basics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Brightness", 1, 100, 50)
        sliders.setupTrackBar(1, "Contrast", 1, 100, 50)
        ocvb.desc = "Show image with varying contrast and brightness."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        src.ConvertTo(dst1, -1, sliders.trackbar(1).Value / 50, sliders.trackbar(0).Value)
        label1 = "Brightness/Contrast"
        label2 = ""
    End Sub
End Class