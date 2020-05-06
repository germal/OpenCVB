Imports System.Drawing
Imports cv = OpenCvSharp
' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OpenCvSharp.Mat)/
Public Class Bitmap_ToMat
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        ocvb.label1 = "Convert color bitmap to Mat"
        ocvb.label2 = "Convert Mat to bitmap and then back to Mat"
        ocvb.desc = "Convert a color and grayscale bitmap to a cv.Mat"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim bitmap = New Bitmap(ocvb.parms.HomeDir + "Data/lena.jpg")
        ocvb.result1 = cv.Extensions.BitmapConverter.ToMat(bitmap).Resize(ocvb.color.Size)

        bitmap = cv.Extensions.BitmapConverter.ToBitmap(ocvb.color)
        ocvb.result2 = cv.Extensions.BitmapConverter.ToMat(bitmap)
    End Sub
End Class