Imports cv = OpenCvSharp
Public Class Object_Basics : Implements IDisposable
    Dim fore As Depth_InRangeTrim
    Dim ccomp As CComp_EdgeMask
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        fore = New Depth_InRangeTrim(ocvb)
        fore.externalUse = True

        ccomp = New CComp_EdgeMask(ocvb)
        ccomp.externalUse = True

        ocvb.desc = "Identify objects in the foreground."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        fore.Run(ocvb)
        If externalUse = False Then
            ocvb.result1 = fore.Mask
            ocvb.result2 = fore.zeroMask
        End If

        ocvb.color.CopyTo(ccomp.srcGray, fore.Mask)
        ccomp.srcGray = ccomp.srcGray.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(ocvb)

    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        fore.Dispose()
        ccomp.Dispose()
    End Sub
End Class