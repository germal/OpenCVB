Imports System.Windows.Forms
Public Class OptionsCheckbox
    Public Box() As CheckBox
    Public Sub Setup(ocvb As AlgorithmData, caller As String, count As Int32)
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
    Private Sub OptionsCheckbox_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(ocvbX.appLocation.X + ocvbX.appLocation.Width / 2 + ocvbX.radioOffset.X, ocvbX.appLocation.Top + ocvbX.appLocation.Height + ocvbX.radioOffset.Y)
        ocvbX.radioOffset.X += offsetIncr
        ocvbX.radioOffset.Y += offsetIncr
        If ocvbX.radioOffset.X > offsetMax Then ocvbX.radioOffset.X = 0
        If ocvbX.radioOffset.Y > offsetMax Then ocvbX.radioOffset.Y = 0
    End Sub
End Class
