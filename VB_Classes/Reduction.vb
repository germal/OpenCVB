Imports cv = OpenCvSharp
Public Class Reduction_Basics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Reduction factor", 0, 12, 6)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Use Reduction"
        check.Box(0).Checked = True

        desc = "Reduction: a simpler way to KMeans by removing low-order bits"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If check.Box(0).Checked Then
            Dim power = Choose(sliders.trackbar(0).Value + 1, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096) - 1
            Dim maskval = 256 - power
            If src.Type = cv.MatType.CV_32S Then maskval = Integer.MaxValue - power
            If src.Type = cv.MatType.CV_8U Or src.Type = cv.MatType.CV_8UC3 And maskval < 2 Then
                Console.WriteLine("Reduction_Basics: the limit of the reduction factor for 8-bit images is 7 or fewer and it is set to 8!")
            End If
            Dim tmp = New cv.Mat(src.Size, src.Type).SetTo(cv.Scalar.All(maskval))
            cv.Cv2.BitwiseAnd(src, tmp, dst1)
            label1 = "Reduced color image after zero'ing bit(s) 0x" + Hex(power)
        Else
            dst1 = src
            label1 = "No reduction requested"
        End If
    End Sub
End Class






Public Class Reduction_Simple
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Simple reduction factor", 1, 4000, 64)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Use Simple Reduction"
        check.Box(0).Checked = True

        desc = "Reduction: a simple way to get KMeans"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If check.Box(0).Checked Then
            dst1 = src / sliders.trackbar(0).Value ' can be any mat type...
            dst1 *= sliders.trackbar(0).Value
            label1 = "Reduced image - factor = " + CStr(sliders.trackbar(0).Value)
        Else
            dst1 = src
            label1 = "No reduction requested"
        End If
    End Sub
End Class







Public Class Reduction_Edges
    Inherits VBparent
    Dim edges As Edges_Laplacian
    Dim reduction As Reduction_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        edges = New Edges_Laplacian(ocvb)
        reduction = New Reduction_Basics(ocvb)
        label1 = "Reduced image"
        label2 = "Laplacian edges of reduced image"
        desc = "Get the edges after reducing the image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)
        dst1 = reduction.dst1.Clone

        edges.src = src
        edges.Run(ocvb)
        dst2 = edges.dst1
    End Sub
End Class




Public Class Reduction_Floodfill
    Inherits VBparent
    Public bflood As Floodfill_Identifiers
    Public reduction As Reduction_Simple
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        bflood = New Floodfill_Identifiers(ocvb)
        reduction = New Reduction_Simple(ocvb)
        desc = "Use the reduction KMeans with floodfill to get masks and centroids of large masses."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)

        bflood.src = reduction.dst1
        bflood.Run(ocvb)

        dst1 = bflood.dst2
        label1 = reduction.label1
    End Sub
End Class






Public Class Reduction_KNN
    Inherits VBparent
    Public reduction As Reduction_Simple
    Public bflood As FloodFill_Black
    Public pTrack As Kalman_PointTracker
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        bflood = New FloodFill_Black(ocvb)
        pTrack = New Kalman_PointTracker(ocvb)
        reduction = New Reduction_Simple(ocvb)

        label2 = "Original floodfill color selections"
        desc = "Use KNN with reduction to consistently identify regions and color them."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        reduction.Run(ocvb)

        bflood.src = reduction.dst1
        bflood.Run(ocvb)
        dst2 = bflood.dst2

        pTrack.queryPoints = New List(Of cv.Point2f)(bflood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(bflood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(bflood.masks)
        pTrack.Run(ocvb)
        dst1 = pTrack.dst1

        Dim vw = pTrack.viewObjects
        For i = 0 To vw.Count - 1
            dst1.Circle(vw.Values(i).centroid, 6, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            dst1.Circle(vw.Values(i).centroid, 4, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
        label1 = reduction.label1
    End Sub
End Class






Public Class Reduction_KNN_Clickable
    Inherits VBparent
    Dim reduction As Reduction_KNN
    Dim highlightPoint As New cv.Point
    Dim highlightRect As New cv.Rect
    Dim highlightMask As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        reduction = New Reduction_KNN(ocvb)

        label1 = "Click near any centroid to highlight the object"
        desc = "Highlight individual rectangles and centroids in Reduction_KNN - Tracker Algorithm"
    End Sub
    Private Sub setPoint(pt As cv.Point, vw As SortedList(Of Integer, viewObject))
        Dim index = findNearestPoint(pt, vw)
        highlightPoint = vw.ElementAt(index).Value.centroid
        highlightRect = vw.ElementAt(index).Value.rectView
        highlightmask = vw.ElementAt(index).Value.mask
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)
        dst1 = reduction.dst1
        Dim vw = reduction.pTrack.viewObjects
        If ocvb.mouseClickFlag Then
            setPoint(ocvb.mouseClickPoint, vw)
            ocvb.mouseClickFlag = False ' absorb the mouse click here only
        End If
        If highlightRect.Width > 0 Then
            setPoint(highlightPoint, vw)
            dst1.Circle(highlightPoint, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            dst1.Rectangle(highlightRect, cv.Scalar.Red, 2)
            Dim rect = New cv.Rect(0, 0, highlightMask.Width, highlightMask.Height)
            dst2.SetTo(0)
            dst2(rect).SetTo(cv.Scalar.Yellow, highlightMask)
        End If
    End Sub
End Class






Public Class Reduction_Depth
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        reduction = New Reduction_Basics(ocvb)
        colorizer = New Depth_Colorizer_CPP(ocvb)
        desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Type = cv.MatType.CV_32S Then
            reduction.src = src
        Else
            src = getDepth32f(ocvb)
            src.ConvertTo(reduction.src, cv.MatType.CV_32S)
        End If
        reduction.Run(ocvb)
        reduction.dst1.ConvertTo(dst1, cv.MatType.CV_32F)
        colorizer.src = dst1
        colorizer.Run(ocvb)
        dst2 = colorizer.dst1
        label1 = reduction.label1
    End Sub
End Class





Public Class Reduction_PointCloud
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Public newPointCloud As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        reduction = New Reduction_Basics(ocvb)
        desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim split() = ocvb.pointCloud.Split()
        split(2) *= 1000 ' convert to mm's
        split(2).ConvertTo(reduction.src, cv.MatType.CV_32S)
        reduction.Run(ocvb)
        reduction.dst1.ConvertTo(dst2, cv.MatType.CV_32F)
        dst1 = dst2.Resize(ocvb.pointCloud.Size)
        split(2) = dst1 / 1000
        cv.Cv2.Merge(split, newPointCloud)
        dst1 = dst1.ConvertScaleAbs(255).CvtColor(cv.ColorConversionCodes.GRAY2BGR).Resize(src.Size)
    End Sub
End Class