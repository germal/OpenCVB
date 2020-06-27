Imports cv = OpenCvSharp
Public Class Sound_ToPCM
    Inherits ocvbClass
    Dim audio As New OptionsAudio
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        If ocvb.parms.ShowOptions Then audio.Show()
        ocvb.desc = "Load a .wav or .MP3 file and convert to PCM"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
    End Sub
End Class