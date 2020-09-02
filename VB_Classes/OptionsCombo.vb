Imports cv = OpenCvSharp
Public Class OptionsCombo
    Public Sub Setup(ocvb As VBocvb, caller As String, label As String, comboList As List(Of String))
        Me.Text = caller + " ComboBox Options"
        Me.Show()
        Label1.Text = label
        For i = 0 To comboList.Count - 1
            Box.Items.Add(comboList.ElementAt(i))
        Next
        Box.SelectedIndex = 0
    End Sub

    Private Sub OptionsCombo_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(ocvbX.radioOffset.X, ocvbX.radioOffset.Y)
        ocvbX.radioOffset.X += offsetIncr
        ocvbX.radioOffset.Y += offsetIncr
        If ocvbX.radioOffset.X > offsetMax Then ocvbX.radioOffset.X = 0
        If ocvbX.radioOffset.Y > offsetMax Then ocvbX.radioOffset.Y = 0
    End Sub
End Class
