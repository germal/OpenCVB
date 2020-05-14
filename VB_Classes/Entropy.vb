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
            Dim hc = Math.Abs(hist.Get(Of Single)(i))
            If hc <> 0 Then entropy += -(hc / total) * Math.Log10(hc / total)
        Next
        Return entropy
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Or src.Width = 0 Then src = ocvb.color
        hist.src = src
        hist.Run(ocvb)
        entropy = 0
        Dim entropyChannels As String = ""
        For i = 0 To src.Channels - 1
            Dim nextEntropy = channelEntropy(src.Total, hist.histNormalized(i))
            entropyChannels += "Entropy for " + Choose(i + 1, "Red", "Green", "Blue") + " " + Format(nextEntropy, "0.00") + ", "
            entropy += nextEntropy
        Next
        If standalone Then
            flow.msgs.Add("Entropy total = " + Format(entropy, "0.00") + " - " + entropyChannels)
            flow.Run(ocvb)
        End If
    End Sub
End Class






Public Class Entropy_Highest_MT
    Inherits ocvbClass
    Dim entropies(0) As Entropy_Basics
    Public grid As Thread_Grid
    Public bestContrast As cv.Rect
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 64
        sliders.Visible = False ' troublesome memory problem is reallocated...
        label1 = "Red rectangle show the location of the highest entropy"
        ocvb.desc = "Find the highest entropy section of the color image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then src = ocvb.color
        grid.Run(ocvb)

        If entropies.Length <> grid.roiList.Count Then
            For i = 0 To entropies.Count - 1
                If entropies(i) IsNot Nothing Then entropies(i).Dispose()
            Next
            ocvb.suppressOptions = True
                ReDim entropies(grid.roiList.Count - 1)
            For i = 0 To entropies.Length - 1
                entropies(i) = New Entropy_Basics(ocvb, caller)
            Next
        End If

        Parallel.For(0, grid.roiList.Count - 1,
         Sub(i)
             entropies(i).src = src(grid.roiList(i))
             entropies(i).Run(ocvb)
         End Sub)

        Dim maxEntropy As Single
        Dim maxIndex As Int32
        For i = 0 To entropies.Count - 1
            If entropies(i).entropy > maxEntropy Then
                maxEntropy = entropies(i).entropy
                maxIndex = i
            End If
        Next

        bestContrast = grid.roiList(maxIndex)
        If standalone Then
            dst1 = src
            dst1.Rectangle(bestContrast, cv.Scalar.Red, 2)
        End If
    End Sub
End Class