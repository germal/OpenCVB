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



' https://github.com/anopara/genetic-drawing
Public Class GeneticDrawing_Basics
    Inherits ocvbClass
    Dim gradient As Gradient_CartToPolar
    Dim minBrush = New cv.Vec2f(0.1, 0.3)
    Dim maxBrush = New cv.Vec2f(0.3, 0.7)
    Dim minSize As Single
    Dim maxSize As Single
    Dim brushes(4 - 1) As cv.Mat
    Dim DNAseq() As DNAentry
    Dim totalError As Single
    Dim brushSize = 300
    Dim padding As Integer
    Dim stage As Integer
    Dim generation As Integer
    Dim generationTotal As Integer
    Dim stageTotal As Integer
    Dim canvas As cv.Mat
    Dim imgBuffer As cv.Mat
    Dim mats As Mat_4to1
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Snapshot Video input to initialize genetic drawing"

        gradient = New Gradient_CartToPolar(ocvb)

        For i = 0 To brushes.Count - 1
            brushes(i) = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/GeneticDrawingBrushes/" + CStr(i) + ".jpg").CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Next

        mats = New Mat_4to1(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Number of Generations", 1, 100, 20)
        sliders.setupTrackBar(1, "Number of Stages", 1, 200, 100)
        sliders.setupTrackBar(2, "Brushstroke count per generation", 1, 100, 10)
        stageTotal = sliders.trackbar(1).Value

        label1 = "(clkwise) original, tbd, canvas, magnitude"
        label2 = "Current result"
        ocvb.desc = "Create a painting from the current video input using a genetic algorithm - painterly"
    End Sub
    Private Function runDNAseq(dna() As DNAentry) As cv.Mat
        Dim nextImage = New cv.Mat(New cv.Size(dst1.Width + 2 * padding, dst1.Height + 2 * padding), cv.MatType.CV_8U, 0)
        nextImage(New cv.Rect(padding, padding, canvas.Width, canvas.Height)) = canvas
        For i = 0 To dna.Count - 1
            Dim d = dna(i)
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
        Return nextImage(New cv.Rect(padding, padding, canvas.Width, canvas.Height))
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
        canvas = imgBuffer
        Dim brushstrokeCount = sliders.trackbar(2).Value
        ReDim DNAseq(brushstrokeCount - 1)
        minSize = calcBrushSize(minBrush)
        maxSize = calcBrushSize(maxBrush)
        padding = CInt(brushSize * maxSize / 2 + 5)

        For i = 0 To brushstrokeCount - 1
            Dim e = New DNAentry
            e.color = msRNG.Next(0, 255)
            e.size = msRNG.NextDouble() * (maxSize - minSize) + minSize
            e.pt = New cv.Point(msRNG.Next(src.Width), msRNG.Next(src.Height))
            Dim localMagnitude = gradient.magnitude.Get(Of Single)(e.pt.Y, e.pt.X)
            Dim localAngle = gradient.angle.Get(Of Single)(e.pt.Y, e.pt.X) + 90
            e.rotation = (msRNG.Next(-180, 180) * (1 - localMagnitude) + localAngle)
            e.brushNumber = CInt(msRNG.Next(0, brushes.Length - 1))
            DNAseq(i) = e
        Next

        mats.mat(3) = runDNAseq(DNAseq)
        totalError = calculateError(mats.mat(3))
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        stageTotal = sliders.trackbar(1).Value
        If stage >= stageTotal Then Exit Sub ' request is complete...
        If generationTotal <> sliders.trackbar(0).Value Or stageTotal <> sliders.trackbar(1).Value Or check.Box(0).Checked Then
            dst2 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, 0)
            canvas = dst2.Clone
            imgBuffer = dst2.Clone
            generationTotal = sliders.trackbar(0).Value
            stageTotal = sliders.trackbar(1).Value
            generation = 0
            stage = 0

            If standalone Then
                src = If(check.Box(0).Checked, ocvb.color.Clone, cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/GeneticDrawingExample.jpg").Resize(src.Size()))
            End If
            check.Box(0).Checked = False

            src = If(src.Channels = 3, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY), src)
            mats.mat(0) = src
            gradient.src = mats.mat(0)
            gradient.Run(ocvb)
            mats.mat(2) = gradient.magnitude.ConvertScaleAbs(255)

            startNewStage()
        End If

        ' evolve!
        Dim nextDNA(DNAseq.Count - 1) As DNAentry
        For i = 0 To DNAseq.Count - 1
            nextDNA(i) = DNAseq(i)
        Next
        Dim changes As Integer, childImg As New cv.Mat, maxOption = 5, saveTotalError = totalError, bestError As Single
        For i = 0 To nextDNA.Count - 1
            Dim changeCount = msRNG.Next(0, maxOption) + 1
            For j = 0 To changeCount - 1
                Select Case msRNG.Next(0, maxOption)
                    Case 0
                        nextDNA(i).color = CInt(msRNG.Next(0, 255))
                    Case 1, 2
                        nextDNA(i).pt = New cv.Point(msRNG.Next(src.Width), msRNG.Next(src.Height))
                    Case 3
                        nextDNA(i).size = msRNG.NextDouble() * (maxSize - minSize) + minSize
                    Case 4
                        Dim localMagnitude = gradient.magnitude.Get(Of Single)(nextDNA(i).pt.Y, nextDNA(i).pt.X)
                        Dim localAngle = gradient.angle.Get(Of Single)(nextDNA(i).pt.Y, nextDNA(i).pt.X) + 90
                        nextDNA(i).rotation = (msRNG.Next(-180, 180) * (1 - localMagnitude) + localAngle)
                    Case Else
                        nextDNA(i).brushNumber = CInt(msRNG.Next(0, brushes.Length - 1))
                End Select
            Next

            childImg = runDNAseq(nextDNA)
            Dim nextError = calculateError(childImg)
            If nextError < totalError Then
                bestError = nextError
                Console.WriteLine("besterror = " + CStr(bestError))
                changes += 1
            Else
                nextDNA(i) = DNAseq(i)
            End If
        Next

        If changes Then
            totalError = bestError
            dst2 = runDNAseq(nextDNA)
            DNAseq = nextDNA
        End If

        If saveTotalError < totalError Then saveTotalError = saveTotalError
        saveTotalError = totalError

        generation += 1
        If generation = generationTotal Then
            imgBuffer = dst2
            generation = 0
            stage += 1
            startNewStage()
        End If

        mats.Run(ocvb)
        dst1 = mats.dst1
        label2 = " stage " + CStr(stage) + " Gen " + Format(generation, "00") + " changes = " + CStr(changes) + " err/1000 = " + CStr(CInt(totalError / 1000))
    End Sub
End Class