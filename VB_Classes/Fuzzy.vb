Imports cv = OpenCvSharp
Public Class Fuzzy_Basics
    Inherits VBparent
    Dim reduction As Reduction_KNN_Color
    Dim hist As Histogram_KalmanSmoothed
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        reduction = New Reduction_KNN_Color(ocvb)
        hist = New Histogram_KalmanSmoothed(ocvb)
        ocvb.desc = "That which is not solid is fuzzy."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)
        dst1 = reduction.dst1

        hist.src = dst1
        hist.Run(ocvb)
        dst2 = hist.dst1
        Dim count = hist.histogram.CountNonZero()
        label2 = CStr(count) + " non-zero buckets in histogram"
        'dst2 = dst1.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
    End Sub
End Class