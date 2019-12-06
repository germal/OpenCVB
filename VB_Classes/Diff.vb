Imports cv = OpenCvSharp
Public Class Diff_Basics : Implements IDisposable
    Public sliders As New OptionsSliders
    Dim lastFrame As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Diff - Color Threshold", 1, 255, 50)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.label1 = "Stable Gray Color"
        ocvb.label2 = "Unstable Gray Color"
        ocvb.desc = "Capture an image and compare it to previous frame using absDiff and threshold"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If ocvb.frameCount > 0 Then
            ocvb.result1 = lastFrame
            cv.Cv2.Absdiff(gray, lastFrame, ocvb.result2)
            ocvb.result2 = ocvb.result2.Threshold(sliders.TrackBar1.Value, 255, cv.ThresholdTypes.Binary)
            ocvb.result1 = ocvb.color.SetTo(0, ocvb.result2)
        End If
        lastFrame = gray.Clone()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Diff_UnstableDepthAndColor : Implements IDisposable
    Dim diff As Diff_Basics
    Dim depth As Depth_Stable
    Dim lastFrames() As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        diff = New Diff_Basics(ocvb)
        diff.sliders.TrackBar1.Value = 20 ' this is color threshold - low means detecting more motion.
        depth = New Depth_Stable(ocvb)
        depth.sliders.TrackBar1.Value = 1 ' just look at previous frame
        ocvb.desc = "Build a mask for any pixels that have either unstable depth or color"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        diff.Run(ocvb)
        Dim unstableColor = ocvb.result2.Clone()
        depth.Run(ocvb)
        If ocvb.result1.Channels = 1 Then
            Dim unstableDepth As New cv.Mat
            cv.Cv2.BitwiseNot(ocvb.result1, unstableDepth)
            cv.Cv2.BitwiseOr(unstableColor, unstableDepth, ocvb.result1)
            ocvb.label1 = "Unstable depth or color"
            ocvb.label2 = "Unknown depth"
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        diff.Dispose()
        depth.Dispose()
    End Sub
End Class