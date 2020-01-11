Imports cv = OpenCvSharp
' http://areshopencv.blogspot.com/2011/12/computing-entropy-of-image.html
Public Class Entropy_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Compute the entropy in an image - a measure of contrast(iness)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)

    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class