Imports cv = OpenCvSharp


' https://docs.opencv.org/master/df/d3d/tutorial_py_inpainting.html#gsc.tab=0
Public Class InPaint_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Thickness", 1, 25, 2)

        radio.Setup(ocvb, caller, 2)
        radio.check(0).Text = "TELEA"
        radio.check(1).Text = "Navier-Stokes"
        radio.check(0).Checked = True

        ocvb.desc = "Create a flaw in an image and then use inPaint to mask it."
        label2 = "Repaired Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim inPaintFlag = If(radio.check(0).Checked, cv.InpaintMethod.Telea, cv.InpaintMethod.NS)

        If ocvb.frameCount Mod 100 Then Exit Sub
        src.CopyTo(dst1)
        Dim p1 = New cv.Point2f(msRNG.Next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), msRNG.Next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
        Dim p2 = New cv.Point2f(msRNG.Next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), msRNG.Next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
        Dim thickness = sliders.sliders(0).Value
        dst1.Line(p1, p2, New cv.Scalar(0, 0, 0), thickness, cv.LineTypes.AntiAlias)
        Dim mask = New cv.Mat(dst1.Size(), cv.MatType.CV_8UC1)
        mask.SetTo(0)
        mask.Line(p1, p2, cv.Scalar.All(255), thickness, cv.LineTypes.AntiAlias)
        cv.Cv2.Inpaint(dst1, mask, dst2, thickness, inPaintFlag)
    End Sub
End Class



Public Class InPaint_Noise
    Inherits ocvbClass
    Dim noise As Draw_Noise
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        noise = New Draw_Noise(ocvb)

        radio.Setup(ocvb, caller, 2)
        radio.check(0).Text = "TELEA"
        radio.check(1).Text = "Navier-Stokes"
        radio.check(0).Checked = True

        ocvb.desc = "Create noise in an image and then use inPaint to remove it."
        label2 = "Repaired Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 100 Then Exit Sub ' give them time to review the inpaint results
        noise.src = src
        noise.Run(ocvb) ' create some noise in the result1 image.
        dst1 = noise.dst1
        Dim inPaintFlag = If(radio.check(0).Checked, cv.InpaintMethod.Telea, cv.InpaintMethod.NS)
        cv.Cv2.Inpaint(dst1, noise.noiseMask, dst2, noise.maxNoiseWidth, inPaintFlag)
    End Sub
End Class

