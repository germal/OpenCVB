Imports cv = OpenCvSharp
' https://github.com/davemk99/Cartoonify-Image/blob/master/main.cpp
Public Class CartoonifyImage_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Cartoon Median Blur kernel", 1, 21, 7)
        sliders.setupTrackBar2("Cartoon Median Blur kernel 2", 1, 21, 3)
        sliders.setupTrackBar3("Cartoon threshold", 1, 255, 80)
        sliders.setupTrackBar4("Cartoon Laplacian kernel", 1, 21, 5)
        label1 = "Mask for Cartoon"
        label2 = "Cartoonify Result"
        ocvb.desc = "Create a cartoon from a color image - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim medianBlur = If(sliders.TrackBar1.Value Mod 2, sliders.TrackBar1.Value, sliders.TrackBar1.Value + 1)
        Dim medianBlur2 = If(sliders.TrackBar2.Value Mod 2, sliders.TrackBar2.Value, sliders.TrackBar2.Value + 1)
        Dim kernelSize = If(sliders.TrackBar4.Value Mod 2, sliders.TrackBar4.Value, sliders.TrackBar4.Value + 1)
        Dim gray8u = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        gray8u = gray8u.MedianBlur(medianBlur)
        Dim edges = gray8u.Laplacian(cv.MatType.CV_8U, kernelSize)
        Dim mask = edges.Threshold(sliders.TrackBar3.Value, 255, cv.ThresholdTypes.Binary)
        dst1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = src.MedianBlur(medianBlur2).MedianBlur(medianBlur2)
        src.CopyTo(dst2, mask)
    End Sub
End Class

