Imports cv = OpenCvSharp
Public Class MiniPC_Basics
    Inherits VBparent
    Dim resize As Resize_Percentage
    Public gCloud As Depth_PointCloud_IMU
    Public Sub New()
        initParent()
        gCloud = New Depth_PointCloud_IMU()
        resize = New Resize_Percentage
        task.desc = "Create a mini point cloud for use with histograms"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        gCloud.Run()

        resize.src = task.pointCloud
        resize.Run()

        Dim split = resize.dst1.Split()
        Dim rect = New cv.Rect(0, 0, resize.dst1.Width, resize.dst1.Height)
        If rect.Height < dst1.Height / 2 Then rect.Y = dst1.Height / 4 ' move it below the dst1 caption
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst1(rect) = split(2).ConvertScaleAbs(255)
        dst1.Rectangle(rect, cv.Scalar.White, 1)
        cv.Cv2.Merge(split, dst2)
        label1 = "MiniPC is " + CStr(rect.Width) + "x" + CStr(rect.Height) + " total pixels = " + CStr(rect.Width * rect.Height)
    End Sub
End Class








Public Class MiniPC_Rotate
    Inherits VBparent
    Dim mini As MiniPC_Basics
    Public Sub New()
        initParent()
        mini = New MiniPC_Basics
        task.desc = "Create a histogram for the mini point cloud"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If standalone Then
            mini.Run()
            input = mini.dst2 ' the task.pointcloud
            dst1 = mini.dst1.Clone
        End If

        Dim cx As Double = 1, sx As Double = 0, cy As Double = 1, sy As Double = 0, cz As Double = 1, sz As Double = 0
        Dim gM(,) As Single = {{cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                               {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                               {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz}}
        Static angleYslider = findSlider("Amount to rotate pointcloud around Y-axis (degrees)")
        Dim angleY = angleYslider.value
        '[cos(a) 0 -sin(a)]
        '[0      1       0]
        '[sin(a) 0   cos(a] rotate the point cloud around the y-axis.
        cy = Math.Cos(angleY * cv.Cv2.PI / 180)
        sy = Math.Sin(angleY * cv.Cv2.PI / 180)
        gM = {{gM(0, 0) * cy + gM(0, 1) * 0 + gM(0, 2) * sy}, {gM(0, 0) * 0 + gM(0, 1) * 1 + gM(0, 2) * 0}, {gM(0, 0) * -sy + gM(0, 1) * 0 + gM(0, 2) * cy},
              {gM(1, 0) * cy + gM(1, 1) * 0 + gM(1, 2) * sy}, {gM(1, 0) * 0 + gM(1, 1) * 1 + gM(1, 2) * 0}, {gM(1, 0) * -sy + gM(1, 1) * 0 + gM(1, 2) * cy},
              {gM(2, 0) * cy + gM(2, 1) * 0 + gM(2, 2) * sy}, {gM(2, 0) * 0 + gM(2, 1) * 1 + gM(2, 2) * 0}, {gM(2, 0) * -sy + gM(2, 1) * 0 + gM(2, 2) * cy}}

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-ocvb.sideFrustrumAdjust, ocvb.sideFrustrumAdjust), New cv.Rangef(0, ocvb.maxZ)}
        Dim histSize() = {task.pointCloud.Height, task.pointCloud.Width}
        cv.Cv2.CalcHist(New cv.Mat() {input}, New Integer() {1, 2}, New cv.Mat, dst2, 2, histSize, ranges)
        dst2 = dst2.ConvertScaleAbs(255)
    End Sub
End Class