Imports cv = OpenCvSharp
Public Class Options_InRange
    Inherits VBparent
    Public depthMask As New cv.Mat
    Public noDepthMask As New cv.Mat
    Public minVal As Single
    Public maxVal As Single
    Public bins As Integer
    Public depth32f As New cv.Mat
    Public depth32fAfterMasking As Boolean
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "InRange Min Depth (mm)", 0, 2000, 200)
            sliders.setupTrackBar(1, "InRange Max Depth (mm)", 200, 15000, 4000)
            sliders.setupTrackBar(2, "Top/Side View Histogram threshold", 0, 3000, 10)
        End If
        label1 = "Depth values that are in-range"
        label2 = "Depth values that are out of range (and < 8m)"
        task.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static minSlider = findSlider("InRange Min Depth (mm)")
        Static maxSlider = findSlider("InRange Max Depth (mm)")
        Static binSlider = findSlider("Top/Side View Histogram threshold")
        minVal = minSlider.Value
        maxVal = maxSlider.Value
        bins = binSlider.value
        If minVal >= maxVal Then maxVal = minVal + 1
        task.depth16.ConvertTo(depth32f, cv.MatType.CV_32F)
        cv.Cv2.InRange(depth32f, minVal, maxVal, depthMask)
        cv.Cv2.BitwiseNot(depthMask, noDepthMask)
        dst1 = depth32f.SetTo(0, noDepthMask)
        If standalone Or depth32fAfterMasking Then dst2 = depth32f.SetTo(0, depthMask)
    End Sub
End Class
