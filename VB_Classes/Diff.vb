Imports cv = OpenCvSharp
Public Class Diff_Basics
    Inherits VBparent
    Public lastFrame As New cv.Mat
    Public Sub New()
        initParent()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Change threshold for each pixel", 1, 255, 25)
        label1 = "Stable Color"
        label2 = "Unstable Color mask"
        task.desc = "Capture an image and compare it to previous frame using absDiff and threshold"
    End Sub
    Public Sub Run()
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim gray = src
        If src.Channels = 3 Then gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If ocvb.frameCount > 0 Then
            dst1 = lastFrame
            cv.Cv2.Absdiff(gray, lastFrame, dst2)
            dst2 = dst2.Threshold(sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary)
            dst1 = src.Clone().SetTo(0, dst2)
        End If
        lastFrame = gray.Clone()
    End Sub
End Class




Public Class Diff_UnstableDepthAndColor
    Inherits VBparent
    Public diff As Diff_Basics
    Public depth As Depth_Stable
    Dim lastFrames() As cv.Mat
    Public Sub New()
        initParent()
        diff = New Diff_Basics()
        diff.sliders.trackbar(0).Value = 20 ' this is color threshold - low means detecting more motion.

        depth = New Depth_Stable()

        label1 = "Stable depth and color"
        task.desc = "Build a mask for any pixels that have either unstable depth or color"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        diff.src = src
        diff.Run()
        Dim unstableColor = diff.dst2.Clone()
        depth.src = task.RGBDepth
        depth.Run()
        Dim unstableDepth As New cv.Mat
        Dim mask As New cv.Mat
        cv.Cv2.BitwiseNot(depth.dst2, unstableDepth)
        If unstableColor.Channels = 3 Then unstableColor = unstableColor.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.BitwiseOr(unstableColor, unstableDepth, mask)
        dst1 = src.Clone()
        dst1.SetTo(0, mask)
        label2 = "Unstable depth/color mask"
        dst2 = mask
    End Sub
End Class

