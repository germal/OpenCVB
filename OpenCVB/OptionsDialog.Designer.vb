<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OptionsDialog
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.CameraGroup = New System.Windows.Forms.GroupBox()
        Me.SnapToGrid = New System.Windows.Forms.CheckBox()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.AccurateProcessing = New System.Windows.Forms.RadioButton()
        Me.lowResolution = New System.Windows.Forms.RadioButton()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.RefreshRate = New System.Windows.Forms.NumericUpDown()
        Me.AvoidDNNCrashes = New System.Windows.Forms.CheckBox()
        Me.ShowConsoleLog = New System.Windows.Forms.CheckBox()
        Me.ShowLabels = New System.Windows.Forms.CheckBox()
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
        Me.CameraGroup.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        CType(Me.RefreshRate, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox4.SuspendLayout()
        CType(Me.TestAllDuration, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox6.SuspendLayout()
        Me.SuspendLayout()
        '
        'CameraGroup
        '
        Me.CameraGroup.Controls.Add(Me.SnapToGrid)
        Me.CameraGroup.Location = New System.Drawing.Point(21, 12)
        Me.CameraGroup.Name = "CameraGroup"
        Me.CameraGroup.Size = New System.Drawing.Size(771, 227)
        Me.CameraGroup.TabIndex = 3
        Me.CameraGroup.TabStop = False
        Me.CameraGroup.Text = "Camera"
        '
        'SnapToGrid
        '
        Me.SnapToGrid.AutoSize = True
        Me.SnapToGrid.Location = New System.Drawing.Point(63, 190)
        Me.SnapToGrid.Name = "SnapToGrid"
        Me.SnapToGrid.Size = New System.Drawing.Size(354, 24)
        Me.SnapToGrid.TabIndex = 2
        Me.SnapToGrid.Text = "Snap to Grid (Resizes to 360x640 for display)"
        Me.SnapToGrid.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.AccurateProcessing)
        Me.GroupBox1.Controls.Add(Me.lowResolution)
        Me.GroupBox1.Location = New System.Drawing.Point(21, 240)
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
        Me.lowResolution.Size = New System.Drawing.Size(471, 24)
        Me.lowResolution.TabIndex = 0
        Me.lowResolution.TabStop = True
        Me.lowResolution.Text = "Run algorithm after resizing image to 640x360 (lowResolution)"
        Me.lowResolution.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.Label3)
        Me.GroupBox2.Controls.Add(Me.RefreshRate)
        Me.GroupBox2.Controls.Add(Me.AvoidDNNCrashes)
        Me.GroupBox2.Controls.Add(Me.ShowConsoleLog)
        Me.GroupBox2.Controls.Add(Me.ShowLabels)
        Me.GroupBox2.Location = New System.Drawing.Point(21, 372)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(936, 192)
        Me.GroupBox2.TabIndex = 5
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Global Options"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(112, 153)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(289, 20)
        Me.Label3.TabIndex = 5
        Me.Label3.Text = "Number of screen refreshes per second"
        '
        'RefreshRate
        '
        Me.RefreshRate.Increment = New Decimal(New Integer() {5, 0, 0, 0})
        Me.RefreshRate.Location = New System.Drawing.Point(16, 147)
        Me.RefreshRate.Minimum = New Decimal(New Integer() {5, 0, 0, 0})
        Me.RefreshRate.Name = "RefreshRate"
        Me.RefreshRate.ReadOnly = True
        Me.RefreshRate.Size = New System.Drawing.Size(89, 26)
        Me.RefreshRate.TabIndex = 4
        Me.RefreshRate.Value = New Decimal(New Integer() {15, 0, 0, 0})
        '
        'AvoidDNNCrashes
        '
        Me.AvoidDNNCrashes.AutoSize = True
        Me.AvoidDNNCrashes.Location = New System.Drawing.Point(16, 100)
        Me.AvoidDNNCrashes.Name = "AvoidDNNCrashes"
        Me.AvoidDNNCrashes.Size = New System.Drawing.Size(449, 24)
        Me.AvoidDNNCrashes.TabIndex = 3
        Me.AvoidDNNCrashes.Text = "DNN's crash some machines.  Check this to not run DNN's."
        Me.AvoidDNNCrashes.UseVisualStyleBackColor = True
        '
        'ShowConsoleLog
        '
        Me.ShowConsoleLog.AutoSize = True
        Me.ShowConsoleLog.Location = New System.Drawing.Point(16, 70)
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
        Me.ShowLabels.Location = New System.Drawing.Point(16, 40)
        Me.ShowLabels.Name = "ShowLabels"
        Me.ShowLabels.Size = New System.Drawing.Size(175, 24)
        Me.ShowLabels.TabIndex = 1
        Me.ShowLabels.Text = "Show Image Labels"
        Me.ShowLabels.UseVisualStyleBackColor = True
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.ShowOptions)
        Me.GroupBox4.Controls.Add(Me.TestAllDuration)
        Me.GroupBox4.Controls.Add(Me.Label1)
        Me.GroupBox4.Location = New System.Drawing.Point(22, 579)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(935, 139)
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
        Me.Label1.Size = New System.Drawing.Size(620, 20)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Duration in seconds of each test when running ""Test All"" (there is a 5 second min" &
    "imum)"
        '
        'GroupBox6
        '
        Me.GroupBox6.Controls.Add(Me.PythonExeName)
        Me.GroupBox6.Controls.Add(Me.SelectPythonFile)
        Me.GroupBox6.Controls.Add(Me.Label2)
        Me.GroupBox6.Location = New System.Drawing.Point(21, 719)
        Me.GroupBox6.Name = "GroupBox6"
        Me.GroupBox6.Size = New System.Drawing.Size(936, 116)
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
        Me.OKButton.Location = New System.Drawing.Point(850, 33)
        Me.OKButton.Name = "OKButton"
        Me.OKButton.Size = New System.Drawing.Size(142, 42)
        Me.OKButton.TabIndex = 10
        Me.OKButton.Text = "OK"
        Me.OKButton.UseVisualStyleBackColor = True
        '
        'Cancel_Button
        '
        Me.Cancel_Button.Location = New System.Drawing.Point(850, 83)
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
        Me.ClientSize = New System.Drawing.Size(1008, 841)
        Me.Controls.Add(Me.Cancel_Button)
        Me.Controls.Add(Me.OKButton)
        Me.Controls.Add(Me.GroupBox6)
        Me.Controls.Add(Me.GroupBox4)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.CameraGroup)
        Me.KeyPreview = True
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.MinimizeBox = False
        Me.Name = "OptionsDialog"
        Me.ShowInTaskbar = False
        Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show
        Me.Text = "OpenCVB Global Settings"
        Me.CameraGroup.ResumeLayout(False)
        Me.CameraGroup.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        CType(Me.RefreshRate, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        CType(Me.TestAllDuration, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox6.ResumeLayout(False)
        Me.GroupBox6.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents CameraGroup As GroupBox
    Friend WithEvents SnapToGrid As CheckBox
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents AccurateProcessing As RadioButton
    Friend WithEvents lowResolution As RadioButton
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents ShowLabels As CheckBox
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
    Friend WithEvents Label3 As Label
    Friend WithEvents RefreshRate As NumericUpDown
End Class
