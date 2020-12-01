Imports System.Windows.Forms
Public Class OptionsRadioButtons
    Public check() As RadioButton
    Public Sub Setup(caller As String, count As Integer)
        Me.MdiParent = aOptions
        ReDim check(count - 1)
        Me.Text = caller + " Radio Options"
        For i = 0 To check.Count - 1
            check(i) = New RadioButton
            check(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(check(i))
        Next
        Me.Location = New System.Drawing.Point(0, 0)
        Me.Show()
        If aOptions.optionsFormTitle.Contains(Me.Text) = False Then
            aOptions.optionsFormTitle.Add(Me.Text)
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
