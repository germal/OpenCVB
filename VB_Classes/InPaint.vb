Imports cv = OpenCvSharp


' https://docs.opencv.org/master/df/d3d/tutorial_py_inpainting.html#gsc.tab=0
Public Class InPaint_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Thickness", 1, 25, 2)

        radio.Setup(ocvb, caller, 2)
        radio.check(0).Text = "TELEA"
        radio.check(1).Text = "Navier-Stokes"
        radio.check(0).Checked = True

        ocvb.desc = "Create a flaw in an image and then use inPaint to mask it."
        ocvb.label2 = "Repaired Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim inPaintFlag = If(radio.check(0).Checked, cv.InpaintMethod.Telea, cv.InpaintMethod.NS)

        If ocvb.frameCount Mod 100 Then Exit Sub
        ocvb.color.CopyTo(dst)
        Dim p1 = New cv.Point2f(ocvb.ms_rng.Next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.Next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
        Dim p2 = New cv.Point2f(ocvb.ms_rng.Next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.Next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
        Dim thickness = sliders.TrackBar1.Value
        dst.Line(p1, p2, New cv.Scalar(0, 0, 0), thickness, cv.LineTypes.AntiAlias)
        Dim mask = New cv.Mat(dst.Size(), cv.MatType.CV_8UC1)
        mask.SetTo(0)
        mask.Line(p1, p2, cv.Scalar.All(255), thickness, cv.LineTypes.AntiAlias)
        cv.Cv2.Inpaint(dst, mask, ocvb.result2, thickness, inPaintFlag)
    End Sub
End Class



Public Class InPaint_Noise
    Inherits ocvbClass
    Dim noise As Draw_Noise
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        noise = New Draw_Noise(ocvb, caller)

        radio.Setup(ocvb, caller, 2)
        radio.check(0).Text = "TELEA"
        radio.check(1).Text = "Navier-Stokes"
        radio.check(0).Checked = True

        ocvb.desc = "Create noise in an image and then use inPaint to remove it."
        ocvb.label2 = "Repaired Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 100 Then Exit Sub ' give them time to review the inpaint results
        noise.Run(ocvb) ' create some noise in the result1 image.
        Dim inPaintFlag = If(radio.check(0).Checked, cv.InpaintMethod.Telea, cv.InpaintMethod.NS)
        cv.Cv2.Inpaint(dst, noise.noiseMask, ocvb.result2, noise.maxNoiseWidth, inPaintFlag)
    End Sub
End Class

