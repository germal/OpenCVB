Imports cv = OpenCvSharp
Imports System.IO
Imports NAudio.Wave
' https://archive.codeplex.com/?p=naudio
' http://ismir2002.ismir.net/proceedings/02-FP04-2.pdf
Public Class Sound_ToPCM
    Inherits ocvbClass
    Dim reader As MediaFoundationReader
    Dim memData As WaveBuffer
    Dim pcmData() As Short
    Dim pcmDuration As Single ' in seconds.
    Dim currentTime As Date
    Dim startTime As Date
    Dim saveFileName As fileinfo
    Public pcm32f As New cv.Mat
    Dim player As IWavePlayer
    Dim savefileStarted As Boolean
    Private Sub LoadSoundData(ocvb As AlgorithmData)
        saveFileName = New FileInfo(ocvb.parms.openFileDialogName)
        If saveFileName.Exists Then
            SaveSetting("OpenCVB", "AudioFileName", "AudioFileName", saveFileName.FullName)
            If reader IsNot Nothing Then reader.Dispose()
            Dim settings = New MediaFoundationReader.MediaFoundationReaderSettings()
            reader = New MediaFoundationReader(saveFileName.FullName, settings)
            Dim tmp(reader.Length - 1) As Byte
            Dim count = reader.Read(tmp, 0, tmp.Length)
            If count <> tmp.Length Then Console.WriteLine("File " + saveFileName.FullName + " was not completely read.  What happened?")
            If reader.WaveFormat.Channels <> 2 Or reader.WaveFormat.BitsPerSample <> 16 Then
                MsgBox("Only 16-bit stereo data is supported at this point...")
                Exit Sub
            End If
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
        If saveFileName.FullName <> ocvb.parms.openFileDialogName Then
            If savefileStarted Then
                player.Stop()
                player.Dispose()
                savefileStarted = False
            End If
            LoadSoundData(ocvb)
        End If
        If saveFileName.Exists Then
            If savefileStarted <> ocvb.parms.fileStarted Then
                savefileStarted = ocvb.parms.fileStarted
                If ocvb.parms.fileStarted Then
                    Dim reader = New MediaFoundationReader(saveFileName.FullName)
                    player = New WaveOut()
                    player.Init(reader)
                    player.Play()
                    startTime = Now
                Else
                    player.Stop()
                    player.Dispose()
                End If
                Dim input = New cv.Mat(pcmData.Length / 2, 1, cv.MatType.CV_16SC2, pcmData) ' divide by 2 because it is stereo data.
                input.ConvertTo(pcm32f, cv.MatType.CV_32F)
                'For i = 0 To dst1.Width - 1

                'Next
            End If
        End If
        ocvb.parms.openFileSliderPercent = (Now - startTime).TotalSeconds / pcmDuration
        If ocvb.parms.openFileSliderPercent > 1 Then savefileStarted = False
    End Sub
    Public Sub Close()
        If savefileStarted Then player.Stop()
        player.Dispose()
        reader.Dispose()
    End Sub
End Class