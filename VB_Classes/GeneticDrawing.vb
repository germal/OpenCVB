Imports cv = OpenCvSharp
Imports py = Python.Runtime
Public Class GeneticDrawing_Basics
    Inherits ocvbClass
    Dim samplingMask As cv.Mat
    Dim seed As Double
    Dim gradient As Gradient_NumPy
    Dim brushesRange(,) As Double = {{0.1, 0.3}, {0.3, 0.7}}
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        If standalone Then
            dst1 = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/GeneticDrawingExample.jpg").Resize(src.Size())
            'samplingMask = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/Genetic")
        End If
        dst2 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, 0)
        seed = msRNG.NextDouble() * 100000000000

        sliders.setupTrackBar1(ocvb, caller, "Number of Generations", 1, 100, 20)
        sliders.setupTrackBar2("Number of Stages", 1, 200, 100)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Snapshot Video input to initialize genetic drawing"

        gradient = New Gradient_NumPy(ocvb)

        label1 = "Original Image"
        ocvb.desc = "Create a painting from the current video input using a genetic algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static generationTotal = sliders.TrackBar1.Value
        Static stageTotal As Integer
        Static stage As Integer
        Static generation As Integer
        If stage >= stageTotal Then Exit Sub ' request is complete...
        If generationTotal <> sliders.TrackBar1.Value Or stageTotal <> sliders.TrackBar2.Value Or check.Box(0).Checked Then
            If check.Box(0).Checked Then
                dst1 = src
                check.Box(0).Checked = False
            End If
            generationTotal = sliders.TrackBar1.Value
            stageTotal = sliders.TrackBar2.Value
            generation = 0
            stage = 0
            samplingMask = Nothing

            If standalone = False Then dst1 = src
            dst1 = If(dst1.Channels = 3, dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY), dst1)
            gradient.src = dst1
            gradient.Run(ocvb)
        End If

        If samplingMask IsNot Nothing Then
            samplingMask = Nothing
        Else
            Dim startStage = stageTotal * 0.2
            If stage >= startStage Then
                Dim blurPercent = (1.0 - (stage - startStage) / Math.Max(stageTotal - startStage - 1, 1)) * 0.25 + 0.005
            End If
        End If

        generation += 1
        If generation = generationTotal Then
            generation = 0
            samplingMask = Nothing
            stage += 1
        End If
        label2 = "Generation " + CStr(generation) + " stage " + CStr(stage)
    End Sub
End Class