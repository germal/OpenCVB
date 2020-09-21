Imports System.Windows.Forms
Public Class OptionsRadioButtons
    Public check() As RadioButton
    Public Sub Setup(ocvb As VBocvb, caller As String, count As integer)
        ReDim check(count - 1)
        Me.Text = caller + " Radio Options"
        For i = 0 To check.Count - 1
            check(i) = New RadioButton
            check(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(check(i))
        Next
        Me.Show()
    End Sub
    Protected Overloads Overrides ReadOnly Property ShowWithoutActivation() As Boolean
        Get
            Return True
        End Get
    End Property
    Private Sub OptionsRadioButtons_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(appLocation.X + appLocation.Width / 2 + radioOffset.X, appLocation.Top + appLocation.Height + radioOffset.Y)
        radioOffset.X += offsetIncr
        radioOffset.Y += offsetIncr
        If radioOffset.X > offsetMax Then radioOffset.X = 0
        If radioOffset.Y > offsetMax Then radioOffset.Y = 0
    End Sub
End Class
