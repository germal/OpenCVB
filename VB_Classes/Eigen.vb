Imports cv = OpenCvSharp
' https://bytefish.de/blog/eigenvalues_in_opencv/
Public Class Eigen_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Solve system of equations using OpenCV's EigenVV"
        ocvb.label1 = "EigenVec (solution)"
        ocvb.label2 = "Relationship between Eigen Vec and Vals"
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

        ocvb.putText(New ActiveClass.TrueType("Eigen Vals", 10, 25))
        For i = 0 To eigenVal.Rows - 1
            Dim scalar = eigenVal.Get(Of cv.Scalar)(0, i)
            solution(i) = scalar.Val0
            Dim nextline = Format(scalar.Val0, "#0.0000") + vbTab
            ocvb.putText(New ActiveClass.TrueType(nextline, 10, 50 + i * 30))
        Next

        ocvb.putText(New ActiveClass.TrueType("Eigen Vecs", 80, 25))
        For i = 0 To eigenVec.Cols - 1
            For j = 0 To eigenVec.Rows - 1
                Dim scalar = eigenVec.Get(Of cv.Scalar)(i, j)
                Dim nextline = Format(scalar.Val0, "#0.0000") + vbTab
                ocvb.putText(New ActiveClass.TrueType(nextline, 80 + j * 40, 50 + i * 30))
            Next
        Next

        ocvb.putText(New ActiveClass.TrueType("Original Matrix", 300, 25))
        For i = 0 To eigenVec.Cols - 1
            For j = 0 To eigenVec.Rows - 1
                Dim nextline = Format(a(i * 5 + j), "#0.0000") + vbTab
                ocvb.putText(New ActiveClass.TrueType(nextline, 300 + j * 40, 50 + i * 30))
            Next
        Next

        For i = 0 To eigenVec.Rows - 1
            Dim nextLine As String = ""
            Dim computed As Double
            Dim plusSign = " + "
            For j = 0 To eigenVec.Cols - 1
                Dim scalar = eigenVec.Get(Of cv.Scalar)(i, j)
                If j = eigenVec.Cols - 1 Then plusSign = vbTab
                nextLine += Format(scalar.Val0, "#0.00") + "*" + Format(solution(j), "#0.00") + plusSign
            Next
            nextLine += vbTab + " = " + vbTab + Format(computed, "#0.00000000")
            ocvb.putText(New ActiveClass.TrueType(nextLine, 10, 50 + i * 30, RESULT2))
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
