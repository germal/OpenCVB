Imports cv = OpenCvSharp
Imports System.ComponentModel
Imports System.Windows.Forms
Public Class aOptionsFrm
    Public optionsTitle As New List(Of String)
    Public hiddenOptions As New List(Of String)
    Public offset = 30
    Public layoutOptionsRequested As Boolean
    Private Sub Options_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Left = GetSetting("OpenCVB", "aOptionsLeft", "aOptionsLeft", ocvb.defaultRect.X - offset)
        Me.Top = GetSetting("OpenCVB", "aOptionsTop", "aOptionsTop", ocvb.defaultRect.Y - offset)
        Me.Width = GetSetting("OpenCVB", "aOptionsWidth", "aOptionsWidth", ocvb.defaultRect.Width)
        Me.Height = GetSetting("OpenCVB", "aOptionsHeight", "aOptionsHeight", ocvb.defaultRect.Height)
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
    Public Sub addTitle(frm As Object)
        If optionsTitle.Contains(frm.Text) = False Then
            optionsTitle.Add(frm.Text)
        Else
            hiddenOptions.Add(frm.Text)
        End If

        frm.show
        layoutOptionsRequested = True
    End Sub
    Public Sub layoutOptions()
        layoutOptionsRequested = False
        Me.Show()
        Application.DoEvents()
        Dim sliderOffset As New cv.Point(0, 0)
        Dim w = GetSetting("OpenCVB", "aOptionsWidth", "aOptionsWidth", ocvb.defaultRect.Width)
        Dim otherOffset As New cv.Point(w / 2, 0)
        For Each title In hiddenOptions
            Dim hideList As New List(Of Form)
            For Each frm In Application.OpenForms
                If frm.text = title Then
                    frm.hide
                    Exit For
                End If
            Next
        Next

        Try
            Dim indexS As Integer = 0
            Dim indexO As Integer = 0
            For Each title In optionsTitle
                If title.EndsWith(" Slider Options") Or title.EndsWith(" Keyboard Options") Or title.EndsWith("OptionsAlphaBlend") Then
                    Dim frm = findRealForm(title)
                    If frm Is Nothing Then Continue For
                    frm.SetDesktopLocation(sliderOffset.X + indexS * offset, sliderOffset.Y + indexS * offset)
                    indexS += 1
                End If
                If title.EndsWith(" Radio Options") Or title.EndsWith(" CheckBox Options") Then
                    Dim frm = findRealForm(title)
                    If frm Is Nothing Then Continue For
                    frm.SetDesktopLocation(otherOffset.X + indexO * offset, otherOffset.Y + indexO * offset)
                    indexO += 1
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("Error in layoutOptions: " + ex.Message)
        End Try
    End Sub
End Class