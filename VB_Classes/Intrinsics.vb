Imports cv = OpenCvSharp
Public Class intrinsicsLeft_Basics
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        ocvb.desc = "Show the depth camera intrinsicsLeft."
        ocvb.label2 = "ppx/ppy location"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim ttStart = 40

        Dim nextLine = "Width = " + CStr(ocvb.parms.intrinsicsLeft.width) + vbTab + " height = " + CStr(ocvb.parms.intrinsicsLeft.height)
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart))

        nextLine = "fx = " + CStr(ocvb.parms.intrinsicsLeft.fx) + vbTab + " fy = " + CStr(ocvb.parms.intrinsicsLeft.fy)
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart + 30))

        nextLine = "ppx = " + CStr(ocvb.parms.intrinsicsLeft.ppx) + vbTab + " ppy = " + CStr(ocvb.parms.intrinsicsLeft.ppy)
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart + 60))

        nextLine = ""
        For i = 0 To ocvb.parms.intrinsicsLeft.coeffs.Length - 1
            nextLine += "coeffs(" + CStr(i) + ") = " + Format(ocvb.parms.intrinsicsLeft.coeffs(i), "#0.000") + "   "
        Next
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart + 90))

        If ocvb.parms.intrinsicsLeft.FOV IsNot Nothing Then
            nextLine = "FOV in x = " + CStr(ocvb.parms.intrinsicsLeft.FOV(0)) + vbTab + "FOV in y = " + CStr(ocvb.parms.intrinsicsLeft.FOV(1))
        End If
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart + 120))

        ocvb.result2.SetTo(0)
        ocvb.result2.Line(New cv.Point(ocvb.color.Width / 2, 0), New cv.Point(ocvb.color.Width / 2, ocvb.color.Height), cv.Scalar.White, 1)
        ocvb.result2.Line(New cv.Point(0, ocvb.color.Height / 2), New cv.Point(ocvb.color.Width, ocvb.color.Height / 2), cv.Scalar.White, 1)

        nextLine = "(" + CStr(ocvb.parms.intrinsicsLeft.ppx) + ", " + CStr(ocvb.parms.intrinsicsLeft.ppy) + ")"
        Dim ttLocation = New cv.Point(CInt(ocvb.parms.imageToTrueTypeLoc * ocvb.color.Width / 2) + 20, CInt(ocvb.parms.imageToTrueTypeLoc * ocvb.color.Height / 2) + 20)
        ocvb.putText(New ActiveClass.TrueType(nextLine, ttLocation.X, ttLocation.Y, RESULT2))

        Dim ptLoc = New cv.Point(ocvb.color.Width / 2 + 4, ocvb.color.Height / 2 + 4)
        ocvb.result2.Line(ptLoc, New cv.Point(ttLocation.X / ocvb.parms.imageToTrueTypeLoc, ttLocation.Y / ocvb.parms.imageToTrueTypeLoc), cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
    End Sub
End Class
