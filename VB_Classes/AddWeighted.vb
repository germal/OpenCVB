Imports cv = OpenCvSharp
Public Class AddWeighted_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Weight", 0, 100, 50)
        ocvb.desc = "Add depth and rgb with specified weights."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim alpha = sliders.TrackBar1.Value / sliders.TrackBar1.Maximum
        cv.Cv2.AddWeighted(ocvb.color, alpha, ocvb.RGBDepth, 1.0 - alpha, 0, dst)
        ocvb.label1 = "depth " + Format(1 - sliders.TrackBar1.Value / 100, "#0%") + " RGB " + Format(sliders.TrackBar1.Value / 100, "#0%")
		MyBase.Finish(ocvb)
    End Sub
End Class





Public Class AddWeighted_Test
    Inherits ocvbClass
    Dim weight As AddWeighted_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(caller)
        weight = New AddWeighted_Basics(ocvb, caller)
        ocvb.desc = "Testing AddWeighted_Basics as a derivative algorithm."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        weight.Run(ocvb)
        dst = weight.dst
        MyBase.Finish(ocvb)
    End Sub
    Public Sub MyDispose()
        weight.Dispose()
    End Sub
End Class
