Imports cv = OpenCvSharp

Public Class Threshold_LaplacianFilter
    Inherits ocvbClass
    Dim edges As Filter_Laplacian
    Dim trim As Depth_InRange
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        trim = New Depth_InRange(ocvb)
        edges = New Filter_Laplacian(ocvb)
        sliders.setupTrackBar1(ocvb, "dist Threshold", 1, 100, 40)
        label1 = "Foreground Input"
        ocvb.desc = "Threshold the output of a Laplacian derivative, mask with depth foreground."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        edges.src = src
        edges.Run(ocvb)
        dst2 = edges.dst2
        trim.src = src
        trim.Run(ocvb)
        Dim gray = trim.dst1

        Dim mask = gray.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        gray.SetTo(0, mask)

        Dim dist = gray.DistanceTransform(cv.DistanceTypes.L2, 3)
        Dim dist32f = dist.Normalize(0, 1, cv.NormTypes.MinMax)
        dist32f = dist.Threshold(sliders.TrackBar1.Value / 100, 1.0, cv.ThresholdTypes.Binary)

        dist32f.ConvertTo(dst1, cv.MatType.CV_8UC1, 255)
    End Sub
End Class

