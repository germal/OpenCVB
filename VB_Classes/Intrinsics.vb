Imports cv = OpenCvSharp
Public Class Intrinsics_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Show the depth camera intrinsics."
        ocvb.label2 = "ppx/ppy location"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim ttStart = 40

        Dim nextLine = "Width = " + CStr(ocvb.parms.intrinsics.width) + vbTab + " height = " + CStr(ocvb.parms.intrinsics.height)
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart))

        nextLine = "fx = " + CStr(ocvb.parms.intrinsics.fx) + vbTab + " fy = " + CStr(ocvb.parms.intrinsics.fy)
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart + 30))

        nextLine = "ppx = " + CStr(ocvb.parms.intrinsics.ppx) + vbTab + " ppy = " + CStr(ocvb.parms.intrinsics.ppy)
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart + 60))

        nextLine = ""
        For i = 0 To ocvb.parms.intrinsics.coeffs.Length - 1
            nextLine += "coeffs(" + CStr(i) + ") = " + Format(ocvb.parms.intrinsics.coeffs(i), "#0.000") + "   "
        Next
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart + 90))

        nextLine = "FOV in x = " + CStr(ocvb.parms.intrinsics.FOV(0)) + vbTab + "FOV in y = " + CStr(ocvb.parms.intrinsics.FOV(1))
        ocvb.putText(New ActiveClass.TrueType(nextLine, 10, ttStart + 120))

        ocvb.result2.SetTo(0)
        ocvb.result2.Line(New cv.Point(ocvb.color.Width / 2, 0), New cv.Point(ocvb.color.Width / 2, ocvb.color.Height), cv.Scalar.White, 1)
        ocvb.result2.Line(New cv.Point(0, ocvb.color.Height / 2), New cv.Point(ocvb.color.Width, ocvb.color.Height / 2), cv.Scalar.White, 1)

        nextLine = "(" + CStr(ocvb.parms.intrinsics.ppx) + ", " + CStr(ocvb.parms.intrinsics.ppy) + ")"
        Dim ttLocation = New cv.Point(CInt(ocvb.parms.imageToTrueTypeLoc * ocvb.color.Width / 2) + 20, CInt(ocvb.parms.imageToTrueTypeLoc * ocvb.color.Height / 2) + 20)
        ocvb.putText(New ActiveClass.TrueType(nextLine, ttLocation.X, ttLocation.Y, RESULT2))

        Dim ptLoc = New cv.Point(ocvb.color.Width / 2 + 4, ocvb.color.Height / 2 + 4)
        ocvb.result2.Line(ptLoc, New cv.Point(ttLocation.X / ocvb.parms.imageToTrueTypeLoc, ttLocation.Y / ocvb.parms.imageToTrueTypeLoc), cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
