Imports cv = OpenCvSharp
Imports System.Windows.Forms
Imports System.Drawing
Imports System.ComponentModel

Public Class PixelViewerForm
    Public line As String
    Public pixelResized As Boolean
    Public pixelDataChanged As Boolean
    Public mousePoint As cv.Point
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
    Private Sub PixelViewerForm_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        pixelResized = True
        SaveSetting("OpenCVB", "PixelViewerLeft", "PixelViewerLeft", Me.Left)
        SaveSetting("OpenCVB", "PixelViewerTop", "PixelViewerTop", Me.Top)
        SaveSetting("OpenCVB", "PixelViewerWidth", "PixelViewerWidth", Me.Width)
        SaveSetting("OpenCVB", "PixelViewerHeight", "PixelViewerHeight", Me.Height)
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If pixelDataChanged Then Me.Refresh()
        pixelDataChanged = False
    End Sub
    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        mousePoint.X -= 1
    End Sub
    Private Sub ToolStripButton2_Click(sender As Object, e As EventArgs) Handles ToolStripButton2.Click
        mousePoint.Y -= 1
    End Sub
    Private Sub ToolStripButton3_Click(sender As Object, e As EventArgs) Handles ToolStripButton3.Click
        mousePoint.Y += 1
    End Sub
    Private Sub ToolStripButton4_Click(sender As Object, e As EventArgs) Handles ToolStripButton4.Click
        mousePoint.X += 1
    End Sub
End Class