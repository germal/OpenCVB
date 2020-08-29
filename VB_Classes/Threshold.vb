Imports cv = OpenCvSharp
Public Class Threshold_LaplacianFilter
    Inherits ocvbClass
    Dim edges As Filter_Laplacian
    Dim trim As Depth_InRange
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        trim = New Depth_InRange(ocvb)
        edges = New Filter_Laplacian(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "dist Threshold", 1, 100, 40)
        label1 = "Foreground Input"
        setDescription(ocvb, "Threshold the output of a Laplacian derivative, mask with depth foreground.  needs more work")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        edges.src = src
        edges.Run(ocvb)
        dst2 = edges.dst2
        trim.src = getDepth32f(ocvb)
        trim.Run(ocvb)
        dst1 = trim.dst1

        Dim mask = dst1.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)
        dst2.SetTo(0, mask)
    End Sub
End Class

