Imports cv = OpenCvSharp
Public Class Object_Basics
    Inherits VBparent
    Dim ccomp As CComp_ColorDepth
    Public Sub New()
        initParent()

        ccomp = New CComp_ColorDepth()

        label1 = "Connected components for objects in the foreground - tracker algorithm"
        label2 = "Mask for background"
        task.desc = "Identify objects in the foreground."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone Then
            dst1 = task.inrange.depthMask
            dst2 = task.inrange.noDepthMask
        End If

        ccomp.src.SetTo(0)
        src.CopyTo(ccomp.src, task.inrange.depthMask)
        ccomp.Run()
        dst1 = ccomp.dst1
    End Sub
End Class

