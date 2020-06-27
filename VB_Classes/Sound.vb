Imports cv = OpenCvSharp
Imports NAudio.Wave
Public Class Sound_ToPCM
    Inherits ocvbClass
    Dim audio As New OptionsAudio
    Dim reader As MediaFoundationReader
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        audio.Show()
        audio.NewAudio(ocvb)
        If audio.fileinfo.Exists Then
            reader = New MediaFoundationReader(audio.fileinfo.FullName)
            WaveFileWriter.CreateWaveFile("c:\temp\test.wav", reader)
        End If
        ocvb.desc = "Load a .wav or .MP3 file and convert to PCM"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If audio.fileinfo.Exists Then

        End If
    End Sub
End Class