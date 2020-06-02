Imports cv = OpenCvSharp
Imports OpenCvSharp.XImgProc

'https://docs.opencv.org/3.4/d7/d4d/tutorial_py_thresholding.html
Public Class Binarize_OTSU
    Inherits ocvbClass
    Dim mats1 As Mat_4to1
    Dim mats2 As Mat_4to1
    Dim plotHist As Plot_Histogram
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        plotHist = New Plot_Histogram(ocvb)

        mats1 = New Mat_4to1(ocvb)
        mats2 = New Mat_4to1(ocvb)

        ocvb.desc = "Binarize an image using Threshold with OTSU."
        label2 = "Histograms correspond to images on the left"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst2.SetTo(0)
        Dim w = src.Width, h = src.Height
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim meanScalar = cv.Cv2.Mean(src)
        label1 = "Threshold 1) binary 2) Binary+OTSU 3) OTSU 4) OTSU+Blur"
        plotHist.bins = 255
        Dim dimensions() = New Integer() {plotHist.bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(plotHist.minRange, plotHist.bins)}
        Dim histogram(3) As cv.Mat

        For i = 0 To histogram.Length - 1
            histogram(i) = New cv.Mat
            If i = histogram.Length - 1 Then src = src.Blur(New cv.Size(5, 5), New cv.Point(3, 3)) ' just blur the last one...
            cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {0}, New cv.Mat(), histogram(i), 1, dimensions, ranges)
            plotHist.hist = histogram(i).Clone()
            plotHist.Run(ocvb)
            mats2.mat(i) = plotHist.dst1
            Dim thresholdType = Choose(i + 1, cv.ThresholdTypes.Binary, cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu,
                                              cv.ThresholdTypes.Otsu, cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu)
            mats1.mat(i) = src.Threshold(meanScalar(0), 255, thresholdType).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Next

        mats1.Run(ocvb)
        dst1 = mats1.dst1
        mats2.Run(ocvb)
        dst2 = mats2.dst1
    End Sub
End Class




Public Class Binarize_Niblack_Sauvola
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Label1.Text = "Kernel Size"
        sliders.setupTrackBar1(ocvb, caller, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar2("Niblack k", -1000, 1000, -200)
        sliders.setupTrackBar3("Sauvola k", -1000, 1000, 100)
        sliders.setupTrackBar4("Sauvola r", 1, 100, 64)

        ocvb.desc = "Binarize an image using Niblack and Sauvola"
        label1 = "Binarize Niblack"
        label2 = "Binarize Sauvola"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim grayBin As New cv.Mat
        cv.Extensions.Binarizer.Niblack(src, grayBin, kernelSize, sliders.TrackBar2.Value / 1000)
        dst1 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Extensions.Binarizer.Sauvola(src, grayBin, kernelSize, sliders.TrackBar3.Value / 1000, sliders.TrackBar4.Value)
        dst2 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Niblack_Nick
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Label1.Text = "Kernel Size"
        sliders.setupTrackBar1(ocvb, caller, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar2("Niblack k", -1000, 1000, -200)
        sliders.setupTrackBar3("Nick k", -1000, 1000, 100)

        ocvb.desc = "Binarize an image using Niblack and Nick"
        label1 = "Binarize Niblack"
        label2 = "Binarize Nick"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayBin As New cv.Mat
        cv.Extensions.Binarizer.Niblack(src, grayBin, kernelSize, sliders.TrackBar2.Value / 1000)
        dst1 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Extensions.Binarizer.Nick(src, grayBin, kernelSize, sliders.TrackBar3.Value / 1000)
        dst2 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Bernson
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Label1.Text = "Kernel Size"
        sliders.setupTrackBar1(ocvb, caller, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar2("Contrast min", 0, 255, 50)
        sliders.setupTrackBar3("bg Threshold", 0, 255, 100)

        label1 = "Binarize Bernson (Draw Enabled)"

        ' ocvb.drawRect = New cv.Rect(100, 100, 100, 100)
        ocvb.desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayBin = gray.Clone()
        If ocvb.drawRect = New cv.Rect() Then
            cv.Extensions.Binarizer.Bernsen(gray, grayBin, kernelSize, sliders.TrackBar2.Value, sliders.TrackBar3.Value)
        Else
            cv.Extensions.Binarizer.Bernsen(gray(ocvb.drawRect), grayBin(ocvb.drawRect), kernelSize, sliders.TrackBar2.Value, sliders.TrackBar3.Value)
        End If
        dst1 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Bernson_MT
    Inherits ocvbClass
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32

        sliders.Label1.Text = "Kernel Size"
        sliders.setupTrackBar1(ocvb, caller, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar2("Contrast min", 0, 255, 50)
        sliders.setupTrackBar3("bg Threshold", 0, 255, 100)

        ocvb.desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
        label1 = "Binarize Bernson"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        grid.Run(ocvb)
        Dim contrastMin = sliders.TrackBar2.Value
        Dim bgThreshold = sliders.TrackBar3.Value

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
            Sub(roi)
                Dim grayBin = src(roi).Clone()
                cv.Extensions.Binarizer.Bernsen(src(roi), grayBin, kernelSize, contrastMin, bgThreshold)
                dst1(roi) = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            End Sub)
    End Sub
End Class

