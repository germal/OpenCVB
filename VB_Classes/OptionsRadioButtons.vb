Imports System.Windows.Forms
Public Class OptionsRadioButtons
    Public check() As RadioButton
    Private Sub optionsSliders_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(appLocation.Left + Me.Width + radioOffset.X, appLocation.Top + appLocation.Height + radioOffset.Y)
        radioOffset.X += offsetIncr
        radioOffset.Y += offsetIncr
    End Sub
    Public Sub Setup(ocvb As AlgorithmData, count As Int32)
        ReDim check(count - 1)
        Me.Text = ocvb.name + " Options"
        For i = 0 To check.Count - 1
            check(i) = New RadioButton
            check(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(check(i))
        Next
    End Sub
End Class
