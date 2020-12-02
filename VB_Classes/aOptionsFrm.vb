Imports cv = OpenCvSharp
Imports System.ComponentModel
Public Class aOptionsFrm
    Public optionsTitle As New List(Of String)
    Public optionsForms As New List(Of System.Windows.Forms.Form)
    Public hiddenOptions As New List(Of String)
    Public hiddenForms As New List(Of System.Windows.Forms.Form)
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
        For i = 0 To optionsTitle.Count - 1
            If optionsTitle(i) = title Then Return optionsForms(i)
        Next
        Return Nothing
    End Function
    Public Function findFormByTitle(title As String) As Object
        For i = 0 To aOptions.optionsTitle.Count - 1
            If optionsTitle(i) = title Then Return optionsForms(i)
        Next
        Return Nothing
    End Function
    Public Sub setParent(frm As Object)
        If findFormByTitle(frm.text) Is Nothing Then frm.MdiParent = aOptions
    End Sub

    Public Sub addTitle(frm As Object)
        frm.show()
        If optionsTitle.Contains(frm.Text) = False Then
            optionsTitle.Add(frm.Text)
            optionsForms.Add(frm)
        Else
            ' If optionsHidden.Contains(frm.Text) = False Then optionsHidden.Add(frm.Text)
            hiddenOptions.Add(frm.Text)
            hiddenForms.Add(frm)
        End If
    End Sub
    Public Sub layoutOptions()
        Me.Show()
        System.Windows.Forms.Application.DoEvents()
        Dim sliderOffset As New cv.Point(0, 0)
        Dim otherOffset As New cv.Point(Me.Width / 2, 0)
        For Each frm In hiddenForms
            frm.Hide()
        Next

        Try
            Dim indexS As Integer = 0
            Dim indexO As Integer = 0
            For Each title In optionsTitle
                If hiddenOptions.Contains(title) Then Continue For
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