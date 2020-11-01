Imports cv = OpenCvSharp
Public Class Object_Basics
    Inherits VBparent
    Dim inrange As Depth_InRange
    Dim ccomp As CComp_ColorDepth
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        inrange = New Depth_InRange(ocvb)

        ccomp = New CComp_ColorDepth(ocvb)

        label1 = "Connected components for objects in the foreground - tracker algorithm"
        label2 = "Mask for background"
        ocvb.desc = "Identify objects in the foreground."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        inrange.src = getDepth32f(ocvb)
        inrange.Run(ocvb)
        If standalone Then
            dst1 = inrange.depthMask
            dst2 = inrange.noDepthMask
        End If

        ccomp.src.SetTo(0)
        src.CopyTo(ccomp.src, inrange.depthMask)
        ccomp.Run(ocvb)
        dst1 = ccomp.dst1
    End Sub
End Class

