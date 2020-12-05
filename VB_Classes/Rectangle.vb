Imports cv = OpenCvSharp
Public Class Rectangle_Basics
    Inherits VBparent
    Public updateFrequency = 30
    Public rectangles As New List(Of cv.Rect)
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Rectangle Count", 1, 255, 3)
        End If
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Draw Rotated Rectangles"
        End If

        task.desc = "Draw the requested number of rotated rectangles."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount Mod updateFrequency = 0 Then
            dst1.SetTo(cv.Scalar.Black)
            rectangles.Clear()
            For i = 0 To sliders.trackbar(0).Value - 1
                Dim nPoint = New cv.Point2f(msRNG.Next(src.Cols / 4, src.Cols * 3 / 4), msRNG.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim width = msRNG.Next(0, src.Cols - nPoint.X - 1)
                Dim height = msRNG.Next(0, src.Rows - nPoint.Y - 1)
                Dim eSize = New cv.Size2f(CSng(msRNG.Next(0, src.Cols - nPoint.X - 1)), CSng(msRNG.Next(0, src.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(msRNG.Next(0, 1000) / 1000.0F)
                Dim rotatedRect = New cv.RotatedRect(nPoint, eSize, angle)

                Dim nextColor = New cv.Scalar(ocvb.vecColors(i).Item0, ocvb.vecColors(i).Item1, ocvb.vecColors(i).Item2)
                If check.Box(0).Checked Then
                    drawRotatedRectangle(rotatedRect, dst1, nextColor)
                Else
                    Dim r = New cv.Rect(nPoint.X, nPoint.Y, width, height)
                    cv.Cv2.Rectangle(dst1, r, nextColor, -1)
                    rectangles.Add(r)
                End If
            Next
        End If
    End Sub
End Class




Public Class Rectangle_Rotated
    Inherits VBparent
    Public rect As Rectangle_Basics
    Public Sub New()
        initParent()
        rect = New Rectangle_Basics
        Dim rotatedCheck = findCheckBox("Draw Rotated Rectangles")
        rotatedCheck.Checked = True
        task.desc = "Draw the requested number of rectangles."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        rect.src = src
        rect.Run()
        dst1 = rect.dst1
    End Sub
End Class







Public Class Rectangle_Overlap
    Inherits VBparent
    Public rect1 As cv.Rect
    Public rect2 As cv.Rect
    Dim draw As Rectangle_Basics
    Public Sub New()
        initParent()

        draw = New Rectangle_Basics
        Dim countSlider = findSlider("Rectangle Count")
        countSlider.Value = 2
        Dim typeCheckBox = findCheckBox("Draw Rotated Rectangles")
        typeCheckBox.Checked = False

        task.desc = "Test if 2 rectangles overlap"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone Then
            draw.Run()
            dst1 = draw.dst1
        End If
        rect1 = draw.rectangles(0)
        rect2 = draw.rectangles(1)
        dst2.SetTo(0)
        dst2.Rectangle(rect1, cv.Scalar.Yellow, 2)
        dst2.Rectangle(rect2, cv.Scalar.Yellow, 2)
        If rect1.IntersectsWith(rect2) Then
            dst2.Rectangle(rect1.Union(rect2), cv.Scalar.White, 4)
            label2 = "Rectangles intersect"
        Else
            label2 = "Rectangles don't intersect"
        End If

        Dim test As cv.RotatedRect
    End Sub
End Class








'Public Class Rectangles_CComp
'    Inherits VBparent
'    Dim ccomp As CComp_Basics
'    Dim overlap As Draw_OverlappingRectangles
'    Public Sub New()
'        initParent()

'        ccomp = New CComp_Basics()
'        ccomp.sliders.trackbar(1).Value = 10 ' allow very small regions.

'        overlap = New Draw_OverlappingRectangles()

'        label1 = "Input Image with all ccomp rectangles"
'        label2 = "Unique rectangles (largest to smallest) colored by size"
'        task.desc = "Define unique regions in the RGB image by eliminating overlapping rectangles."
'    End Sub
'    Public Sub Run()
'        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
'        ccomp.src = src
'        ccomp.Run()
'        dst1 = ccomp.dst2

'        overlap.inputRects = ccomp.rects
'        overlap.Run()

'        dst2.SetTo(0)
'        'For i = 0 To overlap.sortedMasks.Count - 1
'        '    Dim mask = overlap.sortedMasks.ElementAt(overlap.sortedMasks.Count - i - 1).Value
'        '    Dim rect = overlap.sortedMasks.ElementAt(overlap.sortedMasks.Count - i - 1).Key
'        '    dst2(rect).SetTo(ocvb.scalarColors(i), mask)
'        '    dst2.Rectangle(rect, cv.Scalar.White, 2)
'        'Next
'    End Sub
'End Class