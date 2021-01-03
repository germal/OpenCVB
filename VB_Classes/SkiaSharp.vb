Imports cv = OpenCvSharp
Imports sk = SkiaSharp
' https://docs.microsoft.com/en-us/samples/xamarin/xamarin-forms-samples/skiasharpforms-demos/
Public Class SkiaSharp_Hello
    Inherits VBparent
    Dim hello As New CS_Classes.SkiaSharp_Hello
    Public Sub New()
        initParent()
        task.desc = "Create a bitmap with SkiaSharp and display it"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        dst1 = hello.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
    End Sub
End Class
