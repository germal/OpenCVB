Imports cv = OpenCvSharp
Imports System.Threading

Module Puzzle_Solvers
    Public Class fit
        Public correlation As Single
        Public absDiff As Single
        Public index As Int32
        Public neighbor As Int32
        Sub New(corr As Single, abs As cv.Scalar, i As Int32, n As Int32)
            correlation = corr
            absDiff = abs
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

    Public Function fitCheck(ocvb As AlgorithmData, roilist() As cv.Rect, rowCheck As Boolean, fitList() As List(Of fit), edgeTotal As Integer) As List(Of fit)
        Dim saveOptions = ocvb.parms.ShowOptions
        ocvb.parms.ShowOptions = False
        Dim corr = New MatchTemplate_Basics(ocvb)
        corr.externalUse = True
        ocvb.parms.ShowOptions = saveOptions

        For i = 0 To fitList.Count - 1
            fitList(i) = New List(Of fit) ' loops are easier if we don't have to check for "nothing" entries
        Next

        Dim sortedFit As New SortedList(Of Single, fit)(New CompareCorrelations)

        ' compute absDiff of every bottom to every top
        Dim maxDiff = Single.MinValue
        Dim tmp As New cv.Mat
        For i = 0 To roilist.Count - 1
            Dim roi1 = roilist(i)
            If rowCheck Then corr.sample1 = ocvb.result1(roi1).Row(roi1.Height - 1) Else corr.sample1 = ocvb.result1(roi1).Col(roi1.Width - 1)
            sortedFit.Clear()
            For j = 0 To roilist.Count - 1
                If i = j Then Continue For
                Dim roi2 = roilist(j)
                If rowCheck Then corr.sample2 = ocvb.result1(roi2).Row(0) Else corr.sample2 = ocvb.result1(roi2).Col(0)
                corr.Run(ocvb)

                cv.Cv2.Absdiff(corr.sample1, corr.sample2, tmp)
                Dim absD = cv.Cv2.Sum(tmp)
                Dim absDiff As Single = Single.MinValue
                For k = 0 To 2
                    If absDiff < absD.Item(k) Then absDiff = absD.Item(k)
                Next
                If maxDiff < absDiff Then maxDiff = absDiff

                Dim nextFit = New fit(corr.correlationMat.At(Of Single)(0, 0), absDiff, i, j)
                fitList(i).Add(nextFit)
            Next
            For j = 0 To fitList(i).Count - 1
                fitList(i).ElementAt(j).absDiff = (maxDiff - fitList(i).ElementAt(j).absDiff) / maxDiff
                sortedFit.Add(fitList(i).ElementAt(j).absDiff, fitList(i).ElementAt(j))
            Next
            fitList(i).Clear()
            For j = 0 To sortedFit.Count - 1
                fitList(i).Add(sortedFit.ElementAt(j).Value)
            Next
        Next

        Dim edgeList As New List(Of fit)
        Dim tooGood As New SortedList(Of Single, fit)(New CompareCorrelations)
        Dim cutoff = -1.0
        ' This while loop only runs twice - the first time to find the cutoff that gets the necessary tiles to fill the bottom row.
        While edgeList.Count < edgeTotal
            edgeList.Clear()
            tooGood.Clear()
            For i = 0 To fitList.Count - 1
                Dim nextFit = fitList(i).ElementAt(0)
                If nextFit.absDiff > cutoff Then
                    tooGood.Add(nextFit.absDiff, nextFit)
                Else
                    edgeList.Add(nextFit)
                End If
            Next

            ' set the cutoff to the 
            If edgeList.Count <> edgeTotal Then cutoff = tooGood.ElementAt(tooGood.Count - edgeTotal).Value.absDiff
        End While
        corr.Dispose()
        Return edgeList
    End Function
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
        ' These 2 lines help with visual debugging.
        ocvb.color.Line(New cv.Point(0, ocvb.color.Height - 90), New cv.Point(ocvb.color.Width, ocvb.color.Height - 90), cv.Scalar.Red, 4)
        ocvb.color.Line(New cv.Point(ocvb.color.Width - 50, 0), New cv.Point(ocvb.color.Width - 50, ocvb.color.Height), cv.Scalar.Yellow, 4)
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





Public Class Puzzle_SolverVertical : Implements IDisposable
    Dim puzzle As Puzzle_Basics
    Public roilist() As cv.Rect
    Public externalUse As Boolean
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData)
        puzzle = New Puzzle_Basics(ocvb)

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Restart Puzzle"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        ocvb.desc = "Put the puzzle back together using the absdiff values of the top and bottom of each ROI."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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

        Dim fitList(roilist.Count - 1) As List(Of fit)
        Dim edgeTotal = CInt(ocvb.color.Width / roilist(0).Width)
        Dim edgelist = fitCheck(ocvb, roilist, rowCheck:=True, fitList, edgeTotal)

        Dim botI = 0
        For nextx = 0 To ocvb.color.Width - roilist(0).Width Step roilist(0).Width
            Dim roi = roilist(edgelist(botI).index)
            ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(nextx, ocvb.color.Height - roi.Height, roi.Width, roi.Height)))
            fitList(edgelist(botI).index).Clear() ' don't try to fit this tile anywhere else.
            botI += 1
        Next

        Dim botindex = 0
        Dim rectIndex = 0
        For nextX = 0 To ocvb.color.Width - roilist(0).Width Step roilist(0).Width
            rectIndex = edgelist(botindex).index
            For nextY = ocvb.color.Height - roilist(0).Height * 2 To 0 Step -roilist(0).Height
                Dim bestCorr As Single = -1
                Dim bestI As Integer = 0
                Dim bestJ As Integer = 0
                For i = 0 To fitList.Count - 1
                    If i = rectIndex Then Continue For
                    For j = 0 To fitList(i).Count - 1
                        Dim nextFit = fitList(i).ElementAt(j)
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
                fitList(bestI).ElementAt(bestJ).correlation = 0
                ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(nextX, nextY, roi.Width, roi.Height)))
                fitList(bestI).Clear()
                rectIndex = bestI
            Next
            botindex += 1
        Next
        ocvb.label1 = "Current input to puzzle solver"
        If externalUse = False Then
            ocvb.label2 = "Vertically sorted - not horizontally"
        Else
            ocvb.label2 = "Current output of puzzle solver"
        End If

    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        puzzle.Dispose()
        check.Dispose()
    End Sub
End Class






Public Class Puzzle_SolverHorizontal : Implements IDisposable
    Dim puzzle As Puzzle_Basics
    Public roilist() As cv.Rect
    Public externalUse As Boolean
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData)
        puzzle = New Puzzle_Basics(ocvb)

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Restart Puzzle"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        ocvb.desc = "Put the puzzle back together using the correlation coefficients of the top and bottom of each ROI."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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

        Dim fitList(roilist.Count - 1) As List(Of fit)
        Dim edgeTotal = CInt(ocvb.color.Height / roilist(0).Height)
        Dim edgelist = fitCheck(ocvb, roilist, rowCheck:=False, fitList, edgeTotal)

        Dim sideI = 0
        For nexty = 0 To ocvb.color.Height - roilist(0).Height Step roilist(0).Height
            Dim roi = roilist(edgelist(sideI).index)
            ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(ocvb.color.Width - roi.Width, nexty, roi.Width, roi.Height)))
            fitList(edgelist(sideI).index).Clear() ' don't try to fit this tile anywhere else.
            sideI += 1
        Next

        sideI = 0
        Dim rectIndex = 0
        For nextY = 0 To ocvb.color.Height - roilist(0).Height Step roilist(0).Height
            rectIndex = edgelist(sideI).index
            For nextX = ocvb.color.Width - roilist(0).Width * 2 To 0 Step -roilist(0).Width
                Dim bestCorr As Single = -1
                Dim bestI As Integer = 0
                Dim bestJ As Integer = 0
                For i = 0 To fitList.Count - 1
                    If i = rectIndex Then Continue For
                    For j = 0 To fitList(i).Count - 1
                        Dim nextFit = fitList(i).ElementAt(j)
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
                fitList(bestI).ElementAt(bestJ).correlation = 0
                ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(nextX, nextY, roi.Width, roi.Height)))
                fitList(bestI).Clear()
                rectIndex = bestI
            Next
            sideI += 1
        Next
        ocvb.label1 = "Current input to puzzle solver"
        If externalUse = False Then
            ocvb.label2 = "Horizontally sorted - not vertically"
        Else
            ocvb.label2 = "Current output of puzzle solver"
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        puzzle.Dispose()
        check.Dispose()
    End Sub
End Class






Public Class Puzzle_Solver : Implements IDisposable
    Dim sides As Puzzle_SolverHorizontal
    Dim topBot As Puzzle_SolverVertical
    Dim puzzle As Puzzle_Basics
    Dim check As New OptionsCheckbox
    Public src As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        check.Setup(ocvb, 1)
        check.Box(0).Text = "Restart Puzzle"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        puzzle = New Puzzle_Basics(ocvb)

        sides = New Puzzle_SolverHorizontal(ocvb)
        topBot = New Puzzle_SolverVertical(ocvb)

        ocvb.desc = "Put the puzzle back together."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        'Static initialList() As cv.Rect
        'If ocvb.frameCount < 10 Then Exit Sub ' no startup dark images.
        'If check.Box(0).Checked Then
        '    check.Box(0).Checked = False
        '    puzzle.Run(ocvb)
        '    initialList = puzzle.grid.roiList.ToArray
        'End If
        'If initialList.Count > 1 Then
        '    topBot.roilist = initialList
        '    topBot.Run(ocvb)

        '    If topBot.roilist.Count > 1 Then
        '        sides.roilist = topBot.roilist
        '        sides.Run(ocvb)
        '    End If
        'End If
        'initialList = sides.roilist ' for the next iteration

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
