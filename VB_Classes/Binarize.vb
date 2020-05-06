Imports cv = OpenCvSharp
Imports OpenCvSharp.XImgProc

'https://docs.opencv.org/3.4/d7/d4d/tutorial_py_thresholding.html
Public Class Binarize_OTSU
    Inherits VB_Class
    Dim mats1 As Mat_4to1
    Dim mats2 As Mat_4to1
    Dim plotHist As Plot_Histogram
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        plotHist = New Plot_Histogram(ocvb, callerName)
        plotHist.externalUse = True

        mats1 = New Mat_4to1(ocvb, callerName)
        mats1.externalUse = True
        mats2 = New Mat_4to1(ocvb, callerName)
        mats2.externalUse = True

        ocvb.desc = "Binarize an image using Threshold with OTSU."
        ocvb.label2 = "Histograms correspond to images on the left"
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
            mats1.mat(i) = gray.Threshold(meanScalar(0), 255, thresholdType).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Next

        mats1.Run(ocvb)
        ocvb.result1 = ocvb.result2.Clone() ' mat_4to1 puts output in result2
        mats2.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        mats1.Dispose()
        mats2.Dispose()
        plotHist.Dispose()
    End Sub
End Class




Public Class Binarize_Niblack_Sauvola
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        sliders.Label1.Text = "Kernel Size"
        sliders.setupTrackBar1(ocvb, callerName, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar2(ocvb, callerName, "Niblack k", -1000, 1000, -200)
        sliders.setupTrackBar3(ocvb, callerName, "Sauvola k", -1000, 1000, 100)
        sliders.setupTrackBar4(ocvb, callerName, "Sauvola r", 1, 100, 64)

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
End Class




Public Class Binarize_Niblack_Nick
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        sliders.Label1.Text = "Kernel Size"
        sliders.setupTrackBar1(ocvb, callerName, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar2(ocvb, callerName, "Niblack k", -1000, 1000, -200)
        sliders.setupTrackBar3(ocvb, callerName, "Nick k", -1000, 1000, 100)

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
End Class




Public Class Binarize_Bernson
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        sliders.Label1.Text = "Kernel Size"
        sliders.setupTrackBar1(ocvb, callerName, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar2(ocvb, callerName, "Contrast min", 0, 255, 50)
        sliders.setupTrackBar3(ocvb, callerName, "bg Threshold", 0, 255, 100)

        ocvb.label1 = "Binarize Bernson (Draw Enabled)"

        ' ocvb.drawRect = New cv.Rect(100, 100, 100, 100)
        ocvb.desc = "Binarize an image using Bernson.  Draw on image (because Bernson is so slow)."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1

        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim grayBin = gray.Clone()
        If ocvb.drawRect = New cv.Rect() Then
            cv.Extensions.Binarizer.Bernsen(gray, grayBin, kernelSize, sliders.TrackBar2.Value, sliders.TrackBar3.Value)
        Else
            cv.Extensions.Binarizer.Bernsen(gray(ocvb.drawRect), grayBin(ocvb.drawRect), kernelSize, sliders.TrackBar2.Value, sliders.TrackBar3.Value)
        End If
        ocvb.result1 = grayBin.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




Public Class Binarize_Bernson_MT
    Inherits VB_Class
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        grid = New Thread_Grid(ocvb, callerName)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32

        sliders.Label1.Text = "Kernel Size"
        sliders.setupTrackBar1(ocvb, callerName, "Kernel Size", 3, 500, 51)
        sliders.setupTrackBar2(ocvb, callerName, "Contrast min", 0, 255, 50)
        sliders.setupTrackBar3(ocvb, callerName,"bg Threshold", 0, 255, 100)

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
    Public Sub MyDispose()
        grid.Dispose()
    End Sub
End Class
