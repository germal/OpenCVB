Imports cv = OpenCvSharp
Public Class Options_Common
    Inherits VBparent
    Public depthMask As New cv.Mat
    Public noDepthMask As New cv.Mat
    Public minVal As Single
    Public maxVal As Single
    Public bins As Integer
    Public Sub New()
        initParent()
        task.callTrace.Clear() ' special line to clear the tree view otherwise Options_Common is standalone.
        standalone = False
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "InRange Min Depth (mm)", 1, 2000, 200)
        sliders.setupTrackBar(1, "InRange Max Depth (mm)", 200, 15000, 4000)
        sliders.setupTrackBar(2, "Top/Side View Histogram threshold", 0, 200, 10)
        sliders.setupTrackBar(3, "Amount to rotate pointcloud around Y-axis (degrees)", -90, 90, 0)
        task.minRangeSlider = sliders.trackbar(0) ' one of the few places we can be certain there is only one...
        task.maxRangeSlider = sliders.trackbar(1)
        task.binSlider = sliders.trackbar(2)
        task.yRotateSlider = sliders.trackbar(3)

        label1 = "Depth values that are in-range"
        label2 = "Depth values that are out of range (and < 8m)"
        task.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        minVal = task.minRangeSlider.Value
        maxVal = task.maxRangeSlider.Value
        ocvb.maxZ = maxVal / 1000
        bins = task.binSlider.value
        If minVal >= maxVal Then maxVal = minVal + 1
        task.depth16.ConvertTo(task.depth32f, cv.MatType.CV_32F)
        cv.Cv2.InRange(task.depth32f, minVal, maxVal, depthMask)
        cv.Cv2.BitwiseNot(depthMask, noDepthMask)
        dst1 = task.depth32f.SetTo(0, noDepthMask)
        task.pointCloud.SetTo(0, noDepthMask)
    End Sub
End Class
