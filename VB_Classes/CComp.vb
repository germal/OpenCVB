Imports cv = OpenCvSharp
Imports System.Threading

'https://github.com/oreillymedia/Learning-OpenCV-3_examples/blob/master/example_14-03.cpp
Public Class CComp_Basics
    Inherits ocvbClass
        Public srcGray As New cv.Mat
    Public dstGray As New cv.Mat

    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "CComp Threshold", 0, 255, 10)
        sliders.setupTrackBar2(ocvb, caller, "CComp Min Area", 0, 10000, 500)

        ocvb.desc = "Draw bounding boxes around RGB binarized connected Components"
        ocvb.label1 = "CComp binary"
        ocvb.label2 = "Blob Rectangles and centroids"
    End Sub
    Private Function findNonZeroPixel(src As cv.Mat, startPt As cv.Point) As cv.Point
        For y = src.Height / 4 To src.Height - 1
            For x = src.Width / 4 To src.Width - 1
                If src.Get(Of cv.Vec3b)(y, x) <> cv.Scalar.All(0) Then Return New cv.Point(x, y)
            Next
        Next
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        if standalone Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim threshold = sliders.TrackBar1.Value
        Dim binary As New cv.Mat
        If threshold < 128 Then
            binary = srcGray.Threshold(threshold, 255, OpenCvSharp.ThresholdTypes.Binary + OpenCvSharp.ThresholdTypes.Otsu)
        Else
            binary = srcGray.Threshold(threshold, 255, OpenCvSharp.ThresholdTypes.BinaryInv + OpenCvSharp.ThresholdTypes.Otsu)
        End If
        ocvb.result1 = binary.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim cc = cv.Cv2.ConnectedComponentsEx(binary)

        Static lastImage As New cv.Mat

        cc.RenderBlobs(ocvb.result1)
        dstGray = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayDepth = ocvb.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.result1.CopyTo(ocvb.result2)
        For Each blob In cc.Blobs
            If blob.Area < sliders.TrackBar2.Value Then Continue For ' skip it if too small...
            Dim rect = blob.Rect
            ' if it covers everything, then forget it...
            If rect.Width = srcGray.Width And rect.Height = srcGray.Height Then Continue For
            If rect.X + rect.Width > srcGray.Width Or rect.Y + rect.Height > srcGray.Height Then Continue For

            Dim mask = dstGray(rect)
            Dim m = cv.Cv2.Moments(mask, True)
            If m.M00 = 0 Then Continue For ' avoid divide by zero...
            Dim centroid = New cv.Point(CInt(m.M10 / m.M00), CInt(m.M01 / m.M00))
            ocvb.result2(rect).Circle(centroid, 5, cv.Scalar.White, -1)
            ocvb.result2.Rectangle(rect, cv.Scalar.White, 2)
        Next
        lastImage = ocvb.result1.Clone()
		MyBase.Finish(ocvb)
    End Sub
End Class




Public Class CComp_EdgeMask
    Inherits ocvbClass
    Dim ccomp As CComp_Basics
    Dim edges As Edges_CannyAndShadow
    Public srcGray As New cv.Mat
        Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        edges = New Edges_CannyAndShadow(ocvb, caller)

        ccomp = New CComp_Basics(ocvb, caller)

        ocvb.desc = "Isolate Color connected components after applying the Edge Mask"
        ocvb.label1 = "Edges_CannyAndShadow (input to ccomp)"
        ocvb.label2 = "Blob Rectangles with centroids (white)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        edges.Run(ocvb)

        If standalone Then
            ccomp.srcGray = srcGray
        Else
            ccomp.srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        ccomp.srcGray.SetTo(0, ccomp.dstGray)
        ccomp.Run(ocvb)
        ocvb.label1 = "Edges_CannyAndShadow (input to ccomp)"
        ocvb.label2 = "Blob Rectangles with centroids (white)"
		MyBase.Finish(ocvb)
    End Sub
    Public Sub MyDispose()
        ccomp.Dispose()
        edges.Dispose()
    End Sub
End Class



Public Class CComp_ColorDepth
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Color connected components based on their depth"
        ocvb.label1 = "Color by Mean Depth"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim binary = gray.Threshold(0, 255, OpenCvSharp.ThresholdTypes.Binary + OpenCvSharp.ThresholdTypes.Otsu)
        ocvb.result1 = binary.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
        cc.RenderBlobs(ocvb.result2)

        For Each blob In cc.Blobs.Skip(1)
            Dim roi = blob.Rect
            Dim avg = ocvb.RGBDepth(roi).Mean(binary(roi))
            ocvb.result1(roi).SetTo(avg, binary(roi))
        Next

        For Each blob In cc.Blobs.Skip(1)
            ocvb.result1.Rectangle(blob.Rect, cv.Scalar.White, 2)
        Next
		MyBase.Finish(ocvb)
    End Sub
End Class




Public Class CComp_Image
    Inherits ocvbClass
        Public srcGray As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Connect components throughout the image"
        ocvb.label1 = "Color Components with Mean Depth"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        if standalone Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim binary = srcGray.Threshold(0, 255, OpenCvSharp.ThresholdTypes.Binary + OpenCvSharp.ThresholdTypes.Otsu)
        ocvb.result1.SetTo(0)

        Dim cc = cv.Cv2.ConnectedComponentsEx(binary)

        Dim blobList As New List(Of cv.Rect)
        For Each blob In cc.Blobs.Skip(1) ' skip the blob for the whole image.
            If blob.Rect.Width > 1 And blob.Rect.Height > 1 Then blobList.Add(blob.Rect)
        Next

        blobList.Sort(Function(a, b) (a.Width * a.Height).CompareTo(b.Width * b.Height))

        For i = 0 To blobList.Count - 1
            Dim avg = ocvb.RGBDepth(blobList(i)).Mean(binary(blobList(i)))
            ocvb.result1(blobList(i)).SetTo(avg, binary(blobList(i)))
        Next

        cv.Cv2.BitwiseNot(binary, binary)
        cc = cv.Cv2.ConnectedComponentsEx(binary)
        blobList.Clear()
        For Each blob In cc.Blobs.Skip(1) ' skip the blob for the whole image.
            If blob.Rect.Width > 1 And blob.Rect.Height > 1 Then blobList.Add(blob.Rect)
        Next

        blobList.Sort(Function(a, b) (a.Width * a.Height).CompareTo(b.Width * b.Height))

        For i = 0 To blobList.Count - 1
            Dim avg = ocvb.RGBDepth(blobList(i)).Mean(binary(blobList(i)))
            ocvb.result1(blobList(i)).SetTo(avg, binary(blobList(i)))
        Next
		MyBase.Finish(ocvb)
    End Sub
End Class




Public Class CComp_InRange_MT
    Inherits ocvbClass
        Public srcGray As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "InRange # of ranges", 2, 255, 15)
        sliders.setupTrackBar2(ocvb, caller, "InRange Max Depth", 150, 10000, 3000)
        sliders.setupTrackBar3(ocvb, caller, "InRange min Blob Size (in pixels)", 1, 2000, 500)

        ocvb.desc = "Connected components in specific ranges"
        ocvb.label2 = "Blob rectangles - largest to smallest"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1.SetTo(0)
        ocvb.result2.SetTo(0)
        if standalone Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim rangeCount As Int32 = sliders.TrackBar1.Value
        Dim maxDepth = sliders.TrackBar2.Value
        Dim minBlobSize = sliders.TrackBar3.Value

        Dim depth32f = getDepth32f(ocvb)
        Dim mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        Dim totalBlobs As Int32
        Parallel.For(0, rangeCount - 1,
        Sub(i)
            Dim lowerBound = i * (255 / rangeCount)
            Dim upperBound = (i + 1) * (255 / rangeCount)
            Dim binary = srcGray.InRange(lowerBound, upperBound)
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
                    ocvb.result1(roiList(j)).SetTo(avg, bin)
                    ocvb.result2(roiList(j)).SetTo(avg)
                End If
            Next
        End Sub)
        ocvb.label1 = "# of blobs = " + CStr(totalBlobs) + " in " + CStr(rangeCount) + " regions"
		MyBase.Finish(ocvb)
    End Sub
End Class




Public Class CComp_InRange
    Inherits ocvbClass
        Public srcGray As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "InRange # of ranges", 1, 20, 15)
        sliders.setupTrackBar2(ocvb, caller, "InRange min Blob Size (in pixels)", 1, 2000, 500)

        ocvb.desc = "Connect components in specific ranges"
        ocvb.label2 = "Blob rectangles - smallest to largest"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1.SetTo(0)
        ocvb.result2.SetTo(0)
        if standalone Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim rangeCount As Int32 = sliders.TrackBar1.Value
        Dim minBlobSize = sliders.TrackBar2.Value

        Dim mask = getDepth32f(ocvb).Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        ocvb.result1 = mask.Clone()

        Dim roiList As New List(Of cv.Rect)
        For i = 0 To rangeCount - 1
            Dim lowerBound = i * (255 / rangeCount)
            Dim upperBound = (i + 1) * (255 / rangeCount)
            Dim binary = srcGray.InRange(lowerBound, upperBound)
            Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
            For Each blob In cc.Blobs.Skip(1) ' skip the blob for the whole image.
                If blob.Rect.Width * blob.Rect.Height > minBlobSize Then roiList.Add(blob.Rect)
            Next
        Next
        roiList.Sort(Function(a, b) (a.Width * a.Height).CompareTo(b.Width * b.Height))
        For i = 0 To roiList.Count - 1
            Dim avg = ocvb.RGBDepth(roiList(i)).Mean(mask(roiList(i)))
            ocvb.result2(roiList(i)).SetTo(avg)
        Next

        ocvb.label1 = "# of blobs = " + CStr(roiList.Count) + " in " + CStr(rangeCount) + " regions"
		MyBase.Finish(ocvb)
    End Sub
End Class





' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.ConnectedComponents.RenderBlobs(OpenCvSharp.Mat)/
Public Class CComp_Shapes
    Inherits ocvbClass
    Dim shapes As cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        shapes = New cv.Mat(ocvb.parms.HomeDir + "Data/Shapes.png", cv.ImreadModes.Color)
        ocvb.label1 = "Largest connected component"
        ocvb.label2 = "RectView, LabelView, Binary, grayscale"
        ocvb.desc = "Use connected components to isolate objects in image."
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
        ocvb.result1 = filtered.Resize(ocvb.result1.Size())

        Dim matTop As New cv.Mat, matBot As New cv.Mat, mat As New cv.Mat
        cv.Cv2.HConcat(rectView, labelview, matTop)
        cv.Cv2.HConcat(binary, gray, matBot)
        matBot = matBot.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.VConcat(matTop, matBot, mat)
        ocvb.result2 = mat.Resize(ocvb.result2.Size())
		MyBase.Finish(ocvb)
    End Sub
End Class

