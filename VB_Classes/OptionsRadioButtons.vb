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
        If ocvb.suppressOptions = False Then Me.Show()
        Tag = ocvb.parms.activeThreadID
    End Sub
    Private Sub OptionsRadioButtons_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(midFormX + radioOffset.X, appLocation.Top + appLocation.Height + radioOffset.Y)
        radioOffset.X += offsetIncr
        radioOffset.Y += offsetIncr
        If radioOffset.X > offsetMax Then radioOffset.X = 0
        If radioOffset.Y > offsetMax Then radioOffset.Y = 0
    End Sub
End Class
