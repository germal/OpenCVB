Imports cv = OpenCvSharp
Imports System.Numerics
' https://github.com/TheAlgorithms/C-Sharp/blob/master/Algorithms/Other/SieveOfEratosthenes.cs'
Public Class Sieve_Basics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Count of desired primes", 1, 1000, 400)

        ocvb.desc = "Implement the Sieve of Eratothenes"
    End Sub
    Public Function shareResults(sieveList As List(Of BigInteger)) As String
        Dim completeList As String = ""
        Dim nextList As String = "   "
        For Each n In sieveList
            nextList += n.ToString + ", "
            If nextList.Length >= 100 Then
                completeList += nextList + vbCrLf
                nextList = "   "
            End If
        Next
        Return completeList + Mid(nextList, 1, If(nextList.Length > 2, Len(nextList) - 2, ""))
    End Function
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Dim count = sliders.trackbar(0).Value
        Dim output = New List(Of BigInteger)()
        Dim nextEntry As BigInteger = 2
        While output.Count < sliders.trackbar(0).Value
            If output.All(Function(x)
                              If nextEntry Mod x <> 0 Then Return True
                              Return False
                          End Function) Then output.Add(nextEntry)
            nextEntry += 1
        End While
        If output.Count > 0 Then ocvb.trueText(shareResults(output))
    End Sub
End Class






Public Class Sieve_Basics_CS
    Inherits VBparent
    Dim printer As Sieve_Basics
    Dim sieve As New CS_Classes.Sieve
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        printer = New Sieve_Basics(ocvb)
        ocvb.desc = "Implement the Sieve of Eratothenes in C#"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.reviewDSTforObject = caller Then ocvb.reviewObject = Me
        Static countSlider = findSlider("Count of desired primes")
        ocvb.trueText(printer.shareResults(sieve.GetPrimeNumbers(countSlider.value)))
    End Sub
End Class