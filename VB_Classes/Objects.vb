Imports cv = OpenCvSharp
Public Class Object_Basics
    Inherits ocvbClass
    Dim trim As Depth_InRange
    Dim ccomp As CComp_EdgeMask
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        trim = New Depth_InRange(ocvb, caller)

        ccomp = New CComp_EdgeMask(ocvb, caller)

        ocvb.desc = "Identify objects in the foreground."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        If standalone Then
            dst1 = trim.Mask
            dst2 = trim.zeroMask
        End If

        ocvb.color.CopyTo(ccomp.src, trim.Mask)
        ccomp.src = ccomp.src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(ocvb)
    End Sub
End Class
