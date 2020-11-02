Imports cv = OpenCvSharp
Public Class AddWeighted_Basics
    Inherits VBparent
    Public src1 As New cv.Mat
    Public src2 As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Weight", 0, 100, 50)
        ocvb.desc = "Add 2 images with specified weights."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone Then
            src1 = src
            src2 = ocvb.RGBDepth
        End If
        Static weightSlider = findSlider("Weight")
        If weightSlider IsNot Nothing Then
            Dim alpha = weightSlider.Value / weightSlider.Maximum
            cv.Cv2.AddWeighted(src1, alpha, src2, 1.0 - alpha, 0, dst1)
            label1 = "depth " + Format(1 - weightSlider.Value / 100, "#0%") + " RGB " + Format(weightSlider.Value / 100, "#0%")
        End If
    End Sub
End Class


