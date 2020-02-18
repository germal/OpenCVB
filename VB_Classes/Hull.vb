Imports cv = OpenCvSharp

Module Hull_module
    Public Function drawPoly(result As cv.Mat, polyPoints() As cv.Point, color As cv.Scalar) As List(Of cv.Point)
        Dim listOfPoints = New List(Of List(Of cv.Point))
        Dim points = New List(Of cv.Point)
        For j = 0 To polyPoints.Count - 1
            points.Add(New cv.Point(polyPoints(j).X, polyPoints(j).Y))
        Next
        listOfPoints.Add(points)
        cv.Cv2.DrawContours(result, listOfPoints, 0, color, 2)

        For i = 0 To polyPoints.Count - 1
            result.Circle(polyPoints(i), 5, color, -1, cv.LineTypes.AntiAlias)
        Next

        Return points
    End Function
End Module






Public Class Hull_Basics : Implements IDisposable
    Public sliders As New OptionsSliders
    Public hull() As cv.Point
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Hull random points", 1, 20, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Surround a set of random points with a convex hull"
        ocvb.label1 = "Convex Hull Output"
        ocvb.label2 = "Convex Hull Input"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim Count = sliders.TrackBar1.Value
        Dim points(Count - 1) As cv.Point
        Dim pad = 4
        Dim w = ocvb.color.Width - ocvb.color.Width / pad
        Dim h = ocvb.color.Height - ocvb.color.Height / pad
        For i = 0 To Count - 1
            points(i) = New cv.Point2f(ocvb.rng.uniform(ocvb.color.Width / pad, w), ocvb.rng.uniform(ocvb.color.Height / pad, h))
        Next
        hull = cv.Cv2.ConvexHull(points, True)

        If externalUse = False Then
            ocvb.result1.SetTo(0)
            ocvb.result2.SetTo(0)
            For i = 0 To hull.Count - 1
                cv.Cv2.Line(ocvb.result1, hull.ElementAt(i), hull.ElementAt((i + 1) Mod hull.Count), cv.Scalar.White, 2)
                cv.Cv2.Line(ocvb.result2, hull.ElementAt(i), hull.ElementAt((i + 1) Mod hull.Count), cv.Scalar.White, 2)
            Next

            Dim pMat As New cv.Mat(hull.Count, 1, cv.MatType.CV_32SC2, hull)
            Dim sum = pMat.Sum()
            Dim center = New cv.Point(CInt(sum.Val0 / hull.Count), CInt(sum.Val1 / hull.Count))
            Dim pixels = ocvb.result1.FloodFill(center, cv.Scalar.Yellow) ' because the shape is convex, we know the center is in the intere
            ocvb.result1.Circle(center, 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)

            For i = 0 To Count - 1
                ocvb.result1.Circle(points(i), 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                ocvb.result2.Circle(points(i), 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class
