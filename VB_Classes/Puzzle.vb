Imports cv = OpenCvSharp
Imports System.Threading

Module Puzzle_Solvers
    Public Class fit
        Public metricBelowOrLeft As Single
        Public metricAboveOrRight As Single
        Public index As Int32
        Public neighborBelowOrLeft As Integer
        Public neighborAboveOrRight As Integer
        Sub New(abs() As Single, i As Int32, n As Int32)
            metricBelowOrLeft = abs(0)
            metricAboveOrRight = abs(1)
            index = i
            neighborBelowOrLeft = n
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
        Dim absDiff(2 - 1) As Single
        For i = 0 To 3 Step 2
            cv.Cv2.Absdiff(sample(i), sample(i + 1), tmp)
            Dim absD = cv.Cv2.Sum(tmp)
            For j = 0 To 2
                If absDiff(i / 2) < absD.Item(j) Then absDiff(i / 2) = absD.Item(j)
            Next
        Next
        Return absDiff
    End Function
    Private Function fillFitList(ocvb As AlgorithmData, i As Integer, roilist() As cv.Rect, sample() As cv.Mat, rowCheck As Boolean) As SortedList(Of Single, fit)
        Dim maxDiff() = {Single.MinValue, Single.MinValue}
        Dim roi1 = roilist(i)
        If rowCheck Then sample(0) = ocvb.result1(roi1).Row(roi1.Height - 1) Else sample(0) = ocvb.result1(roi1).Col(roi1.Width - 1)
        If rowCheck Then sample(2) = ocvb.result1(roi1).Row(0) Else sample(2) = ocvb.result1(roi1).Col(0)
        Dim nextFitList As New List(Of fit)
        For j = 0 To roilist.Count - 1
            If i = j Then Continue For
            Dim roi2 = roilist(j)
            If rowCheck Then sample(1) = ocvb.result1(roi2).Row(0) Else sample(1) = ocvb.result1(roi2).Col(0)
            If rowCheck Then sample(3) = ocvb.result1(roi2).Row(roi2.Height - 1) Else sample(3) = ocvb.result1(roi2).Col(roi2.Width - 1)

            Dim absDiff() = computeMetric(sample)
            If maxDiff(0) < absDiff(0) Then maxDiff(0) = absDiff(0)
            If maxDiff(1) < absDiff(1) Then maxDiff(1) = absDiff(1)

            nextFitList.Add(New fit(absDiff, i, j))
        Next
        Dim sortedFit As New SortedList(Of Single, fit)(New CompareCorrelations)
        For j = 0 To nextFitList.Count - 1
            nextFitList.ElementAt(j).metricBelowOrLeft = (maxDiff(0) - nextFitList.ElementAt(j).metricBelowOrLeft) / maxDiff(0)
            nextFitList.ElementAt(j).metricAboveOrRight = (maxDiff(1) - nextFitList.ElementAt(j).metricAboveOrRight) / maxDiff(1)
            sortedFit.Add(nextFitList.ElementAt(j).metricBelowOrLeft, nextFitList.ElementAt(j))
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
        Dim sample(4 - 1) As cv.Mat
        For i = 0 To sample.Count - 1
            sample(i) = New cv.Mat
        Next

        Dim sortedFit As New SortedList(Of Single, fit)(New CompareCorrelations)
        For i = 0 To roilist.Count - 1
            sortedFit = fillFitList(ocvb, i, roilist, sample, rowCheck)
            fitList(i).Clear()
            For j = 0 To sortedFit.Count - 1
                fitList(i).Add(sortedFit.ElementAt(j).Value)
            Next
        Next

        Dim edgeList As New List(Of Integer)
        Dim tooGood As New SortedList(Of Single, fit)(New CompareCorrelations)
        Dim cutoff = -1.0
        ' This while loop only runs twice - the first time to find the cutoff that gets the necessary tiles to fill the bottom row.
        While edgeList.Count < edgeTotal
            edgeList.Clear()
            tooGood.Clear()
            For i = 0 To fitList.Count - 1
                Dim nextFit = fitList(i).ElementAt(0)
                If nextFit.metricBelowOrLeft > cutoff Then
                    tooGood.Add(nextFit.metricBelowOrLeft, nextFit)
                Else
                    edgeList.Add(i)
                End If
            Next
            ' set the cutoff to give the right number of edge tiles.
            If edgeList.Count <> edgeTotal Then cutoff = tooGood.ElementAt(tooGood.Count - edgeTotal).Value.metricBelowOrLeft
        End While

        Dim edgeROI(edgeTotal - 1) As cv.Rect
        For i = 0 To edgeROI.Count - 1
            edgeROI(i) = roilist(edgeList(i)) ' create a short list of roi's just for the edge tiles.
        Next

        rowCheck = Not rowCheck ' if we were looking at top and bottom, now we are looking at left and right sides.
        Dim indexLeftOfNeighbor(edgeTotal - 1) As SortedList(Of Single, fit)
        Dim indexRightOfNeighbor(edgeTotal - 1) As List(Of fit)
        For i = 0 To edgeTotal - 1
            indexLeftOfNeighbor(i) = fillFitList(ocvb, i, edgeROI, sample, rowCheck)
            sortedFit.Clear()
            For j = 0 To indexLeftOfNeighbor(i).Count - 1
                sortedFit.Add(indexLeftOfNeighbor(i).ElementAt(j).Value.metricAboveOrRight, indexLeftOfNeighbor(i).ElementAt(j).Value)
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
            If minValue > indexRightOfNeighbor(i).ElementAt(0).metricAboveOrRight Then
                minValue = indexRightOfNeighbor(i).ElementAt(0).metricAboveOrRight
                minIndex = i
            End If
        Next

        edgeList.Clear()
        edgeList.Add(minIndex)
        For i = 1 To edgeTotal - 1
            edgeList.Add(indexLeftOfNeighbor(edgeList.ElementAt(i - 1)).ElementAt(0).Value.neighborBelowOrLeft)
        Next

        ' we have the edge tiles but now must translate them into indexes for the original roilist.
        For i = 0 To edgeTotal - 1
            Dim roi = edgeROI(edgeList(i))
            For j = 0 To roilist.Count - 1
                If roilist(j) = roi Then
                    edgeList(i) = j
                    Exit For
                End If
            Next
        Next
        ' Once the edges are found, sort the fitlist by the AbsDiffAboveOrRight
        For i = 0 To fitList.Count - 1
            sortedFit.Clear()
            For j = 0 To fitList(i).Count - 1
                sortedFit.Add(fitList(i).ElementAt(j).metricAboveOrRight, fitList(i).ElementAt(j))
            Next
            fitList(i).Clear()
            For j = 0 To sortedFit.Count - 1
                fitList(i).Add(sortedFit.ElementAt(j).Value)
            Next
        Next
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
            rectIndex = edgelist(botindex)
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
        Next
        ocvb.label1 = "Current input to puzzle solver"
        If externalUse = False Then
            ocvb.label2 = "Vertically sorted - not horizontally"
        Else
            ocvb.label2 = "Current output of puzzle solver"
        End If
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
            rectIndex = edgelist(sideIndex)
            Dim roi = roilist(rectIndex)
            ocvb.result1(roi).CopyTo(ocvb.result2(New cv.Rect(ocvb.color.Width - roi.Width, nextY, roi.Width, roi.Height)))
            usedList.Add(rectIndex)
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
        Next

        ocvb.label1 = "Current input to puzzle solver"
        If externalUse = False Then
            ocvb.label2 = "Horizontally sorted - not vertically"
        Else
            ocvb.label2 = "Current output of puzzle solver"
        End If
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
