Imports cv = OpenCvSharp

Public Class Distance_Basics
    Inherits VB_Class
    Dim foreground As kMeans_Depth_FG_BG
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        radio.Setup(ocvb, callerName,3)
        radio.check(0).Text = "C"
        radio.check(1).Text = "L1"
        radio.check(2).Text = "L2"
        radio.check(2).Checked = True

        sliders.setupTrackBar1(ocvb, callerName, "kernel size", 1, 5, 3)

        foreground = New kMeans_Depth_FG_BG(ocvb, callerName)
        ocvb.desc = "Distance algorithm basics."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        foreground.Run(ocvb)
        Dim fg = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)

        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.bgr2gray)
        Dim DistanceType = cv.DistanceTypes.L2
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                DistanceType = Choose(i + 1, cv.DistanceTypes.C, cv.DistanceTypes.L1, cv.DistanceTypes.L2)
                Exit For
            End If
        Next

        cv.Cv2.BitwiseAnd(gray, fg, gray)
        Dim kernelSize = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize += 1
        If kernelSize = 1 Then kernelSize = 0 ' this is precise distance (there is no distance of 1)

        Dim dist = gray.DistanceTransform(DistanceType, kernelSize)
        Dim dist32f = dist.Normalize(0, 255, cv.NormTypes.MinMax)
        dist32f.ConvertTo(gray, cv.MatType.CV_8UC1)
        ocvb.result2 = gray.CvtColor(cv.ColorConversionCodes.gray2bgr)
    End Sub
    Public Sub MyDispose()
                foreground.Dispose()
        radio.Dispose()
    End Sub
End Class
