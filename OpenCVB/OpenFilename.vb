Imports System.Windows.Forms
Public Class OpenFilename
    Public playStarted As Boolean
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then
            filename.Text = OpenFileDialog1.FileName
        End If
    End Sub

    Private Sub PlayButton_Click(sender As Object, e As EventArgs) Handles PlayButton.Click
        If PlayButton.Text = "Play" Then
            PlayButton.Text = "Stop"
            playStarted = True
        Else
            PlayButton.Text = "Play"
            playStarted = False
        End If
    End Sub
End Class
