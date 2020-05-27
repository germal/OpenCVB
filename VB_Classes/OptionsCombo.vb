Imports cv = OpenCvSharp
Public Class OptionsCombo
    Private Sub OptionsFilename_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(appLocation.Left + Me.Width + slidersOffset.X, appLocation.Top + appLocation.Height + slidersOffset.Y)
    End Sub
    Public Sub Setup(ocvb As AlgorithmData, caller As String, label As String, comboList As List(Of String))
        Me.Text = caller + " Options"
        If ocvb.parms.ShowOptions Then
            If ocvb.suppressOptions = False Then
                radioOffset.X += offsetIncr
                radioOffset.Y += offsetIncr
                If radioOffset.X > 100 Then radioOffset.X = 0
                If radioOffset.Y > 100 Then radioOffset.Y = 0
                Me.Show()
            End If
        End If
        Label1.Text = label
        For i = 0 To comboList.Count - 1
            ComboBox.Items.Add(comboList.ElementAt(i))
        Next
        ComboBox.SelectedIndex = 0
    End Sub
End Class
