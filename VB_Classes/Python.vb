Imports System.IO
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes

Module Python_Module
    Public Function checkPythonPackage(ocvb As AlgorithmData, packageName As String) As Boolean
        ' make sure that opencv-python and numpy are installed on this system.
        If ocvb.PythonExe = "" Then
            ocvb.putText(New ActiveClass.TrueType("Python is not present but needs to be.", 10, 60, RESULT1))
            ocvb.putText(New ActiveClass.TrueType("Python is distributed with Visual Studio.", 10, 90, RESULT1))
            ocvb.putText(New ActiveClass.TrueType("Open the Visual Studio Install and be sure to select 'Python Development'.", 10, 120, RESULT1))
            Return False
        End If
        Dim pythonFileInfo = New FileInfo(ocvb.PythonExe)
        Dim packageDir = New FileInfo(pythonFileInfo.DirectoryName + "\Lib\site-packages\")
        Dim packageFolder As New IO.DirectoryInfo(packageDir.DirectoryName + "\")
        Dim packageFiles = packageFolder.GetDirectories(packageName, IO.SearchOption.TopDirectoryOnly)

        If packageFiles.Count = 0 Then
            ocvb.putText(New ActiveClass.TrueType("Python is present but the package " + packageName + " is not installed in this environment.", 10, 60, RESULT1))
            ocvb.putText(New ActiveClass.TrueType("Go to the Visual Studio menu 'Tools/Python/Python Environments'", 10, 90, RESULT1))
            ocvb.putText(New ActiveClass.TrueType("Select 'Packages' in the combo box and search for 'opencv-python'", 10, 120, RESULT1))
            Return False
        End If
        Return True
    End Function

    Public Function StartPython(ocvb As AlgorithmData, arguments As String) As Boolean
        If checkPythonPackage(ocvb, "numpy") = False Or checkPythonPackage(ocvb, "cv2") = False Then Return False
        Dim pythonApp = New FileInfo(ocvb.PythonFileName)

        If pythonApp.Exists Then
            Dim p As New Process
            p.StartInfo.FileName = ocvb.PythonExe
            p.StartInfo.WorkingDirectory = pythonApp.DirectoryName
            If arguments = "" Then
                p.StartInfo.Arguments = """" + pythonApp.Name + """"
            Else
                p.StartInfo.Arguments = """" + pythonApp.Name + """" + " " + arguments
            End If
            If ocvb.parms.ShowConsoleLog = False Then p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            If p.Start() = False Then MsgBox("The Python script " + pythonApp.Name + " failed to start")
        Else
            ocvb.putText(New ActiveClass.TrueType(pythonApp.FullName + " is missing.", 10, 60, RESULT1))
            Return False
        End If
        Return True
    End Function
End Module





Public Class Python_Run : Implements IDisposable
    Dim tryCount As Int32
    Public Sub New(ocvb As AlgorithmData)
        If ocvb.PythonFileName = "" Then ocvb.PythonFileName = ocvb.parms.dataPath + "..\VB_Classes\Python\Barebones.py"
        Dim pythonApp = New FileInfo(ocvb.PythonFileName)
        StartPython(ocvb, "")
        ocvb.desc = "Run Python app: " + pythonApp.Name
        ocvb.label1 = ""
        ocvb.label2 = ""
        ocvb.result1.SetTo(0)
        ocvb.result2.SetTo(0)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim proc = Process.GetProcessesByName("python")
        If proc.Count = 0 Then
            If tryCount < 10 Then StartPython(ocvb, "")
            tryCount += 1
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        On Error Resume Next
        Dim proc = Process.GetProcessesByName("python")
        For i = 0 To proc.Count - 1
            proc(i).Kill()
        Next i
    End Sub
End Class




Public Class Python_MemMap : Implements IDisposable
    Dim memMapWriter As MemoryMappedViewAccessor
    Dim memMapFile As MemoryMappedFile
    Dim memMapPtr As IntPtr
    Public memMapValues(49) As Double ' more than we need - buffer for growth
    Public memMapbufferSize As Int32
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        ocvb.parms.ShowConsoleLog = True
        If ocvb.PythonFileName Is Nothing Then
            ocvb.PythonFileName = ocvb.parms.dataPath + "..\VB_Classes\Python\Python_MemMap.py"
        Else
            externalUse = True ' external users will set the pythonfilename.
        End If

        memMapbufferSize = System.Runtime.InteropServices.Marshal.SizeOf(GetType(Double)) * memMapValues.Length
        memMapPtr = Marshal.AllocHGlobal(memMapbufferSize)
        memMapFile = MemoryMappedFile.CreateOrOpen("Python_MemMap", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)
        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length - 1)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length - 1)

        If externalUse = False Then
            If ocvb.parms.externalInvocation = False Then
                StartPython(ocvb, "--MemMapLength=" + CStr(memMapbufferSize))
            End If
            Dim pythonApp = New FileInfo(ocvb.PythonFileName)
            ocvb.desc = "Run Python app: " + pythonApp.Name + " to share memory with OpenCVB and Python."
        End If
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            For i = 0 To memMapValues.Length - 1
                memMapValues(i) = Choose(i + 1, ocvb.frameCount)
            Next
        End If
        Marshal.Copy(memMapValues, 0, memMapPtr, memMapValues.Length - 1)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length - 1)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If memMapPtr <> 0 Then Marshal.FreeHGlobal(memMapPtr)
        Dim proc = Process.GetProcessesByName("python")
        For i = 0 To proc.Count - 1
            proc(i).Kill()
        Next i
    End Sub
End Class




Public Class Python_SurfaceBlit : Implements IDisposable
    Dim memMap As Python_MemMap
    Dim pipeName As String
    Dim pipe As NamedPipeServerStream
    Dim rgbBuffer(1) As Byte
    Dim pointCloudBuffer(1) As Byte
    Dim PythonReady As Boolean
    Public Sub New(ocvb As AlgorithmData)
        ' this Python script requires pygame to be present...
        If checkPythonPackage(ocvb, "pygame") = False Or checkPythonPackage(ocvb, "OpenGL") = False Then
            PythonReady = False
            Exit Sub
        End If
        pipeName = "OpenCVBImages" + CStr(PipeTaskIndex)
        pipe = New NamedPipeServerStream(pipeName, PipeDirection.InOut)
        PipeTaskIndex += 1

        ' this Python script assumes that fast processing is off - the pointcloud is being used and cannot be resized.
        ocvb.parms.fastProcessing = False
        ocvb.PythonFileName = ocvb.parms.dataPath + "..\VB_Classes\Python\Python_SurfaceBlit.py"
        memMap = New Python_MemMap(ocvb)

        If ocvb.parms.externalInvocation Then
            PythonReady = True ' python was already running and invoked OpenCVB.
        Else
            PythonReady = StartPython(ocvb, "--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        End If
        If PythonReady Then pipe.WaitForConnection()
        ocvb.desc = "Stream data to Python_SurfaceBlit Python script."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If PythonReady Then
            For i = 0 To memMap.memMapValues.Length - 1
                memMap.memMapValues(i) = Choose(i + 1, ocvb.frameCount, ocvb.color.Total * ocvb.color.ElemSize, ocvb.parms.pcBufferSize, ocvb.color.Rows, ocvb.color.Cols)
            Next
            memMap.Run(ocvb)

            Dim rgb = ocvb.color.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2RGB)
            If rgbBuffer.Length <> rgb.Total * rgb.ElemSize Then ReDim rgbBuffer(rgb.Total * rgb.ElemSize - 1)
            If pointCloudBuffer.Length <> ocvb.parms.pcBufferSize Then ReDim pointCloudBuffer(ocvb.parms.pcBufferSize - 1)
            Marshal.Copy(rgb.Data, rgbBuffer, 0, rgb.Total * rgb.ElemSize)
            Marshal.Copy(ocvb.pointCloud.Data, pointCloudBuffer, 0, ocvb.parms.pcBufferSize - 1)

            If pipe.IsConnected Then
                On Error Resume Next
                pipe.Write(rgbBuffer, 0, rgbBuffer.Length)
                If pipe.IsConnected Then pipe.Write(pointCloudBuffer, 0, ocvb.parms.pcBufferSize)
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If PythonReady = False Then Exit Sub ' none of this was created if Python wasn't found
        memMap.Dispose()
        If pipe IsNot Nothing Then
            If pipe.IsConnected Then
                pipe.Flush()
                pipe.WaitForPipeDrain()
                pipe.Disconnect()
            End If
        End If

        On Error Resume Next
        Dim proc = Process.GetProcessesByName("python")
        For i = 0 To proc.Count - 1
            proc(i).Kill()
        Next i
    End Sub
End Class



Public Class Python_RGBDepth : Implements IDisposable
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

        ocvb.PythonFileName = ocvb.parms.dataPath + "..\VB_Classes\Python\Python_RGBDepth.py"
        memMap = New Python_MemMap(ocvb)

        If ocvb.parms.externalInvocation Then
            pythonReady = True ' python was already running and invoked OpenCVB.
        Else
            pythonReady = StartPython(ocvb, "--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        End If
        If pythonReady Then pipeImages.WaitForConnection()
        ocvb.desc = "Stream data to the Python_RGBDepth Python script"
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




Public Class Python_Send : Implements IDisposable
    Dim pipeName As String
    Dim pipeImages As NamedPipeServerStream
    Dim rgbBuffer(1) As Byte
    Dim pythonReady As Boolean
    Dim memMap As Python_MemMap
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        pipeName = "OpenCVBImages" + CStr(PipeTaskIndex)
        pipeImages = New NamedPipeServerStream(pipeName, PipeDirection.Out)
        PipeTaskIndex += 1

        ocvb.PythonFileName = ocvb.parms.dataPath + "..\VB_Classes\Python\Python_Camshift.py"
        memMap = New Python_MemMap(ocvb)

        pythonReady = StartPython(ocvb, "--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        If pythonReady Then pipeImages.WaitForConnection()
        Dim pythonApp = New FileInfo(ocvb.PythonFileName)
        ocvb.desc = "Send data to Python script: " + pythonApp.Name
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




Public Class Python_Camshift : Implements IDisposable
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
        ocvb.PythonFileName = ocvb.parms.dataPath + "..\VB_Classes\Python\Python_Camshift.py"
        memMap = New Python_MemMap(ocvb)

        If ocvb.parms.externalInvocation Then
            pythonReady = True ' python was already running and invoked OpenCVB.
        Else
            pythonReady = StartPython(ocvb, "--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        End If
        If pythonReady Then pipeImages.WaitForConnection()
        ocvb.desc = "Stream data to the Python_Camshift Python script"
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