Imports System.IO
Imports System.Windows.Forms
Public Class OptionsAudio
    Public fileinfo As FileInfo
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
        SaveSetting("OpenCVB", "ReplayFileName", "ReplayFileName", fileinfo.FullName)
    End Sub
End Class