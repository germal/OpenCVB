Imports cv = OpenCvSharp
Imports CS_Classes
Module matrixInverse_Module
    Public Function printMatrixResults(src As cv.Mat, dst1 As cv.Mat) As String
        Dim outstr As String = "Original Matrix " + vbCrLf
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                outstr += Format(src.Get(of Double)(y, x), "#0.0000") + vbTab
            Next
            outstr += vbCrLf
        Next
        outstr += vbCrLf + "Matrix Inverse" + vbCrLf
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                outstr += Format(dst1.Get(of Double)(y, x), "#0.0000") + vbTab
            Next
            outstr += vbCrLf
        Next
        Return outstr
    End Function
End Module





' https://visualstudiomagazine.com/articles/2020/04/06/invert-matrix.aspx
Public Class MatrixInverse_Basics_CS
    Inherits ocvbClass
    Public matrix As New MatrixInverse
    Dim defaultInput(,) As Double = {{3, 7, 2, 5}, {4, 0, 1, 1}, {1, 6, 3, 0}, {2, 8, 4, 3}}
    Dim defaultBVector() As Double = {12, 7, 7, 13}
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        src = New cv.Mat(4, 4, cv.MatType.CV_64F, defaultInput)
        ocvb.desc = "Manually invert a matrix"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Width <> src.Height Then
            ocvb.putText(New ActiveClass.TrueType("The src matrix must be square!", 10, 60, RESULT1))
            Exit Sub
        End If

        if standalone Then matrix.bVector = defaultBVector
        dst1 = matrix.Run(src)

        Dim outstr = printMatrixResults(src, dst1)
        ocvb.putText(New ActiveClass.TrueType(outstr + vbCrLf + "Intermediate results are optionally available in the console log.", 10, 60, RESULT1))
    End Sub
End Class






Public Class MatrixInverse_OpenCV
    Inherits ocvbClass
    Dim defaultInput(,) As Double = {{3, 7, 2, 5}, {4, 0, 1, 1}, {1, 6, 3, 0}, {2, 8, 4, 3}}
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        src = New cv.Mat(4, 4, cv.MatType.CV_64F, defaultInput)
        ocvb.desc = "Use OpenCV to invert a matrix"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Width <> src.Height Then
            ocvb.putText(New ActiveClass.TrueType("The src matrix must be square!", 10, 60, RESULT1))
            Exit Sub
        End If

        dst1 = src.EmptyClone.SetTo(0)
        cv.Cv2.Invert(src, dst1, cv.DecompTypes.LU)
        Dim outstr = printMatrixResults(src, dst1)
        ocvb.putText(New ActiveClass.TrueType(outstr, 10, 60, RESULT1))
    End Sub
End Class
