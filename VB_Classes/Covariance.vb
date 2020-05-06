Imports cv = OpenCvSharp
Public Class Covariance_Basics
    Inherits VB_Class
    Dim random As Random_Points
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        random = New Random_Points(ocvb, callerName)
        ocvb.desc = "Calculate the covariance of random depth data points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        random.Run(ocvb)
        Dim samples = New cv.Mat(random.Points.Length, 2, cv.MatType.CV_32F, random.Points2f)
        Dim covar As New cv.Mat
        Dim mean = New cv.Mat
        cv.Cv2.CalcCovarMatrix(samples, covar, mean, cv.CovarFlags.Cols)
        Dim overallMean = mean.Mean()
        If ocvb.frameCount Mod 100 = 0 Then
            ocvb.label1 = "covar(0) = " + Format(covar.Get(Of Double)(0), "#0.0") + " mean = " + Format(overallMean(0), "#0.00")
        End If
    End Sub
    Public Sub MyDispose()
        random.Dispose()
    End Sub
End Class



' http://answers.opencv.org/question/31228/how-to-use-function-calccovarmatrix/
Public Class Covariance_Test
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.desc = "Calculate the covariance of random depth data points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim testInput() As Double = {1.5, 2.3, 3.0, 1.7, 1.2, 2.9, 2.1, 2.2, 3.1, 3.1, 1.3, 2.7, 2.0, 1.7, 1.0, 2.0, 0.5, 0.6, 1.0, 0.9}
        Dim samples = New cv.Mat(10, 2, cv.MatType.CV_64F, testInput)
        Dim covar As New cv.Mat
        Dim mean = New cv.Mat
        cv.Cv2.CalcCovarMatrix(samples, covar, mean, cv.CovarFlags.Cols)
        Dim overallMean = mean.Mean()
        ocvb.label1 = "covar(0) = " + Format(covar.Get(of Double)(0), "#0.0") + " mean(overall) = " + Format(overallMean(0), "#0.00")
    End Sub
    Public Sub MyDispose()
    End Sub
End Class
