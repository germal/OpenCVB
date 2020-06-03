Imports cv = OpenCvSharp
Public Class OptionsCombo
    Private Sub OptionsFilename_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(midFormX + radioOffset.X, appLocation.Top + appLocation.Height + radioOffset.Y)
        radioOffset.X += offsetIncr
        radioOffset.Y += offsetIncr
        If radioOffset.X > offsetMax Then radioOffset.X = 0
        If radioOffset.Y > offsetMax Then radioOffset.Y = 0
    End Sub
    Public Sub Setup(ocvb As AlgorithmData, caller As String, label As String, comboList As List(Of String))
        Me.Text = caller + " Options"
        If ocvb.parms.ShowOptions Then
            If ocvb.suppressOptions = False Then Me.Show()
        End If
        Label1.Text = label
        For i = 0 To comboList.Count - 1
            Box.Items.Add(comboList.ElementAt(i))
        Next
        Box.SelectedIndex = 0
    End Sub
End Class
