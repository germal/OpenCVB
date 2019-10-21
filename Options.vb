Imports System.IO
Public Class OptionsForm
    Public changesRequested As Boolean
    Private Sub Options_Load(sender As Object, e As EventArgs) Handles MyBase.Load
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

        Dim pythonFileInfo As FileInfo = Nothing
        Dim pythonFolder As New IO.DirectoryInfo("C:\Program Files (x86)\Microsoft Visual Studio\")
        For Each File As IO.FileInfo In pythonFolder.GetFiles("python.exe", IO.SearchOption.AllDirectories)
            PythonExes.Items.Add(File.FullName)
        Next

        Dim selectionName = GetSetting("OpenCVB", "PythonExeName", "PythonExeName", "")
        Dim selectionInfo As FileInfo = Nothing
        If selectionName <> "" Then selectionInfo = New FileInfo(selectionName)
        Dim selectionIndex As Int32 = 0
        If selectionName <> "" Then
            If selectionInfo.Exists Then
                selectionIndex = PythonExes.FindStringExact(selectionName)
            End If
        End If
        If PythonExes.Items.Count > selectionIndex Then PythonExes.SelectedIndex = selectionIndex
    End Sub
    Private Sub OptionsForm_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        changesRequested = False
        If e.KeyCode = Keys.Escape Then Me.Close()
    End Sub
    Private Sub OKbutton_Click(sender As Object, e As EventArgs) Handles OKbutton.Click
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
        SaveSetting("OpenCVB", "PythonExeName", "PythonExeName", PythonExes.Text)
        Me.Hide()
    End Sub
    Private Sub CancelButton_Click(sender As Object, e As EventArgs) Handles Cancel.Click
        changesRequested = False
        Me.Hide()
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
End Class
