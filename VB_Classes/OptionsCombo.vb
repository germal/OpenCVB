Imports cv = OpenCvSharp
Public Class OptionsCombo
    Private Sub OptionsFilename_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(appLocation.Left + slidersOffset.X, appLocation.Top + appLocation.Height + slidersOffset.Y)
    End Sub
    Public Sub Setup(ocvb As AlgorithmData, caller As String, label As String, comboList As List(Of String))
        Me.Text = caller + " Options"
        If ocvb.parms.ShowOptions Then
            If ocvb.suppressOptions = False Then
                slidersOffset.X += offsetIncr
                slidersOffset.Y += offsetIncr
                If slidersOffset.X > 100 Then slidersOffset.X = 0
                If slidersOffset.Y > 100 Then slidersOffset.Y = 0
                Me.Show()
            End If
        End If
        Label1.Text = label
        For i = 0 To comboList.Count - 1
            Box.Items.Add(comboList.ElementAt(i))
        Next
        Box.SelectedIndex = 0
    End Sub
End Class
