﻿Imports cv = OpenCvSharp
Public Class Sharpen_UnsharpMask
    Inherits VB_Class
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "sigma", 1, 2000, 100)
        sliders.setupTrackBar2(ocvb, "threshold", 0, 255, 5)
        sliders.setupTrackBar3(ocvb, "Shift Amount", 0, 5000, 1000)
                ocvb.desc = "Sharpen an image"
        ocvb.label2 = "Unsharp mask (difference from Blur)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim blurred As New cv.Mat
        Dim sigma As Double = sliders.TrackBar1.Value / 100
        Dim threshold As Double = sliders.TrackBar2.Value
        Dim amount As Double = sliders.TrackBar3.Value / 1000
        cv.Cv2.GaussianBlur(ocvb.color, ocvb.result2, New cv.Size(), sigma, sigma)

        Dim diff As New cv.Mat
        cv.Cv2.Absdiff(ocvb.color, ocvb.result2, diff)
        diff = diff.Threshold(threshold, 255, cv.ThresholdTypes.Binary)
        ocvb.result1 = ocvb.color * (1 + amount) + diff * (-amount)
        diff.CopyTo(ocvb.result2)
    End Sub
    Public Sub VBdispose()
            End Sub
End Class



' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class Sharpen_DetailEnhance
    Inherits VB_Class
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "DetailEnhance Sigma_s", 0, 200, 60)
        sliders.setupTrackBar2(ocvb, "DetailEnhance Sigma_r", 1, 100, 7)
                ocvb.desc = "Enhance detail on an image - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim sigma_s = sliders.TrackBar1.Value
        Dim sigma_r = sliders.TrackBar2.Value / sliders.TrackBar2.Maximum
        cv.Cv2.DetailEnhance(ocvb.color, ocvb.result1, sigma_s, sigma_r)
    End Sub
    Public Sub VBdispose()
            End Sub
End Class




' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class Sharpen_Stylize
    Inherits VB_Class
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Stylize Sigma_s", 0, 200, 60)
        sliders.setupTrackBar2(ocvb, "Stylize Sigma_r", 1, 100, 7)
                ocvb.desc = "Stylize an image - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim sigma_s = sliders.TrackBar1.Value
        Dim sigma_r = sliders.TrackBar2.Value / sliders.TrackBar2.Maximum
        cv.Cv2.DetailEnhance(ocvb.color, ocvb.result1, sigma_s, sigma_r)
    End Sub
    Public Sub VBdispose()
            End Sub
End Class
