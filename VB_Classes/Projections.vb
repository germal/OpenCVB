Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module Projections
    ' for performance we are putting this in an optimized C++ interface to the Kinect camera for convenience...
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionRun(cPtr As IntPtr, depth As IntPtr, desiredMin As Single, desiredMax As Single, rows As Integer, cols As Integer) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionSide(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionOpen() As IntPtr
    End Function
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub SimpleProjectionClose(cPtr As IntPtr)
    End Sub
End Module



Public Class Projections_SideAndDown : Implements IDisposable
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

#If 0 Then
        ' this VB.Net version is much slower than the optimized C++ version below.
        Dim depth32f As New cv.Mat
        ocvb.depth16.ConvertTo(depth32f, cv.MatType.CV_32F)
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
                         Dim dy = h * (depth - desiredMin) / range
                         If dy < h And dy > 0 Then ocvb.result1.Set(Of cv.Vec3b)(h - dy, x, black)
                         Dim dx = w * (depth - desiredMin) / range
                         If dx < w And dx > 0 Then ocvb.result2.Set(Of cv.Vec3b)(y, dx, black)
                     End If
                 Next
             Next
         End Sub)
#Else
        If depthBytes Is Nothing Then
            ReDim depthBytes(ocvb.depth16.Total * ocvb.depth16.ElemSize - 1)
        End If

        Marshal.Copy(ocvb.depth16.Data, depthBytes, 0, depthBytes.Length)
        Dim handleDepth = GCHandle.Alloc(depthBytes, GCHandleType.Pinned)

        Dim imagePtr = SimpleProjectionRun(cPtr, handleDepth.AddrOfPinnedObject, desiredMin, desiredMax, ocvb.depth16.Height, ocvb.depth16.Width)

        ocvb.result1 = New cv.Mat(ocvb.depth16.Rows, ocvb.depth16.Cols, cv.MatType.CV_8U, imagePtr).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.result2 = New cv.Mat(ocvb.depth16.Rows, ocvb.depth16.Cols, cv.MatType.CV_8U, SimpleProjectionSide(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        handleDepth.Free()
#End If
        ocvb.label1 = "Top View (looking down)"
        ocvb.label2 = "Side View"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        foreground.Dispose()
        grid.Dispose()
        SimpleProjectionClose(cPtr)
    End Sub
End Class






Public Class Projections_GravityTransform : Implements IDisposable
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

        Dim mask = vertSplit(2).Threshold(0.001, 255, cv.ThresholdTypes.Binary)
        mask = mask.ConvertScaleAbs()

        'cv.Cv2.Normalize(vertSplit(0), vertSplit(0), 0, ocvb.color.Width, cv.NormTypes.MinMax, -1, mask)
        cv.Cv2.Normalize(vertSplit(1), vertSplit(1), 0, ocvb.color.Height, cv.NormTypes.MinMax, -1, mask)

        Dim minval As Double, maxval As Double
        Dim minloc As cv.Point, maxloc As cv.Point
        vertSplit(0).MinMaxLoc(minval, maxval, minloc, maxloc, mask)

        Dim black = New cv.Vec3b(0, 0, 0)
        ocvb.result1.SetTo(cv.Scalar.White)
        ocvb.result2.SetTo(cv.Scalar.White)
        Dim desiredMax = sliders.TrackBar1.Value / 1000
        Dim w = ocvb.color.Width
        Dim h = ocvb.color.Height
        Dim xFactor = w / (maxval - minval)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
         Sub(roi)
             'For i = 0 To grid.roiList.Count - 1
             '    Dim roi = grid.roiList.ElementAt(i)
             For y = roi.Y + roi.Height - 1 To roi.Y Step -1
                 For x = roi.X To roi.X + roi.Width - 1
                     Dim m = mask.At(Of Byte)(h - y - 1, x)
                     If m > 0 Then
                         Dim depth = vertSplit(2).At(Of Single)(h - y - 1, x)
                         If depth < desiredMax Then
                             Dim dx = xFactor * (vertSplit(0).At(Of Single)(h - y - 1, x) - minval)
                             Dim dy = Math.Round(h * (desiredMax - depth) / desiredMax)
                             ocvb.result1.Set(Of cv.Vec3b)(h - dy, dx, black)
                             dy = Math.Round(vertSplit(1).At(Of Single)(y, x))
                             dx = Math.Round(w * (desiredMax - depth) / desiredMax)
                             If dy < h And dy > 0 Then ocvb.result2.Set(Of cv.Vec3b)(h - dy, dx, black)
                         End If
                     End If
                 Next
             Next
             'Next
         End Sub)
        ocvb.label1 = "View looking up from under floor"
        ocvb.label2 = "Side View"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        imu.Dispose()
        grid.Dispose()
    End Sub
End Class
