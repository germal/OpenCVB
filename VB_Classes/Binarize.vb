Imports cv = OpenCvSharp
Imports OpenCvSharp.XImgProc

' https://docs.opencv.org/3.4/d7/d4d/tutorial_py_thresholding.html
' https://www.learnopencv.com/otsu-thresholding-with-opencv/?ck_subscriber_id=785741175
' https://github.com/spmallick/learnopencv/tree/master/otsu-method?ck_subscriber_id=785741175
Public Class Binarize_Basics
    Inherits ocvbClass
    Public thresholdType = cv.ThresholdTypes.Otsu
    Public minRange = 0
    Public maxRange = 255
    Public histogram As cv.Mat
    Public meanScalar As cv.Scalar
    Public blurRequest As Boolean
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            meanScalar = cv.Cv2.Mean(src)
            blurRequest = True
        End If
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim dimensions() = New Integer() {maxRange}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}
        If blurRequest Then src = src.Blur(New cv.Size(5, 5), New cv.Point(3, 3)) ' just blur the last one...
        histogram = New cv.Mat
        cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {0}, New cv.Mat(), histogram, 1, dimensions, ranges)
        dst1 = src.Threshold(meanScalar(0), 255, thresholdType).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class





'https://docs.opencv.org/3.4/d7/d4d/tutorial_py_thresholding.html
Public Class Binarize_OTSU
    Inherits ocvbClass
    Dim mats1 As Mat_4to1
    Dim mats2 As Mat_4to1
    Dim plotHist As Plot_Histogram
    Dim binarize As Binarize_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        binarize = New Binarize_Basics(ocvb)

        plotHist = New Plot_Histogram(ocvb)

        mats1 = New Mat_4to1(ocvb)
        mats2 = New Mat_4to1(ocvb)

        label1 = "Threshold 1) binary 2) Binary+OTSU 3) OTSU 4) OTSU+Blur"
        label2 = "Histograms correspond to images on the left"
        ocvb.desc = "Binarize an image using Threshold with OTSU."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst2.SetTo(0)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        binarize.meanScalar = cv.Cv2.Mean(src)
        binarize.src = src
        For i = 0 To 4 - 1
            binarize.thresholdType = Choose(i + 1, cv.ThresholdTypes.Binary, cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu,
                                            cv.ThresholdTypes.Otsu, cv.ThresholdTypes.Binary + cv.ThresholdTypes.Otsu)
            If i = 3 Then binarize.blurRequest = True
            binarize.Run(ocvb)
            mats1.mat(i) = binarize.dst1.Clone()
            plotHist.hist = binarize.histogram.Clone()
            plotHist.Run(ocvb)
            mats2.mat(i) = plotHist.dst1.Clone()
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
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar(1, "Niblack k", -1000, 1000, -200)
        sliders.setupTrackBar(2, "Sauvola k", -1000, 1000, 100)
        sliders.setupTrackBar(3, "Sauvola r", 1, 100, 64)

        ocvb.desc = "Binarize an image using Niblack and Sauvola"
        label1 = "Binarize Niblack"
        label2 = "Binarize Sauvola"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.trackbar(0).Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim grayBin As New cv.Mat
        cv.Extensions.Binarizer.Niblack(src, grayBin, kernelSize, sliders.trackbar(1).Value / 1000)
        dst1 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Extensions.Binarizer.Sauvola(src, grayBin, kernelSize, sliders.trackbar(2).Value / 1000, sliders.trackbar(3).Value)
        dst2 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Niblack_Nick
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar(1, "Niblack k", -1000, 1000, -200)
        sliders.setupTrackBar(2, "Nick k", -1000, 1000, 100)

        ocvb.desc = "Binarize an image using Niblack and Nick"
        label1 = "Binarize Niblack"
        label2 = "Binarize Nick"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.trackbar(0).Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayBin As New cv.Mat
        cv.Extensions.Binarizer.Niblack(src, grayBin, kernelSize, sliders.trackbar(1).Value / 1000)
        dst1 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Extensions.Binarizer.Nick(src, grayBin, kernelSize, sliders.trackbar(2).Value / 1000)
        dst2 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Bernson
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar(1, "Contrast min", 0, 255, 50)
        sliders.setupTrackBar(2, "bg Threshold", 0, 255, 100)

        label1 = "Binarize Bernson (Draw Enabled)"

        ocvb.drawRect = New cv.Rect(100, 100, 100, 100)
        ocvb.desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.trackbar(0).Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayBin = gray.Clone()
        If ocvb.drawRect = New cv.Rect() Then
            cv.Extensions.Binarizer.Bernsen(gray, grayBin, kernelSize, sliders.trackbar(1).Value, sliders.trackbar(2).Value)
        Else
            cv.Extensions.Binarizer.Bernsen(gray(ocvb.drawRect), grayBin(ocvb.drawRect), kernelSize, sliders.trackbar(1).Value, sliders.trackbar(2).Value)
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
        grid.sliders.trackbar(0).Value = 32
        grid.sliders.trackbar(1).Value = 32

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar(1, "Contrast min", 0, 255, 50)
        sliders.setupTrackBar(2, "bg Threshold", 0, 255, 100)

        ocvb.desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
        label1 = "Binarize Bernson"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.trackbar(0).Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        grid.Run(ocvb)
        Dim contrastMin = sliders.trackbar(1).Value
        Dim bgThreshold = sliders.trackbar(2).Value

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
            Sub(roi)
                Dim grayBin = src(roi).Clone()
                cv.Extensions.Binarizer.Bernsen(src(roi), grayBin, kernelSize, contrastMin, bgThreshold)
                dst1(roi) = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            End Sub)
    End Sub
End Class

