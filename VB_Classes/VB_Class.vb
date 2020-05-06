Imports cv = OpenCvSharp
Public Class VB_Class : Implements IDisposable
    Public Class TrueType
        Public Const _RESULT1 = RESULT1
        Public Const _RESULT2 = RESULT2
        Public text As String

        ' Change the default TrueType font and font size here.  Any individual VB_Class can override with a specific font if needed.
        ' To use the global TrueType font, specify _fontName = ocvb.fontName and _fontSize = ocvb.fontSize on the TrueType constructor.
        Const defaultFont = "Microsoft Sans Serif"
        Const defaultFontSize = 8

        Public fontName As String = defaultFont
        Public fontSize As Double = defaultFontSize
        Public picTag As Int32
        Public x As Int32
        Public y As Int32
        Public Sub New(_text As String, _x As Int32, _y As Int32, Optional _fontName As String = defaultFont,
                       Optional _fontSize As Double = defaultFontSize, Optional _picTag As Int32 = _RESULT1)
            text = _text
            x = _x
            y = _y
            fontName = _fontName
            fontSize = _fontSize
            picTag = _picTag
        End Sub
        Public Sub New(_text As String, _x As Int32, _y As Int32, _picTag As Int32)
            text = _text
            x = _x
            y = _y
            picTag = _picTag
        End Sub

        Public Sub New(_text As String, _x As Int32, _y As Int32)
            text = _text
            x = _x
            y = _y
            picTag = _RESULT1
        End Sub
    End Class

    Public sliders As New OptionsSliders
    Public sliders1 As New OptionsSliders
    Public sliders2 As New OptionsSliders
    Public sliders3 As New OptionsSliders
    Public check As New OptionsCheckbox
    Public callerName As String
    Public radio As New OptionsRadioButtons
    Public radio1 As New OptionsRadioButtons
    Public videoOptions As New OptionsVideoName
    Dim classInheritor As Object
    Public Sub New()
        classInheritor = Me
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Dim type = classInheritor.GetType
        If type.GetProperty("MyDispose") IsNot Nothing Then classInheritor.MyDispose()  ' dispose of any managed and unmanaged classes.
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
