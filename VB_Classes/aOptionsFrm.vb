Imports cv = OpenCvSharp
Imports System.ComponentModel
Imports System.Windows.Forms
Public Class aOptionsFrm
    Public nextForm = New Drawing.Point
    Public offset = 30
    Private Sub Options_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim defaultLeft = GetSetting("OpenCVB", "OpenCVBLeft", "OpenCVBLeft", Me.Left)
        Dim defaultTop = GetSetting("OpenCVB", "OpenCVBTop", "OpenCVBTop", Me.Top)
        Dim defaultWidth = GetSetting("OpenCVB", "OpenCVBWidth", "OpenCVBWidth", Me.Width)
        Dim defaultHeight = GetSetting("OpenCVB", "OpenCVBHeight", "OpenCVBHeight", Me.Height)

        Me.Left = GetSetting("OpenCVB", "aOptionsLeft", "aOptionsLeft", defaultLeft - offset)
        Me.Top = GetSetting("OpenCVB", "aOptionsTop", "aOptionsTop", defaultTop - offset)
        Me.Width = GetSetting("OpenCVB", "aOptionsWidth", "aOptionsWidth", defaultWidth)
        Me.Height = GetSetting("OpenCVB", "aOptionsHeight", "aOptionsHeight", defaultHeight)
    End Sub
    Private Sub Options_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        SaveSetting("OpenCVB", "aOptionsLeft", "aOptionsLeft", Me.Left)
        SaveSetting("OpenCVB", "aOptionsTop", "aOptionsTop", Me.Top)
        SaveSetting("OpenCVB", "aOptionsWidth", "aOptionsWidth", Me.Width)
        SaveSetting("OpenCVB", "aOptionsHeight", "aOptionsHeight", Me.Height)
    End Sub
    Public Sub layoutOptions(mainLocation As cv.Rect)
        Dim sliderOffset As New cv.Point(0, 0)
        Dim otherOffset As New cv.Point(mainLocation.Width / 2, 0)
        Try
            Dim indexS As Integer = 0
            Dim indexO As Integer = 0
            For Each frm In Application.OpenForms
                If frm.name.startswith("OptionsSliders") Or frm.name.startswith("OptionsKeyboardInput") Or frm.name.startswith("OptionsAlphaBlend") Then
                    If frm.visible Then
                        Try
                            frm.SetDesktopLocation(sliderOffset.X + indexS * offset, sliderOffset.Y + indexS * offset)
                        Catch ex As Exception

                        End Try
                        indexS += 1
                    End If
                End If
                If frm.name.startswith("OptionsRadioButtons") Or frm.name.startswith("OptionsCheckbox") Or frm.name.startswith("OptionsCombo") Then
                    If frm.visible Then
                        frm.SetDesktopLocation(otherOffset.X + indexO * offset, otherOffset.Y + indexO * offset)
                        indexO += 1
                    End If
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("Error in layoutOptions: " + ex.Message)
        End Try
    End Sub
End Class