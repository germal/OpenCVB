
Imports cv = OpenCvSharp
Public Class FishEye_Basics : Implements IDisposable
    Public externalUse As Boolean
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use OpenCV's FishEye API to undistort a fisheye lens input"
        ocvb.label1 = "Left View"
        ocvb.label2 = "Right View"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex = T265Camera Then
            Dim leftviewMap1 As New cv.Mat
            Dim leftviewMap2 As New cv.Mat
            Dim rightViewMap1 As New cv.Mat
            Dim rightViewMap2 As New cv.Mat
            Static kMatLeft As cv.Mat, dMatLeft As cv.Mat, rMatLeft As cv.Mat, pMatLeft As cv.Mat
            Dim maxDisp = 0
            Dim stero_height_px = 300
            getMats(ocvb, kMatLeft, dMatLeft, rMatLeft, pMatLeft, maxDisp, stero_height_px)

            cv.Cv2.FishEye.InitUndistortRectifyMap(kMatLeft, dMatLeft, rMatLeft, pMatLeft, ocvb.leftView.Size(),
                                                   cv.MatType.CV_32FC1, leftviewMap1, leftviewMap2)
            'cv.Cv2.FishEye.InitUndistortRectifyMap(kMatRight, dMatRight, rMatRight, pMatRight, ocvb.leftView.Size(),
            '                                       cv.MatType.CV_32FC1, rightViewMap1, rightViewMap2)
            leftView = ocvb.leftView.Remap(leftviewMap1, leftviewMap2, cv.InterpolationFlags.Linear).Resize(ocvb.color.Size())
            'rightView = ocvb.rightView.Remap(rightViewMap1, rightViewMap2, cv.InterpolationFlags.Linear).Resize(ocvb.color.Size())
        Else
            ocvb.label1 = "Left View (no fisheye lens present)"
            ocvb.label2 = "Right View (no fisheye lens present)"
            leftView = ocvb.leftView
            rightView = ocvb.rightView
        End If
        If externalUse = False Then
            ocvb.result1 = leftView
            'ocvb.result2 = rightView
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class