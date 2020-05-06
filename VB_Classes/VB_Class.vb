Imports cv = OpenCvSharp
Public Class VB_Class : Implements IDisposable
    Public sliders As New OptionsSliders
    Public sliders1 As New OptionsSliders
    Public sliders2 As New OptionsSliders
    Public sliders3 As New OptionsSliders
    Public check As New OptionsCheckbox
    Public callerName As String
    Public radio As New OptionsRadioButtons
    Public radio1 As New OptionsRadioButtons
    Public videoOptions As New OptionsVideoName
    Public Sub New()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        sliders1.Dispose()
        sliders2.Dispose()
        sliders3.Dispose()
        check.Dispose()
        radio.Dispose()
        radio1.Dispose()
        videoOptions.Dispose()
    End Sub
End Class
