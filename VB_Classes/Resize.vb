Imports cv = OpenCvSharp
Public Class Resize_Basics
    Inherits VBparent
    Public newSize As cv.Size
    Public Sub New()
        initParent()
        SetInterpolationRadioButtons(caller, radio, "Resize")
        ' warp is not allowed in resize
        radio.check(5).Enabled = False
        radio.check(6).Enabled = False

        ocvb.desc = "Resize with different options and compare them"
        label1 = "Rectangle highlight above resized"
        label2 = "Difference from Cubic Resize (Best)"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static frm = findForm("Resize_Basics Radio Options")
        Dim resizeFlag = getInterpolationRadioButtons(radio, frm)
        If standalone Then
            Dim roi = New cv.Rect(src.Width / 4, src.Height / 4, src.Width / 2, src.Height / 2)
            If ocvb.task.drawRect.Width <> 0 Then roi = ocvb.task.drawRect

            dst1 = src(roi).Resize(dst1.Size(), 0, 0, resizeFlag)
            dst2 = (src(roi).Resize(dst1.Size(), 0, 0, cv.InterpolationFlags.Cubic) - dst1).ToMat.Threshold(0, 255, cv.ThresholdTypes.Binary)
            src.Rectangle(roi, cv.Scalar.White, 2)
        Else
            dst1 = src.Resize(newSize, 0, 0, resizeFlag)
        End If
    End Sub
End Class







Public Class Resize_Percentage
    Inherits VBparent
    Public resizeOptions As Resize_Basics
    Public Sub New()
        initParent()
        resizeOptions = New Resize_Basics()

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Resize Percentage (%)", 1, 100, 3)

        ocvb.desc = "Resize by a percentage of the image."
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim percent As Double = CDbl(sliders.trackbar(0).Value / 100)
        Dim resizePercent = sliders.trackbar(0).Value / 100
        resizePercent = Math.Sqrt(resizePercent)
        resizeOptions.newSize = New cv.Size(Math.Ceiling(src.Width * resizePercent), Math.Ceiling(src.Height * resizePercent))
        resizeOptions.src = src
        resizeOptions.Run()

        If standalone Then
            Dim roi As New cv.Rect(0, 0, resizeOptions.dst1.Width, resizeOptions.dst1.Height)
            dst1 = resizeOptions.dst1(roi).Resize(resizeOptions.dst1.Size())
            label1 = "Image after resizing to " + Format(sliders.trackbar(0).Value, "#0.0") + "% of original size"
            label2 = ""
        Else
            dst1 = resizeOptions.dst1
        End If
    End Sub
End Class


