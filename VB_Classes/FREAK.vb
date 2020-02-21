Imports cv = OpenCvSharp
Imports OpenCvSharp.XFeatures2D
'https://github.com/shimat/opencvsharp/wiki/ORB-and-FREAK
Public Class FREAK_Basics : Implements IDisposable
    Dim orb As ORB_Basics
    Public Sub New(ocvb As AlgorithmData)
        orb = New ORB_Basics(ocvb)
        orb.externalUse = True
        ocvb.desc = "Find keypoints using ORB and FREAK algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        orb.gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        orb.Run(ocvb)

        Dim freak = cv.XFeatures2D.FREAK.Create()
        Dim fDesc = New cv.Mat
        freak.Compute(ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY), orb.keypoints, fDesc)

        ocvb.result1 = ocvb.color.Clone()

        For Each kpt In orb.keypoints
            Dim r = kpt.Size / 2
            ocvb.result1.Circle(kpt.Pt, r, cv.Scalar.Green)
            ocvb.result1.Line(New cv.Point(kpt.Pt.X + r, kpt.Pt.Y + r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y - r), cv.Scalar.Green)
            ocvb.result1.Line(New cv.Point(kpt.Pt.X + r, kpt.Pt.Y - r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y + r), cv.Scalar.Green)
        Next
        ocvb.label1 = CStr(orb.keypoints.Count) + " key points were identified"
        ocvb.label2 = CStr(orb.keypoints.Count) + " FREAK Descriptors (resized to fit) Row = keypoint"
        ocvb.result2 = fDesc.Resize(ocvb.result2.Size())
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        orb.Dispose()
    End Sub
End Class