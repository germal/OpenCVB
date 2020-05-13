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
        If ocvb.parms.ShowOptions Then
            If ocvb.suppressOptions = False Then
                radioOffset.X += offsetIncr
                radioOffset.Y += offsetIncr
                Me.Show()
            End If
        End If
    End Sub
    Private Sub OptionsCheckbox_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(appLocation.Left + Me.Width + radioOffset.X, appLocation.Top + appLocation.Height + radioOffset.Y)
    End Sub
End Class
