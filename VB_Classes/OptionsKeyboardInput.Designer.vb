<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OptionsKeyboardInput
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
        Me.components = New System.ComponentModel.Container()
        Me.keyboardCheckbox = New System.Windows.Forms.CheckBox()
        Me.TextBox1 = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.HoldKeyTimer = New System.Windows.Forms.Timer(Me.components)
        Me.SuspendLayout()
        '
        'keyboardCheckbox
        '
        Me.keyboardCheckbox.AutoSize = True
        Me.keyboardCheckbox.Location = New System.Drawing.Point(20, 23)
        Me.keyboardCheckbox.Name = "keyboardCheckbox"
        Me.keyboardCheckbox.Size = New System.Drawing.Size(314, 24)
        Me.keyboardCheckbox.TabIndex = 0
        Me.keyboardCheckbox.Text = "Send all keyboard input to the algorithm"
        Me.keyboardCheckbox.UseVisualStyleBackColor = True
        '
        'TextBox1
        '
        Me.TextBox1.Location = New System.Drawing.Point(84, 64)
        Me.TextBox1.Name = "TextBox1"
        Me.TextBox1.Size = New System.Drawing.Size(103, 26)
        Me.TextBox1.TabIndex = 1
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(193, 70)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(372, 60)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "This textbox is used to receive all keyboard input." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Losing focus here will unche" &
    "ck the checkbox above." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10)
        '
        'HoldKeyTimer
        '
        Me.HoldKeyTimer.Interval = 10
        '
        'OptionsKeyboardInput
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(841, 277)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.TextBox1)
        Me.Controls.Add(Me.keyboardCheckbox)
        Me.Name = "OptionsKeyboardInput"
        Me.Text = "Form1"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents keyboardCheckbox As Windows.Forms.CheckBox
    Friend WithEvents TextBox1 As Windows.Forms.TextBox
    Friend WithEvents Label1 As Windows.Forms.Label
    Friend WithEvents HoldKeyTimer As Windows.Forms.Timer
End Class
