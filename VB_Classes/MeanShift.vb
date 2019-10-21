Imports cv = OpenCvSharp
' http://answers.opencv.org/question/175486/meanshift-sample-code-in-c/
Public Class MeanShift_Basics : Implements IDisposable
    Dim roi As New cv.Rect
    Dim roi_hist As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Demonstrate the use of mean shift algorithm.  Draw on the images to define an object to track"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.drawRect.Width = 0 And ocvb.drawRect.Height = 0 Then Exit Sub
        ocvb.color.CopyTo(ocvb.result1)
        Dim hsv = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim ch() As Int32 = {0, 1, 2}
        Dim hsize() As Int32 = {16, 16, 16}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 180)}
        If roi <> ocvb.drawRect Then
            roi = ocvb.drawRect ' save for later comparison
            Dim maskROI = hsv(roi).InRange(New cv.Scalar(0, 60, 32), New cv.Scalar(180, 255, 255))
            cv.Cv2.CalcHist(New cv.Mat() {hsv(ocvb.drawRect)}, ch, maskROI, roi_hist, 1, hsize, ranges)
            roi_hist = roi_hist.Normalize(0, 255, cv.NormTypes.MinMax)
        End If
        If roi_hist.Rows <> 0 Then
            cv.Cv2.CalcBackProject(New cv.Mat() {hsv}, ch, roi_hist, ocvb.result1, ranges)
            cv.Cv2.MeanShift(ocvb.result1, ocvb.drawRect, cv.TermCriteria.Both(10, 1))
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class MeanShift_Depth : Implements IDisposable
    Dim ms As MeanShift_Basics
    Dim blob As Depth_FindLargestBlob
    Public Sub New(ocvb As AlgorithmData)
        ms = New MeanShift_Basics(ocvb)
        blob = New Depth_FindLargestBlob(ocvb)
        ocvb.desc = "Use depth to start mean shift algorithm."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim restartRequested As Boolean
        Dim depthMin As Int32
        Dim depthMax As Int32
        If blob.trim.sliders.TrackBar1.Value <> depthMin Then restartRequested = True
        If blob.trim.sliders.TrackBar2.Value <> depthMax Then restartRequested = True
        If ocvb.drawRect = New cv.Rect(0, 0, 0, 0) Or restartRequested Then blob.Run(ocvb)
        ms.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        ms.Dispose()
        blob.Dispose()
    End Sub
End Class
