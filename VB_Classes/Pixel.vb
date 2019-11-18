Imports cv = OpenCvSharp
' https://github.com/shimat/opencvsharp_samples/blob/cba08badef1d5ab3c81ab158a64828a918c73df5/SamplesCS/Samples/PixelAccess.cs
Public Class Pixel_GetSet : Implements IDisposable
    Private Sub Swap(Of T)(ByRef a As T, ByRef b As T)
        Dim temp = b
        b = a
        a = temp
    End Sub
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Perform Pixel-level operations in 3 different ways to measure efficiency."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        For y = 0 To ocvb.color.Height - 1
            For x = 0 To ocvb.color.Width - 1
                Dim color = ocvb.color.Get(Of cv.Vec3b)(y, x)
                Swap(color.Item0, color.Item2)
                ocvb.result1.Set(Of cv.Vec3b)(y, x, color)
            Next
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class