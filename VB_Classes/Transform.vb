Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Transform_Resize
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
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
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Angle", 0, 360, 30)
        sliders.setupTrackBar2("Scale Factor", 1, 100, 100)
        sliders.setupTrackBar3("Rotation center X", 1, src.Width, src.Width / 2)
        sliders.setupTrackBar4("Rotation center Y", 1, src.Height, src.Height / 2)
        ocvb.desc = "Rotate and scale and image based on the slider values."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim imageCenter = New cv.Point2f(sliders.TrackBar3.Value, sliders.TrackBar4.Value)
        Dim rotationMat = cv.Cv2.GetRotationMatrix2D(imageCenter, sliders.TrackBar1.Value, sliders.TrackBar2.Value / 100)
        cv.Cv2.WarpAffine(src, dst1, rotationMat, New cv.Size())
        dst1.Circle(imageCenter, 10, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        dst1.Circle(imageCenter, 5, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
    End Sub
End Class



Public Class Transform_Sort
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
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
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
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





Public Class Transform_Affine3D
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Check to snap the first point cloud"
        check.Box(1).Text = "Check to snap the second point cloud"
        ocvb.desc = "Using 2 point clouds compute the 3D affine transform between them"
        ocvb.putText(New oTrueType("Use the check boxes to snapshot the different point clouds", 10, 50, RESULT1))
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static pc1 As cv.Mat
        Static pc2 As cv.Mat
        Static affineTransform As cv.Mat

        If ocvb.parms.testAllRunning Then
            If ocvb.frameCount = 30 Then check.Box(0).Checked = True
            If ocvb.frameCount = 60 Then check.Box(1).Checked = True
        End If

        If check.Box(0).Checked Then
            pc1 = ocvb.pointCloud.Clone()
            check.Box(0).Checked = False
            ocvb.putText(New oTrueType("First point cloud captured", 10, 50, RESULT1))
            affineTransform = Nothing
        End If

        If check.Box(1).Checked Then
            pc2 = ocvb.pointCloud.Clone()
            check.Box(1).Checked = False
            ocvb.putText(New oTrueType("Second point cloud captured", 10, 70, RESULT1))
            affineTransform = Nothing
        End If

        If pc1 IsNot Nothing Then
            If pc2 IsNot Nothing Then
                Dim inliers = New cv.Mat
                affineTransform = New cv.Mat(3, 4, cv.MatType.CV_64F)
                pc1 = pc1.Reshape(3, pc1.Rows * pc1.Cols)
                pc2 = pc2.Reshape(3, pc2.Rows * pc2.Cols)
                cv.Cv2.EstimateAffine3D(pc1, pc2, affineTransform, inliers)
                pc1 = Nothing
                pc2 = Nothing
            End If
        End If

        If affineTransform IsNot Nothing Then
            ocvb.putText(New oTrueType("Affine Transform 3D results:", 10, 90, RESULT1))
            For i = 0 To 3 - 1
                Dim outstr = ""
                For j = 0 To 4 - 1
                    outstr += Format(affineTransform.Get(Of Double)(i, j), "0.000") + vbTab
                Next
                ocvb.putText(New oTrueType(outstr, 10, 110 + i * 25, RESULT1))
            Next
            ocvb.putText(New oTrueType("0" + vbTab + "0" + vbTab + "0" + vbTab + "1", 10, 80 + 4 * 25, RESULT1))
        End If
    End Sub
End Class







' https://stackoverflow.com/questions/19093728/rotate-image-around-x-y-z-axis-in-opencv
' https://stackoverflow.com/questions/7019407/translating-and-rotating-an-image-in-3d-using-opencv
Public Class Transform_Gravity
    Inherits ocvbClass
    Public imu As IMU_GVector
    Public vertSplit(3 - 1) As cv.Mat
    Public xyz(0) As Single
    Dim smooth As Depth_SmoothingMat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        smooth = New Depth_SmoothingMat(ocvb)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Apply smoothing to depth data"
        check.Box(0).Checked = False
        check.Visible = False ' smoothing is not working well enough yet...
        vertSplit(0) = New cv.Mat
        vertSplit(1) = New cv.Mat
        vertSplit(2) = New cv.Mat

        imu = New IMU_GVector(ocvb)
        imu.showLog = False
        ocvb.desc = "Transform the pointcloud with the gravity vector"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        imu.Run(ocvb)

        ' normally it is not desirable to resize the point cloud but it can be here because we are building a histogram.
        Dim pc = ocvb.pointCloud.Resize(ocvb.color.Size())
        Dim split() = cv.Cv2.Split(pc)
        If check.Box(0).Checked Then
            smooth.inputInMeters = True
            smooth.src = split(2)
            smooth.Run(ocvb)
            dst1 = smooth.dst1
            cv.Cv2.Add(split(2), dst1, split(2))
            label1 = smooth.label1
        End If

        Dim zCos = Math.Cos(imu.angleZ)
        Dim zSin = Math.Sin(imu.angleZ)

        Dim xCos = Math.Cos(imu.angleX)
        Dim xSin = Math.Sin(imu.angleX)

        Dim xArray(,) As Single = {{1, 0, 0, 0}, {0, zCos, -zSin, 0}, {0, zSin, zCos, 0}, {0, 0, 0, 1}}
        Dim zArray(,) As Single = {{xCos, -xSin, 0, 0}, {xSin, xCos, 0, 0}, {0, 0, 1, 0}, {0, 0, 0, 1}}

        Dim xRotate = New cv.Mat(4, 4, cv.MatType.CV_32F, xArray)
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
        Marshal.Copy(pc.Data, xyz, 0, xyz.Length) ' why copy it?  To avoid memory leak in parallel for's.  Not sure...

        If standalone Then ocvb.putText(New oTrueType("Pointcloud is now oriented toward gravity " +
                                                      If(check.Box(0).Checked, "using smoothed depth data.", "."), 10, 125))
    End Sub
End Class
