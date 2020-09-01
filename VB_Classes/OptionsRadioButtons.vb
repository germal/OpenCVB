Imports System.Windows.Forms
Public Class OptionsRadioButtons
    Public check() As RadioButton
    Public Sub Setup(ocvb As AlgorithmData, caller As String, count As Int32)
        Me.SetDesktopLocation(midFormX + ocvb.radioOffset.X, applocation.Top + applocation.Height + ocvb.radioOffset.Y)
        ocvb.radioOffset.X += offsetIncr
        ocvb.radioOffset.Y += offsetIncr
        If ocvb.radioOffset.X > offsetMax Then ocvb.radioOffset.X = 0
        If ocvb.radioOffset.Y > offsetMax Then ocvb.radioOffset.Y = 0

        ReDim check(count - 1)
        Me.Text = caller + " Radio Options"
        For i = 0 To check.Count - 1
            check(i) = New RadioButton
            check(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(check(i))
        Next
        Me.Show()
    End Sub
End Class
