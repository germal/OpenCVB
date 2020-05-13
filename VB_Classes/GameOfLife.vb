Imports cv = OpenCvSharp
' http://ptgmedia.pearsoncmg.com/images/0672320665/downloads/The%20Game%20of%20Life.html
Public Class GameOfLife_Basics
    Inherits ocvbClass
    Dim random As Random_Points
    Dim grid As cv.Mat
    Dim nextgrid As cv.Mat
    Dim factor = 8
    Dim generation As Integer
    Public population As Integer
    Private Function CountNeighbors(cellX As Integer, cellY As Integer) As Integer
        If cellX > 0 And cellY > 0 Then
            If grid.Get(Of Byte)(cellY - 1, cellX - 1) Then CountNeighbors += 1
            If grid.Get(Of Byte)(cellY - 1, cellX) Then CountNeighbors += 1
            If grid.Get(Of Byte)(cellY, cellX - 1) Then CountNeighbors += 1
        End If
        If cellX < grid.Width - 1 And cellY < grid.Height - 1 Then
            If grid.Get(Of Byte)(cellY + 1, cellX + 1) Then CountNeighbors += 1
            If grid.Get(Of Byte)(cellY + 1, cellX) Then CountNeighbors += 1
            If grid.Get(Of Byte)(cellY, cellX + 1) Then CountNeighbors += 1
        End If
        If cellX > 0 And cellY < grid.Height - 1 Then
            If grid.Get(Of Byte)(cellY + 1, cellX - 1) Then CountNeighbors += 1
        End If
        If cellX < grid.Width - 1 And cellY > 0 Then
            If grid.Get(Of Byte)(cellY - 1, cellX + 1) Then CountNeighbors += 1
        End If
        Return CountNeighbors
    End Function
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        grid = New cv.Mat(ocvb.color.Height / factor, ocvb.color.Width / factor, cv.MatType.CV_8UC1).SetTo(0)
        nextgrid = grid.Clone()

        random = New Random_Points(ocvb, caller)
        random.rangeRect = New cv.Rect(0, 0, grid.Width, grid.Height)
        random.sliders.TrackBar1.Value = grid.Width * grid.Height * 0.3 ' we want about 30% of cells filled.
        ocvb.desc = "Use OpenCV to implement the Game of Life"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static savePointCount As Integer
        If random.sliders.TrackBar1.Value <> savePointCount Or generation = 0 Then
            random.Run(ocvb)
            generation = 0
            savePointCount = random.sliders.TrackBar1.Value
            For i = 0 To random.Points.Count - 1
                grid.Set(Of Byte)(random.Points(i).Y, random.Points(i).X, 1)
            Next
        End If
        generation += 1

        population = 0
        For y = 0 To grid.Height - 1
            For x = 0 To grid.Width - 1
                Dim neighbors = CountNeighbors(x, y)
                If neighbors = 2 Or neighbors = 3 Then
                    If neighbors = 2 Then
                        nextgrid.Set(Of Byte)(y, x, grid.Get(Of Byte)(y, x))
                    Else
                        nextgrid.Set(Of Byte)(y, x, 1)
                    End If
                Else
                    nextgrid.Set(Of Byte)(y, x, 0)
                End If
                If nextgrid.Get(Of Byte)(y, x) Then
                    Dim pt = New cv.Point(x, y) * factor
                    dst1.Circle(pt, factor / 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                    population += 1
                End If
            Next
        Next

        Static lastPopulation As Integer
        Const countInit = 200
        Static countdown As Integer = countInit
        Dim countdownText = ""
        If lastPopulation = population Then
            countdown -= 1
            countdownText = " Restart in " + CStr(countdown)
            If countdown = 0 Then
                countdownText = ""
                generation = 0
                countdown = countInit
            End If
        End If
        lastPopulation = population
        label1 = "Population " + CStr(population) + " Generation = " + CStr(generation) + countdownText
        grid = nextgrid.Clone()
    End Sub
End Class






Public Class GameOfLife_Population
    Inherits ocvbClass
    Dim plot As Plot_OverTime
    Dim game As GameOfLife_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        game = New GameOfLife_Basics(ocvb, caller)

        plot = New Plot_OverTime(ocvb, caller)
        plot.dst1 = dst2
        plot.maxScale = 2000
        plot.plotCount = 1

        ocvb.desc = "Show Game of Life display with plot of population"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        game.Run(ocvb)

        plot.plotData = New cv.Scalar(game.population, 0, 0)
        plot.Run(ocvb)
    End Sub
End Class
