Imports cv = OpenCvSharp
Public Class VBTest_Interface
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.parms.VBTestInterface.Run(ocvb) ' OpenCVB.vb has already run the constructor of the VBTest_Basics class.
    End Sub
    Public Sub VBdispose()
    End Sub
End Class