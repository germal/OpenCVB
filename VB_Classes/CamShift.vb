Imports cv = OpenCvSharp
' https://docs.opencv.org/3.4.1/d2/dc1/camshiftdemo_8cpp-example.html
Public Class CamShift_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "CamShift Red Min", 0, 255, 255)
        sliders.setupTrackBar2(ocvb, "CamShift Red Max", 0, 255, 10)
        sliders.setupTrackBar3(ocvb, "CamShift Smin (green min)", 0, 255, 30)
        sliders.setupTrackBar4(ocvb, "CamShift Histogram bins", 16, 255, 32)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.label1 = "Draw on any area with hue"
        ocvb.desc = "CamShift Demo - draw on the images to define the object to track."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static roi As New cv.Rect
        Static minRed As Int32
        Static maxRed As Int32
        Static minGreen As Int32
        Static roi_hist As New cv.Mat
        ocvb.color.CopyTo(ocvb.result1)
        Dim hsv = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim ch() As Int32 = {0, 1, 2}
        Dim bins = sliders.TrackBar4.Value
        Dim hsize() As Int32 = {bins, bins, bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 180)}
        Dim min = Math.Min(sliders.TrackBar1.Value, sliders.TrackBar2.Value)
        Dim max = Math.Max(sliders.TrackBar1.Value, sliders.TrackBar2.Value)
        Dim sbins = New cv.Scalar(0, sliders.TrackBar3.Value, min)
        If roi <> New cv.Rect(0, 0, 0, 0) Or roi <> ocvb.drawRect Then
            If roi <> ocvb.drawRect Or min <> minRed Or max <> maxRed Or sbins <> minGreen Then
                minRed = min
                maxRed = max
                minGreen = sbins
                roi = ocvb.drawRect
                If roi.X + roi.Width > hsv.Width Then roi.Width = hsv.Width - roi.X
                If roi.Y + roi.Height > hsv.Height Then roi.Height = hsv.Height - roi.Y
                Dim maskROI = hsv(roi).InRange(sbins, New cv.Scalar(180, 255, max))
                cv.Cv2.CalcHist(New cv.Mat() {hsv(roi)}, ch, maskROI, roi_hist, 1, hsize, ranges)
                roi_hist = roi_hist.Normalize(0, 255, cv.NormTypes.MinMax)
                'histogram2DPlot(roi_hist, ocvb.result2, bins, sbins)
            End If
        End If
        If roi_hist.Rows <> 0 Then
            cv.Cv2.CalcBackProject(New cv.Mat() {hsv}, ch, roi_hist, ocvb.result1, ranges)
            Dim trackBox = cv.Cv2.CamShift(ocvb.result1, roi, cv.TermCriteria.Both(10, 1))
            roi = trackBox.BoundingRect()
            If roi.X < 0 Then roi.X = 0
            If roi.Y < 0 Then roi.Y = 0
            If roi.X + roi.Width > ocvb.color.Width Then roi.Width = ocvb.color.Width - roi.X
            If roi.Y + roi.Height > ocvb.color.Height Then roi.Height = ocvb.color.Height - roi.Y
            ' if we don't grow too big, then use the new roi.
            If roi.Width < 300 And roi.Height < 300 Then
                ocvb.drawRect = roi
            Else
                ocvb.drawRect.X = roi.X
                ocvb.drawRect.Y = roi.Y
            End If
            ocvb.result1.Ellipse(trackBox, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class CamShift_Depth : Implements IDisposable
    Dim cam As CamShift_Basics
    Dim blob As Depth_FindLargestBlob
    Public Sub New(ocvb As AlgorithmData)
        cam = New CamShift_Basics(ocvb)
        blob = New Depth_FindLargestBlob(ocvb)
        ocvb.desc = "CamShift Demo - use depth to find the head and start the camshift demo."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim restartRequested As Boolean
        Dim depthMin As Int32
        Dim depthMax As Int32
        If blob.trim.sliders.TrackBar1.Value <> depthMin Then restartRequested = True
        If blob.trim.sliders.TrackBar2.Value <> depthMax Then restartRequested = True
        If ocvb.drawRect.Width = 0 Or restartRequested Then blob.Run(ocvb)
        cam.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        cam.Dispose()
        blob.Dispose()
    End Sub
End Class
