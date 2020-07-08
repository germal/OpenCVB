Imports cv = OpenCvSharp
Imports py = Python.Runtime
Imports Numpy
Public Structure DNAentry
    Dim color As Byte
    Dim pt As cv.Point
    Dim size As Single
    Dim rotation As Single
    Dim brushNumber As Integer
End Structure
Public Class GeneticDrawing_Basics
    Inherits ocvbClass
    Dim samplingMask As cv.Mat
    Dim gradient As Gradient_CartToPolar
    Dim minBrush = New cv.Vec2f(0.1, 0.3)
    Dim maxBrush = New cv.Vec2f(0.3, 0.7)
    Dim minSize As Single
    Dim maxSize As Single
    Dim maxBrushNumber = 4
    Dim brushes(4 - 1) As cv.Mat
    Dim DNAseq As New List(Of DNAentry)
    Dim totalError As Single
    Dim brushSide = 300
    Dim padding As Integer
    Dim stage As Integer
    Dim generation As Integer
    Dim generationTotal As Integer
    Dim stageTotal = sliders.TrackBar2.Value
    Dim cachedImage As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        sliders.setupTrackBar1(ocvb, caller, "Number of Generations", 1, 100, 20)
        sliders.setupTrackBar2("Number of Stages", 1, 200, 100)
        sliders.setupTrackBar3("Brushstroke count ", 1, 100, 10)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Snapshot Video input to initialize genetic drawing"

        gradient = New Gradient_CartToPolar(ocvb)

        For i = 0 To brushes.Count - 1
            brushes(i) = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/GeneticDrawingBrushes/" + CStr(i) + ".jpg").CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Next
        label1 = "Original Image"
        ocvb.desc = "Create a painting from the current video input using a genetic algorithm - painterly"
    End Sub
    Private Function newPosition() As cv.Point2f
        If samplingMask IsNot Nothing Then

        End If
        Return New cv.Point2f(msRNG.Next(src.Width), msRNG.Next(src.Height))
    End Function
    Private Function startNextStage() As cv.Mat
        Dim nextImage = New cv.Mat(New cv.Size(dst1.Width + 2 * padding, dst1.Height + 2 * padding), cv.MatType.CV_8U, 0)
        nextImage(New cv.Rect(padding, padding, dst2.Width, dst2.Height)) = dst2.Clone()
        For Each d In DNAseq
            Dim brushImg = brushes(d.brushNumber)

            Dim br = brushImg.Resize(New cv.Size(brushImg.Width * d.size + 1, brushImg.Height * d.size + 1))
            Dim m = cv.Cv2.GetRotationMatrix2D(New cv.Point2f(br.Cols / 2, br.Rows / 2), d.rotation, 1)
            cv.Cv2.WarpAffine(br, br, m, New cv.Size(br.Cols, br.Rows))

            Dim foreground = New cv.Mat(br.Size(), cv.MatType.CV_32F, CSng(d.color))
            Dim background As New cv.Mat, alpha As New cv.Mat
            Dim rect = New cv.Rect(d.pt.X, d.pt.Y, br.Width, br.Height)
            nextImage(rect).ConvertTo(background, cv.MatType.CV_32F)

            br.ConvertTo(alpha, cv.MatType.CV_32F, 1 / 255)
            cv.Cv2.Multiply(alpha, foreground, foreground)

            cv.Cv2.Multiply((1.0 - alpha).ToMat, background, background)
            cv.Cv2.Add(foreground, background, foreground)
            foreground.ConvertTo(nextImage(rect), cv.MatType.CV_8U)
        Next
        Return nextImage(New cv.Rect(padding, padding, dst2.Width, dst2.Height))
    End Function
    Private Function calcBrushSize(range As cv.Vec2f) As Single
        Dim t = stage / Math.Max(stageTotal - 1, 1)
        Return (range.Item1 - range.Item0) * (-t * t + 1) + range.Item0
    End Function
    Private Sub reinitialize()
        DNAseq.Clear()
        minSize = calcBrushSize(minBrush)
        maxSize = calcBrushSize(maxBrush)
        padding = CInt(brushSide * maxSize / 2 + 5)

        Dim brushstrokeCount = sliders.TrackBar3.Value
        For i = 0 To brushstrokeCount - 1
            Dim e = New DNAentry
            e.color = msRNG.Next(0, 255)
            e.size = msRNG.NextDouble() * (maxSize - minSize) + minSize
            e.pt = newPosition()
            Dim localMagnitude = gradient.magnitude.Get(Of Single)(e.pt.Y, e.pt.X)
            Dim localAngle = gradient.angle.Get(Of Single)(e.pt.Y, e.pt.X) + 90
            e.rotation = (msRNG.Next(-180, 180) * (1 - localMagnitude) + localAngle)
            e.brushNumber = CInt(msRNG.Next(0, maxBrushNumber - 1))
            DNAseq.Add(e)
        Next

        cachedImage = startNextStage()

        ' compute error for resulting image.
        Dim diff1 As New cv.Mat, diff2 As New cv.Mat
        cv.Cv2.Subtract(dst2, cachedImage, diff1)
        cv.Cv2.Subtract(cachedImage, dst2, diff2)
        Dim diff As New cv.Mat
        cv.Cv2.Add(diff1, diff2, diff)
        totalError = diff.Sum()

        dst2 = cachedImage
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        stageTotal = sliders.TrackBar2.Value
        If stage >= stageTotal Then Exit Sub ' request is complete...
        If generationTotal <> sliders.TrackBar1.Value Or stageTotal <> sliders.TrackBar2.Value Or check.Box(0).Checked Then
            dst2 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, 0)
            If check.Box(0).Checked Then
                dst1 = src
                check.Box(0).Checked = False
            End If
            generationTotal = sliders.TrackBar1.Value
            stageTotal = sliders.TrackBar2.Value
            generation = 0
            stage = 0
            samplingMask = Nothing

            If standalone Then
                src = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/GeneticDrawingExample.jpg").Resize(src.Size())
            End If
            dst1 = src
            dst1 = If(dst1.Channels = 3, dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY), dst1)
            gradient.src = dst1
            gradient.Run(ocvb)
            reinitialize()
        End If

        If samplingMask IsNot Nothing Then
            samplingMask = Nothing
        Else
            Dim startStage = stageTotal * 0.2
            If stage >= startStage Then
                Dim blurPercent = (1.0 - (stage - startStage) / Math.Max(stageTotal - startStage - 1, 1)) * 0.25 + 0.005
                Dim kernelSize = CInt(blurPercent * dst1.Width)
                If kernelSize Mod 2 = 0 Then kernelSize += 1
                If kernelSize > 1 Then cv.Cv2.GaussianBlur(gradient.magnitude, gradient.magnitude, New cv.Size(kernelSize, kernelSize), 0, 0)
                samplingMask = gradient.magnitude.Normalize(255)
                cv.Cv2.ImShow("samplingMask", samplingMask.ConvertScaleAbs(255))
            End If
        End If

        generation += 1

        If generation = generationTotal Then
            generation = 0
            samplingMask = Nothing
            stage += 1
            reinitialize()
        End If
        label2 = " stage " + CStr(stage) + " Generation " + CStr(generation)
    End Sub
End Class