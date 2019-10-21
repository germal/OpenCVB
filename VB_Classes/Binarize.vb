Imports cv = OpenCvSharp

'https://docs.opencv.org/3.4/d7/d4d/tutorial_py_thresholding.html
Public Class Binarize_OTSU : Implements IDisposable
    Dim mats1 As Mat_4to1
    Dim mats2 As Mat_4to1
    Dim plotHist As Plot_Histogram
    Public Sub New(ocvb As AlgorithmData)
        plotHist = New Plot_Histogram(ocvb)
        plotHist.externalUse = True

        mats1 = New Mat_4to1(ocvb)
        mats1.externalUse = True
        mats2 = New Mat_4to1(ocvb)
        mats2.externalUse = True

        ocvb.desc = "Binarize an image using Threshold with OTSU."
        ocvb.label2 = "Note the benefit of Blur in lower right histogram"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result2.SetTo(0)
        Dim w = ocvb.color.Width, h = ocvb.color.Height
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim meanScalar = cv.Cv2.Mean(gray)
        ocvb.label1 = "Threshold 1) binary 2) Binary+OTSU 3) OTSU 4) OTSU+Blur"
        plotHist.bins = 255
        Dim dimensions() = New Integer() {plotHist.bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.bins)}
        Dim histogram(3) As cv.Mat

        For i = 0 To histogram.Length - 1
            histogram(i) = New cv.Mat
            If i = histogram.Length - 1 Then gray = gray.Blur(New cv.Size(5, 5), New cv.Point(3, 3)) ' just blur the last one...
            cv.Cv2.CalcHist(New cv.Mat() {gray}, New Integer() {0}, New cv.Mat(), histogram(i), 1, dimensions, ranges)
            plotHist.hist = histogram(i).Clone()
            plotHist.dst = mats2.mat(i)
            plotHist.Run(ocvb)

            Dim thresholdType = Choose(i + 1, cv.ThresholdTypes.Binary, cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu,
                                              cv.ThresholdTypes.Otsu, cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu)
            Dim thresh = gray.Threshold(meanScalar(0), 255, thresholdType)
            mats1.mat(i) = thresh.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Next

        mats1.Run(ocvb)
        ocvb.result1 = ocvb.result2.Clone() ' mat_4to1 puts output in result2
        mats2.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        mats1.Dispose()
        mats2.Dispose()
        plotHist.Dispose()
    End Sub
End Class




Public Class Binarize_Niblack_Sauvola : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.Label1.Text = "Kernel Size"
        sliders.setupTrackBar1(ocvb, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar2(ocvb, "Niblack k", -1000, 1000, -200)
        sliders.setupTrackBar3(ocvb, "Sauvola k", -1000, 1000, 100)
        sliders.setupTrackBar4(ocvb, "Sauvola r", 1, 100, 64)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Binarize an image using Niblack and Sauvola"
        ocvb.label1 = "Binarize Niblack"
        ocvb.label2 = "Binarize Sauvola"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim grayBin As New cv.Mat
        cv.Extensions.Binarizer.Niblack(gray, grayBin, kernelSize, sliders.TrackBar2.Value / 1000)
        ocvb.result1 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Extensions.Binarizer.Sauvola(gray, grayBin, kernelSize, sliders.TrackBar3.Value / 1000, sliders.TrackBar4.Value)
        ocvb.result2 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Binarize_Niblack_Nick : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.Label1.Text = "Kernel Size"
        sliders.setupTrackBar1(ocvb, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar2(ocvb, "Niblack k", -1000, 1000, -200)
        sliders.setupTrackBar3(ocvb, "Nick k", -1000, 1000, 100)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Binarize an image using Niblack and Nick"
        ocvb.label1 = "Binarize Niblack"
        ocvb.label2 = "Binarize Nick"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayBin As New cv.Mat
        cv.Extensions.Binarizer.Niblack(gray, grayBin, kernelSize, sliders.TrackBar2.Value / 1000)
        ocvb.result1 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Extensions.Binarizer.Nick(gray, grayBin, kernelSize, sliders.TrackBar3.Value / 1000)
        ocvb.result2 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Binarize_Bernson : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.Label1.Text = "Kernel Size"
        sliders.setupTrackBar1(ocvb, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar2(ocvb, "Contrast min", 0, 255, 50)
        sliders.setupTrackBar3(ocvb, "bg Threshold", 0, 255, 100)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.label1 = "Binarize Bernson (Draw Enabled)"

        ocvb.drawRect = New cv.Rect(100, 100, 100, 100)
        ocvb.desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayBin = gray.Clone()
        cv.Extensions.Binarizer.Bernsen(gray(ocvb.drawRect), grayBin(ocvb.drawRect), kernelSize, sliders.TrackBar2.Value, sliders.TrackBar3.Value)
        ocvb.result1 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Binarize_Bernson_MT : Implements IDisposable
    Dim grid As Thread_Grid
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = grid.sliders.TrackBar1.Maximum
        grid.sliders.TrackBar2.Value = grid.sliders.TrackBar2.Minimum

        sliders.Label1.Text = "Kernel Size"
        sliders.setupTrackBar1(ocvb, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar2(ocvb, "Contrast min", 0, 255, 50)
        sliders.setupTrackBar3(ocvb, "bg Threshold", 0, 255, 100)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
        ocvb.label1 = "Binarize Bernson"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        grid.Run(ocvb)
        Dim contrastMin = sliders.TrackBar2.Value
        Dim bgThreshold = sliders.TrackBar3.Value

        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim grayBin = gray(roi).Clone()
            cv.Extensions.Binarizer.Bernsen(gray(roi), grayBin, kernelSize, contrastMin, bgThreshold)
            ocvb.result1(roi) = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        grid.Dispose()
    End Sub
End Class
