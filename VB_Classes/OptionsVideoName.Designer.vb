<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsVideoName
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
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.Filename = New System.Windows.Forms.TextBox()
        Me.TrackBar1 = New System.Windows.Forms.TrackBar()
        Me.FrameNumber = New System.Windows.Forms.Label()
        CType(Me.TrackBar1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'Filename
        '
        Me.Filename.Location = New System.Drawing.Point(57, 17)
        Me.Filename.Name = "Filename"
        Me.Filename.Size = New System.Drawing.Size(710, 26)
        Me.Filename.TabIndex = 0
        '
        'TrackBar1
        '
        Me.TrackBar1.Location = New System.Drawing.Point(57, 49)
        Me.TrackBar1.Name = "TrackBar1"
        Me.TrackBar1.Size = New System.Drawing.Size(710, 69)
        Me.TrackBar1.TabIndex = 1
        '
        'FrameNumber
        '
        Me.FrameNumber.AutoSize = True
        Me.FrameNumber.Location = New System.Drawing.Point(64, 112)
        Me.FrameNumber.Name = "FrameNumber"
        Me.FrameNumber.Size = New System.Drawing.Size(111, 20)
        Me.FrameNumber.TabIndex = 2
        Me.FrameNumber.Text = "FrameNumber"
        '
        'OptionsVideoName
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(822, 168)
        Me.Controls.Add(Me.FrameNumber)
        Me.Controls.Add(Me.TrackBar1)
        Me.Controls.Add(Me.Filename)
        Me.Name = "OptionsVideoName"
        Me.Text = "OptionsFilename"
        CType(Me.TrackBar1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents OpenFileDialog1 As Windows.Forms.OpenFileDialog
    Friend WithEvents Filename As Windows.Forms.TextBox
    Friend WithEvents TrackBar1 As Windows.Forms.TrackBar
    Friend WithEvents FrameNumber As Windows.Forms.Label
End Class
