Imports cv = OpenCvSharp
Public Class Reduction_Basics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Reduction factor", 0, 4000, 64)

        radio.Setup(ocvb, caller, 3)
        radio.check(0).Text = "Use bitwise reduction"
        radio.check(1).Text = "Use simple reduction"
        radio.check(2).Text = "No reduction"
        radio.check(1).Checked = True

        ocvb.desc = "Reduction: a simpler way to KMeans by reducing color resolution"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim reductionSlider = findSlider("Reduction factor")
        Dim reductionVal = reductionSlider.Value
        If radio.check(0).Checked Then
            Dim nearestPowerOf2 = Math.Round(Math.Log(reductionVal, 2)) ' Math.Pow(2, Math.Round(Math.Log(reductionVal) / Math.Log(2)))
            If nearestPowerOf2 = Double.NegativeInfinity Then nearestPowerOf2 = 0
            Dim power = Choose(nearestPowerOf2 + 1, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096) - 1
            Dim maskval = 256 - power
            If src.Type = cv.MatType.CV_32S Then maskval = Integer.MaxValue - power
            Dim tmp = New cv.Mat(src.Size, src.Type).SetTo(cv.Scalar.All(maskval))
            cv.Cv2.BitwiseAnd(src, tmp, dst1)
            label1 = "Reduced color image after zero'ing bit(s) 0x" + Hex(power)
        ElseIf radio.check(1).Checked Then
            If reductionVal = 0 Then reductionVal = 1
            dst1 = src / reductionVal
            dst1 *= reductionVal
            label1 = "Reduced image - factor = " + CStr(reductionVal)
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
        initParent(ocvb)

        edges = New Edges_Laplacian(ocvb)
        reduction = New Reduction_Basics(ocvb)
        reduction.radio.check(0).Checked = True

        ocvb.desc = "Get the edges after reducing the image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)
        dst1 = reduction.dst1.Clone

        Dim reductionRequested = False
        If reduction.radio.check(0).Checked Or reduction.radio.check(1).Checked Then reductionRequested = True
        label1 = If(reductionRequested, "Reduced image", "Original image")
        label2 = If(reductionRequested, "Laplacian edges of reduced image", "Laplacian edges of original image")
        edges.src = dst1
        edges.Run(ocvb)
        dst2 = edges.dst1
    End Sub
End Class




Public Class Reduction_Floodfill
    Inherits VBparent
    Public flood As FloodFill_Basics
    Public reduction As Reduction_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        flood = New FloodFill_Basics(ocvb)
        reduction = New Reduction_Basics(ocvb)
        ocvb.desc = "Use the reduction KMeans with floodfill to get masks and centroids of large masses."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)

        flood.src = reduction.dst1
        flood.Run(ocvb)

        dst1 = flood.dst2
        label1 = flood.label2
    End Sub
End Class






Public Class Reduction_KNN_Color
    Inherits VBparent
    Public reduction As Reduction_Floodfill
    Public pTrack As Kalman_PointTracker
    Dim highlight As Highlight_Basics
    Dim drawRC As Kalman_ViewObjects
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        drawRC = New Kalman_ViewObjects(ocvb)

        pTrack = New Kalman_PointTracker(ocvb)
        reduction = New Reduction_Floodfill(ocvb)
        If standalone Then highlight = New Highlight_Basics(ocvb)

        label2 = "Original floodfill color selections"
        ocvb.desc = "Use KNN with color reduction to consistently identify regions and color them."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        reduction.Run(ocvb)
        dst2 = reduction.dst1

        pTrack.queryPoints = New List(Of cv.Point2f)(reduction.flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(reduction.flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(reduction.flood.masks)
        pTrack.Run(ocvb)
        drawRC.src = pTrack.dst1
        drawRC.Run(ocvb)
        dst1 = drawRC.dst1

        If standalone Then
            highlight.viewObjects = pTrack.vwo.viewObjects
            highlight.src = dst1
            highlight.Run(ocvb)
            dst1 = highlight.dst1
        End If

        Static minSizeSlider = findSlider("FloodFill Minimum Size")
        label1 = "There were " + CStr(pTrack.vwo.viewObjects.Count) + " regions > " + CStr(minSizeSlider.value) + " pixels"
    End Sub
End Class







Public Class Reduction_KNN_ColorAndDepth
    Inherits VBparent
    Dim reduction As Reduction_KNN_Color
    Dim depth As Depth_Edges
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        depth = New Depth_Edges(ocvb)
        reduction = New Reduction_KNN_Color(ocvb)
        label1 = "Detecting objects using only color coherence"
        label2 = "Detecting objects with color and depth coherence"
        ocvb.desc = "Reduction_KNN finds objects with depth.  This algorithm uses only color on the remaining objects."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)
        dst1 = reduction.dst1

        depth.Run(ocvb)
        dst2 = depth.dst1
    End Sub
End Class






Public Class Reduction_Depth
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Dim colorizer As Depth_Colorizer_CPP
    Public reducedDepth32F As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        reduction = New Reduction_Basics(ocvb)
        reduction.radio.check(0).Checked = True
        colorizer = New Depth_Colorizer_CPP(ocvb)
        ocvb.desc = "Use reduction to smooth depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim input = src
        If input.Type = cv.MatType.CV_32S Then
            reduction.src = input
        Else
            input = getDepth32f(ocvb)
            input.ConvertTo(reduction.src, cv.MatType.CV_32S)
        End If
        reduction.Run(ocvb)
        reduction.dst1.ConvertTo(reducedDepth32F, cv.MatType.CV_32F)
        colorizer.src = reducedDepth32F
        colorizer.Run(ocvb)
        dst1 = colorizer.dst1
        label1 = reduction.label1
    End Sub
End Class





Public Class Reduction_PointCloud
    Inherits VBparent
    Dim reduction As Reduction_Basics
    Public newPointCloud As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        reduction = New Reduction_Basics(ocvb)
        reduction.radio.check(0).Checked = True
        ocvb.desc = "Use reduction to smooth depth data"
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







Public Class Reduction_Lines
    Inherits VBparent
    Dim sideView As Histogram_2D_SideView
    Dim topView As Histogram_2D_TopView
    Public lDetect As LineDetector_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sideView = New Histogram_2D_SideView(ocvb)
        topView = New Histogram_2D_TopView(ocvb)
        Dim reductionRadio = findRadio("No reduction")
        reductionRadio.Checked = True

        Dim histSlider = findSlider("Histogram threshold")
        histSlider.Value = 20

        lDetect = New LineDetector_Basics(ocvb)
        ocvb.desc = "Present both the top and side view to minimize pixel counts."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        sideView.Run(ocvb)
        dst1 = sideView.dst1
        lDetect.src = sideView.dst1.Resize(src.Size).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        lDetect.Run(ocvb)
        dst1 = lDetect.dst1.Clone

        topView.Run(ocvb)
        dst2 = topView.dst1
        lDetect.src = topView.dst1.Resize(src.Size).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        lDetect.Run(ocvb)
        dst2 = lDetect.dst1
    End Sub
End Class
