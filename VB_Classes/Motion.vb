'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Imports cv = OpenCvSharp
Public Class Motion_Basics
    Inherits VBparent
    Dim diff As Diff_Basics
    Dim contours As Contours_Basics
    Public intersect As Rectangle_Intersection
    Public changedPixels As Integer
    Public cumulativePixels As Integer
    Public resetAll As Boolean
    Dim imu As IMU_IscameraStable
    Dim minSlider As Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        intersect = New Rectangle_Intersection
        contours = New Contours_Basics()
        imu = New IMU_IscameraStable
        diff = New Diff_Basics()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Single frame motion threshold", 1, 100000, If(task.color.Width = 1280, 20000, 1000)) ' used only externally...
            sliders.setupTrackBar(1, "Cumulative motion threshold", 1, src.Total, If(task.color.Width = 1280, 200000, 100000)) ' used only externally...
            sliders.setupTrackBar(2, "Camera Motion threshold in radians X100", 1, 100, 3) ' how much camera motion is reasonable?
        End If

        minSlider = findSlider("Contour minimum area")
        minSlider.Value = 5

        label1 = "Enclosing rectangles are yellow in dst1 and dst2"
        task.desc = "Detect contours in the motion data and the resulting rectangles"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        diff.src = src
        If diff.src.Channels = 3 Then diff.src = diff.src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else diff.src = diff.src.Clone

        imu.Run()

        Static cumulativeThreshold = findSlider("Cumulative motion threshold")
        Static pixelThreshold = findSlider("Single frame motion threshold")

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

        dst1 = diff.src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If contours.contourlist.Count Then
            intersect.inputRects.Clear()
            For Each c In contours.contourlist
                Dim r = cv.Cv2.BoundingRect(c)
                If r.X < 0 Then r.X = 0
                If r.Y < 0 Then r.Y = 0
                If r.X + r.Width > dst2.Width Then r.Width = dst2.Width - r.X
                If r.Y + r.Height > dst2.Height Then r.Height = dst2.Height - r.Y
                intersect.inputRects.Add(r)
            Next
            intersect.Run()

            If dst2.Channels = 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For Each r In intersect.enclosingRects
                dst1.Rectangle(r, cv.Scalar.Yellow, 2)
                dst2.Rectangle(r, cv.Scalar.Yellow, 2)
            Next
            label2 = "Motion detected"
        Else
            label2 = "No motion detected with contours > " + CStr(minSlider.Value)
        End If
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

        For Each c In contours.contourlist
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







Public Class Motion_MinMaxDepth
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
            radio.check(0).Checked = True
        End If

        label1 = "32-bit format of the stable depth"
        label2 = "Motion mask"
        task.desc = "While minimizing options and dependencies, use RGB motion to figure out what depth values should change."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = task.depth32f.Clone

        motion.src = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        motion.Run()
        dst2 = motion.dst2.Clone

        If motion.resetAll Or externalReset Then
            externalReset = False
            dst1 = input.Clone
        Else
            If dst2.Channels <> 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            input.CopyTo(dst1, dst2)

            Static useNone = findRadio("Use unchanged depth input")
            Static useMax = findRadio("Use farthest distance")
            If useNone.checked = False Then
                If useMax.checked Then cv.Cv2.Max(input, dst1, dst1) Else cv.Cv2.Min(input, dst1, dst1)
            Else
                dst1 = input.Clone()
            End If
        End If
    End Sub
End Class








Public Class Motion_MinMaxPointCloud
    Inherits VBparent
    Public stable As Motion_MinMaxDepth
    Public splitPC() As cv.Mat
    Public Sub New()
        initParent()
        stable = New Motion_MinMaxDepth
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








Public Class Motion_MinMaxDepthColorized
    Inherits VBparent
    Dim stable As Motion_MinMaxDepth
    Dim colorize As Depth_ColorizerFastFade_CPP
    Public Sub New()
        initParent()
        colorize = New Depth_ColorizerFastFade_CPP
        stable = New Motion_MinMaxDepth
        label1 = "32-bit format stable depth data"
        label2 = "Colorized version of image at left"
        task.desc = "Colorize the stable depth (keeps Motion_MinMaxDepth at a minimum complexity)"
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








Public Class Motion_DepthShadow
    Inherits VBparent
    Dim motion As Motion_Basics
    Dim dMin As Depth_SmoothMin
    Public Sub New()
        initParent()
        motion = New Motion_Basics
        dMin = New Depth_SmoothMin

        Dim minSlider = findSlider("Contour minimum area")
        minSlider.Value = 100
        Dim cumSlider = findSlider("Cumulative motion threshold")
        cumSlider.Value = 2000

        label1 = "Motion of the depth shadow"
        task.desc = "Use the motion in the depth shadow to enhance Motion_Basics use of RGB"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        task.inrange.nodepthMask.convertto(dMin.src, cv.MatType.CV_32F)
        dMin.Run()
        dst2 = dMin.dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

        motion.src = dst2
        motion.Run()
        dst1 = motion.dst2

        Dim tmp = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        label2 = "Shadow that is consistently present. " + CStr(tmp.CountNonZero) + " pixels"
    End Sub
End Class









Public Class Motion_CameraStabilizerRandomInput
    Inherits VBparent
    Public Sub New()
        initParent()

        label1 = "Current frame (before)"
        label2 = "Image after shift"
        task.desc = "Generate images that have been arbitrarily shifted"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim shiftX = msRNG.Next(-8, 8)
        Dim shiftY = msRNG.Next(-8, 8)

        dst1 = input.Clone
        If shiftX <> 0 Or shiftY <> 0 Then
            Dim x = If(shiftX < 0, Math.Abs(shiftX), 0)
            Dim y = If(shiftY < 0, Math.Abs(shiftY), 0)

            Dim x2 = If(shiftX < 0, 0, shiftX)
            Dim y2 = If(shiftY < 0, 0, shiftY)

            Dim srcRect = New cv.Rect(x, y, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
            Dim dstRect = New cv.Rect(x2, y2, srcRect.Width, srcRect.Height)
            dst1(srcRect).CopyTo(input(dstRect))
        End If

        dst2 = input
    End Sub
End Class











Public Class Motion_CameraStabilizer
    Inherits VBparent
    Dim templateRect As cv.Rect
    Dim match As MatchTemplate_Basics
    Public shiftX As Integer
    Public shiftY As Integer
    Public Sub New()
        initParent()
        match = New MatchTemplate_Basics

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Maximum percentage of lost pixels before image is reset", 0, 100, 10)
            sliders.setupTrackBar(1, "Stabilizer Correlation Threshold X1000", 0, 1000, 950)
        End If

        templateRect = New cv.Rect(src.Width / 2 - src.Width / 8, src.Height / 2 - src.Height / 8, src.Width / 4, src.Height / 4)

        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        label1 = "Current frame"
        label2 = "Frame after canceled camera motion"
        task.desc = "Stabilize motion across all frames"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static netSlider = findSlider("Maximum percentage of lost pixels before image is reset")
        Dim lostMax = netSlider.value / 100

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame = input

        dst1 = input

        match.searchArea = lastFrame.clone
        match.template = input(templateRect)
        match.Run()

        Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point
        match.correlationMat.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

        Dim updateCount As Integer
        Static thresholdSlider = findSlider("Stabilizer Correlation Threshold X1000")
        Dim resetImage As Boolean
        If maxVal > thresholdSlider.value / thresholdSlider.maximum Then
            updateCount += 1
            shiftX = templateRect.X - maxLoc.X
            shiftY = templateRect.Y - maxLoc.Y
            Dim x1 = If(shiftX < 0, Math.Abs(shiftX), 0)
            Dim y1 = If(shiftY < 0, Math.Abs(shiftY), 0)

            Dim rect = New cv.Rect(x1, y1, src.Width - Math.Abs(shiftX), src.Height - Math.Abs(shiftY))
            dst2.SetTo(0)
            lastFrame(rect).CopyTo(dst2(rect))
            Dim nonZero = dst2.CountNonZero() / (dst2.Width * dst2.Height)
            If nonZero < (1 - lostMax) Then resetImage = True
            label2 = "Offset (x, y) = (" + CStr(shiftX) + "," + CStr(shiftY) + "), preserving " + Format(nonZero, "00%")
        Else
            resetImage = True
        End If
        If resetImage Then
            label2 = "Frame reinitialized"
            input.CopyTo(lastFrame)
            dst2 = lastFrame.clone
        End If
    End Sub
End Class








Public Class Motion_CameraStabilizerTest
    Inherits VBparent
    Dim random As Motion_CameraStabilizerRandomInput
    Dim stabilizer As Motion_CameraStabilizer
    Public Sub New()
        initParent()
        stabilizer = New Motion_CameraStabilizer
        random = New Motion_CameraStabilizerRandomInput

        label1 = "Unstable input to Motion_CameraStabilizer"
        label2 = "Output of Motion_CameraStabilizer"
        task.desc = "Test the Motion_CameraStabilizer with random movement"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        random.src = src
        random.Run()

        stabilizer.src = random.dst2.Clone
        stabilizer.Run()

        dst1 = stabilizer.dst1
        dst2 = stabilizer.dst2
    End Sub
End Class