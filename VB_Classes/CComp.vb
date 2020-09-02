Imports cv = OpenCvSharp
Imports System.Threading

'https://github.com/oreillymedia/Learning-OpenCV-3_examples/blob/master/example_14-03.cpp
Public Class CComp_Basics
    Inherits ocvbClass
    Public connectedComponents As Object
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public drawRectangles As Boolean = True
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "CComp Threshold", 0, 255, 10)
        sliders.setupTrackBar(1, "CComp Min Area", 0, src.Width * src.Height, 500)

        desc = "Draw bounding boxes around RGB binarized connected Components"
        label1 = "CComp binary"
        label2 = "Blob Rectangles and centroids"
    End Sub
    Private Function findNonZeroPixel(src As cv.Mat, startPt As cv.Point) As cv.Point
        For y = src.Height / 4 To src.Height - 1
            For x = src.Width / 4 To src.Width - 1
                If src.Get(Of cv.Vec3b)(y, x) <> cv.Scalar.All(0) Then Return New cv.Point(x, y)
            Next
        Next
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim threshold = sliders.trackbar(0).Value
        Dim binary As New cv.Mat
        If threshold < 128 Then
            binary = src.Threshold(threshold, 255, OpenCvSharp.ThresholdTypes.Binary + OpenCvSharp.ThresholdTypes.Otsu)
        Else
            binary = src.Threshold(threshold, 255, OpenCvSharp.ThresholdTypes.BinaryInv + OpenCvSharp.ThresholdTypes.Otsu)
        End If
        connectedComponents = cv.Cv2.ConnectedComponentsEx(binary)

        Static lastImage As New cv.Mat

        connectedComponents.RenderBlobs(dst1)
        dst1.CopyTo(dst2)
        dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        rects.Clear()
        centroids.Clear()
        masks.Clear()
        For Each blob In connectedComponents.Blobs
            If blob.Area < sliders.trackbar(1).Value Then Continue For ' skip it if too small...
            Dim rect = blob.Rect
            ' if it covers everything, then forget it...
            If rect.Width = src.Width And rect.Height = src.Height Then Continue For
            If rect.X + rect.Width > src.Width Or rect.Y + rect.Height > src.Height Then Continue For
            rects.Add(rect)
            Dim mask = dst1(rect)
            masks.Add(mask)

            Dim m = cv.Cv2.Moments(mask, True)
            If m.M00 = 0 Then Continue For ' avoid divide by zero...
            Dim centroid = New cv.Point(CInt(m.M10 / m.M00), CInt(m.M01 / m.M00))

            centroids.Add(centroid)

            If drawRectangles Then
                dst2(rect).Circle(centroid, 5, cv.Scalar.Yellow, -1)
                dst2.Rectangle(rect, cv.Scalar.White, 2)
            End If
        Next
        lastImage = dst1.Clone()
    End Sub
End Class




Public Class CComp_EdgeMask
    Inherits ocvbClass
    Dim ccomp As CComp_ColorDepth
    Dim edges As Edges_DepthAndColor
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        edges = New Edges_DepthAndColor(ocvb)

        ccomp = New CComp_ColorDepth(ocvb)

        desc = "Isolate Color connected components after applying the Edge Mask"
        label1 = "Edges_DepthAndColor (input to ccomp)"
        label2 = "Blob Rectangles with centroids (white)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        edges.src = src
        edges.Run(ocvb)
        dst1 = edges.dst1

        ccomp.src = If(standalone, edges.src, src)
        ccomp.Run(ocvb)
        dst2 = ccomp.dst1
    End Sub
End Class



Public Class CComp_ColorDepth
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "Min Blob size", 0, 10000, 100)

        label1 = "Color by Mean Depth"
        label2 = "Binary image using threshold binary+Otsu"
        desc = "Color connected components based on their depth"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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




Public Class CComp_Image
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        desc = "Connect components throughout the image"
        label1 = "Connected Components colored with Mean Depth"
        label2 = "Mask binary+otsu to help compute mean depth"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        dst2 = src.Threshold(0, 255, OpenCvSharp.ThresholdTypes.Binary + OpenCvSharp.ThresholdTypes.Otsu)

        Dim cc = cv.Cv2.ConnectedComponentsEx(dst2)

        Dim blobList As New List(Of cv.Rect)
        For Each blob In cc.Blobs.Skip(1) ' skip the blob for the whole image.
            If blob.Rect.Width > 1 And blob.Rect.Height > 1 Then blobList.Add(blob.Rect)
        Next

        blobList.Sort(Function(a, b) (a.Width * a.Height).CompareTo(b.Width * b.Height))

        For i = 0 To blobList.Count - 1
            Dim avg = ocvb.RGBDepth(blobList(i)).Mean(dst2(blobList(i)))
            dst1(blobList(i)).SetTo(avg, dst2(blobList(i)))
        Next

        cv.Cv2.BitwiseNot(dst2, dst2)
        cc = cv.Cv2.ConnectedComponentsEx(dst2)
        blobList.Clear()
        For Each blob In cc.Blobs.Skip(1) ' skip the blob for the whole image.
            If blob.Rect.Width > 1 And blob.Rect.Height > 1 Then blobList.Add(blob.Rect)
        Next

        blobList.Sort(Function(a, b) (a.Width * a.Height).CompareTo(b.Width * b.Height))

        For i = 0 To blobList.Count - 1
            Dim avg = ocvb.RGBDepth(blobList(i)).Mean(dst2(blobList(i)))
            dst1(blobList(i)).SetTo(avg, dst2(blobList(i)))
        Next
    End Sub
End Class




Public Class CComp_InRange_MT
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "InRange # of ranges", 2, 255, 15)
        sliders.setupTrackBar(1, "InRange Max Depth", 150, 10000, 3000)
        sliders.setupTrackBar(2, "InRange min Blob Size (in pixels) X1000", 1, 100, 10)

        desc = "Connected components in specific ranges"
        label2 = "Blob rectangles - largest to smallest"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "InRange # of ranges", 1, 20, 15)
        sliders.setupTrackBar(1, "InRange min Blob Size (in pixels) X1000", 1, 100, 10)

        desc = "Connect components in specific ranges"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
    Dim shapes As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        shapes = New cv.Mat(ocvb.homeDir + "Data/Shapes.png", cv.ImreadModes.Color)
        label1 = "Largest connected component"
        label2 = "RectView, LabelView, Binary, grayscale"
        desc = "Use connected components to isolate objects in image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
    Dim ccomp As CComp_Basics
    Dim overlap As Draw_OverlappingRectangles
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        ccomp = New CComp_Basics(ocvb)
        ccomp.sliders.trackbar(1).Value = 10 ' allow very small regions.

        overlap = New Draw_OverlappingRectangles(ocvb)

        label1 = "Input Image with all ccomp rectangles"
        label2 = "Unique rectangles (largest to smallest) colored by size"
        desc = "Define unique regions in the RGB image by eliminating overlapping rectangles."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ccomp.src = src
        ccomp.Run(ocvb)
        dst1 = ccomp.dst2

        overlap.inputRects = ccomp.rects
        overlap.inputMasks = ccomp.masks
        overlap.Run(ocvb)

        dst2.SetTo(0)
        For i = 0 To overlap.sortedMasks.Count - 1
            Dim mask = overlap.sortedMasks.ElementAt(overlap.sortedMasks.Count - i - 1).Value
            Dim rect = overlap.sortedMasks.ElementAt(overlap.sortedMasks.Count - i - 1).Key
            dst2(rect).SetTo(scalarColors(i), mask)
            dst2.Rectangle(rect, cv.Scalar.White, 2)
        Next
    End Sub
End Class
