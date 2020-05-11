Imports cv = OpenCvSharp
Imports System.Collections.Generic
Imports System.Linq
Public Class Polylines_IEnumerableExample
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Polyline closed if checked"
        check.Box(0).Checked = True
        sliders.setupTrackBar1(ocvb, caller, "Polyline Count", 2, 500, 100)
        sliders.setupTrackBar2(ocvb, caller, "Polyline Thickness", 0, 10, 1)
        ocvb.desc = "Manually create an ienumerable(of ienumerable(of cv.point))."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim points = Enumerable.Range(0, sliders.TrackBar1.Value).Select(Of cv.Point)(
            Function(i)
                Return New cv.Point(CInt(ocvb.ms_rng.Next(0, ocvb.color.Width)), CInt(ocvb.ms_rng.Next(0, ocvb.color.Height)))
            End Function).ToList
        Dim pts As New List(Of List(Of cv.Point))
        pts.Add(points)

        dst1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U, 0)
        ' NOTE: when there are 2 points, there will be 1 line.
        dst1.Polylines(pts, check.Box(0).Checked, cv.Scalar.White, sliders.TrackBar2.Value, cv.LineTypes.AntiAlias)
    End Sub
End Class





' VB.Net implementation of the browse example in OpenCV. 
' https://github.com/opencv/opencv/blob/master/samples/python/browse.py
Public Class Polylines_Random
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Create a random procedural image - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 150 = 0 Then ' every x frames.
            Dim h = ocvb.color.Height, w = ocvb.color.Width
            Dim autorand As New Random
            Dim points2f(10000) As cv.Point2f
            Dim pts As New List(Of List(Of cv.Point))
            Dim points As New List(Of cv.Point)
            points2f(0) = New cv.Point2f(autorand.NextDouble() - 0.5, autorand.NextDouble() - 0.5)
            For i = 1 To points2f.Length - 1
                points2f(i) = New cv.Point2f(autorand.NextDouble() - 0.5 + points2f(i - 1).X, autorand.NextDouble() - 0.5 + points2f(i - 1).Y)
                points.Add(New cv.Point(CInt(points2f(i).X * 10 + w / 2), CInt(points2f(i).Y * 10 + h / 2)))
            Next
            pts.Add(points)

            dst1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U, 0)
            dst1.Polylines(pts, False, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            dst1 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If

        Dim zoomFactor = 4
        Dim width = dst1.Width / zoomFactor
        Dim height = dst1.Height / zoomFactor
        Dim x = Math.Min(ocvb.mousePoint.X, dst1.Width - width)
        Dim y = Math.Min(ocvb.mousePoint.Y, dst1.Height - height)
        ocvb.label2 = CStr(zoomFactor) + "X zoom around mouse"
        dst2 = dst1.GetRectSubPix(New cv.Size(width, height), New cv.Point2f(x, y))
    End Sub
End Class
