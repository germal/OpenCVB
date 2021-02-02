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
        If standalone Then dst2.SetTo(0)
        label1 = "depth " + Format(1 - weightSlider.Value / 100, "#0%") + " RGB " + Format(weightSlider.Value / 100, "#0%")
    End Sub
End Class







Public Class AddWeighted_Edges
    Inherits VBparent
    Dim edges As Edges_BinarizedSobel
    Dim addw As AddWeighted_Basics
    Public Sub New()
        initParent()
        edges = New Edges_BinarizedSobel
        addw = New AddWeighted_Basics
        Dim weightSlider = findSlider("Weight")
        weightSlider.Value = 75
        task.desc = "Add in the edges separating light and dark to the color image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        edges.src = src
        edges.Run()
        dst1 = edges.dst2

        addw.src = task.color
        addw.src2 = edges.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        addw.Run()
        dst2 = addw.dst1
    End Sub
End Class


