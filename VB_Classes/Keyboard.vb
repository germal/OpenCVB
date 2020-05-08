
Imports cv = OpenCvSharp
Public Class Keyboard_Basics
    Inherits ocvbClass
    Public input As String
        Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.label1 = "Type in text to add to image"
        ocvb.desc = "Test the keyboard interface available to all algorithms"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.keyboardInput <> "" Then
            input = ocvb.parms.keyboardInput
        End If
        if standalone Then
            If input = "" Then
                ocvb.putText(New ActiveClass.TrueType("Any text entered will appear here." + input, 10, 50, RESULT1))
            Else
                ocvb.putText(New ActiveClass.TrueType("The last key that was hit was: " + input, 10, 50, RESULT1))
            End If
        End If
        ocvb.parms.keyInputAccepted = True
    End Sub
End Class