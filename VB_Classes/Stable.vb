Imports cv = OpenCvSharp
Public Class Stable_Clusters
    Inherits VBparent
    Dim clusters As Histogram_DepthClusters
    Dim stableD As Depth_Stable
    Public Sub New()
        initParent()

        clusters = New Histogram_DepthClusters
        stableD = New Depth_Stable
        task.desc = "Use the stable depth to identify the depth_clusters using histogram valleys"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        stableD.Run()
        clusters.src = stableD.dst2
        clusters.Run()
        dst1 = clusters.dst1
        dst2 = clusters.dst2
    End Sub
End Class








Public Class Stable_Pointcloud
    Inherits VBparent
    Public stable As Stable_Depth
    Public Sub New()
        initParent()
        stable = New Stable_Depth
        label1 = stable.label1
        label2 = stable.label2
        task.desc = "Use the stable depth values to create a stable point cloud"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        stable.Run()
        dst1 = stable.dst1
        dst2 = stable.dst2

        Dim split = cv.Cv2.Split(task.pointCloud)
        Static splitPC() = split
        splitPC(2) = (stable.dst1 * 0.001).ToMat
        split(0).CopyTo(splitPC(0), stable.dst2)
        split(1).CopyTo(splitPC(1), stable.dst2)
        cv.Cv2.Merge(splitPC, task.pointCloud)
    End Sub
End Class







Public Class Stable_Depth
    Inherits VBparent
    Public resetAll As Boolean
    Public pitch As Single ' in radians.
    Public yaw As Single ' in radians.
    Public roll As Single ' in radians.
    Public cameraStable As Boolean
    Public zeroDepthMask As cv.Mat
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Motion threshold before resyncing entire image", 1, 100000, If(task.color.Width = 1280, 20000, 5000))
            sliders.setupTrackBar(1, "Motion change threshold", 1, 255, 25)
            sliders.setupTrackBar(2, "Camera Motion threshold in radians X100", 1, 100, 3) ' how much motion is reasonable?
        End If

        label1 = "32-bit format of the stable depth"
        label2 = "8-bit format of the motion mask"
        task.desc = "While minimizing options and dependencies, use RGB motion to figure out what depth values should change."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = getDepth32f()

        pitch = task.IMU_AngularVelocity.X
        yaw = task.IMU_AngularVelocity.Y
        roll = task.IMU_AngularVelocity.Z
        Static cameraMotionThreshold = findSlider("Camera Motion threshold in radians X100")
        Dim cameraStable = If(cameraMotionThreshold.Value / 100 < Math.Abs(pitch) + Math.Abs(yaw) + Math.Abs(roll), False, True)

        Dim gray = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static lastFrame As cv.Mat = gray.Clone
        cv.Cv2.Absdiff(gray, lastFrame, dst2)
        lastFrame = gray
        Static thresholdSlider = findSlider("Motion change threshold")
        dst2 = dst2.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)

        Static nonZeroThreshold = findSlider("Motion threshold before resyncing entire image")
        Dim changedPixels = dst2.CountNonZero()
        If cameraStable = False Or changedPixels > nonZeroThreshold.value Or dst1.Type <> cv.MatType.CV_32FC1 Then
            resetAll = True
            dst1 = input
        Else
            resetAll = False
            input.CopyTo(dst1, dst2)
            cv.Cv2.Max(input, dst1, dst1)

            zeroDepthMask = input.ConvertScaleAbs(255).Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
            ' clearing out the zeros degrades the image but does eliminate a small number of pixels where depth should be zero.  Uncomment to review...
            'dst1.SetTo(0, zeroMask)
        End If
    End Sub
End Class







Public Class Stable_DepthColorized
    Inherits VBparent
    Dim stable As Stable_Depth
    Dim colorize As Depth_ColorizerFastFade_CPP
    Public Sub New()
        initParent()
        colorize = New Depth_ColorizerFastFade_CPP
        stable = New Stable_Depth
        label1 = "32-bit format stable depth data"
        label2 = "Colorized version of image at left"
        task.desc = "Colorize the stable depth (keeps Stable_Depth at a minimum complexity)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        stable.Run()

        colorize.src = stable.dst1
        colorize.Run()
        dst1 = stable.dst1
        dst2 = colorize.dst1
    End Sub
End Class