Imports System.Drawing
Imports cv = OpenCvSharp
' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OpenCvSharp.Mat)/
'Public Class Bitmap_ToMat : Implements IDisposable
'    Public Sub New(ocvb As AlgorithmData)
'        ocvb.label1 = "Convert color bitmap to Mat"
'        ocvb.label2 = "Convert gray bitmap to Mat"
'        ocvb.desc = "Convert a color and grayscale bitmap to a cv.Mat"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        Dim bitmap = New Bitmap(ocvb.parms.HomeDir + "Data/space_shuttle.jpg")
'        ocvb.result1 = cv.Extensions.BitmapConverter.ToMat(bitmap)

'        Dim gray = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        bitmap = cv.Extensions.BitmapConverter.ToBitmap(gray)
'        ocvb.result2 = cv.Extensions.BitmapConverter.ToMat(bitmap)
'    End Sub
'    Public Sub Dispose() Implements IDisposable.Dispose
'    End Sub
'End Class