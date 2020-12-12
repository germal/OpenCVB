Imports cv = OpenCvSharp
Module Plane_Exports
    Public Sub testCrossProduct()
        Dim p1 = New cv.Point3f(1, 2, 3)
        Dim p2 = New cv.Point3f(2, 1, -2)
        Dim xp = crossProduct(p1, p2)
        Console.WriteLine("x = " + CStr(xp.X) + " y = " + CStr(xp.Y) + " z = " + CStr(xp.Z))
        ' x = -0.6337502 y = 0.724286 z = -0.2716072 when normalized or (-7, 8, -3) when not normalized.
    End Sub
    Public Sub testParallel()
        Dim p1 = New cv.Point3f(5, 2, 3)
        Dim p2 = New cv.Point3f(20, 8, -12)
        Dim xp = crossProduct(p1, p2)
        Console.WriteLine("x = " + CStr(xp.X) + " y = " + CStr(xp.Y) + " z = " + CStr(xp.Z))
    End Sub
    Public Function crossProduct(a As cv.Point3f, b As cv.Point3f) As cv.Point3f
        Dim product As New cv.Point3f

        product.X = a.Y * b.Z - a.Z * b.Y
        product.Y = a.Z * b.X - a.X * b.Z
        product.Z = a.X * b.Y - a.Y * b.X

        If (Single.IsNaN(product.X) Or Single.IsNaN(product.Y) Or Single.IsNaN(product.Z)) Then Return New cv.Point3f(0, 0, 0)
        Dim magnitude = Math.Sqrt(product.X * product.X + product.Y * product.Y + product.Z * product.Z)
        Return New cv.Point3f(product.X / magnitude, product.Y / magnitude, product.Z / magnitude)
    End Function

    Public Function getWorldCoordinates( p As cv.Point3f) As cv.Point3f
        Dim x = (p.X - ocvb.parms.intrinsicsLeft.ppx) / ocvb.parms.intrinsicsLeft.fx
        Dim y = (p.Y - ocvb.parms.intrinsicsLeft.ppy) / ocvb.parms.intrinsicsLeft.fy
        Return New cv.Point3f(x * p.Z, y * p.Z, p.Z)
    End Function
    Public Function getWorldCoordinatesD6( p As cv.Point3f) As cv.Vec6f
        Dim x = CSng((p.X - ocvb.parms.intrinsicsLeft.ppx) / ocvb.parms.intrinsicsLeft.fx)
        Dim y = CSng((p.Y - ocvb.parms.intrinsicsLeft.ppy) / ocvb.parms.intrinsicsLeft.fy)
        Return New cv.Vec6f(x * p.Z, y * p.Z, p.Z, p.X, p.Y, 0)
    End Function
    Public Function buildPlaneEquation(plane As cv.Vec4f, centroid As cv.Point3f) As cv.Vec4f
        Dim magnitude = Math.Sqrt(plane.Item0 * plane.Item0 + plane.Item1 * plane.Item1 + plane.Item2 * plane.Item2)
        Dim normal = New cv.Point3f(plane.Item0 / magnitude, plane.Item1 / magnitude, plane.Item2 / magnitude)
        Return New cv.Vec4f(normal.X, normal.Y, normal.Z, -(normal.X * centroid.X + normal.Y * centroid.Y + normal.Z * centroid.Z))
    End Function
    ' compute plane equation from the worlddepth points.
    ' Based on: http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
    Public Function computePlaneEquation(worldDepth As List(Of cv.Point3f)) As cv.Vec4f
        Dim columnSum As New cv.Scalar
        For i = 0 To worldDepth.Count - 1
            columnSum(0) += worldDepth(i).X
            columnSum(1) += worldDepth(i).Y
            columnSum(2) += worldDepth(i).Z
        Next

        Dim centroid = New cv.Point3f(columnSum(0) / CDbl(worldDepth.Count), columnSum(1) / CDbl(worldDepth.Count), columnSum(2) / CDbl(worldDepth.Count))

        Dim xx As Double, xy As Double, xz As Double, yy As Double, yz As Double, zz As Double
        For i = 0 To worldDepth.Count - 1
            Dim tmp = worldDepth(i) - centroid
            xx += tmp.X * tmp.X
            xy += tmp.X * tmp.Y
            xz += tmp.X * tmp.Z
            yy += tmp.Y * tmp.Y
            yz += tmp.Y * tmp.Z
            zz += tmp.Z * tmp.Z
        Next

        Dim det_x = yy * zz - yz * yz
        Dim det_y = xx * zz - xz * xz
        Dim det_z = xx * yy - xy * xy

        Dim det_max = Math.Max(det_x, det_y)
        det_max = Math.Max(det_max, det_z)

        Dim plane As New cv.Vec4f
        If det_max = det_x Then
            plane.Item0 = 1
            plane.Item1 = (xz * yz - xy * zz) / det_x
            plane.Item2 = (xy * yz - xz * yy) / det_x
        ElseIf det_max = det_y Then
            plane.Item0 = (yz * xz - xy * zz) / det_y
            plane.Item1 = 1
            plane.Item2 = (xy * xz - yz * xx) / det_y
        Else
            plane.Item0 = (yz * xy - xz * yy) / det_z
            plane.Item1 = (xz * xy - yz * xx) / det_z
            plane.Item2 = 1
        End If

        Return buildPlaneEquation(plane, centroid)
    End Function
End Module




Public Class Plane_Detect
    Inherits VBparent
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 64
        gridHeightSlider.Value = 64

        task.desc = "Identify planes in each segment."
        label2 = "Blue, green, and red show different planes"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim depth32f = getDepth32f()
        grid.Run()

        dst2.SetTo(0)
        task.RGBDepth.CopyTo(dst1)

        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim depthROI = depth32f(roi)

            If depthROI.CountNonZero() < roi.Width * roi.Height / 2 Then Exit Sub
            Dim contours As cv.Point()() = Nothing

            Dim depth8u = depthROI.Normalize(0, 255, cv.NormTypes.MinMax)
            Dim depth8uROI As New cv.Mat
            depth8u.ConvertTo(depth8uROI, cv.MatType.CV_8UC1)
            cv.Cv2.FindContours(depth8uROI, contours, Nothing, cv.RetrievalModes.List, cv.ContourApproximationModes.ApproxNone)
            If contours.Length <= 0 Then Exit Sub

            Dim maxCount = 0
            Dim maxIndex = 0
            For j = 0 To contours.Length - 1
                If contours(j).Length > maxCount Then
                    maxCount = contours(j).Length
                    maxIndex = j
                End If
            Next

            If contours(maxIndex).Length < 10 Then Exit Sub

            Dim worldDepth As New List(Of cv.Point3f)
            Dim stepj = 10
            Dim lastj = CInt(Math.Min(contours(maxIndex).Length / stepj - 1, 8 * stepj))
            Dim k = CInt(lastj / 2 - 1)
            For j = 0 To lastj * stepj Step stepj
                Dim p1 = contours(maxIndex)(j)
                Dim p2 = contours(maxIndex)(k + stepj)
                worldDepth.Add(getWorldCoordinates(New cv.Point3f(roi.X + p1.X, roi.Y + p1.Y, depthROI.Get(Of Single)(p1.Y, p1.X))))
                worldDepth.Add(getWorldCoordinates(New cv.Point3f(roi.X + p2.X, roi.Y + p2.Y, depthROI.Get(Of Single)(p2.Y, p2.X))))
                dst1(roi).Line(p1, p2, cv.Scalar.White, 1, cv.LineTypes.AntiAlias) ' show the line connecting the 2 points used to create the normal
            Next

            ' compute plane equation from the worlddepth points.
            Dim plane = computePlaneEquation(worldDepth)
            Dim angle = Math.Acos(Math.Abs(plane.Item2)) / Math.PI * 180

            Dim showNormal As New cv.Mat(roi.Height, roi.Width, cv.MatType.CV_8UC3)
            showNormal.SetTo(New cv.Scalar(Math.Abs(255 * plane.Item0), Math.Abs(255 * plane.Item1), Math.Abs(255 * plane.Item2)))
            cv.Cv2.AddWeighted(src(roi), 0.5, showNormal, 0.5, 0, dst2(roi))
        End Sub)
        Dim mask = grid.gridMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.BitwiseOr(dst1, mask, dst1)
    End Sub
End Class




Public Class Plane_DetectDebug
    Inherits VBparent
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 32
        gridHeightSlider.Value = 32

        task.desc = "Debug code to identify planes in just one segment."
        label2 = "Blue, green, and red show different planes"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim depth32f = getDepth32f()
        grid.Run()

        dst2.SetTo(0)
        task.RGBDepth.CopyTo(dst1)

        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim depthROI = depth32f(roi)

            If depthROI.CountNonZero() < roi.Width * roi.Height / 2 Then Exit Sub
            Dim contours As cv.Point()() = Nothing

            Dim depth8u = depthROI.Normalize(0, 255, cv.NormTypes.MinMax)
            Dim depth8uROI As New cv.Mat
            depth8u.ConvertTo(depth8uROI, cv.MatType.CV_8UC1)
            cv.Cv2.FindContours(depth8uROI, contours, Nothing, cv.RetrievalModes.List, cv.ContourApproximationModes.ApproxNone)
            If contours.Length <= 0 Then Exit Sub

            Dim maxCount = 0
            Dim maxIndex = 0
            For j = 0 To contours.Length - 1
                If contours(j).Length > maxCount Then
                    maxCount = contours(j).Length
                    maxIndex = j
                End If
            Next

            If contours(maxIndex).Length < 10 Then Exit Sub

            Dim worldDepth As New List(Of cv.Point3f)
            Dim stepj = 10
            Dim lastj = CInt(Math.Min(contours(maxIndex).Length / stepj - 1, 8 * stepj))
            Dim k = CInt(lastj / 2 - 1)
            For j = 0 To lastj * stepj Step stepj
                Dim p1 = contours(maxIndex)(j)
                Dim p2 = contours(maxIndex)(k + stepj)
                worldDepth.Add(getWorldCoordinates(New cv.Point3f(roi.X + p1.X, roi.Y + p1.Y, depthROI.Get(Of Single)(p1.Y, p1.X))))
                worldDepth.Add(getWorldCoordinates(New cv.Point3f(roi.X + p2.X, roi.Y + p2.Y, depthROI.Get(Of Single)(p2.Y, p2.X))))
                dst1(roi).Line(p1, p2, cv.Scalar.White, 1, cv.LineTypes.AntiAlias) ' show the line connecting the 2 points used to create the normal
            Next

            ' compute plane equation from the worlddepth points.
            Dim plane = computePlaneEquation(worldDepth)
            Dim angle = Math.Acos(Math.Abs(plane.Item2)) / Math.PI * 180

            Dim showNormal As New cv.Mat(roi.Height, roi.Width, cv.MatType.CV_8UC3)
            showNormal.SetTo(New cv.Scalar(Math.Abs(255 * plane.Item0), Math.Abs(255 * plane.Item1), Math.Abs(255 * plane.Item2)))
            cv.Cv2.AddWeighted(src(roi), 0.5, showNormal, 0.5, 0, dst2(roi))
        End Sub)
        Dim mask = grid.gridMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.BitwiseOr(dst1, mask, dst1)
    End Sub
End Class






Public Class Plane_XYDiff
    Inherits VBparent
    Dim pcValid As Depth_PointCloud_Stable
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        pcValid = New Depth_PointCloud_Stable

        label1 = "Stabilized x delta"
        label2 = "Stabilized y delta"
        task.desc = "Find planes using the pointcloud X and Y differences"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        pcValid.Run()
        Dim mask = pcValid.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        cv.Cv2.ImShow("mask", mask)

        Dim split = cv.Cv2.Split(pcValid.stableCloud)
        Dim xDiff = New cv.Mat(dst2.Size, cv.MatType.CV_32FC1, 0)
        Dim yDiff = New cv.Mat(dst2.Size, cv.MatType.CV_32FC1, 0)

        grid.Run()

        'Parallel.ForEach(Of cv.Rect)(grid.roiList,
        '    Sub(roi)
        '        For i = 0 To grid.roiList.Count - 1
        'Dim roi = grid.roiList(i)
        Dim roi = New cv.Rect(500, 100, 70, 60)
        Dim r1 = New cv.Rect(roi.X + 0, roi.Y + 0, roi.Width - 1, roi.Height - 1)
        Dim r2 = New cv.Rect(roi.X + 1, roi.Y + 1, roi.Width - 1, roi.Height - 1)

        cv.Cv2.Subtract(split(0)(r1), split(0)(r2), xDiff(r1))
        cv.Cv2.Subtract(split(1)(r2), split(1)(r1), yDiff(r1)) ' r2 - r1 to make all values positive...

        Dim xMean = xDiff(r1).Mean(mask(r1)).Item(0)
        Dim yMean = yDiff(r1).Mean(mask(r1)).Item(0)

        Dim tmpx = xDiff.InRange(xMean - 0.001, xMean + 0.001)
        Dim tmpy = yDiff.InRange(yMean - 0.001, yMean + 0.001)

        dst1 = tmpx
        dst2 = tmpy
        'xDiff(r1) *= 128 / xMean
        'xDiff(r1).ConvertTo(dst1(r1), cv.MatType.CV_8UC1)

        'yDiff(r1) *= 128 / yMean
        'yDiff(r1).ConvertTo(dst2(r1), cv.MatType.CV_8UC1)

        'dst1(roi).SetTo(0, mask(roi))
        'dst2(roi).SetTo(0, mask(roi))
        '    Next
        'End Sub)

        'Dim r1 = New cv.Rect(0, 0, dst1.Width - 1, dst1.Height - 1)
        'Dim r2 = New cv.Rect(1, 1, dst1.Width - 1, dst1.Height - 1)

        'cv.Cv2.Subtract(split(0)(r1), split(0)(r2), xDiff(r1))
        'cv.Cv2.Subtract(split(1)(r2), split(1)(r1), yDiff(r1)) ' r2 - r1 to make all values positive...

        'Dim xMean = xDiff.Mean(mask).Item(0)
        'Dim yMean = yDiff.Mean(mask).Item(0)

        'xDiff *= 128 / xMean
        'xDiff.ConvertTo(dst1, cv.MatType.CV_8UC1)

        'yDiff *= 128 / yMean
        'yDiff.ConvertTo(dst2, cv.MatType.CV_8UC1)

        'dst1.SetTo(0, mask)
        'dst2.SetTo(0, mask)
    End Sub
End Class







Public Class Plane_XYHistogram
    Inherits VBparent
    Dim pcValid As Depth_PointCloud_Stable
    Dim hist As Histogram_Basics
    Dim plotHist As Plot_Histogram
    Public Sub New()
        initParent()
        hist = New Histogram_Basics
        plotHist = New Plot_Histogram
        pcValid = New Depth_PointCloud_Stable

        label1 = "Stabilized x delta"
        label2 = "Stabilized y delta"
        task.desc = "Find planes using the pointcloud X and Y differences"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        pcValid.Run()
        Dim mask = pcValid.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(0, 255, cv.ThresholdTypes.BinaryInv)

        Dim split = cv.Cv2.Split(pcValid.stableCloud)
        Dim xDiff = New cv.Mat(dst2.Size, cv.MatType.CV_32FC1, 0)
        Dim yDiff = New cv.Mat(dst2.Size, cv.MatType.CV_32FC1, 0)

        Dim r1 = New cv.Rect(0, 0, dst1.Width - 1, dst1.Height - 1)
        Dim r2 = New cv.Rect(1, 1, dst1.Width - 1, dst1.Height - 1)

        cv.Cv2.Subtract(split(0)(r1), split(0)(r2), xDiff(r1))
        cv.Cv2.Subtract(split(1)(r2), split(1)(r1), yDiff(r1))

        hist.src = xDiff
        hist.Run()


        plotHist.hist = hist.histRaw
        plotHist.Run()
        dst1 = plotHist.dst1
    End Sub
End Class