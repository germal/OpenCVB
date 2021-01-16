Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.MemoryMappedFiles
Imports System.IO.Pipes
Imports System.IO
Public Class VTK_Basics
    Inherits VBparent
    Dim pipeName As String ' this is name of pipe to the VTK task.  It is dynamic and increments.
    Dim startInfo As New ProcessStartInfo
    Dim hglobal As IntPtr
    Dim pipe As NamedPipeServerStream
    Dim rgbBuffer(2048 * 4096 - 1) As Byte ' set a very large buffer so we don't have to redim
    Dim dataBuffer(2048 * 4096 - 1) As Byte ' set a very large buffer so we don't have to redim
    Dim memMapWriter As MemoryMappedViewAccessor
    Dim memMapbufferSize As Integer
    Dim memMapFile As MemoryMappedFile
    Public memMapSysData(6) As Double ' allow space for 10 user data values
    Public memMapUserData(memMapSysData.Length) As Double ' allow space for 10 user data values
    Public memMapValues(memMapSysData.Length + memMapUserData.Length) As Double
    Public pointSize As Integer = 1
    Public rgbInput As New cv.Mat
    Public dataInput As New cv.Mat
    Public FOV As Single = 60
    Public yaw As Single = 0
    Public pitch As Single = 0
    Public roll As Single = 0
    Public zNear As Single = 0
    Public zFar As Single = 10.0
    Public vtkTitle As String = "VTKDataExample"
    Dim vtkHist As VTK_Histogram3D
    Public Sub New()
        initParent()

        If standalone Then vtkHist = New VTK_Histogram3D
        Dim fileinfo As New FileInfo(vtkTitle + ".exe")
        task.desc = "Create VTK window and update it with images"
    End Sub
    Private Sub memMapUpdate()
        ' setup the memory mapped area and initialize the intrinsicsLeft needed to convert imageXYZ to worldXYZ and for command/control of the interface.
        For i = 0 To memMapSysData.Length - 1
            ' only change this if you are changing the data in the VTK C++ code at the same time...
            memMapValues(i) = Choose(i + 1, ocvb.frameCount, src.Width, src.Height, dataInput.Total * dataInput.ElemSize,
                                         dataInput.Width, dataInput.Height, rgbInput.Total * rgbInput.ElemSize)
        Next

        For i = memMapSysData.Length To memMapSysData.Length + memMapUserData.Length - 1
            memMapValues(i) = memMapUserData(i - memMapSysData.Length)
        Next
    End Sub
    Private Sub startVTKWindow()
        ' first setup the named pipe that will be used to feed data to the VTK window
        pipeName = "VTKImages" + CStr(vtkTaskIndex)
        vtkTaskIndex += 1
        pipe = New NamedPipeServerStream(pipeName, PipeDirection.InOut, 1)

        memMapbufferSize = System.Runtime.InteropServices.Marshal.SizeOf(GetType(Double)) * (memMapValues.Length - 1)

        startInfo.FileName = vtkTitle + ".exe"
        startInfo.Arguments = CStr(src.Height) + " " + CStr(src.Width) + " " + CStr(memMapbufferSize) + " " + pipeName
        If ocvb.parms.ShowConsoleLog = False Then startInfo.WindowStyle = ProcessWindowStyle.Hidden
        Process.Start(startInfo)

        hglobal = Marshal.AllocHGlobal(memMapbufferSize)
        memMapFile = MemoryMappedFile.CreateOrOpen("OpenCVBControl", memMapbufferSize)
        memMapWriter = memMapFile.CreateViewAccessor(0, memMapbufferSize)
        If standalone = False Then pipe.WaitForConnection()
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.parms.VTK_Present = False Then Exit Sub

        If standalone Then
            ocvb.trueText("VTK_Basics is used by any VTK algorithm but has no output by itself.")
            Exit Sub
        End If

        If ocvb.frameCount = 0 Then startVTKWindow()

        Dim readPipe(4) As Byte ' we read 4 bytes because that is the signal that the other end of the named pipe wrote 4 bytes to indicate iteration complete.
        If ocvb.frameCount <> 0 Then
            Dim bytesRead = pipe.Read(readPipe, 0, 4)
            If bytesRead = 0 Then
                ocvb.trueText("The VTK process appears to have stopped.", 20, 100)
            End If
        End If

        If rgbBuffer.Length <= rgbInput.Total * rgbInput.ElemSize Then MsgBox("Stopping VTK.  rgbInput Buffer > buffer limit.")
        If dataBuffer.Length <= dataInput.Total * dataInput.ElemSize Then MsgBox("Stopping VTK.  Vertices > buffer limit.")
        memMapUpdate()

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
    Inherits VBparent
    Dim vtk As VTK_Basics
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Hist 3D bins", 1, 30, 10)
            sliders.setupTrackBar(1, "Hist 3D bin Threshold X1m", 1, 100, 1)
        End If

        vtk = New VTK_Basics

        label1 = "VTK Histogram 3D input"
        task.desc = "Plot a histogram of the depth in 3D"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.parms.VTK_Present = False Then Exit Sub

        Static lastStdev As Integer = -1
        Static binSlider = findSlider("Hist 3D bins")
        Static threshSlider = findSlider("Hist 3D bin Threshold X1m")
        vtk.memMapUserData(2) = 0 ' assume no need to recompute 3D histogram.
        If vtk.memMapUserData(0) <> binSlider.value Or vtk.memMapUserData(1) <> threshSlider.value / 1000000 Then
            vtk.memMapUserData(2) = 1 ' trigger a recompute of the 3D histogram.
        End If

        vtk.memMapUserData(0) = binSlider.Value
        vtk.memMapUserData(1) = threshSlider.Value / 1000000
        dst1 = task.color

        vtk.rgbInput = task.color
        vtk.Run()
    End Sub
End Class






