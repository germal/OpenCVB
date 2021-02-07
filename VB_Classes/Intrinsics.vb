Imports cv = OpenCvSharp
Public Class intrinsicsLeft_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Show the depth camera intrinsicsLeft."
        label2 = "ppx/ppy location"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim ttStart = 40
        Dim ttStr As String = "Width = " + CStr(src.Width) + vbTab + " height = " + CStr(src.Height) + vbCrLf
        ttStr += "fx = " + CStr(ocvb.parms.intrinsicsLeft.fx) + vbTab + " fy = " + CStr(ocvb.parms.intrinsicsLeft.fy) + vbCrLf
        ttStr += "ppx = " + CStr(ocvb.parms.intrinsicsLeft.ppx) + vbTab + " ppy = " + CStr(ocvb.parms.intrinsicsLeft.ppy) + vbCrLf + vbCrLf
        For i = 0 To ocvb.parms.intrinsicsLeft.coeffs.Length - 1
            ttStr += "coeffs(" + CStr(i) + ") = " + Format(ocvb.parms.intrinsicsLeft.coeffs(i), "#0.000") + "   " + vbCrLf
        Next
        If ocvb.parms.intrinsicsLeft.FOV IsNot Nothing Then
            If ocvb.parms.intrinsicsLeft.FOV(0) = 0 Then
                ttStr += "Approximate FOV in x = " + CStr(ocvb.hFov) + vbCrLf +
                         "Approximate FOV in y = " + CStr(ocvb.vFov) + vbCrLf
            Else
                ttStr += "FOV in x = " + CStr(ocvb.parms.intrinsicsLeft.FOV(0)) + vbCrLf + "FOV in y = " + CStr(ocvb.parms.intrinsicsLeft.FOV(1)) + vbCrLf
            End If
        Else
            ttStr += "Approximate FOV in x = " + CStr(ocvb.hFov) + vbCrLf +
                     "Approximate FOV in y = " + CStr(ocvb.vFov) + vbCrLf
        End If
        ocvb.trueText(ttStr)

        dst2.SetTo(0)
        dst2.Line(New cv.Point(src.Width / 2, 0), New cv.Point(src.Width / 2, src.Height), cv.Scalar.White, 1)
        dst2.Line(New cv.Point(0, src.Height / 2), New cv.Point(src.Width, src.Height / 2), cv.Scalar.White, 1)

        Dim nextline = "(" + CStr(ocvb.parms.intrinsicsLeft.ppx) + ", " + CStr(ocvb.parms.intrinsicsLeft.ppy) + ")"
        Dim ttLocation = New cv.Point(CInt(src.Width / 2) + 20, CInt(src.Height / 2) + 40)
        cv.Cv2.PutText(dst2, nextline, ttLocation, ocvb.font, ocvb.fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Dim ptLoc = New cv.Point(src.Width / 2 + 4, src.Height / 2 + 4)
        dst2.Line(ptLoc, ttLocation, cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
    End Sub
End Class


