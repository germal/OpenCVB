Imports cv = OpenCvSharp
' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class Pencil_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Pencil Sigma_s", 0, 200, 60)
        sliders.setupTrackBar2(ocvb, "Pencil Sigma_r", 1, 100, 7)
        sliders.setupTrackBar3(ocvb, "Pencil Shade Factor", 1, 200, 40)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Convert image to a pencil sketch - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim sigma_s = sliders.TrackBar1.Value
        Dim sigma_r = sliders.TrackBar2.Value / sliders.TrackBar2.Maximum
        Dim shadowFactor = sliders.TrackBar3.Value / 1000
        cv.Cv2.PencilSketch(ocvb.color, ocvb.result2, ocvb.result1, sigma_s, sigma_r, shadowFactor)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class
