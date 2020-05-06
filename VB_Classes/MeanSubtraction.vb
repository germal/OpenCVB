Imports cv = OpenCvSharp
'https://www.pyimagesearch.com/2017/11/06/deep-learning-opencvs-blobfromimage-works/
Public Class MeanSubtraction_Basics
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Scaling Factor = mean/scaling factor X100", 1, 500, 100)
        ocvb.desc = "Subtract the mean from the image with a scaling factor"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mean = cv.Cv2.Mean(ocvb.color)
        cv.Cv2.Subtract(mean, ocvb.color, ocvb.result1)
        Dim scalingFactor = sliders.TrackBar1.Value / 100
        ocvb.result1 *= 1 / scalingFactor
    End Sub
End Class