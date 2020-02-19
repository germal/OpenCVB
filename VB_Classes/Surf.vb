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
        If ocvb.frameCount >= 1 Then Exit Sub
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
                ocvb.result1(roi) = surfs(i).dst(leftRect)
                ocvb.result2(roi) = surfs(i).dst(rightRect)
            End If
        End Sub)

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
'Public Class Surf_DrawMatchManual_CS : Implements IDisposable
'    Dim surf As Surf_Basics_CS
'    Public Sub New(ocvb As AlgorithmData)
'        surf = New Surf_Basics_CS(ocvb)
'        surf.CS_SurfBasics.drawPoints = False

'        ocvb.desc = "Compare 2 images to get a homography but draw the points manually!"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        surf.Run(ocvb)
'        cv.Cv2.ImShow("dst", surf.dst)
'        'Dim keys1 = surf.CS_SurfBasics.keypoints1
'        'Dim keys2 = surf.CS_SurfBasics.keypoints2
'        'For i = 0 To Math.Min(keys1.Count, keys2.Count) - 1
'        '    If Math.Abs(keys1(i).Pt.Y - keys2(i).Pt.Y) < 10 Then
'        '        surf.dst.Line(keys1(i).Pt, keys2(i).Pt, cv.Scalar.Yellow, 3, cv.LineTypes.AntiAlias)
'        '    End If
'        'Next
'        'Dim w = CInt(surf.dst.Width / 2)
'        'ocvb.result1 = surf.dst(New cv.Rect(0, 0, w, surf.dst.Height))
'        'ocvb.result2 = surf.dst(New cv.Rect(w, 0, w, surf.dst.Height))
'    End Sub
'    Public Sub Dispose() Implements IDisposable.Dispose
'        surf.Dispose()
'    End Sub
'End Class