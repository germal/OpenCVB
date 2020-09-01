Imports cv = OpenCvSharp
Public Class OptionsCombo
    Public Sub Setup(ocvb As AlgorithmData, caller As String, label As String, comboList As List(Of String))
        Me.SetDesktopLocation(ocvb.radioOffset.X, ocvb.radioOffset.Y)
        ocvb.radioOffset.X += offsetIncr
        ocvb.radioOffset.Y += offsetIncr
        If ocvb.radioOffset.X > offsetMax Then ocvb.radioOffset.X = 0
        If ocvb.radioOffset.Y > offsetMax Then ocvb.radioOffset.Y = 0

        Me.Text = caller + " ComboBox Options"
        Me.Show()
        Label1.Text = label
        For i = 0 To comboList.Count - 1
            Box.Items.Add(comboList.ElementAt(i))
        Next
        Box.SelectedIndex = 0
    End Sub
End Class
