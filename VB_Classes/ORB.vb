Imports cv = OpenCvSharp
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Public Class ORB_Basics
    Inherits ocvbClass
    Public keypoints() As cv.KeyPoint
    Public gray As New cv.Mat
    Dim orb As cv.ORB
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "ORB - desired point count", 10, 2000, 700)

        ocvb.desc = "Find keypoints using ORB"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        orb = cv.ORB.Create(sliders.TrackBar1.Value)
        keypoints = orb.Detect(gray)
        If standalone Then
            dst1 = ocvb.color.Clone()

            For Each kpt In keypoints
                dst1.Circle(kpt.Pt, 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
            Next
            label1 = CStr(keypoints.Count) + " key points were identified"
        End If
    End Sub
End Class
