Imports System.Windows.Forms
Public Class OptionsRadioButtons
    Public check() As RadioButton
    Public Sub Setup(ocvb As AlgorithmData, caller As String, count As Int32)
        ReDim check(count - 1)
        Me.Text = caller + " Radio Options"
        For i = 0 To check.Count - 1
            check(i) = New RadioButton
            check(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(check(i))
        Next
        Me.Show()
    End Sub

    Private Sub OptionsRadioButtons_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(midFormX + ocvbX.radioOffset.X, applocation.Top + applocation.Height + ocvbX.radioOffset.Y)
        ocvbX.radioOffset.X += offsetIncr
        ocvbX.radioOffset.Y += offsetIncr
        If ocvbX.radioOffset.X > offsetMax Then ocvbX.radioOffset.X = 0
        If ocvbX.radioOffset.Y > offsetMax Then ocvbX.radioOffset.Y = 0
    End Sub
End Class
