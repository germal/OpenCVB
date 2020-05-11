Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes

' https://docs.opencv.org/3.4.1/d2/dc1/camshiftdemo_8cpp-example.html
' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class CamShift_Basics
    Inherits ocvbClass
    Public plotHist As Plot_Histogram
    Public trackBox As New cv.RotatedRect
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        plotHist = New Plot_Histogram(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "CamShift vMin", 0, 255, 32)
        sliders.setupTrackBar2(ocvb, caller, "CamShift vMax", 0, 255, 255)
        sliders.setupTrackBar3(ocvb, caller, "CamShift Smin", 0, 255, 60)
        sliders.setupTrackBar4(ocvb, caller, "CamShift Histogram bins", 16, 255, 32)

        ocvb.label1 = "Draw anywhere to create histogram and start camshift"
        ocvb.label2 = "Histogram of targeted region (hue only)"
        ocvb.desc = "CamShift Demo - draw on the images to define the object to track."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static roi As New cv.Rect
        Static vMinLast As Int32
        Static vMaxLast As Int32
        Static sBinsLast As cv.Scalar
        Static roi_hist As New cv.Mat
        Dim mask As New cv.Mat
        ocvb.color.CopyTo(dst1)
        Dim hsv = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim hue = hsv.EmptyClone()
        Dim bins = sliders.TrackBar4.Value
        Dim hsize() As Int32 = {bins, bins, bins}
        Dim ranges() = {New cv.Rangef(0, 180)}
        Dim min = Math.Min(sliders.TrackBar1.Value, sliders.TrackBar2.Value)
        Dim max = Math.Max(sliders.TrackBar1.Value, sliders.TrackBar2.Value)
        Dim sbins = New cv.Scalar(0, sliders.TrackBar3.Value, min)

        cv.Cv2.MixChannels({hsv}, {hue}, {0, 0})
        mask = hsv.InRange(sbins, New cv.Scalar(180, 255, max))

        If ocvb.drawRect.Width > 0 And ocvb.drawRect.Height > 0 Then
            vMinLast = min
            vMaxLast = max
            sBinsLast = sbins
            If ocvb.drawRect.X + ocvb.drawRect.Width > ocvb.color.Width Then ocvb.drawRect.Width = ocvb.color.Width - ocvb.drawRect.X - 1
            If ocvb.drawRect.Y + ocvb.drawRect.Height > ocvb.color.Height Then ocvb.drawRect.Height = ocvb.color.Height - ocvb.drawRect.Y - 1
            cv.Cv2.CalcHist(New cv.Mat() {hue(ocvb.drawRect)}, {0, 0}, mask(ocvb.drawRect), roi_hist, 1, hsize, ranges)
            roi_hist = roi_hist.Normalize(0, 255, cv.NormTypes.MinMax)
            roi = ocvb.drawRect
            ocvb.drawRectClear = True
        End If
        If roi_hist.Rows <> 0 Then
            Dim backproj As New cv.Mat
            cv.Cv2.CalcBackProject({hue}, {0, 0}, roi_hist, backproj, ranges)
            cv.Cv2.BitwiseAnd(backproj, mask, backproj)
            trackBox = cv.Cv2.CamShift(backproj, roi, cv.TermCriteria.Both(10, 1))
            Show_HSV_Hist(ocvb.result2, roi_hist)
            If ocvb.result2.Channels = 1 Then ocvb.result2 = ocvb.color.EmptyClone()
            ocvb.result2 = ocvb.result2.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        End If
        dst1.SetTo(0)
        ocvb.color.CopyTo(dst1, mask)
        If trackBox.Size.Width > 0 Then dst1.Ellipse(trackBox, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
    End Sub
End Class




' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class CamShift_Foreground
    Inherits ocvbClass
    Dim camshift As CamShift_Basics
    Dim blob As Depth_Foreground
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        camshift = New CamShift_Basics(ocvb, caller)
        blob = New Depth_Foreground(ocvb, caller)
        ocvb.label1 = "Automatically finding the head - top of nearest object"
        ocvb.desc = "Use depth to find the head and start the camshift demo. "
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim restartRequested As Boolean
        Static depthMin As Int32
        Static depthMax As Int32
        If blob.trim.sliders.TrackBar1.Value <> depthMin Then
            depthMin = blob.trim.sliders.TrackBar1.Value
            restartRequested = True
        End If
        If blob.trim.sliders.TrackBar2.Value <> depthMax Then
            depthMax = blob.trim.sliders.TrackBar2.Value
            restartRequested = True
        End If
        If restartRequested Then blob.Run(ocvb)
        camshift.Run(ocvb)
        ocvb.label2 = "Mask of objects closer than " + Format(depthMax / 1000, "#0.0") + " meters"
    End Sub
End Class






' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class Camshift_Object
    Inherits ocvbClass
    Dim blob As Blob_DepthClusters
    Dim camshift As CamShift_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        blob = New Blob_DepthClusters(ocvb, caller)

        camshift = New CamShift_Basics(ocvb, caller)

        ocvb.desc = "Use the blob depth cluster as input to initialize a camshift algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        blob.Run(ocvb)

        Dim largestMask = blob.flood.fBasics.maskSizes.ElementAt(0).Value
        If camshift.trackBox.Size.Width = 0 Then ocvb.drawRect = blob.flood.fBasics.maskRects(largestMask)
        camshift.Run(ocvb)
    End Sub
End Class




' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class Camshift_TopObjects
    Inherits ocvbClass
    Dim blob As Blob_DepthClusters
    Dim cams(3) As CamShift_Basics
    Dim mats As Mat_4to1
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        mats = New Mat_4to1(ocvb, caller)

        blob = New Blob_DepthClusters(ocvb, caller)
        sliders.setupTrackBar1(ocvb, caller, "How often should camshift be reinitialized", 1, 500, 100)
        For i = 0 To cams.Length - 1
            cams(i) = New CamShift_Basics(ocvb, caller)
        Next
        ocvb.desc = "Track"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        blob.Run(ocvb)

        Dim updateFrequency = sliders.TrackBar1.Value
        Dim trackBoxes As New List(Of cv.RotatedRect)
        For i = 0 To cams.Length - 1
            If blob.flood.fBasics.maskSizes.Count > i Then
                Dim camIndex = blob.flood.fBasics.maskSizes.ElementAt(i).Value
                If ocvb.frameCount Mod updateFrequency = 0 Or cams(i).trackBox.Size.Width = 0 Then
                    ocvb.drawRect = blob.flood.fBasics.maskRects(camIndex)
                End If

                cams(i).Run(ocvb)
                mats.mat(i) = ocvb.result2.Clone()
                trackBoxes.Add(cams(i).trackBox)
            End If
        Next
        For i = 0 To trackBoxes.Count - 1
            dst1.Ellipse(trackBoxes(i), cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        Next
        mats.Run(ocvb)
    End Sub
End Class
