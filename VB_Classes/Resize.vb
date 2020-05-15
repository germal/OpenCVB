Imports cv = OpenCvSharp
Public Class Resize_Basics
    Inherits ocvbClass
    Public newSize As cv.Size
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        SetInterpolationRadioButtons(ocvb, caller, radio, "Resize")
        ' warp is not allowed in resize
        radio.check(5).Enabled = False
        radio.check(6).Enabled = False

        ocvb.desc = "Resize with different options and compare them"
        label1 = "Rectangle highlight above resized"
        label2 = "Difference from Cubic Resize (Best)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim resizeFlag = getInterpolationRadioButtons(radio)
        if standalone Then
            Dim roi = New cv.Rect(ocvb.color.Width / 4, ocvb.color.Height / 4, ocvb.color.Width / 2, ocvb.color.Height / 2)
            If ocvb.drawRect.Width <> 0 Then roi = ocvb.drawRect

            dst1 = ocvb.color(roi).Resize(dst1.Size(), 0, 0, resizeFlag)

            dst2 = (ocvb.color(roi).Resize(dst1.Size(), 0, 0, cv.InterpolationFlags.Cubic) -
                            dst1).ToMat.Threshold(0, 255, cv.ThresholdTypes.Binary)
            ocvb.color.Rectangle(roi, cv.Scalar.White, 2)
        Else
            dst1 = src.Resize(newSize, 0, 0, resizeFlag)
        End If
    End Sub
End Class





Public Class Resize_After8uc3
    Inherits ocvbClass
    Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        colorizer = New Depth_Colorizer_CPP(ocvb, caller)
        SetInterpolationRadioButtons(ocvb, caller, radio, "Resize")
        ' warp is not allowed in resize
        radio.check(5).Enabled = False
        radio.check(6).Enabled = False

        label1 = "Resized depth16 before running thru colorizer"
        label2 = "Resized depth8UC3 after running thru colorizer"
        ocvb.desc = "When you resize depth16 is important.  Use depth16 at high resolution and resize the 8UC3 result"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim resizeFlag = getInterpolationRadioButtons(radio)
        Dim newSize = ocvb.color.Size()
        If ocvb.parms.lowResolution = False Then newSize = New cv.Size(ocvb.color.Height / 2, ocvb.color.Width / 2)

        Dim depth32f As New cv.Mat
        ocvb.depth16.ConvertTo(depth32f, cv.MatType.CV_32F)
        colorizer.src = depth32f
        colorizer.Run(ocvb)
        dst2 = colorizer.dst1.Resize(newSize, 0, resizeFlag)

        Dim depth16 = ocvb.depth16.Resize(newSize, 0, resizeFlag)
        depth16.ConvertTo(depth32f, cv.MatType.CV_32F)
        colorizer.src = depth32f
        colorizer.Run(ocvb)
        dst1 = colorizer.dst1
    End Sub
End Class






Public Class Resize_Percentage
    Inherits ocvbClass
    Public resizeOptions As Resize_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        resizeOptions = New Resize_Basics(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "Resize Percentage (%)", 1, 100, 3)

        ocvb.desc = "Resize by a percentage of the image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim percent As Double = CDbl(sliders.TrackBar1.Value / 100)
        Dim resizePercent = sliders.TrackBar1.Value / 100
        resizePercent = Math.Sqrt(resizePercent)
        resizeOptions.newSize = New cv.Size(Math.Ceiling(src.Width * resizePercent), Math.Ceiling(src.Height * resizePercent))
        resizeOptions.src = src
        resizeOptions.Run(ocvb)

        If standalone Then
            Dim roi As New cv.Rect(0, 0, resizeOptions.dst1.Width, resizeOptions.dst1.Height)
            dst1 = resizeOptions.dst1(roi).Resize(resizeOptions.dst1.Size())
            label1 = "Image after resizing to " + Format(sliders.TrackBar1.Value, "#0.0") + "% of original size"
            label2 = ""
        Else
            dst1 = resizeOptions.dst1
        End If
    End Sub
End Class

