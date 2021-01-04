Imports cv = OpenCvSharp
Public Class MiniPC_Basics
    Inherits VBparent
    Dim resize As Resize_Percentage
    Public rect As cv.Rect
    Dim gCloud As Depth_PointCloud_IMU
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
        split(2).SetTo(0, task.inrange.nodepthMask.resize(split(2).Size))
        rect = New cv.Rect(0, 0, resize.dst1.Width, resize.dst1.Height)
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
    Public mini As MiniPC_Basics
    Public histogram As New cv.Mat
    Public angleY As Integer
    Public Sub New()
        initParent()
        mini = New MiniPC_Basics
        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        task.desc = "Create a histogram for the mini point cloud"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If standalone Then
            mini.Run()
            input = mini.dst2 ' the task.pointcloud
            Static angleYslider = findSlider("Amount to rotate pointcloud around Y-axis (degrees)")
            angleY = angleYslider.value
        End If

        Dim cx As Double = 1, sx As Double = 0, cy As Double = 1, sy As Double = 0, cz As Double = 1, sz As Double = 0
        Dim gM(,) As Single = {{cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                               {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                               {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz}}
        '[cos(a) 0 -sin(a)]
        '[0      1       0]
        '[sin(a) 0   cos(a] rotate the point cloud around the y-axis.
        cy = Math.Cos(angleY * cv.Cv2.PI / 180)
        sy = Math.Sin(angleY * cv.Cv2.PI / 180)
        gM = {{gM(0, 0) * cy + gM(0, 1) * 0 + gM(0, 2) * sy}, {gM(0, 0) * 0 + gM(0, 1) * 1 + gM(0, 2) * 0}, {gM(0, 0) * -sy + gM(0, 1) * 0 + gM(0, 2) * cy},
              {gM(1, 0) * cy + gM(1, 1) * 0 + gM(1, 2) * sy}, {gM(1, 0) * 0 + gM(1, 1) * 1 + gM(1, 2) * 0}, {gM(1, 0) * -sy + gM(1, 1) * 0 + gM(1, 2) * cy},
              {gM(2, 0) * cy + gM(2, 1) * 0 + gM(2, 2) * sy}, {gM(2, 0) * 0 + gM(2, 1) * 1 + gM(2, 2) * 0}, {gM(2, 0) * -sy + gM(2, 1) * 0 + gM(2, 2) * cy}}

        Dim gMat = New cv.Mat(3, 3, cv.MatType.CV_32F, gM)
        Dim gInput = input.Reshape(1, input.Rows * input.Cols)
        Dim gOutput = (gInput * gMat).ToMat
        input = gOutput.Reshape(3, input.Rows)

        Dim ranges() = New cv.Rangef() {New cv.Rangef(-ocvb.sideFrustrumAdjust, ocvb.sideFrustrumAdjust), New cv.Rangef(1, ocvb.maxZ)}
        Dim histSize() = {input.Height, input.Width}
        cv.Cv2.CalcHist(New cv.Mat() {input}, New Integer() {1, 2}, New cv.Mat, histogram, 2, histSize, ranges)
        histogram = histogram.Threshold(task.thresholdSlider.Value, 255, cv.ThresholdTypes.Binary)

        dst2(mini.rect) = histogram.ConvertScaleAbs(255)
        dst1(mini.rect) = input.ConvertScaleAbs(255)
    End Sub
End Class







Public Class MiniPC_RotateAngle
    Inherits VBparent
    Dim peak As MiniPC_Rotate
    Dim mats As Mat_4to1
    Public plot As Plot_OverTime
    Dim palette As Palette_Basics
    Public Sub New()
        initParent()

        palette = New Palette_Basics
        plot = New Plot_OverTime()
        plot.controlScale = True ' we are controlling the scale...
        plot.maxScale = 300
        plot.minScale = 0

        mats = New Mat_4to1
        peak = New MiniPC_Rotate
        task.desc = "Find a peak value in the side view histograms"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        peak.mini.Run()
        peak.src = peak.mini.dst2

        Dim sz = New cv.Size(peak.mini.rect.Width, peak.mini.rect.Height)
        Static maxValues As New cv.Mat(sz, cv.MatType.CV_32F, 0)
        Static maxIndex As New cv.Mat(sz, cv.MatType.CV_32F, 0)
        Dim r = peak.mini.rect

        Dim prevMaxValues = maxValues.Clone
        peak.Run()
        peak.angleY += 1
        If peak.angleY > 90 Then
            peak.angleY = -90
            maxValues.SetTo(0)
            maxIndex.SetTo(0)
        End If

        cv.Cv2.Max(maxValues, peak.histogram, maxValues)

        Dim mask As New cv.Mat
        cv.Cv2.Absdiff(maxValues, prevMaxValues, mask)
        mask = mask.ConvertScaleAbs(255)
        maxIndex.SetTo(peak.angleY, mask)
        mats.mat(2) = mask

        Dim minVal As Double, maxVal As Double
        Dim minLoc As cv.Point, maxLoc As cv.Point
        peak.histogram.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

        label2 = "Blue is mean*100, red is maxVal/100, green mask count"

        Dim showPlot As Boolean = False
        If showPlot Then
            plot.plotData = New cv.Scalar(peak.histogram.Mean().Item(0) * 100, mask.CountNonZero(), maxVal / 100)
            plot.Run()
            dst2 = plot.dst1
        Else
            dst2 = maxIndex
        End If

        mats.mat(0) = peak.dst1(peak.mini.rect)
        mats.mat(1) = peak.dst2(peak.mini.rect)
        mats.Run()
        dst1 = mats.dst1
    End Sub
End Class
