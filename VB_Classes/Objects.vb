Imports cv = OpenCvSharp
Public Class Object_Basics
    Inherits VBparent
    Dim inrange As Depth_InRange
    Dim ccomp As CComp_ColorDepth
    Public Sub New()
        initParent()
        inrange = New Depth_InRange()

        ccomp = New CComp_ColorDepth()

        label1 = "Connected components for objects in the foreground - tracker algorithm"
        label2 = "Mask for background"
        ocvb.desc = "Identify objects in the foreground."
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        inrange.src = getDepth32f()
        inrange.Run()
        If standalone Then
            dst1 = inrange.depthMask
            dst2 = inrange.noDepthMask
        End If

        ccomp.src.SetTo(0)
        src.CopyTo(ccomp.src, inrange.depthMask)
        ccomp.Run()
        dst1 = ccomp.dst1
    End Sub
End Class

