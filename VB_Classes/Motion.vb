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
        sliders.setupTrackBar(1, "Minimum size for motion rectangle", 1, 10000, 1000)
        sliders.setupTrackBar(2, "Milliseconds to detect no motion", 1, 1000, 100)
        ocvb.desc = "Detect contours in the motion data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        If src.Channels = 3 Then dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else dst1 = src

        Static delayCounter = 0
        Static firstFrame = dst1.Clone
        delayCounter += 1

        Static lastFrame = dst1
        Static persistSlider = findSlider("Frames to persist")
        If delayCounter > persistSlider.value Then
            delayCounter = 0
            lastFrame = dst1.Clone
        End If

        'blur.src = dst1
        'blur.Run(ocvb)

        diff.src = dst1
        diff.Run(ocvb)

        'dilate.src = diff.dst1
        'dilate.Run(ocvb)

        Static minSlider = findSlider("Minimum size for motion rectangle")
        contours.minArea = minSlider.value
        contours.src = diff.dst1
        contours.Run(ocvb)

        Static pixelSlider = findSlider("Change threshold in pixels")
        dst1 = diff.dst2.Threshold(pixelSlider.value, 255, cv.ThresholdTypes.Binary)

        dst2 = If(dst1.Channels = 1, dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR), dst1.Clone)
        For Each c In contours.contours
            Dim r = cv.Cv2.BoundingRect(c)
            If r.X >= 0 And r.Y >= 0 And r.X + r.Width < dst1.Width And r.Y + r.Height < dst1.Height Then
                Dim count = dst1(r).CountNonZero()
                If count > 100 Then dst2.Rectangle(r, cv.Scalar.Yellow, 2)
            End If
        Next
    End Sub
End Class