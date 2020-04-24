Imports cv = OpenCvSharp
Imports System.Threading

Module Puzzle_Solvers
    Public Class CompareCorrelations : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
End Module

' https://github.com/nemanja-m/gaps
Public Class Puzzle_Basics : Implements IDisposable
    Public grid As Thread_Grid
    Public scrambled As New List(Of cv.Rect) ' this is every roi regardless of size. 
    Public unscrambled As New List(Of cv.Rect) ' this is every roi regardless of size. 
    Public restartRequested As Boolean
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = ocvb.color.Width / 5
        grid.sliders.TrackBar2.Value = ocvb.color.Height / 4
        grid.sliders.TrackBar1.Minimum = grid.sliders.TrackBar1.Value
        grid.sliders.TrackBar2.Minimum = grid.sliders.TrackBar2.Value
        grid.Run(ocvb)
        ocvb.desc = "Create the puzzle pieces for a genetic matching algorithm."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static width As Int32
        Static height As Int32
        If width <> grid.sliders.TrackBar1.Value Or height <> grid.sliders.TrackBar2.Value Or ocvb.frameCount = 0 Or restartRequested Then
            restartRequested = False
            grid.Run(ocvb)
            width = grid.roiList(0).Width
            height = grid.roiList(0).Height

            scrambled.Clear()
            unscrambled.Clear()
            Dim inputROI As New List(Of cv.Rect)
            For j = 0 To grid.roiList.Count - 1
                Dim roi = grid.roiList(j)
                If roi.Width = width And roi.Height = height Then
                    scrambled.Add(grid.roiList(j))
                    inputROI.Add(grid.roiList(j))
                    unscrambled.Add(grid.roiList(j))
                End If
            Next

            ' shuffle the roi's
            Dim scramIndex As Int32
            While inputROI.Count > 0
                Dim index = ocvb.ms_rng.Next(0, inputROI.Count - 1)
                Dim roi = inputROI(index)
                inputROI.RemoveAt(index) ' sampling without replacement
                Dim roi2 = scrambled(scramIndex)
                scrambled(scramIndex) = roi
                scramIndex += 1
            End While
        End If

        ' display image with shuffled roi's
        ocvb.result1.SetTo(0)
        For i = 0 To scrambled.Count - 1
            Dim roi = grid.roiList(i)
            Dim roi2 = scrambled(i)
            If roi.Width = width And roi.Height = height And roi2.Width = width And roi2.Height = height Then ocvb.result1(roi2) = ocvb.color(roi)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
    End Sub
End Class







Public Class Puzzle_SolverTopBottom : Implements IDisposable
    Dim corr As MatchTemplate_Basics
    Public roilist() As cv.Rect
    Public Sub New(ocvb As AlgorithmData)
        corr = New MatchTemplate_Basics(ocvb)
        corr.externalUse = True
        corr.sliders.Visible = False
        corr.radio.Visible = False

        ocvb.desc = "Put the puzzle back together."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If roilist Is Nothing Then
            ocvb.putText(New ActiveClass.TrueType("Currently this algorithm is only run in combination with Puzzle_Solver", 10, 50, RESULT2))
            Exit Sub
        End If

        Dim newHeight = CInt(ocvb.color.Width * ocvb.color.Height / roilist(0).Width)
        Dim results As New cv.Mat(newHeight, roilist(0).Width, cv.MatType.CV_8UC3, 0)
        Dim bestList As New SortedList(Of Single, cv.Point)(New CompareCorrelations)
        For i = 0 To roilist.Count - 1
            Dim roi1 = roilist(i)
            Dim bestIndex = 0
            Dim bestCorr As Single = -1
            For j = 0 To roilist.Count - 1
                If i = j Then Continue For
                Dim roi2 = roilist(j)
                corr.sample1 = ocvb.result1(roi1).Row(roi1.Height - 1)
                corr.sample2 = ocvb.result1(roi2).Row(0)
                corr.Run(ocvb)
                If bestCorr < corr.correlationMat.At(Of Single)(0, 0) Then
                    bestIndex = j
                    bestCorr = corr.correlationMat.At(Of Single)(0, 0)
                End If
            Next
            bestList.Add(bestCorr, New cv.Point(i, bestIndex))
        Next

        Dim nextRoilist As New List(Of cv.Rect)
        Dim nextX As Integer
        Dim nextY As Integer
        Dim bestArray = bestList.ToArray
        For i = 0 To bestList.Count - 1
            Dim roi1 = roilist(bestArray(i).Value.X)
            Dim roi2 = roilist(bestArray(i).Value.Y)
            If roi1.Width > 0 And roi2.Width > 0 Then
                ocvb.result1(roi1).CopyTo(results(New cv.Rect(nextX, nextY, roi1.Width, roi1.Height)))
                ocvb.result1(roi2).CopyTo(results(New cv.Rect(nextX, nextY + roi1.Height, roi2.Width, roi2.Height)))

                nextRoilist.Add(New cv.Rect(nextX, nextY, roi2.Width, roi1.Height + roi2.Height))
                nextY += roi1.Height + roi2.Height
                roilist(bestArray(i).Value.X).Width = 0
                roilist(bestArray(i).Value.Y).Width = 0
            End If
        Next
        ' there may be some tiles that matched another neighbor slightly better.  Add the runt roi's here and match on the next round.
        'For i = 0 To roilist.Count - 1
        '    Dim roi = roilist(i)
        '    If roi.Width > 0 Then
        '        nextRoilist.Add(New cv.Rect(nextX, nextY, roi.Width, roi.Height))
        '        ocvb.result1(roi).CopyTo(results(New cv.Rect(nextX, nextY, roi.Width, roi.Height)))
        '        nextY += roi.Height
        '        If nextY >= results.Height Then
        '            nextX += roi.Width
        '            nextY = 0
        '        End If
        '    End If
        'Next
        results.CopyTo(ocvb.result1)
        roilist = nextRoilist.ToArray
        ocvb.label1 = "Current input to puzzle solver"
        ocvb.label2 = "Current output of puzzle solver"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        corr.Dispose()
    End Sub
End Class









Public Class Puzzle_SolverSides : Implements IDisposable
    Dim corr As MatchTemplate_Basics
    Public roilist() As cv.Rect
    Public Sub New(ocvb As AlgorithmData)
        corr = New MatchTemplate_Basics(ocvb)
        corr.externalUse = True
        corr.sliders.Visible = False
        corr.radio.Visible = False

        ocvb.desc = "Put the puzzle back together."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If roilist Is Nothing Then
            ocvb.putText(New ActiveClass.TrueType("Currently this algorithm is only run in combination with Puzzle_Solver", 10, 50, RESULT2))
            Exit Sub
        End If
        Dim newWidth = CInt(ocvb.color.Width * ocvb.color.Height / roilist(0).Height)
        Dim results As New cv.Mat(roilist(0).Height, newWidth, cv.MatType.CV_8UC3, 0)
        Dim bestList As New SortedList(Of Single, cv.Point)(New CompareCorrelations)
        For i = 0 To roilist.Count - 1
            Dim roi1 = roilist(i)
            Dim bestIndex = 0
            Dim bestCorr As Single = -1
            For j = 0 To roilist.Count - 1
                If i = j Then Continue For
                Dim roi2 = roilist(j)
                corr.sample1 = ocvb.result1(roi1).Col(roi1.Width - 1)
                corr.sample2 = ocvb.result1(roi2).Col(0)
                corr.Run(ocvb)
                If bestCorr < corr.correlationMat.At(Of Single)(0, 0) Then
                    bestIndex = j
                    bestCorr = corr.correlationMat.At(Of Single)(0, 0)
                End If
            Next
            bestList.Add(bestCorr, New cv.Point(i, bestIndex))
        Next

        Dim nextRoilist As New List(Of cv.Rect)
        Dim nextX As Integer
        Dim nextY As Integer
        Dim bestArray = bestList.ToArray

        For i = 0 To bestList.Count - 1
            Dim roi1 = roilist(bestArray(i).Value.X)
            Dim roi2 = roilist(bestArray(i).Value.Y)
            If roi1.Width > 0 And roi2.Width > 0 Then
                ocvb.result1(roi1).CopyTo(results(New cv.Rect(nextX, nextY, roi1.Width, roi1.Height)))
                ocvb.result1(roi2).CopyTo(results(New cv.Rect(nextX + roi1.Width, nextY, roi2.Width, roi2.Height)))

                cv.Cv2.ImShow("results", results)
                cv.Cv2.WaitKey()

                nextRoilist.Add(New cv.Rect(nextX, nextY, roi1.Width + roi2.Width, roi1.Height))
                nextX += roi1.Width + roi2.Width
                roilist(bestArray(i).Value.X).Width = 0
                roilist(bestArray(i).Value.Y).Width = 0
            End If
        Next
        ' there may be some tiles that matched another neighbor slightly better.  Add the runt roi's here and match on the next round.
        For i = 0 To roilist.Count - 1
            Dim roi = roilist(i)
            If roi.Width > 0 Then
                nextRoilist.Add(New cv.Rect(nextX, nextY, roi.Width, roi.Height))
                ocvb.result1(roi).CopyTo(results(New cv.Rect(nextX, nextY, roi.Width, roi.Height)))
                nextY += roi.Height
                If nextY >= results.Height Then
                    nextX += roi.Width
                    nextY = 0
                End If
            End If
        Next
        results.CopyTo(ocvb.result1)
        roilist = nextRoilist.ToArray
        ocvb.label1 = "Current input to puzzle solver"
        ocvb.label2 = "Current output of puzzle solver"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        corr.Dispose()
    End Sub
End Class








Public Class Puzzle_Solver : Implements IDisposable
    Dim sides As Puzzle_SolverSides
    Dim topBot As Puzzle_SolverTopBottom
    Dim puzzle As Puzzle_Basics
    Dim check As New OptionsCheckbox
    Public src As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        check.Setup(ocvb, 1)
        check.Box(0).Text = "Restart Puzzle"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        puzzle = New Puzzle_Basics(ocvb)

        sides = New Puzzle_SolverSides(ocvb)
        topBot = New Puzzle_SolverTopBottom(ocvb)

        ocvb.desc = "Put the puzzle back together."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount < 10 Then Exit Sub ' no startup dark images.
        If check.Box(0).Checked Then
            check.Box(0).Checked = False
            puzzle.Run(ocvb)
        End If
        topBot.roilist = puzzle.grid.roiList.ToArray
        Static initialized As Boolean
        If initialized = False Then
            topBot.Run(ocvb)
            sides.roilist = topBot.roilist
            sides.Run(ocvb)
            initialized = True
        End If

        ocvb.label1 = "Current input to puzzle solver"
        ocvb.label2 = "Current output of puzzle solver"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        check.Dispose()
    End Sub
End Class
