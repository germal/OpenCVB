﻿Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.Pipes
Imports System.IO
Structure OakIMUdata ' not working - no interface to the IMU available yet.
    Public translation As cv.Point3f
    Public acceleration As cv.Point3f
    Public velocity As cv.Point3f
    Public rotation As cv.Point3f
    Public angularVelocity As cv.Point3f
    Public angularAcceleration As cv.Point3f
    Public trackerConfidence As Integer
    Public mapperConfidence As Integer
End Structure
Public Class CameraOakD
    Inherits Camera

    Dim pipeName As String
    Dim pipeImages As NamedPipeServerStream
    Dim rgbBuffer(1) As Byte
    Dim depthBuffer(1) As Byte
    Dim leftBuffer(1) As Byte
    Dim rightBuffer(1) As Byte
    Dim pythonReady As Boolean

    Public deviceNum As Integer
    Public cameraName As String
    Dim depthScale As Single
    Public Sub New()
    End Sub
    Public Function queryDeviceCount() As Integer
        Return 1
    End Function
    Public Function queryDevice(index As Integer) As String
        Return "Oak-D"
    End Function
    Public Function querySerialNumber(index As Integer) As String
        Return 0
    End Function
    Public Sub initialize(_width As Integer, _height As Integer, fps As Integer)
        width = _width
        height = _height
        deviceName = "Oak-D"

        Static PipeTaskIndex As Integer
        pipeName = "OpenCVBOakDImages" + CStr(PipeTaskIndex)
        PipeTaskIndex += 1
        pipeImages = New NamedPipeServerStream(pipeName, PipeDirection.In)

        Dim pythonApp = New FileInfo(OpenCVB.HomeDir.FullName + "VB_Classes/CameraOakD.py")

        If pythonApp.Exists Then
            Dim p As New Process
            p.StartInfo.FileName = OpenCVB.optionsForm.PythonExeName.Text
            p.StartInfo.WorkingDirectory = pythonApp.DirectoryName
            p.StartInfo.Arguments = """" + pythonApp.Name + """" + " --Width=" + CStr(width) + " --Height=" + CStr(height) + " --pipeName=" + pipeName
            'p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            If p.Start() = False Then
                MsgBox("The Python script for the Oak-D interface failed to start.  Review " + pythonApp.Name)
            Else
                pipeImages.WaitForConnection()
            End If
        Else
            MsgBox(pythonApp.FullName + " is missing.")
        End If

        color = New cv.Mat(height, width, cv.MatType.CV_8UC3)
        RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3)
        depth16 = New cv.Mat(height, width, cv.MatType.CV_16U)
        leftView = New cv.Mat(height, width, cv.MatType.CV_8U)
        rightView = New cv.Mat(height, width, cv.MatType.CV_8U)
        pointCloud = New cv.Mat(height, width, cv.MatType.CV_32FC3)
    End Sub
    Public Sub GetNextFrame()
        If pipelineClosed Then Exit Sub

        If rgbBuffer.Length <> color.Total * color.ElemSize Then ReDim rgbBuffer(color.Total * color.ElemSize - 1)
        If depthBuffer.Length <> depth16.Total * depth16.ElemSize Then ReDim depthBuffer(depth16.Total * depth16.ElemSize - 1)
        If leftBuffer.Length <> leftView.Total Then ReDim leftBuffer(leftView.Total - 1)
        If rightBuffer.Length <> rightView.Total Then ReDim rightBuffer(rightView.Total - 1)
        SyncLock bufferLock
            pipeImages.Read(leftBuffer, 0, leftBuffer.Length)
            pipeImages.Read(rightBuffer, 0, rightBuffer.Length)
            pipeImages.Read(depthBuffer, 0, depthBuffer.Length)
            pipeImages.Read(rgbBuffer, 0, rgbBuffer.Length)
            Marshal.Copy(leftBuffer, 0, leftView.Data, leftBuffer.Length)
            Marshal.Copy(rightBuffer, 0, rightView.Data, rightBuffer.Length)
            Marshal.Copy(depthBuffer, 0, depth16.Data, depthBuffer.Length)
            Marshal.Copy(rgbBuffer, 0, color.Data, rgbBuffer.Length)

            cv.Cv2.Flip(leftView, leftView, cv.FlipMode.Y)
            cv.Cv2.Flip(rightView, rightView, cv.FlipMode.Y)

            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End SyncLock

        cv.Cv2.ImShow("rgb", color)
    End Sub
    Public Sub stopCamera()
        SyncLock bufferLock
            pipelineClosed = True
            frameCount = 0
        End SyncLock
    End Sub
End Class