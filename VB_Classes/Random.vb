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
        sliders.setupTrackBar1(ocvb, caller, "Random Pixel Count", 1, ocvb.color.cols * ocvb.color.Rows, 20)

        ReDim Points(sliders.TrackBar1.Value - 1)
        ReDim Points2f(sliders.TrackBar1.Value - 1)

        rangeRect = New cv.Rect(0, 0, ocvb.color.cols, ocvb.color.Rows)
        ocvb.desc = "Create a uniform random mask with a specificied number of pixels."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If Points.Length <> sliders.TrackBar1.Value Then
            ReDim Points(sliders.TrackBar1.Value - 1)
            ReDim Points2f(sliders.TrackBar1.Value - 1)
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
        ocvb.desc = "Use randomShuffle to reorder an image."
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
        ocvb.desc = "Use a random Look-Up-Table to modify few colors in a kmeans image."
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
        ocvb.desc = "Create a uniform distribution."
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
        sliders.setupTrackBar1(ocvb, caller, "Random_NormalDist Blue Mean", 0, 255, 25)
        sliders.setupTrackBar2("Random_NormalDist Green Mean", 0, 255, 127)
        sliders.setupTrackBar3("Random_NormalDist Red Mean", 0, 255, 180)
        sliders.setupTrackBar4("Random_NormalDist Stdev", 0, 255, 50)
        ocvb.desc = "Create a normal distribution in all 3 colors with a variable standard deviation."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        cv.Cv2.Randn(dst1, New cv.Scalar(sliders.TrackBar1.Value, sliders.TrackBar2.Value, sliders.TrackBar3.Value), cv.Scalar.All(sliders.TrackBar4.Value))
    End Sub
End Class



Public Class Random_CheckUniformSmoothed
    Inherits ocvbClass
    Dim histogram As Histogram_KalmanSmoothed
    Dim rUniform As Random_UniformDist
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        histogram = New Histogram_KalmanSmoothed(ocvb)
        histogram.sliders.TrackBar1.Value = 255

        rUniform = New Random_UniformDist(ocvb)

        ocvb.desc = "Display the smoothed histogram for a uniform distribution."
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
        histogram.sliders.TrackBar1.Value = 255

        rUniform = New Random_UniformDist(ocvb)

        ocvb.desc = "Display the histogram for a uniform distribution."
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
        histogram.sliders.TrackBar1.Value = 255
        normalDist = New Random_NormalDist(ocvb)
        ocvb.desc = "Display the histogram for a Normal distribution."
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
        histogram.sliders.TrackBar1.Value = 255
        histogram.plotHist.minRange = 1
        normalDist = New Random_NormalDist(ocvb)
        ocvb.desc = "Display the histogram for a Normal distribution."
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
        ocvb.desc = "Generate random patterns for use with 'Random Pattern Calibration'"
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
    Dim cdf As cv.Mat
    Dim histogram As cv.Mat
    Public plotHist As Plot_Histogram
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        Dim loadedDice() As Single = {1, 3, 0.5, 0.5, 0.5, 0.5}
        cdf = New cv.Mat(loadedDice.Length, 1, cv.MatType.CV_32F, loadedDice)
        cdf *= 1 / 6

        plotHist = New Plot_Histogram(ocvb)

        ocvb.desc = "Create a custom random number distribution from any histogram"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If cdf.Get(Of Single)(cdf.Rows - 1, 0) < 0.9 Then ' convert the input histogram to a cdf.
            For i = 1 To cdf.Rows - 1
                cdf.Set(Of Single)(i, 0, cdf.Get(Of Single)(i - 1, 0) + cdf.Get(Of Single)(i, 0))
            Next
        End If
        histogram = New cv.Mat(cdf.Size(), cv.MatType.CV_32F, 0)
        Dim size = histogram.Rows
        For i = 0 To 1000
            Dim uniformR1 = msRNG.NextDouble()
            For j = 1 To size - 1S
                If uniformR1 > cdf.Get(Of Single)(j, 0) Then
                    histogram.Set(Of Single)(j - 1, 0, histogram.Get(Of Single)(j - 1, 0) + 1)
                End If
            Next
        Next

        plotHist.hist = histogram
        plotHist.Run(ocvb)
        dst1 = plotHist.dst1
    End Sub
End Class






' https://www.khanacademy.org/computing/computer-programming/programming-natural-simulations/programming-randomness/a/custom-distribution-of-random-numbers
Public Class Random_MonteCarlo
    Inherits ocvbClass
    Public plotHist As Plot_Histogram
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        plotHist = New Plot_Histogram(ocvb)
        ocvb.desc = "Generate random numbers but prefer higher values"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim dimension = 100
        Dim histogram = New cv.Mat(dimension, 1, cv.MatType.CV_32F, 0)
        For i = 0 To 20000
            While (1)
                Dim r1 = msRNG.NextDouble()
                Dim r2 = msRNG.NextDouble()
                If r2 < r1 Then
                    Dim index = CInt(dimension * r1)
                    histogram.Set(Of Single)(index, 0, histogram.Get(Of Single)(index, 0) + 1)
                    Exit While
                End If
            End While
        Next

        plotHist.hist = histogram
        plotHist.Run(ocvb)
        dst1 = plotHist.dst1
    End Sub
End Class