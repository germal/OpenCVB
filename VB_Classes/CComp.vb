Imports cv = OpenCvSharp
Imports System.Threading

'https://github.com/oreillymedia/Learning-OpenCV-3_examples/blob/master/example_14-03.cpp
Public Class CComp_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public externalUse As Boolean
    Public srcGray As New cv.Mat
    Private Class CompareArea : Implements IComparer(Of Int32)
        Public Function Compare(ByVal a As Int32, ByVal b As Int32) As Integer Implements IComparer(Of Int32).Compare
            ' why have compare for just int32?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "CComp Threshold", 0, 255, 0)
        sliders.setupTrackBar2(ocvb, "CComp Min Area", 0, 10000, 5000)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Draw bounding boxes around RGB binarized connected Components"
        ocvb.label1 = "CComp binary"
        ocvb.label2 = "Blob Rectangles"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim threshold = sliders.TrackBar1.Value
        Dim binary As New cv.Mat
        If threshold < 128 Then
            binary = srcGray.Threshold(threshold, 255, OpenCvSharp.ThresholdTypes.Binary + OpenCvSharp.ThresholdTypes.Otsu)
        Else
            binary = srcGray.Threshold(threshold, 255, OpenCvSharp.ThresholdTypes.BinaryInv + OpenCvSharp.ThresholdTypes.Otsu)
        End If
        ocvb.result1 = binary.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
        cc.RenderBlobs(ocvb.result1)
        Dim grayMasks = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim sortedBlob As New SortedList(Of Int32, cv.Rect)(New CompareArea)
        For i = 1 To cc.Blobs.Count - 1
            Dim blob = cc.Blobs.ElementAt(i)
            If blob.Area >= sliders.TrackBar2.Value Then sortedBlob.Add(blob.Area, blob.Rect)
        Next

        Dim grayDepth = ocvb.depthRGB.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.result2 = New cv.Mat(ocvb.result2.Size(), cv.MatType.CV_8U, 0)
        ocvb.result1.SetTo(0)
        For i = 0 To sortedBlob.Count - 1
            Dim rect = sortedBlob.ElementAt(i).Value
            If rect.X + rect.Width < grayDepth.Width And rect.Y + rect.Height < grayDepth.Height Then
                Dim mask = grayMasks(rect)
                Dim m = cv.Cv2.Moments(mask, True)
                Dim centroid = New cv.Point(CInt(m.M10 / m.M00), CInt(m.M01 / m.M00))
                Dim mean = grayDepth(rect).Mean(mask)
                ocvb.result2(rect).SetTo(mean, mask)
                ocvb.color(rect).CopyTo(ocvb.result1(rect), mask)
                ocvb.result1(rect).Circle(centroid, 5, cv.Scalar.White, -1)
                ocvb.result1.Rectangle(rect, cv.Scalar.White, 1)
                ocvb.result2(rect).Circle(centroid, 5, cv.Scalar.White, -1)
                ocvb.result2.Rectangle(rect, cv.Scalar.White, 1)
            End If
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class CComp_EdgeMask : Implements IDisposable
    Dim ccomp As CComp_Basics
    Dim edges As Edges_CannyAndShadow
    Public srcGray As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        edges = New Edges_CannyAndShadow(ocvb)

        ccomp = New CComp_Basics(ocvb)
        ccomp.externalUse = True

        ocvb.desc = "Isolate Color connected components after applying the Edge Mask"
        ocvb.label1 = "Edges_CannyAndShadow (input to ccomp)"
        ocvb.label2 = "Blob Rectangles with centroids (white)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        edges.Run(ocvb)

        If externalUse Then
            ccomp.srcGray = srcGray
        Else
            ccomp.srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        ccomp.srcGray.SetTo(0, ocvb.result1)
        ccomp.Run(ocvb)
        ocvb.label1 = "Edges_CannyAndShadow (input to ccomp)"
        ocvb.label2 = "Blob Rectangles with centroids (white)"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        ccomp.Dispose()
        edges.Dispose()
    End Sub
End Class



Public Class CComp_ColorDepth : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
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
            Dim avg = ocvb.depthRGB(roi).Mean(binary(roi))
            ocvb.result1(roi).SetTo(avg, binary(roi))
        Next

        For Each blob In cc.Blobs.Skip(1)
            ocvb.result1.Rectangle(blob.Rect, cv.Scalar.White, 2)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class CComp_Image : Implements IDisposable
    Public externalUse As Boolean
    Public srcGray As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Connect components throughout the image"
        ocvb.label1 = "Color Components with Mean Depth"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim binary = srcGray.Threshold(0, 255, OpenCvSharp.ThresholdTypes.Binary + OpenCvSharp.ThresholdTypes.Otsu)
        ocvb.result1.SetTo(0)

        Dim cc = cv.Cv2.ConnectedComponentsEx(binary)

        Dim blobList As New List(Of cv.Rect)
        For Each blob In cc.Blobs.Skip(1) ' skip the blob for the whole image.
            If blob.Rect.Width > 1 And blob.Rect.Height > 1 Then blobList.Add(blob.Rect)
        Next

        blobList.Sort(Function(a, b) (a.Width * a.Height).CompareTo(b.Width * b.Height))

        For i = 0 To blobList.Count - 1
            Dim avg = ocvb.depthRGB(blobList(i)).Mean(binary(blobList(i)))
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
            Dim avg = ocvb.depthRGB(blobList(i)).Mean(binary(blobList(i)))
            ocvb.result1(blobList(i)).SetTo(avg, binary(blobList(i)))
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class CComp_InRange_MT : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public externalUse As Boolean
    Public srcGray As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "InRange # of ranges", 2, 255, 15)
        sliders.setupTrackBar2(ocvb, "InRange Max Depth", 150, 10000, 3000)
        sliders.setupTrackBar3(ocvb, "InRange min Blob Size (in pixels)", 1, 2000, 500)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Connected components in specific ranges"
        ocvb.label2 = "Blob rectangles - largest to smallest"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1.SetTo(0)
        ocvb.result2.SetTo(0)
        If externalUse = False Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim rangeCount As Int32 = sliders.TrackBar1.Value
        Dim maxDepth = sliders.TrackBar2.Value
        Dim minBlobSize = sliders.TrackBar3.Value

        Dim mask = ocvb.depth.Threshold(1, 255, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)

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
                Dim depth = ocvb.depth(roiList(j))
                Dim meanDepth = depth.Mean(mask(roiList(j)))
                If meanDepth.Item(0) < maxDepth Then
                    Dim avg = ocvb.depthRGB(roiList(j)).Mean(mask(roiList(j)))
                    ocvb.result1(roiList(j)).SetTo(avg, bin)
                    ocvb.result2(roiList(j)).SetTo(avg)
                End If
            Next
        End Sub)
        ocvb.label1 = "# of blobs = " + CStr(totalBlobs) + " in " + CStr(rangeCount) + " regions"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class CComp_InRange : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public externalUse As Boolean
    Public srcGray As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "InRange # of ranges", 1, 20, 15)
        sliders.setupTrackBar2(ocvb, "InRange min Blob Size (in pixels)", 1, 2000, 500)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Connect components in specific ranges"
        ocvb.label2 = "Blob rectangles - smallest to largest"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1.SetTo(0)
        ocvb.result2.SetTo(0)
        If externalUse = False Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim rangeCount As Int32 = sliders.TrackBar1.Value
        Dim minBlobSize = sliders.TrackBar2.Value

        Dim mask = ocvb.depth.Threshold(1, 255, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
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
        'For i = roiList.Count - 1 To 0 Step -1
        For i = 0 To roiList.Count - 1
            Dim avg = ocvb.depthRGB(roiList(i)).Mean(mask(roiList(i)))
            ocvb.result2(roiList(i)).SetTo(avg)
        Next

        ocvb.label1 = "# of blobs = " + CStr(roiList.Count) + " in " + CStr(rangeCount) + " regions"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class
