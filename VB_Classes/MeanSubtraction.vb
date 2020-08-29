Imports cv = OpenCvSharp
'https://www.pyimagesearch.com/2017/11/06/deep-learning-opencvs-blobfromimage-works/
Public Class MeanSubtraction_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Scaling Factor = mean/scaling factor X100", 1, 500, 100)
        setDescription(ocvb, "Subtract the mean from the image with a scaling factor")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mean = cv.Cv2.Mean(ocvb.color)
        cv.Cv2.Subtract(mean, ocvb.color, dst1)
        Dim scalingFactor = sliders.trackbar(0).Value / 100
        dst1 *= 1 / scalingFactor
    End Sub
End Class
