Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

' https://github.com/opencv/opencv/blob/master/samples/python/hist.py
Public Class Histogram_Basics
    Inherits VBparent
    Public histRaw(3 - 1) As cv.Mat
    Public histNormalized(3 - 1) As cv.Mat
    Public bins = 50
    Public minRange = 0
    Public maxRange = 255
    Public backColor = cv.Scalar.Gray
    Public plotRequested As Boolean
    Public plotColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram Bins", 2, 256, 50)
            sliders.setupTrackBar(1, "Histogram line thickness", 1, 20, 3)
        End If
        task.desc = "Plot histograms for up to 3 channels."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static histBinSlider = findSlider("Histogram Bins")
        bins = histBinSlider.Value

        Static thicknessSlider = findSlider("Histogram line thickness")
        Dim thickness = thicknessSlider.Value
        Dim dimensions() = New Integer() {bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

        Dim lineWidth = dst1.Cols / bins

        dst1.SetTo(backColor)
        Dim maxVal As Double
        For i = 0 To src.Channels - 1
            Dim hist As New cv.Mat
            cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {i}, New cv.Mat(), hist, 1, dimensions, ranges)
            histRaw(i) = hist.Clone()
            histRaw(i).MinMaxLoc(0, maxVal)
            histNormalized(i) = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)
            If standalone Or plotRequested Then
                Dim points = New List(Of cv.Point)
                Dim listOfPoints = New List(Of List(Of cv.Point))
                For j = 0 To bins - 1
                    points.Add(New cv.Point(CInt(j * lineWidth), dst1.Rows - dst1.Rows * histRaw(i).Get(Of Single)(j, 0) / maxVal))
                Next
                listOfPoints.Add(points)
                dst1.Polylines(listOfPoints, False, plotColors(i), thickness, cv.LineTypes.AntiAlias)
            End If
        Next

        If standalone Or plotRequested Then
            maxVal = Math.Round(maxVal / 1000, 0) * 1000 + 1000 ' smooth things out a little for the scale below
            AddPlotScale(dst1, 0, maxVal, ocvb.fontSize * 2)
            label1 = "Histogram for src image (default color) - " + CStr(bins) + " bins"
        End If
    End Sub
End Class





Module histogram_Functions
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Histogram_3D_RGB(rgbPtr As IntPtr, rows As Integer, cols As Integer, bins As Integer) As IntPtr
    End Function

    Public Sub histogram2DPlot(histogram As cv.Mat, dst1 As cv.Mat, xBins As Integer, yBins As Integer)
        Dim maxVal As Double
        histogram.MinMaxLoc(0, maxVal)
        Dim xScale = dst1.Cols / xBins
        Dim yScale = dst1.Rows / yBins
        For y = 0 To yBins - 1
            For x = 0 To xBins - 1
                Dim binVal = histogram.Get(Of Single)(y, x)
                Dim intensity = Math.Round(binVal * 255 / maxVal)
                Dim pt1 = New cv.Point(x * xScale, y * yScale)
                Dim pt2 = New cv.Point((x + 1) * xScale - 1, (y + 1) * yScale - 1)
                If pt1.X >= dst1.Cols Then pt1.X = dst1.Cols - 1
                If pt1.Y >= dst1.Rows Then pt1.Y = dst1.Rows - 1
                If pt2.X >= dst1.Cols Then pt2.X = dst1.Cols - 1
                If pt2.Y >= dst1.Rows Then pt2.Y = dst1.Rows - 1
                If pt1.X <> pt2.X And pt1.Y <> pt2.Y Then
                    Dim value = cv.Scalar.All(255 - intensity)
                    'value = New cv.Scalar(pt1.X * 255 / dst1.Cols, pt1.Y * 255 / dst1.Rows, 255 - intensity)
                    value = New cv.Scalar(intensity, intensity, intensity)
                    dst1.Rectangle(pt1, pt2, value, -1, cv.LineTypes.AntiAlias)
                End If
            Next
        Next
    End Sub

    Public Sub Show_HSV_Hist(img As cv.Mat, hist As cv.Mat)
        Dim binCount = hist.Height
        Dim binWidth = img.Width / hist.Height
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)
        img.SetTo(0)
        If maxVal = 0 Then Exit Sub
        For i = 0 To binCount - 2
            Dim h = img.Height * (hist.Get(Of Single)(i, 0)) / maxVal
            If h = 0 Then h = 5 ' show the color range in the plot
            cv.Cv2.Rectangle(img, New cv.Rect(i * binWidth + 1, img.Height - h, binWidth - 2, h), New cv.Scalar(CInt(180.0 * i / binCount), 255, 255), -1)
        Next
    End Sub

    Public Sub histogramBars(hist As cv.Mat, dst1 As cv.Mat, savedMaxVal As Single)
        Dim barWidth = Int(dst1.Width / hist.Rows)
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)

        maxVal = Math.Round(maxVal / 1000, 0) * 1000 + 1000

        If maxVal < 0 Then maxVal = savedMaxVal
        If Math.Abs((maxVal - savedMaxVal)) / maxVal < 0.2 Then maxVal = savedMaxVal Else savedMaxVal = Math.Max(maxVal, savedMaxVal)

        dst1.SetTo(cv.Scalar.Red)
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
        End If
    End Sub
End Module





Public Class Histogram_NormalizeGray
    Inherits VBparent
    Public histogram As Histogram_KalmanSmoothed
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Min Gray", 0, 255, 0)
            sliders.setupTrackBar(1, "Max Gray", 0, 255, 255)
        End If
        histogram = New Histogram_KalmanSmoothed()

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Normalize Before Histogram"
            check.Box(0).Checked = True
        End If

        task.desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        histogram.src = src
        If check.Box(0).Checked Then
            cv.Cv2.Normalize(histogram.src, histogram.src, sliders.trackbar(0).Value, sliders.trackbar(1).Value, cv.NormTypes.MinMax) ' only minMax is working...
        End If
        histogram.Run()
        dst1 = histogram.dst1
    End Sub
End Class






' https://docs.opencv.org/2.4/modules/imgproc/doc/histograms.html
Public Class Histogram_2D_HueSaturation
    Inherits VBparent
    Public histogram As New cv.Mat
    Public hsv As cv.Mat

    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Hue bins", 1, 180, 30) ' quantize hue to 30 levels
            sliders.setupTrackBar(1, "Saturation bins", 1, 256, 32) ' quantize sat to 32 levels
        End If
        task.desc = "Create a histogram for hue and saturation."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        hsv = src.CvtColor(cv.ColorConversionCodes.RGB2HSV)
        Dim hbins = sliders.trackbar(0).Value
        Dim sbins = sliders.trackbar(1).Value
        Dim histSize() = {sbins, hbins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, sliders.trackbar(0).Maximum - 1), New cv.Rangef(0, sliders.trackbar(1).Maximum - 1)} ' hue ranges from 0-179

        cv.Cv2.CalcHist(New cv.Mat() {hsv}, New Integer() {0, 1}, New cv.Mat(), histogram, 2, histSize, ranges)

        histogram2DPlot(histogram, dst1, hbins, sbins)
    End Sub
End Class








Public Class Histogram_KalmanSmoothed
    Inherits VBparent
    Public mask As New cv.Mat

    Public histogram As New cv.Mat
    Public kalman As Kalman_Basics
    Public plotHist As Plot_Histogram
    Dim splitColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
    Public Sub New()
        initParent()
        plotHist = New Plot_Histogram()
        plotHist.minRange = 0

        kalman = New Kalman_Basics()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram Bins", 1, 255, 50)
        End If

        label2 = "Histogram - x=bins/y=count"
        task.desc = "Create a histogram of the grayscale image and smooth the bar chart with a kalman filter."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static splitIndex = -1
        Static colorName As String
        If standalone Then
            Dim split() = cv.Cv2.Split(src)
            If ocvb.frameCount Mod 100 = 0 Then
                splitIndex += 1
                If splitIndex > 2 Then splitIndex = 0
            End If
            src = split(splitIndex)
            colorName = Choose(splitIndex + 1, "Blue", "Green", "Red")
        Else
            If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        Static histBinSlider = findSlider("Histogram Bins")
        plotHist.bins = histBinSlider.Value
        Dim histSize() = {plotHist.bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}

        Dim dimensions() = New Integer() {plotHist.bins}
        cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {0}, mask, histogram, 1, dimensions, ranges)

        label2 = "Plot Histogram bins = " + CStr(plotHist.bins)

        ReDim kalman.kInput(plotHist.bins - 1)
        For i = 0 To plotHist.bins - 1
            kalman.kInput(i) = histogram.Get(Of Single)(i, 0)
        Next
        kalman.Run()
        For i = 0 To plotHist.bins - 1
            histogram.Set(Of Single)(i, 0, kalman.kOutput(i))
        Next

        plotHist.hist = histogram
        If standalone Then plotHist.backColor = splitColors(splitIndex)
        plotHist.src = src
        plotHist.Run()
        dst1 = plotHist.dst1
        label1 = colorName + " input to histogram"
    End Sub
End Class




Public Class Histogram_Depth
    Inherits VBparent
    Public inrange As Depth_InRange
    Public plotHist As Plot_Histogram
    Public Sub New()
        initParent()
        plotHist = New Plot_Histogram()

        inrange = New Depth_InRange()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram Depth Bins", 2, src.Cols, 50)
        End If

        task.desc = "Show depth data as a histogram."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        inrange.src = getDepth32f()
        inrange.Run()
        plotHist.minRange = inrange.sliders.trackbar(0).Value
        plotHist.maxRange = inrange.sliders.trackbar(1).Value
        plotHist.bins = sliders.trackbar(0).Value

        Dim histSize() = {plotHist.bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}
        cv.Cv2.CalcHist(New cv.Mat() {inrange.depth32f}, New Integer() {0}, New cv.Mat, plotHist.hist, 1, histSize, ranges)

        If standalone Then
            plotHist.Run()
            dst1 = plotHist.dst1
        End If
        label1 = "Histogram Depth: " + Format(plotHist.minRange / 1000, "0.0") + "m to " + Format(plotHist.maxRange / 1000, "0.0") + " m"
    End Sub
End Class




Public Class Histogram_DepthValleys
    Inherits VBparent
    Dim kalman As Kalman_Basics
    Dim hist As Histogram_Depth
    Public rangeBoundaries As New List(Of cv.Point)
    Public sortedSizes As New List(Of Integer)
    Private Class CompareCounts : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just integer?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return -1
            Return 1
        End Function
    End Class
    Private Sub histogramBarsValleys(img As cv.Mat, hist As cv.Mat, plotColors() As cv.Scalar)
        Dim binCount = hist.Height
        Dim binWidth = CInt(img.Width / hist.Height)
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)
        img.SetTo(0)
        If maxVal = 0 Then Exit Sub
        For i = 0 To binCount - 1
            Dim nextHistCount = hist.Get(Of Single)(i, 0)
            Dim h = CInt(img.Height * nextHistCount / maxVal)
            If h = 0 Then h = 1 ' show the color range in the plot
            Dim barRect As cv.Rect
            barRect = New cv.Rect(i * binWidth, img.Height - h, binWidth, h)
            cv.Cv2.Rectangle(img, barRect, plotColors(i), -1)
        Next
    End Sub
    Public Sub New()
        initParent()
        hist = New Histogram_Depth()
        hist.inrange.sliders.trackbar(1).Value = 5000 ' depth to 5 meters.
        hist.sliders.trackbar(0).Value = 40 ' number of bins.

        kalman = New Kalman_Basics()

        task.desc = "Identify valleys in the Depth histogram."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        hist.Run()
        ReDim kalman.kInput(hist.plotHist.hist.Rows - 1)
        For i = 0 To hist.plotHist.hist.Rows - 1
            kalman.kInput(i) = hist.plotHist.hist.Get(Of Single)(i, 0)
        Next
        kalman.Run()
        For i = 0 To hist.plotHist.hist.Rows - 1
            hist.plotHist.hist.Set(Of Single)(i, 0, kalman.kOutput(i))
        Next

        Dim depthIncr = CInt(hist.inrange.sliders.trackbar(1).Value / hist.sliders.trackbar(0).Value) ' each bar represents this number of millimeters
        Dim pointCount = hist.plotHist.hist.Get(Of Single)(0, 0) + hist.plotHist.hist.Get(Of Single)(1, 0)
        Dim startDepth = 1
        Dim startEndDepth As cv.Point
        Dim depthBoundaries As New SortedList(Of Single, cv.Point)(New CompareCounts)
        For i = 2 To kalman.kOutput.Length - 3
            Dim prev2 = If(i > 2, kalman.kOutput(i - 2), 0)
            Dim prev = If(i > 1, kalman.kOutput(i - 1), 0)
            Dim curr = kalman.kOutput(i)
            Dim post = If(i < kalman.kOutput.Length - 1, kalman.kOutput(i + 1), 0)
            Dim post2 = If(i < kalman.kOutput.Length - 2, kalman.kOutput(i + 2), 0)
            pointCount += kalman.kOutput(i)
            If prev2 > 1 And prev > 1 And curr > 1 And post > 1 And post2 > 1 Then
                If curr < (prev + prev2) / 2 And curr < (post + post2) / 2 And i * depthIncr > startDepth + depthIncr Then
                    startEndDepth = New cv.Point(startDepth, i * depthIncr)
                    depthBoundaries.Add(pointCount, startEndDepth)
                    pointCount = 0
                    startDepth = i * depthIncr + 0.1
                End If
            End If
        Next

        startEndDepth = New cv.Point(startDepth, hist.inrange.sliders.trackbar(1).Value)
        depthBoundaries.Add(pointCount, startEndDepth) ' capped at the max depth we are observing

        rangeBoundaries.Clear()
        sortedSizes.Clear()
        For i = depthBoundaries.Count - 1 To 0 Step -1
            rangeBoundaries.Add(depthBoundaries.ElementAt(i).Value)
            sortedSizes.Add(depthBoundaries.ElementAt(i).Key)
        Next

        Dim plotColors(hist.plotHist.hist.Rows - 1) As cv.Scalar
        For i = 0 To hist.plotHist.hist.Rows - 1
            Dim depth = i * depthIncr + 1
            For j = 0 To rangeBoundaries.Count - 1
                Dim startEnd = rangeBoundaries.ElementAt(j)
                If depth >= startEnd.X And depth < startEnd.Y Then
                    plotColors(i) = ocvb.scalarColors(j Mod 255)
                    Exit For
                End If
            Next
        Next
        histogramBarsValleys(dst1, hist.plotHist.hist, plotColors)
        label1 = "Histogram clustered by valleys and smoothed"
    End Sub
End Class





Public Class Histogram_DepthClusters
    Inherits VBparent
    Public valleys As Histogram_DepthValleys
    Public Sub New()
        initParent()
        valleys = New Histogram_DepthValleys()
        task.desc = "Color each of the Depth Clusters found with Histogram_DepthValleys - stabilized with Kalman."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        valleys.Run()
        dst1 = valleys.dst1

        Dim mask As New cv.Mat
        Dim tmp As New cv.Mat
        dst2.SetTo(0)
        For i = 0 To valleys.rangeBoundaries.Count - 1
            Dim startEndDepth = valleys.rangeBoundaries.ElementAt(i)
            cv.Cv2.InRange(getDepth32f(), startEndDepth.X, startEndDepth.Y, tmp)
            cv.Cv2.ConvertScaleAbs(tmp, mask)
            dst2.SetTo(ocvb.scalarColors(i), mask)
        Next
        If standalone Then
            label1 = "Histogram of " + CStr(valleys.rangeBoundaries.Count) + " Depth Clusters"
            label2 = "Backprojection of " + CStr(valleys.rangeBoundaries.Count) + " histogram clusters"
        End If
    End Sub
End Class




Public Class Histogram_2D_XZ_YZ
    Inherits VBparent
    Dim inrange As Depth_InRange
    Dim xyz As Mat_ImageXYZ_MT
    Public Sub New()
        initParent()
        xyz = New Mat_ImageXYZ_MT()

        inrange = New Depth_InRange()
        inrange.sliders.trackbar(1).Value = 1500 ' up to x meters away

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram X bins", 1, src.Cols, 30)
            sliders.setupTrackBar(1, "Histogram Y bins", 1, src.Rows, 30)
            sliders.setupTrackBar(2, "Histogram Z bins", 1, 200, 100)
        End If
        task.desc = "Create a 2D histogram for depth in XZ and YZ."
        label2 = "Left is XZ (Top View) and Right is YZ (Side View)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim xbins = sliders.trackbar(0).Value
        Dim ybins = sliders.trackbar(1).Value
        Dim zbins = sliders.trackbar(2).Value
        Dim minRange = inrange.sliders.trackbar(0).Value
        Dim maxRange = inrange.sliders.trackbar(1).Value

        Dim histogram As New cv.Mat

        Dim rangesX() = New cv.Rangef() {New cv.Rangef(0, src.Width - 1), New cv.Rangef(minRange, maxRange)}
        Dim rangesY() = New cv.Rangef() {New cv.Rangef(0, src.Width - 1), New cv.Rangef(minRange, maxRange)}

        xyz.Run()
        Dim sizesX() = {xbins, zbins}
        cv.Cv2.CalcHist(New cv.Mat() {xyz.xyDepth}, New Integer() {0, 2}, New cv.Mat(), histogram, 2, sizesX, rangesX)
        histogram2DPlot(histogram, dst1, zbins, xbins)

        Dim sizesY() = {ybins, zbins}
        cv.Cv2.CalcHist(New cv.Mat() {xyz.xyDepth}, New Integer() {1, 2}, New cv.Mat(), histogram, 2, sizesY, rangesY)
        histogram2DPlot(histogram, dst2, zbins, ybins)
    End Sub
End Class








' https://docs.opencv.org/master/d1/db7/tutorial_py_histogram_begins.html
Public Class Histogram_EqualizeColor
    Inherits VBparent
    Public kalmanEq As Histogram_KalmanSmoothed
    Public kalman As Histogram_KalmanSmoothed
    Dim mats As Mat_2to1
    Public displayHist As Boolean = False
    Public channel = 2
    Public Sub New()
        initParent()
        kalmanEq = New Histogram_KalmanSmoothed()
        kalman = New Histogram_KalmanSmoothed()

        Static binSlider = findSlider("Histogram Bins")
        binSlider.Value = 40

        mats = New Mat_2to1()

        task.desc = "Create an equalized histogram of the color image. Image is noticeably enhanced."
        label1 = "Image Enhanced with Equalized Histogram"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim rgb(2) As cv.Mat
        Dim rgbEq(2) As cv.Mat
        cv.Cv2.Split(src, rgbEq)

        For i = 0 To rgb.Count - 1
            cv.Cv2.EqualizeHist(rgbEq(i), rgbEq(i))
        Next

        If standalone Or displayHist Then
            cv.Cv2.Split(src, rgb) ' equalizehist alters the input...
            kalman.src = rgb(channel).Clone()
            kalman.plotHist.backColor = cv.Scalar.Red
            kalman.Run()
            mats.mat(0) = kalman.dst1.Clone()

            kalmanEq.src = rgbEq(channel).Clone()
            kalmanEq.Run()
            mats.mat(1) = kalmanEq.dst1.Clone()

            mats.Run()
            dst2 = mats.dst1
            label2 = "Before (top) and After Red Histogram"

            cv.Cv2.Merge(rgbEq, dst1)
        End If
    End Sub
End Class






'https://docs.opencv.org/master/d1/db7/tutorial_py_histogram_begins.html
Public Class Histogram_EqualizeGray
    Inherits VBparent
    Public histogramEq As Histogram_KalmanSmoothed
    Public histogram As Histogram_KalmanSmoothed
    Public Sub New()
        initParent()
        histogramEq = New Histogram_KalmanSmoothed()

        histogram = New Histogram_KalmanSmoothed()

        label1 = "Before EqualizeHist"
        label2 = "After EqualizeHist"
        task.desc = "Create an equalized histogram of the grayscale image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static binSlider = findSlider("Histogram Bins")
        Static eqCheckBox = findCheckBox("Turn Kalman filtering on")

        binSlider.Value = histogramEq.sliders.trackbar(0).Value
        eqCheckBox.Checked = histogramEq.kalman.check.Box(0).Checked

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        histogram.src = src.Clone
        histogram.Run()
        dst1 = histogram.dst1.Clone
        cv.Cv2.EqualizeHist(histogram.src, histogramEq.src)
        histogramEq.Run()
        dst2 = histogramEq.dst1
    End Sub
End Class





' https://docs.opencv.org/master/d1/db7/tutorial_py_histogram_begins.html
Public Class Histogram_Equalize255
    Inherits VBparent
    Dim eqHist As Histogram_EqualizeColor
    Public Sub New()
        initParent()

        eqHist = New Histogram_EqualizeColor()
        Static binSlider = findSlider("Histogram Bins")
        binSlider.Value = 255
        eqHist.displayHist = True

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Equalize the Blue channel"
            radio.check(1).Text = "Equalize the Green channel"
            radio.check(2).Text = "Equalize the Red channel"
            radio.check(2).Checked = True
        End If
        label1 = "Resulting equalized image"
        label2 = "Upper plot is before equalization.  Bottom is after."
        task.desc = "Reproduce the results of the hist.py example with existing algorithms"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        For i = 0 To 3 - 1
            If radio.check(i).Checked Then eqHist.channel = i
        Next
        eqHist.src = src
        eqHist.Run()
        dst1 = eqHist.dst1.Clone
        dst2 = eqHist.dst2.Clone
    End Sub
End Class





Public Class Histogram_Simple
    Inherits VBparent
    Public plotHist As Plot_Histogram
    Public Sub New()
        initParent()
        plotHist = New Plot_Histogram()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram Bins", 2, src.Cols, 50)
        End If

        task.desc = "Build a simple and reusable histogram for grayscale images."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        plotHist.bins = sliders.trackbar(0).Value

        Dim histSize() = {sliders.trackbar(0).Value}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}
        cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {0}, New cv.Mat, plotHist.hist, 1, histSize, ranges)

        plotHist.Run()
        dst1 = plotHist.dst1
    End Sub
End Class












Public Class Histogram_ColorsAndGray
    Inherits VBparent
    Dim histogram As Histogram_KalmanSmoothed
    Dim mats As Mat_4to1
    Public Sub New()
        initParent()
        mats = New Mat_4to1()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Min Gray", 0, 255, 0)
            sliders.setupTrackBar(1, "Max Gray", 0, 255, 255)
        End If
        histogram = New Histogram_KalmanSmoothed()
        histogram.kalman.check.Box(0).Checked = False
        histogram.kalman.check.Box(0).Enabled = False
        histogram.sliders.trackbar(0).Value = 40

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Normalize Before Histogram"
            check.Box(0).Checked = True
        End If

        label2 = "Click any quadrant at left to view it below"
        task.desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim split = src.Split()
        ReDim Preserve split(4 - 1)
        split(4 - 1) = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) ' add a 4th image - the grayscale image to the R G and B images.
        histogram.src = New cv.Mat
        For i = 0 To split.Length - 1
            If check.Box(0).Checked Then
                cv.Cv2.Normalize(split(i), histogram.src, sliders.trackbar(0).Value, sliders.trackbar(1).Value, cv.NormTypes.MinMax) ' only minMax is working...
            Else
                histogram.src = split(i).Clone()
            End If
            histogram.plotHist.backColor = Choose(i + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.PowderBlue)
            histogram.Run()
            mats.mat(i) = histogram.dst1.Clone()
        Next

        mats.Run()
        dst1 = mats.dst1
        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setQuadrant()
        dst2 = mats.mat(ocvb.quadrantIndex)
    End Sub
End Class





Public Class Histogram_BackProjectionPeak
    Inherits VBparent
    Dim hist As Histogram_KalmanSmoothed
    Public Sub New()
        initParent()

        hist = New Histogram_KalmanSmoothed()
        hist.kalman.check.Box(0).Checked = False

        task.desc = "Create a histogram and back project into the image the grayscale color with the highest occurance."
        label2 = "Grayscale Histogram"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        hist.src = src
        hist.Run()
        dst2 = hist.dst1

        Dim minVal As Single, maxVal As Single
        Dim minIdx As cv.Point, maxIdx As cv.Point
        hist.histogram.MinMaxLoc(minVal, maxVal, minIdx, maxIdx)
        Dim barWidth = dst1.Width / hist.sliders.trackbar(0).Value
        Dim barRange = 255 / hist.sliders.trackbar(0).Value
        Dim histindex = maxIdx.Y
        Dim pixelMin = CInt((histindex) * barRange)
        Dim pixelMax = CInt((histindex + 1) * barRange)

        Dim mask = hist.src.InRange(pixelMin, pixelMax).Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst1.SetTo(0)
        src.CopyTo(dst1, mask)
        label1 = "BackProjection of most frequent gray pixel"
        dst2.Rectangle(New cv.Rect(barWidth * histindex, 0, barWidth, dst1.Height), cv.Scalar.Yellow, 1)
    End Sub
End Class









' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class Histogram_BackProjection2D
    Inherits VBparent
    Dim hist As Histogram_2D_HueSaturation
    Public Sub New()
        initParent()

        hist = New Histogram_2D_HueSaturation()

        task.desc = "Backproject from a hue and saturation histogram."
        label1 = "X-axis is Hue, Y-axis is Sat.  Draw rectangle to isolate ranges"
        label2 = "Backprojection of detected hue and saturation."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        hist.src = src
        hist.Run()
        dst1 = hist.dst1
        Static hueBins = hist.sliders.trackbar(0).Value
        Static satBins = hist.sliders.trackbar(1).Value
        If hueBins <> hist.sliders.trackbar(0).Value Or satBins <> hist.sliders.trackbar(1).Value Then
            task.drawRectClear = True
            hueBins = hist.sliders.trackbar(0).Value
            satBins = hist.sliders.trackbar(1).Value
        End If

        Dim unitsPerHueBin = 180 / hueBins
        Dim unitsPerSatBin = 255 / satBins
        Dim minHue = 0, maxHue = 180, minSat = 0, maxSat = 255
        If task.drawRect.Width <> 0 And task.drawRect.Height <> 0 Then
            Dim intBin = Math.Floor(hueBins * task.drawRect.X / dst1.Width)
            minHue = intBin * unitsPerHueBin
            intBin = Math.Ceiling(hueBins * (task.drawRect.X + task.drawRect.Width) / dst1.Width)
            maxHue = intBin * unitsPerHueBin

            intBin = Math.Floor(satBins * task.drawRect.Y / dst1.Height)
            minSat = intBin * unitsPerSatBin
            intBin = Math.Ceiling(satBins * (task.drawRect.Y + task.drawRect.Height) / dst1.Height)
            maxSat = intBin * unitsPerSatBin

            If minHue = maxHue Then maxHue = minHue + 1
            If minSat = maxSat Then maxSat = minSat + 1
            label2 = "Selection: min/max Hue " + Format(minHue, "0") + "/" + Format(maxHue, "0") + " min/max Sat " + Format(minSat, "0") + "/" + Format(maxSat, "0")
        End If
        ' Dim histogram = hist.histogram.Normalize(0, 255, cv.NormTypes.MinMax)
        Dim bins() = {0, 1}
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim mat() As cv.Mat = {hsv}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minHue, maxHue), New cv.Rangef(minSat, maxSat)}
        Dim mask As New cv.Mat
        cv.Cv2.CalcBackProject(mat, bins, hist.histogram, mask, ranges)

        dst2.SetTo(0)
        src.CopyTo(dst2, mask)
    End Sub
End Class






Public Class Histogram_HueSaturation2DPlot
    Inherits VBparent
    Dim hueSat As Brightness_Hue
    Dim hist2d As Histogram_BackProjection2D
    Dim mats As Mat_4to1
    Public Sub New()
        initParent()

        hueSat = New Brightness_Hue()
        hist2d = New Histogram_BackProjection2D()
        mats = New Mat_4to1()
        ocvb.quadrantIndex = QUAD3
        label2 = "Click any quadrant at left to view it below"
        task.desc = "Compare the hue and brightness images and the results of the histogram_backprojection2d"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        hueSat.src = src
        hueSat.Run()
        mats.mat(0) = hueSat.dst1
        mats.mat(1) = hueSat.dst2

        hist2d.src = src
        hist2d.Run()
        mats.mat(2) = hist2d.dst2
        mats.mat(3) = hist2d.dst1

        mats.Run()
        dst1 = mats.dst1
        If task.mouseClickFlag And task.mousePicTag = RESULT1 Then setQuadrant()
        dst2 = mats.mat(ocvb.quadrantIndex)
    End Sub
End Class








' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class Histogram_BackProjectionGrayscale
    Inherits VBparent
    Dim hist As Histogram_KalmanSmoothed
    Public histIndex As Integer
    Public Sub New()
        initParent()
        hist = New Histogram_KalmanSmoothed()
        Dim binSlider = findSlider("Histogram Bins")
        binSlider.Value = 10

        label1 = "Move mouse to backproject each histogram column"
        task.desc = "Explore Backprojection of each element of a grayscale histogram."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        hist.src = src
        hist.Run()
        dst1 = hist.dst1

        histIndex = CInt(hist.histogram.Rows * task.mousePoint.X / src.Width)
        Dim barWidth = dst1.Width / hist.sliders.trackbar(0).Value
        Dim barRange = 255 / hist.sliders.trackbar(0).Value

        Dim mask As New cv.Mat
        Dim ranges() = New cv.Rangef() {New cv.Rangef(histIndex * barRange, (histIndex + 1) * barRange)}
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) '  = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
        Dim mat() As cv.Mat = {gray}
        Dim bins() = {0}
        cv.Cv2.CalcBackProject(mat, bins, hist.histogram, mask, ranges)

        gray.SetTo(0)
        gray.SetTo(255, mask)
        dst2 = gray.Clone

        label2 = "Backprojection index " + CStr(histIndex) + " with " + Format(hist.histogram.Get(Of Single)(histIndex, 0), "#0") + " samples"
        dst1.Rectangle(New cv.Rect(barWidth * histIndex, 0, barWidth, dst1.Height), cv.Scalar.Yellow, 5)
    End Sub
End Class










Public Class Histogram_Concentration
    Inherits VBparent
    Public sideview As Histogram_SideView2D
    Public topview As Histogram_TopView2D
    Dim palette As Palette_Basics
    Public Sub New()
        initParent()

        palette = New Palette_Basics()
        sideview = New Histogram_SideView2D()
        topview = New Histogram_TopView2D()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Display the top x highlights", 1, 1000, 50)
            sliders.setupTrackBar(1, "Concentration Factor x100", 1, 100, 10)
            sliders.setupTrackBar(2, "Concentration Threshold", 1, 100, 10)
        End If
        task.desc = "Highlight the histogram projections where concentrations are highest"
    End Sub
    Private Function plotHighlights( histOutput As cv.Mat, dst As cv.Mat) As String
        Static concentrationSlider = findSlider("Concentration Factor x100")
        Dim concentrationFactor = concentrationSlider.Value / 100

        Static cThresholdSlider = findSlider("Concentration Threshold")
        Dim concentrationThreshold = cThresholdSlider.Value

        Static minDepthSlider = findSlider("InRange Min Depth (mm)")
        Dim minPixel = CInt(concentrationFactor * minDepthSlider.value * ocvb.pixelsPerMeterH / 1000)

        Dim tmp = histOutput.Resize(New cv.Size(CInt(histOutput.Width * concentrationFactor), CInt(histOutput.Height * concentrationFactor)))
        Dim pts As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalIntegerInverted)
        For y = 0 To tmp.Height - 1
            For x = minPixel To tmp.Width - 1
                Dim val = tmp.Get(Of Single)(y, x)
                If val > concentrationThreshold Then pts.Add(val, New cv.Point(CInt(x / concentrationFactor), CInt(y / concentrationFactor)))
            Next
        Next

        Static topXslider = findSlider("Display the top x highlights")
        Dim topX = topXslider.value
        For i = 0 To Math.Min(pts.Count - 1, topX - 1)
            Dim pt = pts.ElementAt(i).Value
            dst.Circle(pt, ocvb.dotSize, cv.Scalar.All((i * 27 + 100) Mod 255), -1, cv.LineTypes.AntiAlias)
        Next
        palette.src = dst
        palette.Run()
        Dim maxConcentration = If(pts.Count > 0, pts.ElementAt(0).Key, 0)
        Return CStr(pts.Count) + " highlights. Max=" + CStr(maxConcentration)
    End Function
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        sideview.Run()
        dst1 = sideview.dst1
        Dim noDepth = sideview.histOutput.Get(Of Single)(sideview.histOutput.Height / 2, 0)
        label1 = "SideView " + plotHighlights(sideview.histOutput, dst1) + " No depth: " + CStr(CInt(noDepth / 1000)) + "k"
        dst1 = palette.dst1.Clone

        topview.Run()
        dst2 = topview.dst1
        label2 = "TopView " + plotHighlights(topview.histOutput, dst2) + " No depth: " + CStr(CInt(noDepth / 1000)) + "k"
        dst2 = palette.dst1.Clone
    End Sub
End Class









Public Class Histogram_SideView2D
    Inherits VBparent
    Public gCloud As Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Dim cameraYSlider As Windows.Forms.TrackBar
    Dim frustrumSlider As Windows.Forms.TrackBar
    Dim cmat As PointCloud_Colorize
    Public frustrumAdjust As Single
    Public resizeHistOutput As Boolean = True
    Public Sub New()
        initParent()

        cmat = New PointCloud_Colorize()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "SideView Frustrum adjustment", 1, 100, 57)
            sliders.setupTrackBar(1, "SideCameraPoint.x adjustment", -100, 100, 0)
        End If

        frustrumSlider = findSlider("SideView Frustrum adjustment")
        cameraYSlider = findSlider("SideCameraPoint.x adjustment")

        ' The specification for each camera spells out the vertical FOV angle
        ' The sliders adjust the depth data histogram to fill the frustrum which is built from the spec.
        Select Case ocvb.parms.cameraName
            Case VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam
                frustrumSlider.Value = 58
                cameraYSlider.Value = If(ocvb.resolutionIndex = 1, -1, -2)
            Case VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2
                frustrumSlider.Value = 53
                cameraYSlider.Value = -3
            Case VB_Classes.ActiveTask.algParms.camNames.MyntD1000
                frustrumSlider.Value = 50
                cameraYSlider.Value = If(ocvb.resolutionIndex = 3, -8, -3)
            Case VB_Classes.ActiveTask.algParms.camNames.D435i
                frustrumSlider.Value = 57
                cameraYSlider.Value = 0
            Case VB_Classes.ActiveTask.algParms.camNames.D455
                frustrumSlider.Value = 58
                cameraYSlider.Value = If(ocvb.resolutionIndex = 1, -1, -3)
        End Select
        gCloud = New Depth_PointCloud_IMU()

        label1 = "ZY (Side View)"
        task.desc = "Create a 2D side view for ZY histogram of depth - NOTE: x and y scales are the same"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        gCloud.Run()

        ocvb.pixelsPerMeterH = dst1.Width / ocvb.maxZ
        ocvb.pixelsPerMeterV = 2 * ocvb.pixelsPerMeterH * Math.Tan(cv.Cv2.PI / 180 * ocvb.vFov / 2)
        ocvb.sideCameraPoint = New cv.Point(0, src.Height / 2 + cameraYSlider.Value)

        frustrumAdjust = ocvb.maxZ * frustrumSlider.Value / 100 / 2

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-frustrumAdjust, frustrumAdjust), New cv.Rangef(0, ocvb.maxZ)}
        Dim histSize() = {gCloud.imuPointCloud.Height, gCloud.imuPointCloud.Width}
        If resizeHistOutput Then histSize = {dst2.Height, dst2.Width}
        cv.Cv2.CalcHist(New cv.Mat() {gCloud.imuPointCloud}, New Integer() {1, 2}, New cv.Mat, histOutput, 2, histSize, ranges)

        Static histThresholdSlider = findSlider("Histogram threshold")
        If standalone And ocvb.frameCount = 0 Then histThresholdSlider.Value = 1
        Dim tmp = histOutput.Threshold(histThresholdSlider.Value, 255, cv.ThresholdTypes.Binary).Resize(dst1.Size)
        tmp.ConvertTo(dst1, cv.MatType.CV_8UC1)

        dst2 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = cmat.CameraLocationSide(dst2)
    End Sub
End Class








Public Class Histogram_TopView2D
    Inherits VBparent
    Public gCloud As Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Public markers(2 - 1) As cv.Point2f
    Dim cmat As PointCloud_Colorize
    Dim cameraXSlider As Windows.Forms.TrackBar
    Dim frustrumSlider As Windows.Forms.TrackBar
    Public resizeHistOutput As Boolean = True
    Public Sub New()
        initParent()

        cmat = New PointCloud_Colorize()
        gCloud = New Depth_PointCloud_IMU()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "TopView Frustrum adjustment", 1, 300, 175)
            sliders.setupTrackBar(1, "TopCameraPoint.x adjustment", -10, 10, 0)
        End If

        frustrumSlider = findSlider("TopView Frustrum adjustment")
        cameraXSlider = findSlider("TopCameraPoint.x adjustment")

        ' The specification for each camera spells out the vertical FOV angle
        ' The sliders adjust the depth data histogram to fill the frustrum which is built from the spec.
        Select Case ocvb.parms.cameraName
            Case VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam
                frustrumSlider.Value = 180
                cameraXSlider.Value = 0
            Case VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2
                frustrumSlider.Value = 162
                cameraXSlider.Value = If(ocvb.resolutionIndex = 3, 38, 13)
            Case VB_Classes.ActiveTask.algParms.camNames.MyntD1000
                frustrumSlider.Value = 105
                cameraXSlider.Value = If(ocvb.resolutionIndex = 1, 4, 8)
            Case VB_Classes.ActiveTask.algParms.camNames.D435i
                frustrumSlider.Value = 175
                cameraXSlider.Value = 0
            Case VB_Classes.ActiveTask.algParms.camNames.D455
                frustrumSlider.Value = 184
                cameraXSlider.Value = 0
        End Select

        label1 = "XZ (Top View)"
        task.desc = "Create a 2D top view for XZ histogram of depth - NOTE: x and y scales are the same"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        gCloud.Run()

        ocvb.pixelsPerMeterH = dst1.Width / ocvb.maxZ
        ocvb.pixelsPerMeterV = 2 * ocvb.pixelsPerMeterH * Math.Tan((ocvb.vFov / 2) * cv.Cv2.PI / 180)
        ocvb.topCameraPoint = New cv.Point(src.Width / 2 + cameraXSlider.Value, CInt(src.Height))

        Static frustrumSlider = findSlider("TopView Frustrum adjustment")
        Dim fFactor = ocvb.maxZ * frustrumSlider.Value / 100 / 2

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, ocvb.maxZ), New cv.Rangef(-fFactor, fFactor)}
        Dim histSize() = {gCloud.imuPointCloud.Height, gCloud.imuPointCloud.Width}
        If resizeHistOutput Then histSize = {dst2.Height, dst2.Width}
        cv.Cv2.CalcHist(New cv.Mat() {gCloud.imuPointCloud}, New Integer() {2, 0}, New cv.Mat, histOutput, 2, histSize, ranges)

        histOutput = histOutput.Flip(cv.FlipMode.X)
        Static histThresholdSlider = findSlider("Histogram threshold")
        dst1 = histOutput.Threshold(histThresholdSlider.Value, 255, cv.ThresholdTypes.Binary).Resize(dst1.Size)
        dst1.ConvertTo(dst1, cv.MatType.CV_8UC1)
        If standalone Then
            dst2 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst2 = cmat.CameraLocationBot(dst2)
        End If
    End Sub
End Class









Public Class Histogram_TopData
    Inherits VBparent
    Public gCloud As Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Public meterMin As Single
    Public meterMax As Single
    Public split() As cv.Mat
    Public cameraLoc As Integer
    Dim kalman As Kalman_Basics
    Dim IntelBug As Boolean
    Public resizeHistOutput As Boolean = True
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "X scale negative value in meters (meterMin) X100", -400, -5, -200)
            sliders.setupTrackBar(1, "X scale positive value in meters (meterMax) X100", 5, 400, 200)
        End If

        kalman = New Kalman_Basics()
        gCloud = New Depth_PointCloud_IMU()
        If VB_Classes.ActiveTask.algParms.camNames.D455 = ocvb.parms.cameraName Then IntelBug = True

        label1 = "XZ (Top View)"
        task.desc = "Create a 2D top view for XZ histogram of depth in meters - NOTE: x and y scales differ!"
    End Sub

    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        gCloud.Run()
        Dim imuPC = gCloud.imuPointCloud
        split = imuPC.Split()

        Static minSlider = findSlider("X scale negative value in meters (meterMin) X100")
        Static maxSlider = findSlider("X scale positive value in meters (meterMax) X100")
        meterMin = minSlider.Value / 100
        meterMax = maxSlider.value / 100

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, ocvb.maxZ), New cv.Rangef(meterMin, meterMax)}
        Dim histSize() = {gCloud.imuPointCloud.Height, gCloud.imuPointCloud.Width}
        If resizeHistOutput Then histSize = {dst2.Height, dst2.Width}
        cv.Cv2.CalcHist(New cv.Mat() {imuPC}, New Integer() {2, 0}, New cv.Mat, histOutput, 2, histSize, ranges)
        histOutput = histOutput.Flip(cv.FlipMode.X)

        Static histThresholdSlider = findSlider("Histogram threshold")
        dst2 = histOutput.Threshold(histThresholdSlider.value, 255, cv.ThresholdTypes.Binary).Resize(dst1.Size)
        dst2.ConvertTo(dst1, cv.MatType.CV_8UC1)
        dst1 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        cameraLoc = CInt(dst1.Width * Math.Abs(meterMin) / Math.Abs(meterMax - meterMin))

        dst1.Line(New cv.Point(cameraLoc, dst2.Height), New cv.Point(cameraLoc, 0), cv.Scalar.Yellow, 1)
        label1 = "Camera level is " + CStr(cameraLoc) + " rows from the left (in yellow)"
        label2 = "Left x = " + Format(meterMin, "#0.00") + " Right X = " + Format(meterMax, "#0.00")
    End Sub
End Class









Public Class Histogram_SideData
    Inherits VBparent
    Public gCloud As Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Public meterMin As Single
    Public meterMax As Single
    Public split() As cv.Mat
    Public cameraLoc As Integer
    Dim kalman As Kalman_Basics
    Public resizeHistOutput As Boolean = True
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Y scale negative value in meters (meterMin) X100", -400, -5, -200)
            sliders.setupTrackBar(1, "Y scale positive value in meters (meterMax) X100", 5, 400, 200)
        End If
        kalman = New Kalman_Basics()
        gCloud = New Depth_PointCloud_IMU()

        label1 = "ZY (Side View)"
        task.desc = "Create a 2D side view for ZY histogram of depth in meters - NOTE: x and y scales differ!"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        gCloud.Run()
        Dim imuPC = gCloud.imuPointCloud
        split = imuPC.Split()

        Static minSlider = findSlider("Y scale negative value in meters (meterMin) X100")
        Static maxSlider = findSlider("Y scale positive value in meters (meterMax) X100")
        meterMin = minSlider.value / 100
        meterMax = maxSlider.value / 100

        Dim ranges() = New cv.Rangef() {New cv.Rangef(meterMin, meterMax), New cv.Rangef(0, ocvb.maxZ)}
        Dim histSize() = {gCloud.imuPointCloud.Height, gCloud.imuPointCloud.Width}
        If resizeHistOutput Then histSize = {dst2.Height, dst2.Width}
        cv.Cv2.CalcHist(New cv.Mat() {imuPC}, New Integer() {1, 2}, New cv.Mat, histOutput, 2, histSize, ranges)

        Static histThresholdSlider = findSlider("Histogram threshold")
        dst2 = histOutput.Threshold(histThresholdSlider.value, 255, cv.ThresholdTypes.Binary).Resize(dst1.Size)
        dst2.ConvertTo(dst1, cv.MatType.CV_8UC1)
        dst1 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        cameraLoc = CInt(dst1.Height * Math.Abs(meterMin) / Math.Abs(meterMax - meterMin))

        dst1.Circle(New cv.Point(0, cameraLoc), ocvb.dotSize, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        label1 = "Camera dot below at " + CStr(cameraLoc) + " rows from the top"
        label2 = "Top y = " + Format(meterMin, "#0.00") + " Bottom Y = " + Format(meterMax, "#0.00")
    End Sub
End Class
