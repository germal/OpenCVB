Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Public Class Plot_Basics
    Inherits ocvbClass
    Dim plot As Plot_Basics_CPP
    Dim hist As Histogram_Basics
    Public plotCount As Int32 = 3
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        hist = New Histogram_Basics(ocvb)
        hist.plotRequested = True

        plot = New Plot_Basics_CPP(ocvb)

        label1 = "Plot of grayscale histogram"
        label2 = "Same Data but using OpenCV C++ plot"
        ocvb.desc = "Plot data provided in src Mat"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.src = src
        hist.plotColors(0) = cv.Scalar.White
        hist.Run(ocvb)
        dst1 = hist.dst1

        ReDim plot.srcX(hist.histRaw(0).Rows - 1)
        ReDim plot.srcY(hist.histRaw(0).Rows - 1)
        For i = 0 To plot.srcX.Length - 1
            plot.srcX(i) = i
            plot.srcY(i) = hist.histRaw(0).Get(Of Single)(i, 0)
        Next
        plot.Run(ocvb)
        dst2 = plot.dst1
        label1 = hist.label1
    End Sub
End Class




' https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
Public Class Plot_Basics_CPP
    Inherits ocvbClass
    Public srcX() As Double
    Public srcY() As Double
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Demo the use of the integrated 2D plot available in OpenCV (only accessible in C++)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim maxX As Double = Double.MinValue
        Dim minX As Double = Double.MaxValue
        Dim maxY As Double = Double.MinValue
        Dim minY As Double = Double.MaxValue

        If standalone Then
            ReDim srcX(50 - 1)
            ReDim srcY(50 - 1)
            For i = 0 To srcX.Length - 1
                srcX(i) = i
                srcY(i) = i * i * i
            Next
        End If
        For i = 0 To srcX.Length - 1
            If srcX(i) > maxX Then maxX = CInt(srcX(i))
            If srcX(i) < minX Then minX = CInt(srcX(i))
            If srcY(i) > maxY Then maxY = CInt(srcY(i))
            If srcY(i) < minY Then minY = CInt(srcY(i))
        Next

        Dim plotData(dst1.Total * dst1.ElemSize - 1) As Byte
        Dim handlePlot = GCHandle.Alloc(plotData, GCHandleType.Pinned)
        Dim handleX = GCHandle.Alloc(srcX, GCHandleType.Pinned)
        Dim handleY = GCHandle.Alloc(srcY, GCHandleType.Pinned)

        Plot_OpenCVBasics(handleX.AddrOfPinnedObject, handleY.AddrOfPinnedObject, srcX.Length - 1, handlePlot.AddrOfPinnedObject, dst1.Rows, dst1.Cols)

        Marshal.Copy(plotData, 0, dst1.Data, plotData.Length)
        handlePlot.Free()
        handleX.Free()
        handleY.Free()
        label1 = "x-Axis: " + CStr(minX) + " to " + CStr(maxX) + vbTab + " y-axis: " + CStr(minY) + " to " + CStr(maxY)
    End Sub
End Class






Public Class Plot_OverTime
    Inherits ocvbClass
    Public plotData As cv.Scalar
    Public plotCount As Int32 = 3
    Public plotColors() As cv.Scalar = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.White}
    Public backColor = cv.Scalar.Aquamarine
    Public minScale As Int32 = 50
    Public maxScale As Int32 = 200
    Public plotTriggerRescale = 50
    Public columnIndex As Int32
    Public offChartCount As Integer
    Public lastXdelta As New List(Of cv.Scalar)
    Public topBottomPad As Integer
    Dim myStopWatch As Stopwatch
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Reset the plot scale"
        check.Box(0).Checked = True

        sliders.setupTrackBar1(ocvb, caller, "Plot Pixel Height", 1, 40, 4)
        sliders.setupTrackBar2("Plot Pixel Width", 1, 40, 4)
        sliders.setupTrackBar3("Plot (time) Font Size x10", 1, 20, 10)
        ocvb.desc = "Plot an input variable over time"
        myStopWatch = Stopwatch.StartNew()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Const plotSeriesCount = 100
        lastXdelta.Add(plotData)
        Dim pixelHeight = CInt(sliders.TrackBar1.Value)
        Dim pixelWidth = CInt(sliders.TrackBar2.Value)
        If ocvb.frameCount = 0 Then dst1.SetTo(0)
        If columnIndex + pixelWidth >= ocvb.color.Width Then
            dst1.ColRange(columnIndex, ocvb.color.Width).SetTo(backColor)
            columnIndex = 0
        End If
        dst1.ColRange(columnIndex, columnIndex + pixelWidth).SetTo(backColor)
        If standalone Then plotData = src.Mean()

        For i = 0 To plotCount - 1
            If Math.Floor(plotData.Item(i)) < minScale Or Math.Ceiling(plotData.Item(i)) > maxScale Then
                offChartCount += 1
                Exit For
            End If
        Next

        ' if enough points are off the charted area or if manually requested, then redo the scale.
        If (offChartCount > plotTriggerRescale Or check.Box(0).Checked) And lastXdelta.Count >= plotSeriesCount Then
            check.Box(0).Checked = False
            dst1.SetTo(0)
            maxScale = Int32.MinValue
            minScale = Int32.MaxValue
            For i = 0 To lastXdelta.Count - 1
                Dim nextVal = lastXdelta.Item(i)
                For j = 0 To plotCount - 1
                    If Math.Floor(nextVal.Item(j)) < minScale Then minScale = Math.Floor(nextVal.Item(j))
                    If Math.Floor(nextVal.Item(j)) > maxScale Then maxScale = Math.Ceiling(nextVal.Item(j))
                Next
            Next
            If minScale < 5 And minScale > 0 Then minScale = 0 ' nice location...
            minScale -= topBottomPad
            maxScale += topBottomPad
            lastXdelta.Clear()
            offChartCount = 0
            columnIndex = 0 ' restart at the left side of the chart
        End If
        If lastXdelta.Count >= plotSeriesCount Then lastXdelta.RemoveAt(0)

        Dim rectSize = New cv.Size2f(pixelWidth, pixelHeight)
        Dim ellipseSize = New cv.Size(pixelWidth, pixelHeight * 2)
        For i = 0 To plotCount - 1
            Dim y = 1 - (plotData.Item(i) - minScale) / (maxScale - minScale)
            y *= ocvb.color.Height - 1
            Dim c As New cv.Point(columnIndex - pixelWidth, y - pixelHeight)
            Dim rect = New cv.Rect(c.X, c.Y, pixelWidth * 2, pixelHeight * 2)
            Select Case i
                Case 0
                    dst1.Circle(c, pixelWidth, plotColors(i), -1, cv.LineTypes.AntiAlias)
                Case 1
                    dst1.Rectangle(rect, plotColors(i), -1)
                Case 2
                    dst1.Ellipse(c, ellipseSize, 0, 0, 360, plotColors(i), -1)
                Case 3
                    Dim rotatedRect = New cv.RotatedRect(c, rectSize, 45)
                    drawRotatedRectangle(rotatedRect, dst2, plotColors(i))
            End Select
        Next

        Static lastSeconds As Double
        Dim nextWatchVal = myStopWatch.ElapsedMilliseconds
        If nextWatchVal - lastSeconds > 1000 Then
            lastSeconds = nextWatchVal
            dst1.Line(New cv.Point(columnIndex, 0), New cv.Point(columnIndex, dst1.Height), cv.Scalar.White, 1)
        End If

        columnIndex += pixelWidth
        dst1.Col(columnIndex).SetTo(0)
        If standalone Then label1 = "RGB Means: blue = " + Format(plotData.Item(0), "#0.0") + " green = " + Format(plotData.Item(1), "#0.0") + " red = " + Format(plotData.Item(2), "#0.0")
        AddPlotScale(dst1, minScale - topBottomPad, maxScale + topBottomPad, sliders.TrackBar3.Value / 10)
    End Sub
End Class





Public Class Plot_Histogram
    Inherits ocvbClass
    Public hist As New cv.Mat
    Public bins As Int32 = 50
    Public minRange As Int32 = 0
    Public maxRange As Int32 = 255
    Public backColor As cv.Scalar = cv.Scalar.Red
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Histogram Font Size x10", 1, 20, 10)
        ocvb.desc = "Plot histogram data with a stable scale at the left of the image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim dimensions() = New Integer() {bins}
            Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}
            cv.Cv2.CalcHist(New cv.Mat() {gray}, New Integer() {0}, New cv.Mat(), hist, 1, dimensions, ranges)
        End If
        dst1.SetTo(backColor)
        Dim barWidth = Int(dst1.Width / hist.Rows)
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)

        maxVal = Math.Round(maxVal / 1000, 0) * 1000 + 1000

        Static savedMaxVal = maxVal
        If maxVal < 0 Then maxVal = savedMaxVal
        If Math.Abs((maxVal - savedMaxVal)) / maxVal < 0.2 Then maxVal = savedMaxVal Else savedMaxVal = Math.Max(maxVal, savedMaxVal)
        If maxVal > 0 And hist.Rows > 0 Then
            Dim incr = CInt(255 / hist.Rows)
            For i = 0 To hist.Rows - 1
                Dim offset = hist.Get(Of Single)(i)
                If Single.IsNaN(offset) Then offset = 0
                Dim h = CInt(offset * dst1.Height / maxVal)
                Dim color As cv.Scalar = cv.Scalar.Black
                If hist.Rows <= 255 Then color = cv.Scalar.All((i Mod 255) * incr)
                cv.Cv2.Rectangle(dst1, New cv.Rect(i * barWidth, dst1.Height - h, barWidth, h), color, -1)
            Next
            AddPlotScale(dst1, 0, maxVal, sliders.TrackBar1.Value / 10)
        End If
    End Sub
End Class





Module Plot_OpenCV_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Plot_OpenCVBasics(inX As IntPtr, inY As IntPtr, inLen As Int32, dstptr As IntPtr, rows As Int32, cols As Int32)
    End Sub

    Public Sub AddPlotScale(dst1 As cv.Mat, minVal As Double, maxVal As Double, fontsize As Double)
        ' draw a scale along the side
        Dim spacer = CInt(dst1.Height / 5)
        Dim spaceVal = CInt((maxVal - minVal) / 5)
        If spaceVal < 1 Then spaceVal = 1
        For i = 0 To 4
            Dim pt1 = New cv.Point(0, spacer * i)
            Dim pt2 = New cv.Point(dst1.Width, spacer * i)
            dst1.Line(pt1, pt2, cv.Scalar.White, 1)
            If i = 0 Then pt2.Y += 10
            cv.Cv2.PutText(dst1, Format(maxVal - spaceVal * i, "###,###,##0"), New cv.Point(pt1.X + 5, pt1.Y - 4),
                           cv.HersheyFonts.HersheyComplexSmall, fontsize, cv.Scalar.Black, 2)
        Next
    End Sub
End Module






Public Class Plot_Depth
    Inherits ocvbClass
    Dim plot As Plot_Basics_CPP
    Dim hist As Histogram_Depth
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        hist = New Histogram_Depth(ocvb)
        hist.sliders.TrackBar1.Minimum = 3  ' but in the opencv plot contrib code - OBO.  This prevents encountering it.  Should be ok!
        hist.sliders.TrackBar1.Value = 200 ' a lot more bins in a plot than a bar chart.
        hist.trim.sliders.TrackBar2.Value = 5000 ' up to x meters.

        plot = New Plot_Basics_CPP(ocvb)

        ocvb.desc = "Show depth using OpenCV's plot format with variable bins."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.Run(ocvb)
        Dim inRangeMin = hist.trim.sliders.TrackBar1.Value
        Dim inRangeMax = hist.trim.sliders.TrackBar2.Value
        ReDim plot.srcX(hist.plotHist.hist.Rows - 1)
        ReDim plot.srcY(hist.plotHist.hist.Rows - 1)
        For i = 0 To plot.srcX.Length - 1
            plot.srcX(i) = inRangeMin + i * (inRangeMax - inRangeMin) / plot.srcX.Length
            plot.srcY(i) = hist.plotHist.hist.Get(Of Single)(i, 0)
        Next
        plot.Run(ocvb)
        dst1 = plot.dst1

        label1 = "histogram: " + plot.label1
        Dim Split = Regex.Split(label1, "\W+")
        Dim lineCount = CInt(Split(5) / 1000)
        If lineCount > 0 Then
            Dim meterDepth = CInt(src.Width / lineCount)
            For i = 1 To lineCount
                Dim x = i * meterDepth
                dst1.Line(New cv.Point(x, 0), New cv.Point(x, src.Height), cv.Scalar.White, 1)
                cv.Cv2.PutText(dst1, Format(i, "0") + "m", New cv.Point(x + 5, src.Height - 10), cv.HersheyFonts.HersheyComplexSmall, 0.7, cv.Scalar.White, 2)
            Next
        End If
    End Sub
End Class
