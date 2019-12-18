Imports System.IO
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes

Module Python_Module
    Public Function checkPythonPackage(ocvb As AlgorithmData, packageName As String) As Boolean
        ' make sure that opencv-python and numpy are installed on this system.
        If ocvb.PythonExe = "" Then
            ocvb.putText(New ActiveClass.TrueType("Python is not present and needs to be installed.", 10, 60, RESULT1))
            ocvb.putText(New ActiveClass.TrueType("Visit Python.org and download the latest version.", 10, 120, RESULT1))
            Return False
        End If
        Dim pythonFileInfo = New FileInfo(ocvb.PythonExe)
        Dim packageDir = New FileInfo(pythonFileInfo.DirectoryName + "\Lib\site-packages\")
        Dim packageFolder As New IO.DirectoryInfo(packageDir.DirectoryName + "\")
        Dim packageFiles = packageFolder.GetDirectories(packageName, IO.SearchOption.TopDirectoryOnly)

        If packageFiles.Count = 0 Then
            ocvb.putText(New ActiveClass.TrueType("Python is present but the packages needed by this Python script are not present.", 10, 60, RESULT1))
            ocvb.putText(New ActiveClass.TrueType("Go to the Visual Studio menu 'Tools/Python/Python Environments'", 10, 90, RESULT1))
            ocvb.putText(New ActiveClass.TrueType("Select 'Packages' in the combo box and search for packages required by this script.", 10, 120, RESULT1))
            MsgBox("It looks like the " + packageName + " package is missing." + vbCrLf +
                   "Be sure to install Python packages: opencv-python, NumPy, PyOpenGL, pygame, psutil.")
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
        If ocvb.PythonFileName = "" Then ocvb.PythonFileName = ocvb.parms.HomeDir + "VB_Classes/Python/Barebones.py"
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
            If tryCount < 3 Then StartPython(ocvb, "")
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
        If ocvb.PythonFileName Is Nothing Then
            ocvb.PythonFileName = ocvb.parms.HomeDir + "VB_Classes/Python/Python_MemMap.py"
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
        pipeName = "OpenCVBImages" + CStr(PipeTaskIndex)
        pipe = New NamedPipeServerStream(pipeName, PipeDirection.InOut)
        PipeTaskIndex += 1

        ocvb.parms.fastProcessing = False
        ocvb.PythonFileName = ocvb.parms.HomeDir + "VB_Classes/Python/Python_SurfaceBlit.py"
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
    Dim pipe As PipeStream_RGBDepth
    Public Sub New(ocvb As AlgorithmData)
        ocvb.PythonFileName = ocvb.parms.HomeDir + "VB_Classes/Python/Python_RGBDepth.py"
        pipe = New PipeStream_RGBDepth(ocvb)
        ocvb.desc = "Use Python to show RGB and Depth side by side."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        pipe.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        pipe.Dispose()
    End Sub
End Class
