Imports cv = OpenCvSharp
Public Class Projections_SideAndDown : Implements IDisposable
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.externalUse = True
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        foreground = New Depth_ManualTrim(ocvb)
        foreground.sliders.TrackBar1.Value = 300  ' fixed distance to keep the images stable.
        foreground.sliders.TrackBar2.Value = 1200 ' fixed distance to keep the images stable.
        ocvb.label1 = "Top View"
        ocvb.label2 = "Side View"
        ocvb.desc = "Project the depth data onto a top view and side view."
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

        Parallel.ForEach(Of cv.Rect)(grid.roiList,
         Sub(roi)
             For y = roi.Y To roi.Y + roi.Height - 1
                 For x = roi.X To roi.X + roi.Width - 1
                     Dim m = foreground.Mask.At(Of Byte)(y, x)
                     If m > 0 Then
                         Dim depth = ocvb.depth16.Get(Of UShort)(y, x)
                         If depth > 0 Then
                             Dim dy = Math.Round(h * (depth - desiredMin) / range)
                             If dy < h And dy > 0 Then ocvb.result1.Set(Of cv.Vec3b)(h - dy, x, ocvb.color.At(Of cv.Vec3b)(y, x))
                             Dim dx = Math.Round(w * (depth - desiredMin) / range)
                             If dx < w And dx > 0 Then ocvb.result2.Set(Of cv.Vec3b)(y, dx, ocvb.color.At(Of cv.Vec3b)(y, x))
                         End If
                     End If
                 Next
             Next
         End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        foreground.Dispose()
        grid.Dispose()
    End Sub
End Class






Public Class Projections_GravityTransform : Implements IDisposable
    Dim imu As IMU_AnglesToGravity
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        imu = New IMU_AnglesToGravity(ocvb)
        grid = New Thread_Grid(ocvb)
        grid.externalUse = True
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        ocvb.desc = "Rotate the point cloud data with the gravity data and project a top down and side view from the data"
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

        Dim mask = vertSplit(0).Threshold(0.001, 255, cv.ThresholdTypes.Binary)
        mask = mask.ConvertScaleAbs()
        cv.Cv2.Normalize(vertSplit(0), vertSplit(0), 0, ocvb.color.Width, cv.NormTypes.MinMax, -1, mask)
        cv.Cv2.Normalize(vertSplit(2), vertSplit(2), 0, ocvb.color.Height, cv.NormTypes.MinMax, -1, mask)

        Dim minval As Double, maxval As Double
        split(2).MinMaxLoc(minval, maxval)

        Dim black = New cv.Vec3b(0, 0, 0)
        ocvb.result2.SetTo(cv.Scalar.White)
        'Parallel.ForEach(Of cv.Rect)(grid.roiList,
        ' Sub(roi)
        '     For y = roi.Y To roi.Y + roi.Height - 1
        '         For x = roi.X To roi.X + roi.Width - 1
        '             Dim depth = split(1).At(Of Single)(y, x)
        '             If depth > 0 Then
        '                 ocvb.result2.Set(Of cv.Vec3b)(CInt(depth), CInt(split(0).At(Of Single)(y, x)), black)
        '             End If
        '         Next
        '     Next
        ' End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        imu.Dispose()
        grid.Dispose()
    End Sub
End Class