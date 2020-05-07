Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports CS_Classes

' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_Basics_CS
    Inherits ocvbClass
            Public CS_SurfBasics As New CS_SurfBasics
    Dim fisheye As FishEye_Rectified
    Public srcLeft As New cv.Mat
    Public srcRight As New cv.Mat
    Public dst As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        fisheye = New FishEye_Rectified(ocvb, caller)
        fisheye.externalUse = True

        radio.Setup(ocvb, caller,2)
        radio.check(0).Text = "Use BF Matcher"
        radio.check(1).Text = "Use Flann Matcher"
        radio.check(0).Checked = True

        sliders.setupTrackBar1(ocvb, caller, "Hessian threshold", 1, 5000, 2000)

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
        CS_SurfBasics.Run(srcLeft, srcRight, dst, sliders.TrackBar1.Value, radio.check(0).Checked)

        'If dst.Width <> ocvb.color.Width * 2 Then dst = dst.Resize(New cv.Size(ocvb.color.Width * 2, srcLeft.Height))
        If externalUse = False Then
            dst(New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height)).CopyTo(ocvb.result1)
            dst(New cv.Rect(ocvb.color.Width, 0, ocvb.color.Width, ocvb.color.Height)).CopyTo(ocvb.result2)
            ocvb.label1 = If(radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
            If CS_SurfBasics.keypoints1 IsNot Nothing Then ocvb.label1 += " " + CStr(CS_SurfBasics.keypoints1.Count)
        End If
    End Sub
    Public Sub MyDispose()
                        fisheye.Dispose()
    End Sub
End Class






' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_Basics
    Inherits ocvbClass
    Dim surf As Surf_Basics_CS
    Dim fisheye As FishEye_Rectified
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        fisheye = New FishEye_Rectified(ocvb, caller)
        fisheye.externalUse = True

        surf = New Surf_Basics_CS(ocvb, caller)
        surf.externalUse = True

        ocvb.desc = "Use left and right views to match points in horizontal slices."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex = T265Camera Then fisheye.Run(ocvb)

        If ocvb.parms.cameraIndex = T265Camera Then
            surf.srcLeft = fisheye.leftView
            surf.srcRight = fisheye.rightView
        Else
            surf.srcLeft = ocvb.leftView
            surf.srcRight = ocvb.rightView
        End If
        surf.Run(ocvb)
        surf.dst(New cv.Rect(0, 0, surf.srcLeft.Width, surf.srcLeft.Height)).CopyTo(ocvb.result1)
        surf.dst(New cv.Rect(surf.srcLeft.Width, 0, surf.srcLeft.Width, surf.srcLeft.Height)).CopyTo(ocvb.result2)
    End Sub
    Public Sub MyDispose()
        surf.Dispose()
        fisheye.Dispose()
    End Sub
End Class





' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_DrawMatchManual_CS
    Inherits ocvbClass
        Dim surf As Surf_Basics_CS
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        surf = New Surf_Basics_CS(ocvb, caller)
        surf.CS_SurfBasics.drawPoints = False

        sliders.setupTrackBar1(ocvb, caller, "Surf Vertical Range to Search", 0, 50, 10)

        ocvb.desc = "Compare 2 images to get a homography but draw the points manually in horizontal slices."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        surf.Run(ocvb)
        ocvb.result1 = surf.srcLeft.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.result2 = surf.srcRight.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Dim keys1 = surf.CS_SurfBasics.keypoints1
        Dim keys2 = surf.CS_SurfBasics.keypoints2

        For i = 0 To keys1.Count - 1
            ocvb.result1.Circle(keys1(i).Pt, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Next

        Dim matchCount As Integer
        For i = 0 To keys1.Count - 1
            Dim pt = keys1(i).Pt
            For j = 0 To keys2.Count - 1
                If Math.Abs(keys2(j).Pt.X - pt.X) < sliders.TrackBar1.Value And Math.Abs(keys2(j).Pt.Y - pt.Y) < sliders.TrackBar1.Value Then
                    ocvb.result2.Circle(keys2(j).Pt, 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                    keys2(j).Pt.Y = -1 ' so we don't match it again.
                    matchCount += 1
                End If
            Next
        Next
        ' mark those that were not
        For i = 0 To keys2.Count - 1
            Dim pt = keys2(i).Pt
            If pt.Y <> -1 Then ocvb.result2.Circle(keys2(i).Pt, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Next
        ocvb.label2 = "Yellow matched left to right = " + CStr(matchCount) + ". Red is unmatched."
    End Sub
    Public Sub MyDispose()
        surf.Dispose()
    End Sub
End Class