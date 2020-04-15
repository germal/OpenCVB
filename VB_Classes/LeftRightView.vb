Imports cv = OpenCvSharp
Public Class LeftRightView_Basics : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "brightness", 0, 255, 100)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Show the left and right views from the 3D Camera"
        Select Case ocvb.parms.cameraIndex
            Case D400Cam, StereoLabsZED2
                ocvb.label1 = "Left Image"
                ocvb.label2 = "Right Image"
            Case Kinect4AzureCam
                ocvb.label1 = "Infrared Image"
                ocvb.label2 = "There is only one infrared image with Kinect"
                sliders.TrackBar1.Value = 0
            Case T265Camera
                ocvb.label1 = "Raw Left View Image (clipped to fit)"
                ocvb.label2 = "Raw Right Right Image (clipped to fit)"
                sliders.TrackBar1.Value = 0
        End Select
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = ocvb.leftView
        ocvb.result2 = ocvb.rightView

        ocvb.result1 += sliders.TrackBar1.Value
        ocvb.result2 += sliders.TrackBar1.Value
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class






Public Class LeftRightView_CompareUndistorted : Implements IDisposable
    Public sliders As New OptionsSliders
    Public fisheye As FishEye_Rectified
    Public Sub New(ocvb As AlgorithmData)
        fisheye = New FishEye_Rectified(ocvb)
        fisheye.externalUse = True

        sliders.setupTrackBar1(ocvb, "brightness", 0, 255, 0)
        sliders.setupTrackBar2(ocvb, "Slice Starting Y", 0, 300, 100)
        sliders.setupTrackBar3(ocvb, "Slice Height", 1, 300, 50)
        If ocvb.parms.ShowOptions Then sliders.Show()
        Select Case ocvb.parms.cameraIndex
            Case D400Cam, StereoLabsZED2
                ocvb.label1 = "Left Image"
                ocvb.label2 = "Right Image"
            Case Kinect4AzureCam
                ocvb.label1 = "Infrared Image"
                ocvb.label2 = "There is only one infrared image with Kinect"
                sliders.TrackBar1.Value = 0
            Case T265Camera
                ocvb.label1 = "Undistorted Slices of Left and Right Views"
                ocvb.label2 = "Undistorted Right Image"
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
            ocvb.result1 = New cv.Mat(ocvb.color.Height, ocvb.color.Width, cv.MatType.CV_8UC1, 0)
            ocvb.result2 = ocvb.rightView
            leftInput = ocvb.leftView
            rightInput = ocvb.rightView
        End If

        ocvb.result1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1, 0)
        ocvb.result2 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1, 0)

        If ocvb.parms.lowResolution Then
            leftInput = leftInput.Resize(ocvb.color.Size())
            rightInput = rightInput.Resize(ocvb.color.Size())
        End If
        Dim rSrc = New cv.Rect(0, sliceY, leftInput.Width, slideHeight)
        leftInput(rSrc).CopyTo(ocvb.result1(New cv.Rect(0, 100, leftInput.Width, slideHeight)))
        rightInput(rSrc).CopyTo(ocvb.result1(New cv.Rect(0, 100 + slideHeight, leftInput.Width, slideHeight)))

        ocvb.result2 = leftInput
        ocvb.result1 += sliders.TrackBar1.Value
        ocvb.result2 += sliders.TrackBar1.Value
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        fisheye.Dispose()
    End Sub
End Class





Public Class LeftRightView_CompareRaw : Implements IDisposable
    Dim lrView As LeftRightView_Basics
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "brightness", 0, 255, 100)
        sliders.setupTrackBar2(ocvb, "Slice Starting Y", 0, 300, 100)
        sliders.setupTrackBar3(ocvb, "Slice Height", 1, 120, 50)
        If ocvb.parms.ShowOptions Then sliders.Show()
        Select Case ocvb.parms.cameraIndex
            Case D400Cam, StereoLabsZED2
                ocvb.label1 = "Left Image"
                ocvb.label2 = "Right Image"
            Case Kinect4AzureCam
                ocvb.label1 = "Infrared Image"
                ocvb.label2 = "There is only one infrared image with Kinect"
                sliders.TrackBar1.Value = 0
            Case T265Camera
                ocvb.label1 = "Raw Left View Image"
                ocvb.label2 = "Raw Right Right Image"
                sliders.TrackBar1.Value = 50
        End Select
        lrView = New LeftRightView_Basics(ocvb)
        lrView.sliders.Hide()
        ocvb.desc = "Show slices of the left and right view next to each other for visual comparison"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)

        ocvb.leftView += sliders.TrackBar1.Value
        ocvb.rightView += sliders.TrackBar1.Value

        lrView.Run(ocvb)
        Dim leftView = ocvb.result1.Clone() ' we will be using result1 for output now.
        ocvb.result1.SetTo(0)

        Dim sliceY = sliders.TrackBar2.Value
        Dim slideHeight = sliders.TrackBar3.Value
        leftView(New cv.Rect(0, sliceY, leftView.Width, slideHeight)).CopyTo(ocvb.result1(New cv.Rect(0, 100, leftView.Width, slideHeight)))
        ocvb.result2(New cv.Rect(0, sliceY, leftView.Width, slideHeight)).CopyTo(ocvb.result1(New cv.Rect(0, 100 + slideHeight, leftView.Width, slideHeight)))
        Dim rSrc = New cv.Rect(0, sliceY, leftView.Width, slideHeight)
        leftView(rSrc).CopyTo(ocvb.result1(New cv.Rect(0, 100, leftView.Width, slideHeight)))
        ocvb.result2(rSrc).CopyTo(ocvb.result1(New cv.Rect(0, 100 + slideHeight, leftView.Width, slideHeight)))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        lrView.Dispose()
    End Sub
End Class





Public Class LeftRightView_Features : Implements IDisposable
    Dim lrView As LeftRightView_Basics
    Dim features As Features_GoodFeatures
    Public Sub New(ocvb As AlgorithmData)
        features = New Features_GoodFeatures(ocvb)
        features.externalUse = True

        lrView = New LeftRightView_Basics(ocvb)

        ocvb.desc = "Find GoodFeatures in the left and right depalettized infrared images"
        ocvb.label1 = "Left Image"
        ocvb.label2 = "Right Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        lrView.Run(ocvb)
        Dim leftView = ocvb.result1.Clone()
        Dim rightView = ocvb.result2.Clone()

        features.gray = rightView.Clone()
        features.Run(ocvb)
        rightView.CopyTo(ocvb.result2) ' save the right image
        For i = 0 To features.goodFeatures.Count - 1
            cv.Cv2.Circle(ocvb.result2, features.goodFeatures(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        features.gray = leftView
        features.Run(ocvb)
        For i = 0 To features.goodFeatures.Count - 1
            cv.Cv2.Circle(ocvb.result1, features.goodFeatures(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        features.Dispose()
        lrView.Dispose()
    End Sub
End Class




Public Class LeftRightView_Palettized : Implements IDisposable
    Dim lrView As LeftRightView_Basics
    Public Sub New(ocvb As AlgorithmData)
        lrView = New LeftRightView_Basics(ocvb)
        ocvb.desc = "Add color to the 8-bit infrared images."
        ocvb.label1 = "Left Image"
        ocvb.label2 = "Right Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        lrView.Run(ocvb)

        ocvb.result1 = ocvb.result1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.ApplyColorMap(ocvb.result1, ocvb.result1, cv.ColormapTypes.Rainbow)

        ocvb.result2 = ocvb.result2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.ApplyColorMap(ocvb.result2, ocvb.result2, cv.ColormapTypes.Rainbow)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        lrView.Dispose()
    End Sub
End Class




Public Class LeftRightView_BRISK : Implements IDisposable
    Dim lrView As LeftRightView_Basics
    Dim brisk As BRISK_Basics
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Add color to the 8-bit infrared images."
        ocvb.label1 = "Infrared Left Image"
        ocvb.label2 = "Infrared Right Image"

        brisk = New BRISK_Basics(ocvb)
        brisk.externalUse = True
        brisk.sliders.TrackBar1.Value = 20

        lrView = New LeftRightView_Basics(ocvb)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        lrView.Run(ocvb)
        brisk.src = ocvb.result2.Clone()
        brisk.Run(ocvb)

        For Each pt In brisk.features
            ocvb.result2.Circle(pt, 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        brisk.src = ocvb.result1.Clone()
        brisk.Run(ocvb)

        For Each pt In brisk.features
            ocvb.result1.Circle(pt, 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        brisk.Dispose()
        lrView.Dispose()
    End Sub
End Class


