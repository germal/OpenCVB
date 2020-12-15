Imports cv = OpenCvSharp
Public Class Threshold_LaplacianFilter
    Inherits VBparent
    Dim edges As Filter_Laplacian
    Public Sub New()
        initParent()
        task.inrange.depth32fAfterMasking = True

        edges = New Filter_Laplacian()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "dist Threshold", 1, 100, 40)
        End If
        label1 = "Foreground Input"
        task.desc = "Threshold the output of a Laplacian derivative, mask with depth foreground.  needs more work"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        edges.src = src
        edges.Run()
        dst2 = edges.dst2
        dst1 = task.inrange.dst1

        Dim mask = dst1.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)
        dst2.SetTo(0, mask)
    End Sub
End Class


