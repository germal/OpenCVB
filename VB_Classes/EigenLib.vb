Imports cv = OpenCvSharp
Public Class EigenLib_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "NewClass_Basics"
        ocvb.label2 = ""
        ocvb.desc = "New class description"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)

    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class