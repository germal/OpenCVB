Imports cv = OpenCvSharp
Public Class Blob_Input
    Inherits ocvbClass
    Dim rectangles As Draw_rotatedRectangles
    Dim circles As Draw_Circles
    Dim ellipses As Draw_Ellipses
    Dim poly As Draw_Polygon
    Dim Mats As Mat_4to1
    Public updateFrequency = 30
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        rectangles = New Draw_rotatedRectangles(ocvb, caller)
        circles = New Draw_Circles(ocvb, caller)
        ellipses = New Draw_Ellipses(ocvb, caller)
        poly = New Draw_Polygon(ocvb, caller)

        rectangles.rect.sliders.TrackBar1.Value = 5
        circles.sliders.TrackBar1.Value = 5
        ellipses.sliders.TrackBar1.Value = 5
        poly.sliders.TrackBar1.Value = 5

        rectangles.rect.updateFrequency = 1
        circles.updateFrequency = 1
        ellipses.updateFrequency = 1
        poly.updateFrequency = 1

        poly.radio.check(1).Checked = True ' we want the convex polygon filled.

        Mats = New Mat_4to1(ocvb, caller)

        ocvb.desc = "Test simple Blob Detector."
        ocvb.label2 = ""
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            rectangles.Run(ocvb)
            Mats.mat(0) = ocvb.result1.Clone()

            circles.Run(ocvb)
            Mats.mat(1) = ocvb.result1.Clone()

            ellipses.Run(ocvb)
            Mats.mat(2) = ocvb.result1.Clone()

            poly.Run(ocvb)
            Mats.mat(3) = ocvb.result2.Clone()
            Mats.Run(ocvb)
            ocvb.result2.CopyTo(ocvb.result1)
            ocvb.result2.SetTo(0)
        End If
    End Sub
End Class



Public Class Blob_Detector_CS
    Inherits ocvbClass
    Dim input As Blob_Input
    Dim blobDetector As New CS_Classes.Blob_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        input = New Blob_Input(ocvb, caller)
        input.updateFrequency = 1 ' it is pretty fast but sloppy...
        check.Setup(ocvb, caller, 5)
        check.Box(0).Text = "FilterByArea"
        check.Box(1).Text = "FilterByCircularity"
        check.Box(2).Text = "FilterByConvexity"
        check.Box(3).Text = "FilterByInertia"
        check.Box(4).Text = "FilterByColor"
        check.Box(4).Checked = True ' filter by color...

        sliders.setupTrackBar1(ocvb, caller, "min Threshold", 0, 255, 100)
        sliders.setupTrackBar2(ocvb, caller, "max Threshold", 0, 255, 255)
        sliders.setupTrackBar3(ocvb, caller, "Threshold Step", 1, 50, 5)

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
End Class



Public Class Blob_RenderBlobs
    Inherits ocvbClass
    Dim input As Blob_Input
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        input = New Blob_Input(ocvb, caller)
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
End Class






Public Class Blob_DepthClusters
    Inherits ocvbClass
    Public histBlobs As Histogram_DepthClusters
    Public flood As FloodFill_RelativeRange
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        shadow = New Depth_Holes(ocvb, caller)

        histBlobs = New Histogram_DepthClusters(ocvb, caller)

        flood = New FloodFill_RelativeRange(ocvb, caller)
        flood.fBasics.sliders.TrackBar2.Value = 1 ' pixels are exact.
        flood.fBasics.sliders.TrackBar3.Value = 1 ' pixels are exact.

        ocvb.desc = "Highlight the distinct histogram blobs found with depth clustering."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        histBlobs.src = shadow.dst
        histBlobs.Run(ocvb)
        dst = histBlobs.dst
        flood.fBasics.src = histBlobs.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        flood.fBasics.initialMask = shadow.holeMask
        flood.Run(ocvb)
        dst2 = flood.fBasics.dst2
        ocvb.label1 = CStr(histBlobs.valleys.rangeBoundaries.Count) + " Depth Clusters"
    End Sub
End Class






Public Class Blob_Rectangles
    Inherits ocvbClass
    Dim blobs As Blob_Largest
    Dim kalman() As Kalman_Basics
    Private Class CompareRect : Implements IComparer(Of cv.Rect)
        Public Function Compare(ByVal a As cv.Rect, ByVal b As cv.Rect) As Integer Implements IComparer(Of cv.Rect).Compare
            Dim aSize = a.Width * a.Height
            Dim bSize = b.Width * b.Height
            If aSize > bSize Then Return -1
            Return 1
        End Function
    End Class
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.parms.ShowOptions = False
        blobs = New Blob_Largest(ocvb, caller)
        ocvb.desc = "Get the blobs and their masks and outline them with a rectangle."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        blobs.Run(ocvb)
        ocvb.result1 = ocvb.color.Clone()

        ' sort the blobs by size before delivery to kalman
        Dim sortedBlobs As New SortedList(Of cv.Rect, Integer)(New CompareRect)
        For i = 0 To blobs.rects.Count - 1
            sortedBlobs.Add(blobs.rects(i), i)
        Next
        Static blobCount As Int32
        Dim blobsToShow = Math.Min(3, blobs.rects.Count - 1)
        If blobCount <> blobsToShow Then
            blobCount = blobsToShow
            ReDim kalman(blobsToShow - 1)
            For i = 0 To blobsToShow - 1
                kalman(i) = New Kalman_Basics(ocvb, caller)
            Next
        End If

        ocvb.label1 = "Showing top " + CStr(blobsToShow) + " of the " + CStr(blobs.rects.Count) + " blobs found "
        For i = 0 To blobsToShow - 1
            Dim rect = sortedBlobs.ElementAt(i).Key
            kalman(i).input = {rect.X, rect.Y, rect.Width, rect.Height}
            kalman(i).Run(ocvb)
            rect = New cv.Rect(kalman(i).output(0), kalman(i).output(1), kalman(i).output(2), kalman(i).output(3))
            ocvb.result1.Rectangle(rect, ocvb.colorScalar(i Mod 255), 2)
        Next
    End Sub
End Class






Public Class Blob_Largest
    Inherits ocvbClass
    Dim blobs As Blob_DepthClusters
    Public rects As List(Of cv.Rect)
    Public masks As List(Of cv.Mat)
    Public kalman As Kalman_Basics
    Public blobIndex As Int32
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        kalman = New Kalman_Basics(ocvb, caller)

        blobs = New Blob_DepthClusters(ocvb, caller)
        ocvb.desc = "Gather all the blob data and display the largest."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        blobs.Run(ocvb)
        rects = blobs.flood.fBasics.maskRects
        masks = blobs.flood.fBasics.masks

        If masks.Count > 0 Then
            Dim maskIndex = blobs.flood.fBasics.maskSizes.ElementAt(blobIndex).Value ' this is the largest contiguous blob
            ocvb.color.CopyTo(ocvb.result1, masks(maskIndex))
            kalman.input = {rects(maskIndex).X, rects(maskIndex).Y, rects(maskIndex).Width, rects(maskIndex).Height}
            kalman.Run(ocvb)
            Dim res = kalman.output
            Dim rect = New cv.Rect(CInt(res(0)), CInt(res(1)), CInt(res(2)), CInt(res(3)))
            ocvb.result1.Rectangle(rect, cv.Scalar.Red, 2)
        End If
        ocvb.label1 = "Show the largest blob of the " + CStr(rects.Count) + " blobs"
    End Sub
End Class





Public Class Blob_LargestDepthCluster
    Inherits ocvbClass
    Dim blobs As Blob_DepthClusters
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        blobs = New Blob_DepthClusters(ocvb, caller)

        ocvb.desc = "Display only the largest depth cluster (might not be contiguous.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        blobs.Run(ocvb)
        Dim blobList = blobs.histBlobs.valleys.rangeBoundaries

        Dim maxSize = blobs.histBlobs.valleys.sortedSizes.ElementAt(0)
        ocvb.result1.SetTo(0)
        Dim startEndDepth = blobs.histBlobs.valleys.rangeBoundaries.ElementAt(0)
        Dim tmp As New cv.Mat, mask As New cv.Mat
        cv.Cv2.InRange(getDepth32f(ocvb), startEndDepth.X, startEndDepth.Y, tmp)
        cv.Cv2.ConvertScaleAbs(tmp, mask)
        ocvb.color.CopyTo(ocvb.result1, mask)
        ocvb.label1 = "Largest Depth Blob: " + Format(maxSize, "#,000") + " pixels (" + Format(maxSize / ocvb.color.Total, "#0.0%") + ")"
    End Sub
End Class
