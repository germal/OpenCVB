Imports cv = OpenCvSharp
Public Class Font_OpenCV
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Display different font options available in OpenCV"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 30 Then Exit Sub
        Dim hersheyFont = Choose(ocvb.frameCount Mod 7 + 1, cv.HersheyFonts.HersheyComplex, cv.HersheyFonts.HersheyComplexSmall, cv.HersheyFonts.HersheyDuplex,
                                 cv.HersheyFonts.HersheyPlain, cv.HersheyFonts.HersheyScriptComplex, cv.HersheyFonts.HersheyScriptSimplex, cv.HersheyFonts.HersheySimplex,
                                 cv.HersheyFonts.HersheyTriplex, cv.HersheyFonts.Italic)
        Dim hersheyName = Choose(ocvb.frameCount Mod 7 + 1, "HersheyComplex", "HersheyComplexSmall", "HersheyDuplex", "HersheyPlain", "HersheyScriptComplex",
                                 "HersheyScriptSimplex", "HersheySimplex", "HersheyTriplex", "Italic")
        label1 = hersheyName
        label2 = "Italicized " + hersheyName
        dst1.SetTo(0)
        dst2.SetTo(0)
        For i = 1 To 10
            Dim size = 1.5 - i * 0.1
            cv.Cv2.PutText(dst1, hersheyName + " " + Format(size, "#0.0"), New cv.Point(10, 30 + i * 30), hersheyFont, size, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            Dim hersheyFontItalics = hersheyFont + cv.HersheyFonts.Italic
            cv.Cv2.PutText(dst2, hersheyName + " " + Format(size, "#0.0"), New cv.Point(10, 30 + i * 30), hersheyFontItalics, size, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class




Public Class Font_TrueType
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Display different TrueType fonts"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim fontSize = GetSetting("OpenCVB", "FontSize", "FontSize", 12)
        Dim fontName = GetSetting("OpenCVB", "FontName", "FontName", "Tahoma")
        ' get the font on every iteration because it could have changed.  This should be done in any algorithm using OptionsFont.
        ocvb.trueText(New TTtext("TrueType Font is currently set to " + fontName + " with size = " + CStr(fontSize) + vbCrLf +
                                   "Use the Settings button above to change the font name and size.", 10, 50))
    End Sub
End Class




Public Class Font_FlowText
    Inherits ocvbClass
    Public msgs As New List(Of String)
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Show TrueType text flowing through an image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            msgs.Add("-------------------------------------------------------------------------------------------------------------------")
            msgs.Add("To get text to flow across an image in any algorithm, add 'flow = new Font_FlowText(ocvb)' to the class constructor.")
            msgs.Add("Also optionally indicate if you want result1 or result2 for text (the default is result1.)")
            msgs.Add("Then in your Run method, add a line 'flow.msgs.add('your next line of text')' - for as many msgs as you need on each pass.")
            msgs.Add("Then at the end of your Run method, invoke flow.Run(ocvb)")
        End If
        Static lastCount As Int32
        Dim maxLines As Int32 = 21

        Dim firstLine = If(msgs.Count - maxLines < 0, 0, msgs.Count - maxLines)
        For i = firstLine To msgs.Count - 1
            ocvb.trueText(New TTtext(msgs(i), 10, (i - firstLine) * 15 + 20))
        Next

        If ocvb.color.Width > 1000 Then maxLines = 29 ' larger mat gets more lines.
        If msgs.Count >= maxLines Then
            Dim index As Int32
            For i = 0 To lastCount - maxLines - 1
                msgs.RemoveAt(index) ' maxlines was tested with the font specified above. 
                index += 1
            Next
        End If
        lastCount = msgs.Count
    End Sub
End Class
