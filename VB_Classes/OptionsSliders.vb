Imports System.ComponentModel
Imports System.Windows.Forms
Imports System.Drawing

Public Class OptionsSliders
    Public sliders() As TrackBar
    Public sLabels() As Label
    Public countLabel() As Label
    Public Sub Setup(ocvb As AlgorithmData, caller As String, count As Int32)
        ReDim sliders(count - 1)
        ReDim sLabels(count - 1)
        ReDim countLabel(count - 1)
        Me.Text = caller + " Options"
        Dim yIncr = 100
        For i = 0 To sliders.Count - 1
            FlowLayoutPanel1.FlowDirection = FlowDirection.LeftToRight
            sLabels(i) = New Label
            sLabels(i).AutoSize = False
            sLabels(i).Width = 100
            sLabels(i).Height = 50
            FlowLayoutPanel1.Controls.Add(sLabels(i))
            sliders(i) = New TrackBar
            sliders(i).Width = 440
            sliders(i).Tag = i
            sliders(i).Visible = False
            AddHandler sliders(i).ValueChanged, AddressOf TrackBar_ValueChanged
            FlowLayoutPanel1.Controls.Add(sliders(i))
            countLabel(i) = New Label
            countLabel(i).AutoSize = False
            countLabel(i).Width = 100
            countLabel(i).Height = 50
            FlowLayoutPanel1.Controls.Add(countLabel(i))
            FlowLayoutPanel1.SetFlowBreak(countLabel(i), True)
        Next
        If ocvb.parms.ShowOptions Then
            If ocvb.suppressOptions = False Then Me.Show()
        End If
    End Sub
    Private Sub setTrackbar(index As Integer, label As String, min As Integer, max As Integer, value As Integer)
        sLabels(index).Text = label
        sliders(index).Minimum = min
        sliders(index).Maximum = max
        sliders(index).Value = value
        sliders(index).Visible = True
        sLabels(index).Visible = True
        countLabel(index).Text = CStr(value)
        countLabel(index).Visible = True
    End Sub
    Public Sub setupTrackBar1(label As String, min As Integer, max As Integer, value As Integer)
        setTrackbar(0, label, min, max, value)
    End Sub
    Public Sub setupTrackBar2(label As String, min As Integer, max As Integer, value As Integer)
        setTrackbar(1, label, min, max, value)
    End Sub
    Public Sub setupTrackBar3(label As String, min As Integer, max As Integer, value As Integer)
        setTrackbar(2, label, min, max, value)
    End Sub
    Public Sub setupTrackBar4(label As String, min As Integer, max As Integer, value As Integer)
        setTrackbar(3, label, min, max, value)
    End Sub
    Private Sub TrackBar_ValueChanged(sender As Object, e As EventArgs)
        countLabel(sender.tag).Text = CStr(sliders(sender.tag).Value)
    End Sub
    Private Sub OptionsSlider_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Width = 630
        Me.Height = 260
        Me.SetDesktopLocation(applocation.Left + slidersOffset.X, applocation.Top + applocation.Height + slidersOffset.Y)
        slidersOffset.X += offsetIncr
        slidersOffset.Y += offsetIncr
        If slidersOffset.X > offsetMax Then slidersOffset.X = 0
        If slidersOffset.Y > offsetMax Then slidersOffset.Y = 0
    End Sub
End Class