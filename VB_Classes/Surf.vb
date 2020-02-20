Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports CS_Classes

' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_Basics_CS : Implements IDisposable
    Public radio As New OptionsRadioButtons
    Public sliders As New OptionsSliders
    Public CS_SurfBasics As New CS_SurfBasics
    Dim fisheye As FishEye_Basics
    Public srcLeft As New cv.Mat
    Public srcRight As New cv.Mat
    Public dst As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        fisheye = New FishEye_Basics(ocvb)
        fisheye.externalUse = True

        radio.Setup(ocvb, 2)
        radio.check(0).Text = "Use BF Matcher"
        radio.check(1).Text = "Use Flann Matcher"
        radio.check(0).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()

        sliders.setupTrackBar1(ocvb, "Hessian threshold", 1, 5000, 2000)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Compare 2 images to get a homography.  We will use left and right images."
        ocvb.label1 = "BF Matcher output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            If ocvb.parms.cameraIndex = T265Camera Then
                fisheye.Run(ocvb)
                srcLeft = fisheye.leftView
                srcRight = fisheye.rightView
            Else
                srcLeft = ocvb.leftView
                srcRight = ocvb.rightView
            End If
        End If
        dst = New cv.Mat(srcLeft.Rows, srcLeft.Cols * 2, cv.MatType.CV_8UC3)

        CS_SurfBasics.Run(srcLeft, srcRight, dst, sliders.TrackBar1.Value, radio.check(0).Checked)

        ' resize is needed for the T265
        If dst.Width <> ocvb.color.Width * 2 Then dst = dst.Resize(New cv.Size(ocvb.color.Width * 2, srcLeft.Height))
        If externalUse = False Then
            dst(New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height)).CopyTo(ocvb.result1)
            dst(New cv.Rect(ocvb.color.Width, 0, ocvb.color.Width, ocvb.color.Height)).CopyTo(ocvb.result2)
            ocvb.label1 = If(radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
            ocvb.label1 += " " + CStr(CS_SurfBasics.keypoints1.Count)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
        fisheye.Dispose()
    End Sub
End Class






Public Class Surf_Basics : Implements IDisposable
    Dim grid As Thread_Grid
    Dim surf As Surf_Basics_CS
    Dim fisheye As FishEye_Basics
    Public Sub New(ocvb As AlgorithmData)
        fisheye = New FishEye_Basics(ocvb)
        fisheye.externalUse = True

        surf = New Surf_Basics_CS(ocvb)
        surf.externalUse = True

        grid = New Thread_Grid(ocvb)
        grid.externalUse = True
        grid.sliders.TrackBar1.Value = ocvb.color.Width

        ' The Surf OpenCV code is not reentrant.  There is some interference between threads.  This reduces it!
        ' To experiments with the problem, vary the height to be lower and observe that the bottom of the image is chaotically updated.
        grid.sliders.TrackBar2.Value = 72
        If ocvb.parms.cameraIndex = T265Camera Then grid.sliders.TrackBar2.Value = 144

        ocvb.desc = "Use left and right views to match points in horizontal slices."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.sliders.TrackBar1.Value = ocvb.color.Width
        grid.Run(ocvb)

        If ocvb.parms.cameraIndex = T265Camera Then fisheye.Run(ocvb)

        Dim roilist = grid.roiList
        For i = 0 To roilist.Count - 1
            Dim roi = roilist(i)
            If ocvb.parms.cameraIndex = T265Camera Then
                surf.srcLeft = fisheye.leftView(roi).Clone()
                surf.srcRight = fisheye.rightView(roi).Clone()
            Else
                ocvb.leftView(roi).CopyTo(surf.srcLeft)
                ocvb.rightView(roi).CopyTo(surf.srcRight)
            End If
            surf.Run(ocvb)
            surf.dst(New cv.Rect(0, 0, roi.Width, roi.Height)).CopyTo(ocvb.result1(roi))
            surf.dst(New cv.Rect(roi.Width, 0, roi.Width, roi.Height)).CopyTo(ocvb.result2(roi))
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
        surf.Dispose()
        fisheye.Dispose()
    End Sub
End Class






' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_Basics_MT : Implements IDisposable
    Dim radio As New OptionsRadioButtons
    Dim grid As Thread_Grid
    Dim surfs(0) As Surf_Basics_CS
    Dim fisheye As FishEye_Basics
    Public Sub New(ocvb As AlgorithmData)
        fisheye = New FishEye_Basics(ocvb)
        fisheye.externalUse = True

        grid = New Thread_Grid(ocvb)
        grid.externalUse = True
        grid.sliders.TrackBar1.Value = ocvb.color.Width

        ' The Surf OpenCV code is not reentrant.  There is some interference between threads.  This reduces it!
        ' To experiments with the problem, vary the height to be lower and observe that the bottom of the image is chaotically updated.
        grid.sliders.TrackBar2.Value = 72
        If ocvb.parms.cameraIndex = T265Camera Then grid.sliders.TrackBar2.Value = 144

        radio.Setup(ocvb, 2)
        radio.check(0).Text = "Use BF Matcher"
        radio.check(1).Text = "Use Flann Matcher"
        radio.check(0).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()

        ocvb.parms.ShowOptions = False ' this turns off all the options form for the algorithm

        ocvb.desc = "A multi-threaded version of the Surf algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.sliders.TrackBar1.Value = ocvb.color.Width
        grid.Run(ocvb)

        Dim threads = grid.roiList.Count ' the number of threads is the number of horizontal slices in the image.
        If surfs.Count <> threads Then
            For i = 0 To surfs.Count - 1
                If surfs(i) IsNot Nothing Then surfs(i).Dispose()
            Next
            ReDim surfs(threads - 1)
            For i = 0 To surfs.Count - 1
                surfs(i) = New Surf_Basics_CS(ocvb)
                surfs(i).externalUse = True
                surfs(i).radio.check(0).Checked = If(radio.check(0).Checked, 1, 0)
                surfs(i).radio.check(1).Checked = If(radio.check(1).Checked, 1, 0)
            Next
        End If

        If ocvb.parms.cameraIndex = T265Camera Then fisheye.Run(ocvb)

        Dim result1 = ocvb.result1.Clone()
        Dim result2 = ocvb.result2.Clone()
        Parallel.For(0, grid.roiList.Count - 1,
        Sub(i)
            Dim roi = grid.roiList(i)
            If ocvb.parms.cameraIndex = T265Camera Then
                surfs(i).srcLeft = fisheye.leftView(roi)
                surfs(i).srcRight = fisheye.rightView(roi)
            Else
                surfs(i).srcLeft = ocvb.leftView(roi)
                surfs(i).srcRight = ocvb.rightView(roi)
            End If
            surfs(i).Run(ocvb)
            Dim leftRect = New cv.Rect(0, 0, roi.Width, roi.Height)
            Dim rightRect = New cv.Rect(roi.Width, 0, roi.Width, roi.Height)
            If roi.Height = grid.roiList(0).Height Then
                result1(roi) = surfs(i).dst(leftRect)
                result2(roi) = surfs(i).dst(rightRect)
            End If
        End Sub)

        ocvb.result1 = result1.Clone()
        ocvb.result2 = result2.Clone()
        ocvb.label2 = If(radio.check(0).Checked, "Right VIew - BF Matcher output", "Right View - Flann Matcher output")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
        For i = 0 To surfs.Count - 1
            If surfs(i) IsNot Nothing Then surfs(i).Dispose()
        Next
        fisheye.Dispose()
        radio.Dispose()
    End Sub
End Class







' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
' The real value of this exercise is to show how poorly the Surf algorithm is matching points.
' The points must match in y or the camera is poorly calibrated.  Loosen the restriction and it still is poor.
' Only occasionally is it finding points that really match along the (approximate) y-axis.
Public Class Surf_DrawMatchManual_CS : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim surf As Surf_Basics_CS
    Public Sub New(ocvb As AlgorithmData)
        surf = New Surf_Basics_CS(ocvb)
        surf.CS_SurfBasics.drawPoints = False

        sliders.setupTrackBar1(ocvb, "Surf Vertical Range to Search", 0, 50, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Compare 2 images to get a homography but draw the points manually in horizontal slices."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        surf.Run(ocvb)
        ocvb.result1 = surf.srcLeft.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.result2 = surf.srcRight.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim keys1 = surf.CS_SurfBasics.keypoints1
        Dim keys2 = surf.CS_SurfBasics.keypoints2
        Dim matchCount As Integer
        For i = 0 To keys1.Count - 1
            Dim pt = keys1(i).Pt
            For j = 0 To keys2.Count - 1
                If Math.Abs(keys2(i).Pt.Y - pt.Y) < sliders.TrackBar1.Value Then
                    ocvb.result1.Circle(pt, 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                    ocvb.result2.Circle(keys2(i).Pt, 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                    keys2(i).Pt.Y = -1 ' so we don't match it again.
                    matchCount += 1
                End If
            Next
        Next
        ocvb.label2 = "Right View - " + CStr(matchCount) + " matches"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        surf.Dispose()
    End Sub
End Class