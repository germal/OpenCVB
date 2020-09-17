Imports cv = OpenCvSharp
Imports System.Threading

'https://github.com/oreillymedia/Learning-OpenCV-3_examples/blob/master/example_14-03.cpp
Public Class CComp_Basics
    Inherits VBparent
    Public connectedComponents As Object
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public edgeMask As cv.Mat
    Dim mats As Mat_4to1
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        mats = New Mat_4to1(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "CComp Min Area", 0, 10000, 500)
        sliders.setupTrackBar(1, "CComp Max Area", 0, src.Width * src.Height / 2, src.Width * src.Height / 4)
        sliders.setupTrackBar(2, "CComp threshold", 0, 255, 128)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Use OTSU to binarize the image"
        check.Box(1).Text = "Input to CComp is above CComp threshold"
        check.Box(0).Checked = True

        desc = "Draw bounding boxes around RGB binarized connected Components"
    End Sub
    Private Function renderBlobs(minSize As Integer, mask As cv.Mat, maxSize As Integer) As Integer
        Dim count As Integer = 0
        For Each blob In connectedComponents.Blobs
            If blob.Area < minSize Or blob.Area > maxSize Then Continue For ' skip it if too small or too big ...
            If blob.rect.width * blob.rect.height >= src.Width * src.Height Then Continue For
            If blob.rect.width = src.Width Or blob.rect.height = src.Height Then Continue For
            Dim rect = blob.Rect
            rects.Add(rect)
            Dim nextMask = mask(rect)
            masks.Add(nextMask)

            Dim m = cv.Cv2.Moments(nextMask, True)
            If m.M00 = 0 Then Continue For ' avoid divide by zero...
            centroids.Add(New cv.Point(CInt(m.M10 / m.M00 + rect.x), CInt(m.M01 / m.M00 + rect.y)))
            If standalone Then dst1(blob.Rect).SetTo(scalarColors(count), (dst2)(blob.Rect))
            count += 1
        Next
        Return count
    End Function
    Public Sub Run(ocvb As VBocvb)
        rects.Clear()
        centroids.Clear()
        masks.Clear()
        dst1.SetTo(0)
        Static minSizeSlider = findSlider("CComp Min Area")
        Static maxSizeSlider = findSlider("CComp Max Area")
        Dim minSize = minSizeSlider.value
        Dim maxSize = maxSizeSlider.value

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static thresholdSlider = findSlider("CComp threshold")
        Dim threshold = thresholdSlider.value
        Dim tFlag = If(check.Box(1).Checked, OpenCvSharp.ThresholdTypes.Binary, OpenCvSharp.ThresholdTypes.BinaryInv)
        tFlag += If(check.Box(0).Checked, OpenCvSharp.ThresholdTypes.Otsu, 0)
        mats.mat(0) = src.Threshold(threshold, 255, tFlag)
        If edgeMask IsNot Nothing Then mats.mat(0).SetTo(0, edgeMask)

        connectedComponents = cv.Cv2.ConnectedComponentsEx(mats.mat(0))
        connectedComponents.renderblobs(mats.mat(2))

        Dim count = renderBlobs(minSize, mats.mat(0), maxSize)
        cv.Cv2.BitwiseNot(mats.mat(0), mats.mat(1))

        connectedComponents = cv.Cv2.ConnectedComponentsEx(mats.mat(1))
        connectedComponents.renderblobs(mats.mat(3))

        count += renderBlobs(minSize, mats.mat(1), maxSize)
        If standalone Then
            For i = 0 To centroids.Count - 1
                dst1.Circle(centroids.ElementAt(i), 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                dst1.Rectangle(rects.ElementAt(i), cv.Scalar.White, 2)
            Next
        End If
        label1 = CStr(count) + " items found > " + CStr(minSize) + " and < " + CStr(maxSize)
        connectedComponents.renderblobs(dst2)

        mats.Run(ocvb)
        If check.Box(0).Checked Then
            If check.Box(1).Checked Then
                label2 = "OTSU light, OTSU dark, rendered light, rendered dark"
            Else
                label2 = "OTSU dark, OTSU light, rendered dark, rendered light"
            End If
        Else
            If check.Box(1).Checked Then
                label2 = ">Slider, <Slider, rendered >Slider, rendered <slider"
            Else
                label2 = "<Slider, >Slider, rendered <Slider, rendered >slider"
            End If
        End If
        dst2 = mats.dst1
    End Sub
End Class







Public Class CComp_Basics_FullImage
    Inherits VBparent
    Dim mats As Mat_4to1
    Dim basics As CComp_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        mats = New Mat_4to1(ocvb)
        basics = New CComp_Basics(ocvb)

        desc = "Connect components in the light half of OTSU threshold output, then use the dark half, then combine results."
        label2 = "Masks binary+otsu used to compute mean depth"
    End Sub
    Private Function colorWithDepth(ocvb As VBocvb, matIndex As Integer) As Integer
        Dim cc = cv.Cv2.ConnectedComponentsEx(mats.mat(matIndex))

        Dim blobList As New List(Of cv.Rect)
        For Each blob In cc.Blobs
            If blob.Rect.Width > 1 And blob.Rect.Height > 1 Then blobList.Add(blob.Rect)
        Next

        blobList.Sort(Function(a, b) (a.Width * a.Height).CompareTo(b.Width * b.Height))

        Dim count As Integer = 0
        Static minSizeSlider = findSlider("CComp Min Area")
        Static maxSizeSlider = findSlider("CComp Max Area")
        Dim minSize = minSizeSlider.value
        Dim maxSize = maxSizeSlider.value
        For Each blob In cc.Blobs
            If blob.Area < minSize Or blob.Area > maxSize Then Continue For ' skip it if too small or too big ...
            count += 1
            Dim avg = ocvb.RGBDepth(blob.Rect).Mean(mats.mat(matIndex)(blob.Rect))
            dst1(blob.Rect).SetTo(avg, mats.mat(matIndex)(blob.Rect))
        Next
        Return count
    End Function
    Public Sub Run(ocvb As VBocvb)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1.SetTo(0)

        mats.mat(0) = src.Threshold(0, 255, cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu)
        Dim count = colorWithDepth(ocvb, 0)
        cv.Cv2.BitwiseNot(mats.mat(0), mats.mat(1))
        count += colorWithDepth(ocvb, 1)
        label1 = CStr(count) + " items found and colored mean depth"

        mats.Run(ocvb)
        dst2 = mats.dst1
    End Sub
End Class



Public Class CComp_PointTracker
    Inherits VBparent
    Public basics As CComp_Basics
    Public pTrack As Kalman_PointTracker
    Public highlight As Highlight_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        highlight = New Highlight_Basics(ocvb)
        pTrack = New Kalman_PointTracker(ocvb)
        basics = New CComp_Basics(ocvb)

        desc = "Track connected componenent centroids and use it to match coloring"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        basics.src = src
        basics.Run(ocvb)
        dst2 = basics.dst1

        pTrack.queryPoints = basics.centroids
        pTrack.queryRects = basics.rects
        pTrack.queryMasks = basics.masks
        pTrack.Run(ocvb)
        dst1 = pTrack.dst1

        highlight.viewObjects = pTrack.viewObjects
        highlight.src = dst1
        highlight.Run(ocvb)
        dst1 = highlight.dst1
        If highlight.highlightPoint <> New cv.Point Then
            dst2 = highlight.dst2
            label2 = "Selected region in yellow"
        Else
            dst2 = src
        End If
        label1 = basics.label1
    End Sub
End Class







Public Class CComp_MaxBlobs
    Inherits VBparent
    Dim tracker As CComp_PointTracker
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        tracker = New CComp_PointTracker(ocvb)
        Dim checkOTSU = findCheckBox("Use OTSU to binarize the image")
        checkOTSU.Checked = False ' turn off OTSU so the slider works...

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Restart the calculation of the max blobs"

        desc = "Find the most blobs between specified min and max size"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static maxBlobs As Integer = -1
        Static maxValues(255) As Integer ' march through all 255 values and find the best...
        Static thresholdSlider = findSlider("CComp threshold")
        If ocvb.frameCount = 0 Then thresholdSlider.value = 0

        tracker.src = src
        tracker.Run(ocvb)
        dst1 = tracker.dst1
        dst2 = tracker.dst2

        Dim incr = 10
        If thresholdSlider.value + incr >= 255 Then
            For i = 0 To maxValues.Count - 1
                If maxBlobs < maxValues(i) Then
                    maxBlobs = maxValues(i)
                    thresholdSlider.value = i
                End If
            Next
        End If

        If check.Box(0).Checked Then
            check.Box(0).Checked = False
            maxBlobs = -1
            thresholdSlider.value = 0
        End If

        If maxBlobs = -1 Then
            maxValues(thresholdSlider.value) = tracker.pTrack.queryPoints.Count
            If thresholdSlider.value + incr < 255 Then thresholdSlider.value += incr
        End If
        dst2 = tracker.dst2
    End Sub
End Class




Public Class CComp_DepthEdges
    Inherits VBparent
    Dim ccomp As CComp_PointTracker
    Dim depth As Depth_Edges
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        ccomp = New CComp_PointTracker(ocvb)
        depth = New Depth_Edges(ocvb)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Use edge mask in connected components"
        check.Box(0).Checked = True

        desc = "Use depth edges to isolate connected components in depth"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        depth.Run(ocvb)
        If standalone Then dst2 = depth.dst2

        'If check.Box(0).Checked Then ccomp.basics.edgeMask = depth.dst2 Else ccomp.basics.edgeMask = Nothing
        If check.Box(0).Checked Then src.SetTo(0, depth.dst2)
        ccomp.src = src
        ccomp.Run(ocvb)
        dst1 = ccomp.dst1
        If ccomp.highlight.highlightPoint <> New cv.Point Then dst2 = ccomp.highlight.dst2
    End Sub
End Class





Public Class CComp_EdgeMask
    Inherits VBparent
    Dim ccomp As CComp_ColorDepth
    Dim edges As Edges_DepthAndColor
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        edges = New Edges_DepthAndColor(ocvb)

        ccomp = New CComp_ColorDepth(ocvb)

        desc = "Isolate Color connected components after applying the Edge Mask"
        label1 = "Edges_DepthAndColor (input to ccomp)"
        label2 = "Blob Rectangles with centroids (white)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        edges.src = src
        edges.Run(ocvb)
        dst1 = edges.dst1

        ccomp.src = If(standalone, edges.src, src)
        ccomp.Run(ocvb)
        dst2 = ccomp.dst1
    End Sub
End Class



Public Class CComp_ColorDepth
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "Min Blob size", 0, 10000, 100)

        label1 = "Color by Mean Depth"
        label2 = "Binary image using threshold binary+Otsu"
        desc = "Color connected components based on their depth"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = src.Threshold(0, 255, OpenCvSharp.ThresholdTypes.Binary + OpenCvSharp.ThresholdTypes.Otsu)
        dst1 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim cc = cv.Cv2.ConnectedComponentsEx(dst2)

        For Each blob In cc.Blobs.Skip(1)
            Dim roi = blob.Rect
            Dim avg = ocvb.RGBDepth(roi).Mean(dst2(roi))
            dst1(roi).SetTo(avg, dst2(roi))
        Next

        For Each blob In cc.Blobs.Skip(1)
            If blob.Area > sliders.trackbar(0).Value Then dst1.Rectangle(blob.Rect, cv.Scalar.White, 2)
        Next
    End Sub
End Class








Public Class CComp_InRange_MT
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "InRange # of ranges", 2, 255, 15)
        sliders.setupTrackBar(1, "InRange Max Depth", 150, 10000, 3000)
        sliders.setupTrackBar(2, "InRange min Blob Size (in pixels) X1000", 1, 100, 10)

        desc = "Connected components in specific ranges"
        label2 = "Blob rectangles - largest to smallest"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim rangeCount As Int32 = sliders.trackbar(0).Value
        Dim maxDepth = sliders.trackbar(1).Value
        Dim minBlobSize = sliders.trackbar(2).Value * 1000

        Dim depth32f = getDepth32f(ocvb)
        Dim mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        dst1.SetTo(0)
        Dim totalBlobs As Int32
        Parallel.For(0, rangeCount,
        Sub(i)
            Dim lowerBound = i * (255 / rangeCount)
            Dim upperBound = (i + 1) * (255 / rangeCount)
            Dim binary = src.InRange(lowerBound, upperBound)
            Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
            Dim roiList As New List(Of cv.Rect)
            For Each blob In cc.Blobs.Skip(1) ' skip the blob for the whole image.
                If blob.Rect.Width * blob.Rect.Height > minBlobSize Then roiList.Add(blob.Rect)
            Next
            Interlocked.Add(totalBlobs, roiList.Count)
            roiList.Sort(Function(a, b) (a.Width * a.Height).CompareTo(b.Width * b.Height))
            For j = roiList.Count - 1 To 0 Step -1
                Dim bin = binary(roiList(j)).Clone()
                Dim depth = depth32f(roiList(j))
                Dim meanDepth = depth.Mean(mask(roiList(j)))
                If meanDepth.Item(0) < maxDepth Then
                    Dim avg = ocvb.RGBDepth(roiList(j)).Mean(mask(roiList(j)))
                    dst1(roiList(j)).SetTo(avg, bin)
                    dst2(roiList(j)).SetTo(avg)
                End If
            Next
        End Sub)
        label1 = "# of blobs = " + CStr(totalBlobs) + " in " + CStr(rangeCount) + " regions"
    End Sub
End Class




Public Class CComp_InRange
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "InRange # of ranges", 1, 20, 15)
        sliders.setupTrackBar(1, "InRange min Blob Size (in pixels) X1000", 1, 100, 10)

        desc = "Connect components in specific ranges"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim rangeCount As Int32 = sliders.trackbar(0).Value
        Dim minBlobSize = sliders.trackbar(1).Value * 1000

        Dim mask = getDepth32f(ocvb).Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        Dim roiList As New List(Of cv.Rect)
        For i = 0 To rangeCount - 1
            Dim lowerBound = i * (255 / rangeCount)
            Dim upperBound = (i + 1) * (255 / rangeCount)
            Dim binary = src.InRange(lowerBound, upperBound)
            Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
            For Each blob In cc.Blobs.Skip(1) ' skip the blob for the whole image.
                If blob.Rect.Width * blob.Rect.Height > minBlobSize Then roiList.Add(blob.Rect)
            Next
        Next
        roiList.Sort(Function(a, b) (a.Width * a.Height).CompareTo(b.Width * b.Height))
        For i = 0 To roiList.Count - 1
            Dim avg = ocvb.RGBDepth(roiList(i)).Mean(mask(roiList(i)))
            dst1(roiList(i)).SetTo(avg)
        Next

        src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.AddWeighted(dst1, 0.5, src, 0.5, 0, dst1)
        label1 = "# of blobs = " + CStr(roiList.Count) + " in " + CStr(rangeCount) + " regions - smallest in front"
    End Sub
End Class





' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.ConnectedComponents.RenderBlobs(OpenCvSharp.Mat)/
Public Class CComp_Shapes
    Inherits VBparent
    Dim shapes As cv.Mat
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        shapes = New cv.Mat(ocvb.homeDir + "Data/Shapes.png", cv.ImreadModes.Color)
        label1 = "Largest connected component"
        label2 = "RectView, LabelView, Binary, grayscale"
        desc = "Use connected components to isolate objects in image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim gray = shapes.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim binary = gray.Threshold(0, 255, cv.ThresholdTypes.Otsu + cv.ThresholdTypes.Binary)
        Dim labelview = shapes.EmptyClone()
        Dim rectView = binary.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
        If cc.LabelCount <= 1 Then Exit Sub

        cc.RenderBlobs(labelview)
        For Each blob In cc.Blobs.Skip(1)
            rectView.Rectangle(blob.Rect, cv.Scalar.Red, 2)
        Next

        Dim maxBlob = cc.GetLargestBlob()
        Dim filtered = New cv.Mat
        cc.FilterByBlob(shapes, filtered, maxBlob)
        dst1 = filtered.Resize(dst1.Size())

        Dim matTop As New cv.Mat, matBot As New cv.Mat, mat As New cv.Mat
        cv.Cv2.HConcat(rectView, labelview, matTop)
        cv.Cv2.HConcat(binary, gray, matBot)
        matBot = matBot.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.VConcat(matTop, matBot, mat)
        dst2 = mat.Resize(dst2.Size())
    End Sub
End Class






Public Class CComp_OverlappingRectangles
    Inherits VBparent
    Dim ccomp As CComp_Basics
    Dim overlap As Draw_OverlappingRectangles
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        ccomp = New CComp_Basics(ocvb)
        ccomp.sliders.trackbar(1).Value = 10 ' allow very small regions.

        overlap = New Draw_OverlappingRectangles(ocvb)

        label1 = "Input Image with all ccomp rectangles"
        label2 = "Unique rectangles (largest to smallest) colored by size"
        desc = "Define unique regions in the RGB image by eliminating overlapping rectangles."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        ccomp.src = src
        ccomp.Run(ocvb)
        dst1 = ccomp.dst2

        overlap.inputRects = ccomp.rects
        overlap.inputMasks = ccomp.masks
        overlap.Run(ocvb)

        dst2.SetTo(0)
        'For i = 0 To overlap.sortedMasks.Count - 1
        '    Dim mask = overlap.sortedMasks.ElementAt(overlap.sortedMasks.Count - i - 1).Value
        '    Dim rect = overlap.sortedMasks.ElementAt(overlap.sortedMasks.Count - i - 1).Key
        '    dst2(rect).SetTo(scalarColors(i), mask)
        '    dst2.Rectangle(rect, cv.Scalar.White, 2)
        'Next
    End Sub
End Class

