Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module Projections
    ' for performance we are putting this in an optimized C++ interface to the Kinect camera for convenience...
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SimpleProjectionRun(cPtr As IntPtr, depth As IntPtr, mask As IntPtr, desiredMin As Integer, desiredMax As Integer, rows As Integer, cols As Integer) As IntPtr
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

        ' cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        foreground.Run(ocvb)

        ocvb.result1.SetTo(cv.Scalar.White)
        ocvb.result2.SetTo(cv.Scalar.White)

        Dim h = ocvb.result1.Height
        Dim w = ocvb.result1.Width
        Dim desiredMin = foreground.sliders.TrackBar1.Value
        Dim desiredMax = foreground.sliders.TrackBar2.Value
        Dim range = desiredMax - desiredMin

#If 1 Then
        Dim black = New cv.Vec3b(0, 0, 0)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
         Sub(roi)
             For y = roi.Y To roi.Y + roi.Height - 1
                 For x = roi.X To roi.X + roi.Width - 1
                     Dim m = foreground.Mask.At(Of Byte)(y, x)
                     If m > 0 Then
                         Dim depth = ocvb.depth16.Get(Of UShort)(y, x)
                         If depth > 0 Then
                             Dim dy = Math.Round(h * (depth - desiredMin) / range)
                             If dy < h And dy > 0 Then ocvb.result1.Set(Of cv.Vec3b)(h - dy, x, black)
                             Dim dx = Math.Round(w * (depth - desiredMin) / range)
                             If dx < w And dx > 0 Then ocvb.result2.Set(Of cv.Vec3b)(y, dx, black)
                         End If
                     End If
                 Next
             Next
         End Sub)
#Else
        Dim depthBytes(ocvb.depth16.Total * ocvb.depth16.ElemSize - 1)
        Dim handleRGBDepth = GCHandle.Alloc(depthBytes, GCHandleType.Pinned)

#End If
        ocvb.label1 = "Top View"
        ocvb.label2 = "Side View"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        foreground.Dispose()
        grid.Dispose()
        ' SimpleProjectionClose(cPtr)
    End Sub
End Class






Public Class Projections_GravityTransform : Implements IDisposable
    Dim imu As IMU_AnglesToGravity
    Dim sliders As New OptionsSliders
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        imu = New IMU_AnglesToGravity(ocvb)
        imu.result = RESULT2

        grid = New Thread_Grid(ocvb)
        grid.externalUse = True
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        sliders.setupTrackBar1(ocvb, "Gravity Transform Max Depth (in meters)", 0, 10, 4)
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
        vertSplit(2) = xz(8) * split(0) + xz(9) * split(1) + xz(10) * split(2)

        Dim mask = vertSplit(2).Threshold(0.001, 255, cv.ThresholdTypes.Binary)
        mask = mask.ConvertScaleAbs()

        cv.Cv2.Normalize(vertSplit(0), vertSplit(0), 0, ocvb.color.Width, cv.NormTypes.MinMax, -1, mask)

        Dim black = New cv.Vec3b(0, 0, 0)
        ocvb.result1.SetTo(cv.Scalar.White)
        Dim desiredMax = sliders.TrackBar1.Value
        Dim h = ocvb.color.Height
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
         Sub(roi)
             For y = roi.Y To roi.Y + roi.Height - 1
                 For x = roi.X To roi.X + roi.Width - 1
                     Dim m = mask.At(Of Byte)(y, x)
                     If m > 0 Then
                         Dim depth = vertSplit(2).At(Of Single)(y, x)
                         If depth < desiredMax Then
                             'Dim dx = Math.Round(vertSplit(0).At(Of Single)(y, x))
                             Dim dy = Math.Round(h * (desiredMax - depth) / desiredMax)
                             ocvb.result1.Set(Of cv.Vec3b)(h - dy, x, black)
                         End If
                     End If
                 Next
             Next
         End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        imu.Dispose()
        grid.Dispose()
    End Sub
End Class