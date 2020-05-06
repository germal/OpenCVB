Imports cv = OpenCvSharp

Public Class Threshold_LaplacianFilter : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim edges As Filter_Laplacian
    Dim trim As Depth_InRange
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        trim = New Depth_InRange(ocvb, "Threshold_LaplacianFilter")
        edges = New Filter_Laplacian(ocvb, "Threshold_LaplacianFilter")
        sliders.setupTrackBar1(ocvb, "dist Threshold", 1, 100, 40)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.label1 = "Foreground Input"
        ocvb.desc = "Threshold the output of a Laplacian derivative, mask with depth foreground."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        edges.Run(ocvb)
        trim.Run(ocvb)

        Dim gray = ocvb.result1.CvtColor(cv.ColorConversionCodes.bgr2gray)
        Dim mask = ocvb.result1.CvtColor(cv.ColorConversionCodes.bgr2gray).Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        gray.SetTo(0, mask)

        Dim dist = gray.DistanceTransform(cv.DistanceTypes.L2, 3)
        Dim dist32f = dist.Normalize(0, 1, cv.NormTypes.MinMax)
        dist32f = dist.Threshold(sliders.TrackBar1.Value / 100, 1.0, cv.ThresholdTypes.Binary)

        dist32f.ConvertTo(gray, cv.MatType.CV_8UC1, 255)
        ocvb.result1 = gray.CvtColor(cv.ColorConversionCodes.gray2bgr)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        edges.Dispose()
        trim.Dispose()
    End Sub
End Class
