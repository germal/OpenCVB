Imports System.IO
Public Class OptionsDialog
    Public cameraIndex As Int32 ' an index into the cameraRadioButton array.
    Public Const Kinect4AzureCam As Int32 = 0 ' Must be defined in VB_Classes.vb the same way!
    Public Const T265Camera As Int32 = 1 ' Must be defined in VB_Classes.vb the same way!
    Public Const StereoLabsZED2 As Int32 = 2 ' Must be defined in VB_Classes.vb the same way!
    Public Const MyntD1000 As Int32 = 3 ' Must be defined in VB_Classes.vb the same way!
    Public Const D435i As Int32 = 4 ' Must be defined in VB_Classes.vb the same way!
    Public Const L515 As Int32 = 5 ' Must be defined in VB_Classes.vb the same way!

    Public Const resLow = 0 ' Must be defined in OptionsDialog.vb the same way!
    Public Const resMed = 1 ' Must be defined in OptionsDialog.vb the same way!
    Public Const resHigh = 2 ' Must be defined in OptionsDialog.vb the same way!

    Public cameraDeviceCount(L515) As Int32
    Public cameraRadioButton(L515) As RadioButton
    Public cameraTotalCount As Integer = 0
    Private Sub OKButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OKButton.Click
        SaveSetting("OpenCVB", "FastAccurate", "FastAccurate", lowResolution.Checked)
        SaveSetting("OpenCVB", "CameraIndex", "CameraIndex", cameraIndex)

        SaveSetting("OpenCVB", "ShowLabels", "ShowLabels", ShowLabels.Checked)

        SaveSetting("OpenCVB", "TestAllDuration", "TestAllDuration", TestAllDuration.Value)
        SaveSetting("OpenCVB", "SnapToGrid", "SnapToGrid", SnapToGrid.Checked)
        SaveSetting("OpenCVB", "PythonExe", "PythonExe", PythonExeName.Text)
        SaveSetting("OpenCVB", "ShowOptions", "ShowOptions", ShowOptions.Checked)
        SaveSetting("OpenCVB", "ShowConsoleLog", "ShowConsoleLog", ShowConsoleLog.Checked)
        SaveSetting("OpenCVB", "AvoidDNNCrashes", "AvoidDNNCrashes", AvoidDNNCrashes.Checked)
        SaveSetting("OpenCVB", "RefreshRate", "RefreshRate", RefreshRate.Value)
        SaveSetting("OpenCVB", "FontName", "FontName", fontInfo.Font.Name)
        SaveSetting("OpenCVB", "FontSize", "FontSize", fontInfo.Font.Size)
        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub
    Private Sub cameraRadioButton_CheckChanged(sender As Object, e As EventArgs)
        cameraIndex = sender.tag
    End Sub
    Public Sub enableCameras()
        For i = 0 To cameraRadioButton.Count - 1
            If cameraDeviceCount(i) > 0 Then cameraRadioButton(i).Enabled = True
        Next
    End Sub
    Public Sub OptionsDialog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        For i = 0 To cameraRadioButton.Count - 1
            cameraRadioButton(i) = New RadioButton
            CameraGroup.Controls.Add(cameraRadioButton(i))
            cameraRadioButton(i).Visible = True
            If cameraDeviceCount(i) = 0 Then cameraRadioButton(i).Enabled = False
            cameraRadioButton(i).AutoSize = True
            cameraRadioButton(i).BringToFront()
            cameraRadioButton(i).Tag = i ' this will manage the public type for the camera - see VB_Classes.vb.
            cameraRadioButton(i).Location = New Point(16, (i + 1) * 20)
            cameraRadioButton(i).Text = Choose(i + 1, "Microsoft Kinect for Azure Camera", "Intel RealSense T265", "StereoLabs ZED 2 camera",
                                               "MyntEyeD 1000 camera", "Intel RealSense D435i", "Intel RealSense L515")
            AddHandler cameraRadioButton(i).CheckedChanged, AddressOf cameraRadioButton_CheckChanged
        Next

        If GetSetting("OpenCVB", "FastAccurate", "FastAccurate", True) Then
            lowResolution.Checked = True
        Else
            AccurateProcessing.Checked = True
        End If

        cameraIndex = GetSetting("OpenCVB", "CameraIndex", "CameraIndex", D435i)
        cameraRadioButton(cameraIndex).Checked = True

        ShowLabels.Checked = GetSetting("OpenCVB", "ShowLabels", "ShowLabels", False)

        TestAllDuration.Value = GetSetting("OpenCVB", "TestAllDuration", "TestAllDuration", 10)
        SnapToGrid.Checked = GetSetting("OpenCVB", "SnapToGrid", "SnapToGrid", True)
        ShowOptions.Checked = GetSetting("OpenCVB", "ShowOptions", "ShowOptions", False)
        ShowConsoleLog.Checked = GetSetting("OpenCVB", "ShowConsoleLog", "ShowConsoleLog", False)
        AvoidDNNCrashes.Checked = GetSetting("OpenCVB", "AvoidDNNCrashes", "AvoidDNNCrashes", False)
        RefreshRate.Value = GetSetting("OpenCVB", "RefreshRate", "RefreshRate", 15)

        Dim defaultSize = GetSetting("OpenCVB", "FontSize", "FontSize", 8)
        Dim DefaultFont = GetSetting("OpenCVB", "FontName", "FontName", "Tahoma")
        fontInfo.Font = New Drawing.Font(DefaultFont, defaultSize)
        fontInfo.Text = DefaultFont + " with size = " + CStr(defaultSize)

        Dim selectionName = GetSetting("OpenCVB", "PythonExe", "PythonExe", "")
        Dim selectionInfo As FileInfo = Nothing
        If selectionName <> "" Then
            selectionInfo = New FileInfo(selectionName)
            PythonExeName.Text = selectionInfo.FullName
        End If
    End Sub
    Private Sub OptionsDialog_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        Me.Hide()
        If e.KeyCode = Keys.Escape Then Cancel_Button_Click(sender, e)
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
    Private Sub Cancel_Button_Click(sender As Object, e As EventArgs) Handles Cancel_Button.Click
        OptionsDialog_Load(sender, e) ' restore the settings to what they were on entry...
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Hide()
    End Sub

    Private Sub TestAllDuration_ValueChanged(sender As Object, e As EventArgs) Handles TestAllDuration.ValueChanged
        If TestAllDuration.Value < 5 Then TestAllDuration.Value = 5
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        FontDialog1.ShowColor = False
        FontDialog1.ShowApply = False
        FontDialog1.ShowEffects = False
        FontDialog1.ShowHelp = True

        FontDialog1.MaxSize = 40
        FontDialog1.MinSize = 5

        If FontDialog1.ShowDialog() = Windows.Forms.DialogResult.OK Then
            fontInfo.Font = FontDialog1.Font
            fontInfo.Text = FontDialog1.Font.Name + " with size = " + CStr(fontInfo.Font.Size)
        End If
    End Sub
End Class
