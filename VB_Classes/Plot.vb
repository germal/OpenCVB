Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Plot_OverTime : Implements IDisposable
    Public sliders As New OptionsSliders
    Public plotData As cv.Scalar
    Public plotCount As Int32 = 3
    Public externalUse As Boolean
    Public dst As cv.Mat
    Public minVal As Int32 = 0
    Public maxVal As Int32 = 250
    Public columnIndex As Int32
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Pixel Height", 1, 40, 20)
        sliders.setupTrackBar2(ocvb, "Pixel Width", 1, 40, 5)
        sliders.setupTrackBar3(ocvb, "Plot (time) Font Size x10", 1, 20, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Plot an input variable over time"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount = 0 Then
            If externalUse = False Then dst = ocvb.result1
        End If
        Dim pixelHeight = CInt(sliders.TrackBar1.Value)
        Dim pixelWidth = CInt(sliders.TrackBar2.Value)
        If columnIndex + pixelWidth >= ocvb.color.Width Then
            dst.ColRange(columnIndex, ocvb.color.Width).SetTo(0)
            columnIndex = 0
        End If
        dst.ColRange(columnIndex, columnIndex + pixelWidth).SetTo(0)
        If externalUse = False Then plotData = ocvb.color.Mean()

        For i = 0 To plotCount - 1
            Dim color = Choose(i Mod 3 + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red)
            If plotData.Item(i) >= minVal And plotData.Item(i) <= maxVal Then
                Dim y = 1 - (plotData.Item(i) - minVal) / (maxVal - minVal)
                y *= ocvb.color.Height - 1
                If i = 1 Then
                    ' plot the green values as circles
                    dst.Circle(New cv.Point(columnIndex - pixelWidth, y - pixelHeight), pixelWidth, color, -1, cv.LineTypes.AntiAlias)
                Else
                    dst.Rectangle(New cv.Rect(columnIndex - pixelWidth, y - pixelHeight, pixelWidth * 2, pixelHeight * 2), color, -1)
                End If
            End If
        Next
        columnIndex += pixelWidth
        If externalUse = False Then ocvb.label1 = "PlotData: x = " + Format(plotData.Item(0), "#0.0") + " y = " + Format(plotData.Item(1), "#0.0") + " z = " + Format(plotData.Item(2), "#0.0")
        AddPlotScale(dst, minVal, maxVal, sliders.TrackBar3.Value / 10)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Plot_Histogram : Implements IDisposable
    Public sliders As New OptionsSliders
    Public hist As New cv.Mat
    Public dst As New cv.Mat
    Public bins As Int32 = 50
    Public minRange As Int32 = 0
    Public maxRange As Int32 = 255
    Public backColor As cv.Scalar = cv.Scalar.Red
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Histogram Font Size x10", 1, 20, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Plot histogram data with a stable scale at the left of the image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            Dim dimensions() = New Integer() {bins}
            Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}
            cv.Cv2.CalcHist(New cv.Mat() {gray}, New Integer() {0}, New cv.Mat(), hist, 1, dimensions, ranges)
            dst = ocvb.result1
        End If
        Dim barWidth = Int(dst.Width / hist.Rows)
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)

        maxVal = Math.Round(maxVal / 1000, 0) * 1000 + 1000

        Static savedMaxVal = maxVal
        If maxVal < 0 Then maxVal = savedMaxVal
        If Math.Abs((maxVal - savedMaxVal)) / maxVal < 0.2 Then maxVal = savedMaxVal Else savedMaxVal = Math.Max(maxVal, savedMaxVal)
        dst.SetTo(backColor)
        If maxVal > 0 And hist.Rows > 0 Then
            Dim incr = CInt(255 / hist.Rows)
            For i = 0 To hist.Rows - 1
                Dim offset = hist.Get(Of Single)(i)
                If Single.IsNaN(offset) Then offset = 0
                Dim h = CInt(offset * dst.Height / maxVal)
                Dim color As cv.Scalar = cv.Scalar.Black
                If hist.Rows <= 255 Then color = cv.Scalar.All((i Mod 255) * incr)
                cv.Cv2.Rectangle(dst, New cv.Rect(i * barWidth, dst.Height - h, barWidth, h), color, -1)
            Next
            AddPlotScale(dst, 0, maxVal, sliders.TrackBar1.Value / 10)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class





Module Plot_OpenCV_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Plot_OpenCVBasics(inX As IntPtr, inY As IntPtr, inLen As Int32, dstptr As IntPtr, rows As Int32, cols As Int32)
    End Sub

    Public Sub AddPlotScale(dst As cv.Mat, minVal As Double, maxVal As Double, fontsize As Double)
        ' draw a scale along the side
        Dim spacer = CInt(dst.Height / 5)
        Dim spaceVal = CInt((maxVal - minVal) / 5)
        If spaceVal < 1 Then spaceVal = 1
        For i = 0 To 4
            Dim pt1 = New cv.Point(0, spacer * i)
            Dim pt2 = New cv.Point(10, spacer * i)
            dst.Line(pt1, pt2, cv.Scalar.White, 3)
            If i = 0 Then pt2.Y += 10
            cv.Cv2.PutText(dst, Format(maxVal - spaceVal * i, "###,###,##0"), New cv.Point(pt2.X + 5, pt2.Y + 8),
                           cv.HersheyFonts.HersheyComplexSmall, fontsize, cv.Scalar.White, 2)
        Next
    End Sub
End Module



' https://github.com/opencv/opencv_contrib/blob/master/modules/plot/samples/plot_demo.cpp
Public Class Plot_Basics_CPP : Implements IDisposable
    Public srcX(49) As Double
    Public srcY(49) As Double
    Public externalUse As Boolean
    Public dst As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Demo the use of the integrated 2D plot available in OpenCV (only accessible in C++)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim maxX As Double = Double.MinValue
        Dim minX As Double = Double.MaxValue
        Dim maxY As Double = Double.MinValue
        Dim minY As Double = Double.MaxValue
        Dim plotData(ocvb.color.Total * ocvb.color.ElemSize - 1) As Byte
        Dim handlePlot = GCHandle.Alloc(plotData, GCHandleType.Pinned)

        If externalUse = False Then
            For i = 0 To srcX.Length - 1
                srcX(i) = i
                srcY(i) = i * i * i
            Next
            dst = ocvb.result1
        End If
        For i = 0 To srcX.Length - 1
            If srcX(i) > maxX Then maxX = CInt(srcX(i))
            If srcX(i) < minX Then minX = CInt(srcX(i))
            If srcY(i) > maxY Then maxY = CInt(srcY(i))
            If srcY(i) < minY Then minY = CInt(srcY(i))
        Next

        Dim handleX = GCHandle.Alloc(srcX, GCHandleType.Pinned)
        Dim handleY = GCHandle.Alloc(srcY, GCHandleType.Pinned)

        Plot_OpenCVBasics(handleX.AddrOfPinnedObject, handleY.AddrOfPinnedObject, srcX.Length - 1, handlePlot.AddrOfPinnedObject, ocvb.color.Rows, ocvb.color.Cols)

        Marshal.Copy(plotData, 0, dst.Data, plotData.Length)
        handlePlot.Free()
        handleX.Free()
        handleY.Free()
        ocvb.label1 = "x-Axis: " + CStr(minX) + " to " + CStr(maxX) + vbTab + " y-axis: " + CStr(minY) + " to " + CStr(maxY)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class Plot_Basics : Implements IDisposable
    Dim plot As Plot_Basics_CPP
    Dim hist As Histogram_Basics
    Public plotCount As Int32 = 3
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        hist = New Histogram_Basics(ocvb)
        hist.externalUse = True
        hist.plotRequested = True

        plot = New Plot_Basics_CPP(ocvb)
        plot.externalUse = True

        ocvb.label1 = "Plot of grayscale histogram"
        ocvb.label2 = "Same Data but using OpenCV C++ plot"
        ocvb.desc = "Plot data provided in src Mat"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse Then
            If hist.sliders.Visible Then hist.sliders.Hide()
        Else
            hist.src = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            hist.plotColors(0) = cv.Scalar.White
            hist.Run(ocvb)
            plot.dst = ocvb.result2
            ReDim plot.srcX(hist.histRaw(0).Rows - 1)
            ReDim plot.srcY(hist.histRaw(0).Rows - 1)
            For i = 0 To plot.srcX.Length - 1
                plot.srcX(i) = i
                plot.srcY(i) = hist.histRaw(0).At(Of Single)(i, 0)
            Next
            plot.Run(ocvb)
            ocvb.label1 = "histogram with " + ocvb.label1
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        plot.Dispose()
        hist.Dispose()
    End Sub
End Class





Public Class Plot_Depth : Implements IDisposable
    Dim plot As Plot_Basics_CPP
    Dim hist As Histogram_Depth
    Public Sub New(ocvb As AlgorithmData)
        hist = New Histogram_Depth(ocvb)
        hist.externalUse = True
        hist.sliders.TrackBar1.Minimum = 3  ' but in the opencv plot contrib code - OBO.  This prevents encountering it.  Should be ok!
        hist.sliders.TrackBar1.Value = 200 ' a lot more bins in a plot than a bar chart.
        hist.inrange.sliders.TrackBar2.Value = 5000 ' up to x meters.

        plot = New Plot_Basics_CPP(ocvb)
        plot.externalUse = True

        ocvb.desc = "Show depth in a plot format with variable bins."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.Run(ocvb)
        plot.dst = ocvb.result1
        ReDim plot.srcX(hist.plotHist.hist.Rows - 1)
        ReDim plot.srcY(hist.plotHist.hist.Rows - 1)
        Dim inRangeMin = hist.inrange.sliders.TrackBar1.Value
        Dim inRangeMax = hist.inrange.sliders.TrackBar2.Value
        For i = 0 To plot.srcX.Length - 1
            plot.srcX(i) = inRangeMin + i * (inRangeMax - inRangeMin) / plot.srcX.Length
            plot.srcY(i) = hist.plotHist.hist.At(Of Single)(i, 0)
        Next
        plot.Run(ocvb)
        ocvb.label1 = "histogram with " + ocvb.label1
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        plot.Dispose()
        hist.Dispose()
    End Sub
End Class