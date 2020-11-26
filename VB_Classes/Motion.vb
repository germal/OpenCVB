'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Imports cv = OpenCvSharp
Public Class Motion_Basics
    Inherits VBparent
    Dim blur As Blur_Basics
    Dim diff As Diff_Basics
    Dim dilate As DilateErode_Basics
    Dim contours As Contours_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        contours = New Contours_Basics(ocvb)
        dilate = New DilateErode_Basics(ocvb)
        blur = New Blur_Basics(ocvb)
        diff = New Diff_Basics(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Frames to persist", 1, 100, 10)
        sliders.setupTrackBar(1, "Minimum size motion rectangle", 1, 10000, 1000)
        sliders.setupTrackBar(2, "Milliseconds to detect no motion", 1, 1000, 100)
        ocvb.desc = "Detect contours in the motion data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        blur.src = dst1
        blur.Run(ocvb)

        Static delayCounter = 0
        Static firstFrame = dst1.Clone
        delayCounter += 1

        Static lastFrame = dst1
        Static persistSlider = findSlider("Frames to persist")
        If delayCounter > persistSlider.value Then
            delayCounter = 0
            lastFrame = dst1.Clone
        End If
        diff.src = blur.dst1
        diff.Run(ocvb)

        dilate.src = diff.dst1
        dilate.Run(ocvb)

        contours.src = dilate.dst1
        contours.Run(ocvb)

        Static pixelSlider = findSlider("Change threshold in pixels")
        dst1 = dilate.dst1.Threshold(pixelSlider.value, 255, cv.ThresholdTypes.Binary)

    End Sub
End Class