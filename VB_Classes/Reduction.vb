Imports cv = OpenCvSharp
Public Class Reduction_Basics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Reduction factor", 1, 255, 64)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Use Reduction"

        label1 = "Reduced color image."
        desc = "Reduction: a simple way to get KMeans with much less work"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        dst1 = src / sliders.trackbar(0).Value ' can be any mat type...
        dst1 *= sliders.trackbar(0).Value
    End Sub
End Class







Public Class Reduction_Edges
    Inherits VBparent
    Dim edges As Edges_Laplacian
    Dim kReduce As Reduction_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        edges = New Edges_Laplacian(ocvb)
        kReduce = New Reduction_Basics(ocvb)
        label1 = "Reduced image"
        label2 = "Laplacian edges of reduced image"
        desc = "The simplest kmeans is to just reduce the resolution."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        kReduce.src = src
        kReduce.Run(ocvb)
        dst1 = kReduce.dst1.Clone

        edges.src = src
        edges.Run(ocvb)
        dst2 = edges.dst1
    End Sub
End Class




Public Class Reduction_Floodfill
    Inherits VBparent
    Public bflood As Floodfill_Identifiers
    Public kReduce As Reduction_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        bflood = New Floodfill_Identifiers(ocvb)
        kReduce = New Reduction_Basics(ocvb)
        desc = "Use the reduction KMeans with floodfill to get masks and centroids of large masses."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        kReduce.src = src
        kReduce.Run(ocvb)

        bflood.src = kReduce.dst1
        bflood.Run(ocvb)

        dst1 = bflood.dst2
    End Sub
End Class






Public Class Reduction_KNN
    Inherits VBparent
    Dim kReduce As Reduction_Basics
    Dim bflood As FloodFill_Black
    Dim pTrack As Kalman_PointTracker
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        bflood = New FloodFill_Black(ocvb)
        kReduce = New Reduction_Basics(ocvb)

        pTrack = New Kalman_PointTracker(ocvb)
        desc = "Use KNN with reduction to consistently identify regions and color them."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        kReduce.src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        kReduce.Run(ocvb)

        bflood.src = kReduce.dst1
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
    End Sub
End Class





Public Class Reduction_PointCloud
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Dim newPointCloud As New cv.Mat
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
    End Sub
End Class