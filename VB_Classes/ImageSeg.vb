Imports cv = OpenCvSharp
Public Class ImageSeg_Basics
    Inherits VBparent
    Dim addw As AddWeighted_Basics

    Public maskSizes As New SortedList(Of Integer, Integer)(New CompareMaskSize)
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public centroids As New List(Of cv.Point2f)
    Public points As New List(Of cv.Point)

    Public flood As FloodFill_FullImage
    Public Sub New()
        initParent()
        addw = New AddWeighted_Basics
        flood = New FloodFill_FullImage
        task.desc = "Get the image segments and their associated features - centroids, masks, size, and enclosing rectangles"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        flood.src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        flood.Run()
        dst1 = flood.dst2

        maskSizes = New SortedList(Of Integer, Integer)(flood.maskSizes)
        rects = New List(Of cv.Rect)(flood.rects)
        masks = New List(Of cv.Mat)(flood.masks)
        centroids = New List(Of cv.Point2f)(flood.centroids)
        points = New List(Of cv.Point)(flood.points)

        addw.src = dst1
        addw.src2 = src
        addw.Run()
        dst2 = addw.dst1
        label2 = addw.label1.Replace("depth", "ImageSeg")
    End Sub
End Class







Public Class ImageSeg_InRange
    Inherits VBparent
    Dim iSeg As ImageSeg_Basics
    Public Sub New()
        initParent()
        iSeg = New ImageSeg_Basics
        task.desc = "Trim segments that are not in the range requested"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        iSeg.src = src
        iSeg.Run()
        dst1 = iSeg.dst2
        dst1.SetTo(0, task.inrange.noDepthMask)

        'For i = 0 To iSeg.maskSizes.Count - 1
        '    Dim mask = iSeg.masks(i)
        '    Dim r = iSeg.rects(i)
        '    Dim meanDepth = task.depth32f(r).Mean(mask)
        '    If meanDepth.Val0 >= task.inrange.maxval Then dst1(r).SetTo(0, mask)
        '    If meanDepth.Val0 <= task.inrange.minval Then dst1(r).SetTo(0, mask)
        'Next
    End Sub
End Class