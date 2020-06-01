Imports cv = OpenCvSharp
Public Class Object_Basics
    Inherits ocvbClass
    Dim trim As Depth_InRange
    Dim ccomp As CComp_ColorDepth
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        trim = New Depth_InRange(ocvb)

        ccomp = New CComp_ColorDepth(ocvb)

        label1 = "Connected components for objects in the foreground - tracker algorithm"
        label2 = "Mask for background"
        ocvb.desc = "Identify objects in the foreground."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        If standalone Then
            dst1 = trim.Mask
            dst2 = trim.zeroMask
        End If

        ccomp.src.SetTo(0)
        src.CopyTo(ccomp.src, trim.Mask)
        ccomp.Run(ocvb)
        dst1 = ccomp.dst1
    End Sub
End Class
