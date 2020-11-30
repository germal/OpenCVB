Imports cv = OpenCvSharp
Public Class OptionsCombo
    Public Sub Setup(caller As String, label As String, comboList As List(Of String))
        Me.Text = caller + " ComboBox Options"
        Me.Show()
        Label1.Text = label
        For i = 0 To comboList.Count - 1
            Box.Items.Add(comboList.ElementAt(i))
        Next
        Box.SelectedIndex = 0
        Me.Show()
    End Sub
    Protected Overloads Overrides ReadOnly Property ShowWithoutActivation() As Boolean
        Get
            Return True
        End Get
    End Property
End Class
