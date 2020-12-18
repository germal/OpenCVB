Imports cv = OpenCvSharp
' https://github.com/spmallick/learnopencv/tree/master/Photoshop-Filters-in-OpenCV
Public Class PhotoShop_Sepia
    Inherits VBparent
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Sepia Multiplier X100", 0, 1000, 100)
        End If

        task.desc = "Create a sepia image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2RGB)
        Static multSlider = findSlider("Sepia Multiplier X100")
        Dim m As Double = multSlider.value / 100
        Dim tMatrix = New cv.Mat(3, 3, cv.MatType.CV_64F, {{0.393 * m, 0.769 * m, 0.189 * m}, {0.349 * m, 0.686 * m, 0.168 * m}, {0.272 * m, 0.534 * m, 0.131 * m}})
        dst1 = dst1.Transform(tMatrix).Threshold(255, 255, cv.ThresholdTypes.Trunc)
    End Sub
End Class








Public Class PhotoShop_Emboss
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Use the video stream to make it appear like an embossed paper image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
    End Sub
End Class