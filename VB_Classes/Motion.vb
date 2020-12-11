'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Imports cv = OpenCvSharp
Public Class Motion_Basics
    Inherits VBparent
    Dim blur As Blur_Basics
    Dim diff As Diff_Basics
    Dim dilate As DilateErode_Basics
    Dim contours As Contours_Basics
    Public rectList As New List(Of cv.Rect)
    Public changedPixels As Integer
    Dim threshSlider As Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        contours = New Contours_Basics()
        dilate = New DilateErode_Basics()
        diff = New Diff_Basics()
        blur = New Blur_Basics()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Frames to persist", 1, 10000, 10)
            sliders.setupTrackBar(1, "Minimum size for motion rectangle", 1, 10000, 1000)
            sliders.setupTrackBar(2, "Total motion threshold to resync", 1, 100000, If(task.color.Width = 1280, 20000, 5000))
        End If

        Dim iterSlider = findSlider("Dilate/Erode Kernel Size")
        iterSlider.Value = 2

        threshSlider = findSlider("Change threshold for each pixel")
        threshSlider.value = 25

        task.desc = "Detect contours in the motion data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If src.Channels = 3 Then dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else dst1 = src.Clone
        blur.src = dst1
        blur.Run()
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

        diff.src = dst1
        diff.lastFrame = firstFrame
        diff.Run()
        dst2 = diff.dst2
        changedPixels = dst2.CountNonZero()

        dilate.src = dst2
        dilate.Run()

        Static minSlider = findSlider("Minimum size for motion rectangle")
        contours.minArea = minSlider.value
        contours.src = dilate.dst1
        contours.Run()

        For Each c In contours.contours
            Dim r = cv.Cv2.BoundingRect(c)
            If r.X >= 0 And r.Y >= 0 And r.X + r.Width < dst1.Width And r.Y + r.Height < dst1.Height Then
                Dim count = diff.dst2(r).CountNonZero()
                If count > 100 Then rectList.Add(r)
            End If
        Next

        If src.Channels = 1 And dst1.Type = cv.MatType.CV_8U Then
            dst1 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For i = 0 To rectList.Count - 1
                dst1.Rectangle(rectList(i), cv.Scalar.Yellow, 2)
            Next
        End If
        label2 = "Mask of pixel difference > " + CStr(threshSlider.Value)
    End Sub
End Class