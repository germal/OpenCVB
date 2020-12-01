Imports System.Windows.Forms
Public Class OptionsCheckbox
    Public Box() As CheckBox
    Public Sub Setup(caller As String, count As Integer)
        Me.MdiParent = aOptions
        ReDim Box(count - 1)
        Me.Text = caller + " CheckBox Options"
        For i = 0 To Box.Count - 1
            Box(i) = New CheckBox
            Box(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(Box(i))
        Next
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
