Imports cv = OpenCvSharp
Imports NAudio.Wave
' https://archive.codeplex.com/?p=naudio
' http://ismir2002.ismir.net/proceedings/02-FP04-2.pdf
Public Class Sound_ToPCM
    Inherits ocvbClass
    Dim audio As New OptionsAudio
    Dim reader As MediaFoundationReader
    Dim memData As WaveBuffer
    Dim pcmData() As Short
    Dim saveFileName As String
    Public pcm32f As New cv.Mat
    Private Sub LoadSoundData()
        If audio.fileinfo.Exists Then
            If reader IsNot Nothing Then reader.Dispose()
            reader = New MediaFoundationReader(audio.fileinfo.FullName)
            Dim tmp(reader.Length - 1) As Byte
            Dim count = reader.Read(tmp, 0, tmp.Length)
            If count <> tmp.Length Then Console.WriteLine("File " + audio.fileinfo.FullName + " was not completely read.  What happened?")
            If reader.WaveFormat.Channels <> 2 Or reader.WaveFormat.BitsPerSample <> 16 Then
                MsgBox("Only 16-bit stereo data is supported at this point...")
                Exit Sub
            End If
            memData = New WaveBuffer(tmp)
            ReDim pcmData(count / 2 - 1)
            For i = 0 To count / 2 - 1
                pcmData(i) = memData.ShortBuffer(i)
            Next
        End If
        saveFileName = audio.fileinfo.FullName
    End Sub
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        audio.Show()
        audio.NewAudio(ocvb)
        LoadSoundData()
        ocvb.desc = "Load a .wav or .MP3 file and convert to PCM"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If saveFileName <> audio.fileinfo.FullName Then LoadSoundData()
        If audio.fileinfo.Exists Then
            Dim input = New cv.Mat(pcmData.Length / 2, 1, cv.MatType.CV_16SC2, pcmData) ' divide by 2 because it is stereo data.
            input.ConvertTo(pcm32f, cv.MatType.CV_32F)
            'For i = 0 To dst1.Width - 1

            'Next
        End If
    End Sub
End Class