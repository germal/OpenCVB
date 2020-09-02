Imports cv = OpenCvSharp
Public Class LeftRightView_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "brightness", 0, 255, 100)
        desc = "Show the left and right views from the 3D Camera"
        Select Case ocvb.parms.cameraIndex
            Case VB_Classes.ActiveTask.algParms.D435i, VB_Classes.ActiveTask.algParms.StereoLabsZED2
                label1 = "Left Image"
                label2 = "Right Image"
            Case VB_Classes.ActiveTask.algParms.Kinect4AzureCam, VB_Classes.ActiveTask.algParms.L515
                label1 = "Infrared Image"
                label2 = "There is only one infrared image"
                sliders.trackbar(0).Value = 0
            Case VB_Classes.ActiveTask.algParms.T265Camera
                label1 = "Raw Left View Image (clipped to fit)"
                label2 = "Raw Right Right Image (clipped to fit)"
                sliders.trackbar(0).Value = 0
        End Select
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1 = ocvb.leftView
        dst2 = ocvb.rightView

        dst1 += sliders.trackbar(0).Value
        dst2 += sliders.trackbar(0).Value
    End Sub
End Class






Public Class LeftRightView_CompareUndistorted
    Inherits ocvbClass
    Public fisheye As FishEye_Rectified
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        fisheye = New FishEye_Rectified(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "brightness", 0, 255, 0)
        sliders.setupTrackBar(1, "Slice Starting Y", 0, 300, 100)
        sliders.setupTrackBar(2, "Slice Height", 1, (ocvb.color.Rows - 100) / 2, 30)

        Select Case ocvb.parms.cameraIndex
            Case VB_Classes.ActiveTask.algParms.D435i, VB_Classes.ActiveTask.algParms.StereoLabsZED2
                label1 = "Left Image"
                label2 = "Right Image"
            Case VB_Classes.ActiveTask.algParms.Kinect4AzureCam, VB_Classes.ActiveTask.algParms.L515
                label1 = "Infrared Image"
                label2 = "There is only one infrared image"
                sliders.trackbar(0).Value = 0
            Case VB_Classes.ActiveTask.algParms.T265Camera
                label1 = "Undistorted Slices of Left and Right Views"
                label2 = "Undistorted Right Image"
                sliders.trackbar(0).Value = 50
        End Select
        desc = "Show slices of the left and right view next to each other for visual comparison - right view needs more work"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim sliceY = sliders.trackbar(1).Value
        Dim slideHeight = sliders.trackbar(2).Value
        Dim leftInput As cv.Mat, rightInput As cv.Mat
        If ocvb.parms.cameraIndex = VB_Classes.ActiveTask.algParms.T265Camera Then
            fisheye.src = src
            fisheye.Run(ocvb)
            leftInput = fisheye.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            rightInput = fisheye.rightView
        Else
            leftInput = ocvb.leftView
            rightInput = ocvb.rightView
        End If

        dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8UC1, 0)
        dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8UC1, 0)

        Dim rSrc = New cv.Rect(0, sliceY, leftInput.Width, slideHeight)
        leftInput(rSrc).CopyTo(dst1(New cv.Rect(0, 100, leftInput.Width, slideHeight)))
        rightInput(rSrc).CopyTo(dst1(New cv.Rect(0, 100 + slideHeight, leftInput.Width, slideHeight)))

        dst2 = leftInput
        dst1 += sliders.trackbar(0).Value
        dst2 += sliders.trackbar(0).Value
    End Sub
End Class





Public Class LeftRightView_CompareRaw
    Inherits ocvbClass
    Dim lrView As LeftRightView_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "brightness", 0, 255, 100)
        sliders.setupTrackBar(1, "Slice Starting Y", 0, 300, 100)
        sliders.setupTrackBar(2, "Slice Height", 1, (ocvb.color.Rows - 100) / 2, 30)
        Select Case ocvb.parms.cameraIndex
            Case VB_Classes.ActiveTask.algParms.D435i, VB_Classes.ActiveTask.algParms.StereoLabsZED2,
                label1 = "Left Image"
                label2 = "Right Image"
            Case VB_Classes.ActiveTask.algParms.Kinect4AzureCam, VB_Classes.ActiveTask.algParms.L515
                label1 = "Infrared Image"
                label2 = "There is only one infrared image"
                sliders.trackbar(0).Value = 0
            Case VB_Classes.ActiveTask.algParms.T265Camera
                label1 = "Raw Left View Image"
                label2 = "Raw Right Right Image"
                sliders.trackbar(0).Value = 50
        End Select
        lrView = New LeftRightView_Basics(ocvb)
        lrView.sliders.Hide()
        desc = "Show slices of the left and right view next to each other for visual comparison"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        lrView.Run(ocvb)

        dst1 = New cv.Mat(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, 0)

        Dim sliceY = sliders.trackbar(1).Value
        Dim slideHeight = sliders.trackbar(2).Value
        Dim r1 = New cv.Rect(0, sliceY, lrView.dst1.Width, slideHeight)
        Dim r2 = New cv.Rect(0, 100, lrView.dst1.Width, slideHeight)
        lrView.dst1(r1).CopyTo(dst1(r2))

        r2.Y += slideHeight
        lrView.dst2(r1).CopyTo(dst1(r2))
        dst2 = lrView.dst2
    End Sub
End Class





Public Class LeftRightView_Features
    Inherits ocvbClass
    Dim lrView As LeftRightView_Basics
    Dim features As Features_GoodFeatures
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        features = New Features_GoodFeatures(ocvb)

        lrView = New LeftRightView_Basics(ocvb)

        desc = "Find GoodFeatures in the left and right depalettized infrared images"
        label1 = "Left Image"
        label2 = "Right Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        lrView.Run(ocvb)

        features.src = lrView.dst2
        features.Run(ocvb)
        lrView.dst2.CopyTo(dst2)
        For i = 0 To features.goodFeatures.Count - 1
            cv.Cv2.Circle(dst2, features.goodFeatures(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        features.src = lrView.dst1
        features.Run(ocvb)
        lrView.dst1.CopyTo(dst1)
        For i = 0 To features.goodFeatures.Count - 1
            cv.Cv2.Circle(dst1, features.goodFeatures(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class




Public Class LeftRightView_Palettized
    Inherits ocvbClass
    Dim lrView As LeftRightView_Basics
    Dim palette As Palette_ColorMap
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        lrView = New LeftRightView_Basics(ocvb)
        palette = New Palette_ColorMap(ocvb)

        desc = "Add color to the 8-bit infrared images."
        label1 = "Left Image"
        label2 = "Right Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        lrView.Run(ocvb)

        palette.src = lrView.dst1
        palette.Run(ocvb)
        dst1 = palette.dst1

        palette.src = lrView.dst2
        palette.Run(ocvb)
        dst2 = palette.dst1
    End Sub
End Class




Public Class LeftRightView_BRISK
    Inherits ocvbClass
    Dim lrView As LeftRightView_Basics
    Dim brisk As BRISK_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        desc = "Add color to the 8-bit infrared images."
        label1 = "Infrared Left Image"
        label2 = "Infrared Right Image"

        brisk = New BRISK_Basics(ocvb)
        brisk.sliders.trackbar(0).Value = 20

        lrView = New LeftRightView_Basics(ocvb)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        lrView.Run(ocvb)
        brisk.src = lrView.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        brisk.Run(ocvb)
        dst2 = brisk.dst1

        brisk.src = lrView.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        brisk.Run(ocvb)
        dst1 = brisk.dst1
    End Sub
End Class



