<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsAudio
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
        Me.BytesMovedTrackbar = New System.Windows.Forms.TrackBar()
        Me.Filename = New System.Windows.Forms.TextBox()
        Me.PlayButton = New System.Windows.Forms.Button()
        CType(Me.BytesMovedTrackbar, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'BytesMovedTrackbar
        '
        Me.BytesMovedTrackbar.Location = New System.Drawing.Point(25, 74)
        Me.BytesMovedTrackbar.Maximum = 20000
        Me.BytesMovedTrackbar.Name = "BytesMovedTrackbar"
        Me.BytesMovedTrackbar.Size = New System.Drawing.Size(734, 69)
        Me.BytesMovedTrackbar.TabIndex = 6
        '
        'Filename
        '
        Me.Filename.Location = New System.Drawing.Point(37, 24)
        Me.Filename.Name = "Filename"
        Me.Filename.Size = New System.Drawing.Size(710, 26)
        Me.Filename.TabIndex = 5
        '
        'PlayButton
        '
        Me.PlayButton.Location = New System.Drawing.Point(37, 134)
        Me.PlayButton.Name = "PlayButton"
        Me.PlayButton.Size = New System.Drawing.Size(105, 42)
        Me.PlayButton.TabIndex = 7
        Me.PlayButton.Text = "Play"
        Me.PlayButton.UseVisualStyleBackColor = True
        '
        'OptionsAudio
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(949, 241)
        Me.Controls.Add(Me.PlayButton)
        Me.Controls.Add(Me.BytesMovedTrackbar)
        Me.Controls.Add(Me.Filename)
        Me.Name = "OptionsAudio"
        Me.Text = "OptionsAudio"
        CType(Me.BytesMovedTrackbar, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents BytesMovedTrackbar As Windows.Forms.TrackBar
    Friend WithEvents Filename As Windows.Forms.TextBox
    Friend WithEvents PlayButton As Windows.Forms.Button
End Class
