Imports cv = OpenCvSharp
Public Class BRISK_Basics
    Inherits ocvbClass
    Public Brisk As cv.BRISK
    Public features As New List(Of cv.Point2f)
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "BRISK Radius Threshold", 1, 100, 50)
        ocvb.desc = "Detect features with BRISK"
        Brisk = cv.BRISK.Create()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim wt As New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC3, cv.Scalar.All(0))

        if standalone Then
            ocvb.color.CopyTo(src)
            ocvb.color.CopyTo(ocvb.result1)
        End If
        Dim keyPoints = Brisk.Detect(src)
        features.Clear()
        For Each pt In keyPoints
            Dim r = pt.Size
            If r > sliders.TrackBar1.Value Then
                features.Add(New cv.Point2f(pt.Pt.X, pt.Pt.Y))
                wt.Circle(pt.Pt, 2, cv.Scalar.Green, r / 2, cv.LineTypes.AntiAlias)
            End If
        Next
        if standalone Then
            cv.Cv2.AddWeighted(ocvb.color, 0.5, wt, 0.5, 0, ocvb.result1)
        End If
    End Sub
End Class

