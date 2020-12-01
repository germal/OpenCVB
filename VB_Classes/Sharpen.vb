Imports cv = OpenCvSharp
Public Class Sharpen_UnsharpMask
    Inherits VBparent
    Public Sub New()
        initParent()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "sigma", 1, 2000, 100)
        sliders.setupTrackBar(1, "threshold", 0, 255, 5)
        sliders.setupTrackBar(2, "Shift Amount", 0, 5000, 1000)
        task.desc = "Sharpen an image - Painterly Effect"
        label2 = "Unsharp mask (difference from Blur)"
    End Sub
    Public Sub Run()
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim blurred As New cv.Mat
        Dim sigma As Double = sliders.trackbar(0).Value / 100
        Dim threshold As Double = sliders.trackbar(1).Value
        Dim amount As Double = sliders.trackbar(2).Value / 1000
        cv.Cv2.GaussianBlur(src, dst2, New cv.Size(), sigma, sigma)

        Dim diff As New cv.Mat
        cv.Cv2.Absdiff(src, dst2, diff)
        diff = diff.Threshold(threshold, 255, cv.ThresholdTypes.Binary)
        dst1 = src * (1 + amount) + diff * (-amount)
        diff.CopyTo(dst2)
    End Sub
End Class



' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class Sharpen_DetailEnhance
    Inherits VBparent
    Public Sub New()
        initParent()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "DetailEnhance Sigma_s", 0, 200, 60)
        sliders.setupTrackBar(1, "DetailEnhance Sigma_r", 1, 100, 7)
        task.desc = "Enhance detail on an image - Painterly Effect"
    End Sub
    Public Sub Run()
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim sigma_s = sliders.trackbar(0).Value
        Dim sigma_r = sliders.trackbar(1).Value / sliders.trackbar(1).Maximum
        cv.Cv2.DetailEnhance(src, dst1, sigma_s, sigma_r)
    End Sub
End Class




' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class Sharpen_Stylize
    Inherits VBparent
    Public Sub New()
        initParent()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Stylize Sigma_s", 0, 200, 60)
        sliders.setupTrackBar(1, "Stylize Sigma_r", 1, 100, 7)
        task.desc = "Stylize an image - Painterly Effect"
    End Sub
    Public Sub Run()
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim sigma_s = sliders.trackbar(0).Value
        Dim sigma_r = sliders.trackbar(1).Value / sliders.trackbar(1).Maximum
        cv.Cv2.DetailEnhance(src, dst1, sigma_s, sigma_r)
    End Sub
End Class


