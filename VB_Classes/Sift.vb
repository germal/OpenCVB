Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports CS_Classes

' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Sift_Basics_CS
    Inherits ocvbClass
        Dim CS_SiftBasics As New CS_SiftBasics
    Dim fisheye As FishEye_Rectified
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        fisheye = New FishEye_Rectified(ocvb, caller)

        radio.Setup(ocvb, caller,2)
        radio.check(0).Text = "Use BF Matcher"
        radio.check(1).Text = "Use Flann Matcher"
        radio.check(0).Checked = True

        sliders.setupTrackBar1(ocvb, caller, "Points to Match", 1, 1000, 200)

        ocvb.desc = "Compare 2 images to get a homography.  We will use left and right images."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim dst As New cv.Mat(ocvb.leftView.Rows, ocvb.leftView.Cols * 2, cv.MatType.CV_8UC3)

        If ocvb.parms.cameraIndex = T265Camera Then
            fisheye.Run(ocvb)
            CS_SiftBasics.Run(fisheye.leftView, fisheye.rightView, dst, radio.check(0).Checked, sliders.TrackBar1.Value)
        Else
            CS_SiftBasics.Run(ocvb.leftView, ocvb.rightView, dst, radio.check(0).Checked, sliders.TrackBar1.Value)
            If ocvb.parms.lowResolution Then dst = dst.Resize(New cv.Size(ocvb.result1.Width * 2, ocvb.result1.Height))
        End If

        dst(New cv.Rect(0, 0, ocvb.result1.Width, ocvb.result1.Height)).CopyTo(ocvb.result1)
        dst(New cv.Rect(ocvb.result1.Width, 0, ocvb.result1.Width, ocvb.result1.Height)).CopyTo(ocvb.result2)

        ocvb.label1 = If(radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
    Public Sub MyDispose()
                        fisheye.Dispose()
    End Sub
End Class




Public Class Sift_Basics_CS_MT
    Inherits ocvbClass
    Dim grid As Thread_Grid
        Dim CS_SiftBasics As New CS_SiftBasics
    Dim fisheye As FishEye_Rectified
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        fisheye = New FishEye_Rectified(ocvb, caller)
        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = ocvb.color.Width - 1 ' we are just taking horizontal slices of the image.
        grid.sliders.TrackBar2.Value = ocvb.color.Height / 2

        radio.Setup(ocvb, caller,2)
        radio.check(0).Text = "Use BF Matcher"
        radio.check(1).Text = "Use Flann Matcher"
        radio.check(0).Checked = True

        sliders.setupTrackBar1(ocvb, caller, "Points to Match", 1, 1000, 100)

        ocvb.desc = "Compare 2 images to get a homography.  We will use left and right images."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim leftView As cv.Mat
        Dim rightView As cv.Mat
        If ocvb.parms.cameraIndex = T265Camera Then
            fisheye.Run(ocvb)
            leftView = fisheye.leftView
            rightView = fisheye.rightView
        Else
            leftView = ocvb.leftView
            rightView = ocvb.rightView
        End If
        grid.Run(ocvb)

        Dim dst As New cv.Mat(ocvb.color.Rows, ocvb.color.Cols * 2, cv.MatType.CV_8UC3)

        Dim numFeatures = sliders.TrackBar1.Value
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim left = leftView(roi).Clone()  ' sift wants the inputs to be continuous and roi-modified Mats are not continuous.
            Dim right = rightView(roi).Clone()
            Dim dstROI = New cv.Rect(roi.X, roi.Y, roi.Width * 2, roi.Height)
            Dim dstTmp = dst(dstROI).Clone()
            CS_SiftBasics.Run(left, right, dstTmp, radio.check(0).Checked, numFeatures)
            dstTmp.CopyTo(dst(dstROI))
        End Sub)

        dst(New cv.Rect(0, 0, ocvb.result1.Width, ocvb.result1.Height)).CopyTo(ocvb.result1)
        dst(New cv.Rect(ocvb.result1.Width, 0, ocvb.result1.Width, ocvb.result1.Height)).CopyTo(ocvb.result2)

        ocvb.label1 = If(radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
    Public Sub MyDispose()
        grid.Dispose()
                        fisheye.Dispose()
    End Sub
End Class
