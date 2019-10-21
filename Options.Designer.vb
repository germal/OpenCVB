<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsForm
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
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.AccurateProcessing = New System.Windows.Forms.RadioButton()
        Me.FastProcessing = New System.Windows.Forms.RadioButton()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.ShowLabels = New System.Windows.Forms.CheckBox()
        Me.MinimizeMemoryFootprint = New System.Windows.Forms.CheckBox()
        Me.OKbutton = New System.Windows.Forms.Button()
        Me.Cancel = New System.Windows.Forms.Button()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.TestAllDuration = New System.Windows.Forms.NumericUpDown()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.GroupBox5 = New System.Windows.Forms.GroupBox()
        Me.SnapToGrid = New System.Windows.Forms.CheckBox()
        Me.Kinect4Azure = New System.Windows.Forms.RadioButton()
        Me.IntelCamera = New System.Windows.Forms.RadioButton()
        Me.GroupBox6 = New System.Windows.Forms.GroupBox()
        Me.PythonExes = New System.Windows.Forms.ComboBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Filters = New System.Windows.Forms.GroupBox()
        Me.DisparityToDepth = New System.Windows.Forms.CheckBox()
        Me.HoleFillingFilter = New System.Windows.Forms.CheckBox()
        Me.TemporalFilter = New System.Windows.Forms.CheckBox()
        Me.SpatialFilter = New System.Windows.Forms.CheckBox()
        Me.DepthToDisparity = New System.Windows.Forms.CheckBox()
        Me.ThresholdFilter = New System.Windows.Forms.CheckBox()
        Me.DecimationFilter = New System.Windows.Forms.CheckBox()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        CType(Me.TestAllDuration, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox5.SuspendLayout()
        Me.GroupBox6.SuspendLayout()
        Me.Filters.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.AccurateProcessing)
        Me.GroupBox1.Controls.Add(Me.FastProcessing)
        Me.GroupBox1.Location = New System.Drawing.Point(12, 199)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(933, 128)
        Me.GroupBox1.TabIndex = 0
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
        'FastProcessing
        '
        Me.FastProcessing.AutoSize = True
        Me.FastProcessing.Location = New System.Drawing.Point(16, 40)
        Me.FastProcessing.Name = "FastProcessing"
        Me.FastProcessing.Size = New System.Drawing.Size(482, 24)
        Me.FastProcessing.TabIndex = 0
        Me.FastProcessing.TabStop = True
        Me.FastProcessing.Text = "Run algorithm after resizing image to 640x360 (FastProcessing)"
        Me.FastProcessing.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.ShowLabels)
        Me.GroupBox2.Controls.Add(Me.MinimizeMemoryFootprint)
        Me.GroupBox2.Location = New System.Drawing.Point(15, 333)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(933, 125)
        Me.GroupBox2.TabIndex = 1
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Memory"
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
        'OKbutton
        '
        Me.OKbutton.Location = New System.Drawing.Point(794, 22)
        Me.OKbutton.Name = "OKbutton"
        Me.OKbutton.Size = New System.Drawing.Size(151, 40)
        Me.OKbutton.TabIndex = 3
        Me.OKbutton.Text = "OK"
        Me.OKbutton.UseVisualStyleBackColor = True
        '
        'Cancel
        '
        Me.Cancel.Location = New System.Drawing.Point(794, 68)
        Me.Cancel.Name = "Cancel"
        Me.Cancel.Size = New System.Drawing.Size(151, 40)
        Me.Cancel.TabIndex = 4
        Me.Cancel.Text = "Cancel"
        Me.Cancel.UseVisualStyleBackColor = True
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.TestAllDuration)
        Me.GroupBox4.Controls.Add(Me.Label1)
        Me.GroupBox4.Location = New System.Drawing.Point(15, 746)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(933, 116)
        Me.GroupBox4.TabIndex = 3
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "Test All Options"
        '
        'TestAllDuration
        '
        Me.TestAllDuration.Location = New System.Drawing.Point(16, 50)
        Me.TestAllDuration.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.TestAllDuration.Name = "TestAllDuration"
        Me.TestAllDuration.ReadOnly = True
        Me.TestAllDuration.Size = New System.Drawing.Size(89, 26)
        Me.TestAllDuration.TabIndex = 2
        Me.TestAllDuration.Value = New Decimal(New Integer() {5, 0, 0, 0})
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(111, 52)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(405, 20)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Duration in seconds of each test when running ""Test All"""
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.SnapToGrid)
        Me.GroupBox5.Controls.Add(Me.Kinect4Azure)
        Me.GroupBox5.Controls.Add(Me.IntelCamera)
        Me.GroupBox5.Location = New System.Drawing.Point(12, 22)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(776, 160)
        Me.GroupBox5.TabIndex = 2
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
        Me.IntelCamera.Size = New System.Drawing.Size(222, 24)
        Me.IntelCamera.TabIndex = 0
        Me.IntelCamera.TabStop = True
        Me.IntelCamera.Text = "Use Intel D4xx 3D Camera"
        Me.IntelCamera.UseVisualStyleBackColor = True
        '
        'GroupBox6
        '
        Me.GroupBox6.Controls.Add(Me.PythonExes)
        Me.GroupBox6.Controls.Add(Me.Label2)
        Me.GroupBox6.Location = New System.Drawing.Point(12, 868)
        Me.GroupBox6.Name = "GroupBox6"
        Me.GroupBox6.Size = New System.Drawing.Size(933, 116)
        Me.GroupBox6.TabIndex = 5
        Me.GroupBox6.TabStop = False
        Me.GroupBox6.Text = "Python "
        '
        'PythonExes
        '
        Me.PythonExes.FormattingEnabled = True
        Me.PythonExes.Location = New System.Drawing.Point(15, 65)
        Me.PythonExes.Name = "PythonExes"
        Me.PythonExes.Size = New System.Drawing.Size(912, 28)
        Me.PythonExes.TabIndex = 2
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
        'Filters
        '
        Me.Filters.Controls.Add(Me.DisparityToDepth)
        Me.Filters.Controls.Add(Me.HoleFillingFilter)
        Me.Filters.Controls.Add(Me.TemporalFilter)
        Me.Filters.Controls.Add(Me.SpatialFilter)
        Me.Filters.Controls.Add(Me.DepthToDisparity)
        Me.Filters.Controls.Add(Me.ThresholdFilter)
        Me.Filters.Controls.Add(Me.DecimationFilter)
        Me.Filters.Location = New System.Drawing.Point(15, 464)
        Me.Filters.Name = "Filters"
        Me.Filters.Size = New System.Drawing.Size(933, 274)
        Me.Filters.TabIndex = 6
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
        'OptionsForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(960, 1009)
        Me.Controls.Add(Me.Filters)
        Me.Controls.Add(Me.GroupBox6)
        Me.Controls.Add(Me.GroupBox5)
        Me.Controls.Add(Me.GroupBox4)
        Me.Controls.Add(Me.Cancel)
        Me.Controls.Add(Me.OKbutton)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.KeyPreview = True
        Me.Name = "OptionsForm"
        Me.Text = "Options"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        CType(Me.TestAllDuration, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox5.PerformLayout()
        Me.GroupBox6.ResumeLayout(False)
        Me.GroupBox6.PerformLayout()
        Me.Filters.ResumeLayout(False)
        Me.Filters.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents AccurateProcessing As RadioButton
    Friend WithEvents FastProcessing As RadioButton
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents MinimizeMemoryFootprint As CheckBox
    Friend WithEvents ShowLabels As CheckBox
    Friend WithEvents OKbutton As Button
    Friend WithEvents Cancel As Button
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents Label1 As Label
    Friend WithEvents TestAllDuration As NumericUpDown
    Friend WithEvents GroupBox5 As GroupBox
    Friend WithEvents Kinect4Azure As RadioButton
    Friend WithEvents IntelCamera As RadioButton
    Friend WithEvents SnapToGrid As CheckBox
    Friend WithEvents GroupBox6 As GroupBox
    Friend WithEvents PythonExes As ComboBox
    Friend WithEvents Label2 As Label
    Friend WithEvents Filters As GroupBox
    Friend WithEvents DisparityToDepth As CheckBox
    Friend WithEvents HoleFillingFilter As CheckBox
    Friend WithEvents TemporalFilter As CheckBox
    Friend WithEvents SpatialFilter As CheckBox
    Friend WithEvents DepthToDisparity As CheckBox
    Friend WithEvents ThresholdFilter As CheckBox
    Friend WithEvents DecimationFilter As CheckBox
End Class
