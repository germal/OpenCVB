Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class Random_Points
    Inherits ocvbClass
    Public Points() As cv.Point
    Public Points2f() As cv.Point2f
    Public externalUse As Boolean
    Public rangeRect As cv.Rect
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Random Pixel Count", 1, ocvb.color.Width * ocvb.color.Height, 20)

        ReDim Points(sliders.TrackBar1.Value - 1)
        ReDim Points2f(sliders.TrackBar1.Value - 1)

        rangeRect = New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height)
        ocvb.desc = "Create a uniform random mask with a specificied number of pixels."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If Points.Length <> sliders.TrackBar1.Value Then
            ReDim Points(sliders.TrackBar1.Value - 1)
            ReDim Points2f(sliders.TrackBar1.Value - 1)
        End If
        If externalUse = False Then ocvb.result1.SetTo(0)
        For i = 0 To Points.Length - 1
            Dim x = ocvb.ms_rng.Next(rangeRect.X, rangeRect.X + rangeRect.Width)
            Dim y = ocvb.ms_rng.Next(rangeRect.Y, rangeRect.Y + rangeRect.Height)
            Points(i) = New cv.Point2f(x, y)
            Points2f(i) = New cv.Point2f(x, y)
            If externalUse = False Then cv.Cv2.Circle(ocvb.result1, Points(i), 3, cv.Scalar.Gray, -1, cv.LineTypes.AntiAlias, 0)
        Next
    End Sub
End Class




Public Class Random_Shuffle
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Use randomShuffle to reorder an image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.RGBDepth.CopyTo(ocvb.result1)
        Dim myRNG As New cv.RNG
        cv.Cv2.RandShuffle(ocvb.result1, 1.0, myRNG) ' don't remove that myRNG!  It will fail in RandShuffle.
        ocvb.label1 = "Random_shuffle - wave at camera"
    End Sub
End Class



Public Class Random_LUTMask
    Inherits ocvbClass
    Dim random As Random_Points
    Dim km As kMeans_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        km = New kMeans_Basics(ocvb, caller)
        random = New Random_Points(ocvb, caller)
        ocvb.desc = "Use a random Look-Up-Table to modify few colors in a kmeans image.  Note how interpolation impacts results"
        ocvb.label2 = "kmeans run To Get colors"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static lutMat As cv.Mat
        If lutMat Is Nothing Or ocvb.frameCount Mod 10 = 0 Then
            random.Run(ocvb)
            lutMat = cv.Mat.Zeros(New cv.Size(1, 256), cv.MatType.CV_8UC3)
            Dim lutIndex = 0
            km.Run(ocvb) ' sets result1
            ocvb.result1.CopyTo(ocvb.result2)
            For i = 0 To random.Points.Length - 1
                Dim x = random.Points(i).X
                Dim y = random.Points(i).Y
                If x >= ocvb.drawRect.X And x < ocvb.drawRect.X + ocvb.drawRect.Width Then
                    If y >= ocvb.drawRect.Y And y < ocvb.drawRect.Y + ocvb.drawRect.Height Then
                        lutMat.Set(lutIndex, 0, ocvb.result2.Get(Of cv.Vec3b)(y, x))
                        lutIndex += 1
                        If lutIndex >= lutMat.Rows Then Exit For
                    End If
                End If
            Next
        End If
        ocvb.result2 = ocvb.color.LUT(lutMat)
        ocvb.label1 = "Using kmeans colors with interpolation"
    End Sub
    Public Sub MyDispose()
        km.Dispose()
        random.Dispose()
    End Sub
End Class



Public Class Random_UniformDist
    Inherits ocvbClass
    Public uDist As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        uDist = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
        ocvb.desc = "Create a uniform distribution."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        cv.Cv2.Randu(uDist, 0, 255)
        If externalUse = False Then
            ocvb.result1 = uDist.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If
    End Sub
End Class



Public Class Random_NormalDist
    Inherits ocvbClass
    Public nDistImage As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Random_NormalDist Blue Mean", 0, 255, 25)
        sliders.setupTrackBar2(ocvb, caller, "Random_NormalDist Green Mean", 0, 255, 127)
        sliders.setupTrackBar3(ocvb, caller, "Random_NormalDist Red Mean", 0, 255, 180)
        sliders.setupTrackBar4(ocvb, caller, "Random_NormalDist Stdev", 0, 255, 50)
        ocvb.desc = "Create a normal distribution."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        cv.Cv2.Randn(ocvb.result1, New cv.Scalar(sliders.TrackBar1.Value, sliders.TrackBar2.Value, sliders.TrackBar3.Value), cv.Scalar.All(sliders.TrackBar4.Value))
        If externalUse Then nDistImage = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class



Public Class Random_CheckUniformDist
    Inherits ocvbClass
    Dim histogram As Histogram_KalmanSmoothed
    Dim rUniform As Random_UniformDist
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        histogram = New Histogram_KalmanSmoothed(ocvb, caller)
        histogram.externalUse = True
        histogram.sliders.TrackBar1.Value = 255
        histogram.gray = New cv.Mat

        rUniform = New Random_UniformDist(ocvb, caller)
        rUniform.externalUse = True

        ocvb.desc = "Display the histogram for a uniform distribution."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        rUniform.Run(ocvb)
        ocvb.result1 = rUniform.uDist.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        rUniform.uDist.CopyTo(histogram.gray)
        histogram.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        rUniform.Dispose()
        histogram.Dispose()
    End Sub
End Class



Public Class Random_CheckNormalDist
    Inherits ocvbClass
    Dim histogram As Histogram_KalmanSmoothed
    Dim normalDist As Random_NormalDist
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        histogram = New Histogram_KalmanSmoothed(ocvb, caller)
        histogram.externalUse = True
        histogram.sliders.TrackBar1.Value = 255
        histogram.gray = New cv.Mat
        histogram.plotHist.minRange = 1
        normalDist = New Random_NormalDist(ocvb, caller)
        normalDist.externalUse = True
        ocvb.desc = "Display the histogram for a Normal distribution."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        normalDist.Run(ocvb)
        ocvb.result1 = normalDist.nDistImage.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        normalDist.nDistImage.CopyTo(histogram.gray)
        histogram.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        normalDist.Dispose()
        histogram.Dispose()
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        Random_PatternGenerator = Random_PatternGenerator_Open()
        ocvb.desc = "Generate random patterns for use with 'Random Pattern Calibration'"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim src = ocvb.color
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim imagePtr = Random_PatternGenerator_Run(Random_PatternGenerator, src.Rows, src.Cols, src.Channels)

        If imagePtr <> 0 Then
            Dim dstData(src.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            ocvb.result1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
        End If
    End Sub
    Public Sub MyDispose()
        Random_PatternGenerator_Close(Random_PatternGenerator)
    End Sub
End Class
