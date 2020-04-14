Imports cv = OpenCvSharp
Public Class VBTest_Interface : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.parms.VBTestInterface.Run(ocvb) ' OpenCVB.vb has already run the constructor of the VBTest_Basics class.
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class