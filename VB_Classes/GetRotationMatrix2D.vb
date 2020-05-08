Imports cv = OpenCvSharp
Module GetRotationMatrix
    Public Sub SetInterpolationRadioButtons(ocvb As AlgorithmData, caller As String, radio As OptionsRadioButtons, radioName As String)
        radio.Setup(ocvb, caller, 7)
        radio.check(0).Text = radioName + " with Area"
        radio.check(1).Text = radioName + " with Cubic flag"
        radio.check(2).Text = radioName + " with Lanczos4"
        radio.check(3).Text = radioName + " with Linear"
        radio.check(4).Text = radioName + " with Nearest"
        radio.check(5).Text = radioName + " with WarpFillOutliers"
        radio.check(6).Text = radioName + " with WarpInverseMap"
        radio.check(3).Checked = True
    End Sub
    Public Function getInterpolationRadioButtons(radio As OptionsRadioButtons) As cv.InterpolationFlags
        Dim warpFlag As cv.InterpolationFlags
        For i = 0 To radio.check.Length - 1
            If radio.check(i).Checked Then
                warpFlag = Choose(i + 1, cv.InterpolationFlags.Area, cv.InterpolationFlags.Cubic, cv.InterpolationFlags.Lanczos4, cv.InterpolationFlags.Linear,
                                    cv.InterpolationFlags.Nearest, cv.InterpolationFlags.WarpFillOutliers, cv.InterpolationFlags.WarpInverseMap)
                Exit For
            End If
        Next
        Return warpFlag
    End Function
End Module






' https://www.programcreek.com/python/example/89459/cv2.getRotationMatrix2D
Public Class GetRotationMatrix2D_Basics
    Inherits ocvbClass
    Public src As New cv.Mat
        Public M As cv.Mat
    Public Mflip As cv.Mat
    Public warpFlag As Int32
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "GetRotationMatrix2D Angle", 0, 360, 24)
        SetInterpolationRadioButtons(ocvb, caller, radio, "Rotation2D")

        ocvb.desc = "Rotate a rectangle of a specified angle"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        if standalone Then src = ocvb.color
        warpFlag = getInterpolationRadioButtons(radio)

        Dim angle = sliders.TrackBar1.Value
        M = cv.Cv2.GetRotationMatrix2D(New cv.Point2f(src.Width / 2, src.Height / 2), angle, 1)
        ocvb.result1 = src.WarpAffine(M, src.Size(), warpFlag)
        If warpFlag = cv.InterpolationFlags.WarpInverseMap Then Mflip = cv.Cv2.GetRotationMatrix2D(New cv.Point2f(src.Width / 2, src.Height / 2), -angle, 1)
    End Sub
End Class






Public Class GetRotationMatrix2D_Box
    Inherits ocvbClass
    Dim rotation As GetRotationMatrix2D_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        rotation = New GetRotationMatrix2D_Basics(ocvb, caller)
        ocvb.drawRect = New cv.Rect(100, 100, 100, 100)

        ocvb.label1 = "Original Rectangle in the original perspective"
        ocvb.label2 = "Same Rectangle in the new warped perspective"
        ocvb.desc = "Track a rectangle no matter how the perspective is warped.  Draw a rectangle anywhere."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        rotation.src = ocvb.color
        rotation.Run(ocvb)
        ocvb.result2 = ocvb.result1.Clone()

        Dim r = ocvb.drawRect
        ocvb.result1 = ocvb.color.Clone()
        ocvb.result1.Rectangle(r, cv.Scalar.White, 1)

        Dim center = New cv.Point2f(r.X + r.Width / 2, r.Y + r.Height / 2)
        Dim drawBox = New cv.RotatedRect(center, New cv.Size2f(r.Width, r.Height), 0)
        Dim boxPoints = cv.Cv2.BoxPoints(drawBox)
        Dim srcPoints = New cv.Mat(1, 4, cv.MatType.CV_32FC2, boxPoints)
        Dim dstpoints As New cv.Mat

        If rotation.warpFlag <> cv.InterpolationFlags.WarpInverseMap Then
            cv.Cv2.Transform(srcPoints, dstpoints, rotation.M)
        Else
            cv.Cv2.Transform(srcPoints, dstpoints, rotation.Mflip)
        End If
        For i = 0 To dstpoints.Width - 1
            Dim p1 = dstpoints.Get(of cv.Point2f)(0, i)
            Dim p2 = dstpoints.Get(of cv.Point2f)(0, (i + 1) Mod 4)
            ocvb.result2.Line(p1, p2, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub MyDispose()
        rotation.Dispose()
    End Sub
End Class