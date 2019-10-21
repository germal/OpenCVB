Imports cv = OpenCvSharp
' https://github.com/davemk99/Cartoonify-Image/blob/master/main.cpp
Public Class CartoonifyImage_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Cartoon Median Blur kernel", 1, 21, 7)
        sliders.setupTrackBar2(ocvb, "Cartoon Median Blur kernel 2", 1, 21, 3)
        sliders.setupTrackBar3(ocvb, "Cartoon threshold", 1, 255, 80)
        sliders.setupTrackBar4(ocvb, "Cartoon Laplacian kernel", 1, 21, 5)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.label1 = "Mask for Cartoon"
        ocvb.label2 = "Cartoonify Result"
        ocvb.desc = "Create a cartoon from a color image - Painterly effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim medianBlur = sliders.TrackBar1.Value
        If medianBlur Mod 2 = 0 Then medianBlur += 1
        Dim medianBlur2 = sliders.TrackBar2.Value
        If medianBlur2 Mod 2 = 0 Then medianBlur2 += 1
        Dim kernelSize = sliders.TrackBar4.Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1
        Dim gray8u = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        gray8u = gray8u.MedianBlur(medianBlur)
        Dim edges = gray8u.Laplacian(cv.MatType.CV_8U, kernelSize)
        Dim mask = edges.Threshold(sliders.TrackBar3.Value, 255, cv.ThresholdTypes.Binary)
        ocvb.result1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.result2 = ocvb.color.MedianBlur(medianBlur2)
        ocvb.result2 = ocvb.result2.MedianBlur(medianBlur2)
        ocvb.color.CopyTo(ocvb.result2, mask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class
