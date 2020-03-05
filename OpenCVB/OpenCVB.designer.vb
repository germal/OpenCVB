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
        Me.PausePlayButton = New System.Windows.Forms.ToolStripButton()
        Me.OptionsButton = New System.Windows.Forms.ToolStripButton()
        Me.TestAllButton = New System.Windows.Forms.ToolStripButton()
        Me.SnapShotButton = New System.Windows.Forms.ToolStripButton()
        Me.AvailableAlgorithms = New System.Windows.Forms.ComboBox()
        Me.TestAllTimer = New System.Windows.Forms.Timer(Me.components)
        Me.ActivateTimer = New System.Windows.Forms.Timer(Me.components)
        Me.fpsTimer = New System.Windows.Forms.Timer(Me.components)
        Me.AlgorithmDesc = New System.Windows.Forms.Label()
        Me.OpenCVkeyword = New System.Windows.Forms.ComboBox()
        Me.RestartCameraTimer = New System.Windows.Forms.Timer(Me.components)
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.RefreshTimer = New System.Windows.Forms.Timer(Me.components)
        Me.ToolStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'ToolStrip1
        '
        Me.ToolStrip1.AutoSize = False
        Me.ToolStrip1.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.PausePlayButton, Me.OptionsButton, Me.TestAllButton, Me.SnapShotButton})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 0)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Padding = New System.Windows.Forms.Padding(0, 0, 3, 0)
        Me.ToolStrip1.Size = New System.Drawing.Size(1779, 58)
        Me.ToolStrip1.TabIndex = 0
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'PausePlayButton
        '
        Me.PausePlayButton.AutoToolTip = False
        Me.PausePlayButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.PausePlayButton.Image = CType(resources.GetObject("PausePlayButton.Image"), System.Drawing.Image)
        Me.PausePlayButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.PausePlayButton.Name = "PausePlayButton"
        Me.PausePlayButton.Size = New System.Drawing.Size(34, 53)
        Me.PausePlayButton.Text = "PausePlay"
        Me.PausePlayButton.ToolTipText = "Pause/Play"
        '
        'OptionsButton
        '
        Me.OptionsButton.AutoToolTip = False
        Me.OptionsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.OptionsButton.Image = CType(resources.GetObject("OptionsButton.Image"), System.Drawing.Image)
        Me.OptionsButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.OptionsButton.Name = "OptionsButton"
        Me.OptionsButton.Size = New System.Drawing.Size(34, 53)
        Me.OptionsButton.Text = "Options"
        Me.OptionsButton.ToolTipText = "Camera Settings and Global Options"
        '
        'TestAllButton
        '
        Me.TestAllButton.AutoToolTip = False
        Me.TestAllButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.TestAllButton.Image = CType(resources.GetObject("TestAllButton.Image"), System.Drawing.Image)
        Me.TestAllButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.TestAllButton.Name = "TestAllButton"
        Me.TestAllButton.Size = New System.Drawing.Size(34, 53)
        Me.TestAllButton.Text = "Test All"
        Me.TestAllButton.ToolTipText = "Test each algorithm in succession"
        '
        'SnapShotButton
        '
        Me.SnapShotButton.AutoToolTip = False
        Me.SnapShotButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
        Me.SnapShotButton.Image = CType(resources.GetObject("SnapShotButton.Image"), System.Drawing.Image)
        Me.SnapShotButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.SnapShotButton.Name = "SnapShotButton"
        Me.SnapShotButton.Size = New System.Drawing.Size(34, 53)
        Me.SnapShotButton.Text = "Snapshot"
        '
        'AvailableAlgorithms
        '
        Me.AvailableAlgorithms.FormattingEnabled = True
        Me.AvailableAlgorithms.Location = New System.Drawing.Point(194, 12)
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
        Me.AlgorithmDesc.Location = New System.Drawing.Point(888, 15)
        Me.AlgorithmDesc.Name = "AlgorithmDesc"
        Me.AlgorithmDesc.Size = New System.Drawing.Size(119, 22)
        Me.AlgorithmDesc.TabIndex = 2
        Me.AlgorithmDesc.Text = "Algorithm Desc"
        '
        'OpenCVkeyword
        '
        Me.OpenCVkeyword.FormattingEnabled = True
        Me.OpenCVkeyword.Location = New System.Drawing.Point(554, 12)
        Me.OpenCVkeyword.Name = "OpenCVkeyword"
        Me.OpenCVkeyword.Size = New System.Drawing.Size(328, 28)
        Me.OpenCVkeyword.TabIndex = 3
        '
        'RestartCameraTimer
        '
        Me.RestartCameraTimer.Interval = 1000
        '
        'RefreshTimer
        '
        Me.RefreshTimer.Enabled = True
        Me.RefreshTimer.Interval = 30
        '
        'OpenCVB
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1779, 1062)
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
    Friend WithEvents TestAllButton As ToolStripButton
    Friend WithEvents TestAllTimer As Timer
    Friend WithEvents ActivateTimer As Timer
    Friend WithEvents fpsTimer As Timer
    Friend WithEvents AlgorithmDesc As Label
    Friend WithEvents OptionsButton As ToolStripButton
    Friend WithEvents PausePlayButton As ToolStripButton
    Friend WithEvents SnapShotButton As ToolStripButton
    Friend WithEvents OpenCVkeyword As ComboBox
    Friend WithEvents RestartCameraTimer As Timer
    Friend WithEvents ToolTip1 As ToolTip
    Friend WithEvents RefreshTimer As Timer
End Class
