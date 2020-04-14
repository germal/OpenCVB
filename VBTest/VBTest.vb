Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports VB_Classes

'Public Class VBTest_Basics : Implements IDisposable
'    Dim sobel As VB_Classes.Edges_Canny
'    Public Sub New(ocvb As AlgorithmData)
'        ocvb.name = "VBTest_Basics"
'        ocvb.label1 = "VBTest_Basics"
'        ocvb.desc = "Insert and debug new experiments here and then migrate them to the VB_Classes which is compiled in Release mode."
'        sobel = New VB_Classes.Edges_Canny(ocvb)
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        sobel.Run(ocvb)
'        'ocvb.putText(New ActiveClass.TrueType("Test", 10, 125))
'    End Sub
'    Public Sub Dispose() Implements IDisposable.Dispose
'        sobel.Dispose()
'    End Sub
'End Class







Module puzzlePiece_Exports
    Public Const RESULT1 = 2
    Public Const RESULT2 = 3

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Puzzle_PieceCorrelation_Open(puzzlePieces As IntPtr, count As Int32) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Puzzle_PieceCorrelation_Close(saPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Puzzle_PieceCorrelation_Run(saPtr As IntPtr, puzzleOrder As IntPtr, img As IntPtr, rows As Int32, cols As Int32, state As cv.Vec3i) As IntPtr
    End Function
End Module






Public Class VBTest_Basics : Implements IDisposable
    Dim puzzleSolvers(0) As IntPtr
    Dim puzzle(puzzleSolvers.Count - 1) As Puzzle_Basics
    Dim images(puzzleSolvers.Count - 1) As cv.Mat
    Dim puzzleOrder As New List(Of Int32())
    Dim check As New OptionsCheckbox
    Private Sub setup(ocvb As AlgorithmData)
        For i = 0 To puzzleSolvers.Count - 1
            puzzle(i).restartRequested = True
            puzzle(i).Run(ocvb)
            Dim hScrambled = GCHandle.Alloc(puzzle(i).scrambled.ToArray, GCHandleType.Pinned)
            puzzleSolvers(i) = Puzzle_PieceCorrelation_Open(hScrambled.AddrOfPinnedObject, puzzle(i).scrambled.Count)
            hScrambled.Free()
            Dim orderedList(puzzle(i).scrambled.Count - 1) As Int32
            For j = 0 To puzzle(i).scrambled.Count - 1
                orderedList((j + 1) Mod puzzle(i).scrambled.Count) = j
            Next
            puzzleOrder.Add(orderedList)
        Next
    End Sub
    Public Sub New(ocvb As AlgorithmData)
        check.Setup(ocvb, 1)
        check.Box(0).Text = "Restart Annealing"
        If ocvb.parms.ShowOptions Then check.Show()

        For i = 0 To puzzle.Count - 1
            puzzle(i) = New Puzzle_Basics(ocvb)
        Next
        ocvb.desc = "Put the puzzle back together using annealing.  No guarantee it will solve the puzzle.  This is UNFINISHED!"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount = 0 Then setup(ocvb)
        For i = 0 To puzzle.Count - 1
            puzzle(i).Run(ocvb)
            images(i) = ocvb.result1.Clone()
        Next

        Dim energy(puzzleSolvers.Count - 1) As Double
        Parallel.For(0, puzzleSolvers.Count - 1,
         Sub(i)
             Dim state As cv.Vec3i
             Select Case i Mod 3 ' there are 4 sides to each puzzle piece.  Assign each thread to a different side.
                 Case 0
                     state = New cv.Vec3i(0, puzzle(0).scrambled(0).Width - 1, 0)
                 Case 1
                     state = New cv.Vec3i(0, 0, puzzle(0).scrambled(0).Width - 1)
                 Case 2
                     state = New cv.Vec3i(1, puzzle(0).scrambled(0).Height - 1, 0)
                 Case 3
                     state = New cv.Vec3i(1, 0, puzzle(0).scrambled(0).Height - 1)
             End Select
             Dim hOrder = GCHandle.Alloc(puzzleOrder(i), GCHandleType.Pinned)
             Dim rgbData(images(i).Total * images(i).ElemSize - 1) As Byte
             Marshal.Copy(images(i).Data, rgbData, 0, rgbData.Length)
             Dim hImage = GCHandle.Alloc(rgbData, GCHandleType.Pinned)
             Dim out As IntPtr = Puzzle_PieceCorrelation_Run(puzzleSolvers(i), hOrder.AddrOfPinnedObject, hImage.AddrOfPinnedObject, images(i).Rows, images(i).Cols, state)
             hImage.Free()
             hOrder.Free()
             Dim msg = Marshal.PtrToStringAnsi(out)
             Dim split As String() = Regex.Split(msg, "\W+")
             energy(i) = CSng(split(split.Length - 2) + "." + split(split.Length - 1))

             Console.WriteLine(msg)
         End Sub)

        Dim minEnergy As Double = Double.PositiveInfinity
        Dim minIndex As Int32
        For i = 0 To energy.Count - 1
            If energy(i) < minEnergy Then
                minEnergy = energy(i)
                minIndex = i
            End If
        Next

        ocvb.result1 = images(minIndex).Clone()
        For i = 0 To puzzleOrder(minIndex).Count - 1
            Dim sIndex = puzzleOrder(minIndex)(i)
            Dim roi1 = puzzle(minIndex).scrambled(sIndex)
            Dim roi2 = puzzle(minIndex).scrambled(i)
            ocvb.result2(roi1) = ocvb.result1(roi2)
        Next

        Dim sameEnergy As Int32 = 1
        Dim allClosed As Boolean
        If puzzleSolvers.Count > 1 Then
            For i = 1 To puzzleSolvers.Count - 1
                If energy(0) = energy(i) Then sameEnergy += 1
            Next
            If sameEnergy = puzzleSolvers.Count Then allClosed = True
        End If
        If check.Box(0).Checked Or allClosed Then
            check.Box(0).Checked = False
            For i = 0 To puzzleSolvers.Count - 1
                Puzzle_PieceCorrelation_Close(puzzleSolvers(i))
            Next
            setup(ocvb)
        End If

        ocvb.result1.SetTo(0)
        ocvb.putText(New ActiveClass.TrueType("This algorithm is not completed", 10, 100, RESULT1))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        For i = 0 To puzzleSolvers.Count - 1
            Puzzle_PieceCorrelation_Close(puzzleSolvers(i))
            puzzle(i).Dispose()
        Next
        check.Dispose()
    End Sub
End Class