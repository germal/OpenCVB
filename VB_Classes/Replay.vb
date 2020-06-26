Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
Module recordPlaybackCommon
    Public bytesPerColor As Int64
    Public bytesPerDepth16 As Int64
    Public bytesPerRGBDepth As Int64
    Public bytesPerCloud As Int64
    Public Structure fileHeader
        Public pcBufferSize As Int32 ' indicates that a point cloud is in the data stream.

        Public colorWidth As Int32
        Public colorHeight As Int32
        Public colorElemsize As Int32

        Public depthWidth As Int32
        Public depthHeight As Int32
        Public depth16Elemsize As Int32

        Public RGBDepthWidth As Int32
        Public RGBDepthHeight As Int32
        Public RGBDepthElemsize As Int32

        Public cloudWidth As Int32
        Public cloudHeight As Int32
        Public cloudElemsize As Int32
    End Structure
    Public Sub writeHeader(ocvb As AlgorithmData, binWrite As BinaryWriter)
        binWrite.Write(ocvb.color.Width)
        binWrite.Write(ocvb.color.Height)
        binWrite.Write(ocvb.color.ElemSize)

        binWrite.Write(ocvb.depth16.Width)
        binWrite.Write(ocvb.depth16.Height)
        binWrite.Write(ocvb.depth16.ElemSize)

        binWrite.Write(ocvb.RGBDepth.Width)
        binWrite.Write(ocvb.RGBDepth.Height)
        binWrite.Write(ocvb.RGBDepth.ElemSize)

        binWrite.Write(ocvb.pointCloud.Width)
        binWrite.Write(ocvb.pointCloud.Height)
        binWrite.Write(ocvb.pointCloud.ElemSize)
    End Sub
    Public Sub readHeader(ByRef header As fileHeader, binRead As BinaryReader)
        header.colorWidth = binRead.ReadInt32()
        header.colorHeight = binRead.ReadInt32()
        header.colorElemsize = binRead.ReadInt32()

        header.depthWidth = binRead.ReadInt32()
        header.depthHeight = binRead.ReadInt32()
        header.depth16Elemsize = binRead.ReadInt32()

        header.RGBDepthWidth = binRead.ReadInt32()
        header.RGBDepthHeight = binRead.ReadInt32()
        header.RGBDepthElemsize = binRead.ReadInt32()

        header.cloudWidth = binRead.ReadInt32()
        header.cloudHeight = binRead.ReadInt32()
        header.cloudElemsize = binRead.ReadInt32()
    End Sub
End Module




Public Class Replay_Record
    Inherits ocvbClass
    Dim recording As New OptionsRecordPlayback
    Dim binWrite As BinaryWriter
    Dim recordingActive As Boolean
    Dim colorBytes() As Byte
    Dim RGBDepthBytes() As Byte
    Dim depth16Bytes() As Byte
    Dim cloudBytes() As Byte
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        If ocvb.parms.ShowOptions Then recording.Show()
        ocvb.desc = "Create a recording of camera data that contains color, depth, RGBDepth, pointCloud, and IMU data in an .bob file."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static bytesTotal As Int64
        If recording.startRecordPlayback Then
            If recordingActive = False Then
                bytesPerColor = ocvb.color.Total * ocvb.color.ElemSize
                bytesPerRGBDepth = ocvb.RGBDepth.Total * ocvb.RGBDepth.ElemSize
                bytesPerDepth16 = ocvb.depth16.Total * ocvb.depth16.ElemSize
                ' start recording...
                ReDim colorBytes(bytesPerColor - 1)
                ReDim depth16Bytes(bytesPerDepth16 - 1)
                ReDim RGBDepthBytes(bytesPerRGBDepth - 1)
                Dim pcSize = ocvb.pointCloud.Total * ocvb.pointCloud.ElemSize
                ReDim cloudBytes(pcSize - 1)

                binWrite = New BinaryWriter(File.Open(recording.fileinfo.FullName, FileMode.Create))
                recordingActive = True
                writeHeader(ocvb, binWrite)
            Else
                Marshal.Copy(ocvb.color.Data, colorBytes, 0, colorBytes.Length)
                binWrite.Write(colorBytes)
                bytesTotal += colorBytes.Length

                Marshal.Copy(ocvb.depth16.Data, depth16Bytes, 0, depth16Bytes.Length)
                binWrite.Write(depth16Bytes)
                bytesTotal += depth16Bytes.Length

                Marshal.Copy(ocvb.RGBDepth.Data, RGBDepthBytes, 0, RGBDepthBytes.Length)
                binWrite.Write(RGBDepthBytes)
                bytesTotal += RGBDepthBytes.Length

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
    Public Sub Close()
        If recordingActive Then binWrite.Close()
    End Sub
End Class





Public Class Replay_Play
    Inherits ocvbClass
    Dim playback As New OptionsRecordPlayback
    Dim binRead As BinaryReader
    Dim playbackActive As Boolean
    Dim colorBytes() As Byte
    Dim depth16Bytes() As Byte
    Dim RGBDepthBytes() As Byte
    Dim cloudBytes() As Byte
    Dim fh As New fileHeader
    Dim fs As FileStream
    Public Sub New(ocvb As AlgorithmData)
                setCaller(ocvb)
        playback.startButton.Text = "Start Playback"
        playback.Show() ' showing this options form is not optional (ha!)  The fileinfo is needed in Run so always initialize it.
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

                depth16Bytes = binRead.ReadBytes(bytesPerDepth16)
                tmpMat = New cv.Mat(fh.depthHeight, fh.depthWidth, cv.MatType.CV_16U, depth16Bytes)
                bytesTotal += depth16Bytes.Length

                RGBDepthBytes = binRead.ReadBytes(bytesPerRGBDepth)
                tmpMat = New cv.Mat(fh.RGBDepthHeight, fh.RGBDepthWidth, cv.MatType.CV_8UC3, RGBDepthBytes)
                ocvb.RGBDepth = tmpMat.Resize(ocvb.RGBDepth.Size())
                bytesTotal += RGBDepthBytes.Length

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
                dst1 = ocvb.color.Clone()
                dst2 = ocvb.RGBDepth.Clone()
            Else
                If playback.fileinfo.Exists = False Then
                    ocvb.putText(New oTrueType("File not found: " + playback.fileinfo.FullName, 10, 125))
                    playback.Button2_Click(New Object, New EventArgs)
                    Exit Sub
                End If
                ' start playback...
                fs = New FileStream(playback.fileinfo.FullName, FileMode.Open, FileAccess.Read)
                binRead = New BinaryReader(fs)
                playbackActive = True
                readHeader(fh, binRead)

                bytesPerColor = fh.colorWidth * fh.colorHeight * fh.colorElemsize
                bytesPerDepth16 = fh.cloudWidth * fh.cloudHeight * fh.depth16Elemsize
                bytesPerRGBDepth = fh.colorWidth * fh.colorHeight * fh.RGBDepthElemsize
                bytesPerCloud = fh.cloudWidth * fh.cloudHeight * fh.cloudElemsize

                ReDim colorBytes(bytesPerColor - 1)
                ReDim RGBDepthBytes(bytesPerRGBDepth - 1)
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
    Public Sub Close()
        If playbackActive Then binRead.Close()
    End Sub
End Class





Public Class Replay_OpenGL
    Inherits ocvbClass
    Dim ogl As OpenGL_Callbacks
    Dim replay As Replay_Play
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ogl = New OpenGL_Callbacks(ocvb)
        replay = New Replay_Play(ocvb)
        ocvb.desc = "Replay a recorded session with OpenGL"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        replay.Run(ocvb)
        ogl.src = ocvb.color
        ogl.Run(ocvb)
    End Sub
End Class
