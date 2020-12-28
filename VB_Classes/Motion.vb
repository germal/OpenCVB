'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Imports cv = OpenCvSharp
Public Class Motion_Basics
    Inherits VBparent
    Dim diff As Diff_Basics
    Dim contours As Contours_Basics
    Public rectList As New List(Of cv.Rect)
    Public changedPixels As Integer
    Public Sub New()
        initParent()
        contours = New Contours_Basics()
        diff = New Diff_Basics()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Minimum size for contour bounding rectangle", 1, 1000, 200)
            sliders.setupTrackBar(1, "Total motion threshold to resync", 1, 100000, If(task.color.Width = 1280, 20000, 10000)) ' used only externally...
        End If

        label2 = "Mask of pixel differences "
        task.desc = "Detect contours in the motion data and the resulting rectangles"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If src.Channels = 3 Then dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else dst1 = src.Clone
        diff.src = dst1
        diff.Run()
        dst2 = diff.dst2
        changedPixels = dst2.CountNonZero()

        Static minSlider = findSlider("Minimum size for contour bounding rectangle")
        contours.minArea = minSlider.value
        contours.src = dst2
        contours.Run()

        rectList.Clear()
        For Each c In contours.contours
            rectList.Add(cv.Cv2.BoundingRect(c))
        Next

        dst1 = If(src.Channels = 1, src.CvtColor(cv.ColorConversionCodes.GRAY2BGR), src.Clone)
        For i = 0 To rectList.Count - 1
            dst1.Rectangle(rectList(i), cv.Scalar.Yellow, 2)
        Next
    End Sub
End Class







Public Class Motion_WithBlurDilate
    Inherits VBparent
    Dim blur As Blur_Basics
    Dim diff As Diff_Basics
    Dim dilate As DilateErode_Basics
    Dim contours As Contours_Basics
    Public rectList As New List(Of cv.Rect)
    Public changedPixels As Integer
    Public Sub New()
        initParent()
        contours = New Contours_Basics()
        dilate = New DilateErode_Basics()
        diff = New Diff_Basics()
        blur = New Blur_Basics()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Frames to persist", 1, 10000, 10)
            sliders.setupTrackBar(1, "Minimum size for motion rectangle", 1, 10000, 200)
        End If

        Dim iterSlider = findSlider("Dilate/Erode Kernel Size")
        iterSlider.Value = 2

        label2 = "Mask of pixel differences "
        task.desc = "Detect contours in the motion data using blur and dilate"
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

        dst1 = If(src.Channels = 1, src.CvtColor(cv.ColorConversionCodes.GRAY2BGR), src.Clone)
        For i = 0 To rectList.Count - 1
            dst1.Rectangle(rectList(i), cv.Scalar.Yellow, 2)
        Next
    End Sub
End Class