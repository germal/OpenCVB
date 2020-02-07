Imports cv = OpenCvSharp
Public Class LeftRightView_Basics : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "brightness", 0, 255, 100)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Show the left and right views from the 3D Camera"
        Select Case ocvb.parms.cameraIndex
            Case D400Cam
                ocvb.label1 = "Infrared Left Image"
                ocvb.label2 = "Infrared Right Image"
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
        If ocvb.parms.cameraIndex = T265Camera Then
            ocvb.result1 = New cv.Mat(ocvb.color.Height, ocvb.color.Width, cv.MatType.CV_8UC1, 0)
            ocvb.result2 = New cv.Mat(ocvb.color.Height, ocvb.color.Width, cv.MatType.CV_8UC1, 0)
            Dim h = ocvb.parms.height
            If ocvb.parms.lowResolution Then h = ocvb.parms.height * 2

            Dim rawWidth = ocvb.leftView.Width
            Dim rawHeight = ocvb.leftView.Height
            Dim rawSrc = New cv.Rect((rawWidth - rawWidth * h / rawHeight) / 2, 0, rawWidth * h / rawHeight, h)
            Dim rawDst = New cv.Rect(0, 0, rawSrc.Width, rawSrc.Height)
            If ocvb.parms.lowResolution Then rawDst = New cv.Rect(0, 0, rawSrc.Width / 2 + 1, rawSrc.Height / 2)

            Dim tmp As New cv.Mat(New cv.Size(rawSrc.Width, rawSrc.Height), cv.MatType.CV_8UC1)
            ocvb.leftView(rawSrc).CopyTo(tmp)
            If ocvb.parms.lowResolution Then tmp = tmp.Resize(New cv.Size(rawDst.Width, tmp.Height / 2))
            ocvb.result1(rawDst) = tmp

            tmp = New cv.Mat(New cv.Size(rawSrc.Width, rawSrc.Height), cv.MatType.CV_8UC1)
            ocvb.rightView(rawSrc).CopyTo(tmp)
            If ocvb.parms.lowResolution Then tmp = tmp.Resize(New cv.Size(rawDst.Width, tmp.Height / 2))
            ocvb.result2(rawDst) = tmp
        End If
        ocvb.result1 += sliders.TrackBar1.Value
        ocvb.result2 += sliders.TrackBar1.Value
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class






Public Class LeftRightView_CompareUndistorted : Implements IDisposable
    Public sliders As New OptionsSliders
    Public fisheye As FishEye_Basics
    Public Sub New(ocvb As AlgorithmData)
        fisheye = New FishEye_Basics(ocvb)
        fisheye.externalUse = True

        sliders.setupTrackBar1(ocvb, "brightness", 0, 255, 0)
        sliders.setupTrackBar2(ocvb, "Slice Starting Y", 0, 300, 100)
        sliders.setupTrackBar3(ocvb, "Slice Height", 0, 300, 50)
        If ocvb.parms.ShowOptions Then sliders.Show()
        Select Case ocvb.parms.cameraIndex
            Case D400Cam
                ocvb.label1 = "Infrared Left Image"
                ocvb.label2 = "Infrared Right Image"
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

        leftInput(New cv.Rect(0, sliceY, leftInput.Width, slideHeight)).CopyTo(ocvb.result1(New cv.Rect(0, 100, leftInput.Width, slideHeight)))
        rightInput(New cv.Rect(0, sliceY, leftInput.Width, slideHeight)).CopyTo(ocvb.result1(New cv.Rect(0, 100 + slideHeight, leftInput.Width, slideHeight)))

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
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "brightness", 0, 255, 100)
        sliders.setupTrackBar2(ocvb, "Slice Starting Y", 0, 300, 100)
        sliders.setupTrackBar3(ocvb, "Slice Height", 0, 300, 50)
        If ocvb.parms.ShowOptions Then sliders.Show()
        Select Case ocvb.parms.cameraIndex
            Case D400Cam
                ocvb.label1 = "Infrared Left Image"
                ocvb.label2 = "Infrared Right Image"
            Case Kinect4AzureCam
                ocvb.label1 = "Infrared Image"
                ocvb.label2 = "There is only one infrared image with Kinect"
                sliders.TrackBar1.Value = 0
            Case T265Camera
                ocvb.label1 = "Raw Left View Image"
                ocvb.label2 = "Raw Right Right Image"
                sliders.TrackBar1.Value = 50
        End Select
        ocvb.desc = "Show slices of the left and right view next to each other for visual comparison"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.leftView += sliders.TrackBar1.Value
        ocvb.rightView += sliders.TrackBar1.Value

        ocvb.result1 = New cv.Mat(ocvb.color.Height, ocvb.color.Width, cv.MatType.CV_8UC1, 0)
        ocvb.result2 = ocvb.leftView

        ocvb.result1.SetTo(0)
        Dim sliceY = sliders.TrackBar2.Value
        Dim slideHeight = sliders.TrackBar3.Value
        ocvb.leftView(New cv.Rect(0, sliceY, ocvb.leftView.Width, slideHeight)).CopyTo(ocvb.result1(New cv.Rect(0, 100, ocvb.leftView.Width, slideHeight)))
        ocvb.rightView(New cv.Rect(0, sliceY, ocvb.leftView.Width, slideHeight)).CopyTo(ocvb.result1(New cv.Rect(0, 100 + slideHeight, ocvb.leftView.Width, slideHeight)))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class





Public Class LeftRightView_Features : Implements IDisposable
    Dim features As Features_GoodFeatures
    Public Sub New(ocvb As AlgorithmData)
        features = New Features_GoodFeatures(ocvb)
        features.externalUse = True

        ocvb.desc = "Find GoodFeatures in the left and right depalettized infrared images"
        ocvb.label1 = "Infrared Left Image"
        ocvb.label2 = "Infrared Right Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        features.gray = ocvb.rightView
        features.Run(ocvb)
        ocvb.result1.CopyTo(ocvb.result2) ' save the right image

        features.gray = ocvb.leftView
        features.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        features.Dispose()
    End Sub
End Class




Public Class LeftRightView_Palettized : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Add color to the 8-bit infrared images."
        ocvb.label1 = "Infrared Left Image"
        ocvb.label2 = "Infrared Right Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = ocvb.leftView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.ApplyColorMap(ocvb.result1, ocvb.result1, cv.ColormapTypes.Rainbow)

        ocvb.result2 = ocvb.rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.ApplyColorMap(ocvb.result2, ocvb.result2, cv.ColormapTypes.Rainbow)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class LeftRightView_BRISK : Implements IDisposable
    Dim brisk As BRISK_Basics
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Add color to the 8-bit infrared images."
        ocvb.label1 = "Infrared Left Image"
        ocvb.label2 = "Infrared Right Image"

        brisk = New BRISK_Basics(ocvb)
        brisk.externalUse = True
        brisk.sliders.TrackBar1.Value = 20
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        brisk.src = ocvb.rightView.Clone()
        ocvb.result2 = ocvb.rightView.Clone()
        brisk.Run(ocvb)

        For Each pt In brisk.features
            ocvb.result2.Circle(pt, 2, cv.Scalar.Green, -1, cv.LineTypes.AntiAlias)
        Next

        brisk.src = ocvb.leftView.Clone()
        ocvb.result1 = ocvb.leftView.Clone()
        brisk.Run(ocvb)

        For Each pt In brisk.features
            ocvb.result1.Circle(pt, 2, cv.Scalar.Green, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        brisk.Dispose()
    End Sub
End Class


