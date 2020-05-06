Imports cv = OpenCvSharp
Public Class Bitwise_Not
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.label1 = "Color BitwiseNot"
        ocvb.label2 = "Gray BitwiseNot"
        ocvb.desc = "Gray and color bitwise_not"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        cv.Cv2.BitwiseNot(ocvb.color, ocvb.result1)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.BitwiseNot(gray, ocvb.result2)
    End Sub
    Public Sub MyDispose()
    End Sub
End Class