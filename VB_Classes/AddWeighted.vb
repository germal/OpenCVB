Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes
Public Class AddWeighted_DepthRGB : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Weight", 0, 100, 50)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Add depth and rgb with specified weights."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim alpha = sliders.TrackBar1.Value / sliders.TrackBar1.Maximum
        cv.Cv2.AddWeighted(ocvb.color, alpha, ocvb.depthRGB, 1.0 - alpha, 0, ocvb.result1)
        ocvb.label1 = "depth " + Format(1 - sliders.TrackBar1.Value / 100, "#0%") + " RGB " + Format(sliders.TrackBar1.Value / 100, "#0%")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class


'Public Class pipeStream_RGBDepth : Implements IDisposable
'    Public Sub New(ocvb As AlgorithmData)
'        ocvb.label1 = "NewClass_Basics"
'        ocvb.label2 = ""
'        ocvb.desc = "New class description"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)

'    End Sub
'    Public Sub Dispose() Implements IDisposable.Dispose
'    End Sub
'End Class



Public Class AddWeighted_Trackbar : Implements IDisposable
    Dim pipeName As String
    Dim pipeImages As NamedPipeServerStream
    Dim rgbBuffer(1) As Byte
    Dim depthBuffer(1) As Byte
    Dim pythonReady As Boolean
    Dim memMap As Python_MemMap
    Public Sub New(ocvb As AlgorithmData)
        pipeName = "OpenCVBImages" + CStr(PipeTaskIndex)
        pipeImages = New NamedPipeServerStream(pipeName, PipeDirection.Out)
        PipeTaskIndex += 1

        ocvb.PythonFileName = ocvb.parms.HomeDir + "VB_Classes/Python/AddWeighted_Trackbar.py"
        memMap = New Python_MemMap(ocvb)

        If ocvb.parms.externalInvocation Then
            pythonReady = True ' python was already running and invoked OpenCVB.
        Else
            pythonReady = StartPython(ocvb, "--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        End If
        If pythonReady Then pipeImages.WaitForConnection()
        ocvb.desc = "Use Python to build a Weighted image with RGB and Depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If pythonReady Then
            For i = 0 To memMap.memMapValues.Length - 1
                memMap.memMapValues(i) = Choose(i + 1, ocvb.frameCount, ocvb.color.Total * ocvb.color.ElemSize, ocvb.depth.Total * ocvb.depth.ElemSize, ocvb.color.Rows, ocvb.color.Cols)
            Next
            memMap.Run(ocvb)

            If rgbBuffer.Length <> ocvb.color.Total * ocvb.color.ElemSize Then ReDim rgbBuffer(ocvb.color.Total * ocvb.color.ElemSize - 1)
            If depthBuffer.Length <> ocvb.depth.Total * ocvb.depth.ElemSize Then ReDim depthBuffer(ocvb.depth.Total * ocvb.depth.ElemSize - 1)
            Marshal.Copy(ocvb.color.Data, rgbBuffer, 0, ocvb.color.Total * ocvb.color.ElemSize)
            Marshal.Copy(ocvb.depth.Data, depthBuffer, 0, ocvb.depth.Total * ocvb.depth.ElemSize - 1)
            If pipeImages.IsConnected Then
                On Error Resume Next
                pipeImages.Write(rgbBuffer, 0, rgbBuffer.Length)
                If pipeImages.IsConnected Then pipeImages.Write(depthBuffer, 0, depthBuffer.Length)
            End If
        End If
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