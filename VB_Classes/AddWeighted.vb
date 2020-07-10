Imports cv = OpenCvSharp
Public Class AddWeighted_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "Weight", 0, 100, 50)
        ocvb.desc = "Add depth and rgb with specified weights."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim alpha = sliders.sliders(0).Value / sliders.sliders(0).Maximum
        cv.Cv2.AddWeighted(src, alpha, ocvb.RGBDepth, 1.0 - alpha, 0, dst1)
        label1 = "depth " + Format(1 - sliders.sliders(0).Value / 100, "#0%") + " RGB " + Format(sliders.sliders(0).Value / 100, "#0%")
    End Sub
End Class

