Imports System.Windows.Forms
Public Class OptionsCheckbox
    Public Box() As CheckBox
    Private Sub OptionsCheckbox_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(appLocation.Left + Me.Width + radioOffset.X, appLocation.Top + appLocation.Height + radioOffset.Y)
        radioOffset.X += offsetIncr
        radioOffset.Y += offsetIncr
    End Sub
    Public Sub Setup(ocvb As AlgorithmData, count As Int32)
        ReDim Box(count - 1)
        Me.Text = ocvb.caller + " Options"
        For i = 0 To Box.Count - 1
            Box(i) = New CheckBox
            Box(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(Box(i))
        Next
    End Sub
End Class
