Imports cv = OpenCvSharp
Imports System.Threading

Module Puzzle_Solvers
    Public Class fit
        Public index As Int32
        Public neighborBelowOrLeft As Integer
        Public neighborAboveOrRight As Integer
        Public metricUp As Single
        Public metricDn As Single
        Public metricLt As Single
        Public metricRt As Single
        Public bestMetricUp As Single
        Public bestMetricDn As Single
        Public bestMetricLt As Single
        Public bestMetricRt As Single
        Public bestUp As Integer
        Public bestDn As Integer
        Public bestLt As Integer
        Public bestRt As Integer
        Public metricTotal As Single
        Sub New(abs() As Single, _index As Int32, n As Int32)
            metricUp = abs(0)
            metricDn = abs(1)
            metricLt = abs(2)
            metricRt = abs(3)
            index = _index
            neighborBelowOrLeft = n
            neighborAboveOrRight = n
        End Sub
    End Class
    Public Class CompareCorrelations : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Private Function computeMetric(sample() As cv.Mat) As Single()
        Dim tmp As New cv.Mat
        Dim absDiff(4 - 1) As Single
        For i = 0 To 8 - 1 Step 2
            cv.Cv2.Absdiff(sample(i), sample(i + 1), tmp)
            Dim absD = cv.Cv2.Sum(tmp)
            For j = 0 To 3 - 1
                If absDiff(i / 2) < absD.Item(j) Then absDiff(i / 2) = absD.Item(j)
            Next
        Next
        Return absDiff
    End Function
    Private Function fillFitList(ocvb As AlgorithmData, i As Integer, roilist() As cv.Rect, sample() As cv.Mat) As SortedList(Of Single, fit)
        Dim maxDiff() = {Single.MinValue, Single.MinValue, Single.MinValue, Single.MinValue}
        Dim roi1 = roilist(i)
        sample(0) = ocvb.result1(roi1).Row(0)
        sample(2) = ocvb.result1(roi1).Row(roi1.Height - 1)
        sample(4) = ocvb.result1(roi1).Col(0)
        sample(6) = ocvb.result1(roi1).Col(roi1.Width - 1)
        Dim nextFitList As New List(Of fit)
        For j = 0 To roilist.Count - 1
            If i = j Then Continue For
            Dim roi2 = roilist(j)
            sample(1) = ocvb.result1(roi2).Row(roi1.Height - 1)
            sample(3) = ocvb.result1(roi2).Row(0)
            sample(5) = ocvb.result1(roi2).Col(roi2.Width - 1)
            sample(7) = ocvb.result1(roi2).Col(0)

            Dim absDiff() = computeMetric(sample)
            For k = 0 To maxDiff.Count - 1
                If maxDiff(k) < absDiff(k) Then maxDiff(k) = absDiff(k)
            Next

            nextFitList.Add(New fit(absDiff, i, j))
        Next

        Dim sortedUp As New SortedList(Of Single, fit)(New CompareCorrelations)
        Dim sortedDn As New SortedList(Of Single, fit)(New CompareCorrelations)
        Dim sortedLt As New SortedList(Of Single, fit)(New CompareCorrelations)
        Dim sortedRt As New SortedList(Of Single, fit)(New CompareCorrelations)
        For j = 0 To nextFitList.Count - 1
            nextFitList.ElementAt(j).metricUp = (maxDiff(0) - nextFitList.ElementAt(j).metricUp) / maxDiff(0)
            nextFitList.ElementAt(j).metricDn = (maxDiff(1) - nextFitList.ElementAt(j).metricDn) / maxDiff(1)
            nextFitList.ElementAt(j).metricLt = (maxDiff(2) - nextFitList.ElementAt(j).metricLt) / maxDiff(2)
            nextFitList.ElementAt(j).metricRt = (maxDiff(3) - nextFitList.ElementAt(j).metricRt) / maxDiff(3)
            sortedUp.Add(nextFitList.ElementAt(j).metricUp, nextFitList.ElementAt(j))
            sortedDn.Add(nextFitList.ElementAt(j).metricDn, nextFitList.ElementAt(j))
            sortedLt.Add(nextFitList.ElementAt(j).metricLt, nextFitList.ElementAt(j))
            sortedRt.Add(nextFitList.ElementAt(j).metricRt, nextFitList.ElementAt(j))
        Next
        Dim bestUp = sortedUp.ElementAt(0).Value.neighborAboveOrRight
        Dim bestDn = sortedDn.ElementAt(0).Value.neighborBelowOrLeft
        Dim bestLt = sortedLt.ElementAt(0).Value.neighborAboveOrRight
        Dim bestRt = sortedRt.ElementAt(0).Value.neighborBelowOrLeft
        Dim bestMetricUp = sortedUp.ElementAt(0).Value.metricUp
        Dim bestMetricDn = sortedDn.ElementAt(0).Value.metricDn
        Dim bestMetricLt = sortedLt.ElementAt(0).Value.metricLt
        Dim bestMetricRt = sortedRt.ElementAt(0).Value.metricRt
        Dim sortedFit = sortedLt
        For j = 0 To sortedFit.Count - 1
            sortedFit.ElementAt(j).Value.metricTotal = bestMetricUp + bestMetricDn + bestMetricLt + bestMetricRt
            Dim maxMetric = Single.MinValue
            For k = 0 To 4 - 1
                Dim nextVal = Choose(k + 1, sortedFit.ElementAt(j).Value.metricUp, sortedFit.ElementAt(j).Value.metricDn,
                                             sortedFit.ElementAt(j).Value.metricLt, sortedFit.ElementAt(j).Value.metricRt)
                If nextVal > maxMetric Then maxMetric = nextVal
            Next
            sortedFit.ElementAt(j).Value.bestUp = bestUp
            sortedFit.ElementAt(j).Value.bestDn = bestDn
            sortedFit.ElementAt(j).Value.bestLt = bestLt
            sortedFit.ElementAt(j).Value.bestRt = bestRt
            sortedFit.ElementAt(j).Value.bestMetricUp = bestMetricUp
            sortedFit.ElementAt(j).Value.bestMetricDn = bestMetricDn
            sortedFit.ElementAt(j).Value.bestMetricLt = bestMetricLt
            sortedFit.ElementAt(j).Value.bestMetricRt = bestMetricRt
        Next
        Return sortedFit
    End Function
    Public Function fitCheck(ocvb As AlgorithmData, roilist() As cv.Rect, rowCheck As Boolean, fitList() As List(Of fit), edgeTotal As Integer) As List(Of Integer)
        If fitList(0) Is Nothing Then
            For i = 0 To fitList.Count - 1
                fitList(i) = New List(Of fit) ' loops are easier if we don't have to check for "nothing" entries
            Next
        End If

        Dim saveOptions = ocvb.parms.ShowOptions
        ocvb.parms.ShowOptions = False
        ocvb.parms.ShowOptions = saveOptions

        ' compute absDiff of every top/bottom to every left/right side
        Dim tmp As New cv.Mat
        Dim sample(8 - 1) As cv.Mat
        For i = 0 To sample.Count - 1
            sample(i) = New cv.Mat
        Next

        Dim sortedFit As New SortedList(Of Single, fit)(New CompareCorrelations)
        For i = 0 To roilist.Count - 1
            sortedFit = fillFitList(ocvb, i, roilist, sample)
            fitList(i).Clear()
            For j = 0 To sortedFit.Count - 1
                fitList(i).Add(sortedFit.ElementAt(j).Value)
            Next
        Next

        Dim corners As New List(Of Integer)
        Dim nonCorners As New SortedList(Of Single, fit)(New CompareCorrelations)
        Dim cutoff = -1.0
        ' This while loop only runs twice - the first time to find the cutoff that gets the necessary tiles 
        While corners.Count < 4 ' there are only 4 corners.
            corners.Clear()
            nonCorners.Clear()
            For i = 0 To fitList.Count - 1
                Dim nextFit = fitList(i).ElementAt(0)
                If nextFit.metricTotal > cutoff Then nonCorners.Add(nextFit.metricTotal, nextFit) Else corners.Add(i)
            Next
            ' set the cutoff to give the right number of edge tiles.
            If corners.Count <> 4 Then
                cutoff = nonCorners.ElementAt(nonCorners.Count - 4).Value.metricTotal
            End If
        End While

        Dim edgeList As New List(Of Integer)
        Dim tooGood As New SortedList(Of Single, fit)(New CompareCorrelations)
        cutoff = -1.0
        ' This while loop only runs twice - the first time to find the cutoff that gets the necessary tiles to fill the bottom row.
        While edgeList.Count < edgeTotal
            edgeList.Clear()
            tooGood.Clear()
            For i = 0 To fitList.Count - 1
                Dim nextFit = fitList(i).ElementAt(0)
                If rowCheck Then
                    If nextFit.metricDn > cutoff Then tooGood.Add(nextFit.metricDn, nextFit) Else edgeList.Add(i)
                Else
                    If nextFit.metricLt > cutoff Then tooGood.Add(nextFit.metricLt, nextFit) Else edgeList.Add(i)
                End If
            Next
            ' set the cutoff to give the right number of edge tiles.
            If edgeList.Count <> edgeTotal Then
                If rowCheck Then
                    cutoff = tooGood.ElementAt(tooGood.Count - edgeTotal).Value.metricDn
                Else
                    cutoff = tooGood.ElementAt(tooGood.Count - edgeTotal).Value.metricLt
                End If
            End If
        End While

        Dim edgeROI(edgeTotal - 1) As cv.Rect
        For i = 0 To edgeROI.Count - 1
            edgeROI(i) = roilist(edgeList(i)) ' create a short list of roi's just for the edge tiles.
        Next

        Dim indexLeftOfNeighbor(edgeTotal - 1) As SortedList(Of Single, fit)
        Dim indexRightOfNeighbor(edgeTotal - 1) As List(Of fit)
        For i = 0 To edgeTotal - 1
            indexLeftOfNeighbor(i) = fillFitList(ocvb, i, edgeROI, sample)
            sortedFit.Clear()
            For j = 0 To indexLeftOfNeighbor(i).Count - 1
                If rowCheck Then
                    sortedFit.Add(indexLeftOfNeighbor(i).ElementAt(j).Value.metricDn, indexLeftOfNeighbor(i).ElementAt(j).Value)
                Else
                    sortedFit.Add(indexLeftOfNeighbor(i).ElementAt(j).Value.metricRt, indexLeftOfNeighbor(i).ElementAt(j).Value)
                End If
            Next

            indexRightOfNeighbor(i) = New List(Of fit)
            For j = 0 To sortedFit.Count - 1
                indexRightOfNeighbor(i).Add(sortedFit.ElementAt(j).Value)
            Next
        Next

        Dim minValue = Single.MaxValue
        Dim minIndex As Integer
        ' find the leftmost tile - the one with the smallest probability of being to the right of any of the other tiles.
        For i = 0 To edgeTotal - 1
            If rowCheck Then
                If minValue > indexRightOfNeighbor(i).ElementAt(0).metricDn Then
                    minValue = indexRightOfNeighbor(i).ElementAt(0).metricDn
                    minIndex = i
                End If
            Else
                If minValue > indexRightOfNeighbor(i).ElementAt(0).metricRt Then
                    minValue = indexRightOfNeighbor(i).ElementAt(0).metricRt
                    minIndex = i
                End If
            End If
        Next

        Dim newEdges = New List(Of Integer)
        newEdges.Add(minIndex)
        For i = 1 To edgeTotal - 1
            newEdges.Add(indexLeftOfNeighbor(newEdges.ElementAt(i - 1)).ElementAt(0).Value.neighborBelowOrLeft)
        Next

        ' we have the edge tiles but now must translate them into indexes into the original roilist for the calling function.
        For i = 0 To edgeTotal - 1
            Dim roi = edgeROI(newEdges(i))
            For j = 0 To roilist.Count - 1
                If roilist(j) = roi Then
                    newEdges(i) = j
                    Exit For
                End If
            Next
        Next
        For i = 0 To edgeList.Count - 1
            newEdges.Add(edgeList.ElementAt(i))
        Next
        ' Once the edges are found, sort the fitlist by the AbsDiffAboveOrRight
        For i = 0 To fitList.Count - 1
            sortedFit.Clear()
            For j = 0 To fitList(i).Count - 1
                If rowCheck Then
                    sortedFit.Add(fitList(i).ElementAt(j).metricDn, fitList(i).ElementAt(j))
                Else
                    sortedFit.Add(fitList(i).ElementAt(j).metricRt, fitList(i).ElementAt(j))
                End If
            Next
            fitList(i).Clear()
            For j = 0 To sortedFit.Count - 1
                fitList(i).Add(sortedFit.ElementAt(j).Value)
            Next
        Next
        Return newEdges
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
        grid.sliders.TrackBar1.Value = ocvb.color.Width / 10
        grid.sliders.TrackBar2.Value = ocvb.color.Height / 4
        grid.Run(ocvb)
        grid.sliders.Hide()
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
        ocvb.color.Line(New cv.Point(0, ocvb.color.Height - 70), New cv.Point(ocvb.color.Width, ocvb.color.Height - 70), cv.Scalar.Red, 4)
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
                puzzle.restartRequested = True
                puzzle.Run(ocvb)
                roilist = puzzle.grid.roiList.ToArray
            End If
        Else
            check.Visible = False
            puzzle.grid.sliders.Visible = False
        End If

        Static fitList(roilist.Count - 1) As List(Of fit)
        Dim edgeTotal = CInt(ocvb.color.Width / roilist(0).Width)
        Dim edgelist = fitCheck(ocvb, roilist, rowCheck:=True, fitList, edgeTotal)

        Dim botindex = 0
        Dim rectIndex = 0
        Dim usedList As New List(Of Integer)
        For nextX = 0 To ocvb.color.Width - roilist(0).Width Step roilist(0).Width
            For i = 0 To edgelist.Count - 1
                rectIndex = edgelist(botindex)
                If usedList.Contains(rectIndex) Then botindex += 1 Else Exit For
                If botindex >= edgelist.Count Then Exit For
            Next
            Dim roi = roilist(rectIndex)
            ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(nextX, ocvb.color.Height - roi.Height, roi.Width, roi.Height)))
            usedList.Add(rectIndex)
            For nextY = ocvb.color.Height - roilist(0).Height * 2 To 0 Step -roilist(0).Height
                Dim bestIndex = fitList(rectIndex).ElementAt(0).neighborBelowOrLeft
                For i = 0 To fitList(rectIndex).Count - 1
                    If usedList.Contains(fitList(rectIndex).ElementAt(i).neighborBelowOrLeft) = False Then
                        bestIndex = fitList(rectIndex).ElementAt(i).neighborBelowOrLeft
                        Exit For
                    End If
                Next
                usedList.Add(bestIndex)
                roi = roilist(bestIndex)
                ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(nextX, nextY, roi.Width, roi.Height)))
                rectIndex = bestIndex
            Next
            botindex += 1
            If botindex >= edgelist.Count Then Exit For ' we can't find the last piece...
        Next
        ocvb.label1 = "Current input to puzzle solver"
        ocvb.label2 = "Current output of puzzle solver"
        check.Box(0).Checked = False
        If edgelist.Count > 5 Then Thread.Sleep(1000)
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

        ocvb.desc = "Put the puzzle back together using the correlation coefficients of the left and right side of each ROI."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            If ocvb.frameCount < 10 Then Exit Sub ' no startup dark images.
            If check.Box(0).Checked Then
                puzzle.restartRequested = True
                puzzle.Run(ocvb)
                roilist = puzzle.grid.roiList.ToArray
            End If
        Else
            check.Visible = False
            puzzle.grid.sliders.Visible = False
        End If

        Static fitList(roilist.Count - 1) As List(Of fit)
        Dim edgeTotal = CInt(ocvb.color.Height / roilist(0).Height)
        Dim edgelist = fitCheck(ocvb, roilist, rowCheck:=False, fitList, edgeTotal)

        Dim sideIndex = 0
        Dim rectIndex = 0
        Dim usedList As New List(Of Integer)
        For nextY = 0 To ocvb.color.Height - roilist(0).Height Step roilist(0).Height
            For i = 0 To edgelist.Count - 1
                rectIndex = edgelist(sideIndex)
                If usedList.Contains(rectIndex) Then sideIndex += 1 Else Exit For
                If sideIndex >= edgelist.Count Then Exit For
            Next
            usedList.Add(rectIndex)

            Dim roi = roilist(rectIndex)
            ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(ocvb.color.Width - roi.Width, nextY, roi.Width, roi.Height)))
            For nextX = ocvb.color.Width - roilist(0).Width * 2 To 0 Step -roilist(0).Width
                Dim bestIndex = fitList(rectIndex).ElementAt(0).neighborBelowOrLeft
                For i = 0 To fitList(rectIndex).Count - 1
                    If usedList.Contains(fitList(rectIndex).ElementAt(i).neighborBelowOrLeft) = False Then
                        bestIndex = fitList(rectIndex).ElementAt(i).neighborBelowOrLeft
                        Exit For
                    End If
                Next
                usedList.Add(bestIndex)
                roi = roilist(bestIndex)
                ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(nextX, nextY, roi.Width, roi.Height)))
                rectIndex = bestIndex
            Next
            sideIndex += 1
            If sideIndex >= edgelist.Count Then Exit For ' we can't find the last piece...
        Next

        ocvb.label1 = "Current input to puzzle solver"
        ocvb.label2 = "Current output of puzzle solver"
        check.Box(0).Checked = False
        If edgelist.Count > 4 Then Thread.Sleep(1000)
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
