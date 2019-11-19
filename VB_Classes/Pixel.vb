Imports cv = OpenCvSharp
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
' https://github.com/shimat/opencvsharp_samples/blob/cba08badef1d5ab3c81ab158a64828a918c73df5/SamplesCS/Samples/PixelAccess.cs
Public Class Pixel_GetSet : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "Get/Set method (slowest)"
        ocvb.label2 = "Generic Indexer (not fastest)"
        ocvb.desc = "Perform Pixel-level operations in 3 different ways to measure efficiency."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rows = ocvb.color.Height
        Dim cols = ocvb.color.Width
        Dim watch = Stopwatch.StartNew()
        For y = 0 To rows - 1
            For x = 0 To cols - 1
                Dim color = ocvb.color.Get(Of cv.Vec3b)(y, x)
                color.Item0.SwapWith(color.Item2)
                ocvb.result1.Set(Of cv.Vec3b)(y, x, color)
            Next
        Next
        watch.Stop()
        Console.WriteLine("GetSet took " + CStr(watch.ElapsedMilliseconds) + "ms")

        ocvb.result2 = ocvb.color.Clone()
        watch = Stopwatch.StartNew()
        Dim indexer = ocvb.result2.GetGenericIndexer(Of cv.Vec3b)
        For y = 0 To rows - 1
            For x = 0 To cols - 1
                Dim color = indexer(y, x)
                color.Item0.SwapWith(color.Item2)
                indexer(y, x) = color
            Next
        Next
        watch.Stop()
        Console.WriteLine("Generic Indexer took " + CStr(watch.ElapsedMilliseconds) + "ms")

        watch = Stopwatch.StartNew()
        Dim colorArray(cols * rows * 3) As Byte
        Marshal.Copy(ocvb.color.Data, colorArray, 0, colorArray.Length)
        For i = 0 To colorArray.Length - 3 Step 3
            colorArray(i).SwapWith(colorArray(i + 2))
        Next
        Dim FastestMat As New cv.Mat(rows, cols, cv.MatType.CV_8UC3, colorArray)
        cv.Cv2.ImShow("FastestMat", FastestMat)
        watch.Stop()
        Console.WriteLine("Marshal Copy took " + CStr(watch.ElapsedMilliseconds) + "ms")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class