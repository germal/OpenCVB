Imports cv = OpenCvSharp
Public Class Highlight_Basics
    Inherits VBparent
    Dim reduction As Reduction_KNN_Color
    Public highlightPoint As New cv.Point
    Dim highlightRect As New cv.Rect
    Dim preKalmanRect As New cv.Rect
    Dim highlightMask As New cv.Mat
    Public viewObjects As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        If standalone Then reduction = New Reduction_KNN_Color(ocvb)
        ocvb.desc = "Highlight the rectangle and centroid nearest the mouse click"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If standalone Then
            reduction.src = src
            reduction.Run(ocvb)
            viewObjects = reduction.pTrack.viewObjects
            src = reduction.dst1
        End If

        dst1 = src
        If ocvb.mouseClickFlag Then
            highlightPoint = ocvb.mouseClickPoint
            ocvb.mouseClickFlag = False ' absorb the mouse click here only
        End If
        If highlightPoint <> New cv.Point And viewObjects.Count > 0 Then
            Dim index = findNearestPoint(highlightPoint, viewObjects)
            highlightPoint = viewObjects.ElementAt(index).Value.centroid
            highlightRect = viewObjects.ElementAt(index).Value.rectView
            highlightMask = New cv.Mat
            highlightMask = viewObjects.ElementAt(index).Value.mask
            preKalmanRect = viewObjects.ElementAt(index).Value.preKalmanRect

            dst1.Circle(highlightPoint, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            dst1.Rectangle(highlightRect, cv.Scalar.Red, 2)
            Dim rect = New cv.Rect(0, 0, highlightMask.Width, highlightMask.Height)
            ocvb.color.CopyTo(dst2)
            dst2(preKalmanRect).SetTo(cv.Scalar.Yellow, highlightMask)
            label2 = "Highlighting the selected region."
        End If
    End Sub
End Class








Public Class Highlight_Contour
    Inherits VBparent
    Dim tracker As Fuzzy_Tracker
    Public highlightPoint As New cv.Point
    Dim highlightRect As New cv.Rect
    Dim lower As Integer, upper As Integer
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        tracker = New Fuzzy_Tracker(ocvb)
        ocvb.desc = "Isolate a contour and analyze characteristics"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        tracker.src = src
        tracker.Run(ocvb)
        dst1 = tracker.dst1
        label1 = tracker.label1

        If (lower <> 0 Or upper <> 0) And ocvb.mouseClickFlag = False Then
            dst2 = tracker.fuzzy.fuzzyD.basics.gray
            Dim mask = dst2(highlightRect).InRange(lower, upper)
            dst2(highlightRect).SetTo(255, mask)
        Else
            If ocvb.mouseClickFlag Then
                highlightPoint = ocvb.mouseClickPoint
                ocvb.mouseClickFlag = False ' absorb the mouse click here only
                'End If
                'If highlightPoint <> New cv.Point And tracker.pTrack.viewObjects.Count > 0 Then
                Dim vo = tracker.pTrack.viewObjects
                Dim index = findNearestPoint(highlightPoint, vo)
                highlightPoint = vo.ElementAt(index).Value.centroid
                highlightRect = vo.ElementAt(index).Value.rectView
                dst2 = tracker.fuzzy.fuzzyD.basics.gray

                lower = vo.ElementAt(index).Value.LayoutColor
                upper = cv.Scalar.All(vo.ElementAt(index).Value.LayoutColor.Item(0) + 1)
                Dim mask = dst2(highlightRect).InRange(lower, upper)

                Dim preKalmanRect = vo.ElementAt(index).Value.preKalmanRect

                dst2(highlightRect).SetTo(255, mask)
                dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                dst2.Circle(highlightPoint, 6, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                dst2.Circle(highlightPoint, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                dst2.Rectangle(highlightRect, cv.Scalar.Yellow, 2)

                Dim contourMat = vo.ElementAt(index).Value.contourMat
                For i = 0 To contourMat.Rows - 1
                    Dim pt1 = contourMat.Get(Of cv.Point)(i, 0)
                    dst2.Circle(pt1, 1, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                Next

                label2 = "Highlighting the selected region."
            End If
        End If
        If highlightPoint = New cv.Point Then ocvb.trueText("Click on any centroid to see details", 10, 50, 3)
    End Sub
End Class