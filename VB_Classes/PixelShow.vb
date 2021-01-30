Imports cv = OpenCvSharp
Imports System.Windows.Forms
Imports System.Drawing
Public Class PixelViewer
    Private Sub PixelShow_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim defaultSize = GetSetting("OpenCVB", "FontSize", "FontSize", 8)
        Dim DefaultFont = GetSetting("OpenCVB", "FontName", "FontName", "Tahoma")
        FontInfo.Font = New Font(DefaultFont, defaultSize)

    End Sub

    Private Sub PixelShow_Paint(sender As Object, e As PaintEventArgs) Handles Me.Paint
        Dim g As Graphics = e.Graphics
        g.DrawString("Testing", FontInfo.Font, New SolidBrush(Color.Black), 10, 10)
    End Sub
End Class