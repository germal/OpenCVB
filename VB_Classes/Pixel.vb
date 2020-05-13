Imports cv = OpenCvSharp
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
' https://github.com/shimat/opencvsharp_samples/blob/cba08badef1d5ab3c81ab158a64828a918c73df5/SamplesCS/Samples/PixelAccess.cs
Public Class Pixel_GetSet
    Inherits ocvbClass
    Dim mat4 As Mat_4to1
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        mat4 = New Mat_4to1(ocvb, caller)

        label1 = "Log of times for each method"
        label2 = "GetSet/Generic Indexer/Marshal.Copy"
        ocvb.desc = "Perform Pixel-level operations in 3 different ways to measure efficiency."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rows = ocvb.color.Height
        Dim cols = ocvb.color.Width
        Dim output As String = ""
        Dim rgb = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2RGB)

        Dim watch = Stopwatch.StartNew()
        For y = 0 To rows - 1
            For x = 0 To cols - 1
                Dim color = rgb.Get(Of cv.Vec3b)(y, x)
                color.Item0.SwapWith(color.Item2)
                mat4.mat(0).Set(Of cv.Vec3b)(y, x, color)
            Next
        Next
        watch.Stop()
        output += "Upper left image is GetSet and it took " + CStr(watch.ElapsedMilliseconds) + "ms" + vbCrLf + vbCrLf

        mat4.mat(1) = rgb.Clone()
        watch = Stopwatch.StartNew()
        Dim indexer = mat4.mat(1).GetGenericIndexer(Of cv.Vec3b)
        For y = 0 To rows - 1
            For x = 0 To cols - 1
                Dim color = indexer(y, x)
                color.Item0.SwapWith(color.Item2)
                indexer(y, x) = color
            Next
        Next
        watch.Stop()
        output += "Upper right image is Generic Indexer and it took " + CStr(watch.ElapsedMilliseconds) + "ms" + vbCrLf + vbCrLf

        watch = Stopwatch.StartNew()
        Dim colorArray(cols * rows * rgb.ElemSize - 1) As Byte
        Marshal.Copy(rgb.Data, colorArray, 0, colorArray.Length)
        For i = 0 To colorArray.Length - 3 Step 3
            colorArray(i).SwapWith(colorArray(i + 2))
        Next
        mat4.mat(2) = New cv.Mat(rows, cols, cv.MatType.CV_8UC3, colorArray)
        watch.Stop()
        output += "Marshal Copy took " + CStr(watch.ElapsedMilliseconds) + "ms" + vbCrLf

        ocvb.putText(New ActiveClass.TrueType(output, 10, 60, RESULT1))

        mat4.Run(ocvb)
    End Sub
End Class
