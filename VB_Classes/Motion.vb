'  https://github.com/methylDragon/opencv-motion-detector/blob/master/Motion%20Detector.py
Imports cv = OpenCvSharp
Public Class Motion_Basics
    Inherits VBparent
    Dim diff As Diff_Basics
    Dim contours As Contours_Basics
    Public uRect As Rectangle_Union
    Public changedPixels As Integer
    Public cumulativePixels As Integer
    Public resetAll As Boolean
    Dim imu As IMU_IscameraStable
    Dim minSlider As Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        uRect = New Rectangle_Union
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

        uRect.inputRects.Clear()
        uRect.allRect = New cv.Rect
        dst1 = diff.src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If contours.contourlist.Count Then
            For Each c In contours.contourlist
                Dim r = cv.Cv2.BoundingRect(c)
                If r.X < 0 Then r.X = 0
                If r.Y < 0 Then r.Y = 0
                If r.X + r.Width > dst2.Width Then r.Width = dst2.Width - r.X
                If r.Y + r.Height > dst2.Height Then r.Height = dst2.Height - r.Y
                uRect.inputRects.Add(r)
            Next
            uRect.Run()

            If dst2.Channels = 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For Each r In uRect.inputRects
                dst1.Rectangle(r, cv.Scalar.Yellow, 2)
            Next
            dst1.Rectangle(uRect.allRect, cv.Scalar.Red, 2)
            dst2.Rectangle(uRect.allRect, cv.Scalar.Red, 2)
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
            radio.check(1).Checked = True
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
            Static useMin = findRadio("Use closest distance")
            Static useMax = findRadio("Use farthest distance")
            If useNone.checked = False Then
                If useMax.checked Then cv.Cv2.Max(input, dst1, dst1)
                If useMin.checked Then
                    cv.Cv2.Min(input, dst1, dst1)
                    dst1.SetTo(0, task.inrange.noDepthMask)
                End If
            Else
                dst1 = input.Clone()
            End If
        End If
    End Sub
End Class








Public Class Motion_MinMaxDepthRectangleUpdate
    Inherits VBparent
    Public extrema As Depth_SmoothMinMax
    Dim initialReset = True
    Public Sub New()
        initParent()
        extrema = New Depth_SmoothMinMax
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









Public Class Motion_Camera
    Inherits VBparent
    Dim stdev As Math_Stdev
    Dim match As MatchTemplate_Basics
    Public Sub New()
        initParent()
        match = New MatchTemplate_Basics
        stdev = New Math_Stdev

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Correlation Threshold X1000", 0, 1000, 950)
            sliders.setupTrackBar(1, "Segments meeting correlation threshold", 1, 500, 70)
        End If

        Dim widthSlider = findSlider("ThreadGrid Width")
        Dim heightSlider = findSlider("ThreadGrid Height")
        widthSlider.Value = 32
        heightSlider.Value = 32

        task.desc = "Detect camera motion with a concensus of results from stdev and correlation coefficients"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        stdev.src = input
        stdev.Run()
        dst1 = input.Clone
        dst2 = stdev.dst2

        Static stdevThresholdSlider = findSlider("Stdev Threshold")
        Dim stdevThreshold = stdevThresholdSlider.value
        Static corrThresholdSlider = findSlider("Correlation Threshold X1000")
        Dim correlationThreshold = corrThresholdSlider.value / 1000

        Static lastFrame = input
        Dim font = cv.HersheyFonts.HersheyComplex
        Dim fsize = ocvb.fontSize / 3
        Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point
        Dim mean As Double, stdDev As Double
        Dim updateCount As Integer

        Dim offsetX = stdev.grid.roiList(0).Width / 4
        Dim offsety = stdev.grid.roiList(0).Height / 4

        Dim avgX As Single, avgY As Single
        For Each roi In stdev.grid.roiList
            Dim newRoi = New cv.Rect(roi.X + roi.Width / 4, roi.Y + roi.Height / 4, roi.Width / 2, roi.Height / 2)
            cv.Cv2.MeanStdDev(dst1(newRoi), mean, stdDev)
            If stdDev > stdevThreshold Then
                match.sample = dst1(newRoi)
                match.searchMat = lastFrame(roi)
                match.Run()
                match.correlationMat.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)
                If maxVal > correlationThreshold Then
                    updateCount += 1
                    avgX += maxLoc.X - offsetX
                    avgY += maxLoc.Y - offsety
                    Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                    dst1.Rectangle(New cv.Rect(roi.X, roi.Y, roi.Width, roi.Height * 3 / 8), cv.Scalar.Black, -1)
                    cv.Cv2.PutText(dst1, CStr(maxLoc.X - offsetX) + "," + CStr(maxLoc.Y - offsety), pt, font, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                    dst2.Rectangle(New cv.Rect(roi.X, roi.Y, roi.Width, roi.Height * 3 / 8), cv.Scalar.Black, -1)
                    cv.Cv2.PutText(dst2, Format(maxVal, "0.00"), pt, font, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                End If
            End If
        Next

        lastFrame = input
        label1 = "Translation = " + Format(avgX / updateCount, "#0.00") + "," + Format(avgY / updateCount, "#0.00")
        label2 = CStr(updateCount) + " segments had correlation coefficients > " + Format(correlationThreshold, "0.00")
        dst1.SetTo(255, stdev.grid.gridMask)
    End Sub
End Class






Public Class Motion_FilteredRGB
    Inherits VBparent
    Public motion As Motion_Basics
    Public stableRGB As cv.Mat
    Public Sub New()
        initParent()
        motion = New Motion_Basics
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 2)
            radio.check(0).Text = "Use motion-filtered pixel values"
            radio.check(1).Text = "Use original (unchanged) pixels"
            radio.check(0).Checked = True
        End If
        label1 = "Motion-filtered image"
        task.desc = "Stabilize the BGR image but update any areas with motion"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        motion.src = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        motion.Run()
        label2 = motion.label2
        dst2 = If(motion.dst2.Channels = 1, motion.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR), motion.dst2.Clone)

        Dim radioVal As Integer
        Static frm As OptionsRadioButtons = findfrm(caller + " Radio Options")
        For radioVal = 0 To frm.check.Count - 1
            If frm.check(radioVal).Checked Then Exit For
        Next

        If motion.resetAll Or stableRGB Is Nothing Or radioVal = 1 Then
            stableRGB = src.Clone
        Else
            Dim rect = motion.uRect.allRect
            dst2.Rectangle(rect, cv.Scalar.Yellow, 2)
            If rect.Width And rect.Height Then src(rect).CopyTo(stableRGB(rect))
        End If

        dst1 = stableRGB.Clone
    End Sub
End Class






Public Class Motion_FilteredDepth
    Inherits VBparent
    Public motion As Motion_Basics
    Dim filteredRGB As Motion_FilteredRGB
    Public stableDepth As cv.Mat
    Public Sub New()
        initParent()
        motion = New Motion_Basics
        filteredRGB = New Motion_FilteredRGB
        label1 = "Motion-filtered depth data"
        task.desc = "Stabilize the depth image but update any areas with motion"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        motion.src = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        motion.Run()
        label2 = motion.label2
        dst2 = If(motion.dst2.Channels = 1, motion.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR), motion.dst2.Clone)

        Dim radioVal As Integer
        Static frm As OptionsRadioButtons = findfrm("Motion_FilteredRGB Radio Options")
        For radioVal = 0 To frm.check.Count - 1
            If frm.check(radioVal).Checked Then Exit For
        Next

        If motion.resetAll Or stableDepth Is Nothing Or radioVal = 1 Then
            stableDepth = task.depth32f.Clone
        Else
            Dim rect = motion.uRect.allRect
            If motion.uRect.allRect.Width Then
                dst2.Rectangle(rect, cv.Scalar.Yellow, 2)
                If rect.Width And rect.Height Then task.depth32f(rect).CopyTo(stableDepth(rect))
            End If
        End If

        dst1 = stableDepth
    End Sub
End Class