Imports cv = OpenCvSharp
Public Class Resize_Options : Implements IDisposable
    Public radio As New OptionsRadioButtons
    Public externalUse As Boolean
    Public src As cv.Mat
    Public dst As New cv.Mat
    Public newSize As cv.Size
    Public Sub New(ocvb As AlgorithmData)
        SetInterpolationRadioButtons(ocvb, radio, "Resize")
        ' warp is not allowed in resize
        radio.check(5).Enabled = False
        radio.check(6).Enabled = False

        ocvb.desc = "Resize with different options and compare them"
        ocvb.label2 = "Difference from Cubic Resize (Best)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim resizeFlag = getInterpolationRadioButtons(radio)
        If externalUse = False Then
            Dim roi = New cv.Rect(ocvb.color.Width / 4, ocvb.color.Height / 4, ocvb.color.Width / 2, ocvb.color.Height / 2)
            If ocvb.drawRect.Width <> 0 Then roi = ocvb.drawRect

            ocvb.result1 = ocvb.color(roi).Resize(ocvb.result1.Size(), 0, 0, resizeFlag)

            ocvb.result2 = ocvb.color(roi).Resize(ocvb.result1.Size(), 0, 0, cv.InterpolationFlags.Cubic)
            ocvb.result2 -= ocvb.result1
            ocvb.result2 = ocvb.result2.Threshold(0, 255, cv.ThresholdTypes.Binary)
            ocvb.color.Rectangle(roi, cv.Scalar.White, 1)
        Else
            dst = src.Resize(newSize, 0, 0, resizeFlag)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        radio.Dispose()
    End Sub
End Class




Public Class Resize_Percentage : Implements IDisposable
    Public sliders As New OptionsSliders
    Public src As New cv.Mat
    Public dst As New cv.Mat
    Public externalUse As Boolean
    Public resizeOptions As Resize_Options
    Public Sub New(ocvb As AlgorithmData)
        resizeOptions = New Resize_Options(ocvb)
        resizeOptions.externalUse = True

        sliders.setupTrackBar1(ocvb, "Resize Percentage (%)", 1, 100, 3)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Resize by a percentage of the image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim percent As Double = CDbl(sliders.TrackBar1.Value / 100)
        If externalUse = False Then src = ocvb.color
        dim resizePercent = sliders.TrackBar1.Value / 100
        resizePercent = Math.Sqrt(resizePercent)
        resizeOptions.newSize = New cv.Size(Math.Ceiling(src.Width * resizePercent), Math.Ceiling(src.Height * resizePercent))
        resizeOptions.src = src
        resizeOptions.Run(ocvb)

        If externalUse = False Then
            Dim roi As New cv.Rect(0, 0, resizeOptions.dst.Width, resizeOptions.dst.Height)
            ocvb.result1 = resizeOptions.dst(roi).Resize(resizeOptions.dst.Size())
            ocvb.label1 = "Image after resizing to " + Format(sliders.TrackBar1.Value, "#0.0") + "% of original size"
            ocvb.label2 = ""
        Else
            dst = resizeOptions.dst
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        resizeOptions.Dispose()
        sliders.Dispose()
    End Sub
End Class
