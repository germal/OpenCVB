﻿Imports cv = OpenCvSharp
Public Class Polylines_IEnumerableExample : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Manually create an ienumerable(of ienumerable(of cv.point))."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim pts As New List(Of List(Of cv.Point))
        Dim points As New List(Of cv.Point)
        Dim autoRand As New Random()
        For i = 0 To 250 - 1
            points.Add(New cv.Point(CInt((autoRand.NextDouble()) * ocvb.color.Width), CInt((autoRand.NextDouble()) * ocvb.color.Height)))
        Next
        pts.Add(points)

        ocvb.result1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U, 0)
        ocvb.result1.Polylines(pts, False, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class





' VB.Net implementation of the browse example in OpenCV. 
' https://github.com/opencv/opencv/blob/master/samples/python/browse.py
Public Class Polylines_Random : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label2 = "4X zoom around mouse"
        ocvb.desc = "Create a random procedural image - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 150 = 0 Then ' every x frames.
            Dim autoRand As New Random()
            Dim h = ocvb.color.Height, w = ocvb.color.Width
            Dim points2f(10000) As cv.Point2f
            Dim pts As New List(Of List(Of cv.Point))
            Dim points As New List(Of cv.Point)
            points2f(0) = New cv.Point(CInt((autoRand.NextDouble() - 0.5)), CInt((autoRand.NextDouble() - 0.5)))
            For i = 1 To points2f.Length - 1
                points2f(i) = New cv.Point2f(autoRand.NextDouble() - 0.5 + points2f(i - 1).X, autoRand.NextDouble() - 0.5 + points2f(i - 1).Y)
            Next
            For i = 0 To points2f.Length - 1
                points.Add(New cv.Point(CInt(points2f(i).X * 10 + w / 2), CInt(points2f(i).Y * 10 + h / 2)))
            Next
            pts.Add(points)

            ocvb.result1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U, 0)
            ocvb.result1.Polylines(pts, False, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        End If

        Dim width = ocvb.result1.Width / 4
        Dim height = ocvb.result1.Height / 4
        Dim x = Math.Min(Math.Max(ocvb.mousePoint.X - width / 2, 0), ocvb.result1.Width - width)
        Dim y = Math.Min(Math.Max(ocvb.mousePoint.Y - height / 2, 0), ocvb.result1.Height - height)
        Dim rect As New cv.Rect(x, y, width, height)
        ocvb.result2 = ocvb.result1(rect).Resize(ocvb.result1.Size())
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class