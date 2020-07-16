Imports cv = OpenCvSharp
Imports System.IO
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
    Public gradient As Gradient_CartToPolar
    Public minBrushRange = New cv.Rangef(0.1, 0.3)
    Public maxBrushRange = New cv.Rangef(0.3, 0.7)
    Dim minSize As Single
    Dim maxSize As Single
    Public brushes(4 - 1) As cv.Mat
    Dim DNAseq() As DNAentry
    Dim totalError As Single
    Dim stage As Integer
    Public generation As Integer
    Dim generationTotal As Integer
    Dim gradientMagContrast As Integer
    Dim sobelKernel As Integer
    Dim stageTotal As Integer
    Dim imgGeneration As cv.Mat
    Dim imgStage As cv.Mat
    Public mats As Mat_4to1
    Dim brushPercent As Integer
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Snapshot Video input to initialize genetic drawing"
        check.Box(1).Text = "Restart the algorithm with the current settings"
        check.Box(1).Checked = True

        gradient = New Gradient_CartToPolar(ocvb)

        For i = 0 To brushes.Count - 1
            brushes(i) = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/GeneticDrawingBrushes/" + CStr(i) + ".jpg").CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Next

        mats = New Mat_4to1(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Number of Generations", 1, 200, 20)
        sliders.setupTrackBar(1, "Number of Stages", 1, 2000, 100)
        sliders.setupTrackBar(2, "Brushstroke count per generation", 1, 100, 10)
        sliders.setupTrackBar(3, "Brush size Percentage", 5, 100, 100)
        stageTotal = sliders.trackbar(1).Value

        label1 = "(clkwise) original, imgStage, imgGeneration, magnitude"
        label2 = "Current result"
        ocvb.desc = "Create a painting from the current video input using a genetic algorithm - painterly"
    End Sub
    Private Function runDNAseq(dna() As DNAentry) As cv.Mat
        Dim padding = 110
        Dim nextImage = New cv.Mat(New cv.Size(dst1.Width + 2 * padding, dst1.Height + 2 * padding), cv.MatType.CV_8U, 0)
        nextImage(New cv.Rect(padding, padding, imgGeneration.Width, imgGeneration.Height)) = imgGeneration
        For i = 0 To dna.Count - 1
            Dim d = dna(i)
            Dim brushImg = brushes(d.brushNumber)

            Dim br = brushImg.Resize(New cv.Size((brushImg.Width * d.size + 1) * brushPercent / 100, (brushImg.Height * d.size + 1) * brushPercent / 100))
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
        Return nextImage(New cv.Rect(padding, padding, imgGeneration.Width, imgGeneration.Height))
    End Function
    Private Function calcBrushSize(range As cv.Rangef) As Single
        Dim t = stage / Math.Max(stageTotal - 1, 1)
        Return (range.End - range.Start) * (-t * t + 1) + range.Start
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
        Dim brushstrokeCount = sliders.trackbar(2).Value

        ReDim DNAseq(brushstrokeCount - 1)
        minSize = calcBrushSize(minBrushRange)
        maxSize = calcBrushSize(maxBrushRange)

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

        imgGeneration = imgStage
        mats.mat(3) = runDNAseq(DNAseq)
        totalError = calculateError(mats.mat(3))
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        brushPercent = sliders.trackbar(3).Value
        stageTotal = sliders.trackbar(1).Value
        generationTotal = sliders.trackbar(0).Value
        gradientMagContrast = gradient.sliders.trackbar(0).Value
        sobelKernel = gradient.basics.sobel.sliders.trackbar(0).Value
        If check.Box(1).Checked Then
            check.Box(1).Checked = False
            stageTotal = sliders.trackbar(1).Value
            dst2 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, 0)
            imgStage = dst2.Clone
            generationTotal = sliders.trackbar(0).Value
            stageTotal = sliders.trackbar(1).Value
            gradientMagContrast = gradient.sliders.trackbar(0).Value
            sobelKernel = gradient.basics.sobel.sliders.trackbar(0).Value
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
        If stage >= stageTotal Then Exit Sub ' request is complete...

        ' evolve!
        Dim nextDNA(DNAseq.Count - 1) As DNAentry
        For i = 0 To DNAseq.Count - 1
            nextDNA(i) = DNAseq(i)
        Next
        Dim changes As Integer, childImg = imgGeneration.Clone, maxOption = 5, bestError As Single
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

        generation += 1
        If generation = generationTotal Then
            imgStage = dst2
            mats.mat(1) = imgStage
            generation = 0
            stage += 1
            startNewStage()
        End If

        mats.Run(ocvb)
        dst1 = mats.dst1
        label2 = " stage " + CStr(stage) + "/" + CStr(stageTotal) + " Gen " + Format(generation, "00") + " chgs = " + CStr(changes) + " err/1000 = " + CStr(CInt(totalError / 1000))

    End Sub
End Class







' https://github.com/anopara/genetic-drawing
Public Class GeneticDrawing_Color
    Inherits ocvbClass
    Dim gDraw(3 - 1) As GeneticDrawing_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        ocvb.suppressOptions = True
        gDraw(0) = New GeneticDrawing_Basics(ocvb)
        gDraw(1) = New GeneticDrawing_Basics(ocvb)
        ocvb.suppressOptions = False
        gDraw(2) = New GeneticDrawing_Basics(ocvb) ' options for the red channel are visible and will be sync below with the other channels if changed.

        label1 = "Intermediate results - original+2 partial+Mag"
        ocvb.desc = "Use the GeneticDrawing_Basics to create a color painting.  Painterly"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim split() As cv.Mat
        split = cv.Cv2.Split(src)

        For i = 0 To split.Count - 1
            For j = 0 To gDraw(2).sliders.trackbar.Count - 1
                gDraw(i).sliders.trackbar(j).Value = gDraw(2).sliders.trackbar(j).Value
            Next
            For j = 0 To gDraw(2).gradient.sliders.trackbar.Count - 1
                gDraw(i).gradient.sliders.trackbar(j).Value = gDraw(2).gradient.sliders.trackbar(j).Value
            Next

            gDraw(i).check.Box(0).Checked = gDraw(2).check.Box(0).Checked
            gDraw(i).check.Box(1).Checked = gDraw(2).check.Box(1).Checked
        Next

        For i = 0 To split.Count - 1
            gDraw(i).src = split(i)
            gDraw(i).Run(ocvb)
            split(i) = gDraw(i).dst2
        Next

        cv.Cv2.Merge(split, dst2)

        For i = 0 To split.Count - 1
            split(i) = gDraw(i).dst1
        Next
        cv.Cv2.Merge(split, dst1)

        label2 = gDraw(2).label2
    End Sub
    Public Sub close()
        For i = 0 To gDraw.Count - 1
            gDraw(i).Dispose()
        Next
    End Sub
End Class





Public Class GeneticDrawing_Photo
    Inherits ocvbClass
    Dim gDraw As GeneticDrawing_Color
    Dim saveFileName As String
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        ocvb.parms.openFileDialogRequested = True
        ocvb.parms.openFileInitialDirectory = ocvb.parms.HomeDir + "Data\"
        ocvb.parms.openFileDialogName = GetSetting("OpenCVB", "PhotoFileName", "PhotoFileName", ocvb.parms.HomeDir + "Data/GeneticDrawingExample.jpg")
        ocvb.parms.openFileFilter = "jpg (*.jpg)|*.jpg|png (*.png)|*.png|bmp (*.bmp)|*.bmp|All files (*.*)|*.*"
        ocvb.parms.openFileFilterIndex = 1
        ocvb.parms.openFileDialogTitle = "Select an image file to create a paint version"
        ocvb.parms.initialStartSetting = True

        ocvb.desc = "Apply genetic drawing technique to any still photo."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If saveFileName <> ocvb.parms.openFileDialogName Or ocvb.frameCount = 0 Then
            Dim fileinfo = New FileInfo(ocvb.parms.openFileDialogName)
            If fileinfo.Exists = False Then
                label1 = "No input file.  Use dialogbox below..."
                Exit Sub
            End If

            Dim fullsizeImage = cv.Cv2.ImRead(fileinfo.FullName)
            If fullsizeImage.Channels <> 3 Then
                label1 = "Input file must be RGB 3-channel image!"
                Exit Sub
            End If
            saveFileName = ocvb.parms.openFileDialogName

            If gDraw IsNot Nothing Then gDraw.Dispose()
            gDraw = New GeneticDrawing_Color(ocvb)
            If fullsizeImage.Width <> dst1.Width Or fullsizeImage.Height <> dst1.Height Then
                Dim newSize = New cv.Size(dst1.Height * fullsizeImage.Width / fullsizeImage.Height, dst1.Height)
                If newSize.Width > dst1.Width Then
                    newSize = New cv.Size(dst1.Width, dst1.Width * fullsizeImage.Height / fullsizeImage.Width)
                End If
                src.SetTo(0)
                src(New cv.Rect(0, 0, newSize.Width, newSize.Height)) = fullsizeImage.Resize(newSize)
            Else
                src = fullsizeImage
            End If
            SaveSetting("OpenCVB", "PhotoFileName", "PhotoFileName", FileInfo.FullName)
            gDraw.src = src
        End If

        gDraw.Run(ocvb)

        dst1 = gDraw.dst1
        dst2 = gDraw.dst2
        label1 = gDraw.label1
        label2 = gDraw.label2
    End Sub
End Class