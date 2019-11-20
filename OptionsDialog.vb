Imports System.IO
Public Class OptionsDialog
    Private Sub OKButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OKButton.Click
        SaveSetting("OpenCVB", "FastAccurate", "FastAccurate", FastProcessing.Checked)
        SaveSetting("OpenCVB", "IntelCamera", "IntelCamera", IntelCamera.Checked)

        SaveSetting("OpenCVB", "MinimizeMemoryFootprint", "MinimizeMemoryFootprint", MinimizeMemoryFootprint.Checked)
        SaveSetting("OpenCVB", "ShowLabels", "ShowLabels", ShowLabels.Checked)

        SaveSetting("OpenCVB", "DecimationFilter", "DecimationFilter", DecimationFilter.Checked)
        SaveSetting("OpenCVB", "ThresholdFilter", "ThresholdFilter", ThresholdFilter.Checked)
        SaveSetting("OpenCVB", "DepthToDisparity", "DepthToDisparity", DepthToDisparity.Checked)
        SaveSetting("OpenCVB", "SpatialFilter", "SpatialFilter", SpatialFilter.Checked)
        SaveSetting("OpenCVB", "TemporalFilter", "TemporalFilter", TemporalFilter.Checked)
        SaveSetting("OpenCVB", "HoleFillingFilter", "HoleFillingFilter", HoleFillingFilter.Checked)
        SaveSetting("OpenCVB", "DisparityToDepth", "DisparityToDepth", DisparityToDepth.Checked)

        SaveSetting("OpenCVB", "TestAllDuration", "TestAllDuration", TestAllDuration.Value)
        SaveSetting("OpenCVB", "SnapToGrid", "SnapToGrid", SnapToGrid.Checked)
        SaveSetting("OpenCVB", "PythonExe", "PythonExe", PythonExeName.Text)
        SaveSetting("OpenCVB", "ShowOptions", "ShowOptions", ShowOptions.Checked)
        SaveSetting("OpenCVB", "ShowConsoleLog", "ShowConsoleLog", ShowConsoleLog.Checked)
        SaveSetting("OpenCVB", "AvoidDNNCrashes", "AvoidDNNCrashes", AvoidDNNCrashes.Checked)
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub
    Public Sub Dialog1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If GetSetting("OpenCVB", "FastAccurate", "FastAccurate", True) Then
            FastProcessing.Checked = True
        Else
            AccurateProcessing.Checked = True
        End If

        If GetSetting("OpenCVB", "IntelCamera", "IntelCamera", True) Then
            IntelCamera.Checked = True
        Else
            Kinect4Azure.Checked = True
        End If

        MinimizeMemoryFootprint.Checked = GetSetting("OpenCVB", "MinimizeMemoryFootprint", "MinimizeMemoryFootprint", False)
        ShowLabels.Checked = GetSetting("OpenCVB", "ShowLabels", "ShowLabels", False)

        DecimationFilter.Checked = GetSetting("OpenCVB", "DecimationFilter", "DecimationFilter", False)
        ThresholdFilter.Checked = GetSetting("OpenCVB", "ThresholdFilter", "ThresholdFilter", False)
        DepthToDisparity.Checked = GetSetting("OpenCVB", "DepthToDisparity", "DepthToDisparity", True)
        SpatialFilter.Checked = GetSetting("OpenCVB", "SpatialFilter", "SpatialFilter", True)
        TemporalFilter.Checked = GetSetting("OpenCVB", "TemporalFilter", "TemporalFilter", False)
        HoleFillingFilter.Checked = GetSetting("OpenCVB", "HoleFillingFilter", "HoleFillingFilter", True)
        DisparityToDepth.Checked = GetSetting("OpenCVB", "DisparityToDepth", "DisparityToDepth", True)

        TestAllDuration.Value = GetSetting("OpenCVB", "TestAllDuration", "TestAllDuration", 10)
        SnapToGrid.Checked = GetSetting("OpenCVB", "SnapToGrid", "SnapToGrid", True)
        ShowOptions.Checked = GetSetting("OpenCVB", "ShowOptions", "ShowOptions", False)
        ShowConsoleLog.Checked = GetSetting("OpenCVB", "ShowConsoleLog", "ShowConsoleLog", False)
        AvoidDNNCrashes.Checked = GetSetting("OpenCVB", "AvoidDNNCrashes", "AvoidDNNCrashes", False)

        Dim selectionName = GetSetting("OpenCVB", "PythonExe", "PythonExe", "")
        Dim selectionInfo As FileInfo = Nothing
        If selectionName <> "" Then
            selectionInfo = New FileInfo(selectionName)
            PythonExeName.Text = selectionInfo.FullName
        End If
    End Sub
    Private Sub OptionsDialog_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        Me.Hide()
        If e.KeyCode = Keys.Escape Then CancelButton_Click(sender, e)
    End Sub
    Private Sub IntelCamera_CheckedChanged(sender As Object, e As EventArgs) Handles IntelCamera.CheckedChanged
        DecimationFilter.Enabled = True
        ThresholdFilter.Enabled = True
        SpatialFilter.Enabled = True
        TemporalFilter.Enabled = True
        HoleFillingFilter.Enabled = True
        DisparityToDepth.Enabled = True
    End Sub
    Private Sub Kinect4Azure_CheckedChanged(sender As Object, e As EventArgs) Handles Kinect4Azure.CheckedChanged
        DecimationFilter.Enabled = False
        ThresholdFilter.Enabled = False
        SpatialFilter.Enabled = False
        TemporalFilter.Enabled = False
        HoleFillingFilter.Enabled = False
        DisparityToDepth.Enabled = False
    End Sub
    Private Sub SelectPythonFile_Click(sender As Object, e As EventArgs) Handles SelectPythonFile.Click
        Dim pythonInfo As FileInfo
        OpenFileDialog1.FileName = "Python.exe"
        If PythonExeName.Text <> "" Then
            pythonInfo = New FileInfo(PythonExeName.Text)
            OpenFileDialog1.InitialDirectory = pythonInfo.DirectoryName
        Else
            OpenFileDialog1.InitialDirectory = "C:\\"
        End If
        OpenFileDialog1.Filter = "*.exe (*.exe) | *.exe"
        If OpenFileDialog1.ShowDialog() = DialogResult.OK Then
            pythonInfo = New FileInfo(OpenFileDialog1.FileName)
            PythonExeName.Text = pythonInfo.FullName
        End If
    End Sub
    Private Sub CancelButton_Click(sender As Object, e As EventArgs) Handles CancelButton.Click
        Dialog1_Load(sender, e) ' restore the settings to what they were on entry...
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Hide()
    End Sub
End Class
