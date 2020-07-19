Imports cv = OpenCvSharp
Imports System.IO
Imports NAudio.Wave
Imports NAudio.Wave.SampleProviders.SignalGeneratorType
' https://archive.codeplex.com/?p=naudio
' http://ismir2002.ismir.net/proceedings/02-FP04-2.pdf
Public Class Sound_ToPCM
    Inherits ocvbClass
    Public reader As MediaFoundationReader
    Dim memData As WaveBuffer
    Dim pcmData() As Short
    Dim pcmDuration As Single ' in seconds.
    Dim currentTime As Date
    Dim startTime As Date
    Dim inputFileName As FileInfo
    Public pcm32f As New cv.Mat
    Public player As IWavePlayer
    Dim savefileStarted As Boolean
    Private Sub LoadSoundData(ocvb As AlgorithmData)
        inputFileName = New FileInfo(ocvb.parms.openFileDialogName)
        If inputFileName.Exists Then
            SaveSetting("OpenCVB", "AudioFileName", "AudioFileName", inputFileName.FullName)
            If reader IsNot Nothing Then reader.Dispose()
            Dim settings = New MediaFoundationReader.MediaFoundationReaderSettings()
            reader = New MediaFoundationReader(inputFileName.FullName, settings)
            Dim tmp(reader.Length - 1) As Byte
            Dim count = reader.Read(tmp, 0, tmp.Length)
            If count <> tmp.Length Then Console.WriteLine("File " + inputFileName.FullName + " was not completely read.  What happened?")
            If reader.WaveFormat.Channels <> 2 Or reader.WaveFormat.BitsPerSample <> 16 Then MsgBox("16-bit stereo data was expected.  Others are not tested.")
            memData = New WaveBuffer(tmp)
            ReDim pcmData(count / 2 - 1)
            For i = 0 To count / 2 - 1
                pcmData(i) = memData.ShortBuffer(i)
            Next
            pcmDuration = reader.TotalTime.TotalSeconds
        End If
    End Sub
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        ocvb.parms.openFileDialogRequested = True
        ocvb.parms.openFileInitialDirectory = ocvb.parms.HomeDir + "Data\"
        ocvb.parms.openFileDialogName = GetSetting("OpenCVB", "AudioFileName", "AudioFileName", "")
        ocvb.parms.openFileFilter = "m4a (*.m4a)|*.m4a|mp3 (*.mp3)|*.mp3|mp4 (*.mp4)|*.mp4|wav (*.wav)|*.wav|aac (*.aac)|*.aac|All files (*.*)|*.*"
        ocvb.parms.openFileFilterIndex = 1
        ocvb.parms.openFileDialogTitle = "Select an audio file to analyze"
        ocvb.parms.initialStartSetting = True

        LoadSoundData(ocvb)
        ocvb.desc = "Load an audio file, play it, and convert to PCM"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If inputFileName.FullName <> ocvb.parms.openFileDialogName Then
            If savefileStarted Then
                player.Stop()
                player.Dispose()
                savefileStarted = False
            End If
            LoadSoundData(ocvb)
        End If
        If inputFileName.Exists Then
            If savefileStarted <> ocvb.parms.fileStarted Then
                savefileStarted = ocvb.parms.fileStarted
                If ocvb.parms.fileStarted Then
                    Dim reader = New MediaFoundationReader(inputFileName.FullName)
                    player = New WaveOut()
                    player.Init(reader)
                    player.Play()
                    startTime = Now
                Else
                    player.Stop()
                    player.Dispose()
                End If

                Dim channels = reader.WaveFormat.Channels
                Dim bpSample = reader.WaveFormat.BitsPerSample
                Dim mattype = cv.MatType.CV_16SC2
                If bpSample = 8 And channels = 1 Then mattype = cv.MatType.CV_8U
                If bpSample = 8 And channels = 2 Then mattype = cv.MatType.CV_8UC2
                If bpSample = 16 And channels = 1 Then mattype = cv.MatType.CV_16SC1
                Dim input = New cv.Mat(pcmData.Length / channels, 1, mattype, pcmData)
                input.ConvertTo(pcm32f, cv.MatType.CV_32F)
            End If
        End If
        ocvb.parms.openFileSliderPercent = (Now - startTime).TotalSeconds / pcmDuration
        If ocvb.parms.openFileSliderPercent > 1 Then savefileStarted = False
        ocvb.putText(New TTtext("Requested sound data is in the pcm32f cv.Mat", 10, 50, RESULT1))
    End Sub
    Public Sub Close()
        If savefileStarted Then player.Stop()
        player.Dispose()
        reader.Dispose()
    End Sub
End Class







' https://github.com/naudio/sinegenerator-sample
Public Class Sound_SignalGenerator
    Inherits ocvbClass
    Dim player As NAudio.Wave.IWavePlayer
    Dim wGen As New NAudio.Wave.SampleProviders.SignalGenerator
    Public pcm32f As New cv.Mat
    Dim pcmData() As Single
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        sliders.Setup(ocvb, caller, 4)
        sliders.setupTrackBar(0, "Sine Wave Frequency", 10, 4000, 1000)
        sliders.setupTrackBar(1, "Decibels", -100, 0, -20)
        sliders.setupTrackBar(2, "Sweep Only - End Frequency", 20, 4000, 1000)
        sliders.setupTrackBar(3, "Sweep Only - duration secs", 0, 10, 1)

        radio.Setup(ocvb, caller, 7)
        For i = 0 To radio.check.Count - 1
            radio.check(i).Text = Choose(i + 1, "Pink", "White", "Sweep", "Sin", "Square", "Triangle", "SawTooth")
        Next
        radio.check(0).Checked = True

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "PhaseReverse Left"
        check.Box(1).Text = "PhaseReverse Right"

        Dim waveoutEvent = New NAudio.Wave.WaveOutEvent
        waveoutEvent.NumberOfBuffers = 2
        waveoutEvent.DesiredLatency = 100
        player = waveoutEvent
        player.Init(wGen)

        ReDim pcmData(10 * 44100) ' enough for about 10 seconds of audio.
        ocvb.desc = "Generate sound with a sine waveform."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then wGen.Type = Choose(i + 1, Pink, White, Sweep, Sin, Square, Triangle, SawTooth)
        Next

        wGen.PhaseReverse(0) = check.Box(0).Checked
        wGen.PhaseReverse(1) = check.Box(1).Checked

        wGen.Frequency = sliders.trackbar(0).Value
        wGen.Gain = NAudio.Utils.Decibels.DecibelsToLinear(sliders.trackbar(1).Value)

        If wGen.Type = Sweep Then
            wGen.FrequencyEnd = sliders.trackbar(2).Value
            wGen.SweepLengthSecs = sliders.trackbar(3).Value
        End If

        ' save the last 10 seconds of data for use elsewhere.
        Static offset As Integer
        Dim count = wGen.Read(pcmData, offset, pcmData.Length / 10 - 1)
        offset += 44100
        If offset >= pcmData.Length - 1 Then offset = 0
        pcm32f = New cv.Mat(1, pcmData.Length, cv.MatType.CV_32F, pcmData)

        If standalone Then ocvb.putText(New TTtext("Requested sound data is in the pcm32f cv.Mat", 10, 50, RESULT1))

        If ocvb.frameCount = 0 Then player.Play()
    End Sub
End Class






Public Class Sound_Display
    Inherits ocvbClass
    Dim sound As Sound_SignalGenerator
    Dim plot As Plot_Basics_CPP
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        plot = New Plot_Basics_CPP(ocvb)

        sound = New Sound_SignalGenerator(ocvb)
        sound.radio.check(3).Checked = True

        check.Setup(ocvb, caller, 4)
        check.Box(0).Text = "Max Absolute Value"
        check.Box(1).Text = "Max RMS Value"
        check.Box(2).Text = "Sampled Peaks"
        check.Box(3).Text = "Scaled Average"
        check.Box(3).Checked = True

        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "Scale factor %", 10, 100, 25)

        plot.dst1 = New cv.Mat(New cv.Size(dst1.Width * 2, dst1.Height), cv.MatType.CV_8UC3, 0)
        ReDim plot.srcX(plot.dst1.Width)
        ReDim plot.srcY(plot.dst1.Width)
        For i = 0 To plot.srcX.Length - 1
            plot.srcX(i) = i
        Next

        ocvb.desc = "Display a sound buffer in several styles"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        sound.Run(ocvb)

        Dim totalSamples = sound.pcm32f.Cols
        Dim samplesPerColumn = CInt(totalSamples / plot.dst1.Width)
        Dim formatIndex As Integer
        For i = 0 To check.Box.Count - 1
            If check.Box(i).Checked Then formatIndex = i
        Next

        Dim pcmNeg = sound.pcm32f.Threshold(0, 0, cv.ThresholdTypes.TozeroInv)
        Dim pcmPos = sound.pcm32f.Threshold(0, 0, cv.ThresholdTypes.Tozero)
        Dim t1 = pcmNeg.CountNonZero()
        Dim t2 = pcmPos.CountNonZero()
        Console.WriteLine("trackbar = " + CStr(sliders.trackbar(0).Value))
        Select Case formatIndex
            Case 0
            Case 1
            Case 2
            Case 3
                Dim maxVal As Integer = dst1.Height / 2
                Dim scale = maxVal * sliders.trackbar(0).Value / 100
                For i = 0 To plot.srcY.Length - 1
                    Dim rect = New cv.Rect(i, 0, samplesPerColumn, 1)
                    Dim nextVal = pcmPos(rect).Sum()
                    Dim count = pcmPos(rect).CountNonZero()
                    plot.srcY(i) = maxVal + scale * CInt(nextVal.Item(0) / count * maxVal)
                Next
        End Select

        plot.Run(ocvb)
        dst1 = plot.dst1(New cv.Rect(0, 0, src.Width, src.Height))
        dst2 = plot.dst1(New cv.Rect(src.Width, 0, src.Width, src.Height))
    End Sub
End Class