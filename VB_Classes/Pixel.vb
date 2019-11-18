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
        Dim watch = Stopwatch.StartNew()
        For y = 0 To ocvb.color.Height - 1
            For x = 0 To ocvb.color.Width - 1
                Dim color = ocvb.color.Get(Of cv.Vec3b)(y, x)
                Swap(color.Item0, color.Item2)
                ocvb.result1.Set(Of cv.Vec3b)(y, x, color)
            Next
        Next
        watch.Stop()
        Console.WriteLine("GetSet took " + CStr(watch.ElapsedMilliseconds) + "ms")

        ocvb.result2 = ocvb.color.Clone()
        watch = Stopwatch.StartNew()
        Dim indexer = ocvb.result2.GetGenericIndexer(Of cv.Vec3b)
        For y = 0 To ocvb.result2.Height - 1
            For x = 0 To ocvb.result2.Width - 1
                Dim color = indexer(y, x)
                Swap(color.Item0, color.Item2)
                indexer(y, x) = color
            Next
        Next
        watch.Stop()
        Console.WriteLine("Indexer took " + CStr(watch.ElapsedMilliseconds) + "ms")

        'Dim mat = New cv.Mat(Of Byte)(ocvb.color.Data)
        'indexer = mat.GetIndexer()

    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class