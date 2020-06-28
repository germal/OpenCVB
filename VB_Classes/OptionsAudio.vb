Imports System.IO
Imports System.Windows.Forms
Imports NAudio.Wave
Public Class OptionsAudio
    Public fileinfo As FileInfo
    Dim player As IWavePlayer
    Dim reader As MediaFoundationReader
    Private Sub OptionsAudio_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(applocation.Left + slidersOffset.X, applocation.Top + applocation.Height + slidersOffset.Y)
        slidersOffset.X += offsetIncr
        slidersOffset.Y += offsetIncr
    End Sub
    Public Sub NewAudio(ocvb As AlgorithmData)
        Dim inputfile = GetSetting("OpenCVB", "AudioFileName", "AudioFileName", "")
        If inputfile = "" Then inputfile = CurDir() + "/../../Data/01 I Let the Music Speak.m4a"
        fileinfo = New FileInfo(inputfile)
        Filename.Text = fileinfo.FullName
        If ocvb.parms.ShowOptions Then Me.Show()
    End Sub
    Private Sub Filename_TextChanged(sender As Object, e As EventArgs) Handles Filename.TextChanged
        fileinfo = New FileInfo(Filename.Text)
        SaveSetting("OpenCVB", "AudioFileName", "AudioFileName", fileinfo.FullName)
    End Sub

    Private Sub PlayButton_Click(sender As Object, e As EventArgs) Handles PlayButton.Click
        If PlayButton.Text = "Play" Then
            Dim reader = New MediaFoundationReader(fileinfo.FullName)
            player = New WaveOutEvent()
            player.Init(reader)
            player.Play()
            PlayButton.Text = "Stop"
        Else
            player.Stop()
            player.Dispose()
            PlayButton.Text = "Play"
        End If
    End Sub
End Class