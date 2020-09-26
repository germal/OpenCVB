Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
Module recordPlaybackCommon
    Public bytesPerColor As Int64
    Public bytesPerDepth16 As Int64
    Public bytesPerRGBDepth As Int64
    Public bytesPerCloud As Int64
    Public Structure fileHeader
        Public pcBufferSize As integer ' indicates that a point cloud is in the data stream.

        Public colorWidth As integer
        Public colorHeight As integer
        Public colorElemsize As integer

        Public depthWidth As integer
        Public depthHeight As integer
        Public depth16Elemsize As integer

        Public RGBDepthWidth As integer
        Public RGBDepthHeight As integer
        Public RGBDepthElemsize As integer

        Public cloudWidth As integer
        Public cloudHeight As integer
        Public cloudElemsize As integer
    End Structure
    Public Sub writeHeader(ocvb As VBocvb, binWrite As BinaryWriter)
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
    Inherits VBparent
    Dim binWrite As BinaryWriter
    Dim recordingActive As Boolean
    Dim colorBytes() As Byte
    Dim RGBDepthBytes() As Byte
    Dim depth16Bytes() As Byte
    Dim cloudBytes() As Byte
    Dim maxBytes As Single = 20000000000
    Dim recordingFilename As FileInfo
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        ocvb.openFileDialogRequested = True
        ocvb.openFileInitialDirectory = ocvb.parms.homeDir + "/Data/"
        ocvb.openFileDialogName = GetSetting("OpenCVB", "ReplayFileName", "ReplayFileName", ocvb.parms.homeDir + "Recording.ocvb")
        ocvb.openFileFilter = "ocvb (*.ocvb)|*.ocvb"
        ocvb.openFileFilterIndex = 1
        ocvb.openFileDialogTitle = "Select an OpenCVB bag file to create"
        ocvb.initialStartSetting = False

        ocvb.desc = "Create a recording of camera data that contains color, depth, RGBDepth, pointCloud, and IMU data in an .bob file."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static bytesTotal As Int64
        recordingFilename = New FileInfo(ocvb.openFileDialogName)
        If ocvb.parms.useRecordedData And recordingFilename.Exists = False Then
            ocvb.trueText("Record the file: " + recordingFilename.FullName + " first before attempting to use it in the regression tests.", 10, 125)
            Exit Sub
        End If

        If ocvb.fileStarted Then
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

                binWrite = New BinaryWriter(File.Open(recordingFilename.FullName, FileMode.Create))
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

                If bytesTotal >= maxBytes Then
                    ocvb.fileStarted = False
                    recordingActive = False
                Else
                    ocvb.openFileSliderPercent = bytesTotal / maxBytes
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
        If recordingFilename IsNot Nothing Then SaveSetting("OpenCVB", "ReplayFileName", "ReplayFileName", recordingFilename.FullName)
        If recordingActive Then binWrite.Close()
    End Sub
End Class





Public Class Replay_Play
    Inherits VBparent
    Dim binRead As BinaryReader
    Dim playbackActive As Boolean
    Dim colorBytes() As Byte
    Dim depth16Bytes() As Byte
    Dim RGBDepthBytes() As Byte
    Dim cloudBytes() As Byte
    Dim fh As New fileHeader
    Dim fs As FileStream
    Dim recordingFilename As FileInfo
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.openFileDialogRequested = True
        ocvb.openFileInitialDirectory = ocvb.parms.homeDir + "/Data/"
        ocvb.openFileDialogName = GetSetting("OpenCVB", "ReplayFileName", "ReplayFileName", ocvb.parms.homeDir + "Recording.ocvb")
        ocvb.openFileFilter = "ocvb (*.ocvb)|*.ocvb"
        ocvb.openFileFilterIndex = 1
        ocvb.openFileDialogTitle = "Select an OpenCVB bag file to create"
        ocvb.initialStartSetting = True

        ocvb.desc = "Playback a file recorded by OpenCVB"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static bytesTotal As Int64
        recordingFilename = New FileInfo(ocvb.openFileDialogName)
        If recordingFilename.Exists = False Then ocvb.trueText("File not found: " + recordingFilename.FullName, 10, 125)
        If ocvb.fileStarted And recordingFilename.Exists Then
            Dim maxBytes = recordingFilename.Length
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
                ocvb.openFileSliderPercent = bytesTotal / recordingFilename.Length
                dst1 = ocvb.color.Clone()
                dst2 = ocvb.RGBDepth.Clone()
            Else
                ' start playback...
                fs = New FileStream(recordingFilename.FullName, FileMode.Open, FileAccess.Read)
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
        If recordingFilename IsNot Nothing Then SaveSetting("OpenCVB", "ReplayFileName", "ReplayFileName", recordingFilename.FullName)
        If playbackActive Then binRead.Close()
    End Sub
End Class





Public Class Replay_OpenGL
    Inherits VBparent
    Dim ogl As OpenGL_Callbacks
    Dim replay As Replay_Play
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ogl = New OpenGL_Callbacks(ocvb)
        replay = New Replay_Play(ocvb)
        ocvb.desc = "Replay a recorded session with OpenGL"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        replay.Run(ocvb)
        ogl.src = ocvb.color
        ogl.Run(ocvb)
    End Sub
End Class
