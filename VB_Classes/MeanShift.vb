Imports cv = OpenCvSharp
' http://answers.opencv.org/question/175486/meanshift-sample-code-in-c/
Public Class MeanShift_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "Draw anywhere to start mean shift tracking."
        ocvb.desc = "Demonstrate the use of mean shift algorithm.  Draw on the images to define an object to track"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim hsv = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim ch() As Int32 = {0, 1, 2}
        Dim hsize() As Int32 = {16, 16, 16}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 180)}
        Static roi_hist As New cv.Mat
        Static saveDrawRect As New cv.Rect
        If ocvb.drawRect.Width > 0 And ocvb.drawRect.Height > 0 Then
            saveDrawRect = ocvb.drawRect
            Dim maskROI = hsv(ocvb.drawRect).InRange(New cv.Scalar(0, 60, 32), New cv.Scalar(180, 255, 255))
            cv.Cv2.CalcHist(New cv.Mat() {hsv(ocvb.drawRect)}, ch, maskROI, roi_hist, 1, hsize, ranges)
            roi_hist = roi_hist.Normalize(0, 255, cv.NormTypes.MinMax)
            ocvb.drawRectClear = True
        End If
        If roi_hist.Rows <> 0 Then
            Dim backProj As New cv.Mat
            cv.Cv2.CalcBackProject(New cv.Mat() {hsv}, ch, roi_hist, backProj, ranges)
            cv.Cv2.MeanShift(backProj, saveDrawRect, cv.TermCriteria.Both(10, 1))
            ocvb.result1 = backProj.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            ocvb.result1.Rectangle(saveDrawRect, cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
            Show_HSV_Hist(ocvb.result2, roi_hist)
            ocvb.result2 = ocvb.result2.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        Else
            ocvb.result1 = ocvb.color
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



'http://study.marearts.com/2014/12/opencv-meanshiftfiltering-example.html
Public Class MeanShift_PyrFilter : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "MeanShift Spatial Radius", 1, 100, 10)
        sliders.setupTrackBar2(ocvb, "MeanShift color Radius", 1, 100, 15)
        sliders.setupTrackBar3(ocvb, "MeanShift Max Pyramid level", 1, 8, 3)
        sliders.Show()
        ocvb.desc = "Use PyrMeanShiftFiltering to segment an image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim spatialRadius = sliders.TrackBar1.Value
        Dim colorRadius = sliders.TrackBar2.Value
        Dim maxPyrLevel = sliders.TrackBar3.Value
        cv.Cv2.PyrMeanShiftFiltering(ocvb.color, ocvb.result1, spatialRadius, colorRadius, maxPyrLevel)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class
