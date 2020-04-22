Imports cv = OpenCvSharp
Public Class GameOfLife_Basics : Implements IDisposable
    Dim random As Random_Points
    Dim grid As cv.Mat
    Dim nextgrid As cv.Mat
    Dim factor = 8
    Dim generation As Integer
    Private Function CountNeighbors(cellX As Integer, cellY As Integer) As Integer
        Dim count As Integer
        If cellX > 0 And cellY > 0 Then
            If grid.At(Of Byte)(cellY - 1, cellX - 1) Then count += 1
            If grid.At(Of Byte)(cellY - 1, cellX) Then count += 1
            If grid.At(Of Byte)(cellY, cellX - 1) Then count += 1
        End If
        If cellX < grid.Width And cellY < grid.Height Then
            If grid.At(Of Byte)(cellY + 1, cellX + 1) Then count += 1
            If grid.At(Of Byte)(cellY + 1, cellX) Then count += 1
            If grid.At(Of Byte)(cellY, cellX + 1) Then count += 1
        End If
        If cellX > 0 And cellY < grid.Height Then
            If grid.At(Of Byte)(cellY + 1, cellX - 1) Then count += 1
        End If
        If cellX < grid.Width And cellY > 0 Then
            If grid.At(Of Byte)(cellY - 1, cellX + 1) Then count += 1
        End If
        Return count
    End Function
    Public Sub New(ocvb As AlgorithmData)
        grid = New cv.Mat(ocvb.color.Height / factor, ocvb.color.Width / factor, cv.MatType.CV_8UC1).SetTo(0)
        nextgrid = grid.Clone()

        random = New Random_Points(ocvb)
        random.externalUse = True
        random.rangeRect = New cv.Rect(0, 0, grid.Width, grid.Height)
        random.sliders.TrackBar1.Value = grid.Width * grid.Height * 0.3 ' we want about 30% of cells filled.
        ocvb.desc = "Use OpenCV to implement the Game of Life"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static savePointCount As Integer
        If ocvb.frameCount = 0 Or random.sliders.TrackBar1.Value <> savePointCount Then
            random.Run(ocvb)
            generation = 0
            savePointCount = random.sliders.TrackBar1.Value
            For i = 0 To random.Points.Count - 1
                grid.Set(Of Byte)(random.Points(i).Y, random.Points(i).X, 1)
            Next
        End If
        generation += 1

        ocvb.result1.SetTo(0)
        For y = 0 To grid.Height - 1
            For x = 0 To grid.Width - 1
                Dim neighbors = CountNeighbors(x, y)
                If neighbors = 2 Or neighbors = 3 Then
                    If neighbors = 2 Then
                        nextgrid.Set(Of Byte)(y, x, grid.At(Of Byte)(y, x))
                    Else
                        nextgrid.Set(Of Byte)(y, x, 1)
                    End If
                Else
                    nextgrid.Set(Of Byte)(y, x, 0)
                End If
                If nextgrid.At(Of Byte)(y, x) Then
                    Dim pt = New cv.Point(x, y) * factor
                    ocvb.result1.Circle(pt, factor / 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                End If
            Next
        Next

        Static saveEarlierGrid = grid.Clone().SetTo(1)

        Dim tmp As New cv.Mat
        cv.Cv2.Subtract(saveEarlierGrid, nextgrid, tmp)
        saveEarlierGrid = grid.Clone()
        Static countDownText As String = ""
        If tmp.CountNonZero() = 0 Then
            Static countDown = 200
            countDown -= 1
            countDownText = " countdown = " + CStr(countDown)
            If countDown = 0 Then
                savePointCount = -1 ' let's start a new game.  Nothing is really moving....
                countDown = 200
            End If
        End If
        grid = nextgrid.Clone()
        ocvb.label1 = "Generation = " + CStr(generation) + countDownText
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        random.Dispose()
    End Sub
End Class