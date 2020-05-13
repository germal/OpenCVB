Imports cv = OpenCvSharp

Public Class Threshold_LaplacianFilter
    Inherits ocvbClass
    Dim edges As Filter_Laplacian
    Dim trim As Depth_InRange
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        trim = New Depth_InRange(ocvb, caller)
        edges = New Filter_Laplacian(ocvb, caller)
        sliders.setupTrackBar1(ocvb, caller, "dist Threshold", 1, 100, 40)
        label1 = "Foreground Input"
        ocvb.desc = "Threshold the output of a Laplacian derivative, mask with depth foreground."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        edges.Run(ocvb)
        trim.Run(ocvb)

        Dim gray = dst1.CvtColor(cv.ColorConversionCodes.bgr2gray)
        Dim mask = dst1.CvtColor(cv.ColorConversionCodes.bgr2gray).Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        gray.SetTo(0, mask)

        Dim dist = gray.DistanceTransform(cv.DistanceTypes.L2, 3)
        Dim dist32f = dist.Normalize(0, 1, cv.NormTypes.MinMax)
        dist32f = dist.Threshold(sliders.TrackBar1.Value / 100, 1.0, cv.ThresholdTypes.Binary)

        dist32f.ConvertTo(gray, cv.MatType.CV_8UC1, 255)
        dst1 = gray.CvtColor(cv.ColorConversionCodes.gray2bgr)
    End Sub
End Class

