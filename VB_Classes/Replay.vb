Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
Module recordPlaybackCommon
    Public bytesPerColor As Int64
    Public bytesPerDepth As Int64
    Public bytesPerDepthRGB As Int64
    Public bytesPerCloud As Int64
    Public Structure fileHeader
        Public pcBufferSize As Int32 ' indicates that a point cloud is in the data stream.

        Public colorWidth As Int32
        Public colorHeight As Int32
        Public colorElemsize As Int32

        Public depthWidth As Int32
        Public depthHeight As Int32
        Public depthElemsize As Int32

        Public depthRGBWidth As Int32
        Public depthRGBHeight As Int32
        Public depthRGBElemsize As Int32

        Public cloudWidth As Int32
        Public cloudHeight As Int32
        Public cloudElemsize As Int32
    End Structure
    Public Sub writeHeader(ocvb As AlgorithmData, binWrite As BinaryWriter)
        binWrite.Write(ocvb.color.Width)
        binWrite.Write(ocvb.color.Height)
        binWrite.Write(ocvb.color.ElemSize)

        binWrite.Write(ocvb.depth.Width)
        binWrite.Write(ocvb.depth.Height)
        binWrite.Write(ocvb.depth.ElemSize)

        binWrite.Write(ocvb.depthRGB.Width)
        binWrite.Write(ocvb.depthRGB.Height)
        binWrite.Write(ocvb.depthRGB.ElemSize)

        binWrite.Write(ocvb.pointCloud.Width)
        binWrite.Write(ocvb.pointCloud.Height)
        binWrite.Write(ocvb.pointCloud.ElemSize)
        binWrite.Write(ocvb.parms.pcBufferSize)
    End Sub
    Public Sub readHeader(ByRef header As fileHeader, binRead As BinaryReader)
        header.colorWidth = binRead.ReadInt32()
        header.colorHeight = binRead.ReadInt32()
        header.colorElemsize = binRead.ReadInt32()

        header.depthWidth = binRead.ReadInt32()
        header.depthHeight = binRead.ReadInt32()
        header.depthElemsize = binRead.ReadInt32()

        header.depthRGBWidth = binRead.ReadInt32()
        header.depthRGBHeight = binRead.ReadInt32()
        header.depthRGBElemsize = binRead.ReadInt32()

        header.cloudWidth = binRead.ReadInt32()
        header.cloudHeight = binRead.ReadInt32()
        header.cloudElemsize = binRead.ReadInt32()
        header.pcBufferSize = binRead.ReadInt32()
    End Sub
End Module




Public Class Replay_Record : Implements IDisposable
    Dim recording As New OptionsRecordPlayback
    Dim binWrite As BinaryWriter
    Dim recordingActive As Boolean
    Dim colorBytes() As Byte
    Dim depthBytes() As Byte
    Dim depthRGBBytes() As Byte
    Dim cloudBytes() As Byte
    Public Sub New(ocvb As AlgorithmData)
        If ocvb.parms.ShowOptions Then recording.Show()
        ocvb.desc = "Create a recording of camera data that contains color, depth, depthRGB, pointCloud, and IMU data in an .bob file."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static bytesTotal As Int64
        If recording.startRecordPlayback Then
            If recordingActive = False Then
                bytesPerColor = ocvb.color.Total * ocvb.color.ElemSize
                bytesPerDepth = ocvb.depth.Total * ocvb.depth.ElemSize
                bytesPerDepthRGB = ocvb.depthRGB.Total * ocvb.depthRGB.ElemSize
                ' start recording...
                ReDim colorBytes(bytesPerColor - 1)
                ReDim depthBytes(bytesPerDepth - 1)
                ReDim depthRGBBytes(bytesPerDepthRGB - 1)
                If ocvb.parms.pcBufferSize Then ReDim cloudBytes(ocvb.parms.pcBufferSize - 1)

                binWrite = New BinaryWriter(File.Open(recording.fileinfo.FullName, FileMode.Create))
                recordingActive = True
                writeHeader(ocvb, binWrite)
            Else
                Marshal.Copy(ocvb.color.Data, colorBytes, 0, colorBytes.Length)
                binWrite.Write(colorBytes)
                bytesTotal += colorBytes.Length

                Marshal.Copy(ocvb.depth.Data, depthBytes, 0, depthBytes.Length)
                binWrite.Write(depthBytes)
                bytesTotal += depthBytes.Length

                Marshal.Copy(ocvb.depthRGB.Data, depthRGBBytes, 0, depthRGBBytes.Length)
                binWrite.Write(depthRGBBytes)
                bytesTotal += depthRGBBytes.Length

                Marshal.Copy(ocvb.pointCloud.Data, cloudBytes, 0, cloudBytes.Length)
                binWrite.Write(cloudBytes)
                bytesTotal += cloudBytes.Length

                If bytesTotal >= 20000000000 Then
                    recording.Button2_Click(New Object, New EventArgs)
                    recordingActive = False
                Else
                    recording.BytesMovedTrackbar.Value = bytesTotal / 1000000
                End If
            End If
        Else
            If recordingActive Then
                ' stop recording
                binWrite.Close()
                recordingActive = False
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If recordingActive Then binWrite.Close()
        recording.Dispose()
    End Sub
End Class





Public Class Replay_Play : Implements IDisposable
    Dim playback As New OptionsRecordPlayback
    Dim binRead As BinaryReader
    Dim playbackActive As Boolean
    Dim colorBytes() As Byte
    Dim depthBytes() As Byte
    Dim depthRGBBytes() As Byte
    Dim cloudBytes() As Byte
    Dim fh As New fileHeader
    Dim fs As FileStream
    Public Sub New(ocvb As AlgorithmData)
        playback.startButton.Text = "Start Playback"
        playback.Show()
        playback.Button2_Click(New Object, New EventArgs) ' autoplay the recorded data (if it exists.)

        ocvb.desc = "Playback a file recorded by OpenCVB"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static bytesTotal As Int64
        If playback.startRecordPlayback Then
            If playbackActive Then
                colorBytes = binRead.ReadBytes(bytesPerColor)
                Dim tmpMat = New cv.Mat(fh.colorHeight, fh.colorWidth, cv.MatType.CV_8UC3, colorBytes)
                ocvb.color = tmpMat.Resize(ocvb.color.Size())
                bytesTotal += colorBytes.Length

                depthBytes = binRead.ReadBytes(bytesPerDepth)
                tmpMat = New cv.Mat(fh.depthHeight, fh.depthWidth, cv.MatType.CV_16U, depthBytes)
                ocvb.depth = tmpMat.Resize(ocvb.depth.Size())
                bytesTotal += depthBytes.Length

                depthRGBBytes = binRead.ReadBytes(bytesPerDepthRGB)
                tmpMat = New cv.Mat(fh.depthRGBHeight, fh.depthRGBWidth, cv.MatType.CV_8UC3, depthRGBBytes)
                ocvb.depthRGB = tmpMat.Resize(ocvb.depthRGB.Size())
                bytesTotal += depthRGBBytes.Length

                cloudBytes = binRead.ReadBytes(bytesPerCloud)
                ocvb.pointCloud = New cv.Mat(fh.cloudHeight, fh.cloudWidth, cv.MatType.CV_32FC3, cloudBytes)  ' we cannot resize the point cloud.
                bytesTotal += cloudBytes.Length

                ' restart the video at the beginning.
                If binRead.PeekChar < 0 Then
                    binRead.Close()
                    playbackActive = False
                    bytesTotal = 0
                End If
                playback.BytesMovedTrackbar.Value = bytesTotal / 1000000
            Else
                If playback.fileinfo.Exists = False Then
                    ocvb.putText(New ActiveClass.TrueType("File not found: " + playback.fileinfo.FullName, 10, 125))
                    playback.Button2_Click(New Object, New EventArgs)
                    Exit Sub
                End If
                ' start playback...
                fs = New FileStream(playback.fileinfo.FullName, FileMode.Open, FileAccess.Read)
                binRead = New BinaryReader(fs)
                playbackActive = True
                readHeader(fh, binRead)

                bytesPerColor = fh.colorWidth * fh.colorHeight * fh.colorElemsize
                bytesPerDepth = fh.depthWidth * fh.depthHeight * fh.depthElemsize
                bytesPerDepthRGB = fh.depthRGBWidth * fh.depthRGBHeight * fh.depthRGBElemsize
                bytesPerCloud = fh.cloudWidth * fh.cloudHeight * fh.cloudElemsize

                ReDim colorBytes(bytesPerColor - 1)
                ReDim depthBytes(bytesPerDepth - 1)
                ReDim depthRGBBytes(bytesPerDepthRGB - 1)
                ReDim cloudBytes(bytesPerCloud - 1)
            End If
        Else
            If playbackActive Then
                ' stop playback
                binRead.Close()
                playbackActive = False
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        If playbackActive Then binRead.Close()
        playback.Dispose()
    End Sub
End Class





Public Class Replay_OpenGL : Implements IDisposable
    Dim ogl As OpenGL_Callbacks
    Dim replay As Replay_Play
    Public Sub New(ocvb As AlgorithmData)
        ogl = New OpenGL_Callbacks(ocvb)
        replay = New Replay_Play(ocvb)
        ocvb.desc = "Replay a recorded session with OpenGL"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        replay.Run(ocvb)
        ogl.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        ogl.Dispose()
        replay.Dispose()
    End Sub
End Class