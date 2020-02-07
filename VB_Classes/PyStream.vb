Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes
Public Class PyStream_Basics : Implements IDisposable
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

        ' Was this class invoked directly?  Then just run something that works with RGB and depth...
        If ocvb.PythonFileName Is Nothing Then
            ocvb.PythonFileName = ocvb.parms.HomeDir + "VB_Classes/Python/AddWeighted_Trackbar_PS.py"
        End If

        memMap = New Python_MemMap(ocvb)

        If ocvb.parms.externalInvocation Then
            pythonReady = True ' python was already running and invoked OpenCVB.
        Else
            pythonReady = StartPython(ocvb, "--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        End If
        If pythonReady Then pipeImages.WaitForConnection()
        ocvb.desc = "General purpose class to pipe RGB and Depth to Python scripts."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If pythonReady Then
            For i = 0 To memMap.memMapValues.Length - 1
                memMap.memMapValues(i) = Choose(i + 1, ocvb.frameCount, ocvb.color.Total * ocvb.color.ElemSize, ocvb.depth16.Total * ocvb.depth16.ElemSize, ocvb.color.Rows, ocvb.color.Cols)
            Next
            memMap.Run(ocvb)

            If rgbBuffer.Length <> ocvb.color.Total * ocvb.color.ElemSize Then ReDim rgbBuffer(ocvb.color.Total * ocvb.color.ElemSize - 1)
            If depthBuffer.Length <> ocvb.depth16.Total * ocvb.depth16.ElemSize Then ReDim depthBuffer(ocvb.depth16.Total * ocvb.depth16.ElemSize - 1)
            Marshal.Copy(ocvb.color.Data, rgbBuffer, 0, ocvb.color.Total * ocvb.color.ElemSize)
            Marshal.Copy(ocvb.depth16.Data, depthBuffer, 0, ocvb.depth16.Total * ocvb.depth16.ElemSize - 1)
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