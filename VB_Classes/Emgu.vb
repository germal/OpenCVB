Imports cv = OpenCvSharp

Public Class Emgu_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Test a sample EMGU usage."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim img = Emgu_Classes.DrawSubdivision.Draw(ocvb.color.Rows, ocvb.color.Cols, 20)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class