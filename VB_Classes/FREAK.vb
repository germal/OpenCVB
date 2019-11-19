Imports cv = OpenCvSharp
Imports OpenCvSharp.XFeatures2D
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Public Class FREAK_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Find keypoints using ORB and FREAK algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim orb = cv.ORB.Create(1000)
        Dim keypoints = orb.Detect(gray)
        Dim freak = cv.XFeatures2D.FREAK.Create()
        Dim fDesc = New cv.Mat
        freak.Compute(gray, keypoints, fDesc)

        If keypoints.Count = 0 Then Exit Sub

        ocvb.result1 = ocvb.color.Clone()
        For Each kpt In keypoints
            Dim r = kpt.Size / 2
            ocvb.result1.Circle(kpt.Pt, r, cv.Scalar.Green)
            ocvb.result1.Line(New cv.Point(kpt.Pt.X + r, kpt.Pt.Y + r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y - r), cv.Scalar.Green)
            ocvb.result1.Line(New cv.Point(kpt.Pt.X + r, kpt.Pt.Y - r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y + r), cv.Scalar.Green)
        Next
        ocvb.label1 = CStr(keypoints.Count) + " key points were identified"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class