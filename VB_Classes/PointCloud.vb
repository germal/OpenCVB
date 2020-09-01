Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Module PointCloud
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
    Public Function Project_GravityHist_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Project_GravityHist_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Project_GravityHist_Side(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Project_GravityHist_Run(cPtr As IntPtr, xyzPtr As IntPtr, maxZ As Single, rows As Int32, cols As Int32) As IntPtr
    End Function

    Public Class compareAllowIdenticalIntInverted : Implements IComparer(Of Integer)
        Public Function Compare(ByVal a As Integer, ByVal b As Integer) As Integer Implements IComparer(Of Integer).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Private Function setDetails(detailPoint As cv.Point, viewObjects As SortedList(Of Integer, viewObject)) As Integer
        Dim minIndex As Integer
        Dim minDistance As Single = Single.MaxValue
        For i = 0 To viewObjects.Count - 1
            Dim pt = viewObjects.Values(i).centroid
            Dim distance = Math.Sqrt((detailPoint.X - pt.X) * (detailPoint.X - pt.X) + (detailPoint.Y - pt.Y) * (detailPoint.Y - pt.Y))
            If distance < minDistance Then
                minIndex = i
                minDistance = distance
            End If
        Next
        Return minIndex
    End Function
    Public hFOVangles() As Single = {90, 0, 100, 78, 70, 70, 86}  ' T265 has no point cloud so there is a 0 where it would have been.
    Public vFOVangles() As Single = {60, 0, 55, 65, 69, 67, 60}  ' T265 has no point cloud so there is a 0 where it would have been.
End Module





' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
' https://www.mynteye.com/pages/mynt-eye-d
Public Class PointCloud_Colorize
    Inherits ocvbClass
    Dim palette As Palette_Gradient
    Public rect As cv.Rect
    Public shift As Integer
    Dim centroidRadius As Integer
    Dim arcSize As Integer
    Public startangle As Integer

    Public Function CameraLocationBot(ocvb As AlgorithmData, dst As cv.Mat) As cv.Mat
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000

        dst.Circle(topCameraPoint, centroidRadius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        For i = maxZ - 1 To 0 Step -1
            Dim ymeter = CInt(dst.Height * i / maxZ)
            dst.Line(New cv.Point(0, ymeter), New cv.Point(dst.Width, ymeter), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst, CStr(maxZ - i) + "m", New cv.Point(10, ymeter - 10), cv.HersheyFonts.HersheyComplexSmall, fontsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next

        ' draw the arc showing the camera FOV
        Dim startAngle = If(standalone, sliders.trackbar(0).Value, 90 - hFOVangles(ocvb.parms.cameraIndex) / 2)
        Dim x = dst.Height / Math.Tan(startAngle * cv.Cv2.PI / 180)
        Dim xloc = topCameraPoint.X + x

        Dim fovRight = New cv.Point(xloc, 0)
        Dim fovLeft = New cv.Point(topCameraPoint.X - x, fovRight.Y)

        dst.Ellipse(topCameraPoint, New cv.Size(arcSize, arcSize), -startAngle, startAngle, 0, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        dst.Ellipse(topCameraPoint, New cv.Size(arcSize, arcSize), 0, 180, 180 + startAngle, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        dst.Line(topCameraPoint, fovLeft, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Dim labelLocation = New cv.Point(dst.Width / 2 + shift, dst.Height * 15 / 16)
        cv.Cv2.PutText(dst, "hFOV=" + CStr(180 - startAngle * 2) + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fontsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst, CStr(startAngle) + " deg.", New cv.Point(topCameraPoint.X - 100, topCameraPoint.Y - 5), cv.HersheyFonts.HersheyComplexSmall, fontsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst, CStr(startAngle) + " deg.", New cv.Point(topCameraPoint.X + 60, topCameraPoint.Y - 5), cv.HersheyFonts.HersheyComplexSmall, fontsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        dst.Line(topCameraPoint, fovRight, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Return dst
    End Function
    Public Function CameraLocationSide(ocvb As AlgorithmData, ByRef dst As cv.Mat) As cv.Mat
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000

        Dim shift = (src.Width - src.Height) / 2
        dst.Circle(sideCameraPoint, centroidRadius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        For i = 0 To maxZ
            Dim xmeter = CInt(dst.Height * i / maxZ)
            dst.Line(New cv.Point(shift + xmeter, 0), New cv.Point(shift + xmeter, dst.Height), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst, CStr(i) + "m", New cv.Point(shift + xmeter + 10, dst.Height - 10), cv.HersheyFonts.HersheyComplexSmall, fontsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next

        ' draw the arc showing the camera FOV
        Dim startAngle = If(standalone, sliders.trackbar(1).Value, vFOVangles(ocvb.parms.cameraIndex))
        Dim y = (dst.Width - shift) / Math.Tan(startAngle * cv.Cv2.PI / 180)
        Dim yloc = sideCameraPoint.Y - y

        Dim fovTop = New cv.Point(dst.Width, yloc)
        Dim fovBot = New cv.Point(dst.Width, sideCameraPoint.Y + y)

        dst.Ellipse(sideCameraPoint, New cv.Size(arcSize, arcSize), -startAngle + 90, startAngle, 0, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        dst.Ellipse(sideCameraPoint, New cv.Size(arcSize, arcSize), 90, 180, 180 + startAngle, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        dst.Line(sideCameraPoint, fovTop, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Dim labelLocation = New cv.Point(src.Width * 0.1, sideCameraPoint.Y)
        cv.Cv2.PutText(dst, "vFOV=" + CStr(180 - startAngle * 2) + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fontsize,
                       cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst, CStr(startAngle) + " deg.", New cv.Point(sideCameraPoint.X - 80, sideCameraPoint.Y + 50), cv.HersheyFonts.HersheyComplexSmall, fontsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst, CStr(startAngle) + " deg.", New cv.Point(sideCameraPoint.X - 80, sideCameraPoint.Y - 50), cv.HersheyFonts.HersheyComplexSmall, fontsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        dst.Line(sideCameraPoint, fovBot, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Return dst
    End Function
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        centroidRadius = src.Width / 100

        palette = New Palette_Gradient(ocvb)
        palette.color1 = cv.Scalar.Yellow
        palette.color2 = cv.Scalar.Blue
        palette.frameModulo = 1
        arcSize = src.Width / 10

        If standalone Then
            sliders.Setup(ocvb, caller)
            sliders.setupTrackBar(0, "Top View angle for FOV", 0, 180, 90 - hFOVangles(ocvb.parms.cameraIndex) / 2)
            sliders.setupTrackBar(1, "Side View angle for FOV", 0, 180, vFOVangles(ocvb.parms.cameraIndex))
        End If

        palette.Run(ocvb)
        dst1 = palette.dst1
        dst2 = dst1.Clone
        rect = New cv.Rect((src.Width - src.Height) / 2, 0, dst1.Height, dst1.Height)
        cv.Cv2.Rotate(dst1(rect), dst2(rect), cv.RotateFlags.Rotate90Clockwise)

        label1 = "Colorize mask for top down view"
        label2 = "Colorize mask for side view"
        ocvb.desc = "Create the colorizeMat's used for projections"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
    End Sub
End Class




Public Class PointCloud_Raw_CPP
    Inherits ocvbClass
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 64
        gridHeightSlider.Value = 32

        foreground = New Depth_ManualTrim(ocvb)
        foreground.sliders.trackbar(0).Value = 300  ' fixed distance to keep the images stable.
        foreground.sliders.trackbar(1).Value = 4000 ' fixed distance to keep the images stable.
        label1 = "Top View"
        label2 = "Side View"
        ocvb.desc = "Project the depth data onto a top view and side view."

        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        foreground.Run(ocvb)

        Dim h = src.Height
        Dim w = src.Width
        Dim desiredMin = CSng(foreground.sliders.trackbar(0).Value)
        Dim desiredMax = CSng(foreground.sliders.trackbar(1).Value)
        Dim range = CSng(desiredMax - desiredMin)
        Dim depth32f = getDepth32f(ocvb)
        If depthBytes Is Nothing Then
            ReDim depthBytes(depth32f.Total * depth32f.ElemSize - 1)
        End If

        Marshal.Copy(depth32f.Data, depthBytes, 0, depthBytes.Length)
        Dim handleDepth = GCHandle.Alloc(depthBytes, GCHandleType.Pinned)

        Dim imagePtr = SimpleProjectionRun(cPtr, handleDepth.AddrOfPinnedObject, desiredMin, desiredMax, depth32f.Height, depth32f.Width)

        dst1 = New cv.Mat(depth32f.Rows, depth32f.Cols, cv.MatType.CV_8U, imagePtr).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = New cv.Mat(depth32f.Rows, depth32f.Cols, cv.MatType.CV_8U, SimpleProjectionSide(cPtr)).CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        handleDepth.Free()
        label1 = "Top View (looking down)"
        label2 = "Side View"
    End Sub
    Public Sub Close()
        SimpleProjectionClose(cPtr)
    End Sub
End Class





Public Class PointCloud_Raw
    Inherits ocvbClass
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 64
        gridHeightSlider.Value = 32

        foreground = New Depth_ManualTrim(ocvb)
        foreground.sliders.trackbar(0).Value = 300  ' fixed distance to keep the images stable.
        foreground.sliders.trackbar(1).Value = 4000 ' fixed distance to keep the images stable.
        label1 = "Top View"
        label2 = "Side View"
        ocvb.desc = "Project the depth data onto a top view and side view - using only VB code (too slow.)"

        cPtr = SimpleProjectionOpen()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        foreground.Run(ocvb)

        Dim h = src.Height
        Dim w = src.Width
        Dim desiredMin = CSng(foreground.sliders.trackbar(0).Value)
        Dim desiredMax = CSng(foreground.sliders.trackbar(1).Value)
        Dim range = CSng(desiredMax - desiredMin)
        Dim depth32f = getDepth32f(ocvb)

        ' this VB.Net version is much slower than the optimized C++ version below.
        dst1 = src.EmptyClone.SetTo(cv.Scalar.White)
        dst2 = dst1.Clone()
        Dim black = New cv.Vec3b(0, 0, 0)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
             Sub(roi)
                 For y = roi.Y To roi.Y + roi.Height - 1
                     For x = roi.X To roi.X + roi.Width - 1
                         Dim m = foreground.Mask.Get(Of Byte)(y, x)
                         If m > 0 Then
                             Dim depth = depth32f.Get(Of Single)(y, x)
                             Dim dy = CInt(h * (depth - desiredMin) / range)
                             If dy < h And dy > 0 Then dst1.Set(Of cv.Vec3b)(h - dy, x, black)
                             Dim dx = CInt(w * (depth - desiredMin) / range)
                             If dx < w And dx > 0 Then dst2.Set(Of cv.Vec3b)(y, dx, black)
                         End If
                     Next
                 Next
             End Sub)
        label1 = "Top View (looking down)"
        label2 = "Side View"
    End Sub
    Public Sub Close()
        SimpleProjectionClose(cPtr)
    End Sub
End Class






Public Class PointCloud_TopView
    Inherits ocvbClass
    Public hist As Histogram_2D_TopView
    Public cMats As PointCloud_Colorize
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        cMats = New PointCloud_Colorize(ocvb)
        cMats.shift = 0
        cMats.Run(ocvb)

        hist = New Histogram_2D_TopView(ocvb)
        hist.histOpts.check.Box(0).Checked = False

        ocvb.desc = "Display the histogram with and without adjusting for gravity"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.Run(ocvb)
        Static checkIMU = findCheckBox("Use IMU gravity vector")
        If checkIMU?.Checked Then
            dst1 = hist.dst1
        Else
            dst1 = cMats.CameraLocationBot(ocvb, hist.dst1)
        End If
    End Sub
End Class





Public Class PointCloud_SideView
    Inherits ocvbClass
    Public hist As Histogram_2D_SideView
    Public cMats As PointCloud_Colorize
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        cMats = New PointCloud_Colorize(ocvb)
        cMats.shift = (src.Width - src.Height) / 2
        cMats.Run(ocvb)

        hist = New Histogram_2D_SideView(ocvb)
        hist.histOpts.check.Box(0).Checked = False

        ocvb.desc = "Display the histogram with and without adjusting for gravity"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.Run(ocvb)
        Static checkIMU = findCheckBox("Use IMU gravity vector")
        If checkIMU?.Checked Then
            dst1 = hist.dst1
        Else
            dst1 = cMats.CameraLocationSide(ocvb, hist.dst1)
        End If
    End Sub
End Class







Public Class PointCloud_Objects_TopView
    Inherits ocvbClass
    Public measure As PointCloud_Kalman_TopView
    Public viewObjects As New SortedList(Of Integer, viewObject)(New compareAllowIdenticalIntInverted)
    Dim cmats As PointCloud_Colorize
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        If standalone Then cmats = New PointCloud_Colorize(ocvb)
        measure = New PointCloud_Kalman_TopView(ocvb)

        If standalone Then
            sliders.Setup(ocvb, caller, 1)
            sliders.setupTrackBar(0, "Distance from camera in mm", 1, 4000, 1500)
        End If
        ocvb.desc = "Validate the formula for pixel width as a function of distance"
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        Static showRectanglesCheck = findCheckBox("Draw rectangle for each mask")
        Dim drawLines = showRectanglesCheck.checked
        measure.Run(ocvb)
        dst1 = measure.dst1
        label1 = measure.label1

        Dim FOV = hFOVangles(ocvb.parms.cameraIndex)

        Dim xpt1 As cv.Point, xpt2 As cv.Point
        If standalone Then
            Dim pixeldistance = src.Height * ((sliders.trackbar(0).Value / 1000) / measure.maxZ)
            Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * pixeldistance)
            xpt1 = New cv.Point(topCameraPoint.X - lineHalf, src.Height - pixeldistance)
            xpt2 = New cv.Point(topCameraPoint.X + lineHalf, src.Height - pixeldistance)

            sliders.trackbar(0).Maximum = measure.maxZ * 1000
            If drawLines Then dst1.Line(xpt1, xpt2, cv.Scalar.Red, 3)
        End If

        viewObjects.Clear()
        For i = 0 To measure.pTrack.viewObjects.Count - 1
            Dim r = measure.pTrack.viewObjects.Values(i).rectView
            Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * (src.Height - (r.Y + r.Height)))
            If lineHalf = 0 Then Continue For
            Dim pixeldistance = src.Height - r.Y - r.Height
            xpt1 = New cv.Point(topCameraPoint.X - lineHalf, src.Height - pixeldistance)
            xpt2 = New cv.Point(topCameraPoint.X + lineHalf, src.Height - pixeldistance)
            Dim pt1 = New cv.Point(topCameraPoint.X - lineHalf, r.Y + r.Height)
            Dim pt2 = New cv.Point(topCameraPoint.X + lineHalf, r.Y + r.Height)
            Dim leftX = Math.Max(Math.Max(xpt1.X, r.X), pt1.X)
            Dim rightX = Math.Min(Math.Min(xpt2.X, r.X + r.Width), pt2.X)
            If drawLines Then dst1.Line(New cv.Point(leftX, pt1.Y), New cv.Point(rightX, pt1.Y), cv.Scalar.Yellow, 3)
            Dim addlen As Single
            If topCameraPoint.X > r.X And topCameraPoint.X < r.X + r.Width Then
                addlen = 0
            Else
                ' need to add a small amount to the object width in pixels based on the angle to the camera of the back edge.
                If r.X > topCameraPoint.X Then
                    addlen = r.Height * Math.Abs(r.X - topCameraPoint.X) / (src.Height - r.Y)
                    If drawLines Then dst1.Line(New cv.Point(r.X, r.Y + r.Height), New cv.Point(r.X - addlen, r.Y + r.Height), cv.Scalar.Yellow, 3)
                    leftX -= addlen
                Else
                    addlen = r.Height * (topCameraPoint.X - (r.X + r.Width)) / (src.Height - r.Y)
                    If drawLines Then dst1.Line(New cv.Point(r.X + r.Width, r.Y + r.Height), New cv.Point(r.X + r.Width + addlen, r.Y + r.Height), cv.Scalar.Yellow, 3)
                    If leftX - addlen >= xpt1.X Then leftX -= addlen
                End If
            End If
            Dim vo = measure.pTrack.viewObjects.Values(i)
            Dim newX = (leftX - xpt1.X) * src.Width / (lineHalf * 2)
            Dim newWidth = src.Width * (addlen + rightX - leftX) / (lineHalf * 2)
            vo.rectFront = New cv.Rect(newX, r.Y, newWidth, r.Height)
            viewObjects.Add(vo.rectFront.Width * vo.rectFront.Height, vo)
        Next
        If standalone Then dst1 = cmats.CameraLocationBot(ocvb, dst1)
    End Sub
End Class








Public Class PointCloud_Objects_SideView
    Inherits ocvbClass
    Public measure As PointCloud_Kalman_SideView
    Public viewObjects As New SortedList(Of Integer, viewObject)(New compareAllowIdenticalIntInverted)
    Dim cmats As PointCloud_Colorize
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        If standalone Then cmats = New PointCloud_Colorize(ocvb)
        measure = New PointCloud_Kalman_SideView(ocvb)

        If standalone Then
            sliders.Setup(ocvb, caller, 1)
            sliders.setupTrackBar(0, "Distance from camera in mm", 1, 4000, 1500)
        End If
        ocvb.desc = "Validate the formula for pixel height as a function of distance"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static showRectanglesCheck = findCheckBox("Draw rectangle for each mask")
        Dim drawLines = showRectanglesCheck.checked

        measure.Run(ocvb)
        dst1 = measure.dst1
        label1 = measure.label1

        Dim FOV = vFOVangles(ocvb.parms.cameraIndex)

        Dim xpt1 As cv.Point, xpt2 As cv.Point
        If standalone Then
            Static distanceSlider = findSlider("Distance from camera in mm")
            Dim pixeldistance = src.Height * ((distanceSlider.Value / 1000) / measure.maxZ)
            Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * pixeldistance)
            xpt1 = New cv.Point(CInt(sideCameraPoint.X + pixeldistance), CInt(sideCameraPoint.Y - lineHalf))
            xpt2 = New cv.Point(CInt(sideCameraPoint.X + pixeldistance), CInt(sideCameraPoint.Y + lineHalf))

            sliders.trackbar(0).Maximum = measure.maxZ * 1000
            If drawLines Then dst1.Line(xpt1, xpt2, cv.Scalar.Red, 3)
        End If

        viewObjects.Clear()
        For i = 0 To measure.pTrack.viewObjects.Count - 1
            Dim r = measure.pTrack.viewObjects.Values(i).rectView
            Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * (r.X - sideCameraPoint.X))
            If lineHalf = 0 Then Continue For
            Dim pixeldistance = r.X - sideCameraPoint.X
            xpt1 = New cv.Point(CInt(sideCameraPoint.X + pixeldistance), CInt(sideCameraPoint.Y - lineHalf))
            xpt2 = New cv.Point(CInt(sideCameraPoint.X + pixeldistance), CInt(sideCameraPoint.Y + lineHalf))
            Dim pt1 = New cv.Point(r.X, sideCameraPoint.Y - lineHalf)
            Dim pt2 = New cv.Point(r.X, sideCameraPoint.Y + lineHalf)
            Dim topY = Math.Max(Math.Max(xpt1.Y, r.Y), pt1.Y)
            Dim botY = Math.Min(Math.Min(xpt2.Y, r.Y + r.Height), pt2.Y)
            If drawLines Then dst1.Line(New cv.Point(r.X, topY), New cv.Point(r.X, botY), cv.Scalar.Yellow, 3)
            Dim addlen As Single
            If sideCameraPoint.Y > r.Y And sideCameraPoint.Y < r.Y + r.Height Then
                addlen = 0
            Else
                ' need to add a small amount to the object width in pixels based on the angle to the camera of the back edge
                If r.Y > sideCameraPoint.Y Then
                    addlen = r.Width * (r.Y - sideCameraPoint.Y) / (r.X + r.Width - sideCameraPoint.X)
                    If drawLines Then dst1.Line(New cv.Point(r.X, r.Y), New cv.Point(r.X, r.Y - addlen), cv.Scalar.Yellow, 3)
                    r = New cv.Rect(r.X, r.Y - addlen, r.Width, botY - topY - addlen)
                    If botY - addlen >= xpt2.Y Then botY -= addlen
                Else
                    addlen = r.Width * (sideCameraPoint.Y - r.Y) / (r.X + r.Width - sideCameraPoint.X)
                    If drawLines Then dst1.Line(New cv.Point(r.X, r.Y + r.Height), New cv.Point(r.X, r.Y + r.Height + addlen), cv.Scalar.Yellow, 3)
                    r = New cv.Rect(r.X, r.Y + addlen, r.Width, botY - topY + addlen)
                    topY += addlen
                End If
            End If
            Dim vo = measure.pTrack.viewObjects.Values(i)
            Dim newY = (topY - xpt1.Y) * src.Height / (lineHalf * 2)
            Dim newHeight = src.Height * (addlen + botY - topY) / (lineHalf * 2)
            vo.rectFront = New cv.Rect(r.X, newY, r.Width, newHeight)
            viewObjects.Add(vo.centroid.X, vo)
        Next
        If standalone Then dst1 = cmats.CameraLocationSide(ocvb, dst1)
    End Sub
End Class








Public Class PointCloud_Kalman_TopView
    Inherits ocvbClass
    Public pTrack As Kalman_PointTracker
    Public flood As Floodfill_Identifiers
    Public view As PointCloud_TopView
    Public pixelsPerMeter As Single ' pixels per meter at the distance requested.
    Dim cmats As PointCloud_Colorize
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        If standalone Then cmats = New PointCloud_Colorize(ocvb)
        flood = New Floodfill_Identifiers(ocvb)
        flood.basics.sliders.trackbar(0).Value = 100
        pTrack = New Kalman_PointTracker(ocvb)
        view = New PointCloud_TopView(ocvb)

        ocvb.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000

        view.src = src
        view.Run(ocvb)

        Static sliderHistThreshold = findSlider("Histogram threshold")
        flood.src = view.hist.histOutput.Threshold(sliderHistThreshold?.Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        flood.Run(ocvb)

        pTrack.src = flood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        pTrack.queryPoints = New List(Of cv.Point2f)(flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.masks)
        pTrack.Run(ocvb)
        dst1 = pTrack.dst1

        If standalone Then
            Static checkIMU = findCheckBox("Use IMU gravity vector")
            If checkIMU?.Checked = False Then dst1 = cmats.CameraLocationBot(ocvb, dst1)
            Dim FOV = hFOVangles(ocvb.parms.cameraIndex)
            Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * src.Height)
            pixelsPerMeter = lineHalf / (Math.Tan(FOV / 2 * 0.0174533) * maxZ)
            label1 = Format(pixelsPerMeter, "0") + " pixels per meter with maxZ at " + Format(maxZ, "0.0") + " meters"
        End If
    End Sub
End Class






Public Class PointCloud_Kalman_SideView
    Inherits ocvbClass
    Public flood As Floodfill_Identifiers
    Public view As PointCloud_SideView
    Public pTrack As Kalman_PointTracker
    Public pixelsPerMeter As Single ' pixels per meter at the distance requested.
    Dim cmats As PointCloud_Colorize
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        If standalone Then cmats = New PointCloud_Colorize(ocvb)
        flood = New Floodfill_Identifiers(ocvb)
        flood.basics.sliders.trackbar(0).Value = 100
        view = New PointCloud_SideView(ocvb)
        pTrack = New Kalman_PointTracker(ocvb)

        ocvb.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000

        view.src = src
        view.Run(ocvb)

        Static sliderHistThreshold = findSlider("Histogram threshold")
        flood.src = view.hist.histOutput.Threshold(sliderHistThreshold?.Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        flood.Run(ocvb)

        pTrack.src = flood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        pTrack.queryPoints = New List(Of cv.Point2f)(flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.masks)
        pTrack.Run(ocvb)
        dst1 = pTrack.dst1

        If standalone Then
            Static checkIMU = findCheckBox("Use IMU gravity vector")
            If checkIMU?.Checked = False Then dst1 = cmats.CameraLocationSide(ocvb, dst1)
            Dim FOV = vFOVangles(ocvb.parms.cameraIndex)
            Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * src.Height)
            pixelsPerMeter = lineHalf / (Math.Tan(FOV / 2 * 0.0174533) * maxZ)
            label1 = Format(pixelsPerMeter, "0") + " pixels per meter at " + Format(maxZ, "0.0") + " meters"
        End If
    End Sub
End Class










Public Class PointCloud_BackProject
    Inherits ocvbClass
    Dim both As PointCloud_BothViews
    Dim mats As Mat_4to1
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        both = New PointCloud_BothViews(ocvb)
        mats = New Mat_4to1(ocvb)
        label1 = "Click any quadrant below to enlarge it"
        label2 = "Click any centroid to display details"
        ocvb.desc = "Backproject the selected object"
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.mouseClickFlag Then
            ' lower left image is the mat_4to1
            If ocvb.mousePicTag = 2 Then
                If ocvb.mouseClickFlag Then setQuadrant(ocvb)
                ocvb.mouseClickFlag = False ' absorb the mouse click here only
            End If
        End If
        both.Run(ocvb)

        mats.mat(0) = both.dst1
        mats.mat(1) = both.dst2
        mats.mat(2) = both.backMat
        mats.mat(3) = both.backMatMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        mats.Run(ocvb)
        dst1 = mats.dst1
        dst2 = mats.mat(ocvb.quadrantIndex)
        label2 = both.detailText
    End Sub
End Class








Public Class PointCloud_BothViews
    Inherits ocvbClass
    Public topPixel As PointCloud_Objects_TopView
    Public sidePixel As PointCloud_Objects_SideView
    Dim levelCheck As IMU_IsCameraLevel
    Public detailText As String
    Public backMat As New cv.Mat
    Public backMatMask As New cv.Mat
    Public vwTop As New SortedList(Of Integer, viewObject)(New compareAllowIdenticalIntInverted)
    Public vwSide As New SortedList(Of Integer, viewObject)(New compareAllowIdenticalIntInverted)
    Dim cmats As PointCloud_Colorize
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        cmats = New PointCloud_Colorize(ocvb)
        topPixel = New PointCloud_Objects_TopView(ocvb)
        sidePixel = New PointCloud_Objects_SideView(ocvb)

        levelCheck = New IMU_IsCameraLevel(ocvb)

        backMat = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC3)
        backMatMask = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)

        ocvb.desc = "Find the actual width in pixels for the objects detected in the top view"
    End Sub
    Private Function setDetails(detailPoint As cv.Point, vw As SortedList(Of Integer, viewObject)) As Integer
        Dim minIndex As Integer = 0
        Dim minDistance As Single = Single.MaxValue
        For i = 0 To vw.Count - 1
            Dim pt = vw.Values(i).centroid
            Dim distance = Math.Sqrt((detailPoint.X - pt.X) * (detailPoint.X - pt.X) + (detailPoint.Y - pt.Y) * (detailPoint.Y - pt.Y))
            If distance < minDistance Then
                minIndex = i
                minDistance = distance
            End If
        Next
        Return minIndex
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        Static showRectanglesCheck = findCheckBox("Draw rectangle for each mask")
        Dim showDetails = showRectanglesCheck.checked

        Dim depth32f = getDepth32f(ocvb)

        topPixel.Run(ocvb)
        sidePixel.Run(ocvb)
        Dim maxZ = topPixel.measure.maxZ

        If standalone Then
            Dim instructions = "Click any centroid to get details"
            Static useIMUcheckbox = findCheckBox("Use IMU gravity vector")
            If useIMUcheckbox Is Nothing Then useIMUcheckbox = findCheckBox("Use IMU gravity vector")
            Dim accMsg1 = "TopView - distances are accurate"
            Dim accMsg2 = "SideView - distances are accurate"
            If useIMUcheckbox.checked Then
                levelCheck.Run(ocvb)
                If levelCheck.cameraLevel Then
                    accMsg1 = "TopView - distances are APPROXIMATE - level cam"
                    accMsg2 = "SideView - distances are APPROXIMATE - level cam"
                Else
                    accMsg1 = "TopView - distances are NOT accurate"
                    accMsg2 = "SideView - distances are NOT accurate"
                End If
            End If

            Dim pad = src.Width / 15
            ocvb.trueText(New TTtext(accMsg1 + vbCrLf + instructions, New cv.Point(10, src.Height - pad)))
            ocvb.trueText(New TTtext(accMsg2 + vbCrLf + instructions, New cv.Point(10, src.Height - pad), 3))
        End If

        dst1 = topPixel.dst1
        dst2 = sidePixel.dst1

        Static minDepth As Single, maxDepth As Single
        Dim activeView = ocvb.quadrantIndex
        vwTop = topPixel.viewObjects
        vwSide = sidePixel.viewObjects
        Dim roi = New cv.Rect(0, 0, dst1.Width, dst1.Height)
        Dim minIndex As Integer
        Dim detailPoint As cv.Point
        Dim vw As New SortedList(Of Integer, viewObject)
        Dim topActive = If(standalone, True, (activeView = QUAD0 Or activeView = QUAD2))
        Dim sideActive = If(standalone, True, (activeView = QUAD1 Or activeView = QUAD3))

        If vwTop.Count And topActive Then
            minIndex = setDetails(ocvb.mouseClickPoint, vwTop)
            Dim rView = vwTop.Values(minIndex).rectView
            detailPoint = New cv.Point(rView.X, rView.Y)
            Dim rFront = vwTop.Values(minIndex).rectFront
            Dim pixelPerMeter = topPixel.measure.pixelsPerMeter

            If detailPoint.X = 0 And detailPoint.Y = 0 Then detailPoint = New cv.Point(CInt(rView.X), CInt(rView.Y))
            minDepth = maxZ * (topCameraPoint.Y - rView.Y - rView.Height) / src.Height
            maxDepth = maxZ * (topCameraPoint.Y - rView.Y) / src.Height
            detailText = Format(minDepth, "#0.0") + "-" + Format(maxDepth, "#0.0") + "m & " +
                     CStr(rView.Width) + " pixels wide or " + Format(rView.Width / pixelPerMeter, "0.0") + "m"
            roi = New cv.Rect(rFront.X, 0, rFront.Width, src.Height)
            vw = vwTop
            If showDetails Then
                ocvb.trueText(New TTtext(detailText, detailPoint, If(standalone, 2, 3)))
                If standalone Then label1 = "Clicked: " + detailText Else label2 = "Clicked: " + detailText
            End If
        End If

        If vwSide.Count And sideActive Then
            minIndex = setDetails(ocvb.mouseClickPoint, vwSide)
            Dim rView = vwSide.Values(minIndex).rectView
            detailPoint = New cv.Point(rView.X, rView.Y)
            Dim rFront = vwSide.Values(minIndex).rectFront
            Dim pixelPerMeter = sidePixel.measure.pixelsPerMeter

            minDepth = maxZ * (rView.X - sideCameraPoint.X) / src.Height
            maxDepth = maxZ * (rView.X + rView.Width - sideCameraPoint.X) / src.Height
            detailText = Format(minDepth, "#0.0") + "-" + Format(maxDepth, "#0.0") + "m & " +
                             CStr(rView.Width) + " pixels wide or " + Format(rView.Height / pixelPerMeter, "0.0") + "m"
            roi = New cv.Rect(0, rFront.Y, src.Width, rFront.Y + rFront.Height)
            vw = vwSide
            If showDetails Then
                ocvb.trueText(New TTtext(detailText, detailPoint, 3))
                label2 = "Clicked: " + detailText
            End If
        End If

        backMat = ocvb.color
        If vw.Count > 0 Then
            If roi.X + roi.Width > src.Width Then roi.Width = src.Width - roi.X
            If roi.Y + roi.Height > src.Height Then roi.Height = src.Height - roi.Y
            If roi.Width > 0 And roi.Height > 0 Then
                backMatMask.SetTo(0)
                cv.Cv2.InRange(depth32f(roi), cv.Scalar.All(minDepth * 1000), cv.Scalar.All(maxDepth * 1000), backMatMask(roi))
                backMat(roi).SetTo(vw.Values(minIndex).color, backMatMask(roi))
            End If
        End If

        Static checkIMU = findCheckBox("Use IMU gravity vector")
        If checkIMU?.Checked = False Then
            dst1 = cmats.CameraLocationBot(ocvb, dst1)
            dst2 = cmats.CameraLocationSide(ocvb, dst2)
        End If
    End Sub
End Class






Public Class PointCloud_WallsFloorsCeilings
    Inherits ocvbClass
    Dim both As PointCloud_BothViews
    Dim lDetect As lineDetector_FLD
    Dim dilate As DilateErode_Basics
    Public walls As List(Of cv.Vec4f)
    Public floorsCeilings As List(Of cv.Vec4f)
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        dilate = New DilateErode_Basics(ocvb)
        lDetect = New lineDetector_FLD(ocvb)
        both = New PointCloud_BothViews(ocvb)
        label1 = "Top View: wall candidates in red"
        label2 = "Side View: floors/ceiling candidates in red"
        ocvb.desc = "Use the top down view to detect walls with a line detector algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static showRectanglesCheck = findCheckBox("Draw rectangle for each mask")
        If ocvb.frameCount = 0 Then showRectanglesCheck.checked = False
        Static checkIMU = findCheckBox("Use IMU gravity vector")
        If ocvb.frameCount = 0 Then checkIMU.checked = True
        both.src = src
        both.Run(ocvb)
        dst1 = both.dst1
        dst2 = both.dst2

        For i = 0 To 1
            Dim dst = Choose(i + 1, dst1, dst2)
            dilate.src = dst
            dilate.Run(ocvb)
            lDetect.src = If(dilate.dst1.Channels = 3, dilate.dst1, dilate.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
            lDetect.Run(ocvb)
            If i = 0 Then walls = lDetect.lines Else floorsCeilings = lDetect.lines
            Dim vec4 = If(i = 0, walls, floorsCeilings)
            For j = 0 To vec4.Count - 1 Step 4
                Dim v = vec4(j)
                Dim pt1 = New cv.Point(v(0), v(1))
                dst.line(New cv.Point(v(0), v(1)), New cv.Point(v(2), v(3)), cv.Scalar.Red, 3, cv.LineTypes.AntiAlias)
            Next
        Next
    End Sub
End Class

