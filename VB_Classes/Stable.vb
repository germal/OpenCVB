Imports cv = OpenCvSharp
Public Class Stable_Basics
    Inherits VBparent
    Public resetAll As Boolean
    Public pitch As Single ' in radians.
    Public yaw As Single ' in radians.
    Public roll As Single ' in radians.
    Public cameraStable As Boolean
    Public cumulativeChanges As Integer
    Public changedPixels As Integer
    Public externalReset As Boolean
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Motion threshold before resyncing entire image", 1, 100000, If(task.color.Width = 1280, 20000, 5000))
            sliders.setupTrackBar(1, "Motion change threshold", 1, 255, 25)
            sliders.setupTrackBar(2, "Camera Motion threshold in radians X100", 1, 100, 3) ' how much motion is reasonable?
        End If

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Use farthest distance"
            radio.check(1).Text = "Use closest distance"
            radio.check(2).Text = "Use unchanged depth input"
            radio.check(0).Checked = True
        End If

        label1 = "32-bit format of the stable depth"
        label2 = "8-bit format of the motion mask"
        task.desc = "While minimizing options and dependencies, use RGB motion to figure out what depth values should change."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = task.depth32f.Clone

        pitch = task.IMU_AngularVelocity.X
        yaw = task.IMU_AngularVelocity.Y
        roll = task.IMU_AngularVelocity.Z

        Static cameraMotionThreshold = findSlider("Camera Motion threshold in radians X100")
        Static nonZeroThreshold = findSlider("Motion threshold before resyncing entire image")
        Static thresholdSlider = findSlider("Motion change threshold")

        cameraStable = If(cameraMotionThreshold.Value / 100 < Math.Abs(pitch) + Math.Abs(yaw) + Math.Abs(roll), False, True)

        Dim gray = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static lastFrame As cv.Mat = gray.Clone
        cv.Cv2.Absdiff(gray, lastFrame, dst2)
        lastFrame = gray
        dst2 = dst2.Threshold(thresholdSlider.value, 255, cv.ThresholdTypes.Binary)

        changedPixels = dst2.CountNonZero()
        cumulativeChanges += changedPixels
        Static saveMaxRange As Integer
        Static saveMinRange As Integer
        Static saveYRotate As Integer
        '  If cameraStable = False Or cumulativeChanges > nonZeroThreshold.value Or saveMaxRange <> task.maxRangeSlider.Value Or
        If cameraStable = False Or changedPixels > nonZeroThreshold.value Or saveMaxRange <> task.maxRangeSlider.Value Or
            saveYRotate <> task.yRotateSlider.Value Or externalReset Or saveMinRange <> task.minRangeSlider.Value Then
            resetAll = True
            saveMaxRange = task.maxRangeSlider.Value
            saveMinRange = task.minRangeSlider.Value
            saveYRotate = task.yRotateSlider.Value
            externalReset = False
            dst1 = input
            cumulativeChanges = 0
        Else
            resetAll = False
            dst1.SetTo(0)
            input.CopyTo(dst1, dst2)

            Static useNone = findRadio("Use unchanged depth input")
            Static useMin = findRadio("Use closest distance")
            Static useMax = findRadio("Use farthest distance")
            If useNone.checked = False Then
                If useMax.checked Then cv.Cv2.Max(input, dst1, dst1)
                If useMin.checked Then cv.Cv2.Min(input, dst1, dst1)
                dst1.SetTo(0, task.inrange.noDepthMask)
            Else
                input.CopyTo(dst1)
            End If
        End If
    End Sub
End Class






Public Class Stable_Clusters
    Inherits VBparent
    Dim clusters As Histogram_DepthClusters
    Dim stableD As Stable_Basics
    Public Sub New()
        initParent()

        clusters = New Histogram_DepthClusters
        stableD = New Stable_Basics
        label1 = "Histogram of stable depth"
        label2 = "Backprojection of stable depth"
        task.desc = "Use the stable depth to identify the depth_clusters using histogram valleys"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        stableD.Run()
        clusters.src = stableD.dst1
        clusters.Run()
        dst1 = clusters.dst1
        dst2 = clusters.dst2
    End Sub
End Class








Public Class Stable_Pointcloud
    Inherits VBparent
    Public stable As Stable_Basics
    Public splitPC() As cv.Mat
    Public Sub New()
        initParent()
        stable = New Stable_Basics
        label1 = stable.label1
        label2 = stable.label2
        task.desc = "Use the stable depth values to create a stable point cloud"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim split = cv.Cv2.Split(task.pointCloud)
        stable.src = split(2) * 1000

        stable.Run()

        dst1 = stable.dst1
        dst2 = stable.dst2
        splitPC = split
        label2 = "Cumulative Motion = " + Format(stable.changedPixels / 1000, "#0.0") + "k pixels "
        If stable.resetAll Then
            splitPC = split
        Else
            splitPC(2) = (stable.dst1 * 0.001).ToMat
            split(0).CopyTo(splitPC(0), stable.dst2)
            split(1).CopyTo(splitPC(1), stable.dst2)
            cv.Cv2.Merge(splitPC, task.pointCloud)
        End If
    End Sub
End Class








Public Class Stable_BasicsColorized
    Inherits VBparent
    Dim stable As Stable_Basics
    Dim colorize As Depth_ColorizerFastFade_CPP
    Public Sub New()
        initParent()
        colorize = New Depth_ColorizerFastFade_CPP
        stable = New Stable_Basics
        label1 = "32-bit format stable depth data"
        label2 = "Colorized version of image at left"
        task.desc = "Colorize the stable depth (keeps Stable_Basics at a minimum complexity)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static saveMin = task.minRangeSlider.Value
        Static saveMax = task.maxRangeSlider.Value
        If saveMin <> task.minRangeSlider.Value Or saveMax <> task.maxRangeSlider.Value Then stable.externalReset = True
        stable.Run()

        colorize.src = stable.dst1
        colorize.Run()
        dst1 = stable.dst1
        dst2 = colorize.dst1
    End Sub
End Class

