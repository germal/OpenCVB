Imports cv = OpenCvSharp
Public Class imShow_Basics
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        cv.Cv2.ImShow("color", ocvb.color)
    End Sub
    Public Sub VBdispose()
        cv.Cv2.DestroyAllWindows() ' not really needed
    End Sub
End Class
