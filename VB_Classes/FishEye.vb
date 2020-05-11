
Imports cv = OpenCvSharp
Public Class FishEye_Rectified
    Inherits ocvbClass
        Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Use OpenCV's FishEye API to undistort a fisheye lens input"
        ocvb.label1 = "Left View"
        ocvb.label2 = "Right View"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static kMatLeft As cv.Mat, dMatLeft As cv.Mat, rMatLeft As cv.Mat, pMatLeft As cv.Mat
        Static kMatRight As cv.Mat, dMatRight As cv.Mat, rMatRight As cv.Mat, pMatRight As cv.Mat
        If ocvb.frameCount = 0 Then
            Dim maxDisp = 0
            Dim stero_height_px = 300
            undistortSetup(ocvb, kMatLeft, dMatLeft, rMatLeft, pMatLeft, maxDisp, stero_height_px, ocvb.parms.intrinsicsLeft)
            undistortSetup(ocvb, kMatRight, dMatRight, rMatRight, pMatRight, maxDisp, stero_height_px, ocvb.parms.intrinsicsRight)
        End If
        If ocvb.parms.cameraIndex = T265Camera Then
            Dim leftviewMap1 As New cv.Mat
            Dim leftviewMap2 As New cv.Mat
            Dim rightViewMap1 As New cv.Mat
            Dim rightViewMap2 As New cv.Mat

            cv.Cv2.FishEye.InitUndistortRectifyMap(kMatLeft, dMatLeft, rMatLeft, pMatLeft, ocvb.leftView.Size(),
                                                   cv.MatType.CV_32FC1, leftviewMap1, leftviewMap2)
            cv.Cv2.FishEye.InitUndistortRectifyMap(kMatRight, dMatRight, rMatRight, pMatRight, ocvb.leftView.Size(),
                                                   cv.MatType.CV_32FC1, rightViewMap1, rightViewMap2)
            leftView = ocvb.leftView.Remap(leftviewMap1, leftviewMap2, cv.InterpolationFlags.Linear).Resize(ocvb.color.Size())
            rightView = ocvb.rightView.Remap(rightViewMap1, rightViewMap2, cv.InterpolationFlags.Linear).Resize(ocvb.color.Size())
        Else
            ocvb.label1 = "Left View (no fisheye lens present)"
            ocvb.label2 = "Right View (no fisheye lens present)"
            leftView = ocvb.leftView
            rightView = ocvb.rightView
        End If
        if standalone Then
            dst = leftView
            ocvb.result2 = rightView
        End If
    End Sub
End Class





Public Class FishEye_Raw
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Display the Raw FishEye images for the T265 (only)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex <> T265Camera Then
            ocvb.putText(New ActiveClass.TrueType("Only the T265 camera is has FishEye images at this point.", 10, 100, RESULT1))
            Exit Sub
        End If
        ocvb.label1 = "Left Fisheye Image"
        ocvb.label2 = "Right Fisheye Image"
        dst = ocvb.leftView
        ocvb.result2 = ocvb.rightView
    End Sub
End Class
