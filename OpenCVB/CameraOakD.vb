Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO.Pipes
'Public Class PyStream_Basics
'    Public Sub New()
'    End Sub
'    Public Sub Run()
'        If Task.intermediateReview = caller Then ocvb.intermediateObject = Me
'        If pythonReady Then
'            For i = 0 To memMap.memMapValues.Length - 1
'                memMap.memMapValues(i) = Choose(i + 1, ocvb.frameCount, src.Total * src.ElemSize,
'                                                Task.depth32f.Total * Task.depth32f.ElemSize, src.Rows, src.Cols)
'            Next
'            memMap.Run()

'            If rgbBuffer.Length <> src.Total * src.ElemSize Then ReDim rgbBuffer(src.Total * src.ElemSize - 1)
'            If depthBuffer.Length <> Task.depth32f.Total * Task.depth32f.ElemSize Then ReDim depthBuffer(Task.depth32f.Total * Task.depth32f.ElemSize - 1)
'            Marshal.Copy(src.Data, rgbBuffer, 0, src.Total * src.ElemSize)
'            Marshal.Copy(Task.depth32f.Data, depthBuffer, 0, depthBuffer.Length)
'            If pipeImages.IsConnected Then
'                On Error Resume Next
'                pipeImages.Write(rgbBuffer, 0, rgbBuffer.Length)
'                If pipeImages.IsConnected Then pipeImages.Write(depthBuffer, 0, depthBuffer.Length)
'            End If
'        End If
'    End Sub
'End Class
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
    Dim pythonReady As Boolean
    '  Dim memMap As Python_MemMap

    Public deviceNum As Integer
    Public cameraName As String
    Dim lidarRect As New cv.Rect
    Dim lidarWidth = 1024
    Dim depthScale As Single
    Public Sub New()
        pipeName = "OpenCVBOakDImages"
        pipeImages = New NamedPipeServerStream(pipeName, PipeDirection.In)

        Dim pythonTaskName = OpenCVB.HomeDir.FullName + "VB_Classes/Python/AddWeighted_Trackbar_PS.py"

        'memMap = New Python_MemMap()

        'If ocvb.parms.externalPythonInvocation Then
        '    pythonReady = True ' python was already running and invoked OpenCVB.
        'Else
        '    pythonReady = StartPython("--MemMapLength=" + CStr(memMap.memMapbufferSize) + " --pipeName=" + pipeName)
        'End If
        'If pythonReady Then pipeImages.WaitForConnection()
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
        deviceName = cameraName ' devicename is used to determine that the camera has been initialized.
        lidarRect = New cv.Rect((width - lidarWidth) / 2, 0, lidarWidth, height)
        leftView = New cv.Mat(height, width, cv.MatType.CV_8U, 0)
        rightView = New cv.Mat(height, width, cv.MatType.CV_8U, 0)
    End Sub
    Public Sub GetNextFrame()
        If pipelineClosed Or cPtr = 0 Then Exit Sub

        SyncLock bufferLock
            'color = New cv.Mat(height, width, cv.MatType.CV_8UC3, RS2Color(cPtr)).Clone()

            'Dim accelFrame = RS2Accel(cPtr)
            'If accelFrame <> 0 Then IMU_Acceleration = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame)
            'IMU_Acceleration.Z *= -1 ' make it consistent that the z-axis positive axis points out from the camera.

            'Dim gyroFrame = RS2Gyro(cPtr)
            'If gyroFrame <> 0 Then IMU_AngularVelocity = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame)

            'Static imuStartTime = RS2IMUTimeStamp(cPtr)
            'IMU_TimeStamp = RS2IMUTimeStamp(cPtr) - imuStartTime

            'RGBDepth = New cv.Mat(height, width, cv.MatType.CV_8UC3, RS2RGBDepth(cPtr)).Clone()
            'depth16 = New cv.Mat(height, width, cv.MatType.CV_16U, RS2RawDepth(cPtr)) * depthScale
            'leftView = New cv.Mat(height, width, cv.MatType.CV_8U, RS2LeftRaw(cPtr)).Clone()
            'rightView = New cv.Mat(height, width, cv.MatType.CV_8U, RS2RightRaw(cPtr)).Clone()
            'pointCloud = New cv.Mat(height, width, cv.MatType.CV_32FC3, RS2PointCloud(cPtr)).Clone()
            MyBase.GetNextFrameCounts(IMU_FrameTime)
        End SyncLock
    End Sub
    Public Sub stopCamera()
        pipelineClosed = True
        frameCount = 0
        cPtr = 0
    End Sub
End Class