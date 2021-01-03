Imports cv = OpenCvSharp
Public Class OptionsCommon_Depth
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
        sliders.setupTrackBar(2, "Top and Side Views Histogram threshold", 0, 200, 10)
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
        bins = task.binSlider.Value
        If minVal >= maxVal Then maxVal = minVal + 1

        Static saveMaxVal As Integer
        Static saveMinVal As Integer
        Static saveYRotate As Integer
        If saveMaxVal <> maxVal Or saveMinVal <> minVal Or saveYRotate <> task.yRotateSlider.Value Then
            task.depthOptionsChanged = True
            saveMaxVal = maxVal
            saveMinVal = minVal
            saveYRotate = task.yRotateSlider.Value
        Else
            task.depthOptionsChanged = False
        End If

        ' forced resize of the depth16 - probably a mistake but avoids failure when camera is switching from 1280 to 640 and vice versa
        If src.Width <> task.depth16.Width Then task.depth16 = task.depth16.Resize(task.color.Size)

        task.depth16.ConvertTo(task.depth32f, cv.MatType.CV_32F)
        cv.Cv2.InRange(task.depth32f, minVal, maxVal, depthMask)
        cv.Cv2.BitwiseNot(depthMask, noDepthMask)
        dst1 = task.depth32f.SetTo(0, noDepthMask)
    End Sub
End Class







Public Class OptionsCommon_Histogram
    Inherits VBparent
    Public Sub New()
        initParent()
        task.callTrace.Clear() ' special line to clear the tree view otherwise Options_Common is standalone.

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "SideView Frustrum adjustment", 1, 200, 57)
            sliders.setupTrackBar(1, "TopView Frustrum adjustment", 1, 200, 57)
            sliders.setupTrackBar(2, "SideCameraPoint adjustment", -100, 100, 0)
            sliders.setupTrackBar(3, "TopCameraPoint adjustment", -10, 10, 0)
        End If

        Dim sideFrustrumSlider = findSlider("SideView Frustrum adjustment")
        Dim topFrustrumSlider = findSlider("TopView Frustrum adjustment")
        Dim cameraYSlider = findSlider("SideCameraPoint adjustment")
        Dim cameraXSlider = findSlider("TopCameraPoint adjustment")

        ' The specification for each camera spells out the FOV angle
        ' The sliders adjust the depth data histogram to fill the frustrum which is built from the spec.
        Select Case ocvb.parms.cameraName
            Case VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam
                sideFrustrumSlider.Value = 58
                topFrustrumSlider.Value = 180
                cameraXSlider.Value = 0
                cameraYSlider.Value = If(ocvb.resolutionIndex = 1, -1, -2)
            Case VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2
                sideFrustrumSlider.Value = 53
                topFrustrumSlider.Value = 162
                cameraXSlider.Value = If(ocvb.resolutionIndex = 3, 38, 13)
                cameraYSlider.Value = -3
            Case VB_Classes.ActiveTask.algParms.camNames.MyntD1000
                sideFrustrumSlider.Value = 50
                topFrustrumSlider.Value = 105
                cameraXSlider.Value = If(ocvb.resolutionIndex = 1, 4, 8)
                cameraYSlider.Value = If(ocvb.resolutionIndex = 3, -8, -3)
            Case VB_Classes.ActiveTask.algParms.camNames.D435i
                If src.Width = 640 Then
                    sideFrustrumSlider.Value = 75
                    topFrustrumSlider.Value = 101
                    cameraXSlider.Value = 0
                    cameraYSlider.Value = 0
                Else
                    sideFrustrumSlider.Value = 57
                    topFrustrumSlider.Value = 175
                    cameraXSlider.Value = 0
                    cameraYSlider.Value = 0
                End If
            Case VB_Classes.ActiveTask.algParms.camNames.D455
                If src.Width = 640 Then
                    sideFrustrumSlider.Value = 86
                    topFrustrumSlider.Value = 113
                    cameraXSlider.Value = 1
                    cameraYSlider.Value = -1
                Else
                    sideFrustrumSlider.Value = 58
                    topFrustrumSlider.Value = 184
                    cameraXSlider.Value = 0
                    cameraYSlider.Value = -3
                End If
        End Select

        ocvb.sideFrustrumAdjust = ocvb.maxZ * sideFrustrumSlider.Value / 100 / 2
        ocvb.topFrustrumAdjust = ocvb.maxZ * topFrustrumSlider.Value / 100 / 2
        ocvb.sideCameraPoint = New cv.Point(0, CInt(src.Height / 2 + cameraYSlider.Value))
        ocvb.topCameraPoint = New cv.Point(CInt(src.Width / 2 + cameraXSlider.Value), CInt(src.Height))

        task.desc = "The options for the side view are shared with this algorithm"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        ocvb.trueText("This algorithm was created only to share the sliders used for the side views." + vbCrLf +
                      "Each camera setting was carefully set to reflect the specification for each camera.")
    End Sub
End Class