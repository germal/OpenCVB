Public Class OptionsFont
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        FontDialog1.ShowColor = False
        FontDialog1.ShowApply = False
        FontDialog1.ShowEffects = False
        FontDialog1.ShowHelp = True

        FontDialog1.MaxSize = 40
        FontDialog1.MinSize = 5

        If FontDialog1.ShowDialog() = Windows.Forms.DialogResult.OK Then
            Label1.Font = FontDialog1.Font
            Label1.Text = FontDialog1.Font.Name + " with size = " + CStr(Label1.Font.Size)
            SaveSetting("OpenCVB", "FontName", "FontName", Label1.Font.Name)
            SaveSetting("OpenCVB", "FontSize", "FontSize", Label1.Font.Size)
        End If
    End Sub

    Private Sub OptionsFont_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim defaultSize = GetSetting("OpenCVB", "FontSize", "FontSize", 12)
        Dim DefaultFont = GetSetting("OpenCVB", "FontName", "FontName", "Tahoma")
        Label1.Font = New Drawing.Font(DefaultFont, defaultSize)
        Label1.Text = DefaultFont + " with size = " + CStr(defaultSize)
        Me.SetDesktopLocation(appLocation.Left + slidersOffset.X, appLocation.Top + appLocation.Height + slidersOffset.Y)
        slidersOffset.X += offsetIncr
        slidersOffset.Y += offsetIncr
    End Sub
End Class
