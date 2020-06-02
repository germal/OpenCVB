Imports cv = OpenCvSharp
Public Class AddWeighted_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Weight", 0, 100, 50)
        ocvb.desc = "Add depth and rgb with specified weights."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim alpha = sliders.TrackBar1.Value / sliders.TrackBar1.Maximum
        cv.Cv2.AddWeighted(src, alpha, ocvb.RGBDepth, 1.0 - alpha, 0, dst1)
        label1 = "depth " + Format(1 - sliders.TrackBar1.Value / 100, "#0%") + " RGB " + Format(sliders.TrackBar1.Value / 100, "#0%")
    End Sub
End Class





Public Class AddWeighted_Test
    Inherits ocvbClass
    Dim weight As AddWeighted_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        weight = New AddWeighted_Basics(ocvb)
        ocvb.desc = "Testing AddWeighted_Basics as a derivative algorithm."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        weight.src = src
        weight.Run(ocvb)
        dst1 = weight.dst1
    End Sub
End Class
