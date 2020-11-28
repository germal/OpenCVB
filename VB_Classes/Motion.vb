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
        diff = New Diff_Basics(ocvb)
        blur = New Blur_Basics(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Frames to persist", 1, 100, 10)
        sliders.setupTrackBar(1, "Minimum size for motion rectangle", 1, 10000, 1000)
        sliders.setupTrackBar(2, "Milliseconds to detect no motion", 1, 1000, 100)

        Dim iterSlider = findSlider("Dilate/Erode Kernel Size")
        iterSlider.Value = 2
        ocvb.desc = "Detect contours in the motion data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static rectList As New List(Of cv.Rect)

        If src.Channels = 3 Then dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else dst1 = src
        blur.src = dst1
        blur.Run(ocvb)
        dst1 = blur.dst1

        Static delayCounter = 0
        Static firstFrame = dst1.Clone
        delayCounter += 1

        Static persistSlider = findSlider("Frames to persist")
        If delayCounter > persistSlider.value Then
            delayCounter = 0
            firstFrame = dst1.Clone
            rectList.Clear()
        End If

        Static threshSlider = findSlider("Change threshold for each pixel")
        If ocvb.frameCount = 0 Then threshSlider.value = 25
        diff.src = dst1
        diff.lastFrame = firstFrame
        diff.Run(ocvb)
        dst2 = diff.dst2

        dilate.src = dst2
        dilate.Run(ocvb)

        Static minSlider = findSlider("Minimum size for motion rectangle")
        contours.minArea = minSlider.value
        contours.src = dilate.dst1
        contours.Run(ocvb)

        For Each c In contours.contours
            Dim r = cv.Cv2.BoundingRect(c)
            If r.X >= 0 And r.Y >= 0 And r.X + r.Width < dst1.Width And r.Y + r.Height < dst1.Height Then
                Dim count = diff.dst2(r).CountNonZero()
                If count > 100 Then rectList.Add(r)
            End If
        Next

        dst1 = ocvb.color
        For i = 0 To rectList.Count - 1
            dst1.Rectangle(rectList(i), cv.Scalar.Yellow, 2)
        Next
    End Sub
End Class