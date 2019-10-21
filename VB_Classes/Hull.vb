Imports cv = OpenCvSharp
Public Class Hull_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Surround a set of random points with a convex hull"
        ocvb.label1 = "Convex Hull Output"
        ocvb.label2 = "Convex Hull Input"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim Count = 10
        Dim points(Count - 1) As cv.Point
        For i = 0 To Count - 1
            points(i) = New cv.Point2f(ocvb.rng.uniform(0, ocvb.color.Width), ocvb.rng.uniform(0, ocvb.color.Height))
        Next
        Dim pMat As New cv.Mat(Count, 2, cv.MatType.CV_32S, points)
        Dim hull As New cv.Mat
        cv.Cv2.ConvexHull(pMat, hull, True)

        ocvb.result1.SetTo(0)
        ocvb.result2.SetTo(0)
        For i = 0 To hull.Rows - 1
            cv.Cv2.Line(ocvb.result1, hull.Get(Of cv.Point)(i), hull.Get(Of cv.Point)((i + 1) Mod hull.Rows), cv.Scalar.White, 2)
            cv.Cv2.Line(ocvb.result2, hull.Get(Of cv.Point)(i), hull.Get(Of cv.Point)((i + 1) Mod hull.Rows), cv.Scalar.White, 2)
        Next

        Dim sum = hull.Sum()
        Dim center = New cv.Point(CInt(sum.Val0 / hull.Rows), CInt(sum.Val1 / hull.Rows))
        Dim pixels = ocvb.result1.FloodFill(center, cv.Scalar.Yellow) ' because the shape is convex, we know the center is in the intere
        ocvb.result1.Circle(center, 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)

        For i = 0 To Count - 1
            ocvb.result1.Circle(points(i), 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
            ocvb.result2.Circle(points(i), 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
