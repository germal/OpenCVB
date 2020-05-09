Imports cv = OpenCvSharp
Public Class MotionBlur_Basics
    Inherits ocvbClass
    Public kernel As cv.Mat
    Public showDirection As Boolean = True
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Motion Blur Length", 1, 101, 51)
        sliders.setupTrackBar2(ocvb, caller, "Motion Blur Angle", -90, 90, 0)
        ocvb.desc = "Use Filter2D to create a motion blur"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        if standalone Then
            If sliders.TrackBar2.Value < sliders.TrackBar2.Maximum Then
                sliders.TrackBar2.Value += 1
            Else
                sliders.TrackBar2.Value = sliders.TrackBar2.Minimum
            End If
        End If
        Dim kernelSize = sliders.TrackBar1.Value
        kernel = New cv.Mat(kernelSize, kernelSize, cv.MatType.CV_32F, 0)
        Dim theta = sliders.TrackBar2.Value / (180 / Math.PI)
        Dim pt1 = New cv.Point(0, (kernelSize - 1) / 2)
        Dim pt2 = New cv.Point(kernelSize * Math.Cos(theta) + pt1.X, kernelSize * Math.Sin(theta) + pt1.Y)
        kernel.Line(pt1, pt2, New cv.Scalar(1 / kernelSize))
        ocvb.result1 = ocvb.color.Filter2D(-1, kernel)
        pt1 += New cv.Point(ocvb.color.Width / 2, ocvb.color.Height / 2)
        pt2 += New cv.Point(ocvb.color.Width / 2, ocvb.color.Height / 2)
        If showDirection Then ocvb.result1.Line(pt1, pt2, cv.Scalar.Yellow, 5, cv.LineTypes.AntiAlias)
		MyBase.Finish(ocvb)
    End Sub
End Class




' https://docs.opencv.org/trunk/d1/dfd/tutorial_motion_deblur_filter.html
Public Class MotionBlur_Deblur
    Inherits ocvbClass
    Dim mblur As MotionBlur_Basics
    Private Sub ResetBlurredImage(ocvb As AlgorithmData)
        mblur.sliders.TrackBar1.Value = ocvb.ms_rng.Next(mblur.sliders.TrackBar1.Minimum, mblur.sliders.TrackBar1.Maximum)
        mblur.sliders.TrackBar2.Value = ocvb.ms_rng.Next(mblur.sliders.TrackBar2.Minimum, mblur.sliders.TrackBar2.Maximum)
        mblur.Run(ocvb)
        mblur.showDirection = False
    End Sub
    Private Function calcPSF(filterSize As cv.Size, len As Int32, theta As Double) As cv.Mat
        Dim h As New cv.Mat(filterSize, cv.MatType.CV_32F, 0)
        Dim pt = New cv.Point(filterSize.Width / 2, filterSize.Height / 2)
        h.Ellipse(pt, New cv.Size(0, CInt(len / 2)), 90 - theta, 0, 360, New cv.Scalar(255), -1)
        Dim summa = h.Sum()
        Return h / summa(0)
    End Function
    Private Function calcWeinerFilter(input_h_PSF As cv.Mat, nsr As Double) As cv.Mat
        Dim h_PSF_shifted = fftShift(input_h_PSF)
        Dim planes() = {h_PSF_shifted.Clone(), New cv.Mat(h_PSF_shifted.Size(), cv.MatType.CV_32F, 0)}
        Dim complexI As New cv.Mat
        cv.Cv2.Merge(planes, complexI)
        cv.Cv2.Dft(complexI, complexI)
        planes = complexI.Split()
        Dim denom As New cv.Mat
        cv.Cv2.Pow(cv.Cv2.Abs(planes(0)), 2, denom)
        denom += nsr
        Dim output_G As New cv.Mat
        cv.Cv2.Divide(planes(0), denom, output_G)
        Return output_G
    End Function
    Private Function fftShift(inputImg As cv.Mat) As cv.Mat
        Dim outputImg = inputImg.Clone()
        Dim cx = outputImg.Width / 2
        Dim cy = outputImg.Height / 2
        Dim q0 = outputImg(New cv.Rect(0, 0, cx, cy))
        Dim q1 = outputImg(New cv.Rect(cx, 0, cx, cy))
        Dim q2 = outputImg(New cv.Rect(0, cy, cx, cy))
        Dim q3 = outputImg(New cv.Rect(cx, cy, cx, cy))
        Dim tmp = q0.Clone()
        q3.CopyTo(q0)
        tmp.CopyTo(q3)
        q1.CopyTo(tmp)
        q2.CopyTo(q1)
        tmp.CopyTo(q2)
        Return outputImg
    End Function
    Private Function edgeTaper(inputImg As cv.Mat, gamma As Double, beta As Double) As cv.Mat
        Dim nx = inputImg.Width
        Dim ny = inputImg.Height
        Dim w1 As New cv.Mat(1, nx, cv.MatType.CV_32F, 0)
        Dim w2 As New cv.Mat(ny, 1, cv.MatType.CV_32F, 0)

        Dim dx = CSng(2.0 * Math.PI / nx)
        Dim x = CSng(-Math.PI)
        For i = 0 To nx - 1
            w1.Set(Of Single)(0, i, 0.5 * (Math.Tanh((x + gamma / 2) / beta) - Math.Tanh((x - gamma / 2) / beta)))
            x += dx
        Next

        Dim dy = CSng(2.0 * Math.PI / ny)
        Dim y = CSng(-Math.PI)
        For i = 0 To ny - 1
            w2.Set(Of Single)(i, 0, 0.5 * (Math.Tanh((y + gamma / 2) / beta) - Math.Tanh((y - gamma / 2) / beta)))
            y += dy
        Next
        Dim w = w2 * w1
        Dim outputImg As New cv.Mat
        cv.Cv2.Multiply(inputImg, w, outputImg)
        Return outputImg
    End Function
    Private Function filter2DFreq(inputImg As cv.Mat, H As cv.Mat) As cv.Mat
        Dim planes() = {inputImg.Clone(), New cv.Mat(inputImg.Size(), cv.MatType.CV_32F, 0)}
        Dim complexI As New cv.Mat
        cv.Cv2.Merge(planes, complexI)
        cv.Cv2.Dft(complexI, complexI, cv.DftFlags.Scale)
        Dim planesH() = {H.Clone(), New cv.Mat(H.Size(), cv.MatType.CV_32F, 0)}
        Dim complexH As New cv.Mat
        cv.Cv2.Merge(planesH, complexH)
        Dim complexIH As New cv.Mat
        cv.Cv2.MulSpectrums(complexI, complexH, complexIH, 0)

        cv.Cv2.Idft(complexIH, complexIH)
        planes = complexIH.Split()
        Return planes(0)
    End Function
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        check.Setup(ocvb, caller,  1)
        check.Box(0).Text = "Redo motion blurred image"

        mblur = New MotionBlur_Basics(ocvb, caller)
        ResetBlurredImage(ocvb)

        sliders.setupTrackBar1(ocvb, caller, "Deblur Restore Vector", 1, mblur.sliders.TrackBar1.Maximum, 10)
        sliders.setupTrackBar2(ocvb, caller, "Deblur Angle of Restore Vector", mblur.sliders.TrackBar2.Minimum, mblur.sliders.TrackBar2.Maximum, 0)
        sliders.setupTrackBar3(ocvb, caller,"Deblur Signal to Noise Ratio", 1, 1000, 700)
        sliders.setupTrackBar4(ocvb, caller,  "Deblur Gamma", 1, 100, 5)

        ocvb.desc = "Deblur a motion blurred image"
        ocvb.label1 = "Blurred Image Input"
        ocvb.label2 = "Deblurred Image Output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If check.Box(0).Checked Then
            check.Box(0).Checked = False
            ResetBlurredImage(ocvb)
        End If
        mblur.Run(ocvb) ' the motion blurred image is in result1

        Dim len = sliders.TrackBar1.Value
        Dim theta = sliders.TrackBar2.Value / (180 / Math.PI)
        Dim SNR = CDbl(sliders.TrackBar3.Value)
        Dim gamma = CDbl(sliders.TrackBar4.Value)
        Dim beta = 0.2

        Dim width = ocvb.color.Width
        Dim height = ocvb.color.Height
        Dim roi = New cv.Rect(0, 0, If(width Mod 2, width - 1, width), If(height Mod 2, height - 1, height))

        Dim h = calcPSF(roi.Size(), len, theta)
        Dim hW = calcWeinerFilter(h, 1.0 / SNR)

        Dim gray8u = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim imgIn As New cv.Mat
        gray8u.ConvertTo(imgIn, cv.MatType.CV_32F)
        imgIn = edgeTaper(imgIn, gamma, beta)

        Dim imgOut = filter2DFreq(imgIn(roi), hW)
        imgOut.ConvertTo(ocvb.result2, cv.MatType.CV_8U)
        ocvb.result2.Normalize(0, 255, cv.NormTypes.MinMax)
		MyBase.Finish(ocvb)
    End Sub
    Public Sub MyDispose()
                        mblur.Dispose()
    End Sub
End Class
