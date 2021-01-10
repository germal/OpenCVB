'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Imports cv = OpenCvSharp
Public Class Motion_Basics
    Inherits VBparent
    Dim diff As Diff_Basics
    Dim contours As Contours_Basics
    Public rectList As New List(Of cv.Rect)
    Public changedPixels As Integer
    Public cumulativePixels As Integer
    Public resetAll As Boolean
    Dim imu As IMU_IscameraStable
    Public Sub New()
        initParent()
        imu = New IMU_IscameraStable
        contours = New Contours_Basics()
        diff = New Diff_Basics()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Single frame motion threshold", 1, 100000, If(task.color.Width = 1280, 20000, 1000)) ' used only externally...
            sliders.setupTrackBar(1, "Cumulative motion threshold", 1, 100000, If(task.color.Width = 1280, 50000, 25000)) ' used only externally...
            sliders.setupTrackBar(2, "Camera Motion threshold in radians X100", 1, 100, 3) ' how much camera motion is reasonable?
        End If

        label2 = "Mask of pixel differences "
        task.desc = "Detect contours in the motion data and the resulting rectangles"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Channels = 3 Then dst1 = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else dst1 = input.Clone

        imu.Run()

        Static cumulativeThreshold = findSlider("Cumulative motion threshold")
        Static pixelThreshold = findSlider("Single frame motion threshold")

        diff.src = dst1
        diff.Run()
        dst2 = diff.dst2
        changedPixels = dst2.CountNonZero()
        cumulativePixels += changedPixels

        resetAll = imu.cameraStable = False Or cumulativePixels > cumulativeThreshold.value Or changedPixels > pixelThreshold.value Or task.depthOptionsChanged
        If resetAll Then
            cumulativePixels = 0
            task.depthOptionsChanged = False
        End If

        contours.src = dst2
        contours.Run()

        rectList.Clear()
        For Each c In contours.contours
            rectList.Add(cv.Cv2.BoundingRect(c))
        Next

        dst1 = If(input.Channels = 1, input.CvtColor(cv.ColorConversionCodes.GRAY2BGR), input)
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
    Public cumulativePixels As Integer
    Public Sub New()
        initParent()
        contours = New Contours_Basics()
        dilate = New DilateErode_Basics()
        diff = New Diff_Basics()
        blur = New Blur_Basics()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Frames to persist", 1, 100, 10)
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
        delayCounter += 1

        Static persistSlider = findSlider("Frames to persist")
        If delayCounter > persistSlider.value Then
            delayCounter = 0
            rectList.Clear()
        End If

        diff.src = dst1
        diff.Run()
        dst2 = diff.dst2
        changedPixels = dst2.CountNonZero()
        cumulativePixels += changedPixels

        dilate.src = dst2
        dilate.Run()

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







Public Class Motion_StableDepth
    Inherits VBparent
    Public motion As Motion_Basics
    Public externalReset As Boolean
    Public Sub New()
        initParent()
        motion = New Motion_Basics

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Use farthest distance"
            radio.check(1).Text = "Use closest distance"
            radio.check(2).Text = "Use unchanged depth input"
            radio.check(1).Checked = True
        End If

        label1 = "32-bit format of the stable depth"
        label2 = "8-bit format of the motion mask"
        task.desc = "While minimizing options and dependencies, use RGB motion to figure out what depth values should change."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = task.depth32f.Clone

        motion.src = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        motion.Run()
        dst2 = motion.dst2

        If motion.resetAll Or externalReset Then
            externalReset = False
            dst1 = input
        Else
            input.CopyTo(dst1, dst2)

            Static useNone = findRadio("Use unchanged depth input")
            Static useMin = findRadio("Use closest distance")
            Static useMax = findRadio("Use farthest distance")
            If useNone.checked = False Then
                If useMax.checked Then cv.Cv2.Max(input, dst1, dst1)
                If useMin.checked Then
                    cv.Cv2.Min(input, dst1, dst1)
                    dst1.SetTo(0, task.inrange.noDepthMask)
                End If
            Else
                input.CopyTo(dst1)
            End If
        End If
    End Sub
End Class








Public Class Motion_StableDepthRectangleUpdate
    Inherits VBparent
    Public extrema As Depth_Smooth
    Dim initialReset = True
    Public Sub New()
        initParent()
        extrema = New Depth_Smooth
        label2 = "dst2 = depth in 32-bit format in mm's"
        task.desc = "Use motion in RGB to determine when to update a 32-bit format input - depth X, Y, and Z."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.depth32f

        extrema.src = input
        extrema.Run()

        If extrema.resetAll Or initialReset Then
            initialReset = False
            dst2 = input
            Console.WriteLine("refresh...")
        Else
            dst2 = extrema.dst2
        End If
        dst1 = extrema.dst1
    End Sub
End Class








Public Class Motion_StablePointCloud
    Inherits VBparent
    Public stable As Motion_StableDepth
    Public splitPC() As cv.Mat
    Public Sub New()
        initParent()
        stable = New Motion_StableDepth
        label1 = stable.label1
        label2 = stable.label2
        task.desc = "Use the stable depth values to create a stable point cloud"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src.Clone
        If input.Type <> cv.MatType.CV_32FC3 Then input = task.pointCloud
        Dim split = input.Split()
        stable.src = split(2) * 1000
        stable.Run()

        dst1 = stable.dst1
        dst2 = stable.dst2
        label2 = "Cumulative Motion = " + Format(stable.motion.changedPixels / 1000, "#0.0") + "k pixels "
        If stable.motion.resetAll Or splitPC Is Nothing Or ocvb.frameCount < 30 Then
            splitPC = split
            dst2 = input
        Else
            splitPC(2) = (stable.dst1 * 0.001).ToMat
            split(0).CopyTo(splitPC(0), dst2)
            split(1).CopyTo(splitPC(1), dst2)
            cv.Cv2.Merge(splitPC, dst2)
        End If
    End Sub
End Class








Public Class Motion_StableDepthColorized
    Inherits VBparent
    Dim stable As Motion_StableDepth
    Dim colorize As Depth_ColorizerFastFade_CPP
    Public Sub New()
        initParent()
        colorize = New Depth_ColorizerFastFade_CPP
        stable = New Motion_StableDepth
        label1 = "32-bit format stable depth data"
        label2 = "Colorized version of image at left"
        task.desc = "Colorize the stable depth (keeps Motion_StableDepth at a minimum complexity)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static saveMin = task.minRangeSlider.Value
        Static saveMax = task.maxRangeSlider.Value
        If saveMin <> task.minRangeSlider.Value Or saveMax <> task.maxRangeSlider.Value Then stable.externalReset = True
        stable.Run()
        dst1 = stable.dst1

        colorize.src = dst1
        colorize.Run()
        dst2 = colorize.dst1
    End Sub
End Class