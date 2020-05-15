Imports cv = OpenCvSharp
Public Class Covariance_Basics
    Inherits ocvbClass
    Dim random As Random_Points
    Public samples As cv.Mat
    Public result1or2 As Integer = RESULT2
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        random = New Random_Points(ocvb, caller)
        ocvb.desc = "Calculate the covariance of random depth data points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim covariance As New cv.Mat, mean = New cv.Mat
        dst1.SetTo(0)
        If standalone Then
            random.Run(ocvb)
            samples = New cv.Mat(random.Points.Length, 2, cv.MatType.CV_32F, random.Points2f)
            For i = 0 To random.Points.Length - 1
                dst1.Circle(random.Points(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
            Next
        End If
        Dim samples2 = samples.Reshape(2)
        cv.Cv2.CalcCovarMatrix(samples, covariance, mean, cv.CovarFlags.Cols)
        Dim overallMean = samples2.Mean()
        ocvb.putText(New ActiveClass.TrueType("Covar(0, 0), Covar(0, 1)" + vbTab + Format(covariance.Get(Of Double)(0, 0), "#0.0") + vbTab +
                     Format(covariance.Get(Of Double)(0, 1), "#0.0"), 20, 60, result1or2))
        ocvb.putText(New ActiveClass.TrueType("Covar(1 0), Covar(1, 1)" + vbTab + Format(covariance.Get(Of Double)(1, 0), "#0.0") + vbTab +
                     Format(covariance.Get(Of Double)(1, 1), "#0.0"), 20, 90, result1or2))
        ocvb.putText(New ActiveClass.TrueType("Mean X, Mean Y" + vbTab + vbTab + Format(overallMean(0), "#0.00") + vbTab + vbTab +
                     Format(overallMean(1), "#0.00"), 20, 120, result1or2))
        If standalone Then
            Dim newCenter = New cv.Point(overallMean(0), overallMean(1))
            Static lastCenter = newCenter
            dst1.Circle(newCenter, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            dst1.Circle(lastCenter, 5, cv.Scalar.Yellow, 2, cv.LineTypes.AntiAlias)
            dst1.Line(newCenter, lastCenter, cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
            lastCenter = newCenter
            ocvb.putText(New ActiveClass.TrueType("Yellow is last center, red is the current center", 20, 150, result1or2))
        End If
    End Sub
End Class



' http://answers.opencv.org/question/31228/how-to-use-function-calccovarmatrix/
Public Class Covariance_Test
    Inherits ocvbClass
    Dim covar As Covariance_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)

        covar = New Covariance_Basics(ocvb, caller)
        ocvb.desc = "Test the covariance basics algorithm."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim testInput() As Double = {1.5, 2.3, 3.0, 1.7, 1.2, 2.9, 2.1, 2.2, 3.1, 3.1, 1.3, 2.7, 2.0, 1.7, 1.0, 2.0, 0.5, 0.6, 1.0, 0.9}
        covar.samples = New cv.Mat(10, 2, cv.MatType.CV_64F, testInput)
        covar.result1or2 = RESULT1
        covar.Run(ocvb)
        ocvb.putText(New ActiveClass.TrueType("Results should be a symmetric array with 2.1 and -2.1", 20, 150, RESULT1))
    End Sub
End Class

