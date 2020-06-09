Imports System.Windows.Forms
Public Class OptionsCheckbox
    Public Box() As CheckBox
    Public Sub Setup(ocvb As AlgorithmData, caller As String, count As Int32)
        ReDim Box(count - 1)
        Me.Text = caller + " Options"
        For i = 0 To Box.Count - 1
            Box(i) = New CheckBox
            Box(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(Box(i))
        Next
        If ocvb.parms.ShowOptions Then If ocvb.suppressOptions = False Then Me.Show()
    End Sub
    Private Sub OptionsCheckbox_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(midFormX + radioOffset.X, appLocation.Top + appLocation.Height + radioOffset.Y)
        radioOffset.X += offsetIncr
        radioOffset.Y += offsetIncr
        If radioOffset.X > offsetMax Then radioOffset.X = 0
        If radioOffset.Y > offsetMax Then radioOffset.Y = 0
    End Sub
End Class
