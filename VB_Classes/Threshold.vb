Imports cv = OpenCvSharp

Public Class Threshold_LaplacianFilter : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim edges As Filter_Laplacian
    Dim foreground As Depth_InRangeTrim
    Public Sub New(ocvb As AlgorithmData)
        foreground = New Depth_InRangeTrim(ocvb)
        edges = New Filter_Laplacian(ocvb)
        sliders.setupTrackBar1(ocvb, "dist Threshold", 1, 100, 40)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Threshold the output of a Laplacian derivative, mask with depth foreground."
        ocvb.label1 = "Foreground Input"
        ocvb.label2 = "Foreground Input"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        edges.Run(ocvb)
        foreground.Run(ocvb)

        Dim gray = ocvb.result1.CvtColor(cv.ColorConversionCodes.bgr2gray)
        Dim mask = ocvb.result1.CvtColor(cv.ColorConversionCodes.bgr2gray).Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        gray.SetTo(0, mask)

        gray.Threshold(40, 255, cv.ThresholdTypes.Binary Or cv.ThresholdTypes.Otsu)
        ocvb.result2 = gray.CvtColor(cv.ColorConversionCodes.gray2bgr)

        Dim dist = gray.DistanceTransform(cv.DistanceTypes.L2, 3)
        Dim dist32f = dist.Normalize(0, 1, cv.NormTypes.MinMax)
        dist32f = dist.Threshold(sliders.TrackBar1.Value / 100, 1.0, cv.ThresholdTypes.Binary)

        dist32f.ConvertTo(gray, cv.MatType.CV_8UC1, 255)
        ocvb.result1 = gray.CvtColor(cv.ColorConversionCodes.gray2bgr)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        edges.Dispose()
        foreground.Dispose()
    End Sub
End Class
