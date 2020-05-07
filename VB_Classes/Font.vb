Imports cv = OpenCvSharp
Public Class Font_OpenCV
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Display different font options available in OpenCV"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 30 Then Exit Sub
        ocvb.result1.SetTo(0)
        ocvb.result2.SetTo(0)
        Dim hersheyFont = Choose(ocvb.frameCount Mod 7 + 1, cv.HersheyFonts.HersheyComplex, cv.HersheyFonts.HersheyComplexSmall, cv.HersheyFonts.HersheyDuplex,
                                 cv.HersheyFonts.HersheyPlain, cv.HersheyFonts.HersheyScriptComplex, cv.HersheyFonts.HersheyScriptSimplex, cv.HersheyFonts.HersheySimplex,
                                 cv.HersheyFonts.HersheyTriplex, cv.HersheyFonts.Italic)
        Dim hersheyName = Choose(ocvb.frameCount Mod 7 + 1, "HersheyComplex", "HersheyComplexSmall", "HersheyDuplex", "HersheyPlain", "HersheyScriptComplex",
                                 "HersheyScriptSimplex", "HersheySimplex", "HersheyTriplex", "Italic")
        ocvb.label1 = hersheyName
        ocvb.label2 = "Italicized " + hersheyName
        For i = 1 To 10
            Dim size = 1.5 - i * 0.1
            cv.Cv2.PutText(ocvb.result1, hersheyName + " " + Format(size, "#0.0"), New cv.Point(10, 30 + i * 30), hersheyFont, size, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            Dim hersheyFontItalics = hersheyFont + cv.HersheyFonts.Italic
            cv.Cv2.PutText(ocvb.result2, hersheyName + " " + Format(size, "#0.0"), New cv.Point(10, 30 + i * 30), hersheyFontItalics, size, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class




Public Class Font_TrueType
    Inherits ocvbClass
    Dim font As New OptionsFont
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        If ocvb.parms.ShowOptions Then font.Show()
        ocvb.desc = "Display different TrueType fonts"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ' get the font on every iteration because it could have changed.  This should be done in any algorithm using OptionsFont.
        ocvb.fontSize = GetSetting("OpenCVB", "FontSize", "FontSize", 12)
        ocvb.fontName = GetSetting("OpenCVB", "FontName", "FontName", "Tahoma")
        ocvb.putText(New ActiveClass.TrueType("TrueType Font Example (override default font) = Times New Roman with size 10" + vbCrLf +
                                              "Use 'Change' button in the font dialog below to set a global font: " + ocvb.fontName + vbCrLf +
                                              "Global TrueType Font = " + ocvb.fontName + " with size " + CStr(ocvb.fontSize) + vbCrLf +
                                              "Use 'ocvb.putText' with 'ocvb.fontName' and 'ocvb.fontSize' to exploit global font.",
                                              10, 50, "Times New Roman", 10, RESULT1))
    End Sub
    Public Sub MyDispose()
        font.Dispose()
    End Sub
End Class




Public Class Font_FlowText
    Inherits ocvbClass
    Public msgs As New List(Of String)
    Public externalUse As Boolean
    Public result1or2 As Int32 = RESULT1
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Show TrueType text flowing through an image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False And ocvb.frameCount = 0 Then
            msgs.Add("To get text to flow across an image in any other class, add flow = new Font_FlowText(ocvb) to your class constructor.")
            msgs.Add("Also in your constructor, update flow.externalUse = true and optionally indicate if you want result1 or result2 for text.")
            msgs.Add("Then in your Run method, flow.msgs.add('your next line of text') - for as many msgs as you need on each pass.")
            msgs.Add("Then at the end of your Run method, invoke flow.Run(ocvb)")
        Else
            If result1or2 = RESULT1 Then ocvb.result1.SetTo(0) Else ocvb.result2.SetTo(0)
        End If

        For i = 0 To msgs.Count - 1
            ocvb.putText(New ActiveClass.TrueType(msgs(i), 10, (i + 1) * 15 + 10, "Microsoft Sans Serif", 8, result1or2))
        Next

        Static lastCount As Int32
        Dim maxLines As Int32 = 21
        If ocvb.color.Height = 480 Or ocvb.color.Height = 240 Then maxLines = 29
        If msgs.Count > maxLines Then
            Dim index As Int32
            For i = lastCount To maxLines - 1 Step -1
                msgs.RemoveAt(index) ' maxlines was tested with the font specified above. 
                index += 1
            Next
        End If
        lastCount = msgs.Count
    End Sub
End Class