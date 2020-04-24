Imports cv = OpenCvSharp
Imports VB_Classes

''Public Class VBTest_Basics : Implements IDisposable
''    Dim sobel As VB_Classes.Edges_Canny
''    Public Sub New(ocvb As AlgorithmData)
''        ocvb.name = "VBTest_Basics"
''        ocvb.label1 = "VBTest_Basics"
''        ocvb.desc = "Insert and debug new experiments here and then migrate them to the VB_Classes which is compiled in Release mode."
''        sobel = New VB_Classes.Edges_Canny(ocvb)
''    End Sub
''    Public Sub Run(ocvb As AlgorithmData)
''        sobel.Run(ocvb)
''        'ocvb.putText(New ActiveClass.TrueType("Test", 10, 125))
''    End Sub
''    Public Sub Dispose() Implements IDisposable.Dispose
''        sobel.Dispose()
''    End Sub
''End Class







'Module puzzlePiece_Exports
'    Public Const RESULT1 = 2
'    Public Const RESULT2 = 3

'    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
'    Public Function Puzzle_PieceCorrelation_Open(puzzlePieces As IntPtr, count As Int32) As IntPtr
'    End Function
'    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
'    Public Sub Puzzle_PieceCorrelation_Close(saPtr As IntPtr)
'    End Sub
'    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
'    Public Function Puzzle_PieceCorrelation_Run(saPtr As IntPtr, puzzleOrder As IntPtr, img As IntPtr, rows As Int32, cols As Int32, state As cv.Vec3i) As IntPtr
'    End Function
'End Module






'Public Class VBTest_Basics : Implements IDisposable
'    Dim puzzleSolvers(0) As IntPtr
'    Dim puzzle(puzzleSolvers.Count - 1) As Puzzle_Basics
'    Dim images(puzzleSolvers.Count - 1) As cv.Mat
'    Dim puzzleOrder As New List(Of Int32())
'    Dim check As New OptionsCheckbox
'    Private Sub setup(ocvb As AlgorithmData)
'        For i = 0 To puzzleSolvers.Count - 1
'            puzzle(i).restartRequested = True
'            puzzle(i).Run(ocvb)
'            Dim hScrambled = GCHandle.Alloc(puzzle(i).scrambled.ToArray, GCHandleType.Pinned)
'            puzzleSolvers(i) = Puzzle_PieceCorrelation_Open(hScrambled.AddrOfPinnedObject, puzzle(i).scrambled.Count)
'            hScrambled.Free()
'            Dim orderedList(puzzle(i).scrambled.Count - 1) As Int32
'            For j = 0 To puzzle(i).scrambled.Count - 1
'                orderedList((j + 1) Mod puzzle(i).scrambled.Count) = j
'            Next
'            puzzleOrder.Add(orderedList)
'        Next
'    End Sub
'    Public Sub New(ocvb As AlgorithmData)
'        check.Setup(ocvb, 1)
'        check.Box(0).Text = "Restart Annealing"
'        If ocvb.parms.ShowOptions Then check.Show()

'        For i = 0 To puzzle.Count - 1
'            puzzle(i) = New Puzzle_Basics(ocvb)
'        Next
'        ocvb.desc = "Put the puzzle back together using annealing.  No guarantee it will solve the puzzle."
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        If ocvb.frameCount = 0 Then setup(ocvb)
'        For i = 0 To puzzle.Count - 1
'            puzzle(i).Run(ocvb)
'            images(i) = ocvb.result1.Clone()
'        Next

'        Dim energy(puzzleSolvers.Count - 1) As Double
'        Parallel.For(0, puzzleSolvers.Count - 1,
'         Sub(i)
'             Dim state As cv.Vec3i
'             Select Case i Mod 3 ' there are 4 sides to each puzzle piece.  Assign each thread to a different side.
'                 Case 0
'                     state = New cv.Vec3i(0, puzzle(0).scrambled(0).Width - 1, 0)
'                 Case 1
'                     state = New cv.Vec3i(0, 0, puzzle(0).scrambled(0).Width - 1)
'                 Case 2
'                     state = New cv.Vec3i(1, puzzle(0).scrambled(0).Height - 1, 0)
'                 Case 3
'                     state = New cv.Vec3i(1, 0, puzzle(0).scrambled(0).Height - 1)
'             End Select
'             Dim hOrder = GCHandle.Alloc(puzzleOrder(i), GCHandleType.Pinned)
'             Dim rgbData(images(i).Total * images(i).ElemSize - 1) As Byte
'             Marshal.Copy(images(i).Data, rgbData, 0, rgbData.Length)
'             Dim hImage = GCHandle.Alloc(rgbData, GCHandleType.Pinned)
'             Dim out As IntPtr = Puzzle_PieceCorrelation_Run(puzzleSolvers(i), hOrder.AddrOfPinnedObject, hImage.AddrOfPinnedObject, images(i).Rows, images(i).Cols, state)
'             hImage.Free()
'             hOrder.Free()
'             Dim msg = Marshal.PtrToStringAnsi(out)
'             Dim split As String() = Regex.Split(msg, "\W+")
'             energy(i) = CSng(split(split.Length - 2) + "." + split(split.Length - 1))

'             Console.WriteLine(msg)
'         End Sub)

'        Dim minEnergy As Double = Double.PositiveInfinity
'        Dim minIndex As Int32
'        For i = 0 To energy.Count - 1
'            If energy(i) < minEnergy Then
'                minEnergy = energy(i)
'                minIndex = i
'            End If
'        Next

'        ocvb.result1 = images(minIndex).Clone()
'        For i = 0 To puzzleOrder(minIndex).Count - 1
'            Dim sIndex = puzzleOrder(minIndex)(i)
'            Dim roi1 = puzzle(minIndex).scrambled(sIndex)
'            Dim roi2 = puzzle(minIndex).scrambled(i)
'            ocvb.result2(roi1) = ocvb.result1(roi2)
'        Next

'        Dim sameEnergy As Int32 = 1
'        Dim allClosed As Boolean
'        If puzzleSolvers.Count > 1 Then
'            For i = 1 To puzzleSolvers.Count - 1
'                If energy(0) = energy(i) Then sameEnergy += 1
'            Next
'            If sameEnergy = puzzleSolvers.Count Then allClosed = True
'        End If
'        If check.Box(0).Checked Or allClosed Then
'            check.Box(0).Checked = False
'            For i = 0 To puzzleSolvers.Count - 1
'                Puzzle_PieceCorrelation_Close(puzzleSolvers(i))
'            Next
'            setup(ocvb)
'        End If

'        ocvb.result1.SetTo(0)
'        ocvb.putText(New ActiveClass.TrueType("This algorithm is not completed", 10, 100, RESULT1))
'    End Sub
'    Public Sub Dispose() Implements IDisposable.Dispose
'        For i = 0 To puzzleSolvers.Count - 1
'            Puzzle_PieceCorrelation_Close(puzzleSolvers(i))
'            puzzle(i).Dispose()
'        Next
'        check.Dispose()
'    End Sub
'End Class




Public Class VBTest_Basics : Implements IDisposable
    Dim corr As MatchTemplate_Basics
    Dim puzzle As Puzzle_Basics
    Dim puzzleOrder As New List(Of Int32())
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData)
        check.Setup(ocvb, 1)
        check.Box(0).Text = "Restart Puzzle"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        corr = New MatchTemplate_Basics(ocvb)
        corr.externalUse = True
        corr.sliders.Visible = False

        puzzle = New Puzzle_Basics(ocvb)
        ocvb.desc = "Put the puzzle back together."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static roilist() As cv.Rect
        If check.Box(0).Checked Then
            check.Box(0).Checked = False
            puzzle.Run(ocvb)
            roilist = puzzle.grid.roiList.ToArray
        End If

        ' ocvb.result2.SetTo(0)

        Dim roi1 As cv.Rect
        Dim roi2 As cv.Rect
        Dim outCol As Integer
        Dim outRow As Integer = -2
        Dim tilesPerRow = puzzle.grid.tilesPerRow
        Dim tilesPerCol = puzzle.grid.tilesPerCol
        Dim width = puzzle.grid.roiList(0).Width
        Dim height = puzzle.grid.roiList(0).Height
        Static rowIncr = 2
        Dim nextROIlist(roilist.Count - tilesPerRow) As cv.Rect ' each pass will eliminate one row from the roilist.
        Dim nextIndex As Integer
        Dim flip As Boolean = False
        If rowIncr < tilesPerCol And roilist.Count = 20 Then
            For i = 0 To roilist.Count - 1
                roi1 = roilist(i)
                If roi1.Width = 0 Then Continue For
                Dim bestIndex = 0
                Dim bestCorr As Single = -1
                Dim correlation1 As Single = -1, correlation2 As Single = -1
                Dim bestCorrelation1 As Single = -1, bestCorrelation2 As Single = -1
                For j = 0 To roilist.Count - 1
                    roi2 = roilist(j)
                    If roi2.Width = 0 Or i = j Then Continue For
                    corr.sample1 = ocvb.result1(roi1).Row(height - 1)
                    corr.sample2 = ocvb.result1(roi2).Row(0)
                    corr.Run(ocvb)
                    correlation1 = corr.correlationMat.At(Of Single)(0, 0)
                    corr.sample1 = ocvb.result1(roi1).Row(0)
                    corr.sample2 = ocvb.result1(roi2).Row(height - 1)
                    corr.Run(ocvb)
                    correlation2 = corr.correlationMat.At(Of Single)(0, 0)
                    If correlation1 > bestCorrelation1 Then bestCorrelation1 = correlation1
                    If correlation2 > bestCorrelation2 Then bestCorrelation2 = correlation2
                    If bestCorr < Math.Max(correlation1, correlation2) Then
                        bestIndex = j
                        bestCorr = Math.Max(correlation1, correlation2)
                    End If
                Next
                Console.WriteLine("i = " + CStr(i) + " bestcorr = " + CStr(bestCorr))
                If bestCorr > 0.95 Then
                    If bestCorrelation1 < bestCorrelation2 Then flip = True Else flip = False
                    Dim topImage = If(flip, ocvb.result1(roilist(bestIndex)), ocvb.result1(roi1))
                    Dim botImage = If(flip, ocvb.result1(roi1), ocvb.result1(roilist(bestIndex)))
                    If outCol Mod tilesPerRow = 0 Then
                        outCol = 0
                        outRow += rowIncr
                    End If
                    topImage.CopyTo(ocvb.result2(New cv.Rect(outCol * width, outRow * height, width, height)))
                    If outRow < tilesPerRow Then
                        botImage.CopyTo(ocvb.result2(New cv.Rect(outCol * width, outRow * height + height, width, height)))
                        nextROIlist(nextIndex) = New cv.Rect(outCol * width, outRow * height, width, height * 2)
                    Else
                        nextROIlist(nextIndex) = New cv.Rect(outCol * width, outRow * height, width, height)
                    End If
                    roilist(i).Width = 0
                    roilist(bestIndex).Width = 0
                    outCol += 1
                    nextIndex += 1
                    'cv.Cv2.ImShow("result2", ocvb.result2)
                    'cv.Cv2.WaitKey()
                End If
            Next
            rowIncr += 1
            roilist = nextROIlist
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        check.Dispose()
        corr.Dispose()
    End Sub
End Class