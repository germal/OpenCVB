Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class Random_Points
    Inherits ocvbClass
    Public Points() As cv.Point
    Public Points2f() As cv.Point2f
    Public rangeRect As cv.Rect
    Public plotPoints As Boolean = False
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Random Pixel Count", 1, ocvb.color.Cols * ocvb.color.Rows, 20)

        ReDim Points(sliders.trackbar(0).Value - 1)
        ReDim Points2f(sliders.trackbar(0).Value - 1)

        rangeRect = New cv.Rect(0, 0, ocvb.color.cols, ocvb.color.Rows)
        setDescription(ocvb, "Create a uniform random mask with a specificied number of pixels.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If Points.Length <> sliders.trackbar(0).Value Then
            ReDim Points(sliders.trackbar(0).Value - 1)
            ReDim Points2f(sliders.trackbar(0).Value - 1)
        End If
        dst1.SetTo(0)
        For i = 0 To Points.Length - 1
            Dim x = msRNG.Next(rangeRect.X, rangeRect.X + rangeRect.Width)
            Dim y = msRNG.Next(rangeRect.Y, rangeRect.Y + rangeRect.Height)
            Points(i) = New cv.Point2f(x, y)
            Points2f(i) = New cv.Point2f(x, y)
            If standalone Or plotPoints = True Then cv.Cv2.Circle(dst1, Points(i), 3, cv.Scalar.Gray, -1, cv.LineTypes.AntiAlias, 0)
        Next
    End Sub
End Class




Public Class Random_Shuffle
    Inherits ocvbClass
    Dim myRNG As New cv.RNG
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        setDescription(ocvb, "Use randomShuffle to reorder an image.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.RGBDepth.CopyTo(dst1)
        cv.Cv2.RandShuffle(dst1, 1.0, myRNG) ' don't remove that myRNG!  It will fail in RandShuffle.
        label1 = "Random_shuffle - wave at camera"
    End Sub
End Class



Public Class Random_LUTMask
    Inherits ocvbClass
    Dim random As Random_Points
    Dim km As kMeans_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        km = New kMeans_Basics(ocvb)
        random = New Random_Points(ocvb)
        setDescription(ocvb, "Use a random Look-Up-Table to modify few colors in a kmeans image.")
        label2 = "kmeans run To Get colors"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static lutMat As cv.Mat
        If lutMat Is Nothing Or ocvb.frameCount Mod 10 = 0 Then
            random.Run(ocvb)
            lutMat = cv.Mat.Zeros(New cv.Size(1, 256), cv.MatType.CV_8UC3)
            Dim lutIndex = 0
            km.src = src
            km.Run(ocvb)
            dst1 = km.dst1
            For i = 0 To random.Points.Length - 1
                Dim x = random.Points(i).X
                Dim y = random.Points(i).Y
                lutMat.Set(lutIndex, 0, dst1.Get(Of cv.Vec3b)(y, x))
                lutIndex += 1
                If lutIndex >= lutMat.Rows Then Exit For
            Next
        End If
        dst2 = src.LUT(lutMat)
        label1 = "Using kmeans colors with interpolation"
    End Sub
End Class



Public Class Random_UniformDist
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        setDescription(ocvb, "Create a uniform distribution.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U)
        cv.Cv2.Randu(dst1, 0, 255)
    End Sub
End Class



Public Class Random_NormalDist
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Random_NormalDist Blue Mean", 0, 255, 25)
        sliders.setupTrackBar(1, "Random_NormalDist Green Mean", 0, 255, 127)
        sliders.setupTrackBar(2, "Random_NormalDist Red Mean", 0, 255, 180)
        sliders.setupTrackBar(3, "Random_NormalDist Stdev", 0, 255, 50)
        setDescription(ocvb, "Create a normal distribution in all 3 colors with a variable standard deviation.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        cv.Cv2.Randn(dst1, New cv.Scalar(sliders.trackbar(0).Value, sliders.trackbar(1).Value, sliders.trackbar(2).Value), cv.Scalar.All(sliders.trackbar(3).Value))
    End Sub
End Class



Public Class Random_CheckUniformSmoothed
    Inherits ocvbClass
    Dim histogram As Histogram_KalmanSmoothed
    Dim rUniform As Random_UniformDist
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        histogram = New Histogram_KalmanSmoothed(ocvb)
        histogram.sliders.trackbar(0).Value = 255

        rUniform = New Random_UniformDist(ocvb)

        setDescription(ocvb, "Display the smoothed histogram for a uniform distribution.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        rUniform.src = src
        rUniform.Run(ocvb)
        dst1 = rUniform.dst1
        histogram.src = dst1
        histogram.plotHist.maxRange = 255
        histogram.Run(ocvb)
        dst2 = histogram.dst1
    End Sub
End Class






Public Class Random_CheckUniformDist
    Inherits ocvbClass
    Dim histogram As Histogram_Basics
    Dim rUniform As Random_UniformDist
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        histogram = New Histogram_Basics(ocvb)
        histogram.sliders.trackbar(0).Value = 255

        rUniform = New Random_UniformDist(ocvb)

        setDescription(ocvb, "Display the histogram for a uniform distribution.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        rUniform.src = src
        rUniform.Run(ocvb)
        dst1 = rUniform.dst1
        histogram.src = dst1
        histogram.plotRequested = True
        histogram.Run(ocvb)
        dst2 = histogram.dst1
    End Sub
End Class






Public Class Random_CheckNormalDist
    Inherits ocvbClass
    Dim histogram As Histogram_Basics
    Dim normalDist As Random_NormalDist
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        histogram = New Histogram_Basics(ocvb)
        histogram.sliders.trackbar(0).Value = 255
        normalDist = New Random_NormalDist(ocvb)
        setDescription(ocvb, "Display the histogram for a Normal distribution.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        normalDist.src = src
        normalDist.Run(ocvb)
        dst1 = normalDist.dst1
        histogram.src = dst1
        histogram.plotRequested = True
        histogram.Run(ocvb)
        dst2 = histogram.dst1
    End Sub
End Class





Public Class Random_CheckNormalDistSmoothed
    Inherits ocvbClass
    Dim histogram As Histogram_KalmanSmoothed
    Dim normalDist As Random_NormalDist
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        histogram = New Histogram_KalmanSmoothed(ocvb)
        histogram.sliders.trackbar(0).Value = 255
        histogram.plotHist.minRange = 1
        normalDist = New Random_NormalDist(ocvb)
        setDescription(ocvb, "Display the histogram for a Normal distribution.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        normalDist.src = src
        normalDist.Run(ocvb)
        dst1 = normalDist.dst1
        histogram.src = dst1
        histogram.Run(ocvb)
        dst2 = histogram.dst1
    End Sub
End Class





Module Random_PatternGenerator_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_PatternGenerator_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Random_PatternGenerator_Close(Random_PatternGeneratorPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Random_PatternGenerator_Run(Random_PatternGeneratorPtr As IntPtr, rows As Int32, cols As Int32, channels As Int32) As IntPtr
    End Function
End Module




Public Class Random_PatternGenerator_CPP
    Inherits ocvbClass
    Dim Random_PatternGenerator As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        Random_PatternGenerator = Random_PatternGenerator_Open()
        setDescription(ocvb, "Generate random patterns for use with 'Random Pattern Calibration'")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim imagePtr = Random_PatternGenerator_Run(Random_PatternGenerator, src.Rows, src.Cols, src.Channels)

        If imagePtr <> 0 Then
            Dim dstData(src.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
        End If
    End Sub
    Public Sub Close()
        Random_PatternGenerator_Close(Random_PatternGenerator)
    End Sub
End Class








Public Class Random_CustomDistribution
    Inherits ocvbClass
    Public inputCDF As cv.Mat ' place a cumulative distribution function here (or just put the histogram that reflects the desired random number distribution)
    Public outputRandom = New cv.Mat(10000, 1, cv.MatType.CV_32S, 0) ' allocate the desired number of random numbers - size can be just one to get the next random value
    Public outputHistogram As cv.Mat
    Public plotHist As Plot_Histogram
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        Dim loadedDice() As Single = {1, 3, 0.5, 0.5, 0.75, 0.25}
        inputCDF = New cv.Mat(loadedDice.Length, 1, cv.MatType.CV_32F, loadedDice)

        If standalone Then plotHist = New Plot_Histogram(ocvb)

        setDescription(ocvb, "Create a custom random number distribution from any histogram")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim lastValue = inputCDF.Get(Of Single)(inputCDF.Rows - 1, 0)
        If Not (lastValue > 0.99 And lastValue <= 1.0) Then ' convert the input histogram to a cdf.
            inputCDF *= 1 / (inputCDF.Sum().Item(0))
            For i = 1 To inputCDF.Rows - 1
                inputCDF.Set(Of Single)(i, 0, inputCDF.Get(Of Single)(i - 1, 0) + inputCDF.Get(Of Single)(i, 0))
            Next
        End If
        outputHistogram = New cv.Mat(inputCDF.Size(), cv.MatType.CV_32F, 0)
        Dim size = outputHistogram.Rows
        For i = 0 To outputRandom.rows - 1
            Dim uniformR1 = msRNG.NextDouble()
            For j = 0 To size - 1
                If uniformR1 < inputCDF.Get(Of Single)(j, 0) Then
                    outputHistogram.Set(Of Single)(j, 0, outputHistogram.Get(Of Single)(j, 0) + 1)
                    outputRandom.set(Of Integer)(i, 0, j) ' the output is an integer reflecting a bin in the histogram.
                    Exit For
                End If
            Next
        Next

        If standalone Then
            plotHist.hist = outputHistogram
            plotHist.Run(ocvb)
            dst1 = plotHist.dst1
        End If
    End Sub
End Class






' https://www.khanacademy.org/computing/computer-programming/programming-natural-simulations/programming-randomness/a/custom-distribution-of-random-numbers
Public Class Random_MonteCarlo
    Inherits ocvbClass
    Public plotHist As Plot_Histogram
    Public outputRandom = New cv.Mat(4000, 1, cv.MatType.CV_32S, 0) ' allocate the desired number of random numbers - size can be just one to get the next random value
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        plotHist = New Plot_Histogram(ocvb)
        plotHist.fixedMaxVal = 100

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Number of bins", 1, 255, 91)
        setDescription(ocvb, "Generate random numbers but prefer higher values - a linearly increasing random distribution")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim dimension = sliders.trackbar(0).Value
        Dim histogram = New cv.Mat(dimension, 1, cv.MatType.CV_32F, 0)
        For i = 0 To outputRandom.rows - 1
            While (1)
                Dim r1 = msRNG.NextDouble()
                Dim r2 = msRNG.NextDouble()
                If r2 < r1 Then
                    Dim index = CInt(dimension * r1)
                    histogram.Set(Of Single)(index, 0, histogram.Get(Of Single)(index, 0) + 1)
                    outputRandom.set(Of Integer)(i, 0, index)
                    Exit While
                End If
            End While
        Next

        If standalone Then
            plotHist.hist = histogram
            plotHist.Run(ocvb)
            dst1 = plotHist.dst1
        End If
    End Sub
End Class






Public Class Random_CustomHistogram
    Inherits ocvbClass
    Public random As Random_CustomDistribution
    Public hist As Histogram_Simple
    Public saveHist As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        random = New Random_CustomDistribution(ocvb)
        random.outputRandom = New cv.Mat(1000, 1, cv.MatType.CV_32S, 0)

        hist = New Histogram_Simple(ocvb)
        hist.sliders.trackbar(0).Value = 255

        label1 = "Histogram of the grayscale image"
        label2 = "Histogram of the resulting random numbers"

        setDescription(ocvb, "Create a random number distribution that reflects histogram of a grayscale image")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static saveBins As Integer
        If hist.sliders.trackbar(0).Value <> saveBins Then
            saveBins = hist.sliders.trackbar(0).Value
            hist.src = src
            hist.plotHist.fixedMaxVal = 0 ' we are sharing the plothist with the code below...
            hist.Run(ocvb)
            dst1 = hist.dst1.Clone()
            saveHist = hist.plotHist.hist.Clone()
        End If

        random.inputCDF = saveHist ' it will convert the histogram into a cdf where the last value must be near one.
        random.Run(ocvb)

        If standalone Then
            hist.plotHist.fixedMaxVal = 100
            hist.plotHist.hist = random.outputHistogram
            hist.plotHist.Run(ocvb)
            dst2 = hist.plotHist.dst1
        End If
    End Sub
End Class