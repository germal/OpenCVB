Imports cv = OpenCvSharp
Public Class Blob_Input : Implements IDisposable
    Dim rectangles As Draw_rotatedRectangles
    Dim circles As Draw_Circles
    Dim ellipses As Draw_Ellipses
    Dim poly As Draw_Polygon
    Dim Mats As Mat_4to1
    Public updateFrequency = 30
    Public Sub New(ocvb As AlgorithmData)
        rectangles = New Draw_rotatedRectangles(ocvb)
        circles = New Draw_Circles(ocvb)
        ellipses = New Draw_Ellipses(ocvb)
        poly = New Draw_Polygon(ocvb)

        rectangles.rect.sliders.TrackBar1.Value = 5
        circles.sliders.TrackBar1.Value = 5
        ellipses.sliders.TrackBar1.Value = 5
        poly.sliders.TrackBar1.Value = 5

        rectangles.rect.updateFrequency = 1
        circles.updateFrequency = 1
        ellipses.updateFrequency = 1
        poly.updateFrequency = 1

        poly.radio.check(1).Checked = True ' we want the convex polygon filled.

        Mats = New Mat_4to1(ocvb)
        Mats.externalUse = True

        ocvb.desc = "Test simple Blob Detector."
        ocvb.label2 = ""
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            rectangles.Run(ocvb)
            mats.mat(0) = ocvb.result1.Clone()

            circles.Run(ocvb)
            mats.mat(1) = ocvb.result1.Clone()

            ellipses.Run(ocvb)
            mats.mat(2) = ocvb.result1.Clone()

            poly.Run(ocvb)
            mats.mat(3) = ocvb.result2.Clone()
            Mats.Run(ocvb)
            ocvb.result2.CopyTo(ocvb.result1)
            ocvb.result2.SetTo(0)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        rectangles.Dispose()
        circles.Dispose()
        ellipses.Dispose()
        poly.Dispose()
        Mats.Dispose()
    End Sub
End Class



Public Class Blob_Detector_CS : Implements IDisposable
    Dim input As Blob_Input
    Dim check As New OptionsCheckbox
    Dim sliders As New OptionsSliders
    Dim blobDetector As New CS_Classes.Blob_Basics
    Public Sub New(ocvb As AlgorithmData)
        input = New Blob_Input(ocvb)
        input.updateFrequency = 1 ' it is pretty fast but sloppy...
        check.Setup(ocvb, 5)
        check.Box(0).Text = "FilterByArea"
        check.Box(1).Text = "FilterByCircularity"
        check.Box(2).Text = "FilterByConvexity"
        check.Box(3).Text = "FilterByInertia"
        check.Box(4).Text = "FilterByColor"
        If ocvb.parms.ShowOptions Then check.Show()
        check.Box(4).Checked = True ' filter by color...

        sliders.setupTrackBar1(ocvb, "min Threshold", 0, 255, 100)
        sliders.setupTrackBar2(ocvb, "max Threshold", 0, 255, 255)
        sliders.setupTrackBar3(ocvb, "Threshold Step", 1, 50, 5)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.label1 = "Blob_Detector_CS Input"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim blobParams = New cv.SimpleBlobDetector.Params
        blobParams.FilterByArea = check.Box(0).Checked
        blobParams.FilterByCircularity = check.Box(1).Checked
        blobParams.FilterByConvexity = check.Box(2).Checked
        blobParams.FilterByInertia = check.Box(3).Checked
        blobParams.FilterByColor = check.Box(4).Checked

        blobParams.MaxArea = 100
        blobParams.MinArea = 0.001

        blobParams.MinThreshold = sliders.TrackBar1.Value
        blobParams.MaxThreshold = sliders.TrackBar2.Value
        blobParams.ThresholdStep = sliders.TrackBar3.Value

        blobParams.MinDistBetweenBlobs = 10
        blobParams.MinRepeatability = 1

        input.Run(ocvb)

        ' The create method in SimpleBlobDetector is not available in VB.Net.  Not sure why.  To get around this, just use C# where create method works fine.
        blobDetector.Start(ocvb.result1, ocvb.result2, blobParams)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        input.Dispose()
        check.Dispose()
    End Sub
End Class



Public Class Blob_RenderBlobs : Implements IDisposable
    Dim input As Blob_Input
    Public Sub New(ocvb As AlgorithmData)
        input = New Blob_Input(ocvb)
        input.updateFrequency = 1

        ocvb.desc = "Use connected components to find blobs."
        ocvb.label2 = "Showing only the largest blob in test data"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 100 = 0 Then
            input.Run(ocvb)
            Dim gray = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim binary = gray.Threshold(0, 255, cv.ThresholdTypes.Otsu Or cv.ThresholdTypes.BinaryInv)
            Dim labelView = ocvb.result1.EmptyClone
            Dim stats As New cv.Mat
            Dim centroids As New cv.Mat
            Dim cc = cv.Cv2.ConnectedComponentsEx(binary)
            Dim labelCount = cv.Cv2.ConnectedComponentsWithStats(binary, labelView, stats, centroids)
            If cc.LabelCount <= 1 Then Exit Sub
            cc.RenderBlobs(labelView)

            Dim maxBlob = cc.GetLargestBlob()
            ocvb.result2.SetTo(0)
            cc.FilterByBlob(ocvb.result1, ocvb.result2, maxBlob)

            For Each blob In cc.Blobs.Skip(1)
                ocvb.result1.Rectangle(blob.Rect, cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        input.Dispose()
    End Sub
End Class






Public Class Blob_DepthClusters : Implements IDisposable
    Public histBlobs As Histogram_DepthClusters
    Public flood As FloodFill_RelativeRange
    Public externalUse As Boolean
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As AlgorithmData)
        shadow = New Depth_Holes(ocvb)
        shadow.externalUse = True

        histBlobs = New Histogram_DepthClusters(ocvb)

        flood = New FloodFill_RelativeRange(ocvb)
        flood.fBasics.sliders.TrackBar2.Value = 1 ' pixels are exact.
        flood.fBasics.sliders.TrackBar3.Value = 1 ' pixels are exact.
        flood.fBasics.externalUse = True

        ocvb.desc = "Highlight the distinct histogram blobs found with depth clustering."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        histBlobs.Run(ocvb)
        flood.fBasics.srcGray = ocvb.result2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        flood.fBasics.initialMask = shadow.holeMask
        flood.Run(ocvb)
        ocvb.label1 = CStr(histBlobs.valleys.rangeBoundaries.Count) + " Depth Clusters"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        histBlobs.Dispose()
        flood.Dispose()
        shadow.Dispose()
    End Sub
End Class






Public Class Blob_Rectangles : Implements IDisposable
    Dim blobs As Blob_LargestBlob
    Dim kalman() As Kalman_kDimension
    Public Sub New(ocvb As AlgorithmData)
        blobs = New Blob_LargestBlob(ocvb)
        ocvb.desc = "Get the blobs and their masks and outline them with a rectangle."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        blobs.Run(ocvb)
        ocvb.result1 = ocvb.color.Clone()
        Static blobCount As Int32
        If blobCount <> blobs.rects.Count Then
            blobCount = blobs.rects.Count
            ReDim kalman(blobs.rects.Count - 1)
            For i = 0 To blobCount - 1
                kalman(i) = New Kalman_kDimension(ocvb)
                kalman(i).kDimension = 4 ' there are 4 values in a cv.Rect to Kalmanize...
            Next
        End If
        For i = 0 To blobs.rects.Count - 1
            Dim rect = blobs.rects(i)
            ocvb.result1.Rectangle(rect, cv.Scalar.Red, 2)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        blobs.Dispose()
        If kalman IsNot Nothing Then
            For i = 0 To kalman.Length - 1
                kalman(i).Dispose()
            Next
        End If
    End Sub
End Class






Public Class Blob_LargestBlob : Implements IDisposable
    Dim blobs As Blob_DepthClusters
    Public rects As List(Of cv.Rect)
    Public masks As List(Of cv.Mat)
    Public externalUse As Boolean
    Public kalman As Kalman_GeneralPurpose
    Public blobIndex As Int32
    Public Sub New(ocvb As AlgorithmData)
        kalman = New Kalman_GeneralPurpose(ocvb)
        kalman.externalUse = True

        blobs = New Blob_DepthClusters(ocvb)
        ocvb.desc = "Gather all the blob data and display the largest."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        blobs.Run(ocvb)
        ocvb.result2 = ocvb.result1.Clone()
        rects = blobs.flood.fBasics.maskRects
        masks = blobs.flood.fBasics.masks

        Dim maskIndex = blobs.flood.fBasics.maskSizes.ElementAt(blobIndex).Value ' this is the largest contiguous blob
        ocvb.color.CopyTo(ocvb.result1, masks(maskIndex))
        kalman.src = {rects(maskIndex).X, rects(maskIndex).Y, rects(maskIndex).Width, rects(maskIndex).Height}
        kalman.Run(ocvb)
        Dim res = kalman.dst
        Dim rect = New cv.Rect(CInt(res(0)), CInt(res(1)), CInt(res(2)), CInt(res(3)))
        ocvb.result1.Rectangle(rect, cv.Scalar.Red, 2)
        ocvb.label1 = "Show the largest blob of the " + CStr(rects.Count) + " blobs"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        blobs.Dispose()
        If kalman IsNot Nothing Then kalman.Dispose()
    End Sub
End Class





Public Class Blob_LargestDepthCluster : Implements IDisposable
    Dim blobs As Blob_DepthClusters
    Public Sub New(ocvb As AlgorithmData)
        blobs = New Blob_DepthClusters(ocvb)

        ocvb.desc = "Display only the largest depth cluster (might not be contiguous.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        blobs.Run(ocvb)
        Dim blobList = blobs.histBlobs.valleys.rangeBoundaries

        Dim maxSize = blobs.histBlobs.valleys.sortedSizes.ElementAt(0)
        ocvb.result1.SetTo(0)
        Dim startEndDepth = blobs.histBlobs.valleys.rangeBoundaries.ElementAt(0)
        Dim tmp16 As New cv.Mat, mask As New cv.Mat
        If ocvb.color.Size <> ocvb.depth16.Size Then ocvb.depth16 = ocvb.depth16.Resize(ocvb.color.Size())
        cv.Cv2.InRange(ocvb.depth16, startEndDepth.X, startEndDepth.Y, tmp16)
        cv.Cv2.ConvertScaleAbs(tmp16, mask)
        ocvb.color.CopyTo(ocvb.result1, mask)
        ocvb.label1 = "Largest Depth Blob: " + Format(maxSize, "#,000") + " pixels (" + Format(maxSize / ocvb.color.Total, "#0.0%") + ")"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        blobs.Dispose()
    End Sub
End Class