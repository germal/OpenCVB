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
        Me.Filename.Location = New System.Drawing.Point(29, 23)
        Me.Filename.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.Filename.Name = "Filename"
        Me.Filename.Size = New System.Drawing.Size(475, 20)
        Me.Filename.TabIndex = 1
        '
        'startButton
        '
        Me.startButton.Location = New System.Drawing.Point(29, 103)
        Me.startButton.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.startButton.Name = "startButton"
        Me.startButton.Size = New System.Drawing.Size(102, 25)
        Me.startButton.TabIndex = 3
        Me.startButton.Text = "Start Recording"
        Me.startButton.UseVisualStyleBackColor = True
        '
        'BytesMovedTrackbar
        '
        Me.BytesMovedTrackbar.Location = New System.Drawing.Point(21, 55)
        Me.BytesMovedTrackbar.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.BytesMovedTrackbar.Maximum = 20000
        Me.BytesMovedTrackbar.Name = "BytesMovedTrackbar"
        Me.BytesMovedTrackbar.Size = New System.Drawing.Size(489, 45)
        Me.BytesMovedTrackbar.TabIndex = 4
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(513, 48)
        Me.Label1.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(78, 13)
        Me.Label1.TabIndex = 5
        Me.Label1.Text = "20 Gbytes Max"
        '
        'OptionsRecordPlayback
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(624, 149)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.BytesMovedTrackbar)
        Me.Controls.Add(Me.startButton)
        Me.Controls.Add(Me.Filename)
        Me.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
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
