Imports cv = OpenCvSharp
Imports System.Collections.Generic
Imports System.Linq
Public Class Polylines_IEnumerableExample : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        check.Setup(ocvb, 2)
        check.Box(0).Text = "Polyline closed if checked"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()
        sliders.setupTrackBar1(ocvb, "Polyline Count", 2, 500, 100)
        sliders.setupTrackBar2(ocvb, "Polyline Thickness", 0, 10, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Manually create an ienumerable(of ienumerable(of cv.point))."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim points = Enumerable.Range(0, sliders.TrackBar1.Value).Select(Of cv.Point)(
            Function(i)
                Return New cv.Point(CInt(ocvb.ms_rng.Next(0, ocvb.color.Width)), CInt(ocvb.ms_rng.Next(0, ocvb.color.Height)))
            End Function).ToList
        Dim pts As New List(Of List(Of cv.Point))
        pts.Add(points)

        ocvb.result1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U, 0)
        ' NOTE: when there are 2 points, there will be 1 line.
        ocvb.result1.Polylines(pts, check.Box(0).Checked, cv.Scalar.White, sliders.TrackBar2.Value, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        check.Dispose()
    End Sub
End Class





' VB.Net implementation of the browse example in OpenCV. 
' https://github.com/opencv/opencv/blob/master/samples/python/browse.py
Public Class Polylines_Random : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
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

            ocvb.result1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U, 0)
            ocvb.result1.Polylines(pts, False, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            ocvb.result1 = ocvb.result1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If

        Dim zoomFactor = 4
        Dim width = ocvb.result1.Width / zoomFactor
        Dim height = ocvb.result1.Height / zoomFactor
        Dim x = Math.Min(ocvb.mousePoint.X, ocvb.result1.Width - width)
        Dim y = Math.Min(ocvb.mousePoint.Y, ocvb.result1.Height - height)
        ocvb.label2 = CStr(zoomFactor) + "X zoom around mouse"
        ocvb.result2 = ocvb.result1.GetRectSubPix(New cv.Size(width, height), New cv.Point2f(x, y))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class