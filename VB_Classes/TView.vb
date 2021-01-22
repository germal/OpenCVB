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
    Public flood As FloodFill_Basics
    Public tView As TView_Basics
    Public Sub New()
        initParent()

        flood = New FloodFill_Basics
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

        flood.src = dst1
        flood.Run()
        dst1 = flood.dst1.Clone

        flood.src = dst2
        flood.Run()
        dst2 = flood.dst1
    End Sub
End Class