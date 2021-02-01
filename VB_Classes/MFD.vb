﻿Imports cv = OpenCvSharp
Public Class MFD_Basics
    Inherits VBparent
    Public motion As Motion_Basics
    Public stableImg As cv.Mat
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
        task.desc = "Motion-Filtered basics - update only the changed regions"
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

        If motion.resetAll Or stableImg Is Nothing Or radioVal = 1 Then
            stableImg = src.Clone
        Else
            For Each rect In motion.intersect.enclosingRects
                dst2.Rectangle(rect, cv.Scalar.Yellow, 2)
                If rect.Width And rect.Height Then src(rect).CopyTo(stableImg(rect))
            Next
        End If

        dst1 = stableImg.Clone
    End Sub
End Class






Public Class MFD_Depth
    Inherits VBparent
    Dim mfd As MFD_Basics
    Public Sub New()
        initParent()
        mfd = New MFD_Basics
        label1 = "Motion-filtered depth data"
        task.desc = "Stabilize the depth image but update any areas with motion"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        mfd.src = task.depth32f
        mfd.Run()
        dst1 = mfd.dst1
        dst2 = mfd.dst2
        label2 = mfd.label2
    End Sub
End Class






Public Class MFD_PointCloud
    Inherits VBparent
    Dim mfd As MFD_Basics
    Public Sub New()
        initParent()
        mfd = New MFD_Basics
        label1 = "Motion-filtered PointCloud"
        task.desc = "Stabilize the PointCloud but update any areas with motion"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        mfd.src = task.pointCloud
        mfd.Run()
        dst1 = mfd.dst1
        dst2 = mfd.dst2
        label2 = mfd.label2
    End Sub
End Class








Public Class MFD_Sobel
    Inherits VBparent
    Dim mfd As MFD_Basics
    Dim sobel As Edges_Sobel
    Public Sub New()
        initParent()
        mfd = New MFD_Basics
        sobel = New Edges_Sobel
        label1 = "Sobel edges of Motion-Filtered RGB"
        task.desc = "Stabilize the Sobel output with MFD"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        mfd.src = src
        mfd.Run()
        dst2 = mfd.dst2
        label2 = mfd.label2

        sobel.src = mfd.dst1
        sobel.Run()
        dst1 = sobel.dst1
    End Sub
End Class







Public Class MFD_BinarizedSobel
    Inherits VBparent
    Dim mfd As MFD_Basics
    Dim mfdNoDepth As MFD_Basics
    Dim sobel As Edges_BinarizedSobel
    Public Sub New()
        initParent()
        mfd = New MFD_Basics
        mfdNoDepth = New MFD_Basics
        sobel = New Edges_BinarizedSobel
        label1 = "Binarized Sobel edges of Motion-Filtered RGB"
        task.desc = "Stabilize the binarized Sobel output with MFD"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        mfd.src = src
        mfd.Run()

        mfdNoDepth.src = task.inrange.nodepthMask
        mfdNoDepth.Run()

        sobel.src = mfd.dst1.Clone
        sobel.src.SetTo(0, mfdNoDepth.dst1)
        sobel.Run()

        dst1 = sobel.dst1
        dst1.SetTo(0, mfdNoDepth.dst1)
        dst2 = sobel.dst2
    End Sub
End Class






Public Class MFD_FloodFill
    Inherits VBparent
    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public floodPoints As New List(Of cv.Point)
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Dim initialMask As New cv.Mat
    Dim palette As Palette_Basics
    Dim sobel As MFD_BinarizedSobel
    Public Sub New()
        initParent()

        palette = New Palette_Basics
        sobel = New MFD_BinarizedSobel

        Dim paletteRadio = findRadio("Random - use slider to adjust")
        paletteRadio.Checked = True

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "FloodFill Step Size", 1, src.Cols / 2, 15)
            sliders.setupTrackBar(1, "FloodFill point distance from edge", 1, 25, 10)
            sliders.setupTrackBar(2, "Minimum length for missing contours", 3, 25, 4)
        End If

        Dim paletteSlider = findSlider("Number of color transitions (Used only with Random)")
        paletteSlider.Value = 180 ' insures every region will be a significantly different color
        task.desc = "Floodfill the image of MFD edges (binarized Sobel output)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static stepSlider = findSlider("FloodFill Step Size")
        Static fillSlider = findSlider("FloodFill point distance from edge")
        Dim fill = fillSlider.value
        Dim stepSize = stepSlider.Value

        Static MFD_OnOffRadio = findRadio("Use motion-filtered pixel values")
        Static saveStepSize As Integer
        Static saveFillDistance As Integer
        Static saveMFD_OnOff = MFD_OnOffRadio.checked
        Dim resetColors As Boolean
        If saveStepSize <> stepSize Or saveFillDistance <> fill Or MFD_OnOffRadio.checked <> saveMFD_OnOff Then
            resetColors = True
            saveStepSize = stepSize
            saveFillDistance = fill
            saveMFD_OnOff = MFD_OnOffRadio.checked
        End If

        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setMyActiveMat()

        masks.Clear()
        maskSizes.Clear()
        rects.Clear()
        centroids.Clear()

        Dim input = src.Clone
        If input.Type <> cv.MatType.CV_8UC1 Then
            sobel.src = src
            sobel.Run()
            input = sobel.dst2.Clone
        End If

        Static zero As New cv.Scalar(0)
        Static maskRect = New cv.Rect(1, 1, input.Width, input.Height)

        Dim maskPlus = New cv.Mat(New cv.Size(input.Width + 2, input.Height + 2), cv.MatType.CV_8UC1, 0)
        Dim rect As cv.Rect
        Dim pt As cv.Point
        Dim testCount As Integer
        Dim floodCount As Integer
        floodPoints.Clear()

        Dim inputRect As New cv.Rect(0, 0, fill, fill)
        Dim depthThreshold = fill * fill / 2
        Static lastFrame = input.Clone
        dst1 = input.Clone
        For y = fill To input.Height - fill - 1 Step stepSize
            For x = fill To input.Width - fill - 1 Step stepSize
                testCount += 1
                inputRect.X = x
                inputRect.Y = y
                Dim edgeCount = input(inputRect).CountNonZero
                Dim depthCount = task.depth32f(inputRect).CountNonZero
                If edgeCount = 0 And depthCount > depthThreshold Then
                    floodCount += 1
                    pt.X = x + fill / 2
                    pt.Y = y + fill / 2
                    Dim colorIndex = lastFrame.Get(Of Byte)(pt.Y, pt.X)
                    If resetColors Or colorIndex = 0 Then colorIndex = (255 - masks.Count - 1) Mod 255
                    Dim pixelCount = cv.Cv2.FloodFill(dst1, maskPlus, pt, cv.Scalar.All(colorIndex), rect, zero, zero, floodFlag Or (255 << 8))

                    If rect.Width And rect.Height Then
                        floodPoints.Add(pt)
                        Dim m = cv.Cv2.Moments(maskPlus(rect), True)
                        Dim centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)

                        maskSizes.Add(pixelCount, masks.Count)
                        masks.Add(maskPlus(maskRect)(rect))
                        rects.Add(rect)
                        centroids.Add(centroid)
                    End If
                End If
            Next
        Next

        lastFrame = dst1.Clone
        palette.src = dst1
        palette.Run()
        dst1 = palette.dst1

        dst2 = input.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each pt In floodPoints
            dst2.Circle(pt, ocvb.dotSize, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class