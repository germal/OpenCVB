Imports cv = OpenCvSharp
Public Class TView_Basics
    Inherits VBparent
    Public sideView As Histogram_SideView2D
    Public topView As Histogram_TopView2D
    Dim hist As Histogram_Basics
    Public Sub New()
        initParent()

        hist = New Histogram_Basics
        sideView = New Histogram_SideView2D
        topView = New Histogram_TopView2D

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Show counts > X", 0, 300, 10)
        End If
        task.desc = "Triple View that highlights concentrations of depth pixels"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static countSlider = findSlider("Show counts > X")

        sideView.Run()

        Dim sideOrig = sideView.originalHistOutput.CountNonZero()
        dst2 = sideView.originalHistOutput.Threshold(countSlider.value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)

        topView.Run()

        dst1 = topView.originalHistOutput.Threshold(countSlider.value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)

        label1 = "TopView showing all histogram entries > " + CStr(countSlider.value)
        label2 = "SideView showing all histogram entries > " + CStr(countSlider.value)
    End Sub
End Class











Public Class TView_FloodFill
    Inherits VBparent
    Public floodSide As FloodFill_Basics
    Public floodTop As FloodFill_Basics
    Public tView As TView_Basics
    Public Sub New()
        initParent()

        floodSide = New FloodFill_Basics
        floodTop = New FloodFill_Basics
        Dim minFloodSlider = findSlider("FloodFill Minimum Size")
        minFloodSlider.Value = 100
        tView = New TView_Basics

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Fuse X frames", 1, 50, 10)
        End If

        task.desc = "FloodFill the histograms of side and top views - TView_Basics"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        tView.Run()
        dst1 = tView.dst1.Clone
        dst2 = tView.dst2.Clone

        Static fuseSlider = findSlider("Fuse X frames")
        Static saveFuseCount = -1
        Static fuseSide As New List(Of cv.Mat)
        Static fuseTop As New List(Of cv.Mat)
        Dim fuseCount = fuseSlider.value
        If saveFuseCount <> fuseSlider.value Then
            fuseSide.Clear()
            fuseTop.Clear()
            saveFuseCount = fuseSlider.value
        End If
        If fuseSide.Count > fuseCount Then fuseSide.RemoveAt(0)
        If fuseTop.Count > fuseCount Then fuseTop.RemoveAt(0)
        For i = 0 To fuseSide.Count - 1
            cv.Cv2.Max(fuseSide(i), dst1, dst1)
            cv.Cv2.Max(fuseTop(i), dst2, dst2)
        Next
        fuseSide.Add(tView.dst1.Clone)
        fuseTop.Add(tView.dst2.Clone)

        floodTop.src = dst1
        floodTop.Run()
        dst1 = floodTop.dst1

        floodSide.src = dst2
        floodSide.Run()
        dst2 = floodSide.dst1
    End Sub
End Class








Public Class TView_Centroids
    Inherits VBparent
    Public knn As KNN_Basics
    Dim tview As TView_FloodFill
    Public queryPoints As New List(Of cv.Point2f)
    Public responses As New List(Of cv.Point2f)
    Public Sub New()
        initParent()
        tview = New TView_FloodFill
        knn = New KNN_Basics

        label1 = "Top view with centroids in yellow"
        label2 = "Side view with centroids in yellow"
        task.desc = "Use KNN to track the query points"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        tview.Run()
        dst1 = tview.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = tview.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        For i = 0 To tview.floodTop.centroids.Count - 1
            dst1.Circle(tview.floodTop.centroids(i), ocvb.dotSize, cv.Scalar.Yellow, -1)
        Next
        For i = 0 To tview.floodSide.centroids.Count - 1
            dst2.Circle(tview.floodSide.centroids(i), ocvb.dotSize, cv.Scalar.Yellow, -1)
        Next

        Static saveTopQueries = New List(Of cv.Point2f)(tview.floodTop.centroids)
        knn.knnQT.trainingPoints = saveTopQueries
        knn.knnQT.queryPoints = New List(Of cv.Point2f)(tview.floodTop.centroids)
        knn.Run()
        For i = 0 To knn.neighbors.Rows - 1
            Dim qPoint = tview.floodTop.centroids(i)
            cv.Cv2.Circle(dst1, qPoint, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias, 0)
            Dim pt = saveTopQueries(knn.neighbors.Get(Of Single)(i, 0))
            Dim cpt = New cv.Point(CInt(pt.x), CInt(pt.y))
            dst1.Line(cpt, qPoint, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
        Next

        saveTopQueries = New List(Of cv.Point2f)(tview.floodTop.centroids)
    End Sub
End Class








Public Class TView_Rectangles
    Inherits VBparent
    Dim mOverLap As Rectangle_MultiOverlap
    Dim tview As TView_FloodFill
    Public Sub New()
        initParent()

        mOverLap = New Rectangle_MultiOverlap
        tview = New TView_FloodFill

        label1 = "Top view with rectangles in yellow"
        label2 = "Side view with rectangles in yellow"
        task.desc = "Use KNN to track the query points"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        tview.Run()
        dst1 = tview.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = tview.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        mOverLap.inputRects = New List(Of cv.Rect)(tview.floodTop.rects)
        mOverLap.Run()
        For i = 0 To mOverLap.outputRects.Count - 1
            dst1.Rectangle(mOverLap.outputRects(i), cv.Scalar.Yellow, 1)
        Next

        mOverLap.inputRects = New List(Of cv.Rect)(tview.floodSide.rects)
        mOverLap.Run()
        For i = 0 To mOverLap.outputRects.Count - 1
            dst2.Rectangle(mOverLap.outputRects(i), cv.Scalar.Yellow, 1)
        Next
    End Sub
End Class