Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Collections.Concurrent

Public Class Thread_Grid : Implements IDisposable
    Public roiList As List(Of cv.Rect)
    Public borderList As List(Of cv.Rect)
    Public sliders As New OptionsSliders
    Public gridMask As cv.Mat
    Public externalUse As Boolean
    Public tilesPerRow As Int32
    Public tilesPerCol As Int32
    Dim incompleteRegions As Int32
    Private Sub drawGrid(rList As List(Of cv.Rect))
        For Each roi In rList
            Dim p1 = New cv.Point(roi.X + roi.Width, roi.Y)
            Dim p2 = New cv.Point(roi.X + roi.Width, roi.Y + roi.Height)
            If roi.X + roi.Width <= gridMask.Width Then
                gridMask.Line(p1, p2, cv.Scalar.White, 1)
            End If
            If roi.Y + roi.Height <= gridMask.Height Then
                Dim p3 = New cv.Point(roi.X, roi.Y + roi.Height)
                gridMask.Line(p2, p3, cv.Scalar.White, 1)
            End If
        Next
    End Sub
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "ThreadGrid Width", 5, ocvb.color.Width, 32)
        sliders.setupTrackBar2(ocvb, "ThreadGrid Height", 5, ocvb.color.Height, 32)
        sliders.setupTrackBar3(ocvb, "ThreadGrid Border", 0, 20, 0)
        If ocvb.parms.ShowOptions Then sliders.Show()
        roiList = New List(Of cv.Rect)
        borderList = New List(Of cv.Rect)
        gridMask = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
        ocvb.desc = "Create a grid for use with parallel.ForEach."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static lastWidth As Int32
        Static lastHeight As Int32
        Static lastBorder As Int32

        Dim borderSize = sliders.TrackBar3.Value
        If lastWidth <> sliders.TrackBar1.Value Or lastHeight <> sliders.TrackBar2.Value Or lastBorder <> borderSize Then
            roiList.Clear()
            borderList.Clear()

            gridMask.SetTo(0)
            incompleteRegions = 0
            For y = 0 To ocvb.color.Height - 1 Step sliders.TrackBar2.Value
                For x = 0 To ocvb.color.Width - 1 Step sliders.TrackBar1.Value
                    Dim roi = New cv.Rect(x, y, sliders.TrackBar1.Value, sliders.TrackBar2.Value)
                    If x + roi.Width > ocvb.color.Width Then roi.Width = ocvb.color.Width - x
                    If y + roi.Height >= ocvb.color.Height Then roi.Height = ocvb.color.Height - y
                    If roi.Width > 0 And roi.Height > 0 Then
                        If y = 0 Then tilesPerRow += 1
                        If x = 0 Then tilesPerCol += 1
                        roiList.Add(roi)
                        If roi.Width <> sliders.TrackBar1.Value Or roi.Height <> sliders.TrackBar2.Value Then incompleteRegions += 1
                    End If
                Next
                drawGrid(roiList)
            Next

            For i = 0 To roiList.Count - 1
                Dim roi = roiList(i)
                Dim broi = New cv.Rect(roi.X - borderSize, roi.Y - borderSize, roi.Width + 2 * borderSize, roi.Height + 2 * borderSize)
                If broi.X < 0 Then
                    broi.Width += broi.X
                    broi.X = 0
                End If
                If broi.Y < 0 Then
                    broi.Height += broi.Y
                    broi.Y = 0
                End If
                If broi.Width + broi.X > ocvb.color.Width Then
                    broi.Width = ocvb.color.Width - broi.X
                End If
                If broi.Height + broi.Y > ocvb.color.Height Then
                    broi.Height = ocvb.color.Height - broi.Y
                End If
                borderList.Add(broi)
            Next

            If externalUse = False Then drawGrid(borderList)

            lastWidth = sliders.TrackBar1.Value
            lastHeight = sliders.TrackBar2.Value
            lastBorder = borderSize
        End If

        If externalUse = False Then
            ocvb.color.CopyTo(ocvb.result1)
            ocvb.result1.SetTo(cv.Scalar.All(255), gridMask)
        End If
        ocvb.label1 = "Thread_Grid " + CStr(roiList.Count - incompleteRegions) + " (" + CStr(tilesPerRow) + "X" + CStr(tilesPerCol) + ") " +
                      CStr(roiList(0).Width) + "X" + CStr(roiList(0).Height) + " regions"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        gridMask.Dispose()
    End Sub
End Class