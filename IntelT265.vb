Imports System.Windows.Controls
Imports rs = Intel.RealSense
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp

Module T265_Interface
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Open() As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265WaitFrame(kc As IntPtr, color As IntPtr, depthRGB As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Extrinsics(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function T265Intrinsics(kc As IntPtr) As IntPtr
    End Function
    <DllImport(("Camera_IntelT265.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub T265Close(kc As IntPtr)
    End Sub
End Module
Public Class IntelT265 : Implements IDisposable
    Dim align As rs.Align
    Dim blocks As List(Of rs.ProcessingBlock)
    Dim cfg As New rs.Config
    Dim colorBytes() As Byte
    Dim colorizer As New rs.Colorizer
    Dim ctx As New rs.Context
    Dim depth2Disparity As New rs.DisparityTransform
    Dim depthBytes() As Byte
    Dim depthRGBBytes() As Byte
    Dim device As rs.Device
    Dim devices As rs.DeviceList
    Dim disparityBytes() As Byte
    Dim h As Int32
    Dim pipeline As New rs.Pipeline
    Dim pipeline_profile As rs.PipelineProfile
    Dim leftViewBytes() As Byte
    Dim rightViewBytes() As Byte
    Dim sensor As rs.Sensor
    Dim vertices() As Byte
    Dim w As Int32
    Public color As cv.Mat
    Public DecimationFilter As Boolean
    Public depth As cv.Mat
    Public depthRGB As cv.Mat
    Public DepthToDisparity As Boolean
    Public deviceCount As Int32
    Public deviceName As String
    Public disparity As cv.Mat
    Public DisparityToDepth As Boolean
    Public Extrinsics_VB As VB_Classes.ActiveClass.Extrinsics_VB
    Public failedImageCount As Int32
    Public HoleFillingFilter As Boolean
    Public imuAccel As cv.Point3f
    Public imuGyro As cv.Point3f
    Public IMUpresent As Boolean
    Public imuTimeStamp As Double
    Public intrinsics_VB As VB_Classes.ActiveClass.Intrinsics_VB
    Public modelInverse As Boolean
    Public pc As New rs.PointCloud
    Public pcMultiplier As Single = 1
    Public pointCloud As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat
    Public SpatialFilter As Boolean
    Public TemporalFilter As Boolean
    Public ThresholdFilter As Boolean
    Dim tcPtr As IntPtr
    Dim block As New rs.CustomProcessingBlock(
        Sub(f, src)
            Using varRelease = New rs.FramesReleaser
                Try
                    For Each p In blocks
                        If p.Info.Item(0) = "Decimation Filter" And DecimationFilter Then f = p.Process(f)
                        If p.Info.Item(0) = "Threshold Filter" And ThresholdFilter Then f = p.Process(f)
                        If p.Info.Item(0) = "Depth to Disparity" Then f = p.Process(f) ' always have depth to disparity
                        If p.Info.Item(0) = "Spatial Filter" And SpatialFilter Then f = p.Process(f)
                        If p.Info.Item(0) = "Temporal Filter" And TemporalFilter Then f = p.Process(f)
                        If p.Info.Item(0) = "Hole Filling Filter" And HoleFillingFilter Then f = p.Process(f)
                        If p.Info.Item(0) = "Disparity to Depth" Then f = p.Process(f) ' always have disparity to depth
                    Next

                    f = align.Process(f)
                    f = colorizer.Process(f)

                    Dim frames = f.As(Of rs.FrameSet)()
                    Dim depthFrame = frames.DepthFrame
                    Dim colorFrame = frames.ColorFrame
                    Dim disparityFrame = depth2Disparity.Process(depthFrame)
                    Dim depthRGBframe = colorizer.Process(depthFrame)
                    Dim rightView As rs.Frame = Nothing
                    Dim leftView = frames.InfraredFrame
                    For Each frame In frames
                        If frame.Profile.Stream = rs.Stream.Infrared Then
                            If frame.Profile.Index = 2 Then
                                rightView = frame
                                Exit For
                            End If
                        End If
                    Next

                    Dim points = pc.Process(depthFrame).As(Of rs.Points)
                    Marshal.Copy(points.Data, vertices, 0, vertices.Length)

                    Marshal.Copy(colorFrame.Data, colorBytes, 0, colorBytes.Length)
                    Marshal.Copy(disparityFrame.Data, disparityBytes, 0, disparityBytes.Length)
                    Marshal.Copy(depthRGBframe.Data, depthRGBBytes, 0, depthRGBBytes.Length)
                    Marshal.Copy(depthFrame.Data, depthBytes, 0, depthBytes.Length)
                    Marshal.Copy(leftView.Data, leftViewBytes, 0, leftViewBytes.Length)
                    Marshal.Copy(rightView.Data, rightViewBytes, 0, rightViewBytes.Length)

                    ' get motion data and timestamp from the gyro and accelerometer
                    If IMUpresent Then
                        Dim gyroFrame = frames.FirstOrDefault(Of rs.Frame)(rs.Stream.Gyro, rs.Format.MotionXyz32f)
                        imuGyro = Marshal.PtrToStructure(Of cv.Point3f)(gyroFrame.Data)
                        imuTimeStamp = gyroFrame.Timestamp

                        Dim accelFrame = frames.FirstOrDefault(Of rs.Frame)(rs.Stream.Accel, rs.Format.MotionXyz32f)
                        imuAccel = Marshal.PtrToStructure(Of cv.Point3f)(accelFrame.Data)
                    End If
                Catch ex As Exception
                    Console.WriteLine("Error in CustomProcessingBlock: " + ex.Message)
                    failedImageCount += 1
                End Try
            End Using
        End Sub
    )
    Public Sub New(fps As Int32, width As Int32, height As Int32)
        If OpenCVB.deviceSearch("T265") Then deviceCount = 1
        If deviceCount = 0 Then Return

        cfg.EnableStream(rs.Stream.Pose, rs.Format.SixDOF)
        cfg.EnableStream(rs.Stream.Fisheye, 1, rs.Format.Y8)
        cfg.EnableStream(rs.Stream.Fisheye, 2, rs.Format.Y8)

        pipeline_profile = pipeline.Start(cfg)
    End Sub
    Public Sub GetNextFrame()
        Dim frames = pipeline.WaitForFrames(1000)
        Dim f = frames.FirstOrDefault(rs.Stream.Pose)
        Dim pose_data = f.As(Of rs.PoseFrame).PoseData()
        'Dim fs = f.As(Of rs.FrameSet)()
    End Sub
    Protected Overridable Sub Dispose(disposing As Boolean)
        pipeline.Stop()
        cfg.DisableAllStreams()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
    End Sub
End Class
