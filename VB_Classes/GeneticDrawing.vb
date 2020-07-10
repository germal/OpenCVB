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
    Dim brushes(4 - 1) As cv.Mat
    Dim DNAseq As New List(Of DNAentry)
    Dim totalError As Single
    Dim brushSide = 300
    Dim padding As Integer
    Dim stage As Integer
    Dim generation As Integer
    Dim generationTotal As Integer
    Dim stageTotal As Integer
    Dim cachedImage As New cv.Mat
    Dim mats As Mat_4to1
    Dim random As Random_CustomHistogram
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Snapshot Video input to initialize genetic drawing"

        gradient = New Gradient_CartToPolar(ocvb)

        For i = 0 To brushes.Count - 1
            brushes(i) = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/GeneticDrawingBrushes/" + CStr(i) + ".jpg").CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Next

        mats = New Mat_4to1(ocvb)

        random = New Random_CustomHistogram(ocvb)
        random.random.outputRandom = New cv.Mat(1, 1, cv.MatType.CV_32S, 0)
        random.hist.plotHist.backColor = cv.Scalar.White

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Number of Generations", 1, 100, 20)
        sliders.setupTrackBar(1, "Number of Stages", 1, 200, 100)
        sliders.setupTrackBar(2, "Brushstroke count ", 1, 100, 10)
        stageTotal = sliders.sliders(1).Value

        label1 = "(clkwise) img, smaplingMask, histogram, magnitude"
        label2 = "Current result"
        ocvb.desc = "Create a painting from the current video input using a genetic algorithm - painterly"
    End Sub
    Private Function runDNAseq() As cv.Mat
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
    Private Function calculateError(ByRef img As cv.Mat) As Single
        ' compute error for resulting image.
        Dim diff1 As New cv.Mat, diff2 As New cv.Mat
        cv.Cv2.Subtract(mats.mat(0), img, diff1)
        cv.Cv2.Subtract(img, mats.mat(0), diff2)
        cv.Cv2.Add(diff1, diff2, diff1)
        Return diff1.Sum()
    End Function
    Private Sub startNewStage()
        DNAseq.Clear()
        minSize = calcBrushSize(minBrush)
        maxSize = calcBrushSize(maxBrush)
        padding = CInt(brushSide * maxSize / 2 + 5)

        Dim brushstrokeCount = sliders.sliders(2).Value
        For i = 0 To brushstrokeCount - 1
            Dim e = New DNAentry
            e.color = msRNG.Next(0, 255)
            e.size = msRNG.NextDouble() * (maxSize - minSize) + minSize
            e.pt = New cv.Point(msRNG.Next(src.Width), msRNG.Next(src.Height))
            Dim localMagnitude = gradient.magnitude.Get(Of Single)(e.pt.Y, e.pt.X)
            Dim localAngle = gradient.angle.Get(Of Single)(e.pt.Y, e.pt.X) + 90
            e.rotation = (msRNG.Next(-180, 180) * (1 - localMagnitude) + localAngle)
            e.brushNumber = CInt(msRNG.Next(0, brushes.Length - 1))
            DNAseq.Add(e)
        Next

        dst2 = runDNAseq()
        totalError = calculateError(dst2)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        stageTotal = sliders.sliders(1).Value
        If stage >= stageTotal Then Exit Sub ' request is complete...
        If generationTotal <> sliders.sliders(0).Value Or stageTotal <> sliders.sliders(1).Value Or check.Box(0).Checked Then
            dst2 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, 0)
            check.Box(0).Checked = False
            generationTotal = sliders.sliders(0).Value
            stageTotal = sliders.sliders(1).Value
            generation = 0
            stage = 0
            samplingMask = Nothing

            If standalone Then
                src = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/GeneticDrawingExample.jpg").Resize(src.Size())
            End If
            src = If(src.Channels = 3, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src)
            mats.mat(0) = src
            gradient.src = mats.mat(0)
            gradient.Run(ocvb)
            mats.mat(2) = gradient.magnitude.ConvertScaleAbs(255)

            random.src = src
            random.Run(ocvb) ' create a custom random number generator that reflects the histogram of the src image.
            mats.mat(3) = random.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            startNewStage()
        End If

        If samplingMask IsNot Nothing Then
            samplingMask = Nothing
        Else
            Dim startStage = stageTotal * 0.2
            Dim blurPercent = (1.0 - (stage - startStage) / Math.Max(stageTotal - startStage - 1, 1)) * 0.25 + 0.005
            Dim kernelSize = CInt(blurPercent * src.Width)
            If kernelSize Mod 2 = 0 Then kernelSize += 1
            samplingMask = New cv.Mat
            If kernelSize > 1 Then cv.Cv2.GaussianBlur(gradient.magnitude, samplingMask, New cv.Size(kernelSize, kernelSize), 0, 0)
            samplingMask = samplingMask.Normalize(255)
            mats.mat(1) = samplingMask.ConvertScaleAbs(255)
        End If

        ' evolve!
        Dim maxOption = 5
        Dim nextDNA = DNAseq
        Dim childImg As New cv.Mat
        For i = 0 To DNAseq.Count - 1
            Dim child = DNAseq.ElementAt(i)
            Dim changeCount = msRNG.Next(0, maxOption)
            For j = 0 To changeCount - 1
                Select Case msRNG.Next(0, maxOption)
                    Case 0
                        child.color = CInt(msRNG.Next(0, 255))
                    Case 1, 2
                        child.pt = New cv.Point(msRNG.Next(src.Width), msRNG.Next(src.Height))
                    Case 3
                        child.size = msRNG.NextDouble() * (maxSize - minSize) + minSize
                    Case 4
                        Dim localMagnitude = gradient.magnitude.Get(Of Single)(child.pt.Y, child.pt.X)
                        Dim localAngle = gradient.angle.Get(Of Single)(child.pt.Y, child.pt.X) + 90
                        child.rotation = (msRNG.Next(-180, 180) * (1 - localMagnitude) + localAngle)
                    Case Else
                        child.brushNumber = CInt(msRNG.Next(0, brushes.Length - 1))
                End Select
            Next
            childImg = runDNAseq()
            Dim nextError = calculateError(childImg)
            If nextError < totalError Then
                nextDNA.RemoveAt(i)
                nextDNA.Add(child)
                totalError = nextError
            End If
        Next

        DNAseq = nextDNA
        generation += 1
        If generation = generationTotal Then
            generation = 0
            samplingMask = Nothing
            stage += 1
            startNewStage()
        End If

        mats.Run(ocvb)
        dst1 = mats.dst1
        label2 = " stage " + CStr(stage) + " Generation " + CStr(generation)
    End Sub
End Class