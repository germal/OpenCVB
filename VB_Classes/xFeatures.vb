Imports cv = OpenCvSharp
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/StarDetectorSample.vb
Public Class XFeatures2D_StarDetector
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        ocvb.desc = "Basics of the StarDetector - a 2D feature detector.  FAILS IN COMPUTE.  Uncomment to investigate further."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.bgr2gray)
        Dim detector = OpenCvSharp.XFeatures2D.StarDetector.Create()
        Dim keypoints() = detector.Detect(gray)

        If keypoints IsNot Nothing Then
            ocvb.result1 = ocvb.color.Clone()
            For Each kpt As cv.KeyPoint In keypoints
                Dim r As Single = kpt.Size / 2
                cv.Cv2.Circle(ocvb.result1, kpt.Pt, CInt(Math.Truncate(r)), New cv.Scalar(0, 255, 0), 1, cv.LineTypes.Link8, 0)
                cv.Cv2.Line(ocvb.result1, New cv.Point(kpt.Pt.X + r, kpt.Pt.Y + r), New cv.Point(kpt.Pt.X - r, kpt.Pt.Y - r), New cv.Scalar(0, 255, 0), 1, cv.LineTypes.Link8, 0)
                cv.Cv2.Line(ocvb.result1, New cv.Point(kpt.Pt.X - r, kpt.Pt.Y + r), New cv.Point(kpt.Pt.X + r, kpt.Pt.Y - r), New cv.Scalar(0, 255, 0), 1, cv.LineTypes.Link8, 0)
            Next kpt
        End If
    End Sub
End Class
