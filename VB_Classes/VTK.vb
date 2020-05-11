Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes
Imports System.IO

Public Class VTK_Basics
    Inherits ocvbClass
    Dim pipeName As String ' this is name of pipe to the VTK task.  It is dynamic and increments.
    Dim startInfo As New ProcessStartInfo
    Dim hglobal As IntPtr
    Dim pipe As NamedPipeServerStream
    Dim rgbBuffer(2048 * 4096 - 1) As Byte ' set a very large buffer so we don't have to redim
    Dim dataBuffer(2048 * 4096 - 1) As Byte ' set a very large buffer so we don't have to redim
    Dim memMapWriter As MemoryMappedViewAccessor
    Dim memMapbufferSize As Int32
    Dim memMapFile As MemoryMappedFile
    Public memMapSysData(6) As Double ' allow space for 10 user data values
    Public memMapUserData(10) As Double ' allow space for 10 user data values
    Public memMapValues(49) As Double ' more than needed - room for growth
    Public usingDepthAndRGB As Boolean = True ' if false, we are using plotData, not depth32f.
    Public pointSize As Int32 = 1
    Public rgbInput As New cv.Mat
    Public dataInput As New cv.Mat
    Public FOV As Single = 60
    Public yaw As Single = 0
    Public pitch As Single = 0
    Public roll As Single = 0
    Public zNear As Single = 0
    Public zFar As Single = 10.0
    Public vtkTitle As String = "VTK_Data"
    Public vtkPresent As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        If ocvb.parms.vtkDirectory.Length > 0 Then vtkPresent = True
        Dim fileinfo As New FileInfo(vtkTitle + ".exe")
        If fileinfo.Exists = False Then vtkPresent = False
        Dispose() ' make sure there wasn't an old VTKWindow sitting around...
        ocvb.desc = "Create VTK window and update it with images"
    End Sub
    Private Sub memMapUpdate(ocvb As AlgorithmData)
        ' setup the memory mapped area and initialize the intrinsicsLeft needed to convert imageXYZ to worldXYZ and for command/control of the interface.
        For i = 0 To memMapSysData.Length - 1
            ' only change this if you are changing the data in the VTK C++ code at the same time...
            memMapValues(i) = Choose(i + 1, ocvb.frameCount, ocvb.color.Width, ocvb.color.Height, dataInput.Total * dataInput.ElemSize,
                                         dataInput.Width, dataInput.Height, rgbInput.Total * rgbInput.ElemSize)
        Next

        For i = memMapSysData.Length To memMapValues.Length - 1
            memMapValues(i) = memMapUserData(i - memMapSysData.Length)
        Next
    End Sub
    Private Sub startVTKWindow(ocvb As AlgorithmData)
        ' first setup the named pipe that will be used to feed data to the VTK window
        pipeName = "VTKImages" + CStr(vtkTaskIndex)
        vtkTaskIndex += 1
        pipe = New NamedPipeServerStream(pipeName, PipeDirection.InOut, 1)

        memMapbufferSize = System.Runtime.InteropServices.Marshal.SizeOf(GetType(Double)) * (memMapValues.Length - 1)

        startInfo.FileName = vtkTitle + ".exe"
        startInfo.Arguments = "720 1280 " + CStr(memMapbufferSize) + " " + pipeName
        If ocvb.parms.ShowConsoleLog = False Then startInfo.WindowStyle = ProcessWindowStyle.Hidden
        Process.Start(startInfo)

        hglobal = Marshal.AllocHGlobal(memMapbufferSize)
        memMapFile = MemoryMappedFile.CreateOrOpen("OpenCVBControl", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)
        pipe.WaitForConnection()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If vtkPresent = False Then
            ocvb.vtkInstructions()
            Exit Sub
        End If
        If ocvb.frameCount = 0 Then startVTKWindow(ocvb)

        Dim readPipe(4) As Byte ' we read 4 bytes because that is the signal that the other end of the named pipe wrote 4 bytes to indicate iteration complete.
        If ocvb.frameCount <> 0 Then
            Dim bytesRead = pipe.Read(readPipe, 0, 4)
            If bytesRead = 0 Then
                ocvb.putText(New ActiveClass.TrueType("The VTK process appears to have stopped.", 20, 100))
            End If
        End If

        If usingDepthAndRGB Then
            rgbInput = ocvb.color.Clone()
            dataInput = getDepth32f(ocvb)
        End If

        If rgbBuffer.Length <= rgbInput.Total * rgbInput.ElemSize Then MsgBox("Stopping VTK.  rgbInput Buffer > buffer limit.")
        If dataBuffer.Length <= dataInput.Total * dataInput.ElemSize Then MsgBox("Stopping VTK.  Vertices > buffer limit.")
        memMapUpdate(ocvb)

        Marshal.Copy(memMapValues, 0, hglobal, memMapValues.Length - 1)
        memMapWriter.WriteArray(Of Double)(0, memMapValues, 0, memMapValues.Length - 1)

        If rgbInput.Rows > 0 Then Marshal.Copy(rgbInput.Data, rgbBuffer, 0, rgbInput.Total * rgbInput.ElemSize)
        If dataInput.Rows > 0 Then Marshal.Copy(dataInput.Data, dataBuffer, 0, dataInput.Total * dataInput.ElemSize)
        If pipe.IsConnected Then
            If rgbInput.Rows > 0 Then pipe.Write(rgbBuffer, 0, rgbInput.Total * rgbInput.ElemSize)
            If dataInput.Rows > 0 Then pipe.Write(dataBuffer, 0, dataInput.Total * dataInput.ElemSize)
        End If
    End Sub
    Public Sub Close()
        Dim proc = Process.GetProcessesByName(vtkTitle)
        For i = 0 To proc.Count - 1
            proc(i).CloseMainWindow()
        Next i
        If hglobal <> 0 Then Marshal.FreeHGlobal(hglobal)
    End Sub
End Class





Public Class VTK_Histogram3D
    Inherits ocvbClass
    Dim vtk As VTK_Basics
    Dim mats As Mat_4to1
    Dim random As Random_NormalDist
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Random Number Stdev", 0, 255, 10)
        sliders.setupTrackBar2(ocvb, caller, "Hist 3D bins", 1, 100, 32)
        sliders.setupTrackBar3(ocvb, caller, "Hist 3D bin Threshold X1000000", 10, 100, 20)

        mats = New Mat_4to1(ocvb, caller)

        ocvb.label2 = "Input to VTK plot"

        vtk = New VTK_Basics(ocvb, caller)
        vtk.usingDepthAndRGB = False

        random = New Random_NormalDist(ocvb, caller)
        ocvb.desc = "Create the test pattern and send it to VTK for 3D display."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If vtk.vtkPresent = False Then
            ocvb.vtkInstructions()
            Exit Sub
        End If

        Static lastStdev As Int32 = -1
        If vtk.memMapUserData(0) <> sliders.TrackBar2.Value Or vtk.memMapUserData(1) <> sliders.TrackBar3.Value / 1000000 Or
                lastStdev <> sliders.TrackBar1.Value Then
            vtk.memMapUserData(2) = 1 ' trigger a recompute of the 3D histogram.
        Else
            vtk.memMapUserData(2) = 0 ' no need to recompute 3D histogram.
        End If

        vtk.memMapUserData(0) = sliders.TrackBar2.Value ' number of bins
        vtk.memMapUserData(1) = sliders.TrackBar3.Value / 1000000 ' threshold

        If lastStdev <> sliders.TrackBar1.Value Then
            For i = 0 To 3
                random.sliders.TrackBar1.Value = Choose(i + 1, 25, 187, 25, 25)
                random.sliders.TrackBar2.Value = Choose(i + 1, 127, 127, 65, 65)
                random.sliders.TrackBar3.Value = Choose(i + 1, 180, 180, 180, 244)
                random.sliders.TrackBar4.Value = sliders.TrackBar1.Value
                random.Run(ocvb)
                mats.mat(i) = dst1.Clone()
            Next
            lastStdev = sliders.TrackBar1.Value
        End If

        mats.Run(ocvb)
        dst1.SetTo(0)

        vtk.rgbInput = ocvb.result2.Clone()
        vtk.dataInput = New cv.Mat ' ocvb.depth
        vtk.Run(ocvb)
    End Sub
End Class





