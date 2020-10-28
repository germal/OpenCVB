Imports cv = OpenCvSharp
Imports mn = MathNet.Spatial.Euclidean
' https://github.com/opencv/opencv/blob/master/samples/python/hist.py
Public Class Histogram_Basics
    Inherits VBparent
    Public histRaw(3 - 1) As cv.Mat
    Public histNormalized(3 - 1) As cv.Mat
    Public bins As Integer = 50
    Public minRange As Integer = 0
    Public maxRange As Integer = 255
    Public backColor = cv.Scalar.Gray
    Public plotRequested As Boolean
    Public plotColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Histogram Bins", 2, 256, 50)
        sliders.setupTrackBar(1, "Histogram line thickness", 1, 20, 3)

        ocvb.desc = "Plot histograms for up to 3 channels."
    End Sub
    Public Sub Run(ocvb As VBocvb)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Min Gray", 0, 255, 0)
        sliders.setupTrackBar(1, "Max Gray", 0, 255, 255)

        histogram = New Histogram_KalmanSmoothed(ocvb)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Normalize Before Histogram"
        check.Box(0).Checked = True
        ocvb.desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        histogram.src = src
        If check.Box(0).Checked Then
            cv.Cv2.Normalize(histogram.src, histogram.src, sliders.trackbar(0).Value, sliders.trackbar(1).Value, cv.NormTypes.MinMax) ' only minMax is working...
        End If
        histogram.Run(ocvb)
        dst1 = histogram.dst1
    End Sub
End Class






' https://docs.opencv.org/2.4/modules/imgproc/doc/histograms.html
Public Class Histogram_2D_HueSaturation
    Inherits VBparent
    Public histogram As New cv.Mat
    Public hsv As cv.Mat

    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Hue bins", 1, 180, 30) ' quantize hue to 30 levels
        sliders.setupTrackBar(1, "Saturation bins", 1, 256, 32) ' quantize sat to 32 levels
        ocvb.desc = "Create a histogram for hue and saturation."
    End Sub
    Public Sub Run(ocvb As VBocvb)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        plotHist = New Plot_Histogram(ocvb)
        plotHist.minRange = 0

        kalman = New Kalman_Basics(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Histogram Bins", 1, 255, 50)

        label2 = "Histogram - x=bins/y=count"
        ocvb.desc = "Create a histogram of the grayscale image and smooth the bar chart with a kalman filter."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static splitIndex As Integer = -1
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
        kalman.Run(ocvb)
        For i = 0 To plotHist.bins - 1
            histogram.Set(Of Single)(i, 0, kalman.kOutput(i))
        Next

        plotHist.hist = histogram
        If standalone Then plotHist.backColor = splitColors(splitIndex)
        plotHist.src = src
        plotHist.Run(ocvb)
        dst1 = plotHist.dst1
        label1 = colorName + " input to histogram"
    End Sub
End Class




Public Class Histogram_Depth
    Inherits VBparent
    Public inrange As Depth_InRange
    Public plotHist As Plot_Histogram
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        plotHist = New Plot_Histogram(ocvb)

        inrange = New Depth_InRange(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Histogram Depth Bins", 2, src.Cols, 50)

        ocvb.desc = "Show depth data as a histogram."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        inrange.src = getDepth32f(ocvb)
        inrange.Run(ocvb)
        plotHist.minRange = inrange.sliders.trackbar(0).Value
        plotHist.maxRange = inrange.sliders.trackbar(1).Value
        plotHist.bins = sliders.trackbar(0).Value

        Dim histSize() = {plotHist.bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}
        cv.Cv2.CalcHist(New cv.Mat() {inrange.depth32f}, New Integer() {0}, New cv.Mat, plotHist.hist, 1, histSize, ranges)

        If standalone Then
            plotHist.Run(ocvb)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        hist = New Histogram_Depth(ocvb)
        hist.inrange.sliders.trackbar(1).Value = 5000 ' depth to 5 meters.
        hist.sliders.trackbar(0).Value = 40 ' number of bins.

        kalman = New Kalman_Basics(ocvb)

        ocvb.desc = "Identify valleys in the Depth histogram."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        hist.Run(ocvb)
        ReDim kalman.kInput(hist.plotHist.hist.Rows - 1)
        For i = 0 To hist.plotHist.hist.Rows - 1
            kalman.kInput(i) = hist.plotHist.hist.Get(Of Single)(i, 0)
        Next
        kalman.Run(ocvb)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        valleys = New Histogram_DepthValleys(ocvb)
        ocvb.desc = "Color each of the Depth Clusters found with Histogram_DepthValleys - stabilized with Kalman."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        valleys.Run(ocvb)
        dst1 = valleys.dst1

        Dim mask As New cv.Mat
        Dim tmp As New cv.Mat
        dst2.SetTo(0)
        For i = 0 To valleys.rangeBoundaries.Count - 1
            Dim startEndDepth = valleys.rangeBoundaries.ElementAt(i)
            cv.Cv2.InRange(getDepth32f(ocvb), startEndDepth.X, startEndDepth.Y, tmp)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        xyz = New Mat_ImageXYZ_MT(ocvb)

        inrange = New Depth_InRange(ocvb)
        inrange.sliders.trackbar(1).Value = 1500 ' up to x meters away

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Histogram X bins", 1, src.Cols, 30)
        sliders.setupTrackBar(1, "Histogram Y bins", 1, src.Rows, 30)
        sliders.setupTrackBar(2, "Histogram Z bins", 1, 200, 100)

        ocvb.desc = "Create a 2D histogram for depth in XZ and YZ."
        label2 = "Left is XZ (Top View) and Right is YZ (Side View)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim xbins = sliders.trackbar(0).Value
        Dim ybins = sliders.trackbar(1).Value
        Dim zbins = sliders.trackbar(2).Value
        Dim minRange = inrange.sliders.trackbar(0).Value
        Dim maxRange = inrange.sliders.trackbar(1).Value

        Dim histogram As New cv.Mat

        Dim rangesX() = New cv.Rangef() {New cv.Rangef(0, src.Width - 1), New cv.Rangef(minRange, maxRange)}
        Dim rangesY() = New cv.Rangef() {New cv.Rangef(0, src.Width - 1), New cv.Rangef(minRange, maxRange)}

        xyz.Run(ocvb)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        kalmanEq = New Histogram_KalmanSmoothed(ocvb)
        kalmanEq.sliders.trackbar(0).Value = 40

        kalman = New Histogram_KalmanSmoothed(ocvb)
        kalman.sliders.trackbar(0).Value = 40

        mats = New Mat_2to1(ocvb)

        ocvb.desc = "Create an equalized histogram of the color image. Image is noticeably enhanced."
        label1 = "Image Enhanced with Equalized Histogram"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        kalman.sliders.trackbar(0).Value = kalmanEq.sliders.trackbar(0).Value
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
            kalman.Run(ocvb)
            mats.mat(0) = kalman.dst1.Clone()

            kalmanEq.src = rgbEq(channel).Clone()
            kalmanEq.Run(ocvb)
            mats.mat(1) = kalmanEq.dst1.Clone()

            mats.Run(ocvb)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        histogramEq = New Histogram_KalmanSmoothed(ocvb)

        histogram = New Histogram_KalmanSmoothed(ocvb)

        label1 = "Before EqualizeHist"
        label2 = "After EqualizeHist"
        ocvb.desc = "Create an equalized histogram of the grayscale image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        histogram.sliders.trackbar(0).Value = histogramEq.sliders.trackbar(0).Value
        histogram.kalman.check.Box(0).Checked = histogramEq.kalman.check.Box(0).Checked

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        histogram.src = src.Clone
        histogram.Run(ocvb)
        dst1 = histogram.dst1.Clone
        cv.Cv2.EqualizeHist(histogram.src, histogramEq.src)
        histogramEq.Run(ocvb)
        dst2 = histogramEq.dst1
    End Sub
End Class





' https://docs.opencv.org/master/d1/db7/tutorial_py_histogram_begins.html
Public Class Histogram_Equalize255
    Inherits VBparent
    Dim eqHist As Histogram_EqualizeColor
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        eqHist = New Histogram_EqualizeColor(ocvb)
        eqHist.kalmanEq.sliders.trackbar(0).Value = 255
        eqHist.kalman.sliders.trackbar(0).Value = 255
        eqHist.displayHist = True

        radio.Setup(ocvb, caller, 3)
        radio.check(0).Text = "Equalize the Blue channel"
        radio.check(1).Text = "Equalize the Green channel"
        radio.check(2).Text = "Equalize the Red channel"
        radio.check(2).Checked = True
        label1 = "Resulting equalized image"
        label2 = "Upper plot is before equalization.  Bottom is after."
        ocvb.desc = "Reproduce the results of the hist.py example with existing algorithms"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        For i = 0 To 3 - 1
            If radio.check(i).Checked Then eqHist.channel = i
        Next
        eqHist.src = src
        eqHist.Run(ocvb)
        dst1 = eqHist.dst1.Clone
        dst2 = eqHist.dst2.Clone
    End Sub
End Class





Public Class Histogram_Simple
    Inherits VBparent
    Public plotHist As Plot_Histogram
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        plotHist = New Plot_Histogram(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Histogram Bins", 2, src.Cols, 50)

        ocvb.desc = "Build a simple and reusable histogram for grayscale images."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        plotHist.bins = sliders.trackbar(0).Value

        Dim histSize() = {sliders.trackbar(0).Value}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}
        cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {0}, New cv.Mat, plotHist.hist, 1, histSize, ranges)

        plotHist.Run(ocvb)
        dst1 = plotHist.dst1
    End Sub
End Class












Public Class Histogram_ColorsAndGray
    Inherits VBparent
    Dim histogram As Histogram_KalmanSmoothed
    Dim mats As Mat_4to1
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        mats = New Mat_4to1(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Min Gray", 0, 255, 0)
        sliders.setupTrackBar(1, "Max Gray", 0, 255, 255)

        histogram = New Histogram_KalmanSmoothed(ocvb)
        histogram.kalman.check.Box(0).Checked = False
        histogram.kalman.check.Box(0).Enabled = False
        histogram.sliders.trackbar(0).Value = 40

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Normalize Before Histogram"
        check.Box(0).Checked = True

        label2 = "Click any quadrant at left to view it below"
        ocvb.desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub Run(ocvb As VBocvb)
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
            histogram.Run(ocvb)
            mats.mat(i) = histogram.dst1.Clone()
        Next

        mats.Run(ocvb)
        dst1 = mats.dst1
        If ocvb.mouseClickFlag And ocvb.mousePicTag = RESULT1 Then setQuadrant(ocvb)
        dst2 = mats.mat(ocvb.quadrantIndex)
    End Sub
End Class





Public Class Histogram_BackProjectionPeak
    Inherits VBparent
    Dim hist As Histogram_KalmanSmoothed
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        hist = New Histogram_KalmanSmoothed(ocvb)
        hist.kalman.check.Box(0).Checked = False

        ocvb.desc = "Create a histogram and back project into the image the grayscale color with the highest occurance."
        label2 = "Grayscale Histogram"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        hist.src = src
        hist.Run(ocvb)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        hist = New Histogram_2D_HueSaturation(ocvb)

        ocvb.desc = "Backproject from a hue and saturation histogram."
        label1 = "X-axis is Hue, Y-axis is Sat.  Draw rectangle to isolate ranges"
        label2 = "Backprojection of detected hue and saturation."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        hist.src = src
        hist.Run(ocvb)
        dst1 = hist.dst1
        Static hueBins = hist.sliders.trackbar(0).Value
        Static satBins = hist.sliders.trackbar(1).Value
        If hueBins <> hist.sliders.trackbar(0).Value Or satBins <> hist.sliders.trackbar(1).Value Then
            ocvb.drawRectClear = True
            hueBins = hist.sliders.trackbar(0).Value
            satBins = hist.sliders.trackbar(1).Value
        End If

        Dim unitsPerHueBin = 180 / hueBins
        Dim unitsPerSatBin = 255 / satBins
        Dim minHue = 0, maxHue = 180, minSat = 0, maxSat = 255
        If ocvb.drawRect.Width <> 0 And ocvb.drawRect.Height <> 0 Then
            Dim intBin = Math.Floor(hueBins * ocvb.drawRect.X / dst1.Width)
            minHue = intBin * unitsPerHueBin
            intBin = Math.Ceiling(hueBins * (ocvb.drawRect.X + ocvb.drawRect.Width) / dst1.Width)
            maxHue = intBin * unitsPerHueBin

            intBin = Math.Floor(satBins * ocvb.drawRect.Y / dst1.Height)
            minSat = intBin * unitsPerSatBin
            intBin = Math.Ceiling(satBins * (ocvb.drawRect.Y + ocvb.drawRect.Height) / dst1.Height)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        hueSat = New Brightness_Hue(ocvb)
        hist2d = New Histogram_BackProjection2D(ocvb)
        mats = New Mat_4to1(ocvb)
        ocvb.quadrantIndex = QUAD3
        label2 = "Click any quadrant at left to view it below"
        ocvb.desc = "Compare the hue and brightness images and the results of the histogram_backprojection2d"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        hueSat.src = src
        hueSat.Run(ocvb)
        mats.mat(0) = hueSat.dst1
        mats.mat(1) = hueSat.dst2

        hist2d.src = src
        hist2d.Run(ocvb)
        mats.mat(2) = hist2d.dst2
        mats.mat(3) = hist2d.dst1

        mats.Run(ocvb)
        dst1 = mats.dst1
        If ocvb.mouseClickFlag And ocvb.mousePicTag = RESULT1 Then setQuadrant(ocvb)
        dst2 = mats.mat(ocvb.quadrantIndex)
    End Sub
End Class








' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class Histogram_BackProjectionGrayscale
    Inherits VBparent
    Dim hist As Histogram_KalmanSmoothed
    Public mats As Mat_4to1
    Public histIndex As Integer
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        hist = New Histogram_KalmanSmoothed(ocvb)
        Dim binSlider = findSlider("Histogram Bins")
        binSlider.Value = 10
        mats = New Mat_4to1(ocvb)

        label1 = "Move mouse to backproject each histogram column"
        ocvb.desc = "Explore Backprojection of each element of a grayscale histogram."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        hist.src = src
        hist.Run(ocvb)
        mats.mat(0) = hist.dst1

        histIndex = If(standalone, Math.Floor(hist.histogram.Rows * ocvb.mousePoint.X / src.Width), histIndex) ' provided externally when not standalone.
        Dim barWidth = dst1.Width / hist.sliders.trackbar(0).Value
        Dim barRange = 255 / hist.sliders.trackbar(0).Value

        Dim mask As New cv.Mat
        For i = 0 To 4 - 1
            Dim index = (histIndex + i)
            If index >= hist.histogram.Rows Then index = i - 1
            Dim ranges() = New cv.Rangef() {New cv.Rangef(index * barRange, (index + 1) * barRange)}
            Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) '  = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
            Dim mat() As cv.Mat = {gray}
            Dim bins() = {0}
            cv.Cv2.CalcBackProject(mat, bins, hist.histogram, mask, ranges)
            gray.SetTo(255)
            gray.SetTo(0, mask)
            If i = 0 Then
                dst2 = gray.Clone
                If standalone = False Then Exit For ' minimize work when not running standalone.
            Else
                mats.mat(i) = gray.Clone
            End If
        Next

        label2 = "Backprojection index " + CStr(histIndex) + " with " + Format(hist.histogram.Get(Of Single)(histIndex, 0), "#0") + " samples"
        If standalone Then
            mats.mat(0).Rectangle(New cv.Rect(barWidth * histIndex, 0, barWidth, dst1.Height), cv.Scalar.Yellow, 5)
            Dim tmp = mats.mat(0).Clone
            mats.mat(0) = mats.mat(1).Clone ' avoids clipping the tops of the bar chart.  
            mats.mat(1) = tmp
            mats.Run(ocvb)
            dst1 = mats.dst1
        End If
    End Sub
End Class






Public Class Histogram_HighlightSide
    Inherits VBparent
    Public sideview As Histogram_2D_SideView
    Public topview As Histogram_2D_TopView
    Dim palette As Palette_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        palette = New Palette_Basics(ocvb)
        sideview = New Histogram_2D_SideView(ocvb)
        topview = New Histogram_2D_TopView(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Display the top x highlights", 1, 1000, 50)

        ocvb.desc = "Highlight the histogram projections where concentrations are highest"
    End Sub
    Private Function plotHighlights(ocvb As VBocvb, histOutput As cv.Mat, dst As cv.Mat) As String
        Dim tmp = histOutput.Resize(New cv.Size(histOutput.Width / 10, histOutput.Height / 10))
        Dim pts As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalIntegerInverted)
        For y = 0 To tmp.Height - 1
            For x = 1 To tmp.Width - 1 ' skip the first column to avoid count at 0,0,0 in world coordinates (the count of points with no depth)
                Dim val = tmp.Get(Of Single)(y, x)
                If val > 10 Then
                    pts.Add(val, New cv.Point(x * 10, y * 10))
                End If
            Next
        Next

        Static topXslider = findSlider("Display the top x highlights")
        Dim topX = topXslider.value
        For i = 0 To Math.Min(pts.Count - 1, topX - 1)
            Dim pt = pts.ElementAt(i).Value
            dst.Circle(pt, ocvb.dotSize, cv.Scalar.All((i * 27 + 100) Mod 255), -1, cv.LineTypes.AntiAlias)
        Next
        palette.src = dst
        palette.Run(ocvb)
        Return CStr(pts.Count) + " highlights. Max=" + CStr(pts.ElementAt(0).Key)
    End Function
    Public Sub Run(ocvb As VBocvb)
        sideview.Run(ocvb)
        dst1 = sideview.dst1
        Dim noDepth = sideview.histOutput.Get(Of Single)(sideview.histOutput.Height / 2, 0)
        label1 = "SideView " + plotHighlights(ocvb, sideview.histOutput, dst1) + " No depth: " + CStr(CInt(noDepth / 1000)) + "k"
        dst1 = palette.dst1.Clone

        topview.Run(ocvb)
        dst2 = topview.dst1
        label2 = "TopView " + plotHighlights(ocvb, topview.histOutput, dst2) + " No depth: " + CStr(CInt(noDepth / 1000)) + "k"
        dst2 = palette.dst1.Clone
    End Sub
End Class






Public Class Histogram_2D_TopView
    Inherits VBparent
    Public gCloudIMU As Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Public markers(2 - 1) As cv.Point2f
    Dim cmat As PointCloud_Colorize
    Dim cameraXSlider As Windows.Forms.TrackBar
    Dim frustrumSlider As Windows.Forms.TrackBar
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        cmat = New PointCloud_Colorize(ocvb)
        gCloudIMU = New Depth_PointCloud_IMU(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "TopView Frustrum adjustment", 1, 300, 175)
        sliders.setupTrackBar(1, "TopCameraPoint.x adjustment", -10, 10, 0)
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
                cameraXslider.Value = If(ocvb.resolutionIndex = 1, 4, 8)
            Case VB_Classes.ActiveTask.algParms.camNames.D435i
                frustrumSlider.Value = 175
                cameraXSlider.Value = 0
            Case VB_Classes.ActiveTask.algParms.camNames.D455
                frustrumSlider.Value = 184
                cameraXSlider.Value = 0
        End Select

        label1 = "XZ (Top View)"
        ocvb.desc = "Create a 2D histogram for depth in XZ (top view.)"
    End Sub
    Private Function computeFrustrumLine(ocvb As VBocvb, marker As cv.Point2f, x As Integer) As cv.Point2f
        Dim m = (marker.Y - ocvb.topCameraPoint.Y) / (marker.X - ocvb.topCameraPoint.X)
        Dim b = marker.Y - marker.X * m
        Return New cv.Point2f(x, m * x + b)
    End Function
    Public Sub Run(ocvb As VBocvb)
        ocvb.topCameraPoint = New cv.Point(src.Width / 2 + cameraXSlider.Value, CInt(src.Height))

        gCloudIMU.Run(ocvb)

        Static frustrumSlider = findSlider("TopView Frustrum adjustment")
        Dim fFactor = ocvb.maxZ * frustrumSlider.Value / 100 / 2

        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, ocvb.maxZ), New cv.Rangef(-fFactor, fFactor)}
        Dim histSize() = {dst1.Height, dst1.Width}
        cv.Cv2.CalcHist(New cv.Mat() {gCloudIMU.imuPointCloud}, New Integer() {2, 0}, New cv.Mat, histOutput, 2, histSize, ranges)

        histOutput = histOutput.Flip(cv.FlipMode.X)
        Static histThresholdSlider = findSlider("Histogram threshold")
        dst1 = histOutput.Threshold(histThresholdSlider.Value, 255, cv.ThresholdTypes.Binary).Resize(dst1.Size)
        dst1.ConvertTo(dst1, cv.MatType.CV_8UC1)
        If standalone Then
            dst2 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst2 = cmat.CameraLocationBot(ocvb, dst2)
        End If
    End Sub
End Class








Public Class Histogram_2D_SideView
    Inherits VBparent
    Public gCloudIMU As Depth_PointCloud_IMU
    Public histOutput As New cv.Mat
    Dim cameraYSlider As Windows.Forms.TrackBar
    Dim frustrumSlider As Windows.Forms.TrackBar
    Dim cmat As PointCloud_Colorize
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        cmat = New PointCloud_Colorize(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "SideView Frustrum adjustment", 1, 100, 57)
        sliders.setupTrackBar(1, "sideCameraPoint.x adjustment", -100, 100, 0)
        frustrumSlider = findSlider("SideView Frustrum adjustment")
        cameraYSlider = findSlider("sideCameraPoint.x adjustment")

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
        gCloudIMU = New Depth_PointCloud_IMU(ocvb)
        Dim thresholdSlider = findSlider("Histogram threshold")
        If standalone Then thresholdSlider.Value = 1

        label1 = "ZY (Side View)"
        ocvb.desc = "Create a 2D histogram for depth in ZY (side view.)"
    End Sub
    Private Function rotatePoint(ocvb As VBocvb, pt As cv.Point2f) As cv.Point2f
        Dim rPt = New cv.Point2f(-(pt.Y - dst1.Height), pt.X - dst1.Height)
        Return New cv.Point2f((rPt.X + ocvb.sideCameraPoint.X), (rPt.Y + ocvb.sideCameraPoint.Y))
    End Function
    Private Function computeFrustrumLine(ocvb As VBocvb, marker As cv.Point2f) As cv.Point2f
        Dim m = (marker.Y - ocvb.sideCameraPoint.Y) / (marker.X - ocvb.sideCameraPoint.X)
        Dim b = marker.Y - marker.X * m
        Return New cv.Point2f(dst1.Width, m * dst1.Width + b)
    End Function
    Public Sub Run(ocvb As VBocvb)
        gCloudIMU.Run(ocvb)

        ocvb.pixelsPerMeterH = dst1.Width / ocvb.maxZ
        Dim fovAngle = ocvb.vFov
        ocvb.pixelsPerMeterV = ocvb.pixelsPerMeterH * Math.Tan((fovAngle / 2) * cv.Cv2.PI / 180)

        ocvb.sideCameraPoint = New cv.Point(0, src.Height / 2 + cameraYSlider.Value)

        Static frustrumSlider = findSlider("SideView Frustrum adjustment")
        ocvb.v2hRatio = ocvb.maxZ * frustrumSlider.Value / 100 / 2

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-ocvb.v2hRatio, ocvb.v2hRatio), New cv.Rangef(0, ocvb.maxZ)}
        Dim histSize() = {dst1.Height, dst1.Width}
        cv.Cv2.CalcHist(New cv.Mat() {gCloudIMU.imuPointCloud}, New Integer() {1, 2}, New cv.Mat, histOutput, 2, histSize, ranges)

        Static histThresholdSlider = findSlider("Histogram threshold")
        Dim tmp = histOutput.Threshold(histThresholdSlider.Value, 255, cv.ThresholdTypes.Binary).Resize(dst1.Size)
        tmp.ConvertTo(dst1, cv.MatType.CV_8UC1)

        If standalone Then
            dst2 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst2 = cmat.CameraLocationSide(ocvb, dst2)
        End If
    End Sub
End Class
