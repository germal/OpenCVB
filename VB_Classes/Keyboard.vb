
Imports cv = OpenCvSharp
Public Class Keyboard_Basics
    Inherits VBparent
    Public keyInput As New List(Of String)
    Dim flow As Font_FlowText
    Public checkKeys As New OptionsKeyboardInput
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        checkKeys.Setup(ocvb, caller)
        flow = New Font_FlowText(ocvb)
        label1 = "Keyboard data will flow to algorithm"
        ocvb.desc = "Test the keyboard interface available to all algorithms"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        keyInput = New List(Of String)(checkKeys.inputText)
        checkKeys.inputText.Clear()
        If standalone Then
            Dim inputText As String = ""
            For i = 0 To keyInput.Count - 1
                inputText += keyInput(i).ToString()
            Next
            If inputText <> "" Then flow.msgs.Add(inputText)
            flow.Run(ocvb)
        End If
    End Sub
End Class

