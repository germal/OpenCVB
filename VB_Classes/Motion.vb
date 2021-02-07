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










Public Class Motion_Camera
    Inherits VBparent
    Dim stdev As Math_Stdev
    Dim match As MatchTemplate_Basics
    Public lastFrame As cv.Mat
    Public avgX As Single
    Public avgY As Single
    Public updateCount As Integer
    Public Sub New()
        initParent()
        match = New MatchTemplate_Basics
        stdev = New Math_Stdev

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Correlation Threshold X1000", 0, 1000, 950)
            sliders.setupTrackBar(1, "Segments meeting correlation threshold", 1, 500, 70)
        End If


        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "Display results with grid mask"
            check.Box(1).Text = "Display motion offset (x,y) value"
            check.Box(0).Checked = True
            check.Box(1).Checked = True
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

        If lastFrame Is Nothing Then lastFrame = input
        Dim fsize = ocvb.fontSize / 4
        Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point
        Dim mean As Double, stdDev As Double

        Dim offsetX = stdev.grid.roiList(0).Width / 4
        Dim offsety = stdev.grid.roiList(0).Height / 4

        updateCount = 0
        avgX = 0
        avgY = 0
        For Each roi In stdev.grid.roiList
            Dim newRoi = New cv.Rect(roi.X + roi.Width / 4, roi.Y + roi.Height / 4, roi.Width / 2, roi.Height / 2)
            cv.Cv2.MeanStdDev(dst1(newRoi), mean, stdDev)
            If stdDev > stdevThreshold Then
                match.searchArea = dst1(newRoi)
                match.template = lastFrame(roi)
                match.Run()
                match.correlationMat.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)
                If maxVal > correlationThreshold Then
                    updateCount += 1
                    avgX += offsetX - maxLoc.X
                    avgY += offsety - maxLoc.Y
                    Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                    Static xyValCheck = findCheckBox("Display motion offset (x,y) value")
                    If xyValCheck.checked Then
                        dst1.Rectangle(New cv.Rect(roi.X, roi.Y, roi.Width, roi.Height * 3 / 8), cv.Scalar.Black, -1)
                        cv.Cv2.PutText(dst1, CStr(offsetX - maxLoc.X) + "," + CStr(offsety - maxLoc.Y), pt, ocvb.font, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                        dst2.Rectangle(New cv.Rect(roi.X, roi.Y, roi.Width, roi.Height * 3 / 8), cv.Scalar.Black, -1)
                        cv.Cv2.PutText(dst2, Format(maxVal, "0.00"), pt, ocvb.font, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                    End If
                End If
            End If
        Next

        If updateCount > 0 Then
            avgX /= updateCount
            avgY /= updateCount
            lastFrame = input
            label1 = "Translation = " + Format(avgX, "#0.00") + "," + Format(avgY, "#0.00")
            label2 = CStr(updateCount) + " segments had correlation coefficients > " + Format(correlationThreshold, "0.00")

            Static gridCheck = findCheckBox("Display results with grid mask")
            If gridCheck.checked Then dst1.SetTo(255, stdev.grid.gridMask)
        End If
    End Sub
End Class









Public Class Motion_Camera8
    Inherits VBparent
    Public lastFrame As cv.Mat
    Public Sub New()
        initParent()

        task.desc = "Detect camera motion with a concensus of results from stdev and correlation coefficients"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If lastFrame Is Nothing Then lastFrame = input
        Dim fsize = ocvb.fontSize / 4
        Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point

        Dim maxX = 8, maxY = 8
        Dim inRect = New cv.Rect(maxX, maxY, dst1.Width - maxX * 2, dst1.Height - maxY * 2)

        Dim correlationMat As New cv.Mat
        cv.Cv2.MatchTemplate(input, input(inRect), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
        Dim correlation = correlationMat.Get(Of Single)(0, 0)
        label1 = "Correlation = " + Format(correlation, "#,##0.000")

        correlationMat.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)
        dst1 = correlationMat

        dst2.SetTo(0)
        dst2.Circle(maxLoc, ocvb.dotSize, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        dst2.Circle(New cv.Point(dst2.Width / 2, dst2.Height / 2), 1, cv.Scalar.Red, -1)

        lastFrame = input
    End Sub
End Class








Public Class Motion_Camera4Corner
    Inherits VBparent
    Public shiftX As Integer
    Public shiftY As Integer
    Public updateCount As Integer
    Public expectedUpdateCount = 2
    Dim match As MatchTemplate_Basics
    Dim corners(4 - 1) As cv.Rect
    Dim searchArea(corners.Length - 1) As cv.Rect
    Const cSize = 100
    Const cOffset = 20
    Public lastFrame As cv.Mat
    Public Sub New()
        initParent()
        corners(0) = New cv.Rect(cOffset, cOffset, cSize, cSize)
        corners(1) = New cv.Rect(src.Width - cSize - cOffset, cOffset, cSize, cSize)
        corners(2) = New cv.Rect(src.Width - cSize - cOffset, src.Height - cSize - cOffset, cSize, cSize)
        corners(3) = New cv.Rect(cOffset, src.Height - cSize - cOffset, cSize, cSize)

        Dim sSize = cOffset * 2 + cSize
        searchArea(0) = New cv.Rect(0, 0, sSize, sSize)
        searchArea(1) = New cv.Rect(src.Width - sSize, 0, sSize, sSize)
        searchArea(2) = New cv.Rect(src.Width - sSize, src.Height - sSize, sSize, sSize)
        searchArea(3) = New cv.Rect(0, src.Height - sSize, sSize, sSize)

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Stabilizer Correlation Threshold X1000", 0, 1000, 970)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Display info on results"
            check.Box(0).Checked = True
        End If

        match = New MatchTemplate_Basics
        task.desc = "Stabilize the image using the corners of the image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Type <> cv.MatType.CV_8UC1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = input.Clone

        If lastFrame Is Nothing Then lastFrame = input.Clone
        shiftX = 0
        shiftY = 0
        updateCount = 0
        Static shiftCheckBox = findCheckBox("Display info on results")
        Dim showShiftInfo = shiftCheckBox.checked
        For i = 0 To corners.Length - 1
            match.searchArea = dst1(searchArea(i))
            match.template = lastFrame(corners(i)).Clone
            match.Run()

            Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point
            match.correlationMat.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

            Static thresholdSlider = findSlider("Stabilizer Correlation Threshold X1000")
            If maxVal > thresholdSlider.value / thresholdSlider.maximum Then
                updateCount += 1
                shiftX += searchArea(i).X + maxLoc.X - corners(i).X
                shiftY += searchArea(i).Y + maxLoc.Y - corners(i).Y
            End If

            If showShiftInfo Then
                Dim msg = "(" + CStr(shiftX) + "," + CStr(shiftY) + ") " + Format(maxVal, "#0.00")
                Dim pt = If(i < 2, New cv.Point(searchArea(i).X, searchArea(i).Y + cSize + cOffset * 2), New cv.Point(searchArea(i).X, searchArea(i).Y - cOffset))
                dst1.Rectangle(New cv.Rect(pt.X, pt.Y, searchArea(i).Width, cOffset), cv.Scalar.Black, -1)
                ocvb.trueText(msg, pt.X, pt.Y)
            End If
        Next

        ' if at least 2 corners provide valid results, then the shift amounts should be accurate.
        If updateCount >= expectedUpdateCount Then
            shiftX /= updateCount
            shiftY /= updateCount
        End If
        If showShiftInfo Then
            For i = 0 To corners.Length - 1
                dst1.Rectangle(corners(i), cv.Scalar.White, 1)
                dst1.Rectangle(searchArea(i), cv.Scalar.White, 1)
            Next
        End If
        dst2 = lastFrame - input
        lastFrame = input
    End Sub
End Class








Public Class Motion_CameraTest
    Inherits VBparent
    Public cam As Motion_Camera4Corner
    Public shiftedInput As cv.Mat
    Public Sub New()
        initParent()
        cam = New Motion_Camera4Corner

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Insert X motion (in pixels)", -10, 10, 1)
            sliders.setupTrackBar(1, "Insert Y motion (in pixels)", -10, 10, 0)
        End If

        label2 = "Difference of the 2 frame sent to Motion_Camera"
        task.desc = "Test camera motion with specific image movement"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cam.lastFrame = input.Clone

        Static xSlider = findSlider("Insert X motion (in pixels)")
        Static ySlider = findSlider("Insert Y motion (in pixels)")
        Dim x1 = xSlider.value, y1 = ySlider.value, x As Integer, y As Integer, x2 As Integer, y2 As Integer
        If xSlider.value <> 0 Or ySlider.value <> 0 Then
            If x1 < 0 Then x = Math.Abs(x1) Else x = 0
            If y1 < 0 Then y = Math.Abs(y1) Else y = 0
            If x1 < 0 Then x2 = 0 Else x2 = x1
            If y1 < 0 Then y2 = 0 Else y2 = y1
            Dim rect = New cv.Rect(x, y, src.Width - Math.Abs(x1), src.Height - Math.Abs(y1))
            Dim dest = New cv.Rect(x2, y2, rect.Width, rect.Height)
            cam.lastFrame(rect).CopyTo(input(dest))
        End If
        shiftedInput = input.Clone

        cam.src = input.Clone
        cam.Run()
        dst1 = cam.dst1
        dst2 = cam.dst2
        label1 = cam.label1
    End Sub
End Class






Public Class Motion_Camera4Test
    Inherits VBparent
    Dim match As Motion_Camera4Corner
    Dim stdev As Math_Stdev
    Public lastFrame As cv.Mat
    Public avgX As Single
    Public avgY As Single
    Public updateCount As Integer
    Public Sub New()
        initParent()
        match = New Motion_Camera4Corner
        stdev = New Math_Stdev

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Correlation Threshold X1000", 0, 1000, 950)
            sliders.setupTrackBar(1, "Segments meeting correlation threshold", 1, 500, 70)
        End If


        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "Display results with grid mask"
            check.Box(1).Text = "Display motion offset (x,y) value"
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If

        Dim widthSlider = findSlider("ThreadGrid Width")
        Dim heightSlider = findSlider("ThreadGrid Height")
        widthSlider.Value = 32
        heightSlider.Value = 32

        task.desc = "Detect camera motion with a concensus of results from stdev and correlation coefficients"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        'Dim input = src
        'If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        'stdev.src = input
        'stdev.Run()
        'dst1 = input.Clone
        'dst2 = stdev.dst2

        'Static stdevThresholdSlider = findSlider("Stdev Threshold")
        'Dim stdevThreshold = stdevThresholdSlider.value
        'Static corrThresholdSlider = findSlider("Correlation Threshold X1000")
        'Dim correlationThreshold = corrThresholdSlider.value / 1000

        'If lastFrame Is Nothing Then lastFrame = input
        'Dim fsize = ocvb.fontSize / 4
        'Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point
        'Dim mean As Double, stdDev As Double

        'Dim offsetX = stdev.grid.roiList(0).Width / 4
        'Dim offsety = stdev.grid.roiList(0).Height / 4

        'updateCount = 0
        'avgX = 0
        'avgY = 0
        'For Each roi In stdev.grid.roiList
        '    Dim newRoi = New cv.Rect(roi.X + roi.Width / 4, roi.Y + roi.Height / 4, roi.Width / 2, roi.Height / 2)
        '    cv.Cv2.MeanStdDev(dst1(newRoi), mean, stdDev)
        '    If stdDev > stdevThreshold Then
        '        match.searchArea = dst1(newRoi)
        '        match.template = lastFrame(roi)
        '        match.Run()
        '        match.correlationMat.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)
        '        If maxVal > correlationThreshold Then
        '            updateCount += 1
        '            avgX += offsetX - maxLoc.X
        '            avgY += offsety - maxLoc.Y
        '            Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
        '            Static xyValCheck = findCheckBox("Display motion offset (x,y) value")
        '            If xyValCheck.checked Then
        '                dst1.Rectangle(New cv.Rect(roi.X, roi.Y, roi.Width, roi.Height * 3 / 8), cv.Scalar.Black, -1)
        '                cv.Cv2.PutText(dst1, CStr(offsetX - maxLoc.X) + "," + CStr(offsety - maxLoc.Y), pt, ocvb.font, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        '                dst2.Rectangle(New cv.Rect(roi.X, roi.Y, roi.Width, roi.Height * 3 / 8), cv.Scalar.Black, -1)
        '                cv.Cv2.PutText(dst2, Format(maxVal, "0.00"), pt, ocvb.font, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        '            End If
        '        End If
        '    End If
        'Next

        'If updateCount > 0 Then
        '    avgX /= updateCount
        '    avgY /= updateCount
        '    lastFrame = input
        '    label1 = "Translation = " + Format(avgX, "#0.00") + "," + Format(avgY, "#0.00")
        '    label2 = CStr(updateCount) + " segments had correlation coefficients > " + Format(correlationThreshold, "0.00")

        '    Static gridCheck = findCheckBox("Display results with grid mask")
        '    If gridCheck.checked Then dst1.SetTo(255, stdev.grid.gridMask)
        'End If
    End Sub
End Class









Public Class Motion_CameraRandom
    Inherits VBparent
    Public camTest As Motion_CameraTest
    Public Sub New()
        initParent()
        camTest = New Motion_CameraTest

        label2 = "Difference of the 2 frame sent to Motion_Camera"
        task.desc = "Test camera motion algorithm with random motion"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static xSlider = findSlider("Insert X motion (in pixels)")
        Static ySlider = findSlider("Insert Y motion (in pixels)")

        'xSlider.value = msRNG.Next(-8, 8)
        ySlider.value = 0 ' msRNG.Next(-8, 8)
        camTest.src = src
        camTest.Run()
        dst1 = camTest.shiftedInput
        dst2 = camTest.dst2
        label1 = camTest.label1
        label2 = camTest.label2
    End Sub
End Class








Public Class Motion_CameraCancel
    Inherits VBparent
    Dim camData As Motion_CameraRandom
    Dim cam As Motion_Camera4Corner
    Public Sub New()
        initParent()
        camData = New Motion_CameraRandom
        cam = New Motion_Camera4Corner
        Dim shiftCheckBox = findCheckBox("Display info on results")
        shiftCheckBox.Checked = False

        ocvb.fontSize /= 2
        label2 = "Difference of the 2 frame sent to Motion_Camera"
        task.desc = "Cancel the camera motion and center a stable image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        camData.src = src
        camData.Run()
        dst1 = camData.dst1.Clone

        cam.src = dst1.Clone
        cam.Run()

        Dim temp = cam.lastFrame - dst1.Clone
        cv.Cv2.ImShow("tmp", temp)
        label1 = cam.label1
        label2 = cam.label2

        Dim maxX = 8, maxY = 8
        Dim x1 = -cam.shiftX
        Dim y1 = -cam.shiftY
        Dim x As Integer, y As Integer

        Static xSlider = findSlider("Insert X motion (in pixels)")
        Static ySlider = findSlider("Insert Y motion (in pixels)")

        Console.WriteLine("x slider = " + CStr(xSlider.value))

        dst2.SetTo(0)
        x = maxX - x1
        y = maxY - y1
        Dim rect = New cv.Rect(x, y, src.Width - maxX * 2, src.Height - maxY * 2)
        Dim dest = New cv.Rect(maxX, maxY, rect.Width, rect.Height)
        If (x1 <> 0 Or y1 <> 0) And Math.Abs(x1) <= maxX And Math.Abs(y1) <= maxY And cam.updateCount >= cam.expectedUpdateCount Then
            cam.dst1(rect).CopyTo(dst2(dest))
        Else
            cam.dst1.CopyTo(dst2)
        End If
        cv.Cv2.PutText(dst2, "expected = " + CStr(xSlider.value) + " got = " + CStr(CInt(x1)), New cv.Point(10, 40), ocvb.font, ocvb.fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        dst1.Rectangle(dest, cv.Scalar.White, 1)
        dst2.Rectangle(dest, cv.Scalar.White, 1)
    End Sub
End Class