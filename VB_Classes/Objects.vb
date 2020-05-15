Imports cv = OpenCvSharp
Public Class Object_Basics
    Inherits ocvbClass
    Dim trim As Depth_InRange
    Dim ccomp As CComp_ColorDepth
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        trim = New Depth_InRange(ocvb, caller)

        ccomp = New CComp_ColorDepth(ocvb, caller)

        label1 = "Connected components for objects in the foreground"
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
