Imports cv = OpenCvSharp
' http://answers.opencv.org/question/175486/meanshift-sample-code-in-c/
Public Class MeanShift_Basics
    Inherits ocvbClass
    Public rectangleEdgeWidth As Int32 = 2
    Public trackbox As New cv.Rect
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.label1 = "Draw anywhere to start mean shift tracking."
        ocvb.desc = "Demonstrate the use of mean shift algorithm.  Draw on the images to define an object to track"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim hsv = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim ch() As Int32 = {0, 1, 2}
        Dim hsize() As Int32 = {16, 16, 16}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 180)}
        Static roi_hist As New cv.Mat
        If ocvb.drawRect.Width > 0 And ocvb.drawRect.Height > 0 Then
            trackbox = ocvb.drawRect
            Dim maskROI = hsv(ocvb.drawRect).InRange(New cv.Scalar(0, 60, 32), New cv.Scalar(180, 255, 255))
            cv.Cv2.CalcHist(New cv.Mat() {hsv(ocvb.drawRect)}, ch, maskROI, roi_hist, 1, hsize, ranges)
            roi_hist = roi_hist.Normalize(0, 255, cv.NormTypes.MinMax)
            ocvb.drawRectClear = True
        End If
        If roi_hist.Rows <> 0 Then
            Dim backProj As New cv.Mat
            cv.Cv2.CalcBackProject(New cv.Mat() {hsv}, ch, roi_hist, backProj, ranges)
            cv.Cv2.MeanShift(backProj, trackbox, cv.TermCriteria.Both(10, 1))
            ocvb.result1 = backProj.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            ocvb.result1.Rectangle(trackbox, cv.Scalar.Red, rectangleEdgeWidth, cv.LineTypes.AntiAlias)
            Show_HSV_Hist(ocvb.result2, roi_hist)
            ocvb.result2 = ocvb.result2.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        Else
            ocvb.result1 = ocvb.color
        End If
    End Sub
End Class




Public Class MeanShift_Depth
    Inherits ocvbClass
    Dim ms As MeanShift_Basics
    Dim blob As Depth_Foreground
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ms = New MeanShift_Basics(ocvb, caller)
        blob = New Depth_Foreground(ocvb, caller)
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
End Class



'http://study.marearts.com/2014/12/opencv-meanshiftfiltering-example.html
Public Class MeanShift_PyrFilter
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "MeanShift Spatial Radius", 1, 100, 10)
        sliders.setupTrackBar2(ocvb, caller, "MeanShift color Radius", 1, 100, 15)
        sliders.setupTrackBar3(ocvb, caller, "MeanShift Max Pyramid level", 1, 8, 3)
        ocvb.desc = "Use PyrMeanShiftFiltering to segment an image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim spatialRadius = sliders.TrackBar1.Value
        Dim colorRadius = sliders.TrackBar2.Value
        Dim maxPyrLevel = sliders.TrackBar3.Value
        cv.Cv2.PyrMeanShiftFiltering(ocvb.color, ocvb.result1, spatialRadius, colorRadius, maxPyrLevel)
    End Sub
End Class





' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class Meanshift_TopObjects
    Inherits ocvbClass
    Dim blob As Blob_DepthClusters
    Dim cams(3) As MeanShift_Basics
    Dim mats1 As Mat_4to1
    Dim mats2 As Mat_4to1
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        mats1 = New Mat_4to1(ocvb, caller)

        mats2 = New Mat_4to1(ocvb, caller)

        blob = New Blob_DepthClusters(ocvb, caller)
        sliders.setupTrackBar1(ocvb, caller, "How often should camshift be reinitialized", 1, 500, 100)
        For i = 0 To cams.Length - 1
            cams(i) = New MeanShift_Basics(ocvb, caller)
            cams(i).rectangleEdgeWidth = 8
        Next
        ocvb.desc = "Track"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        blob.Run(ocvb)

        Dim updateFrequency = sliders.TrackBar1.Value
        Dim trackBoxes As New List(Of cv.Rect)
        For i = 0 To cams.Length - 1
            If blob.flood.fBasics.maskSizes.Count > i Then
                Dim camIndex = blob.flood.fBasics.maskSizes.ElementAt(i).Value
                If ocvb.frameCount Mod updateFrequency = 0 Or cams(i).trackbox.Size.Width = 0 Or ocvb.frameCount < 3 Then
                    ocvb.drawRect = blob.flood.fBasics.maskRects(camIndex)
                End If

                cams(i).Run(ocvb)
                mats1.mat(i) = ocvb.result1.Clone()
                mats2.mat(i) = ocvb.result2.Clone()
                trackBoxes.Add(cams(i).trackbox)
            End If
        Next
        mats1.Run(ocvb)
        ocvb.result1 = ocvb.result2.Clone()
        mats2.Run(ocvb)
    End Sub
End Class

