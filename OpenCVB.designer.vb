<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OpenCVB
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
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(OpenCVB))
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip()
        Me.ToolStripButton1 = New System.Windows.Forms.ToolStripButton()
        Me.testAllButton = New System.Windows.Forms.ToolStripButton()
        Me.Options = New System.Windows.Forms.ToolStripButton()
        Me.RecordButton = New System.Windows.Forms.ToolStripButton()
        Me.AvailableAlgorithms = New System.Windows.Forms.ComboBox()
        Me.TestAllTimer = New System.Windows.Forms.Timer(Me.components)
        Me.ActivateTimer = New System.Windows.Forms.Timer(Me.components)
        Me.fpsTimer = New System.Windows.Forms.Timer(Me.components)
        Me.AlgorithmDesc = New System.Windows.Forms.Label()
        Me.OpenCVkeyword = New System.Windows.Forms.ComboBox()
        Me.RestartCameraTimer = New System.Windows.Forms.Timer(Me.components)
        Me.ToolStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'ToolStrip1
        '
        Me.ToolStrip1.AutoSize = False
        Me.ToolStrip1.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripButton1, Me.testAllButton, Me.Options, Me.RecordButton})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 0)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Size = New System.Drawing.Size(1779, 40)
        Me.ToolStrip1.TabIndex = 0
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'ToolStripButton1
        '
        Me.ToolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.ToolStripButton1.Image = CType(resources.GetObject("ToolStripButton1.Image"), System.Drawing.Image)
        Me.ToolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripButton1.Name = "ToolStripButton1"
        Me.ToolStripButton1.Size = New System.Drawing.Size(34, 35)
        Me.ToolStripButton1.Text = "ToolStripButton1"
        '
        'testAllButton
        '
        Me.testAllButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.testAllButton.Image = CType(resources.GetObject("testAllButton.Image"), System.Drawing.Image)
        Me.testAllButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.testAllButton.Name = "testAllButton"
        Me.testAllButton.Size = New System.Drawing.Size(71, 35)
        Me.testAllButton.Text = "Test All"
        '
        'Options
        '
        Me.Options.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.Options.Image = CType(resources.GetObject("Options.Image"), System.Drawing.Image)
        Me.Options.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.Options.Name = "Options"
        Me.Options.Size = New System.Drawing.Size(80, 35)
        Me.Options.Text = "Options"
        '
        'RecordButton
        '
        Me.RecordButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.RecordButton.Image = CType(resources.GetObject("RecordButton.Image"), System.Drawing.Image)
        Me.RecordButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.RecordButton.Name = "RecordButton"
        Me.RecordButton.Size = New System.Drawing.Size(71, 35)
        Me.RecordButton.Text = "Record"
        Me.RecordButton.Visible = False
        '
        'AvailableAlgorithms
        '
        Me.AvailableAlgorithms.FormattingEnabled = True
        Me.AvailableAlgorithms.Location = New System.Drawing.Point(218, 3)
        Me.AvailableAlgorithms.Name = "AvailableAlgorithms"
        Me.AvailableAlgorithms.Size = New System.Drawing.Size(354, 28)
        Me.AvailableAlgorithms.TabIndex = 1
        '
        'TestAllTimer
        '
        Me.TestAllTimer.Interval = 5000
        '
        'ActivateTimer
        '
        '
        'fpsTimer
        '
        Me.fpsTimer.Interval = 1000
        '
        'AlgorithmDesc
        '
        Me.AlgorithmDesc.AutoSize = True
        Me.AlgorithmDesc.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.AlgorithmDesc.Location = New System.Drawing.Point(912, 6)
        Me.AlgorithmDesc.Name = "AlgorithmDesc"
        Me.AlgorithmDesc.Size = New System.Drawing.Size(119, 22)
        Me.AlgorithmDesc.TabIndex = 2
        Me.AlgorithmDesc.Text = "Algorithm Desc"
        '
        'OpenCVkeyword
        '
        Me.OpenCVkeyword.FormattingEnabled = True
        Me.OpenCVkeyword.Location = New System.Drawing.Point(578, 3)
        Me.OpenCVkeyword.Name = "OpenCVkeyword"
        Me.OpenCVkeyword.Size = New System.Drawing.Size(328, 28)
        Me.OpenCVkeyword.TabIndex = 3
        '
        'RestartCameraTimer
        '
        Me.RestartCameraTimer.Interval = 1000
        '
        'OpenCVB
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1779, 1411)
        Me.Controls.Add(Me.OpenCVkeyword)
        Me.Controls.Add(Me.AlgorithmDesc)
        Me.Controls.Add(Me.AvailableAlgorithms)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Name = "OpenCVB"
        Me.Text = "OpenCVB"
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents AvailableAlgorithms As ComboBox
    Friend WithEvents testAllButton As ToolStripButton
    Friend WithEvents TestAllTimer As Timer
    Friend WithEvents ActivateTimer As Timer
    Friend WithEvents fpsTimer As Timer
    Friend WithEvents AlgorithmDesc As Label
    Friend WithEvents Options As ToolStripButton
    Friend WithEvents ToolStripButton1 As ToolStripButton
    Friend WithEvents RecordButton As ToolStripButton
    Friend WithEvents OpenCVkeyword As ComboBox
    Friend WithEvents RestartCameraTimer As Timer
End Class
