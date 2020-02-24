Imports cv = OpenCvSharp
Public Class Math_Subtract : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Red", 0, 255, 255)
        sliders.setupTrackBar2(ocvb, "Green", 0, 255, 255)
        sliders.setupTrackBar3(ocvb, "Blue", 0, 255, 255)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Invert the image colors using subtract"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim tmp = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC3)
        tmp.SetTo(New cv.Scalar(sliders.TrackBar3.Value, sliders.TrackBar2.Value, sliders.TrackBar1.Value))
        cv.Cv2.Subtract(tmp, ocvb.color, ocvb.result1)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Module Math_Functions
    Public Function computeMedian(src As cv.Mat, mask As cv.Mat, bins As Int32, rangeMin As Single, rangeMax As Single) As Double
        Dim dimensions() = New Integer() {bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(rangeMin, rangeMax)}

        Dim hist As New cv.Mat()
        cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {0}, mask, hist, 1, dimensions, ranges)
        Dim totalPixels = mask.CountNonZero()
        If totalPixels = 0 Then totalPixels = src.Total
        Dim halfPixels = totalPixels / 2

        Dim median As Double
        Dim cdfVal As Double = hist.At(Of Single)(0)
        For i = 1 To bins - 1
            cdfVal += hist.At(Of Single)(i)
            If cdfVal > halfPixels Then
                median = i * (rangeMax - rangeMin) / bins
                Exit For
            End If
        Next
        Return median
    End Function
End Module



Public Class Math_Median_CDF : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public src As cv.Mat
    Dim dst As cv.mat
    Public medianVal As Double
    Public rangeMin As Integer = 0
    Public rangeMax As Integer = 255
    Public externalUse As Boolean
    Public bins As Int32 = 10
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Histogram Bins", 4, 1000, 100)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Compute the src image median"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            bins = sliders.TrackBar1.Value
            src = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If

        medianVal = computeMedian(src, New cv.Mat, bins, rangeMin, rangeMax)

        If externalUse = False Then
            Dim mask = New cv.Mat
            mask = src.GreaterThan(medianVal)
            ocvb.result1.SetTo(0)
            ocvb.color.CopyTo(ocvb.result1, mask)
            ocvb.label1 = "Grayscale pixels > " + Format(medianVal, "#0.0")

            cv.Cv2.BitwiseNot(mask, mask)
            ocvb.result2.SetTo(0)
            ocvb.color.CopyTo(ocvb.result2, mask) ' show the other half.
            ocvb.label2 = "Grayscale pixels < " + Format(medianVal, "#0.0")
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If sliders IsNot Nothing Then sliders.Dispose()
    End Sub
End Class





Public Class Math_DepthMeanStdev : Implements IDisposable
    Dim minMax As Depth_Stable
    Public Sub New(ocvb As AlgorithmData)
        minMax = New Depth_Stable(ocvb)
        ocvb.desc = "This algorithm shows that just using the max depth at each pixel does not improve depth!  Mean and stdev don't change."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        minMax.Run(ocvb)
        Dim mean As Single = 0, stdev As Single = 0
        Dim mask = ocvb.result2 ' the mask for stable depth.
        If mask.Size() <> ocvb.depth16.Size() Then mask = mask.Resize(ocvb.depth16.Size())
        cv.Cv2.MeanStdDev(ocvb.depth16, mean, stdev, mask)
        ocvb.label2 = "stablized depth mean=" + Format(mean, "#0.0") + " stdev=" + Format(stdev, "#0.0")
        cv.Cv2.MeanStdDev(ocvb.depth16, mean, stdev)
        ocvb.label1 = "raw depth mean=" + Format(mean, "#0.0") + " stdev=" + Format(stdev, "#0.0")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        minMax.Dispose()
    End Sub
End Class





Public Class Math_RGBCorrelation : Implements IDisposable
    Dim flow As Font_FlowText
    Dim corr As MatchTemplate_Correlation
    Public Sub New(ocvb As AlgorithmData)
        flow = New Font_FlowText(ocvb)
        flow.externalUse = True
        flow.result1or2 = RESULT2

        corr = New MatchTemplate_Correlation(ocvb)
        corr.externalUse = True
        corr.reportFreq = 1

        ocvb.desc = "Compute the correlation coefficient of Red-Green and Red-Blue and Green-Blue"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim split = ocvb.color.Split()
        corr.sample1 = split(0)
        corr.sample2 = split(1)
        corr.Run(ocvb)
        Dim blueGreenCorrelation = "Blue-Green " + ocvb.label1

        corr.sample1 = split(2)
        corr.sample2 = split(1)
        corr.Run(ocvb)
        Dim redGreenCorrelation = "Red-Green " + ocvb.label1

        corr.sample1 = split(2)
        corr.sample2 = split(0)
        corr.Run(ocvb)
        Dim redBlueCorrelation = "Red-Blue " + ocvb.label1

        flow.msgs.Add(blueGreenCorrelation + " " + redGreenCorrelation + " " + redBlueCorrelation)
        flow.Run(ocvb)
        ocvb.label1 = ""
        ocvb.label2 = "Log of " + corr.matchText
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        corr.Dispose()
        flow.Dispose()
    End Sub
End Class