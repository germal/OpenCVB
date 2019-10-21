<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsRecordPlayback
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
        Me.Filename = New System.Windows.Forms.TextBox()
        Me.startButton = New System.Windows.Forms.Button()
        Me.BytesMovedTrackbar = New System.Windows.Forms.TrackBar()
        Me.Label1 = New System.Windows.Forms.Label()
        CType(Me.BytesMovedTrackbar, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Filename
        '
        Me.Filename.Location = New System.Drawing.Point(44, 35)
        Me.Filename.Name = "Filename"
        Me.Filename.Size = New System.Drawing.Size(710, 26)
        Me.Filename.TabIndex = 1
        '
        'startButton
        '
        Me.startButton.Location = New System.Drawing.Point(44, 159)
        Me.startButton.Name = "startButton"
        Me.startButton.Size = New System.Drawing.Size(153, 39)
        Me.startButton.TabIndex = 3
        Me.startButton.Text = "Start Recording"
        Me.startButton.UseVisualStyleBackColor = True
        '
        'BytesMovedTrackbar
        '
        Me.BytesMovedTrackbar.Location = New System.Drawing.Point(31, 84)
        Me.BytesMovedTrackbar.Maximum = 20000
        Me.BytesMovedTrackbar.Name = "BytesMovedTrackbar"
        Me.BytesMovedTrackbar.Size = New System.Drawing.Size(733, 69)
        Me.BytesMovedTrackbar.TabIndex = 4
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(770, 74)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(115, 20)
        Me.Label1.TabIndex = 5
        Me.Label1.Text = "20 Gbytes Max"
        '
        'OptionsRecordPlayback
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(936, 229)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.BytesMovedTrackbar)
        Me.Controls.Add(Me.startButton)
        Me.Controls.Add(Me.Filename)
        Me.Name = "OptionsRecordPlayback"
        Me.Text = "OptionsRecordPlayback"
        CType(Me.BytesMovedTrackbar, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents Filename As Windows.Forms.TextBox
    Friend WithEvents startButton As Windows.Forms.Button
    Friend WithEvents BytesMovedTrackbar As Windows.Forms.TrackBar
    Friend WithEvents Label1 As Windows.Forms.Label
End Class
