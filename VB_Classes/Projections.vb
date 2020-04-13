Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Text
Module Projections
    ' for performance we are putting this in an optimized C++ interface to the Kinect camera for convenience...
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionRun(cPtr As IntPtr, depth As IntPtr, desiredMin As Single, desiredMax As Single, rows As Integer, cols As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionSide(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionOpen() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub SimpleProjectionClose(cPtr As IntPtr)
    End Sub



    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Projections_Gravity_Open(filename As String) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Projections_Gravity_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Projections_Gravity_Side(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Projections_Gravity_Run(cPtr As IntPtr, xPtr As IntPtr, yPtr As IntPtr, zPtr As IntPtr, maxZ As Single, rows As Int32, cols As Int32) As IntPtr
    End Function




    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Projections_GravityHist_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Projections_GravityHist_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Projections_GravityHist_Side(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Projections_GravityHist_Run(cPtr As IntPtr, xPtr As IntPtr, yPtr As IntPtr, zPtr As IntPtr, maxZ As Single, rows As Int32, cols As Int32) As IntPtr
    End Function
End Module



Public Class Projections_NoGravity_CPP : Implements IDisposable
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.externalUse = True
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        foreground = New Depth_ManualTrim(ocvb)
        foreground.externalUse = True
        foreground.sliders.TrackBar1.Value = 300  ' fixed distance to keep the images stable.
        foreground.sliders.TrackBar2.Value = 4000 ' fixed distance to keep the images stable.
        ocvb.label1 = "Top View"
        ocvb.label2 = "Side View"
        ocvb.desc = "Project the depth data onto a top view and side view."

        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        foreground.Run(ocvb)

        Dim h = ocvb.result1.Height
        Dim w = ocvb.result1.Width
        Dim desiredMin = CSng(foreground.sliders.TrackBar1.Value)
        Dim desiredMax = CSng(foreground.sliders.TrackBar2.Value)
        Dim range = CSng(desiredMax - desiredMin)
        Dim depth32f = getDepth32f(ocvb)
        If depthBytes Is Nothing Then
            ReDim depthBytes(depth32f.Total * depth32f.ElemSize - 1)
        End If

        Marshal.Copy(depth32f.Data, depthBytes, 0, depthBytes.Length)
        Dim handleDepth = GCHandle.Alloc(depthBytes, GCHandleType.Pinned)

        Dim imagePtr = SimpleProjectionRun(cPtr, handleDepth.AddrOfPinnedObject, desiredMin, desiredMax, depth32f.Height, depth32f.Width)

        ocvb.result1 = New cv.Mat(depth32f.Rows, depth32f.Cols, cv.MatType.CV_8U, imagePtr).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.result2 = New cv.Mat(depth32f.Rows, depth32f.Cols, cv.MatType.CV_8U, SimpleProjectionSide(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        handleDepth.Free()
        ocvb.label1 = "Top View (looking down)"
        ocvb.label2 = "Side View"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        foreground.Dispose()
        grid.Dispose()
        SimpleProjectionClose(cPtr)
    End Sub
End Class



Public Class Projections_NoGravity : Implements IDisposable
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.externalUse = True
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        foreground = New Depth_ManualTrim(ocvb)
        foreground.externalUse = True
        foreground.sliders.TrackBar1.Value = 300  ' fixed distance to keep the images stable.
        foreground.sliders.TrackBar2.Value = 4000 ' fixed distance to keep the images stable.
        ocvb.label1 = "Top View"
        ocvb.label2 = "Side View"
        ocvb.desc = "Project the depth data onto a top view and side view - using only VB code (too slow.)"

        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        foreground.Run(ocvb)

        Dim h = ocvb.result1.Height
        Dim w = ocvb.result1.Width
        Dim desiredMin = CSng(foreground.sliders.TrackBar1.Value)
        Dim desiredMax = CSng(foreground.sliders.TrackBar2.Value)
        Dim range = CSng(desiredMax - desiredMin)
        Dim depth32f = getDepth32f(ocvb)

        ' this VB.Net version is much slower than the optimized C++ version below.
        ocvb.result1.SetTo(cv.Scalar.White)
        ocvb.result2.SetTo(cv.Scalar.White)
        Dim black = New cv.Vec3b(0, 0, 0)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
         Sub(roi)
             For y = roi.Y To roi.Y + roi.Height - 1
                 For x = roi.X To roi.X + roi.Width - 1
                     Dim m = foreground.Mask.At(Of Byte)(y, x)
                     If m > 0 Then
                         Dim depth = depth32f.Get(Of Single)(y, x)
                         Dim dy = CInt(h * (depth - desiredMin) / range)
                         If dy < h And dy > 0 Then ocvb.result1.Set(Of cv.Vec3b)(h - dy, x, black)
                         Dim dx = CInt(w * (depth - desiredMin) / range)
                         If dx < w And dx > 0 Then ocvb.result2.Set(Of cv.Vec3b)(y, dx, black)
                     End If
                 Next
             Next
         End Sub)
        ocvb.label1 = "Top View (looking down)"
        ocvb.label2 = "Side View"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        foreground.Dispose()
        grid.Dispose()
        SimpleProjectionClose(cPtr)
    End Sub
End Class








Public Class Projections_GravityVB : Implements IDisposable
    Dim imu As IMU_AnglesToGravity
    Dim sliders As New OptionsSliders
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        imu = New IMU_AnglesToGravity(ocvb)
        imu.result = RESULT2
        imu.externalUse = True

        grid = New Thread_Grid(ocvb)
        grid.externalUse = True
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        sliders.setupTrackBar1(ocvb, "Gravity Transform Max Depth (in millimeters)", 0, 10000, 4000)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Rotate the point cloud data with the gravity data and project a top down and side view"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        imu.Run(ocvb)
        Dim split() = cv.Cv2.Split(ocvb.pointCloud)
        Dim vertSplit = split

        Dim zCos = Math.Cos(imu.angleZ)
        Dim zSin = Math.Sin(imu.angleZ)

        Dim xCos = Math.Cos(imu.angleX)
        Dim xSin = Math.Sin(imu.angleX)

        Dim xArray(,) As Single = {{1, 0, 0, 0}, {0, zCos, -zSin, 0}, {0, zSin, zCos, 0}, {0, 0, 0, 1}}
        Dim xRotate = New cv.Mat(4, 4, cv.MatType.CV_32F, xArray)

        Dim zArray(,) As Single = {{xCos, -xSin, 0, 0}, {xSin, xCos, 0, 0}, {0, 0, 1, 0}, {0, 0, 0, 1}}
        Dim zRotate = New cv.Mat(4, 4, cv.MatType.CV_32F, zArray)
        Dim yRotate = (xRotate * zRotate).ToMat

        Dim xz(4 * 4) As Single
        For j = 0 To yRotate.Rows - 1
            For i = 0 To yRotate.Cols - 1
                xz(i * 4 + j) = yRotate.At(Of Single)(i, j)
            Next
        Next

        vertSplit(0) = xz(0) * split(0) + xz(1) * split(1) + xz(2) * split(2)
        vertSplit(1) = xz(4) * split(0) + xz(5) * split(1) + xz(6) * split(2)
        vertSplit(2) = xz(8) * split(0) + xz(9) * split(1) + xz(10) * split(2)

        Dim mask = vertSplit(2).Threshold(0.001, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        cv.Cv2.Normalize(vertSplit(1), vertSplit(1), 0, ocvb.color.Height, cv.NormTypes.MinMax, -1, mask)

        Dim minval As Double, maxval As Double
        Dim minloc As cv.Point, maxloc As cv.Point
        vertSplit(0).MinMaxLoc(minval, maxval, minloc, maxloc, mask)

        Dim black = New cv.Vec3b(0, 0, 0)
        ocvb.result1.SetTo(cv.Scalar.White)
        ocvb.result2.SetTo(cv.Scalar.White)
        Dim desiredMax = sliders.TrackBar1.Value / 1000
        Dim w = CSng(ocvb.color.Width)
        Dim h = CSng(ocvb.color.Height)
        Dim xFactor = w / (maxval - minval)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
         Sub(roi)
             For y = roi.Y + roi.Height - 1 To roi.Y Step -1
                 For x = roi.X To roi.X + roi.Width - 1
                     Dim m = mask.At(Of Byte)(h - y - 1, x)
                     If m > 0 Then
                         Dim depth = vertSplit(2).At(Of Single)(h - y - 1, x)
                         If depth < desiredMax Then
                             Dim dx = xFactor * (vertSplit(0).At(Of Single)(h - y - 1, x) - minval)
                             Dim dy = Math.Round(h * (desiredMax - depth) / desiredMax)
                             If dy < h And dy > 0 Then ocvb.result1.Set(Of cv.Vec3b)(CInt(h - dy), CInt(dx), black)
                             dy = Math.Round(vertSplit(1).At(Of Single)(y, x))
                             dx = Math.Round(w * (desiredMax - depth) / desiredMax)
                             If dy < h And dy > 0 Then ocvb.result2.Set(Of cv.Vec3b)(CInt(h - dy), CInt(dx), black)
                         End If
                     End If
                 Next
             Next
         End Sub)
        ocvb.label1 = "View looking up from under floor"
        ocvb.label2 = "Side View"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        imu.Dispose()
        sliders.Dispose()
        grid.Dispose()
    End Sub
End Class






Public Class Projections_Gravity_CPP : Implements IDisposable
    Dim imu As IMU_AnglesToGravity
    Dim sliders As New OptionsSliders
    Dim cPtr As IntPtr
    Dim histPtr As IntPtr
    Dim xBytes() As Byte
    Dim yBytes() As Byte
    Dim zBytes() As Byte
    Public histogramRun As Boolean
    Public Sub New(ocvb As AlgorithmData)
        imu = New IMU_AnglesToGravity(ocvb)
        imu.result = RESULT2
        imu.externalUse = True

        sliders.setupTrackBar1(ocvb, "Gravity Transform Max Depth (in millimeters)", 0, 10000, 4000)
        If ocvb.parms.ShowOptions Then sliders.Show()

        Dim fileInfo As New FileInfo(ocvb.parms.OpenCVfullPath + "/../../../modules/imgproc/doc/pics/colormaps/colorscale_jet.jpg")
        If fileInfo.Exists = False Then
            MsgBox("The colormaps have moved!  Projections_Gravity_CPP won't work." + vbCrLf + "Look for this file:" + fileInfo.FullName)
        End If
        cPtr = Projections_Gravity_Open(fileInfo.FullName)
        histPtr = Projections_GravityHist_Open()

        ocvb.desc = "Rotate the point cloud data with the gravity data and project a top down and side view"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        imu.Run(ocvb)
        Dim split() = cv.Cv2.Split(ocvb.pointCloud)
        Dim vertSplit = split

        Dim zCos = Math.Cos(imu.angleZ)
        Dim zSin = Math.Sin(imu.angleZ)

        Dim xCos = Math.Cos(imu.angleX)
        Dim xSin = Math.Sin(imu.angleX)

        Dim xArray(,) As Single = {{1, 0, 0, 0}, {0, zCos, -zSin, 0}, {0, zSin, zCos, 0}, {0, 0, 0, 1}}
        Dim xRotate = New cv.Mat(4, 4, cv.MatType.CV_32F, xArray)

        Dim zArray(,) As Single = {{xCos, -xSin, 0, 0}, {xSin, xCos, 0, 0}, {0, 0, 1, 0}, {0, 0, 0, 1}}
        Dim zRotate = New cv.Mat(4, 4, cv.MatType.CV_32F, zArray)
        Dim yRotate = (xRotate * zRotate).ToMat

        Dim xz(4 * 4) As Single
        For j = 0 To yRotate.Rows - 1
            For i = 0 To yRotate.Cols - 1
                xz(i * 4 + j) = yRotate.At(Of Single)(i, j)
            Next
        Next

        vertSplit(0) = xz(0) * split(0) + xz(1) * split(1) + xz(2) * split(2)
        vertSplit(1) = xz(4) * split(0) + xz(5) * split(1) + xz(6) * split(2)
        vertSplit(2) = xz(8) * split(0) + xz(9) * split(1) + xz(10) * split(2)

        Dim maxZ = sliders.TrackBar1.Value / 1000

        If zBytes Is Nothing Then
            ReDim xBytes(vertSplit(0).Total * vertSplit(0).ElemSize - 1)
            ReDim yBytes(vertSplit(1).Total * vertSplit(1).ElemSize - 1)
            ReDim zBytes(vertSplit(2).Total * vertSplit(2).ElemSize - 1)
        End If
        Marshal.Copy(vertSplit(0).Data, xBytes, 0, xBytes.Length)
        Marshal.Copy(vertSplit(1).Data, yBytes, 0, yBytes.Length)
        Marshal.Copy(vertSplit(2).Data, zBytes, 0, zBytes.Length)
        Dim handleX = GCHandle.Alloc(xBytes, GCHandleType.Pinned)
        Dim handleY = GCHandle.Alloc(yBytes, GCHandleType.Pinned)
        Dim handleZ = GCHandle.Alloc(zBytes, GCHandleType.Pinned)

        Dim imagePtr As IntPtr
        If histogramRun Then
            imagePtr = Projections_GravityHist_Run(histPtr, handleX.AddrOfPinnedObject, handleY.AddrOfPinnedObject, handleZ.AddrOfPinnedObject,
                                               maxZ, vertSplit(2).Height, vertSplit(2).Width)

            Dim histTop = New cv.Mat(vertSplit(2).Rows, vertSplit(2).Cols, cv.MatType.CV_32F, imagePtr).Clone()
            Dim histSide = New cv.Mat(vertSplit(2).Rows, vertSplit(2).Cols, cv.MatType.CV_32F, Projections_GravityHist_Side(histPtr)).Clone()

            ocvb.result1 = histTop.Normalize(255).ConvertScaleAbs()
            ocvb.result2 = histSide.Normalize(255).ConvertScaleAbs()

            ocvb.label1 = "Top View - normalized"
            ocvb.label2 = "Side View - normalized"
        Else
            imagePtr = Projections_Gravity_Run(cPtr, handleX.AddrOfPinnedObject, handleY.AddrOfPinnedObject, handleZ.AddrOfPinnedObject,
                                               maxZ, vertSplit(2).Height, vertSplit(2).Width)

            ocvb.result1 = New cv.Mat(vertSplit(2).Rows, vertSplit(2).Cols, cv.MatType.CV_8UC3, imagePtr).Clone()
            ocvb.result2 = New cv.Mat(vertSplit(2).Rows, vertSplit(2).Cols, cv.MatType.CV_8UC3, Projections_Gravity_Side(cPtr)).Clone()

            ocvb.label1 = "Top View (looking down)"
            ocvb.label2 = "Side View"
        End If

        handleX.Free()
        handleY.Free()
        handleZ.Free()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        imu.Dispose()
        sliders.Dispose()
        Projections_Gravity_Close(cPtr)
        Projections_GravityHist_Close(histPtr)
    End Sub
End Class






Public Class Projections_GravityHistogram : Implements IDisposable
    Dim gravity As Projections_Gravity_CPP
    Public Sub New(ocvb As AlgorithmData)
        gravity = New Projections_Gravity_CPP(ocvb)
        gravity.histogramRun = True

        ocvb.desc = "Use the top/down projection to create a histogram of 3D points"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        gravity.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        gravity.Dispose()
    End Sub
End Class
