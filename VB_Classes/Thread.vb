Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Collections.Concurrent

Public Class Thread_Grid
    Inherits ocvbClass
    Public roiList As List(Of cv.Rect)
    Public borderList As List(Of cv.Rect)
        Public gridMask As cv.Mat
        Public tilesPerRow As Int32
    Public tilesPerCol As Int32
    Dim incompleteRegions As Int32
    Public src As New cv.Mat
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        src = ocvb.color
        sliders.setupTrackBar1(ocvb, caller, "ThreadGrid Width", 5, src.Width, 32)
        sliders.setupTrackBar2(ocvb, caller, "ThreadGrid Height", 5, src.Height, 32)
        sliders.setupTrackBar3(ocvb, caller, "ThreadGrid Border", 0, 20, 0)
        roiList = New List(Of cv.Rect)
        borderList = New List(Of cv.Rect)
        gridMask = New cv.Mat(src.Size(), cv.MatType.CV_8UC1)
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
            For y = 0 To src.Height - 1 Step sliders.TrackBar2.Value
                For x = 0 To src.Width - 1 Step sliders.TrackBar1.Value
                    Dim roi = New cv.Rect(x, y, sliders.TrackBar1.Value, sliders.TrackBar2.Value)
                    If x + roi.Width > src.Width Then roi.Width = src.Width - x
                    If y + roi.Height >= src.Height Then roi.Height = src.Height - y
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
                If broi.Width + broi.X > src.Width Then
                    broi.Width = src.Width - broi.X
                End If
                If broi.Height + broi.Y > src.Height Then
                    broi.Height = src.Height - broi.Y
                End If
                borderList.Add(broi)
            Next

            if standalone Then drawGrid(borderList)

            lastWidth = sliders.TrackBar1.Value
            lastHeight = sliders.TrackBar2.Value
            lastBorder = borderSize
        End If

        if standalone Then
            src.CopyTo(ocvb.result1)
            ocvb.result1.SetTo(cv.Scalar.All(255), gridMask)
            ocvb.label1 = "Thread_Grid " + CStr(roiList.Count - incompleteRegions) + " (" + CStr(tilesPerRow) + "X" + CStr(tilesPerCol) + ") " +
                          CStr(roiList(0).Width) + "X" + CStr(roiList(0).Height) + " regions"
        End If
    End Sub
    Public Sub MyDispose()
                gridMask.Dispose()
    End Sub
End Class