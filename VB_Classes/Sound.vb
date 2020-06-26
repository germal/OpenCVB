Imports cv = OpenCvSharp
Imports Un4seen.Bass
Public Class Sound_ToPCM
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Load a .wav or .MP3 file and convert to PCM"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
    End Sub
End Class