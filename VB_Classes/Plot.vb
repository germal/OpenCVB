Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Plot_OverTime : Implements IDisposable
    Public sliders As New OptionsSliders
    Public plotData As cv.Scalar
    Public plotCount As Int32 = 3
    Public externalUse As Boolean
    Public dst As cv.Mat
    Public maxVal As Int32 = 250
    Dim columnIndex As Int32
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Pixel Height", 1, 40, 20)
        sliders.setupTrackBar2(ocvb, "Pixel Width", 1, 40, 5)
        sliders.setupTrackBar3(ocvb, "Scale Font Size x10", 1, 20, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Plot an input variable over time"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount = 0 Then
            If externalUse = False Then dst = ocvb.result1
        End If
        Dim pixelHeight = CInt((sliders.TrackBar1.Value + 1) / 2)
        Dim pixelWidth = CInt((sliders.TrackBar2.Value + 1) / 2)
        If columnIndex + pixelWidth >= ocvb.color.Width Then
            dst.ColRange(columnIndex, ocvb.color.Width).SetTo(0)
            columnIndex = 0
        End If
        dst.ColRange(columnIndex, columnIndex + pixelWidth).SetTo(0)
        If externalUse = False Then plotData = ocvb.color.Mean()
        For i = 0 To plotCount - 1
            Dim color = Choose(i Mod 3 + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red)
            Dim y = 1 - plotData.Item(i) / (maxVal - pixelHeight * 2)
            y *= ocvb.color.Height - pixelHeight
            dst.Rectangle(New cv.Rect(columnIndex - pixelWidth, y - pixelHeight, pixelWidth * 2, pixelHeight * 2), color, -1)
        Next
        columnIndex += pixelWidth
        If externalUse = False Then ocvb.label1 = "PlotData: x = " + Format(plotData.Item(0), "#0.0") + " y = " + Format(plotData.Item(1), "#0.0") + " z = " + Format(plotData.Item(2), "#0.0")
        AddPlotScale(dst, maxVal, sliders.TrackBar3.Value / 10)
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
        sliders.setupTrackBar1(ocvb, "Plot Scale Font Size x10", 1, 20, 10)
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
            AddPlotScale(dst, maxVal, sliders.TrackBar1.Value / 10)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class





Module Plot_OpenCV_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Plot_OpenCVBasics(plota As IntPtr, plotb As IntPtr, rows As Int32, cols As Int32)
    End Sub

    Public Sub AddPlotScale(dst As cv.Mat, maxVal As Double, fontsize As Double)
        ' draw a scale along the side
        Dim spacer = CInt(dst.Height / 5)
        Dim spaceVal = CInt(maxVal / 5)
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
Public Class Plot_OpenCVBasics_CPP : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "Default Visualization of Plot2D"
        ocvb.label2 = "Custom Visualization of Plot2D"
        ocvb.desc = "Demo the use of the integrated 2D plot available in OpenCV (only accessible in C++)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim plotAData(ocvb.color.Total * ocvb.color.ElemSize - 1) As Byte
        Dim handleA = GCHandle.Alloc(plotAData, GCHandleType.Pinned)

        Dim PlotBData(ocvb.color.Total * ocvb.color.ElemSize - 1) As Byte
        Dim handleB = GCHandle.Alloc(PlotBData, GCHandleType.Pinned)

        Plot_OpenCVBasics(handleA.AddrOfPinnedObject, handleB.AddrOfPinnedObject, ocvb.color.Rows, ocvb.color.Cols)

        Marshal.Copy(plotAData, 0, ocvb.result1.Data, plotAData.Length)
        Marshal.Copy(PlotBData, 0, ocvb.result2.Data, PlotBData.Length)
        handleA.Free()
        handleB.Free()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class Plot_Basics : Implements IDisposable
    Public sliders As New OptionsSliders
    Public plotCount As Int32 = 3
    Public externalUse As Boolean
    Public src As cv.Mat
    Public dst As cv.Mat
    Public maxVal As Int32 = 250
    Public plotColor As New cv.Scalar
    Dim columnIndex As Int32
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Pixel Height", 1, 40, 2)
        sliders.setupTrackBar2(ocvb, "Scale Font Size x10", 1, 20, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Plot data provided in src Mat"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            plotColor = cv.Scalar.Red
            dst = ocvb.result1
            src = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            ocvb.label1 = "Plot of grayscale image values"
        End If
        If src.Channels <> 1 Then
            ocvb.putText(New ActiveClass.TrueType("Only single channel data can be input to Plot_Basics", 10, 125))
            Exit Sub
        End If
        If src.Cols <> 1 Then src = src.Reshape(1, src.Width * src.Height)
        Dim src32f As New cv.Mat
        src.ConvertTo(src32f, cv.MatType.CV_32F)

        Dim minVal As Single, maxVal As Single
        src32f.MinMaxLoc(minVal, maxVal)
        maxVal = Math.Round(maxVal / 5, 0) * 5 + 5

        Dim pixelHeight = CInt((sliders.TrackBar1.Value + 1) / 2)
        Dim pixelWidth = dst.Cols / src32f.Rows
        If pixelWidth < 1 Then pixelWidth = 1
        dst.SetTo(0)
        Dim lastX As Int32
        For i = 0 To src32f.Rows - 1
            Dim nextVal = src32f.At(Of Single)(i, 0)
            Dim x = CInt((i / src32f.Rows) * dst.Cols)
            If lastX <> x Then
                Dim y = dst.Rows - CInt(dst.Rows * nextVal / (maxVal - minVal))
                dst.Rectangle(New cv.Rect(x, y - pixelHeight, pixelWidth * 2, pixelHeight * 2), plotColor, -1)
            End If
            lastX = x
        Next

        AddPlotScale(dst, maxVal, sliders.TrackBar2.Value / 10)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class