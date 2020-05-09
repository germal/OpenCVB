Imports cv = OpenCvSharp
' http://areshopencv.blogspot.com/2011/12/computing-entropy-of-image.html
Public Class Entropy_Basics
    Inherits ocvbClass
    Dim flow As Font_FlowText
    Dim hist As Histogram_Basics
    Public entropy As Single
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        flow = New Font_FlowText(ocvb, caller)
        flow.result1or2 = RESULT1

        hist = New Histogram_Basics(ocvb, caller)

        ocvb.desc = "Compute the entropy in an image - a measure of contrast(iness)"
    End Sub
    Private Function channelEntropy(total As Int32, hist As cv.Mat) As Single
        Dim entropy As Single
        For i = 0 To hist.Rows - 1
            Dim hc = Math.Abs(hist.Get(of Single)(i))
            If hc <> 0 Then entropy += -(hc / total) * Math.Log10(hc / total)
        Next
        Return entropy
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        if standalone Then src = ocvb.color
        hist.src = src
        hist.Run(ocvb)
        entropy = 0
        Dim entropyChannels As String = ""
        For i = 0 To src.Channels - 1
            Dim nextEntropy = channelEntropy(src.Total, hist.histNormalized(i))
            entropyChannels += "Entropy for " + Choose(i + 1, "Red", "Green", "Blue") + " " + Format(nextEntropy, "0.00") + ", "
            entropy += nextEntropy
        Next
        if standalone Then
            flow.msgs.Add("Entropy total = " + Format(entropy, "0.00") + " - " + entropyChannels)
            flow.Run(ocvb)
        End If
    End Sub
    Public Sub MyDispose()
        hist.Dispose()
        flow.Dispose()
    End Sub
End Class