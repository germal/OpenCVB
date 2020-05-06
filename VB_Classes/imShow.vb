Imports cv = OpenCvSharp
Public Class imShow_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        ocvb.desc = "This is just a reminder that all HighGUI methods are available in OpenCVB"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        cv.Cv2.ImShow("color", ocvb.color)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        cv.Cv2.DestroyAllWindows() ' not really needed
    End Sub
End Class
