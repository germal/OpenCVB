Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes

' https://docs.opencv.org/3.4.1/d2/dc1/camshiftdemo_8cpp-example.html
' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class CamShift_Basics
    Inherits VBparent
    Public plotHist As Plot_Histogram
    Public trackBox As New cv.RotatedRect
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        plotHist = New Plot_Histogram(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "CamShift vMin", 0, 255, 32)
        sliders.setupTrackBar(1, "CamShift vMax", 0, 255, 255)
        sliders.setupTrackBar(2, "CamShift Smin", 0, 255, 60)
        sliders.setupTrackBar(3, "CamShift Histogram bins", 16, 255, 32)

        label1 = "Draw anywhere to create histogram and start camshift"
        label2 = "Histogram of targeted region (hue only)"
        desc = "CamShift Demo - draw on the images to define the object to track. Tracker Algorithm"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static roi As New cv.Rect
        Static vMinLast As integer
        Static vMaxLast As integer
        Static sBinsLast As cv.Scalar
        Static roi_hist As New cv.Mat
        Dim hsv = src.CvtColor(cv.ColorConversionCodes.BGR2HSV)
        Dim hue = hsv.EmptyClone()
        Dim bins = sliders.trackbar(3).Value
        Dim hsize() As integer = {bins, bins, bins}
        Dim ranges() = {New cv.Rangef(0, 180)}
        Dim min = Math.Min(sliders.trackbar(0).Value, sliders.trackbar(1).Value)
        Dim max = Math.Max(sliders.trackbar(0).Value, sliders.trackbar(1).Value)
        Dim sbins = New cv.Scalar(0, sliders.trackbar(2).Value, min)

        cv.Cv2.MixChannels({hsv}, {hue}, {0, 0})
        Dim mask = hsv.InRange(sbins, New cv.Scalar(180, 255, max))

        If ocvb.drawRect.Width > 0 And ocvb.drawRect.Height > 0 Then
            vMinLast = min
            vMaxLast = max
            sBinsLast = sbins
            If ocvb.drawRect.X + ocvb.drawRect.Width > src.Width Then ocvb.drawRect.Width = src.Width - ocvb.drawRect.X - 1
            If ocvb.drawRect.Y + ocvb.drawRect.Height > src.Height Then ocvb.drawRect.Height = src.Height - ocvb.drawRect.Y - 1
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
            Show_HSV_Hist(dst2, roi_hist)
            If dst2.Channels = 1 Then dst2 = src
            dst2 = dst2.CvtColor(cv.ColorConversionCodes.HSV2BGR)
        End If
        dst1.SetTo(0)
        src.CopyTo(dst1, mask)
        If trackBox.Size.Width > 0 Then dst1.Ellipse(trackBox, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
    End Sub
End Class




' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class CamShift_Foreground
    Inherits VBparent
    Dim camshift As CamShift_Basics
    Dim fore As Depth_Foreground
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        camshift = New CamShift_Basics(ocvb)
        fore = New Depth_Foreground(ocvb)
        label1 = "Automatically finding the head - top of nearest object"
        desc = "Use depth to find the head and start the camshift demo.  Tracker Algorithm"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim restartRequested As Boolean
        Static depthMin As integer
        Static depthMax As integer
        If camshift.trackBox.Size.Width < 50 Then restartRequested = True
        If fore.trim.sliders.trackbar(0).Value <> depthMin Then
            depthMin = fore.trim.sliders.trackbar(0).Value
            restartRequested = True
        End If
        If fore.trim.sliders.trackbar(1).Value <> depthMax Then
            depthMax = fore.trim.sliders.trackbar(1).Value
            restartRequested = True
        End If
        If restartRequested Then fore.Run(ocvb)
        camshift.src = src
        camshift.Run(ocvb)
        dst1 = camshift.dst1
    End Sub
End Class






' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class Camshift_Object
    Inherits VBparent
    Dim blob As Blob_DepthClusters
    Dim camshift As CamShift_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        blob = New Blob_DepthClusters(ocvb)

        camshift = New CamShift_Basics(ocvb)

        label1 = "Largest blob with hue tracked.  Draw enabled."
        label2 = "Backprojection of depth clusters masked with hue"
        desc = "Use the blob depth cluster as input to initialize a camshift algorithm.  Tracker Algorithm"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        blob.Run(ocvb)
        dst2 = blob.dst2.Clone()


        If blob.flood.fBasics.masks.Count > 0 Then
            Dim largestMask = blob.flood.fBasics.maskSizes.ElementAt(0).Value
            If camshift.trackBox.Size.Width > src.Width Or camshift.trackBox.Size.Height > src.Height Then
                ocvb.drawRect = blob.flood.fBasics.rects(largestMask)
            End If
            If camshift.trackBox.Size.Width < 50 Then ocvb.drawRect = blob.flood.fBasics.rects(largestMask)
            camshift.src = src
            camshift.Run(ocvb)
            dst1 = camshift.dst1
            Dim mask = camshift.dst1.ConvertScaleAbs(255)
            cv.Cv2.BitwiseNot(mask, mask)
            dst2.SetTo(0, mask)
            If camshift.trackBox.Size.Width > 0 Then dst2.Ellipse(camshift.trackBox, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        End If
    End Sub
End Class




' https://docs.opencv.org/3.4/d7/d00/tutorial_meanshift.html
Public Class Camshift_TopObjects
    Inherits VBparent
    Dim blob As Blob_DepthClusters
    Dim cams(4 - 1) As CamShift_Basics
    Dim mats As Mat_4to1
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        mats = New Mat_4to1(ocvb)

        blob = New Blob_DepthClusters(ocvb)
        For i = 0 To cams.Length - 1
            cams(i) = New CamShift_Basics(ocvb)
        Next

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Reinitialize camshift after x frames", 1, 500, 100)
        desc = "Track - Tracker Algorithm"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        blob.Run(ocvb)
        dst1 = blob.dst2

        Static updateSlider = findSlider("Reinitialize camshift after x frames")
        Dim updateFrequency = updateSlider.Value
        Dim trackBoxes As New List(Of cv.RotatedRect)
        For i = 0 To cams.Length - 1
            If blob.flood.fBasics.maskSizes.Count > i Then
                Dim camIndex = blob.flood.fBasics.maskSizes.ElementAt(i).Value
                If ocvb.frameCount Mod updateFrequency = 0 Or cams(i).trackBox.Size.Width = 0 Then
                    ocvb.drawRect = blob.flood.fBasics.rects(camIndex)
                End If

                cams(i).src = src
                cams(i).Run(ocvb)
                mats.mat(i) = cams(i).dst1.Clone()
                trackBoxes.Add(cams(i).trackBox)
            End If
        Next
        For i = 0 To trackBoxes.Count - 1
            dst1.Ellipse(trackBoxes(i), cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        Next
        mats.Run(ocvb)
        dst2 = mats.dst1
    End Sub
End Class
