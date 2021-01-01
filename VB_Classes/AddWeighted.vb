Imports cv = OpenCvSharp
Public Class AddWeighted_Basics
    Inherits VBparent
    Public src2 As New cv.Mat
    Public weightSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Weight", 0, 100, 50)
        End If
        weightSlider = findSlider("Weight")
        task.desc = "Add 2 images with specified weights."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone Or task.intermediateReview = caller Then src2 = task.RGBDepth ' external use must provide src2!
        Dim alpha = weightSlider.Value / 100
        cv.Cv2.AddWeighted(src, alpha, src2, 1.0 - alpha, 0, dst1)
        label1 = "depth " + Format(1 - weightSlider.Value / 100, "#0%") + " RGB " + Format(weightSlider.Value / 100, "#0%")
    End Sub
End Class


