Imports cv = OpenCvSharp
' https://www.codeproject.com/Articles/5280034/Generation-of-Infinite-Sequences-in-Csharp-and-Unm
Public Class Fibonacci_Basics
    Inherits VBparent
    Dim flow As Font_FlowText
    Public Sub New()
        initParent()
        flow = New Font_FlowText()
        ocvb.desc = "Generate the fibonacci sequence using conventional code"
    End Sub
    Public Sub Run()
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static a As Double = 0, b As Double = 1
        If a = 1134903170 Then
            a = 0
            b = 1
        End If
        Dim t = a + b
        a = b
        b = t
        flow.msgs.Add(t.ToString)
        flow.Run()
    End Sub
End Class






' https://www.codeproject.com/Articles/5280034/Generation-of-Infinite-Sequences-in-Csharp-and-Unm
Public Class Fibonacci_Yield
    Inherits VBparent
    Dim flow As Font_FlowText
    Public Sub New()
        initParent()
        flow = New Font_FlowText()
        ocvb.desc = "Generate the fibonacci sequence using ienumerable's"
    End Sub
    Private Iterator Function nextFib() As System.Collections.Generic.IEnumerable(Of Double)
        Dim a As Double = 0
        Dim b As Double = 1
        Dim t As Double
        While (1)
            Yield a
            t = a + b
            If a = 806515533049393 Then ' start to lose precision after this...
                a = 0
                b = 1
                t = a + b
            End If
            a = b
            b = t
        End While
    End Function
    Public Sub Run()
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim fibs As System.Collections.Generic.IEnumerable(Of Double) = nextFib()
        flow.msgs.Add(Format(ocvb.frameCount Mod 74, "00") + " fibonacci number " + Format(fibs.ElementAt(ocvb.frameCount), "###,##0"))
        flow.Run()
    End Sub
End Class
