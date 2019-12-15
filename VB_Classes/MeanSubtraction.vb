Imports cv = OpenCvSharp
Public Class MeanSubtraction_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Scaling Factor = mean/scaling factor", 1, 20, 2)
        sliders.Show()
        ocvb.desc = "Subtract the mean from the image with a scaling factor"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mean = cv.Cv2.Mean(ocvb.color)
        cv.Cv2.Subtract(mean, ocvb.color, ocvb.result1)
        Dim scalingFactor = sliders.TrackBar1.Value ' this is optional...
        ocvb.result1 *= scalingFactor
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class