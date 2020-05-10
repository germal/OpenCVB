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
            ocvb.result1 = trim.Mask
            ocvb.result2 = trim.zeroMask
        End If

        ocvb.color.CopyTo(ccomp.srcGray, trim.Mask)
        ccomp.srcGray = ccomp.srcGray.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(ocvb)
    End Sub
End Class
