Imports cv = OpenCvSharp
Public Class Resize_Basics
    Inherits VB_Class
    Public externalUse As Boolean
    Public src As cv.Mat
    Public dst As New cv.Mat
    Public newSize As cv.Size
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        SetInterpolationRadioButtons(ocvb, callerName, radio, "Resize")
        ' warp is not allowed in resize
        radio.check(5).Enabled = False
        radio.check(6).Enabled = False

        ocvb.desc = "Resize with different options and compare them"
        ocvb.label1 = "Rectangle highlight above resized"
        ocvb.label2 = "Difference from Cubic Resize (Best)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim resizeFlag = getInterpolationRadioButtons(radio)
        If externalUse = False Then
            Dim roi = New cv.Rect(ocvb.color.Width / 4, ocvb.color.Height / 4, ocvb.color.Width / 2, ocvb.color.Height / 2)
            If ocvb.drawRect.Width <> 0 Then roi = ocvb.drawRect

            ocvb.result1 = ocvb.color(roi).Resize(ocvb.result1.Size(), 0, 0, resizeFlag)

            ocvb.result2 = (ocvb.color(roi).Resize(ocvb.result1.Size(), 0, 0, cv.InterpolationFlags.Cubic) -
                            ocvb.result1).ToMat.Threshold(0, 255, cv.ThresholdTypes.Binary)
            ocvb.color.Rectangle(roi, cv.Scalar.White, 2)
        Else
            dst = src.Resize(newSize, 0, 0, resizeFlag)
        End If
    End Sub
End Class





Public Class Resize_After8uc3
    Inherits VB_Class
        Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        colorizer = New Depth_Colorizer_CPP(ocvb, callerName)
        colorizer.externalUse = True
        SetInterpolationRadioButtons(ocvb, callerName, radio, "Resize")
        ' warp is not allowed in resize
        radio.check(5).Enabled = False
        radio.check(6).Enabled = False

        ocvb.label1 = "Resized depth16 before running thru colorizer"
        ocvb.label2 = "Resized depth8UC3 after running thru colorizer"
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
        ocvb.result2 = colorizer.dst.Resize(newSize, 0, resizeFlag)

        Dim depth16 = ocvb.depth16.Resize(newSize, 0, resizeFlag)
        depth16.ConvertTo(depth32f, cv.MatType.CV_32F)
        colorizer.src = depth32f
        colorizer.Run(ocvb)
        ocvb.result1 = colorizer.dst
    End Sub
    Public Sub MyDispose()
                colorizer.Dispose()
    End Sub
End Class






Public Class Resize_Percentage
    Inherits VB_Class
        Public src As New cv.Mat
    Public dst As New cv.Mat
    Public externalUse As Boolean
    Public resizeOptions As Resize_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        resizeOptions = New Resize_Basics(ocvb, callerName)
        resizeOptions.externalUse = True

        sliders.setupTrackBar1(ocvb, callerName, "Resize Percentage (%)", 1, 100, 3)

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
    Public Sub MyDispose()
        resizeOptions.Dispose()
            End Sub
End Class
