Imports cv = OpenCvSharp
Imports System.ComponentModel
Imports System.Windows.Forms
Public Class aOptionsFrm
    Public optionsFormTitle As New List(Of String)
    Public optionsForms As New List(Of System.Windows.Forms.Form)
    Public optionsHidden As New List(Of String)
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
    Public Function findRealForm(title As String) As Windows.Forms.Form
        For Each frm In Application.OpenForms
            If frm.text = title Then Return frm
        Next
        Return Nothing
    End Function
    Public Sub layoutOptions()
        Me.Show()
        Application.DoEvents()
        Dim sliderOffset As New cv.Point(0, 0)
        Dim otherOffset As New cv.Point(Me.Width / 2, 0)
        For Each title In optionsHidden
            Dim frm = findRealForm(title)
            While frm IsNot Nothing
                frm.Hide()
                frm = findRealForm(title)
            End While
        Next

        Try
            Dim indexS As Integer = 0
            Dim indexO As Integer = 0
            For Each title In optionsFormTitle
                If aOptions.optionsHidden.Contains(title) Then Continue For
                If title.EndsWith(" Slider Options") Or title.EndsWith(" Keyboard Options") Or title.EndsWith("OptionsAlphaBlend") Then
                    Dim frm = findRealForm(title)
                    frm.SetDesktopLocation(sliderOffset.X + indexS * offset, sliderOffset.Y + indexS * offset)
                    indexS += 1
                End If
                If title.EndsWith(" Radio Options") Or title.EndsWith(" CheckBox Options") Then
                    Dim frm = findRealForm(title)
                    frm.SetDesktopLocation(otherOffset.X + indexO * offset, otherOffset.Y + indexO * offset)
                    indexO += 1
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("Error in layoutOptions: " + ex.Message)
        End Try
    End Sub
End Class