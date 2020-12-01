Imports System.Windows.Forms

Public Class OptionsKeyboardInput
    Public inputText As New List(Of String)
    Dim keyboardLastInput As String
    Public Sub Setup(caller As String)
        Me.MdiParent = aOptions
        Me.Text = caller + " Keyboard Options"
        Me.Location = New System.Drawing.Point(0, 0)
        Me.Show()
        If aOptions.optionsFormTitle.Contains(Me.Text) = False Then
            aOptions.optionsFormTitle.Add(Me.Text)
            aOptions.optionsForms.Add(Me)
        Else
            If aOptions.optionsHidden.Contains(Me.Text) = False Then aOptions.optionsHidden.Add(Me.Text)
        End If
    End Sub
    Private Sub TextBox1_KeyUp(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyUp
        HoldKeyTimer.Enabled = False
        inputText.Add(e.KeyCode.ToString)
    End Sub
    Private Sub TextBox1_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyDown
        keyboardLastInput = e.KeyCode.ToString
        HoldKeyTimer.Enabled = True
    End Sub
    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles keyboardCheckbox.CheckedChanged
        My.Computer.Keyboard.SendKeys(vbTab, True) ' shift focus to the textbox...
    End Sub
    Private Sub TextBox1_LostFocus(sender As Object, e As EventArgs) Handles TextBox1.LostFocus
        keyboardCheckbox.Checked = False
    End Sub
    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        TextBox1.Text = ""
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles HoldKeyTimer.Tick
        inputText.Add(keyboardLastInput) ' press and hold means send this key again...
    End Sub
    Protected Overloads Overrides ReadOnly Property ShowWithoutActivation() As Boolean
        Get
            Return True
        End Get
    End Property
End Class