Imports cv = OpenCvSharp
Public Class imShow_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        cv.Cv2.ImShow("color", ocvb.color)
    End Sub
End Class

