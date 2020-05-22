Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Transform_Resize
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Resize Percent", 50, 1000, 50)
        ocvb.desc = "Resize an image based on the slider value."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim resizeFactor = sliders.TrackBar1.Value / 100
        Dim w = CInt(resizeFactor * src.Width)
        Dim h = CInt(resizeFactor * src.Height)
        If resizeFactor > 1 Then
            Dim tmp As New cv.Mat
            tmp = src.Resize(New cv.Size(w, h), 0)
            Dim roi = New cv.Rect((w - src.Width) / 2, (h - src.Height) / 2, src.Width, src.Height)
            tmp(roi).CopyTo(dst1)
        Else
            Dim roi = New cv.Rect((src.Width - w) / 2, (src.Height - h) / 2, w, h)
            dst1(roi) = src.Resize(New cv.Size(w, h), 0)
        End If
    End Sub
End Class




Public Class Transform_Rotate
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Angle", 0, 180, 30)
        sliders.setupTrackBar2(ocvb, caller, "Scale Factor", 1, 100, 50)
        ocvb.desc = "Rotate and scale and image based on the slider values."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim imageCenter = New cv.Point2f(src.Width / 2, src.Height / 2)
        Dim rotationMat = cv.Cv2.GetRotationMatrix2D(imageCenter, sliders.TrackBar1.Value, sliders.TrackBar2.Value / 100)
        cv.Cv2.WarpAffine(src, dst1, rotationMat, New cv.Size())
    End Sub
End Class



Public Class Transform_Sort
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        radio.Setup(ocvb, caller, 4)
        radio.check(0).Text = "Ascending"
        radio.check(0).Checked = True
        radio.check(1).Text = "Descending"
        radio.check(2).Text = "EveryColumn"
        radio.check(3).Text = "EveryRow"
        ocvb.desc = "Sort the pixels of a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim sortOption = cv.SortFlags.Ascending
        If radio.check(1).Checked Then sortOption = cv.SortFlags.Descending
        If radio.check(2).Checked Then sortOption = cv.SortFlags.EveryColumn
        If radio.check(3).Checked Then sortOption = cv.SortFlags.EveryRow
        dst1 = src.Sort(sortOption + cv.SortFlags.EveryColumn)
    End Sub
End Class



Public Class Transform_SortReshape
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        radio.Setup(ocvb, caller, 2)
        radio.check(0).Text = "Ascending"
        radio.check(0).Checked = True
        radio.check(1).Text = "Descending"
        ocvb.desc = "Sort the pixels of a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim sortOption = cv.SortFlags.Ascending
        If radio.check(1).Checked Then sortOption = cv.SortFlags.Descending
        src = src.Reshape(1, src.Rows * src.Cols)
        Dim sorted = src.Sort(sortOption + cv.SortFlags.EveryColumn)
        dst1 = sorted.Reshape(1, src.Rows)
    End Sub
End Class


Public Class Transform_Gravity
    Inherits ocvbClass
    Dim imu As IMU_GVector
    Public vertSplit() As cv.Mat
    Public xyz(0) As Single
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        imu = New IMU_GVector(ocvb, caller)
        ocvb.desc = "Transform the pointcloud with the gravity vector"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex = T265Camera Then
            ocvb.putText(New ActiveClass.TrueType("T265 camera has no pointcloud data", 10, 125))
            Exit Sub
        End If

        imu.Run(ocvb)

        ' normally it is not desirable to resize the point cloud but it can be here because we are building a histogram.
        Dim pc = ocvb.pointCloud.Resize(ocvb.color.Size())
        Dim split() = cv.Cv2.Split(pc)
        vertSplit = split

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
                xz(i * 4 + j) = yRotate.Get(Of Single)(i, j)
            Next
        Next

        vertSplit(0) = xz(0) * split(0) + xz(1) * split(1) + xz(2) * split(2)
        vertSplit(1) = xz(4) * split(0) + xz(5) * split(1) + xz(6) * split(2)
        vertSplit(2) = xz(8) * split(0) + xz(9) * split(1) + xz(10) * split(2)

        cv.Cv2.Merge(vertSplit, pc)
        If xyz.Length <> pc.Total * 3 Then ReDim xyz(pc.Total * 3 - 1)
        Marshal.Copy(pc.Data, xyz, 0, xyz.Length) ' why copy it?  To avoid memory leak in parallel for's.

        If standalone Then ocvb.putText(New ActiveClass.TrueType("Pointcloud is now oriented toward gravity.", 10, 125))
    End Sub
End Class