Imports cv = OpenCvSharp
'https://www.pyimagesearch.com/2017/11/06/deep-learning-opencvs-blobfromimage-works/
Public Class MeanSubtraction_Basics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Scaling Factor = mean/scaling factor X100", 1, 500, 100)
        ocvb.desc = "Subtract the mean from the image with a scaling factor"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim mean = cv.Cv2.Mean(src)
        cv.Cv2.Subtract(mean, src, dst1)
        Dim scalingFactor = sliders.trackbar(0).Value / 100
        dst1 *= 1 / scalingFactor
    End Sub
End Class

