Imports System.Windows.Forms
Public Class OptionsRadioButtons
    Public check() As RadioButton
    Public Sub Setup(ocvb As AlgorithmData, caller As String, count As Int32)
        ReDim check(count - 1)
        Me.Text = caller + " Options"
        For i = 0 To check.Count - 1
            check(i) = New RadioButton
            check(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(check(i))
        Next
        If ocvb.parms.ShowOptions Then
            If ocvb.suppressOptions = False Then
                radioOffset.X += offsetIncr
                radioOffset.Y += offsetIncr
                If radioOffset.X > 100 Then radioOffset.X = 0
                If radioOffset.Y > 100 Then radioOffset.Y = 0
                Me.Show()
            End If
        End If
    End Sub
    Private Sub OptionsRadioButtons_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(appLocation.Left + Me.Width + radioOffset.X, appLocation.Top + appLocation.Height + radioOffset.Y)
    End Sub
End Class
