Imports cv = OpenCvSharp
Public Class Diff_Basics
    Inherits ocvbClass
    Dim lastFrame As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Diff - Color Threshold", 1, 255, 5)
        label1 = "Stable Gray Color"
        label2 = "Unstable Color mask"
        ocvb.desc = "Capture an image and compare it to previous frame using absDiff and threshold"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Or src.Width = 0 Then src = ocvb.color
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If ocvb.frameCount > 0 Then
            dst1 = lastFrame
            cv.Cv2.Absdiff(gray, lastFrame, dst2)
            dst2 = dst2.Threshold(sliders.TrackBar1.Value, 255, cv.ThresholdTypes.Binary)
            dst1 = ocvb.color.Clone().SetTo(0, dst2)
        End If
        lastFrame = gray.Clone()
    End Sub
End Class




Public Class Diff_UnstableDepthAndColor
    Inherits ocvbClass
    Public diff As Diff_Basics
    Public depth As Depth_Stable
    Dim lastFrames() As cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        diff = New Diff_Basics(ocvb, caller)
        diff.sliders.TrackBar1.Value = 20 ' this is color threshold - low means detecting more motion.

        depth = New Depth_Stable(ocvb, caller)

        label1 = "Stable depth and color"
        ocvb.desc = "Build a mask for any pixels that have either unstable depth or color"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Or src.Width = 0 Then src = ocvb.color
        diff.src = src
        diff.Run(ocvb)
        Dim unstableColor = diff.dst2.Clone()
        depth.src = ocvb.RGBDepth
        depth.Run(ocvb)
        Dim unstableDepth As New cv.Mat
        Dim mask As New cv.Mat
        cv.Cv2.BitwiseNot(depth.dst2, unstableDepth)
        If unstableColor.Channels = 3 Then unstableColor = unstableColor.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.BitwiseOr(unstableColor, unstableDepth, mask)
        dst1 = ocvb.color.Clone()
        dst1.SetTo(0, mask)
        label2 = "Unstable depth/color mask"
        dst2 = mask
    End Sub
End Class
