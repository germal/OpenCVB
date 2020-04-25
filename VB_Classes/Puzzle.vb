Imports cv = OpenCvSharp
Imports System.Threading

Module Puzzle_Solvers
    Public Class fit
        Public correlation As Single
        Public index As Int32
        Public neighbor As Int32
        Sub New(corr As Single, i As Int32, n As Int32)
            correlation = corr
            index = i
            neighbor = n
        End Sub
    End Class
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
                Dim index = ocvb.ms_rng.Next(inputROI.Count - 1)
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







Public Class Puzzle_SolverBottomUp : Implements IDisposable
    Dim corr As MatchTemplate_Basics
    Dim puzzle As Puzzle_Basics
    Public roilist() As cv.Rect
    Public externalUse As Boolean
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData)
        corr = New MatchTemplate_Basics(ocvb)
        corr.externalUse = True
        corr.sliders.Visible = False
        corr.radio.Visible = False

        puzzle = New Puzzle_Basics(ocvb)

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Restart Puzzle"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        ocvb.desc = "Put the puzzle back together using the correlation coefficients of the top and bottom of each ROI."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Const correlationThreshold = 0.75
        If externalUse = False Then
            If ocvb.frameCount < 10 Then Exit Sub ' no startup dark images.
            If check.Box(0).Checked Then
                check.Box(0).Checked = False
                puzzle.restartRequested = True
                puzzle.Run(ocvb)
                roilist = puzzle.grid.roiList.ToArray
            End If
        Else
            check.Visible = False
            puzzle.grid.sliders.Visible = False
        End If

        Dim goodFits(roilist.Count - 1) As List(Of fit)
        For i = 0 To goodFits.Count - 1
            goodFits(i) = New List(Of fit)  ' loops are easier if we don't have to check for "nothing" entries
        Next
        For i = 0 To roilist.Count - 1
            Dim roi1 = roilist(i)
            For j = 0 To roilist.Count - 1
                If i = j Then Continue For
                Dim roi2 = roilist(j)
                corr.sample1 = ocvb.result1(roi1).Row(roi1.Height - 1)
                corr.sample2 = ocvb.result1(roi2).Row(0)
                corr.Run(ocvb)
                If corr.correlationMat.At(Of Single)(0, 0) > 0 Then
                    goodFits(i).Add(New fit(corr.correlationMat.At(Of Single)(0, 0), i, j))
                End If
            Next
        Next

        Dim botList As New List(Of fit)
        Dim botTotal = CInt(ocvb.color.Width / roilist(0).Width)
        Dim colTotal = CInt(ocvb.color.Height / roilist(0).Height)
        Dim cutoff = correlationThreshold ' threshold for fit
        While botList.Count < botTotal
            botList.Clear()
            For i = 0 To goodFits.Count - 1
                Dim bot = -1
                For j = 0 To goodFits(i).Count - 1
                    Dim nextFit = goodFits(i).ElementAt(j)
                    If nextFit.correlation > cutoff Then
                        bot = i
                        Exit For
                    End If
                Next
                If bot = -1 Then
                    Dim bestFit As New fit(-1, 0, 0)
                    For j = 0 To goodFits(i).Count - 1
                        Dim nextFit = goodFits(i).ElementAt(j)
                        If bestFit.correlation < nextFit.correlation Then bestFit = nextFit
                    Next
                    botList.Add(bestFit)
                End If
            Next
            cutoff += 0.01
        End While

        Dim nextX As Integer
        Dim nextY As Integer
        For i = 0 To botList.Count - 1
            Dim roi = roilist(botList(i).index)
            ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(nextX, ocvb.color.Height - roi.Height, roi.Width, roi.Height)))
            nextX += roi.Width
            If nextX >= ocvb.color.Width Then Exit For
        Next

        Dim botindex = 0
        Dim rectIndex = 0
        For nextX = 0 To ocvb.color.Width - roilist(0).Width Step roilist(0).Width
            rectIndex = botList(botindex).index
            For nextY = ocvb.color.Height - roilist(0).Height * 2 To 0 Step -roilist(0).Height
                Dim bestCorr As Single = -1
                Dim bestI As Integer = 0
                Dim bestJ As Integer = 0
                For i = 0 To goodFits.Count - 1
                    If i = rectIndex Then Continue For
                    For j = 0 To goodFits(i).Count - 1
                        Dim nextFit = goodFits(i).ElementAt(j)
                        If nextFit.neighbor = rectIndex Then
                            If bestCorr < nextFit.correlation Then
                                bestJ = j
                                bestI = i
                                bestCorr = nextFit.correlation
                            End If
                        End If
                    Next
                Next
                Dim roi = roilist(bestI)
                goodFits(bestI).ElementAt(bestJ).correlation = 0
                ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(nextX, nextY, roi.Width, roi.Height)))
                rectIndex = bestI
                'cv.Cv2.ImShow("result2", ocvb.result2)
                'cv.Cv2.WaitKey()
            Next
            botindex += 1
        Next
        ocvb.label1 = "Current input to puzzle solver"
        ocvb.label2 = "Current output of puzzle solver"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        corr.Dispose()
        puzzle.Dispose()
        check.Dispose()
    End Sub
End Class


Public Class Puzzle_SolverLeftRight : Implements IDisposable
    Dim corr As MatchTemplate_Basics
    Dim puzzle As Puzzle_Basics
    Public roilist() As cv.Rect
    Public externalUse As Boolean
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData)
        corr = New MatchTemplate_Basics(ocvb)
        corr.externalUse = True
        corr.sliders.Visible = False
        corr.radio.Visible = False

        puzzle = New Puzzle_Basics(ocvb)

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Restart Puzzle"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        ocvb.desc = "Put the puzzle back together using correlation coefficients of the sides of each ROI."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Const correlationThreshold = 0.75
        If externalUse = False Then
            If ocvb.frameCount < 10 Then Exit Sub ' no startup dark images.
            If check.Box(0).Checked Then
                check.Box(0).Checked = False
                puzzle.restartRequested = True
                puzzle.Run(ocvb)
                roilist = puzzle.grid.roiList.ToArray
            End If
        Else
            check.Visible = False
            puzzle.grid.sliders.Visible = False
        End If

        Dim goodFits(roilist.Count - 1) As List(Of fit)
        For i = 0 To goodFits.Count - 1
            goodFits(i) = New List(Of fit)  ' loops are easier if we don't have to check for "nothing" entries
        Next
        For i = 0 To roilist.Count - 1
            Dim roi1 = roilist(i)
            For j = 0 To roilist.Count - 1
                If i = j Then Continue For
                Dim roi2 = roilist(j)
                corr.sample1 = ocvb.result1(roi1).Col(roi1.Width - 1)
                corr.sample2 = ocvb.result1(roi2).Col(0)
                corr.Run(ocvb)
                If corr.correlationMat.At(Of Single)(0, 0) > 0 Then
                    goodFits(i).Add(New fit(corr.correlationMat.At(Of Single)(0, 0), i, j))
                End If
            Next
        Next

        Dim rightList As New List(Of fit)
        Dim rightTotal = CInt(ocvb.color.Width / roilist(0).Width)
        Dim colTotal = CInt(ocvb.color.Height / roilist(0).Height)
        Dim cutoff = correlationThreshold ' threshold for fit
        While rightList.Count < rightTotal
            rightList.Clear()
            For i = 0 To goodFits.Count - 1
                Dim right = -1
                For j = 0 To goodFits(i).Count - 1
                    Dim nextFit = goodFits(i).ElementAt(j)
                    If nextFit.correlation > cutoff Then
                        right = i
                        Exit For
                    End If
                Next
                If right = -1 Then
                    Dim bestFit As New fit(-1, 0, 0)
                    For j = 0 To goodFits(i).Count - 1
                        Dim nextFit = goodFits(i).ElementAt(j)
                        If bestFit.correlation < nextFit.correlation Then bestFit = nextFit
                    Next
                    rightList.Add(bestFit)
                End If
            Next
            cutoff += 0.01
        End While

        Dim nextX As Integer
        Dim nextY As Integer
        For i = 0 To rightList.Count - 1
            Dim roi = roilist(rightList(i).index)
            ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(nextX, ocvb.color.Height - roi.Height, roi.Width, roi.Height)))
            nextX += roi.Width
            If nextX >= ocvb.color.Width Then Exit For
        Next

        Dim rightindex = 0
        Dim rectIndex = 0
        For nextY = 0 To ocvb.color.Height - roilist(0).Height Step roilist(0).Height
            rectIndex = rightList(rightindex).index
            For nextX = ocvb.color.Width - roilist(0).Width * 2 To 0 Step -roilist(0).Width
                Dim bestCorr As Single = -1
                Dim bestI As Integer = 0
                Dim bestJ As Integer = 0
                For i = 0 To goodFits.Count - 1
                    If i = rectIndex Then Continue For
                    For j = 0 To goodFits(i).Count - 1
                        Dim nextFit = goodFits(i).ElementAt(j)
                        If nextFit.neighbor = rectIndex Then
                            If bestCorr < nextFit.correlation Then
                                bestJ = j
                                bestI = i
                                bestCorr = nextFit.correlation
                            End If
                        End If
                    Next
                Next
                Dim roi = roilist(bestI)
                goodFits(bestI).ElementAt(bestJ).correlation = 0
                ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(nextX, nextY, roi.Width, roi.Height)))
                rectIndex = bestI
                'cv.Cv2.ImShow("result2", ocvb.result2)
                'cv.Cv2.WaitKey()
            Next
            rightindex += 1
        Next
        ocvb.label1 = "Current input to puzzle solver"
        ocvb.label2 = "Current output of puzzle solver"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        corr.Dispose()
        puzzle.Dispose()
        check.Dispose()
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
        Static initialList() As cv.Rect
        If ocvb.frameCount < 10 Then Exit Sub ' no startup dark images.
        If check.Box(0).Checked Then
            check.Box(0).Checked = False
            puzzle.Run(ocvb)
            initialList = puzzle.grid.roiList.ToArray
        End If
        If initialList.Count > 1 Then
            topBot.roilist = initialList
            topBot.Run(ocvb)

            If topBot.roilist.Count > 1 Then
                sides.roilist = topBot.roilist
                sides.Run(ocvb)
            End If
        End If
        initialList = sides.roilist ' for the next iteration

        ocvb.label1 = "Current input to puzzle solver"
        ocvb.label2 = "Current output of puzzle solver"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        check.Dispose()
        puzzle.Dispose()
        sides.Dispose()
        topBot.Dispose()
    End Sub
End Class
