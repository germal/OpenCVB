Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module dft_Module
    Public Function inverseDFT(complexImage As cv.Mat) As cv.Mat
        Dim invDFT As New cv.Mat
        cv.Cv2.Dft(complexImage, invDFT, cv.DftFlags.Inverse Or cv.DftFlags.RealOutput)
        invDFT = invDFT.Normalize(0, 255, cv.NormTypes.MinMax)
        Dim inverse8u As New cv.Mat
        invDFT.ConvertTo(inverse8u, cv.MatType.CV_8U)
        Return inverse8u
    End Function
End Module




' http://stackoverflow.com/questions/19761526/how-to-do-inverse-dft-in-opencv
Public Class DFT_Basics
    Inherits ocvbClass
    Dim mats As Mat_4to1
    Public magnitude As New cv.Mat
    Public spectrum As New cv.Mat
    Public complexImage As New cv.Mat
    Public gray As cv.Mat
    Public rows As Int32
    Public cols As Int32
        Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        mats = New Mat_4to1(ocvb, caller)
        mats.noLines = True

        ocvb.desc = "Explore the Discrete Fourier Transform."
        ocvb.label1 = "Image after inverse DFT"
        ocvb.label2 = "DFT_Basics Spectrum Magnitude"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        if standalone Then gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        rows = cv.Cv2.GetOptimalDFTSize(gray.Rows)
        cols = cv.Cv2.GetOptimalDFTSize(gray.Cols)
        Dim padded = New cv.Mat(gray.Width, gray.Height, cv.MatType.CV_8UC3)
        cv.Cv2.CopyMakeBorder(gray, padded, 0, rows - gray.Rows, 0, cols - gray.Cols, cv.BorderTypes.Constant, cv.Scalar.All(0))
        Dim padded32 As New cv.Mat
        padded.ConvertTo(padded32, cv.MatType.CV_32F)
        Dim planes() = {padded32, New cv.Mat(padded.Size(), cv.MatType.CV_32F, 0)}
        cv.Cv2.Merge(planes, complexImage)
        cv.Cv2.Dft(complexImage, complexImage)

        if standalone Then
            ' compute the magnitude And switch to logarithmic scale => log(1 + sqrt(Re(DFT(I))^2 + Im(DFT(I))^2))
            cv.Cv2.Split(complexImage, planes)

            cv.Cv2.Magnitude(planes(0), planes(1), magnitude)
            magnitude += cv.Scalar.All(1) ' switch To logarithmic scale
            cv.Cv2.Log(magnitude, magnitude)

            ' crop the spectrum, if it has an odd number of rows Or columns
            spectrum = magnitude(New cv.Rect(0, 0, magnitude.Cols And -2, magnitude.Rows And -2))
            ' Transform the matrix with float values into range 0-255
            spectrum = spectrum.Normalize(0, 255, cv.NormTypes.MinMax)
            spectrum.ConvertTo(padded, cv.MatType.CV_8U)

            ' rearrange the quadrants of Fourier image  so that the origin is at the image center
            Dim cx = CInt(padded.Cols / 2)
            Dim cy = CInt(padded.Rows / 2)

            mats.mat(3) = padded(New cv.Rect(0, 0, cx, cy)).Clone()
            mats.mat(2) = padded(New cv.Rect(cx, 0, cx, cy)).Clone()
            mats.mat(1) = padded(New cv.Rect(0, cy, cx, cy)).Clone()
            mats.mat(0) = padded(New cv.Rect(cx, cy, cx, cy)).Clone()
            mats.Run(ocvb)

            ocvb.result1 = inverseDFT(complexImage)
        End If
    End Sub
    Public Sub MyDispose()
        mats.Dispose()
    End Sub
End Class





' http://opencvexamples.blogspot.com/
Public Class DFT_Inverse
    Inherits ocvbClass
    Dim mats As Mat_2to1
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        mats = New Mat_2to1(ocvb, caller)
        ocvb.desc = "Take the inverse of the Discrete Fourier Transform."
        ocvb.label1 = "Image after Inverse DFT"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim gray32f As New cv.Mat
        gray.ConvertTo(gray32f, cv.MatType.CV_32F)
        Dim planes() = {gray32f, New cv.Mat(gray32f.Size(), cv.MatType.CV_32F, 0)}
        Dim complex As New cv.Mat, complexImage As New cv.Mat
        cv.Cv2.Merge(planes, complex)
        cv.Cv2.Dft(complex, complexImage)

        ocvb.result1 = inverseDFT(complexImage)

        Dim diff As New cv.Mat
        cv.Cv2.Absdiff(gray, ocvb.result1, diff)
        mats.mat(0) = diff.Threshold(1, 255, cv.ThresholdTypes.Binary)
        mats.mat(1) = (diff * 50).ToMat
        mats.Run(ocvb)
        If mats.mat(0).countnonzero() > 0 Then
            ocvb.result2 = mats.dst
            ocvb.label2 = "Mask of difference (top) and relative diff (bot)"
        Else
            ocvb.label2 = "InverseDFT reproduced original"
            ocvb.result2.SetTo(0)
        End If
    End Sub
    Public Sub MyDispose()
        mats.Dispose()
    End Sub
End Class





' http://breckon.eu/toby/teaching/dip/opencv/lecture_demos/c++/butterworth_lowpass.cpp
' https://github.com/ruohoruotsi/Butterworth-Filter-Design
Public Class DFT_ButterworthFilter
    Inherits ocvbClass
    Public dft As DFT_Basics
        Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "DFT B Filter - Radius", 1, ocvb.color.Height, ocvb.color.Height)
        sliders.setupTrackBar2(ocvb, caller, "DFT B Filter - Order", 1, ocvb.color.Height, 2)
                dft = New DFT_Basics(ocvb, caller)
        ocvb.desc = "Use the Butterworth filter on a DFT image - color image input."
        ocvb.label1 = "Image with Butterworth Low Pass Filter Applied"
        ocvb.label2 = "Same filter with radius / 2"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dft.Run(ocvb)

        Static radius As Int32
        Static order As Int32
        Static butterworthFilter(1) As cv.Mat
        ' only create the filter if radius or order has changed.
        If radius <> sliders.TrackBar1.Value Or order <> sliders.TrackBar2.Value Then
            radius = sliders.TrackBar1.Value
            order = sliders.TrackBar2.Value

            Parallel.For(0, 2,
            Sub(k)
                Dim r = radius / (k + 1), rNext As Double
                butterworthFilter(k) = New cv.Mat(dft.complexImage.Size, cv.MatType.CV_32FC2)
                Dim tmp As New cv.Mat(butterworthFilter(k).Size(), cv.MatType.CV_32F, 0)
                Dim center As New cv.Point(butterworthFilter(k).Rows / 2, butterworthFilter(k).Cols / 2)
                For i = 0 To butterworthFilter(k).Rows - 1
                    For j = 0 To butterworthFilter(k).Cols - 1
                        rNext = Math.Sqrt(Math.Pow(i - center.X, 2) + Math.Pow(j - center.Y, 2))
                        tmp.Set(Of Single)(i, j, 1 / (1 + Math.Pow(rNext / r, 2 * order)))
                    Next
                Next
                Dim tmpMerge() = {tmp, tmp}
                cv.Cv2.Merge(tmpMerge, butterworthFilter(k))
            End Sub)
        End If
        Parallel.For(0, 2,
       Sub(k)
           Dim complex As New cv.Mat
           cv.Cv2.MulSpectrums(butterworthFilter(k), dft.complexImage, complex, cv.DftFlags.None)
           If k = 0 Then ocvb.result1 = inverseDFT(complex) Else ocvb.result2 = inverseDFT(complex)
       End Sub)
    End Sub
    Public Sub MyDispose()
                dft.Dispose()
    End Sub
End Class






' http://breckon.eu/toby/teaching/dip/opencv/lecture_demos/c++/butterworth_lowpass.cpp
' https://github.com/ruohoruotsi/Butterworth-Filter-Design
Public Class DFT_ButterworthDepth
    Inherits ocvbClass
    Dim bfilter As DFT_ButterworthFilter
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        bfilter = New DFT_ButterworthFilter(ocvb, caller)

        ocvb.desc = "Use the Butterworth filter on a DFT image - RGBDepth as input."
        ocvb.label1 = "Image with Butterworth Low Pass Filter Applied"
        ocvb.label2 = "Same filter with radius / 2"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        bfilter.dft.gray = ocvb.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        bfilter.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        bfilter.Dispose()
    End Sub
End Class