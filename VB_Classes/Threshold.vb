Imports cv = OpenCvSharp
Public Class Threshold_LaplacianFilter
    Inherits VBparent
    Dim edges As Filter_Laplacian
    Dim inrange As Depth_InRange
    Public Sub New()
        initParent()
        inrange = New Depth_InRange()
        inrange.depth32fAfterMasking = True

        edges = New Filter_Laplacian()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "dist Threshold", 1, 100, 40)
        label1 = "Foreground Input"
        task.desc = "Threshold the output of a Laplacian derivative, mask with depth foreground.  needs more work"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        edges.src = src
        edges.Run()
        dst2 = edges.dst2
        inrange.src = getDepth32f()
        inrange.Run()
        dst1 = inrange.dst1

        Dim mask = dst1.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)
        dst2.SetTo(0, mask)
    End Sub
End Class


