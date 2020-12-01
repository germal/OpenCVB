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
        If lookupAlgorithm(caller) = 1 Then Me.Show() ' only the first one gets to be visible...
    End Sub
    Private Function lookupAlgorithm(caller As String) As Integer
        For i = 0 To callerNames.Length - 1
            If callerNames(i) = caller Then
                callerCheckBoxCounts(i) += 1
                Return callerCheckBoxCounts(i)
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
