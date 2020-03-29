﻿Imports cv = OpenCvSharp
Public Class Object_Basics : Implements IDisposable
    Dim trim As Depth_InRange
    Dim ccomp As CComp_EdgeMask
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        trim = New Depth_InRange(ocvb)
        trim.externalUse = True

        ccomp = New CComp_EdgeMask(ocvb)
        ccomp.externalUse = True

        ocvb.desc = "Identify objects in the foreground."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        If externalUse = False Then
            ocvb.result1 = trim.Mask
            ocvb.result2 = trim.zeroMask
        End If

        ocvb.color.CopyTo(ccomp.srcGray, trim.Mask)
        ccomp.srcGray = ccomp.srcGray.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        trim.Dispose()
        ccomp.Dispose()
    End Sub
End Class