Imports cv = OpenCvSharp
Public Class Concat_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Concatenate 2 images - horizontally and vertically"
        ocvb.label1 = "Horizontal concatenation"
        ocvb.label2 = "Vertical concatenation"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim tmp As New cv.Mat
        cv.Cv2.HConcat(ocvb.color, ocvb.RGBDepth, tmp)
        ocvb.result1 = tmp.Resize(ocvb.color.Size())
        cv.Cv2.VConcat(ocvb.color, ocvb.RGBDepth, tmp)
        ocvb.result2 = tmp.Resize(ocvb.color.Size())
		MyBase.Finish(ocvb)
    End Sub
End Class




Public Class Concat_4way
    Inherits ocvbClass
    Public img(3) As cv.Mat
        Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Concatenate 4 images - horizontally and vertically"
        For i = 0 To img.Length - 1
            img(i) = New cv.Mat
        Next
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        if standalone Then
            img(0) = ocvb.color
            img(1) = ocvb.RGBDepth
            img(2) = ocvb.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR).Resize(ocvb.color.Size())
            img(3) = ocvb.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR).Resize(ocvb.color.Size())
        End If
        Dim tmp1 As New cv.Mat, tmp2 As New cv.Mat, tmp3 As New cv.Mat
        cv.Cv2.HConcat(img(0), img(1), tmp1)
        cv.Cv2.HConcat(img(2), img(3), tmp2)
        cv.Cv2.VConcat(tmp1, tmp2, tmp3)
        ocvb.result1 = tmp3.Resize(ocvb.color.Size())
		MyBase.Finish(ocvb)
    End Sub
End Class
