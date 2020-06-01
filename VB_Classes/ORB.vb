Imports cv = OpenCvSharp
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Public Class ORB_Basics
    Inherits ocvbClass
    Public keypoints() As cv.KeyPoint
    Dim orb As cv.ORB
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, "ORB - desired point count", 10, 2000, 100)

        ocvb.desc = "Find keypoints using ORB"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        orb = cv.ORB.Create(sliders.TrackBar1.Value)
        keypoints = orb.Detect(src)
        If standalone Then
            dst1 = src.Clone()
            For Each kpt In keypoints
                dst1.Circle(kpt.Pt, 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
            Next
            label1 = CStr(keypoints.Count) + " key points were identified"
        End If
    End Sub
End Class
