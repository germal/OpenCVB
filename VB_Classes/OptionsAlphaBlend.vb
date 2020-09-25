Public Class OptionsAlphaBlend
    Protected Overloads Overrides ReadOnly Property ShowWithoutActivation() As Boolean
        Get
            Return True
        End Get
    End Property
    Private Sub OptionsAlphaBlend_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.SetDesktopLocation(optionLocation.X, optionLocation.Y)
    End Sub
End Class
