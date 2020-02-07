Imports cv = OpenCvSharp
Public Class AddWeighted_RGBDepth : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Weight", 0, 100, 50)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Add depth and rgb with specified weights."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim alpha = sliders.TrackBar1.Value / sliders.TrackBar1.Maximum
        cv.Cv2.AddWeighted(ocvb.color, alpha, ocvb.RGBDepth, 1.0 - alpha, 0, ocvb.result1)
        ocvb.label1 = "depth " + Format(1 - sliders.TrackBar1.Value / 100, "#0%") + " RGB " + Format(sliders.TrackBar1.Value / 100, "#0%")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class
