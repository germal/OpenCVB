Imports cv = OpenCvSharp
Public Class VBTest_Interface : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.parms.VBTestInterface.Run(ocvb) ' OpenCVB.vb has already run the constructor of the VBTest_Basics class.
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class