Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes

' https://docs.opencv.org/3.4.1/d2/dc1/camshiftdemo_8cpp-example.html
Public Class CamShift_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "CamShift vMin", 0, 255, 32)
        sliders.setupTrackBar2(ocvb, "CamShift vMax", 0, 255, 255)
        sliders.setupTrackBar3(ocvb, "CamShift Smin", 0, 255, 60)
        sliders.setupTrackBar4(ocvb, "CamShift Histogram bins", 16, 255, 16)
        sliders.Show()

        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.label1 = "Draw on any "
        ocvb.desc = "CamShift Demo - draw on the images to define the object to track."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static roi As New cv.Rect
        Static trackBox As New cv.RotatedRect
        Static vMinLast As Int32
        Static vMaxLast As Int32
        Static sBinsLast As cv.Scalar
        Static roi_hist As New cv.Mat
        Dim mask As New cv.Mat
        ocvb.color.CopyTo(ocvb.result1)
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
            cv.Cv2.CalcHist(New cv.Mat() {hue(ocvb.drawRect)}, {0, 0}, mask(ocvb.drawRect), roi_hist, 1, hsize, ranges)
            roi_hist = roi_hist.Normalize(0, 255, cv.NormTypes.MinMax)
            roi = ocvb.drawRect
            ocvb.drawRect = New cv.Rect(0, 0, 0, 0)
            'histogram2DPlot(roi_hist, ocvb.result2, bins, sbins.Val1)
        End If
        If roi_hist.Rows <> 0 Then
            Dim backproj As New cv.Mat
            cv.Cv2.CalcBackProject({hue}, {0, 0}, roi_hist, backproj, ranges)
            cv.Cv2.BitwiseAnd(backproj, mask, backproj)
            trackBox = cv.Cv2.CamShift(backproj, roi, cv.TermCriteria.Both(10, 1))
        End If
        ocvb.result1.SetTo(0)
        If trackBox.Size.Width > 0 Then ocvb.result1.Ellipse(trackBox, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        ocvb.color.CopyTo(ocvb.result1, mask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class CamShift_Depth : Implements IDisposable
    Dim camshift As CamShift_Basics
    Dim blob As Depth_FindLargestBlob
    Public Sub New(ocvb As AlgorithmData)
        camshift = New CamShift_Basics(ocvb)
        blob = New Depth_FindLargestBlob(ocvb)
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
    Public Sub Dispose() Implements IDisposable.Dispose
        camshift.Dispose()
        blob.Dispose()
    End Sub
End Class




Public Class Camshift_Python : Implements IDisposable
    Dim memMap As Python_MemMap
    Dim pipeName As String
    Dim pipeImages As NamedPipeServerStream
    Dim rgbBuffer(1) As Byte
    Dim pythonReady As Boolean
    Public Sub New(ocvb As AlgorithmData)
        pipeName = "OpenCVBImages" + CStr(PipeTaskIndex)
        pipeImages = New NamedPipeServerStream(pipeName, PipeDirection.Out)
        PipeTaskIndex += 1

        ' set the pythonfilename before initializing memMap (it indicates Python_MemMap is not running standalone.)
        ocvb.PythonFileName = ocvb.parms.HomeDir + "VB_Classes\Python\Camshift_Python.py"
        memMap = New Python_MemMap(ocvb)

        If ocvb.parms.externalInvocation Then
            pythonReady = True ' python was already running and invoked OpenCVB.
        Else
            pythonReady = StartPython(ocvb, "--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        End If
        If pythonReady Then pipeImages.WaitForConnection()
        ocvb.desc = "Stream data to the Camshift_Python Python script"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If pythonReady Then
            For i = 0 To memMap.memMapValues.Length - 1
                memMap.memMapValues(i) = Choose(i + 1, ocvb.frameCount, ocvb.color.Total * ocvb.color.ElemSize, ocvb.color.Rows, ocvb.color.Cols)
            Next
            memMap.Run(ocvb)

            If rgbBuffer.Length <> ocvb.color.Total * ocvb.color.ElemSize Then ReDim rgbBuffer(ocvb.color.Total * ocvb.color.ElemSize - 1)
            Marshal.Copy(ocvb.color.Data, rgbBuffer, 0, ocvb.color.Total * ocvb.color.ElemSize)
            If pipeImages.IsConnected Then
                On Error Resume Next
                pipeImages.Write(rgbBuffer, 0, rgbBuffer.Length)
            End If
        End If
        ocvb.putText(New ActiveClass.TrueType("Draw a rectangle anywhere on the 'camshift' (Python) window nearby.", 10, 140, RESULT1))
        ocvb.putText(New ActiveClass.TrueType("Mouse down will show highlighted areas that may be used for tracking.", 10, 180, RESULT1))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        memMap.Dispose()
        If pipeImages IsNot Nothing Then
            If pipeImages.IsConnected Then
                pipeImages.Flush()
                pipeImages.WaitForPipeDrain()
                pipeImages.Disconnect()
            End If
        End If
        On Error Resume Next
        Dim proc = Process.GetProcessesByName("python")
        For i = 0 To proc.Count - 1
            proc(i).Kill()
        Next i
    End Sub
End Class