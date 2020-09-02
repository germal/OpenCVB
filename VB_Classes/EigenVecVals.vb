Imports cv = OpenCvSharp
' https://bytefish.de/blog/eigenvalues_in_opencv/
Public Class EigenVecVals_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        desc = "Solve system of equations using OpenCV's EigenVV"
        label1 = "EigenVec (solution)"
        label2 = "Relationship between Eigen Vec and Vals"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim a() As Double = {1.96, -6.49, -0.47, -7.2, -0.65,
                             -6.49, 3.8, -6.39, 1.5, -6.34,
                             -0.47, -6.39, 4.17, -1.51, 2.67,
                             -7.2, 1.5, -1.51, 5.7, 1.8,
                             -0.65, -6.34, 2.67, 1.8, -7.1}
        Dim mat As New cv.Mat(5, 5, cv.MatType.CV_64FC1, a)
        Dim eigenVal As New cv.Mat, eigenVec As New cv.Mat
        cv.Cv2.Eigen(mat, eigenVal, eigenVec)
        Dim solution(mat.Cols) As Double

        ocvb.trueText(New TTtext("Eigen Vals" + vbTab + "Eigen Vectors" + vbTab + vbTab + vbTab + vbTab + vbTab + "Original Matrix" + vbCrLf, 10, 25))
        Dim nextLine As String = ""
        Dim scalar As cv.Scalar
        For i = 0 To eigenVal.Rows - 1
            scalar = eigenVal.Get(Of cv.Scalar)(0, i)
            solution(i) = scalar.Val0
            nextLine = Format(scalar.Val0, "#0.00") + vbTab + vbTab
            For j = 0 To eigenVec.Rows - 1
                scalar = eigenVec.Get(Of cv.Scalar)(i, j)
                nextLine += Format(scalar.Val0, "#0.00") + vbTab
            Next
            For j = 0 To eigenVec.Rows - 1
                nextLine += vbTab + Format(a(i * 5 + j), "#0.00")
            Next
            ocvb.trueText(New TTtext(nextLine, 10, 50 + i * 30))
        Next

        For i = 0 To eigenVec.Rows - 1
            nextLine = ""
            Dim plusSign = " + "
            For j = 0 To eigenVec.Cols - 1
                scalar = eigenVec.Get(Of cv.Scalar)(i, j)
                If j = eigenVec.Cols - 1 Then plusSign = vbTab
                nextLine += Format(scalar.Val0, "#0.00") + " * " + Format(solution(j), "#0.00") + plusSign
            Next
            nextLine += " = " + vbTab + "0.0"
            ocvb.trueText(New TTtext(nextLine, 10, 50 + (eigenVal.Rows + i) * 30))
        Next
    End Sub
End Class

