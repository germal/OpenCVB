Imports cv = OpenCvSharp
Public Class Math_Subtract
    Inherits VBparent
    Public Sub New()
        initParent()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Red", 0, 255, 255)
        sliders.setupTrackBar(1, "Green", 0, 255, 255)
        sliders.setupTrackBar(2, "Blue", 0, 255, 255)
        ocvb.desc = "Invert the image colors using subtract"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
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
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Histogram Bins", 4, 1000, 100)
        ocvb.desc = "Compute the src image median"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If standalone Then bins = sliders.trackbar(0).Value

        medianVal = computeMedian(src, New cv.Mat, src.Total, bins, rangeMin, rangeMax)

        If standalone Then
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
    Dim minMax As Depth_Stable
    Public Sub New()
        initParent()
        minMax = New Depth_Stable()
        ocvb.desc = "This algorithm shows that just using the max depth at each pixel does not improve quality of measurement"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        minMax.src = src
        minMax.Run()
        Dim mean As Single = 0, stdev As Single = 0
        Dim mask = minMax.dst2 ' the mask for stable depth.
        dst2.SetTo(0)
        ocvb.task.RGBDepth.CopyTo(dst2, mask)
        Dim depth32f = getDepth32f()
        cv.Cv2.MeanStdDev(depth32f, mean, stdev, mask)
        label2 = "stablized depth mean=" + Format(mean, "#0.0") + " stdev=" + Format(stdev, "#0.0")

        dst1 = ocvb.task.RGBDepth
        cv.Cv2.MeanStdDev(depth32f, mean, stdev)
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
        ocvb.desc = "Compute the correlation coefficient of Red-Green and Red-Blue and Green-Blue"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim split = src.Split()
        corr.sample1 = split(0)
        corr.sample2 = split(1)
        corr.Run()
        Dim blueGreenCorrelation = "Blue-Green " + corr.label1

        corr.sample1 = split(2)
        corr.sample2 = split(1)
        corr.Run()
        Dim redGreenCorrelation = "Red-Green " + corr.label1

        corr.sample1 = split(2)
        corr.sample2 = split(0)
        corr.Run()
        Dim redBlueCorrelation = "Red-Blue " + corr.label1

        flow.msgs.Add(blueGreenCorrelation + " " + redGreenCorrelation + " " + redBlueCorrelation)
        flow.Run()
        label1 = "Log of " + corr.matchText
    End Sub
End Class

