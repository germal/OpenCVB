Imports System.Windows.Forms

Public Class OptionsKeyboardInput
    Public inputText As New List(Of String)
    Dim keyboardLastInput As String
    Public Sub Setup(ocvb As AlgorithmData, caller As String)
        Me.Text = caller + " CheckBox Options"
        Me.Show() ' only the first one   gets to be visible...
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

    Private Sub OptionsKeyboardInput_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(ocvbX.appLocation.X + ocvbX.appLocation.Width / 2 + ocvbX.radioOffset.X, ocvbX.appLocation.Top + ocvbX.appLocation.Height + ocvbX.radioOffset.Y)
        ocvbX.radioOffset.X += offsetIncr
        ocvbX.radioOffset.Y += offsetIncr
        If ocvbX.radioOffset.X > offsetMax Then ocvbX.radioOffset.X = 0
        If ocvbX.radioOffset.Y > offsetMax Then ocvbX.radioOffset.Y = 0
    End Sub
End Class