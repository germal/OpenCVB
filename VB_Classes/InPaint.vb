Imports cv = OpenCvSharp


' https://docs.opencv.org/master/df/d3d/tutorial_py_inpainting.html#gsc.tab=0
Public Class InPaint_Basics
    Inherits VB_Class
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Thickness", 1, 25, 2)

        radio.Setup(ocvb, callerName,2)
        radio.check(0).Text = "TELEA"
        radio.check(1).Text = "Navier-Stokes"
        radio.check(0).Checked = True

        ocvb.desc = "Create a flaw in an image and then use inPaint to mask it."
        ocvb.label2 = "Repaired Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim inPaintFlag = If(radio.check(0).Checked, cv.InpaintMethod.Telea, cv.InpaintMethod.NS)

        If ocvb.frameCount Mod 100 Then Exit Sub
        ocvb.color.CopyTo(ocvb.result1)
        Dim p1 = New cv.Point2f(ocvb.ms_rng.Next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.Next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
        Dim p2 = New cv.Point2f(ocvb.ms_rng.Next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.Next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
        Dim thickness = sliders.TrackBar1.Value
        ocvb.result1.Line(p1, p2, New cv.Scalar(0, 0, 0), thickness, cv.LineTypes.AntiAlias)
        Dim mask = New cv.Mat(ocvb.result1.Size(), cv.MatType.CV_8UC1)
        mask.SetTo(0)
        mask.Line(p1, p2, cv.Scalar.All(255), thickness, cv.LineTypes.AntiAlias)
        cv.Cv2.Inpaint(ocvb.result1, mask, ocvb.result2, thickness, inPaintFlag)
    End Sub
    Public Sub MyDispose()
        radio.Dispose()
            End Sub
End Class



Public Class InPaint_Noise
    Inherits VB_Class
    Dim noise As Draw_Noise
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        noise = New Draw_Noise(ocvb, callerName)

        radio.Setup(ocvb, callerName,2)
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
        cv.Cv2.Inpaint(ocvb.result1, noise.noiseMask, ocvb.result2, noise.maxNoiseWidth, inPaintFlag)
    End Sub
    Public Sub MyDispose()
        radio.Dispose()
        noise.Dispose()
    End Sub
End Class
