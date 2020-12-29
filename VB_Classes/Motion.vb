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
            sliders.setupTrackBar(0, "Total motion threshold to resync", 1, 100000, If(task.color.Width = 1280, 20000, 10000)) ' used only externally...
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
    Public resetAll As Boolean
    Public pitch As Single ' in radians.
    Public yaw As Single ' in radians.
    Public roll As Single ' in radians.
    Public cameraStable As Boolean
    Public cumulativeChanges As Integer
    Public externalReset As Boolean
    Public Sub New()
        initParent()
        motion = New Motion_Basics
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Camera Motion threshold in radians X100", 1, 100, 3) ' how much camera motion is reasonable?
            sliders.setupTrackBar(1, "Image change threshold for single image", 1, 5000, 3000) ' how much motion is reasonable in a single image?
        End If

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

        pitch = task.IMU_AngularVelocity.X
        yaw = task.IMU_AngularVelocity.Y
        roll = task.IMU_AngularVelocity.Z

        Static cameraMotionThreshold = findSlider("Camera Motion threshold in radians X100")
        Static resyncThreshold = findSlider("Total motion threshold to resync")
        Static pixelThreshold = findSlider("Image change threshold for single image")

        cameraStable = If(cameraMotionThreshold.Value / 100 < Math.Abs(pitch) + Math.Abs(yaw) + Math.Abs(roll), False, True)

        motion.src = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        motion.Run()
        dst2 = motion.dst2

        cumulativeChanges += motion.changedPixels
        If cameraStable = False Or cumulativeChanges > resyncThreshold.value Or motion.changedPixels > pixelThreshold.value Or task.depthOptionsChanged Or externalReset Then
            resetAll = True
            externalReset = False
            dst1 = input
            cumulativeChanges = 0
        Else
            resetAll = False
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
    Public stableCloud As cv.Mat
    Public split() As cv.Mat
    Public myResetAll As Boolean
    Public Sub New()
        initParent()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Only preserve the Z depth data (unchecked will preserve X, Y, and Z)"
        End If

        stableCloud = task.pointCloud
        extrema = New Depth_Smooth
        task.desc = "Provide only a validated point cloud - one which has consistent depth data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        split = task.pointCloud.Split

        extrema.src = src
        If extrema.src.Type <> cv.MatType.CV_32F Then extrema.src = split(2) * 1000

        extrema.Run()

        ' if many pixels changed, then resetAll was triggered.  Leave task.pointcloud alone...
        If extrema.resetAll = False And myResetAll = False Then
            Static zCheck = findCheckBox("Only preserve the Z depth data (unchecked will preserve X, Y, and Z)")
            If zCheck.checked Then
                split(2) = extrema.dst2 * 0.001
                cv.Cv2.Merge(split, stableCloud)
            Else
                task.pointCloud.CopyTo(stableCloud, extrema.dMin.updateMask)
            End If
        Else
            myResetAll = False
            stableCloud = task.pointCloud
        End If
        dst1 = extrema.dst1
        dst2 = extrema.dst2
        task.pointCloud = stableCloud
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

        Dim split = task.pointCloud.Split()
        stable.src = split(2) * 1000

        stable.Run()

        dst1 = stable.dst1
        dst2 = stable.dst2
        label2 = "Cumulative Motion = " + Format(stable.motion.changedPixels / 1000, "#0.0") + "k pixels "
        If stable.resetAll Then
            splitPC = split
        Else
            splitPC(2) = (stable.dst1 * 0.001).ToMat
            split(0).CopyTo(splitPC(0), dst2)
            split(1).CopyTo(splitPC(1), dst2)
            cv.Cv2.Merge(splitPC, task.pointCloud)
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