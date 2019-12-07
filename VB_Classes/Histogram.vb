Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices



Module histogram_Functions
    Public Sub histogram2DPlot(histogram As cv.Mat, dst As cv.Mat, aBins As Int32, bBins As Int32)
        Dim maxVal As Double
        histogram.MinMaxLoc(0, maxVal)
        Dim hScale = CInt(Math.Ceiling(dst.Rows / aBins))
        Dim sScale = CInt(Math.Ceiling(dst.Cols / bBins))
        For h = 0 To aBins - 1
            For s = 0 To bBins - 1
                Dim binVal = histogram.At(Of Single)(h, s)
                Dim intensity = Math.Round(binVal * 255 / maxVal)
                Dim pt1 = New cv.Point(s * sScale, h * hScale)
                Dim pt2 = New cv.Point((s + 1) * sScale - 1, (h + 1) * hScale - 1)
                If pt1.X >= dst.Cols Then pt1.X = dst.Cols - 1
                If pt1.Y >= dst.Rows Then pt1.Y = dst.Rows - 1
                If pt2.X >= dst.Cols Then pt2.X = dst.Cols - 1
                If pt2.Y >= dst.Rows Then pt2.Y = dst.Rows - 1
                If pt1.X <> pt2.X And pt1.Y <> pt2.Y Then
                    Dim value = cv.Scalar.All(255 - intensity)
                    value = New cv.Scalar(pt1.X * 255 / dst.Cols, pt1.Y * 255 / dst.Rows, 255 - intensity)
                    dst.Rectangle(pt1, pt2, value, -1, cv.LineTypes.AntiAlias)
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
            Dim h = img.Height * (hist.At(Of Single)(i, 0)) / maxVal
            If h = 0 Then h = 5 ' show the color range in the plot
            cv.Cv2.Rectangle(img, New cv.Rect(i * binWidth + 1, img.Height - h, binWidth - 2, h), New cv.Scalar(CInt(180.0 * i / binCount), 255, 255), -1)
        Next
    End Sub

    Public Sub histogramPlot(hist As cv.Mat, dst As cv.Mat, savedMaxVal As Single)
        Dim barWidth = Int(dst.Width / hist.Rows)
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)

        maxVal = Math.Round(maxVal / 1000, 0) * 1000 + 1000

        If maxVal < 0 Then maxVal = savedMaxVal
        If Math.Abs((maxVal - savedMaxVal)) / maxVal < 0.2 Then maxVal = savedMaxVal Else savedMaxVal = Math.Max(maxVal, savedMaxVal)

        dst.SetTo(cv.Scalar.Red)
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
        End If
    End Sub
End Module




' https://github.com/opencv/opencv/blob/master/samples/python/hist.py
Public Class Histogram_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public src As New cv.Mat
    Public dst As New cv.Mat
    Public bins As Int32 = 50
    Public minRange As Int32 = 0
    Public maxRange As Int32 = 255
    Public externalUse As Boolean
    Dim indices As cv.Mat
    Dim plotColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Histogram Bins", 2, 255, 255)
        sliders.setupTrackBar2(ocvb, "Histogram line thickness", 1, 20, 3)
        sliders.setupTrackBar3(ocvb, "Scale Font Size x10", 1, 20, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()

        indices = New cv.Mat(256, 1, cv.MatType.CV_32F)
        For i = 0 To 255
            indices.Set(Of Single)(i, 0, CSng(i))
        Next
        ocvb.desc = "Plot histograms for up to 3 channels."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim thickness = sliders.TrackBar2.Value
        Dim bins = sliders.TrackBar1.Value
        Dim dimensions() = New Integer() {bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}
        If externalUse = False Then
            src = ocvb.color
            dst = ocvb.result1
        End If

        dst.SetTo(0)
        Dim pixelWidth = dst.Cols / bins

        Dim maxVal As Double
        For i = 0 To src.Channels - 1
            Dim hist = New cv.Mat
            cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {i}, New cv.Mat(), hist, 1, dimensions, ranges)
            hist.MinMaxLoc(0, maxVal)
            hist = hist.Normalize(0, dst.Rows, cv.NormTypes.MinMax)
            Dim points = New List(Of cv.Point)
            Dim listOfPoints = New List(Of List(Of cv.Point))
            For j = 0 To bins - 1
                points.Add(New cv.Point(CInt(j * pixelWidth), dst.Rows - hist.At(Of Single)(j, 0)))
            Next
            listOfPoints.Add(points)
            dst.Polylines(listOfPoints, False, plotColors(i), thickness, cv.LineTypes.AntiAlias)
        Next

        maxVal = Math.Round(maxVal / 1000, 0) * 1000 + 1000 ' smooth things out a little for the scale below

        AddPlotScale(dst, maxVal, sliders.TrackBar3.Value / 10)
        ocvb.label1 = "Histogram for Color image above - " + CStr(bins) + " bins"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Histogram_NormalizeGray : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim check As New OptionsCheckbox
    Public histogram As Histogram_KalmanSmoothed
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Min Gray", 0, 255, 0)
        sliders.setupTrackBar2(ocvb, "Max Gray", 0, 255, 255)
        If ocvb.parms.ShowOptions Then sliders.show()

        histogram = New Histogram_KalmanSmoothed(ocvb)
        histogram.externalUse = True

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Normalize Before Histogram"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.show()
        ocvb.desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        histogram.gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If check.Box(0).Checked Then
            cv.Cv2.Normalize(histogram.gray, histogram.gray, sliders.TrackBar1.Value, sliders.TrackBar2.Value, cv.NormTypes.MinMax) ' only minMax is working...
        End If
        histogram.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        histogram.Dispose()
        check.Dispose()
    End Sub
End Class


Public Class Histogram_EqualizeColor : Implements IDisposable
    Dim kalman As Histogram_KalmanSmoothed
    Dim mats As Mat_2to1
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        kalman = New Histogram_KalmanSmoothed(ocvb)
        kalman.externalUse = True
        kalman.sliders.TrackBar1.Value = 40

        mats = New Mat_2to1(ocvb)
        mats.externalUse = True

        ocvb.desc = "Create an equalized histogram of the color image.  Histogram differences are very subtle but image is noticeably enhanced."
        ocvb.label1 = "Image Enhanced with Equalized Histogram"
        ocvb.label2 = "Before (top) and After Green Histograms"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rgb(2) As cv.Mat
        Dim rgbEq(2) As cv.Mat
        cv.Cv2.Split(ocvb.color, rgb)

        For i = 0 To rgb.Count - 1
            rgbEq(i) = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
            cv.Cv2.EqualizeHist(rgb(i), rgbEq(i))
        Next
        cv.Cv2.Merge(rgbEq, ocvb.result1)

        If externalUse = False Then
            Dim test As New cv.Mat
            cv.Cv2.Subtract(rgb(0), rgbEq(0), test)

            kalman.gray = rgb(0).Clone() ' just show the green plane
            kalman.dst = mats.mat(0)
            kalman.plotHist.backColor = cv.Scalar.Green
            kalman.Run(ocvb)

            kalman.gray = rgbEq(0).Clone()
            kalman.dst = mats.mat(1)
            kalman.Run(ocvb)

            mats.Run(ocvb)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        kalman.Dispose()
        mats.Dispose()
    End Sub
End Class


Public Class Histogram_EqualizeGray : Implements IDisposable
    Public histogram As Histogram_KalmanSmoothed
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        histogram = New Histogram_KalmanSmoothed(ocvb)
        ocvb.desc = "Create an equalized histogram of the grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then histogram.gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.EqualizeHist(histogram.gray, histogram.gray)
        histogram.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        histogram.Dispose()
    End Sub
End Class


' https://docs.opencv.org/2.4/modules/imgproc/doc/histograms.html
Public Class Histogram_2D_HueSaturation : Implements IDisposable
    Public histogram As New cv.Mat
    Public dst As New cv.Mat
    Public src As New cv.Mat
    Public hsv As cv.Mat

    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Hue bins", 1, 180, 30) ' quantize hue to 30 levels
        sliders.setupTrackBar2(ocvb, "Saturation bins", 1, 256, 32) ' quantize sat to 32 levels
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Create a histogram for hue and saturation."
        dst = ocvb.result1
        src = ocvb.color
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hsv = src.CvtColor(cv.ColorConversionCodes.RGB2HSV)
        Dim sbins = sliders.TrackBar1.Value
        Dim hbins = sliders.TrackBar2.Value
        Dim histSize() = {hbins, sbins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, sliders.TrackBar1.Maximum - 1), New cv.Rangef(0, sliders.TrackBar2.Maximum - 1)} ' hue ranges from 0-179

        cv.Cv2.CalcHist(New cv.Mat() {hsv}, New Integer() {0, 1}, New cv.Mat(), histogram, 2, histSize, ranges)

        histogram2DPlot(histogram, dst, hbins, sbins)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Histogram_2D_XZ_YZ : Implements IDisposable
    Dim xyDepth As Mat_ImageXYZ_MT
    Dim mats As Mat_4to1
    Dim inrange As Depth_InRangeTrim
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        xyDepth = New Mat_ImageXYZ_MT(ocvb)

        mats = New Mat_4to1(ocvb)
        mats.externalUse = True

        inrange = New Depth_InRangeTrim(ocvb)
        inrange.sliders.TrackBar2.Value = 1500 ' up to x meters away 

        sliders.setupTrackBar1(ocvb, "Histogram X bins", 1, ocvb.color.Width / 2, 30)
        sliders.setupTrackBar2(ocvb, "Histogram Z bins", 1, 200, 100)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Create a 2D histogram for depth in XZ and YZ."
        ocvb.label2 = "Left is XZ (Top View) and Right is YZ (Side View)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        xyDepth.Run(ocvb) ' get xyDepth coordinates - note: in image coordinates not physical coordinates.
        Dim xbins = sliders.TrackBar1.Value
        Dim zbins = sliders.TrackBar2.Value
        Dim histSize() = {xbins, zbins}
        inrange.Run(ocvb)
        Dim minRange = inrange.sliders.TrackBar1.Value
        Dim maxRange = inrange.sliders.TrackBar2.Value

        Dim histogram As New cv.Mat

        Dim rangesX() = New cv.Rangef() {New cv.Rangef(0, ocvb.color.Width - 1), New cv.Rangef(minRange, maxRange)}
        cv.Cv2.CalcHist(New cv.Mat() {xyDepth.xyDepth}, New Integer() {2, 0}, New cv.Mat(), histogram, 2, histSize, rangesX)
        histogram2DPlot(histogram, ocvb.result2, xbins, zbins)
        mats.mat(2) = ocvb.result2.Clone()

        Dim rangesY() = New cv.Rangef() {New cv.Rangef(0, ocvb.color.Height - 1), New cv.Rangef(minRange, maxRange)}
        cv.Cv2.CalcHist(New cv.Mat() {xyDepth.xyDepth}, New Integer() {1, 2}, New cv.Mat(), histogram, 2, histSize, rangesY)
        histogram2DPlot(histogram, ocvb.result2, xbins, zbins)
        mats.mat(3) = ocvb.result2.Clone()
        mats.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        inrange.Dispose()
        xyDepth.Dispose()
        mats.Dispose()
    End Sub
End Class





Public Class Histogram_BackProjectionGrayScale : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim hist As Histogram_KalmanSmoothed
    Public Sub New(ocvb As AlgorithmData)
        hist = New Histogram_KalmanSmoothed(ocvb)
        hist.externalUse = True
        hist.dst = ocvb.result2
        hist.check.Box(0).Checked = False

        sliders.setupTrackBar1(ocvb, "Histogram Bins", 1, 255, 50)
        sliders.setupTrackBar2(ocvb, "Number of neighbors to include", 0, 10, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Create a histogram and back project into the image the grayscale color with the highest occurance."
        ocvb.label2 = "Grayscale Histogram"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.sliders.TrackBar1.Value = sliders.TrackBar1.Value ' reflect the number of bins into the histogram code.

        hist.gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        hist.Run(ocvb)

        Dim minVal As Single, maxVal As Single
        Dim minIdx(2) As Int32, maxIdx(2) As Int32
        hist.histogram.MinMaxIdx(minVal, maxVal, minIdx, maxIdx)
        Dim pixelMin = CInt(255 * maxIdx(0) / hist.sliders.TrackBar1.Value)
        Dim pixelMax = CInt(255 * (maxIdx(0) + 1) / hist.sliders.TrackBar1.Value)
        Dim incr = pixelMax - pixelMin
        Dim neighbors = sliders.TrackBar2.Value
        If neighbors Mod 2 = 0 Then
            pixelMin -= incr * neighbors / 2
            pixelMax += incr * neighbors / 2
        Else
            pixelMin -= incr * ((neighbors / 2) + 1)
            pixelMax += incr * ((neighbors - 1) / 2)
        End If
        pixelMin -= incr
        pixelMax += incr
        Dim mask = hist.gray.InRange(pixelMin, pixelMax)
        ocvb.result1.SetTo(0)
        ocvb.color.CopyTo(ocvb.result1, mask)
        ocvb.label1 = "BackProjection of most frequent pixel + " + CStr(neighbors) + " neighbor" + If(neighbors <> 1, "s", "")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        hist.Dispose()
        sliders.Dispose()
    End Sub
End Class



' https://docs.opencv.org/3.4/dc/df6/tutorial_py_histogram_backprojection.html
Public Class Histogram_BackProjection : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim hist As Histogram_2D_HueSaturation
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Backprojection Mask Threshold", 0, 255, 10)
        If ocvb.parms.ShowOptions Then sliders.show()

        hist = New Histogram_2D_HueSaturation(ocvb)
        hist.dst = ocvb.result2

        ocvb.desc = "Backproject from a hue and saturation histogram."
        ocvb.label1 = "Backprojection of detected hue and saturation."
        ocvb.label2 = "2D Histogram for Hue (X) vs. Saturation (Y)"

        ocvb.drawRect = New cv.Rect(100, 100, 200, 100)  ' an arbitrary rectangle to use for the backprojection.
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.src = ocvb.color(ocvb.drawRect)
        hist.Run(ocvb)
        Dim histogram = hist.histogram.Normalize(0, 255, cv.NormTypes.MinMax)
        Dim bins() = {0, 1}
        Dim hsv = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim mat() As cv.Mat = {hsv}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 180), New cv.Rangef(0, 256)}
        Dim mask As New cv.Mat
        cv.Cv2.CalcBackProject(mat, bins, histogram, mask, ranges)

        mask = mask.Threshold(sliders.TrackBar1.Value, 255, cv.ThresholdTypes.Binary)
        ocvb.result1.SetTo(0)
        ocvb.color.CopyTo(ocvb.result1, mask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        hist.Dispose()
        sliders.Dispose()
    End Sub
End Class




Public Class Histogram_ColorsAndGray : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim check As New OptionsCheckbox
    Dim histogram As Histogram_KalmanSmoothed
    Dim mats As Mat_4to1
    Public Sub New(ocvb As AlgorithmData)
        mats = New Mat_4to1(ocvb)
        mats.externalUse = True

        sliders.setupTrackBar1(ocvb, "Min Gray", 0, 255, 0)
        sliders.setupTrackBar2(ocvb, "Max Gray", 0, 255, 255)
        If ocvb.parms.ShowOptions Then sliders.Show()

        histogram = New Histogram_KalmanSmoothed(ocvb)
        histogram.externalUse = True
        histogram.check.Box(0).Checked = False
        histogram.check.Box(0).Enabled = False ' if we use Kalman, all the plots are identical as the values converge on the gray level setting...
        histogram.sliders.TrackBar1.Value = 40

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Normalize Before Histogram"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()
        ocvb.desc = "Create a histogram of a normalized image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim split = ocvb.color.Split()
        ReDim Preserve split(3)
        split(3) = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        histogram.gray = New cv.Mat
        For i = 0 To split.Length - 1
            If check.Box(0).Checked Then
                cv.Cv2.Normalize(split(i), histogram.gray, sliders.TrackBar1.Value, sliders.TrackBar2.Value, cv.NormTypes.MinMax) ' only minMax is working...
            Else
                histogram.gray = split(i).Clone()
            End If
            histogram.plotHist.backColor = Choose(i + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red, cv.Scalar.PowderBlue)
            histogram.Run(ocvb)
            mats.mat(i) = histogram.dst.Clone()
        Next

        mats.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        histogram.Dispose()
        check.Dispose()
        mats.Dispose()
    End Sub
End Class




Public Class Histogram_KalmanSmoothed : Implements IDisposable
    Public gray As cv.Mat
    Public mask As New cv.Mat
    Public dst As New cv.Mat
    Public externalUse As Boolean

    Public sliders As New OptionsSliders
    Public check As New OptionsCheckbox
    Public histogram As New cv.Mat
    Public mykf As Kalman_kDimension_Options
    Public plotHist As Plot_Histogram
    Dim splitColors() = {cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red}
    Public Sub New(ocvb As AlgorithmData)
        dst = ocvb.result2
        plotHist = New Plot_Histogram(ocvb)
        plotHist.externalUse = True

        mykf = New Kalman_kDimension_Options(ocvb)

        sliders.setupTrackBar1(ocvb, "Histogram Bins", 1, 255, 50)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Create a histogram of the grayscale image and smooth the bar chart with a kalman filter."

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Use Kalman to calm graph"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        ocvb.label1 = "Gray scale input to histogram"
        ocvb.label2 = "Histogram - x=bins/y=count"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static splitIndex As Int32 = -1
        If externalUse = False Then
            Dim split() = cv.Cv2.Split(ocvb.color)
            If ocvb.frameCount Mod 100 = 0 Then
                splitIndex += 1
                If splitIndex > 2 Then splitIndex = 0
            End If
            gray = split(splitIndex).Clone
        End If
        plotHist.bins = sliders.TrackBar1.Value
        plotHist.minRange = 0
        Dim histSize() = {plotHist.bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}

        mykf.kf.kDimension = plotHist.bins
        Dim dimensions() = New Integer() {plotHist.bins}
        cv.Cv2.CalcHist(New cv.Mat() {gray}, New Integer() {0}, mask, histogram, 1, dimensions, ranges)

        ocvb.result1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.label2 = "Plot Histogram bins = " + CStr(plotHist.bins)

        If check.Box(0).Checked Then
            mykf.kf.inputReal = histogram.Clone()
            mykf.Run(ocvb)
            If ocvb.frameCount > 0 Then histogram = mykf.kf.statePoint
        End If

        plotHist.hist = histogram
        plotHist.dst = dst
        If externalUse = False Then plotHist.backColor = splitColors(splitIndex)
        plotHist.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        check.Dispose()
        mykf.Dispose()
        plotHist.Dispose()
    End Sub
End Class




Public Class Histogram_Depth : Implements IDisposable
    Public inrange As Depth_InRangeTrim
    Public sliders As New OptionsSliders
    Public plotHist As Plot_Histogram
    Public Sub New(ocvb As AlgorithmData)
        plotHist = New Plot_Histogram(ocvb)
        plotHist.externalUse = True

        inrange = New Depth_InRangeTrim(ocvb)
        sliders.setupTrackBar1(ocvb, "Histogram Depth Bins", 1, ocvb.color.Width, 10) ' max is the number of columns we have.
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Show depth data as a histogram."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        inrange.Run(ocvb)
        plotHist.minRange = inrange.sliders.TrackBar1.Value
        plotHist.maxRange = inrange.sliders.TrackBar2.Value
        plotHist.bins = sliders.TrackBar1.Value
        Dim histSize() = {plotHist.bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.maxRange)}

        cv.Cv2.CalcHist(New cv.Mat() {ocvb.depth}, New Integer() {0}, New cv.Mat(), plotHist.hist, 1, histSize, ranges)

        plotHist.dst = ocvb.result2
        plotHist.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        inrange.Dispose()
        plotHist.Dispose()
    End Sub
End Class




Public Class Histogram_DepthValleys : Implements IDisposable
    Dim mykf As Kalman_kDimension_Options
    Dim hist As Histogram_Depth
    Dim check As New OptionsCheckbox
    Public boundaryInverse As New List(Of cv.Point)
    Private Class CompareCounts : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just int32?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return -1
            Return 1
        End Function
    End Class
    Private Sub histogramPlotValleys(img As cv.Mat, hist As cv.Mat, plotColors() As cv.Scalar)
        Dim binCount = hist.Height
        Dim binWidth = img.Width / hist.Height
        Dim minVal As Single, maxVal As Single
        hist.MinMaxLoc(minVal, maxVal)
        img.SetTo(0)
        If maxVal = 0 Then Exit Sub
        For i = 0 To binCount - 1
            Dim nextHistCount = hist.At(Of Single)(i, 0)
            Dim h = CInt(img.Height * nextHistCount / maxVal)
            If h = 0 Then h = 1 ' show the color range in the plot
            Dim barRect As cv.Rect
            If binWidth > 3 Then
                barRect = New cv.Rect(CInt(i * binWidth + 1), img.Height - h, CInt(binWidth - 2), h) ' add a column of space between bars.
            Else
                barRect = New cv.Rect(i * binWidth + 1, img.Height - h, binWidth, h)
            End If
            cv.Cv2.Rectangle(img, barRect, plotColors(i), -1)
        Next
    End Sub
    Public Sub New(ocvb As AlgorithmData)
        hist = New Histogram_Depth(ocvb)
        hist.inrange.sliders.TrackBar2.Value = 5000 ' depth to 5 meters.
        hist.sliders.TrackBar1.Value = 40 ' number of bins.

        mykf = New Kalman_kDimension_Options(ocvb)

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Use Kalman Filter to smooth histogram plot."
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        ocvb.label1 = "Histogram clustered by valleys"
        ocvb.desc = "Identify valleys in the Depth histogram."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.Run(ocvb)
        If check.Box(0).Checked Then mykf.kf.kDimension = hist.plotHist.bins

        Dim depthIncr = CInt(hist.inrange.sliders.TrackBar2.Value / hist.sliders.TrackBar1.Value) ' each bar represents this number of millimeters
        Dim pointCount = hist.plotHist.hist.At(Of Single)(0, 0) + hist.plotHist.hist.At(Of Single)(1, 0)
        Dim startDepth = 1
        Dim startEndDepth As cv.Point
        Dim depthBoundaries As New SortedList(Of Single, cv.Point)(New CompareCounts)
        For i = 2 To hist.plotHist.hist.Rows - 3
            Dim prev2 = hist.plotHist.hist.At(Of Single)(i - 2, 0)
            Dim prev = hist.plotHist.hist.At(Of Single)(i - 1, 0)
            Dim curr = hist.plotHist.hist.At(Of Single)(i, 0)
            Dim post = hist.plotHist.hist.At(Of Single)(i + 1, 0)
            Dim post2 = hist.plotHist.hist.At(Of Single)(i + 2, 0)
            pointCount += hist.plotHist.hist.At(Of Single)(i, 0)
            If prev2 < prev Then prev2 = prev
            If post2 < post Then post2 = post
            If curr < prev2 And curr < prev And curr < post And curr < post2 Then
                startEndDepth = New cv.Point(startDepth, i * depthIncr)
                depthBoundaries.Add(pointCount, startEndDepth)
                pointCount = 0
                startDepth = i * depthIncr + 0.1
            End If
        Next

        If check.Box(0).Checked Then
            mykf.kf.inputReal = hist.plotHist.hist.Clone()
            mykf.Run(ocvb)
            If ocvb.frameCount > 0 Then hist.plotHist.hist = mykf.kf.statePoint
        End If

        startEndDepth = New cv.Point(startDepth, hist.inrange.sliders.TrackBar2.Value)
        depthBoundaries.Add(pointCount, startEndDepth) ' capped at the max depth we are observing

        boundaryInverse.Clear()
        For i = depthBoundaries.Count - 1 To 0 Step -1
            boundaryInverse.Add(depthBoundaries.ElementAt(i).Value)
        Next

        Dim plotColors(hist.plotHist.hist.Rows - 1) As cv.Scalar
        For i = 0 To hist.plotHist.hist.Rows - 1
            Dim depth = i * depthIncr + 1
            For j = 0 To boundaryInverse.Count - 1
                Dim startEnd = boundaryInverse.ElementAt(j)
                If depth >= startEnd.X And depth < startEnd.Y Then
                    plotColors(i) = ocvb.colorScalar(j Mod 255)
                    Exit For
                End If
            Next
        Next
        histogramPlotValleys(ocvb.result1, hist.plotHist.hist, plotColors)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        hist.Dispose()
        mykf.Dispose()
    End Sub
End Class





Public Class Histogram_DepthClusters : Implements IDisposable
    Dim valleys As Histogram_DepthValleys
    Public Sub New(ocvb As AlgorithmData)
        valleys = New Histogram_DepthValleys(ocvb)

        ocvb.desc = "Color each of the Depth Clusters found with Histogram_DepthValleys"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        valleys.Run(ocvb)

        ocvb.result2.SetTo(0)
        Dim mask As New cv.Mat
        Dim tmp16 As New cv.Mat
        For i = 0 To valleys.boundaryInverse.Count - 1
            Dim startEndDepth = valleys.boundaryInverse.ElementAt(i)
            cv.Cv2.InRange(ocvb.depth, startEndDepth.X, startEndDepth.Y, tmp16)
            cv.Cv2.ConvertScaleAbs(tmp16, mask)
            ocvb.result2.SetTo(ocvb.colorScalar(i), mask)
        Next
        ocvb.label1 = "Histogram of " + CStr(valleys.boundaryInverse.Count) + " Depth Clusters"
        ocvb.label2 = "Backprojection of " + CStr(valleys.boundaryInverse.Count) + " histogram clusters"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        valleys.Dispose()
    End Sub
End Class