Imports System.IO
Imports System.Windows.Forms

Public Class OptionsRecordPlayback
    Public startRecordPlayback As Boolean
    Public fileinfo As FileInfo
    Public Sub Button2_Click(sender As Object, e As EventArgs) Handles startButton.Click
        If startRecordPlayback = False Then
            startRecordPlayback = True
            If InStr(startButton.Text, "Recording") Then
                startButton.Text = "Stop Recording"
            Else
                startButton.Text = "Stop Playback"
            End If
        Else
            startRecordPlayback = False
            If InStr(startButton.Text, "Recording") Then
                startButton.Text = "Start Recording"
            Else
                startButton.Text = "Start Playback"
            End If
        End If
    End Sub
    Private Sub OptionsKinectRecord_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim inputfile = GetSetting("OpenCVB", "ReplayFileName", "ReplayFileName", "")
        If inputfile = "" Then inputfile = CurDir() + "/../../Data/Recording.bob"
        fileinfo = New FileInfo(inputfile)

        Filename.Text = fileinfo.FullName
        Me.SetDesktopLocation(appLocation.Left + slidersOffset.X, appLocation.Top + appLocation.Height + slidersOffset.Y)
        slidersOffset.X += offsetIncr
        slidersOffset.Y += offsetIncr
    End Sub
    Private Sub OptionsKinectRecord_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        startRecordPlayback = False
    End Sub
    Private Sub Filename_TextChanged(sender As Object, e As EventArgs) Handles Filename.TextChanged
        fileinfo = New FileInfo(Filename.Text)
        SaveSetting("OpenCVB", "ReplayFileName", "ReplayFileName", fileinfo.FullName)
    End Sub
End Class