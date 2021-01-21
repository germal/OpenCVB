Imports cv = OpenCvSharp
Imports System.Threading
Public Class Math_Subtract
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Red", 0, 255, 255)
            sliders.setupTrackBar(1, "Green", 0, 255, 255)
            sliders.setupTrackBar(2, "Blue", 0, 255, 255)
        End If

        task.desc = "Invert the image colors using subtract"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim tmp = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        tmp.SetTo(New cv.Scalar(sliders.trackbar(2).Value, sliders.trackbar(1).Value, sliders.trackbar(0).Value))
        cv.Cv2.Subtract(tmp, src, dst1)
    End Sub
End Class



Module Math_Functions
    Public Function computeMedian(src As cv.Mat, mask As cv.Mat, totalPixels As Integer, bins As integer, rangeMin As Single, rangeMax As Single) As Double
        Dim dimensions() = New Integer() {bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(rangeMin, rangeMax)}

        Dim hist As New cv.Mat()
        cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {0}, mask, hist, 1, dimensions, ranges)
        Dim halfPixels = totalPixels / 2

        Dim median As Double
        Dim cdfVal As Double = hist.Get(Of Single)(0)
        For i = 1 To bins - 1
            cdfVal += hist.Get(Of Single)(i)
            If cdfVal >= halfPixels Then
                median = (rangeMax - rangeMin) * i / bins
                Exit For
            End If
        Next
        Return median
    End Function
End Module



Public Class Math_Median_CDF
    Inherits VBparent
    Public medianVal As Double
    Public rangeMin As Integer = 0
    Public rangeMax As Integer = 255
    Public bins As integer = 10
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Histogram Bins", 4, 1000, 100)
        End If
        task.desc = "Compute the src image median"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If standalone or task.intermediateReview = caller Then bins = sliders.trackbar(0).Value

        medianVal = computeMedian(src, New cv.Mat, src.Total, bins, rangeMin, rangeMax)

        If standalone or task.intermediateReview = caller Then
            Dim mask = New cv.Mat
            mask = src.GreaterThan(medianVal)

            dst1.SetTo(0)
            src.CopyTo(dst1, mask)
            label1 = "Grayscale pixels > " + Format(medianVal, "#0.0")

            cv.Cv2.BitwiseNot(mask, mask)
            dst2.SetTo(0)
            src.CopyTo(dst2, mask) ' show the other half.
            label2 = "Grayscale pixels < " + Format(medianVal, "#0.0")
        End If
    End Sub
End Class





Public Class Math_DepthMeanStdev
    Inherits VBparent
    Dim minMax As Depth_NotMissing
    Public Sub New()
        initParent()
        minMax = New Depth_NotMissing()
        task.desc = "This algorithm shows that just using the max depth at each pixel does not improve quality of measurement"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        minMax.src = src
        minMax.Run()
        Dim mean As Single = 0, stdev As Single = 0
        Dim mask = minMax.dst2 ' the mask for stable depth.
        dst2.SetTo(0)
        task.RGBDepth.CopyTo(dst2, mask)
        cv.Cv2.MeanStdDev(task.depth32f, mean, stdev, mask)
        label2 = "stablized depth mean=" + Format(mean, "#0.0") + " stdev=" + Format(stdev, "#0.0")

        dst1 = task.RGBDepth
        cv.Cv2.MeanStdDev(task.depth32f, mean, stdev)
        label1 = "raw depth mean=" + Format(mean, "#0.0") + " stdev=" + Format(stdev, "#0.0")
    End Sub
End Class





Public Class Math_RGBCorrelation
    Inherits VBparent
    Dim flow As Font_FlowText
    Dim corr As MatchTemplate_Basics
    Public Sub New()
        initParent()
        flow = New Font_FlowText()

        corr = New MatchTemplate_Basics()
        task.desc = "Compute the correlation coefficient of Red-Green and Red-Blue and Green-Blue"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim split = src.Split()
        corr.sample = split(0)
        corr.searchMat = split(1)
        corr.Run()
        Dim blueGreenCorrelation = "Blue-Green " + corr.label1

        corr.sample = split(2)
        corr.searchMat = split(1)
        corr.Run()
        Dim redGreenCorrelation = "Red-Green " + corr.label1

        corr.sample = split(2)
        corr.searchMat = split(0)
        corr.Run()
        Dim redBlueCorrelation = "Red-Blue " + corr.label1

        flow.msgs.Add(blueGreenCorrelation + " " + redGreenCorrelation + " " + redBlueCorrelation)
        flow.Run()
        label1 = "Log of " + corr.matchText
    End Sub
End Class





Public Class Math_ImageAverage
    Inherits VBparent
    Dim images As New List(Of cv.Mat)
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Average - number of input images", 1, 100, 10)
        End If
        task.desc = "Create an image that is the mean of x number of previous images."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static avgSlider = findSlider("Average - number of input images")
        Static saveImageCount = avgSlider.Value
        If avgSlider.Value <> saveImageCount Then
            saveImageCount = avgSlider.Value
            images.Clear()
        End If
        Dim nextImage As New cv.Mat
        If src.Type <> cv.MatType.CV_32F Then src.ConvertTo(nextImage, cv.MatType.CV_32F) Else nextImage = src
        cv.Cv2.Multiply(nextImage, cv.Scalar.All(1 / saveImageCount), nextImage)
        images.Add(nextImage.Clone())

        nextImage.SetTo(0)
        For Each img In images
            nextImage += img
        Next
        If images.Count > saveImageCount Then images.RemoveAt(0)
        If nextImage.Type <> src.Type Then nextImage.ConvertTo(dst1, src.Type) Else dst1 = nextImage
        label1 = "Average image over previous " + CStr(avgSlider.value) + " images"
    End Sub
End Class













Public Class Math_Stdev
    Inherits VBparent
    Dim grid As Thread_Grid
    Public mask As cv.Mat
    Public Sub New()
        initParent()
        grid = New Thread_Grid

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Stdev Threshold", 0, 100, 20)
        End If

        mask = New cv.Mat(dst1.Size, cv.MatType.CV_8U)
        task.desc = "Compute the standard deviation in each segment"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim updateCount As Integer
        mask.SetTo(0)
        Dim font = cv.HersheyFonts.HersheyComplex
        Dim fsize = ocvb.fontSize / 3

        grid.Run()

        dst1 = src.Clone
        If dst1.Channels = 3 Then dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static stdevSlider = findSlider("Stdev Threshold")
        Dim stdevThreshold = CSng(stdevSlider.Value)

        Static lastFrame As cv.Mat = dst1.Clone()
        Dim saveFrame As cv.Mat = dst1.Clone
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim mean As Single = 0, stdev As Single = 0
            cv.Cv2.MeanStdDev(dst1(roi), mean, stdev)
            If stdev < stdevThreshold Then
                Interlocked.Increment(updateCount)
                Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                cv.Cv2.PutText(dst1, Format(mean, "#0"), pt, font, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                cv.Cv2.PutText(dst1, Format(stdev, "#0.00"), New cv.Point(pt.X, roi.Y + roi.Height - 4), font, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            Else
                mask(roi).SetTo(255)
                dst1(roi).SetTo(0)
            End If
        End Sub)
        dst1.SetTo(255, grid.gridMask)
        dst2.SetTo(0)
        saveFrame.CopyTo(dst2, mask)
        lastFrame = saveFrame
        Dim stdevPercent = " stdev " + Format(stdevSlider.value, "0.0")
        label1 = CStr(updateCount) + " of " + CStr(grid.roiList.Count) + " segments with < " + stdevPercent
        label2 = CStr(grid.roiList.Count - updateCount) + " out of " + CStr(grid.roiList.Count) + " had stdev > " + Format(stdevSlider.value, "0.0")
    End Sub
End Class