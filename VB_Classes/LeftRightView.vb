Imports cv = OpenCvSharp
Public Class LeftRightView_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "brightness", 0, 255, 100)
        ocvb.desc = "Show the left and right views from the 3D Camera"
        Select Case ocvb.parms.cameraIndex
            Case D400Cam, StereoLabsZED2
                label1 = "Left Image"
                label2 = "Right Image"
            Case Kinect4AzureCam
                label1 = "Infrared Image"
                label2 = "There is only one infrared image with Kinect"
                sliders.TrackBar1.Value = 0
            Case T265Camera
                label1 = "Raw Left View Image (clipped to fit)"
                label2 = "Raw Right Right Image (clipped to fit)"
                sliders.TrackBar1.Value = 0
        End Select
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1 = ocvb.leftView
        dst2 = ocvb.rightView

        dst1 += sliders.TrackBar1.Value
        dst2 += sliders.TrackBar1.Value
    End Sub
End Class






Public Class LeftRightView_CompareUndistorted
    Inherits ocvbClass
    Public fisheye As FishEye_Rectified
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        fisheye = New FishEye_Rectified(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "brightness", 0, 255, 0)
        sliders.setupTrackBar2(ocvb, caller, "Slice Starting Y", 0, 300, 100)
        sliders.setupTrackBar3(ocvb, caller, "Slice Height", 1, 300, 50)
        Select Case ocvb.parms.cameraIndex
            Case D400Cam, StereoLabsZED2
                label1 = "Left Image"
                label2 = "Right Image"
            Case Kinect4AzureCam
                label1 = "Infrared Image"
                label2 = "There is only one infrared image with Kinect"
                sliders.TrackBar1.Value = 0
            Case T265Camera
                label1 = "Undistorted Slices of Left and Right Views"
                label2 = "Undistorted Right Image"
                sliders.TrackBar1.Value = 50
        End Select
        ocvb.desc = "Show slices of the left and right view next to each other for visual comparison"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim sliceY = sliders.TrackBar2.Value
        Dim slideHeight = sliders.TrackBar3.Value
        Dim leftInput As cv.Mat, rightInput As cv.Mat
        If ocvb.parms.cameraIndex = T265Camera Then
            fisheye.Run(ocvb)
            leftInput = fisheye.leftView.Clone()
            rightInput = fisheye.rightView.Clone()
        Else
            dst1 = New cv.Mat(ocvb.color.Height, ocvb.color.Width, cv.MatType.CV_8UC1, 0)
            dst2 = ocvb.rightView
            leftInput = ocvb.leftView
            rightInput = ocvb.rightView
        End If

        dst1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1, 0)
        dst2 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1, 0)

        If ocvb.parms.lowResolution Then
            leftInput = leftInput.Resize(ocvb.color.Size())
            rightInput = rightInput.Resize(ocvb.color.Size())
        End If
        Dim rSrc = New cv.Rect(0, sliceY, leftInput.Width, slideHeight)
        leftInput(rSrc).CopyTo(dst1(New cv.Rect(0, 100, leftInput.Width, slideHeight)))
        rightInput(rSrc).CopyTo(dst1(New cv.Rect(0, 100 + slideHeight, leftInput.Width, slideHeight)))

        dst2 = leftInput
        dst1 += sliders.TrackBar1.Value
        dst2 += sliders.TrackBar1.Value
    End Sub
End Class





Public Class LeftRightView_CompareRaw
    Inherits ocvbClass
    Dim lrView As LeftRightView_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "brightness", 0, 255, 100)
        sliders.setupTrackBar2(ocvb, caller, "Slice Starting Y", 0, 300, 100)
        sliders.setupTrackBar3(ocvb, caller, "Slice Height", 1, 120, 50)
        Select Case ocvb.parms.cameraIndex
            Case D400Cam, StereoLabsZED2
                label1 = "Left Image"
                label2 = "Right Image"
            Case Kinect4AzureCam
                label1 = "Infrared Image"
                label2 = "There is only one infrared image with Kinect"
                sliders.TrackBar1.Value = 0
            Case T265Camera
                label1 = "Raw Left View Image"
                label2 = "Raw Right Right Image"
                sliders.TrackBar1.Value = 50
        End Select
        lrView = New LeftRightView_Basics(ocvb, caller)
        lrView.sliders.Hide()
        ocvb.desc = "Show slices of the left and right view next to each other for visual comparison"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)

        ocvb.leftView += sliders.TrackBar1.Value
        ocvb.rightView += sliders.TrackBar1.Value

        lrView.Run(ocvb)
        Dim leftView = dst1.Clone() ' we will be using result1 for output now.

        Dim sliceY = sliders.TrackBar2.Value
        Dim slideHeight = sliders.TrackBar3.Value
        leftView(New cv.Rect(0, sliceY, leftView.Width, slideHeight)).CopyTo(dst1(New cv.Rect(0, 100, leftView.Width, slideHeight)))
        dst2(New cv.Rect(0, sliceY, leftView.Width, slideHeight)).CopyTo(dst1(New cv.Rect(0, 100 + slideHeight, leftView.Width, slideHeight)))
        Dim rSrc = New cv.Rect(0, sliceY, leftView.Width, slideHeight)
        leftView(rSrc).CopyTo(dst1(New cv.Rect(0, 100, leftView.Width, slideHeight)))
        dst2(rSrc).CopyTo(dst1(New cv.Rect(0, 100 + slideHeight, leftView.Width, slideHeight)))
    End Sub
End Class





Public Class LeftRightView_Features
    Inherits ocvbClass
    Dim lrView As LeftRightView_Basics
    Dim features As Features_GoodFeatures
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        features = New Features_GoodFeatures(ocvb, caller)

        lrView = New LeftRightView_Basics(ocvb, caller)

        ocvb.desc = "Find GoodFeatures in the left and right depalettized infrared images"
        label1 = "Left Image"
        label2 = "Right Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        lrView.Run(ocvb)
        Dim leftView = dst1.Clone()
        Dim rightView = dst2.Clone()

        features.gray = rightView.Clone()
        features.Run(ocvb)
        rightView.CopyTo(dst2) ' save the right image
        For i = 0 To features.goodFeatures.Count - 1
            cv.Cv2.Circle(dst2, features.goodFeatures(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        features.gray = leftView
        features.Run(ocvb)
        For i = 0 To features.goodFeatures.Count - 1
            cv.Cv2.Circle(dst1, features.goodFeatures(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class




Public Class LeftRightView_Palettized
    Inherits ocvbClass
    Dim lrView As LeftRightView_Basics
    Dim palette As Palette_ColorMap
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        lrView = New LeftRightView_Basics(ocvb, caller)
        palette = New Palette_ColorMap(ocvb, caller)

        ocvb.desc = "Add color to the 8-bit infrared images."
        label1 = "Left Image"
        label2 = "Right Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        lrView.Run(ocvb)
        Dim left = dst1.Clone()
        Dim right = dst2.Clone()

        palette.src = dst1
        palette.Run(ocvb)
        left = dst1.Clone()

        palette.src = right
        palette.Run(ocvb)
        dst2 = dst1

        dst1 = left
    End Sub
End Class




Public Class LeftRightView_BRISK
    Inherits ocvbClass
    Dim lrView As LeftRightView_Basics
    Dim brisk As BRISK_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Add color to the 8-bit infrared images."
        label1 = "Infrared Left Image"
        label2 = "Infrared Right Image"

        brisk = New BRISK_Basics(ocvb, caller)
        brisk.sliders.TrackBar1.Value = 20

        lrView = New LeftRightView_Basics(ocvb, caller)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        lrView.Run(ocvb)
        brisk.src = dst2.Clone()
        brisk.Run(ocvb)

        For Each pt In brisk.features
            dst2.Circle(pt, 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        brisk.src = dst1.Clone()
        brisk.Run(ocvb)

        For Each pt In brisk.features
            dst1.Circle(pt, 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class



