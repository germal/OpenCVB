Imports cv = OpenCvSharp
Imports System.Windows.Forms
Imports System.Drawing
Imports System.ComponentModel

Public Class PixelViewer
    Public line As String
    Private Sub PixelShow_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim defaultSize = GetSetting("OpenCVB", "FontSize", "FontSize", 8)
        Dim DefaultFont = GetSetting("OpenCVB", "FontName", "FontName", "Tahoma")
        FontInfo.Font = New Font(DefaultFont, defaultSize)

        Me.Left = GetSetting("OpenCVB", "PixelViewerLeft", "PixelViewerLeft", Me.Left)
        Me.Top = GetSetting("OpenCVB", "PixelViewerTop", "PixelViewerTop", Me.Top)

        Dim goodPoint = Screen.GetWorkingArea(New Point(Me.Left, Me.Top)) ' when they change the main screen, old coordinates can go way off the screen.
        If goodPoint.X > Me.Left Then Me.Left = goodPoint.X
        If goodPoint.Y > Me.Top Then Me.Top = goodPoint.Y

        Me.Width = GetSetting("OpenCVB", "PixelViewerWidth", "PixelViewerWidth", 1280)
        Me.Height = GetSetting("OpenCVB", "PixelViewerHeight", "PixelViewerHeight", 720)

    End Sub

    Private Sub PixelShow_Paint(sender As Object, e As PaintEventArgs) Handles Me.Paint
        Dim g As Graphics = e.Graphics
        g.DrawString(line, FontInfo.Font, New SolidBrush(Color.Black), 10, 10)
    End Sub

    Private Sub PixelViewer_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        SaveSetting("OpenCVB", "PixelViewerLeft", "PixelViewerLeft", Me.Left)
        SaveSetting("OpenCVB", "PixelViewerTop", "PixelViewerTop", Me.Top)
        SaveSetting("OpenCVB", "PixelViewerWidth", "PixelViewerWidth", Me.Width)
        SaveSetting("OpenCVB", "PixelViewerHeight", "PixelViewerHeight", Me.Height)
    End Sub
End Class