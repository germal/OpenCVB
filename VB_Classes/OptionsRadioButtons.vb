Imports System.Windows.Forms
Public Class OptionsRadioButtons
    Public check() As RadioButton
    Public Sub Setup(ocvb As VBocvb, caller As String, count As Integer)
        ReDim check(count - 1)
        Me.Text = caller + " Radio Options"
        For i = 0 To check.Count - 1
            check(i) = New RadioButton
            check(i).AutoSize = True
            FlowLayoutPanel1.Controls.Add(check(i))
        Next
        If lookupAlgorithm(caller) = 1 Then Me.Show() ' only the first one gets to be visible...
    End Sub
    Private Function lookupAlgorithm(caller As String) As Integer
        For i = 0 To callerNames.Length - 1
            If callerNames(i) = caller Then
                callerRadioCounts(i) += 1
                Return callerRadioCounts(i)
            End If
        Next
        Return 0
    End Function
    Protected Overloads Overrides ReadOnly Property ShowWithoutActivation() As Boolean
        Get
            Return True
        End Get
    End Property
End Class
