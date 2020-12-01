Imports cv = OpenCvSharp
Public Class OptionsCombo
    Public Sub Setup(caller As String, label As String, comboList As List(Of String))
        Me.MdiParent = aOptions
        Me.Text = caller + " ComboBox Options"
        Label1.Text = label
        For i = 0 To comboList.Count - 1
            Box.Items.Add(comboList.ElementAt(i))
        Next
        Box.SelectedIndex = 0
        Me.Location = New System.Drawing.Point(0, 0)
        Me.Show()
        If aOptions.optionsTitle.Contains(Me.Text) = False Then
            aOptions.optionsTitle.Add(Me.Text)
            aOptions.optionsForms.Add(Me)
        Else
            If aOptions.optionsHidden.Contains(Me.Text) = False Then aOptions.optionsHidden.Add(Me.Text)
        End If
    End Sub
    Protected Overloads Overrides ReadOnly Property ShowWithoutActivation() As Boolean
        Get
            Return True
        End Get
    End Property
End Class
