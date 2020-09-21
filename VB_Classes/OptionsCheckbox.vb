Imports System.Windows.Forms
Public Class OptionsCheckbox
    Public Box() As CheckBox
    Public Sub Setup(ocvb As VBocvb, caller As String, count As integer)
        ReDim Box(count - 1)
        Me.Text = caller + " CheckBox Options"
        For i = 0 To Box.Count - 1
            Box(i) = New CheckBox
            Box(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(Box(i))
        Next
        If lookupAlgorithm(caller) = 1 Then Me.Show() ' only the first one gets to be visible...
    End Sub
    Private Function lookupAlgorithm(caller As String) As Integer
        For i = 0 To callerNames.Length - 1
            If callerNames(i) = caller Then
                callerCheckBoxCounts(i) += 1
                Return callerCheckBoxCounts(i)
            End If
        Next
        Return 0
    End Function
    Protected Overloads Overrides ReadOnly Property ShowWithoutActivation() As Boolean
        Get
            Return True
        End Get
    End Property
    Private Sub OptionsCheckbox_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(appLocation.X + appLocation.Width / 2 + radioOffset.X, appLocation.Top + appLocation.Height + radioOffset.Y)
        radioOffset.X += offsetIncr
        radioOffset.Y += offsetIncr
        If radioOffset.X > offsetMax Then radioOffset.X = 0
        If radioOffset.Y > offsetMax Then radioOffset.Y = 0
    End Sub
End Class
