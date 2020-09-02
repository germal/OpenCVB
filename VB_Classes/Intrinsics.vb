Imports cv = OpenCvSharp
Public Class intrinsicsLeft_Basics
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        desc = "Show the depth camera intrinsicsLeft."
        label2 = "ppx/ppy location"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim ttStart = 40
        Dim ttStr As String = "Width = " + CStr(ocvb.color.Width) + vbTab + " height = " + CStr(ocvb.color.Height) + vbCrLf
        ttStr += "fx = " + CStr(ocvb.intrinsicsLeft.fx) + vbTab + " fy = " + CStr(ocvb.intrinsicsLeft.fy) + vbCrLf
        ttStr += "ppx = " + CStr(ocvb.intrinsicsLeft.ppx) + vbTab + " ppy = " + CStr(ocvb.intrinsicsLeft.ppy) + vbCrLf + vbCrLf
        For i = 0 To ocvb.intrinsicsLeft.coeffs.Length - 1
            ttStr += "coeffs(" + CStr(i) + ") = " + Format(ocvb.intrinsicsLeft.coeffs(i), "#0.000") + "   " + vbCrLf
        Next
        If ocvb.intrinsicsLeft.FOV IsNot Nothing Then
            If ocvb.intrinsicsLeft.FOV(0) = 0 Then
                ttStr += "Approximate FOV in x = " + CStr(hFOVangles(ocvb.parms.cameraIndex)) + vbCrLf +
                         "Approximate FOV in y = " + CStr(vFOVangles(ocvb.parms.cameraIndex)) + vbCrLf
            Else
                ttStr += "FOV in x = " + CStr(ocvb.intrinsicsLeft.FOV(0)) + vbCrLf + "FOV in y = " + CStr(ocvb.intrinsicsLeft.FOV(1)) + vbCrLf
            End If
        Else
            ttStr += "Approximate FOV in x = " + CStr(hFOVangles(ocvb.parms.cameraIndex)) + vbCrLf +
                     "Approximate FOV in y = " + CStr(vFOVangles(ocvb.parms.cameraIndex)) + vbCrLf
        End If
        ocvb.trueText(New TTtext(ttStr, 10, 50))

        dst2.SetTo(0)
        dst2.Line(New cv.Point(src.Width / 2, 0), New cv.Point(src.Width / 2, src.Height), cv.Scalar.White, 1)
        dst2.Line(New cv.Point(0, src.Height / 2), New cv.Point(src.Width, src.Height / 2), cv.Scalar.White, 1)

        Dim nextline = "(" + CStr(ocvb.intrinsicsLeft.ppx) + ", " + CStr(ocvb.intrinsicsLeft.ppy) + ")"
        Dim ttLocation = New cv.Point(CInt(src.Width / 2) + 20, CInt(src.Height / 2) + 40)
        cv.Cv2.PutText(dst2, nextLine, ttLocation, cv.HersheyFonts.HersheyComplex, fontsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Dim ptLoc = New cv.Point(src.Width / 2 + 4, src.Height / 2 + 4)
        dst2.Line(ptLoc, ttLocation, cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
    End Sub
End Class

