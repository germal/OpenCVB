Imports cv = OpenCvSharp

Public Class Distance_Basics : Implements IDisposable
    Dim foreground As kMeans_Depth_FG_BG
    Dim radio As New OptionsRadioButtons
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        radio.Setup(ocvb, 3)
        radio.check(0).Text = "C"
        radio.check(1).Text = "L1"
        radio.check(2).Text = "L2"
        radio.check(2).Checked = True
        If ocvb.parms.ShowOptions Then radio.show()

        sliders.setupTrackBar1(ocvb, "kernel size", 1, 5, 3)
        If ocvb.parms.ShowOptions Then sliders.show()

        foreground = New kMeans_Depth_FG_BG(ocvb)
        ocvb.desc = "Distance algorithm basics."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        foreground.Run(ocvb)
        Dim fg = ocvb.result1.CvtColor(cv.ColorConversionCodes.bgr2gray)
        fg = fg.Threshold(1, 255, cv.ThresholdTypes.Binary)

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
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        foreground.Dispose()
        radio.Dispose()
    End Sub
End Class
