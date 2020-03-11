﻿Imports cv = OpenCvSharp
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Public Class ORB_Basics : Implements IDisposable
    Public sliders As New OptionsSliders
    Public keypoints() As cv.KeyPoint
    Public gray As New cv.Mat
    Public externalUse As Boolean
    Dim orb As cv.ORB
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "ORB - desired point count", 10, 2000, 700)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Find keypoints using ORB"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        orb = cv.ORB.Create(sliders.TrackBar1.Value)
        keypoints = orb.Detect(gray)
        If externalUse = False Then
            ocvb.result1 = ocvb.color.Clone()

            For Each kpt In keypoints
                ocvb.result1.Circle(kpt.Pt, 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
            Next
            ocvb.label1 = CStr(keypoints.Count) + " key points were identified"
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        orb.Dispose()
        sliders.Dispose()
    End Sub
End Class