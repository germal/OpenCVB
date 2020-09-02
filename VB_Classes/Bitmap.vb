Imports System.Drawing
Imports cv = OpenCvSharp
' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OpenCvSharp.Mat)/
Public Class Bitmap_ToMat
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        label1 = "Convert color bitmap to Mat"
        label2 = "Convert Mat to bitmap and then back to Mat"
        desc = "Convert a color and grayscale bitmap to a cv.Mat"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim bitmap = New Bitmap(ocvb.parms.HomeDir + "Data/lena.jpg")
        dst1 = cv.Extensions.BitmapConverter.ToMat(bitmap).Resize(src.Size)

        bitmap = cv.Extensions.BitmapConverter.ToBitmap(src)
        dst2 = cv.Extensions.BitmapConverter.ToMat(bitmap)
    End Sub
End Class
