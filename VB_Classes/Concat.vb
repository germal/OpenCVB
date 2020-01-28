Imports cv = OpenCvSharp
Public Class Concat_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Concatenate 2 images - horizontally and vertically"
        ocvb.label1 = "Horizontal concatenation"
        ocvb.label2 = "Vertical concatenation"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim tmp As New cv.Mat
        cv.Cv2.HConcat(ocvb.color, ocvb.depthRGB, tmp)
        ocvb.result1 = tmp.Resize(ocvb.color.Size())
        cv.Cv2.VConcat(ocvb.color, ocvb.depthRGB, tmp)
        ocvb.result2 = tmp.Resize(ocvb.color.Size())
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class Concat_4way : Implements IDisposable
    Public img(3) As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Concatenate 4 images - horizontally and vertically"
        For i = 0 To img.Length - 1
            img(i) = New cv.Mat
        Next
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            img(0) = ocvb.color
            img(1) = ocvb.depthRGB
            img(2) = ocvb.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            img(3) = ocvb.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If
        Dim tmp1 As New cv.Mat, tmp2 As New cv.Mat, tmp3 As New cv.Mat
        cv.Cv2.HConcat(img(0), img(1), tmp1)
        cv.Cv2.HConcat(img(2), img(3), tmp2)
        cv.Cv2.VConcat(tmp1, tmp2, tmp3)
        ocvb.result1 = tmp3.Resize(ocvb.color.Size())
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class