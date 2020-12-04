Imports System.Windows.Forms
Public Class OptionsRadioButtons
    Public check() As RadioButton
    Public Sub Setup(caller As String, count As Integer)
        Me.MdiParent = aOptions
        ReDim check(count - 1)
        Me.Text = caller + " Radio Options"
        aOptions.addTitle(Me)
        For i = 0 To check.Count - 1
            check(i) = New RadioButton
            check(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(check(i))
        Next
    End Sub
End Class
