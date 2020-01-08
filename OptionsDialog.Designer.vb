<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsDialog
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.GroupBox5 = New System.Windows.Forms.GroupBox()
        Me.SnapToGrid = New System.Windows.Forms.CheckBox()
        Me.Kinect4Azure = New System.Windows.Forms.RadioButton()
        Me.IntelCamera = New System.Windows.Forms.RadioButton()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.AccurateProcessing = New System.Windows.Forms.RadioButton()
        Me.lowResolution = New System.Windows.Forms.RadioButton()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.AvoidDNNCrashes = New System.Windows.Forms.CheckBox()
        Me.ShowConsoleLog = New System.Windows.Forms.CheckBox()
        Me.ShowLabels = New System.Windows.Forms.CheckBox()
        Me.MinimizeMemoryFootprint = New System.Windows.Forms.CheckBox()
        Me.Filters = New System.Windows.Forms.GroupBox()
        Me.DisparityToDepth = New System.Windows.Forms.CheckBox()
        Me.HoleFillingFilter = New System.Windows.Forms.CheckBox()
        Me.TemporalFilter = New System.Windows.Forms.CheckBox()
        Me.SpatialFilter = New System.Windows.Forms.CheckBox()
        Me.DepthToDisparity = New System.Windows.Forms.CheckBox()
        Me.ThresholdFilter = New System.Windows.Forms.CheckBox()
        Me.DecimationFilter = New System.Windows.Forms.CheckBox()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.ShowOptions = New System.Windows.Forms.CheckBox()
        Me.TestAllDuration = New System.Windows.Forms.NumericUpDown()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.GroupBox6 = New System.Windows.Forms.GroupBox()
        Me.PythonExeName = New System.Windows.Forms.TextBox()
        Me.SelectPythonFile = New System.Windows.Forms.Button()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.OKButton = New System.Windows.Forms.Button()
        Me.Cancel_Button = New System.Windows.Forms.Button()
        Me.GroupBox5.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.Filters.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        CType(Me.TestAllDuration, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox6.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.SnapToGrid)
        Me.GroupBox5.Controls.Add(Me.Kinect4Azure)
        Me.GroupBox5.Controls.Add(Me.IntelCamera)
        Me.GroupBox5.Location = New System.Drawing.Point(21, 12)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(692, 160)
        Me.GroupBox5.TabIndex = 3
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "Camera"
        '
        'SnapToGrid
        '
        Me.SnapToGrid.AutoSize = True
        Me.SnapToGrid.Location = New System.Drawing.Point(84, 114)
        Me.SnapToGrid.Name = "SnapToGrid"
        Me.SnapToGrid.Size = New System.Drawing.Size(354, 24)
        Me.SnapToGrid.TabIndex = 2
        Me.SnapToGrid.Text = "Snap to Grid (Resizes to 360x640 for display)"
        Me.SnapToGrid.UseVisualStyleBackColor = True
        '
        'Kinect4Azure
        '
        Me.Kinect4Azure.AutoSize = True
        Me.Kinect4Azure.Location = New System.Drawing.Point(16, 70)
        Me.Kinect4Azure.Name = "Kinect4Azure"
        Me.Kinect4Azure.Size = New System.Drawing.Size(309, 24)
        Me.Kinect4Azure.TabIndex = 1
        Me.Kinect4Azure.TabStop = True
        Me.Kinect4Azure.Text = "Use Microsoft Kinect for Azure Camera"
        Me.Kinect4Azure.UseVisualStyleBackColor = True
        '
        'IntelCamera
        '
        Me.IntelCamera.AutoSize = True
        Me.IntelCamera.Location = New System.Drawing.Point(16, 40)
        Me.IntelCamera.Name = "IntelCamera"
        Me.IntelCamera.Size = New System.Drawing.Size(271, 24)
        Me.IntelCamera.TabIndex = 0
        Me.IntelCamera.TabStop = True
        Me.IntelCamera.Text = "Use Intel D400Series 3D Camera"
        Me.IntelCamera.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.AccurateProcessing)
        Me.GroupBox1.Controls.Add(Me.lowResolution)
        Me.GroupBox1.Location = New System.Drawing.Point(21, 178)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(936, 128)
        Me.GroupBox1.TabIndex = 4
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Speed"
        '
        'AccurateProcessing
        '
        Me.AccurateProcessing.AutoSize = True
        Me.AccurateProcessing.Location = New System.Drawing.Point(16, 70)
        Me.AccurateProcessing.Name = "AccurateProcessing"
        Me.AccurateProcessing.Size = New System.Drawing.Size(225, 24)
        Me.AccurateProcessing.TabIndex = 1
        Me.AccurateProcessing.TabStop = True
        Me.AccurateProcessing.Text = "Run algorithm at 1280x720"
        Me.AccurateProcessing.UseVisualStyleBackColor = True
        '
        'lowResolution
        '
        Me.lowResolution.AutoSize = True
        Me.lowResolution.Location = New System.Drawing.Point(16, 40)
        Me.lowResolution.Name = "lowResolution"
        Me.lowResolution.Size = New System.Drawing.Size(482, 24)
        Me.lowResolution.TabIndex = 0
        Me.lowResolution.TabStop = True
        Me.lowResolution.Text = "Run algorithm after resizing image to 640x360 (lowResolution)"
        Me.lowResolution.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.AvoidDNNCrashes)
        Me.GroupBox2.Controls.Add(Me.ShowConsoleLog)
        Me.GroupBox2.Controls.Add(Me.ShowLabels)
        Me.GroupBox2.Controls.Add(Me.MinimizeMemoryFootprint)
        Me.GroupBox2.Location = New System.Drawing.Point(21, 312)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(936, 192)
        Me.GroupBox2.TabIndex = 5
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Global Options"
        '
        'AvoidDNNCrashes
        '
        Me.AvoidDNNCrashes.AutoSize = True
        Me.AvoidDNNCrashes.Location = New System.Drawing.Point(15, 136)
        Me.AvoidDNNCrashes.Name = "AvoidDNNCrashes"
        Me.AvoidDNNCrashes.Size = New System.Drawing.Size(449, 24)
        Me.AvoidDNNCrashes.TabIndex = 3
        Me.AvoidDNNCrashes.Text = "DNN's crash some machines.  Check this to not run DNN's."
        Me.AvoidDNNCrashes.UseVisualStyleBackColor = True
        '
        'ShowConsoleLog
        '
        Me.ShowConsoleLog.AutoSize = True
        Me.ShowConsoleLog.Location = New System.Drawing.Point(16, 106)
        Me.ShowConsoleLog.Name = "ShowConsoleLog"
        Me.ShowConsoleLog.Size = New System.Drawing.Size(630, 24)
        Me.ShowConsoleLog.TabIndex = 2
        Me.ShowConsoleLog.Text = "Show Console Log for external processes - external process messages will not show" &
    "."
        Me.ShowConsoleLog.UseVisualStyleBackColor = True
        '
        'ShowLabels
        '
        Me.ShowLabels.AutoSize = True
        Me.ShowLabels.Location = New System.Drawing.Point(16, 76)
        Me.ShowLabels.Name = "ShowLabels"
        Me.ShowLabels.Size = New System.Drawing.Size(175, 24)
        Me.ShowLabels.TabIndex = 1
        Me.ShowLabels.Text = "Show Image Labels"
        Me.ShowLabels.UseVisualStyleBackColor = True
        '
        'MinimizeMemoryFootprint
        '
        Me.MinimizeMemoryFootprint.AutoSize = True
        Me.MinimizeMemoryFootprint.Location = New System.Drawing.Point(16, 46)
        Me.MinimizeMemoryFootprint.Name = "MinimizeMemoryFootprint"
        Me.MinimizeMemoryFootprint.Size = New System.Drawing.Size(224, 24)
        Me.MinimizeMemoryFootprint.TabIndex = 0
        Me.MinimizeMemoryFootprint.Text = "Minimize Memory Footprint"
        Me.MinimizeMemoryFootprint.UseVisualStyleBackColor = True
        '
        'Filters
        '
        Me.Filters.Controls.Add(Me.DisparityToDepth)
        Me.Filters.Controls.Add(Me.HoleFillingFilter)
        Me.Filters.Controls.Add(Me.TemporalFilter)
        Me.Filters.Controls.Add(Me.SpatialFilter)
        Me.Filters.Controls.Add(Me.DepthToDisparity)
        Me.Filters.Controls.Add(Me.ThresholdFilter)
        Me.Filters.Controls.Add(Me.DecimationFilter)
        Me.Filters.Location = New System.Drawing.Point(21, 510)
        Me.Filters.Name = "Filters"
        Me.Filters.Size = New System.Drawing.Size(936, 274)
        Me.Filters.TabIndex = 7
        Me.Filters.TabStop = False
        Me.Filters.Text = "RealSense Camera Filters (Running in the DSP chip)"
        '
        'DisparityToDepth
        '
        Me.DisparityToDepth.AutoSize = True
        Me.DisparityToDepth.Enabled = False
        Me.DisparityToDepth.Location = New System.Drawing.Point(16, 226)
        Me.DisparityToDepth.Name = "DisparityToDepth"
        Me.DisparityToDepth.Size = New System.Drawing.Size(162, 24)
        Me.DisparityToDepth.TabIndex = 6
        Me.DisparityToDepth.Text = "Disparity to Depth"
        Me.DisparityToDepth.UseVisualStyleBackColor = True
        '
        'HoleFillingFilter
        '
        Me.HoleFillingFilter.AutoSize = True
        Me.HoleFillingFilter.Location = New System.Drawing.Point(16, 196)
        Me.HoleFillingFilter.Name = "HoleFillingFilter"
        Me.HoleFillingFilter.Size = New System.Drawing.Size(151, 24)
        Me.HoleFillingFilter.TabIndex = 5
        Me.HoleFillingFilter.Text = "Hole Filling Filter"
        Me.HoleFillingFilter.UseVisualStyleBackColor = True
        '
        'TemporalFilter
        '
        Me.TemporalFilter.AutoSize = True
        Me.TemporalFilter.Location = New System.Drawing.Point(16, 166)
        Me.TemporalFilter.Name = "TemporalFilter"
        Me.TemporalFilter.Size = New System.Drawing.Size(140, 24)
        Me.TemporalFilter.TabIndex = 4
        Me.TemporalFilter.Text = "Temporal Filter"
        Me.TemporalFilter.UseVisualStyleBackColor = True
        '
        'SpatialFilter
        '
        Me.SpatialFilter.AutoSize = True
        Me.SpatialFilter.Location = New System.Drawing.Point(16, 136)
        Me.SpatialFilter.Name = "SpatialFilter"
        Me.SpatialFilter.Size = New System.Drawing.Size(123, 24)
        Me.SpatialFilter.TabIndex = 3
        Me.SpatialFilter.Text = "Spatial Filter"
        Me.SpatialFilter.UseVisualStyleBackColor = True
        '
        'DepthToDisparity
        '
        Me.DepthToDisparity.AutoSize = True
        Me.DepthToDisparity.Enabled = False
        Me.DepthToDisparity.Location = New System.Drawing.Point(16, 106)
        Me.DepthToDisparity.Name = "DepthToDisparity"
        Me.DepthToDisparity.Size = New System.Drawing.Size(162, 24)
        Me.DepthToDisparity.TabIndex = 2
        Me.DepthToDisparity.Text = "Depth to Disparity"
        Me.DepthToDisparity.UseVisualStyleBackColor = True
        '
        'ThresholdFilter
        '
        Me.ThresholdFilter.AutoSize = True
        Me.ThresholdFilter.Location = New System.Drawing.Point(16, 76)
        Me.ThresholdFilter.Name = "ThresholdFilter"
        Me.ThresholdFilter.Size = New System.Drawing.Size(144, 24)
        Me.ThresholdFilter.TabIndex = 1
        Me.ThresholdFilter.Text = "Threshold Filter"
        Me.ThresholdFilter.UseVisualStyleBackColor = True
        '
        'DecimationFilter
        '
        Me.DecimationFilter.AutoSize = True
        Me.DecimationFilter.Location = New System.Drawing.Point(16, 46)
        Me.DecimationFilter.Name = "DecimationFilter"
        Me.DecimationFilter.Size = New System.Drawing.Size(154, 24)
        Me.DecimationFilter.TabIndex = 0
        Me.DecimationFilter.Text = "Decimation Filter"
        Me.DecimationFilter.UseVisualStyleBackColor = True
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.ShowOptions)
        Me.GroupBox4.Controls.Add(Me.TestAllDuration)
        Me.GroupBox4.Controls.Add(Me.Label1)
        Me.GroupBox4.Location = New System.Drawing.Point(24, 797)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(941, 139)
        Me.GroupBox4.TabIndex = 8
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "Test All Options"
        '
        'ShowOptions
        '
        Me.ShowOptions.AutoSize = True
        Me.ShowOptions.Location = New System.Drawing.Point(76, 94)
        Me.ShowOptions.Name = "ShowOptions"
        Me.ShowOptions.Size = New System.Drawing.Size(310, 24)
        Me.ShowOptions.TabIndex = 7
        Me.ShowOptions.Text = "Show algorithm options during 'Test All'"
        Me.ShowOptions.UseVisualStyleBackColor = True
        '
        'TestAllDuration
        '
        Me.TestAllDuration.Location = New System.Drawing.Point(16, 50)
        Me.TestAllDuration.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.TestAllDuration.Name = "TestAllDuration"
        Me.TestAllDuration.ReadOnly = True
        Me.TestAllDuration.Size = New System.Drawing.Size(89, 26)
        Me.TestAllDuration.TabIndex = 2
        Me.TestAllDuration.Value = New Decimal(New Integer() {10, 0, 0, 0})
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(111, 52)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(629, 20)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Duration in seconds of each test when running ""Test All"" (there is a 10 second mi" &
    "nimum)"
        '
        'GroupBox6
        '
        Me.GroupBox6.Controls.Add(Me.PythonExeName)
        Me.GroupBox6.Controls.Add(Me.SelectPythonFile)
        Me.GroupBox6.Controls.Add(Me.Label2)
        Me.GroupBox6.Location = New System.Drawing.Point(21, 942)
        Me.GroupBox6.Name = "GroupBox6"
        Me.GroupBox6.Size = New System.Drawing.Size(941, 116)
        Me.GroupBox6.TabIndex = 9
        Me.GroupBox6.TabStop = False
        Me.GroupBox6.Text = "Python "
        '
        'PythonExeName
        '
        Me.PythonExeName.Location = New System.Drawing.Point(64, 64)
        Me.PythonExeName.Name = "PythonExeName"
        Me.PythonExeName.Size = New System.Drawing.Size(869, 26)
        Me.PythonExeName.TabIndex = 4
        '
        'SelectPythonFile
        '
        Me.SelectPythonFile.Location = New System.Drawing.Point(15, 65)
        Me.SelectPythonFile.Name = "SelectPythonFile"
        Me.SelectPythonFile.Size = New System.Drawing.Size(43, 30)
        Me.SelectPythonFile.TabIndex = 3
        Me.SelectPythonFile.Text = "..."
        Me.SelectPythonFile.UseVisualStyleBackColor = True
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(12, 37)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(552, 20)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Select the version of Python that should be used when running Python scripts"
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'OKButton
        '
        Me.OKButton.Location = New System.Drawing.Point(798, 33)
        Me.OKButton.Name = "OKButton"
        Me.OKButton.Size = New System.Drawing.Size(142, 42)
        Me.OKButton.TabIndex = 10
        Me.OKButton.Text = "OK"
        Me.OKButton.UseVisualStyleBackColor = True
        '
        'Cancel_Button
        '
        Me.Cancel_Button.Location = New System.Drawing.Point(798, 83)
        Me.Cancel_Button.Name = "Cancel_Button"
        Me.Cancel_Button.Size = New System.Drawing.Size(142, 42)
        Me.Cancel_Button.TabIndex = 11
        Me.Cancel_Button.Text = "Cancel"
        Me.Cancel_Button.UseVisualStyleBackColor = True
        '
        'OptionsDialog
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1049, 1127)
        Me.Controls.Add(Me.Cancel_Button)
        Me.Controls.Add(Me.OKButton)
        Me.Controls.Add(Me.GroupBox6)
        Me.Controls.Add(Me.GroupBox4)
        Me.Controls.Add(Me.Filters)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.GroupBox5)
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.MinimizeBox = False
        Me.Name = "OptionsDialog"
        Me.ShowInTaskbar = False
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show
        Me.Text = "OpenCVB Global Settings"
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox5.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.Filters.ResumeLayout(False)
        Me.Filters.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        CType(Me.TestAllDuration, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox6.ResumeLayout(False)
        Me.GroupBox6.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents GroupBox5 As GroupBox
    Friend WithEvents SnapToGrid As CheckBox
    Friend WithEvents Kinect4Azure As RadioButton
    Friend WithEvents IntelCamera As RadioButton
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents AccurateProcessing As RadioButton
    Friend WithEvents lowResolution As RadioButton
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents ShowLabels As CheckBox
    Friend WithEvents MinimizeMemoryFootprint As CheckBox
    Friend WithEvents Filters As GroupBox
    Friend WithEvents DisparityToDepth As CheckBox
    Friend WithEvents HoleFillingFilter As CheckBox
    Friend WithEvents TemporalFilter As CheckBox
    Friend WithEvents SpatialFilter As CheckBox
    Friend WithEvents DepthToDisparity As CheckBox
    Friend WithEvents ThresholdFilter As CheckBox
    Friend WithEvents DecimationFilter As CheckBox
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents TestAllDuration As NumericUpDown
    Friend WithEvents Label1 As Label
    Friend WithEvents GroupBox6 As GroupBox
    Friend WithEvents PythonExeName As TextBox
    Friend WithEvents SelectPythonFile As Button
    Friend WithEvents Label2 As Label
    Friend WithEvents OpenFileDialog1 As OpenFileDialog
    Friend WithEvents ShowOptions As CheckBox
    Friend WithEvents ShowConsoleLog As CheckBox
    Friend WithEvents AvoidDNNCrashes As CheckBox
    Friend WithEvents OKButton As Button
    Friend WithEvents Cancel_Button As Button
End Class
