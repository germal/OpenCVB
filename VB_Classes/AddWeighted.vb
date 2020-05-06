Imports cv = OpenCvSharp
Public Class AddWeighted_RGBDepth
    Inherits VB_Class

    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Weight", 0, 100, 50)
        ocvb.desc = "Add depth and rgb with specified weights."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim alpha = sliders.TrackBar1.Value / sliders.TrackBar1.Maximum
        cv.Cv2.AddWeighted(ocvb.color, alpha, ocvb.RGBDepth, 1.0 - alpha, 0, ocvb.result1)
        ocvb.label1 = "depth " + Format(1 - sliders.TrackBar1.Value / 100, "#0%") + " RGB " + Format(sliders.TrackBar1.Value / 100, "#0%")
    End Sub
End Class
