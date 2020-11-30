Imports System.ComponentModel

Public Class aOptionsFrm
    Public check As New OptionsCheckbox
    Public combo As New OptionsCombo
    Public radio As New OptionsRadioButtons
    Public radio1 As New OptionsRadioButtons
    Public sliders As New OptionsSliders
    Public nextForm = New Drawing.Point
    Public offset = 30
    Private Sub Options_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim defaultLeft = GetSetting("OpenCVB", "OpenCVBLeft", "OpenCVBLeft", Me.Left)
        Dim defaultTop = GetSetting("OpenCVB", "OpenCVBTop", "OpenCVBTop", Me.Top)

        Me.Left = GetSetting("OpenCVB", "aOptionsLeft", "aOptionsLeft", defaultLeft - 30)
        Me.Top = GetSetting("OpenCVB", "aOptionsTop", "aOptionsTop", defaultTop - 30)
        'Me.Width = GetSetting("OpenCVB", "aOptionsLeft", "aOptionsLeft", defaultLeft - 30)
        'Me.Height = GetSetting("OpenCVB", "aOptionsTop", "aOptionsTop", defaultTop - 30)
    End Sub
    Public Sub setup()
        Me.Width = ocvb.mainLocation.Width
        Me.Height = ocvb.mainLocation.Height
        combo.MdiParent = Me
        radio.MdiParent = Me
        radio1.MdiParent = Me
        sliders.MdiParent = Me
        check.MdiParent = Me
    End Sub
    Private Sub Options_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        'SaveSetting("OpenCVB", "aOptionsLeft", "aOptionsLeft", Me.Left)
        'SaveSetting("OpenCVB", "aOptionsTop", "aOptionsTop", Me.Top)
        'SaveSetting("OpenCVB", "aOptionsWidth", "aOptionsWidth", Me.Width)
        'SaveSetting("OpenCVB", "aOptionsHeight", "aOptionsHeight", Me.Height)

        sliders.Dispose()
        check.Dispose()
        radio.Dispose()
        radio1.Dispose()
        combo.Dispose()
    End Sub
End Class