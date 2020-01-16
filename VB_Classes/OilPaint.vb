Imports cv = OpenCvSharp
Imports System.Windows.Forms

' Source: https://hackernoon.com/https-medium-com-matteoronchetti-pointillism-with-python-and-opencv-f4274e6bbb7b
Public Class OilPaint_Pointilism : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim randomMask As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Stroke Scale", 1, 5, 3)
        sliders.setupTrackBar2(ocvb, "Smoothing Radius", 0, 100, 32)
        If ocvb.parms.ShowOptions Then
            sliders.Show()
            Application.DoEvents() ' because the rest of initialization takes so long, let the show take effect.
        End If

        Dim w = ocvb.color.Width / 8
        Dim h = ocvb.color.Height / 8
        ocvb.drawRect = New cv.Rect(w * 3, h * 3, w * 2, h * 2)

        ocvb.desc = "Alter the image to effect the pointilism style - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = ocvb.color.Clone()
        Dim src = ocvb.result1(ocvb.drawRect)
        Static saveDrawRect As New cv.Rect
        If saveDrawRect <> ocvb.drawRect Then
            saveDrawRect = ocvb.drawRect
            ' only need to create the mask to order the brush strokes once.
            randomMask = New cv.Mat(src.Size(), cv.MatType.CV_32SC2)
            Dim nPt As New cv.Point
            For y = 0 To randomMask.Height - 1
                For x = 0 To randomMask.Width - 1
                    nPt.X = (ocvb.rng.uniform(-1, 1) + x) Mod (randomMask.Width - 1)
                    nPt.Y = (ocvb.rng.uniform(-1, 1) + y) Mod (randomMask.Height - 1)
                    If nPt.X < 0 Then nPt.X = 0
                    If nPt.Y < 0 Then nPt.Y = 0
                    randomMask.Set(Of cv.Point)(y, x, nPt)
                Next
            Next
            Dim myRNG As New cv.RNG
            cv.Cv2.RandShuffle(randomMask, 1.0, myRNG) ' the RNG is not optional.
        End If
        Dim rand = randomMask.Resize(src.Size())
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim fieldx As New cv.Mat, fieldy As New cv.Mat
        cv.Cv2.Scharr(gray, fieldx, cv.MatType.CV_32FC1, 1, 0, 1 / 15.36)
        cv.Cv2.Scharr(gray, fieldy, cv.MatType.CV_32FC1, 0, 1, 1 / 15.36)

        Dim smoothingRadius = sliders.TrackBar2.Value * 2 + 1
        cv.Cv2.GaussianBlur(fieldx, fieldx, New cv.Size(smoothingRadius, smoothingRadius), 0, 0)
        cv.Cv2.GaussianBlur(fieldy, fieldy, New cv.Size(smoothingRadius, smoothingRadius), 0, 0)

        Dim strokeSize = sliders.TrackBar1.Value
        src.SetTo(0)
        For y = 0 To src.Height - 1
            For x = 0 To src.Width - 1
                Dim nPt = rand.At(Of cv.Point)(y, x)
                Dim fx = fieldx(ocvb.drawRect).At(Of Single)(nPt.Y, nPt.X)
                Dim fy = fieldy(ocvb.drawRect).At(Of Single)(nPt.Y, nPt.X)
                Dim nPoint = New cv.Point2f(nPt.X, nPt.Y)
                Dim gradient_magnitude = Math.Sqrt(fx * fx + fy * fy)
                Dim slen = Math.Round(strokeSize + strokeSize * Math.Sqrt(gradient_magnitude))
                Dim eSize = New cv.Size2f(slen, strokeSize)
                Dim direction = Math.Atan2(fx, fy)
                Dim angle = direction * 180.0 / Math.PI + 90

                Dim nextColor = ocvb.color(ocvb.drawRect).At(Of cv.Vec3b)(nPt.Y, nPt.X)
                ocvb.result1(ocvb.drawRect).Circle(nPoint, slen / 4, nextColor, -1, cv.LineTypes.AntiAlias)
            Next
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class





Public Class OilPaint_ColorProbability : Implements IDisposable
    Public color_probability() As Single
    Public km As kMeans_RGBFast
    Public Sub New(ocvb As AlgorithmData)
        km = New kMeans_RGBFast(ocvb)
        km.sliders.TrackBar1.Value = 12 ' we would like a dozen colors or so in the color image.
        ReDim color_probability(km.sliders.TrackBar1.Value - 1)
        ocvb.desc = "Determine color probabilities on the output of kMeans - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        km.Run(ocvb)
        Dim c() = km.clusterColors
        If c Is Nothing Then Exit Sub
        For y = 0 To ocvb.result2.Height - 1
            For x = 0 To ocvb.result2.Width - 1
                Dim pixel = ocvb.result2.Get(Of cv.Vec3b)(y, x)
                For i = 0 To c.Length - 1
                    If pixel = c(i) Then
                        color_probability(i) += 1
                        Exit For
                    End If
                Next
            Next
        Next

        For i = 0 To color_probability.Length - 1
            color_probability(i) /= ocvb.result2.Total
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        km.Dispose()
    End Sub
End Class




' https://code.msdn.microsoft.com/Image-Oil-Painting-and-b0977ea9
Public Class OilPaint_Manual : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Filter Size", 3, 15, 3)
        sliders.setupTrackBar2(ocvb, "Intensity", 5, 150, 25)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Alter an image so it appears more like an oil painting - Painterly Effect.  Select a region of interest."
        Dim w = ocvb.color.Width / 8
        Dim h = ocvb.color.Height / 8
        ocvb.drawRect = New cv.Rect(w * 3, h * 3, w * 2, h * 2)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim filtersize = sliders.TrackBar1.Value
        Dim levels = sliders.TrackBar2.Value

        If filtersize Mod 2 = 0 Then filtersize += 1 ' must be odd
        Dim roi = ocvb.drawRect
        ocvb.color.CopyTo(ocvb.result1)
        Dim color = ocvb.color(roi)
        Dim result1 = color.Clone()
        For y = filtersize To roi.Height - filtersize - 1
            For x = filtersize To roi.Width - filtersize - 1
                Dim intensitybins(levels) As Int32
                Dim bluebin(levels) As Int32
                Dim greenbin(levels) As Int32
                Dim redbin(levels) As Int32
                Dim maxIntensity As Int32 = 0
                Dim maxIndex As Int32 = 0
                Dim vec As cv.Vec3b = Nothing
                For yy = y - filtersize To y + filtersize - 1
                    For xx = x - filtersize To x + filtersize - 1
                        vec = color.Get(Of cv.Vec3b)(yy, xx)
                        Dim currentIntensity = Math.Round((CSng(vec(0)) + CSng(vec(1)) + CSng(vec(2))) * levels / (255 * 3))
                        intensitybins(currentIntensity) += 1
                        bluebin(currentIntensity) += vec(0)
                        greenbin(currentIntensity) += vec(1)
                        redbin(currentIntensity) += vec(0)

                        If intensitybins(currentIntensity) > maxIntensity Then
                            maxIndex = currentIntensity
                            maxIntensity = intensitybins(currentIntensity)
                        End If
                    Next
                Next

                vec(0) = If((bluebin(maxIndex) / maxIntensity) > 255, 255, bluebin(maxIndex) / maxIntensity)
                vec(1) = If((greenbin(maxIndex) / maxIntensity) > 255, 255, greenbin(maxIndex) / maxIntensity)
                vec(2) = If((redbin(maxIndex) / maxIntensity) > 255, 255, redbin(maxIndex) / maxIntensity)
                result1.Set(Of cv.Vec3b)(y, x, vec)
            Next
        Next
        result1.CopyTo(ocvb.result1(roi))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



' https://code.msdn.microsoft.com/Image-Oil-Painting-and-b0977ea9
Public Class OilPaint_Manual_CS : Implements IDisposable
    Dim oilPaint As New CS_Classes.OilPaintManual
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Kernel Size", 1, 10, 4)
        sliders.setupTrackBar2(ocvb, "Intensity", 0, 250, 20)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Alter an image so it appears painted by a pointilist - Painterly Effect.  Select a region of interest to paint."
        ocvb.label2 = "Selected area only"

        Dim w = ocvb.color.Width / 16
        Dim h = ocvb.color.Height / 16
        ocvb.drawRect = New cv.Rect(w, h, w * 2, h * 2)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1
        Dim roi = ocvb.drawRect
        ocvb.color.CopyTo(ocvb.result1)
        oilPaint.Start(ocvb.color(roi), ocvb.result1(roi), kernelSize, sliders.TrackBar2.Value)
        ocvb.result2.SetTo(0)
        Dim factor As Int32 = Math.Min(Math.Floor(ocvb.result2.Width / roi.Width), Math.Floor(ocvb.result2.Height / roi.Height))
        Dim s = New cv.Size(roi.Width * factor, roi.Height * factor)
        cv.Cv2.Resize(ocvb.result1(roi), ocvb.result2(New cv.Rect(0, 0, s.Width, s.Height)), s)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




' https://code.msdn.microsoft.com/Image-Oil-Painting-and-b0977ea9
Public Class OilPaint_Cartoon : Implements IDisposable
    Dim oil As OilPaint_Manual_CS
    Dim laplacian As Edges_Laplacian
    Public Sub New(ocvb As AlgorithmData)
        laplacian = New Edges_Laplacian(ocvb)

        oil = New OilPaint_Manual_CS(ocvb)
        Dim w = ocvb.color.Width / 16
        Dim h = ocvb.color.Height / 16
        ocvb.drawRect = New cv.Rect(ocvb.color.Width / 4 + w, ocvb.color.Height / 4 + h, w * 2, h * 2)

        oil.sliders.setupTrackBar3(ocvb, "Threshold", 0, 200, 25) ' add the third slider for the threshold.
        ocvb.desc = "Alter an image so it appears more like a cartoon - Painterly Effect"
        ocvb.label1 = "OilPaint_Cartoon"
        ocvb.label2 = "Laplacian Edges"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim roi = ocvb.drawRect
        laplacian.Run(ocvb)
        Dim edges = ocvb.result1.CvtColor(cv.ColorConversionCodes.bgr2gray)

        oil.Run(ocvb)

        ocvb.result2 = edges.CvtColor(cv.ColorConversionCodes.gray2bgr)

        Dim threshold = oil.sliders.TrackBar3.Value
        Dim vec000 = New cv.Vec3b(0, 0, 0)
        For y = 0 To roi.Height - 1
            For x = 0 To roi.Width - 1
                If edges(roi).At(Of Byte)(y, x) >= threshold Then
                    ocvb.result1(roi).Set(Of cv.Vec3b)(y, x, vec000)
                End If
            Next
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        laplacian.Dispose()
        oil.Dispose()
    End Sub
End Class