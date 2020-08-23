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
    Public labelShift As Integer
    Dim fontSize As Single
    Dim radius As Integer
    Dim arcSize As Integer = 100
    Public hFOVangles() As Single = {90, 0, 100, 78, 70, 70, 86}  ' T265 has no point cloud so there is a 0 where it would have been.
    Public vFOVangles() As Single = {60, 0, 55, 65, 69, 67, 60}  ' T265 has no point cloud so there is a 0 where it would have been.
    Public cameraPoint As cv.Point
    Public startangle As Integer

    Public Function CameraLocationBot(ocvb As AlgorithmData, mask As cv.Mat, maxZ As Single) As cv.Mat
        Dim dst As New cv.Mat(mask.Size, cv.MatType.CV_8UC3, 0)

        ' if not a mask, then the image is already colorized.
        If mask.Channels = 1 Then dst2.CopyTo(dst, mask) Else dst = mask.Clone()
        cameraPoint = New cv.Point(dst.Height, dst.Height)
        Dim cameraLocation = New cv.Point(shift + dst.Height / 2, dst.Height - 5)
        dst.Circle(cameraPoint, radius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        For i = maxZ - 1 To 0 Step -1
            Dim ymeter = dst.Height * i / maxZ
            dst.Line(New cv.Point(0, ymeter), New cv.Point(dst.Width, ymeter), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst, CStr(maxZ - i) + "m", New cv.Point(10, ymeter - 10), cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next

        ' draw the arc showing the camera FOV
        Dim startAngle = If(standalone, sliders.trackbar(0).Value, 90 - hFOVangles(ocvb.parms.cameraIndex) / 2)
        Dim x = dst.Height / Math.Tan(startAngle * cv.Cv2.PI / 180)
        Dim xloc = cameraPoint.X + x

        Dim fovRight = New cv.Point(xloc, 0)
        Dim fovLeft = New cv.Point(cameraPoint.X - x, fovRight.Y)

        dst.Ellipse(cameraPoint, New cv.Size(arcSize, arcSize), -startAngle, startAngle, 0, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        dst.Ellipse(cameraPoint, New cv.Size(arcSize, arcSize), 0, 180, 180 + startAngle, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        dst.Line(cameraPoint, fovLeft, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Dim labelLocation = New cv.Point(dst.Width / 2 + labelShift, dst.Height * 15 / 16)
        cv.Cv2.PutText(dst, "hFOV=" + CStr(180 - startAngle * 2) + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst, CStr(startAngle) + " deg.", New cv.Point(cameraPoint.X - 100, cameraPoint.Y - 5), cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst, CStr(startAngle) + " deg.", New cv.Point(cameraPoint.X + 60, cameraPoint.Y - 5), cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        dst.Line(cameraPoint, fovRight, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Return dst
    End Function
    Public Function CameraLocationSide(ocvb As AlgorithmData, ByRef mask As cv.Mat, maxZ As Single) As cv.Mat
        Dim dst As New cv.Mat(mask.Size, cv.MatType.CV_8UC3, 0)

        ' if not a mask, then the image is already colorized.
        If mask.Channels = 1 Then dst2.CopyTo(dst, mask) Else dst = mask.Clone()
        cameraPoint = New cv.Point(shift, src.Height - (src.Width - src.Height) / 2)
        dst.Circle(cameraPoint, radius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        For i = 0 To maxZ
            Dim xmeter = dst.Height * i / maxZ
            dst.Line(New cv.Point(shift + xmeter, 0), New cv.Point(shift + xmeter, dst.Height), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst, CStr(i) + "m", New cv.Point(shift + xmeter + 10, dst.Height - 10), cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next

        ' draw the arc showing the camera FOV
        Dim startAngle = If(standalone, sliders.trackbar(1).Value, vFOVangles(ocvb.parms.cameraIndex))
        Dim y = (dst.Width - shift) / Math.Tan(startAngle * cv.Cv2.PI / 180)
        Dim yloc = cameraPoint.Y - y

        Dim fovTop = New cv.Point(dst.Width, yloc)
        Dim fovBot = New cv.Point(dst.Width, cameraPoint.Y + y)

        dst.Ellipse(cameraPoint, New cv.Size(arcSize, arcSize), -startAngle + 90, startAngle, 0, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        dst.Ellipse(cameraPoint, New cv.Size(arcSize, arcSize), 90, 180, 180 + startAngle, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        dst.Line(cameraPoint, fovTop, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Dim labelLocation = New cv.Point(100, cameraPoint.Y)
        cv.Cv2.PutText(dst, "vFOV=" + CStr(180 - startAngle * 2) + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst, CStr(startAngle) + " deg.", New cv.Point(cameraPoint.X - 80, cameraPoint.Y + 50), cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst, CStr(startAngle) + " deg.", New cv.Point(cameraPoint.X - 80, cameraPoint.Y - 50), cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        dst.Line(cameraPoint, fovBot, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Return dst
    End Function
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        fontSize = 0.9
        If ocvb.parms.resolution = resMed Then fontSize = 0.6
        radius = If(ocvb.parms.resolution = resMed, 5, 12)
        shift = (src.Width - src.Height) / 2
        labelShift = shift

        palette = New Palette_Gradient(ocvb)
        palette.color1 = cv.Scalar.Yellow
        palette.color2 = cv.Scalar.Blue
        palette.frameModulo = 1
        If ocvb.parms.resolution <> resHigh Then arcSize = 50


        If standalone Then
            sliders.Setup(ocvb, caller)
            sliders.setupTrackBar(0, "Top View angle for FOV", 0, 180, 90 - hFOVangles(ocvb.parms.cameraIndex) / 2)
            sliders.setupTrackBar(1, "Side View angle for FOV", 0, 180, vFOVangles(ocvb.parms.cameraIndex))
        End If

        palette.Run(ocvb)
        dst1 = palette.dst1
        dst2 = dst1.Clone
        rect = New cv.Rect(shift, 0, dst1.Height, dst1.Height)
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






'Public Class PointCloud_WallPlane
'    Inherits ocvbClass
'    Dim objects As PointCloud_Distance_TopView
'    Dim lines As lineDetector_FLD_CPP
'    Dim dilate As DilateErode_Basics
'    Public Sub New(ocvb As AlgorithmData)
'        setCaller(ocvb)
'        dilate = New DilateErode_Basics(ocvb)
'        lines = New lineDetector_FLD_CPP(ocvb)
'        objects = New PointCloud_Distance_TopView(ocvb)
'        ocvb.desc = "Use the top down view to detect walls with a line detector algorithm"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        objects.src = src
'        objects.Run(ocvb)
'        dst1 = objects.dst1
'        dst2 = objects.dst2
'        label2 = objects.label2

'        Static checkIMU = findCheckBox("Use IMU gravity vector")
'        If checkIMU?.Checked Then
'            label1 = "Top View: walls in red"
'            dilate.src = dst1
'            dilate.Run(ocvb)

'            lines.src = If(dilate.dst1.Channels = 3, dilate.dst1, dilate.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
'            lines.Run(ocvb)
'            dst1 = lines.dst1
'        Else
'            label1 = "Top View of pointcloud data"
'        End If
'    End Sub
'End Class






'Public Class PointCloud_FloorPlane
'    Inherits ocvbClass
'    Dim objects As PointCloud_Distance_SideView
'    Dim lines As lineDetector_FLD_CPP
'    Dim dilate As DilateErode_Basics
'    Public Sub New(ocvb As AlgorithmData)
'        setCaller(ocvb)
'        dilate = New DilateErode_Basics(ocvb)
'        lines = New lineDetector_FLD_CPP(ocvb)
'        objects = New PointCloud_Distance_SideView(ocvb)
'        ocvb.desc = "Use the side view to detect ceilings and floors with a line detector algorithm"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        objects.src = src
'        objects.Run(ocvb)
'        dst1 = objects.dst1
'        dst2 = objects.dst2
'        label2 = objects.label2

'        Static checkIMU = findCheckBox("Use IMU gravity vector")
'        If checkIMU?.Checked Then
'            label1 = "Top View: floors (and some walls) in red"
'            dilate.src = dst1
'            dilate.Run(ocvb)

'            lines.src = If(dilate.dst1.Channels = 3, dilate.dst1, dilate.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
'            lines.Run(ocvb)
'            dst1 = lines.dst1
'        Else
'            label1 = "Side View of pointcloud data"
'        End If
'    End Sub
'End Class






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
            Static inRangeSlider = findSlider("InRange Max Depth (mm)")
            dst1 = cMats.CameraLocationBot(ocvb, hist.dst1.ConvertScaleAbs(255), inRangeSlider.Value / 1000)
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
            Static inRangeSlider = findSlider("InRange Max Depth (mm)")
            dst1 = cMats.CameraLocationSide(ocvb, hist.dst1.ConvertScaleAbs(255), inRangeSlider.Value / 1000)
        End If
    End Sub
End Class









'Public Class PointCloud_Distance_TopView
'    Inherits ocvbClass
'    Public gVec As PointCloud_Object_TopView
'    Dim showDist As PointCloud_ShowDistance
'    Public Sub New(ocvb As AlgorithmData)
'        setCaller(ocvb)
'        showDist = New PointCloud_ShowDistance(ocvb)
'        gVec = New PointCloud_Object_TopView(ocvb)

'        ocvb.desc = "Show identified clusters of depth data in a top view"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        gVec.Run(ocvb)

'        showDist.src = gVec.dst1
'        showDist.rects = gVec.flood.rects
'        showDist.centroids = gVec.flood.centroids
'        showDist.Run(ocvb)
'        dst1 = showDist.dst1

'        label1 = CStr(CInt(gVec.flood.rects.Count)) + " objects > " + CStr(gVec.flood.minFloodSize) + " pixels"
'    End Sub
'End Class







'Public Class PointCloud_Distance_SideView
'    Inherits ocvbClass
'    Public gVec As PointCloud_Object_SideView
'    Dim showDist As PointCloud_ShowDistance
'    Public Sub New(ocvb As AlgorithmData)
'        setCaller(ocvb)
'        showDist = New PointCloud_ShowDistance(ocvb)
'        gVec = New PointCloud_Object_SideView(ocvb)

'        ocvb.desc = "Show identified clusters of depth data in a side view"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        gVec.Run(ocvb)

'        showDist.src = gVec.dst1
'        showDist.rects = gVec.flood.rects
'        showDist.centroids = gVec.flood.centroids
'        showDist.Run(ocvb)
'        dst1 = showDist.dst1

'        label1 = CStr(CInt(gVec.flood.rects.Count)) + " objects > " + CStr(gVec.flood.minFloodSize) + " pixels"
'    End Sub
'End Class





'Public Class PointCloud_TrimDepth
'    Inherits ocvbClass
'    Public hist As Histogram_2D_TopView
'    Public cMats As PointCloud_Colorize
'    Dim objects As PointCloud_Distance_TopView
'    Public Sub New(ocvb As AlgorithmData)
'        setCaller(ocvb)
'        cMats = New PointCloud_Colorize(ocvb)
'        cMats.shift = 0
'        cMats.Run(ocvb)

'        objects = New PointCloud_Distance_TopView(ocvb)
'        objects.gVec.flood.sliders.trackbar(0).Value = 1 ' we want all possible objects in view.
'        objects.gVec.view.hist.histOpts.sliders.trackbar(0).Value = 20 ' should be substantial object...

'        hist = New Histogram_2D_TopView(ocvb)
'        hist.histOpts.check.Box(0).Checked = False

'        ocvb.desc = "Trim the depth data to identified depth objects"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        hist.Run(ocvb)
'        objects.Run(ocvb)
'        dst1 = objects.dst1
'    End Sub
'End Class







'Public Class PointCloud_ShowDistance
'    Inherits ocvbClass
'    Dim distance As PointCloud_Distance_TopView
'    Public rects As List(Of cv.Rect)
'    Public centroids As List(Of cv.Point2f)
'    Public Sub New(ocvb As AlgorithmData)
'        setCaller(ocvb)
'        If standalone Then distance = New PointCloud_Distance_TopView(ocvb)
'        ocvb.desc = "Given a list of centroids, display the results"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        If standalone Then
'            distance.run(ocvb)
'            dst1 = distance.dst1
'            dst2 = distance.dst2
'        Else
'            src.CopyTo(dst1)
'            If rects.Count = 0 Or centroids.Count = 0 Then Exit Sub
'            Static imuCheckBox = findCheckBox("Use IMU gravity vector")
'            If imuCheckBox.checked Then
'                ocvb.trueText(New TTtext("Uncheck the 'Use IMU gravity vector' to see distances.", 10, 60))
'                ocvb.trueText(New TTtext("Distance may look correct if camera is level but a rotated and projected image distorts distance.", 10, 90))
'            Else
'                Dim fontSize As Single = 1.0
'                If ocvb.parms.resolution = resMed Then fontSize = 0.6
'                Static sliderMaxDepth = findSlider("InRange Max Depth")
'                Dim maxZ = sliderMaxDepth.Value / 1000
'                Dim mmPerPixel = maxZ * 1000 / dst1.Height
'                For i = 0 To centroids.Count - 1
'                    Dim rect = rects(i)
'                    Dim minDistanceFromCamera = (dst1.Height - rect.Y - rect.Height) * mmPerPixel
'                    Dim maxDistanceFromCamera = (dst1.Height - rect.Y) * mmPerPixel
'                    Dim objectWidth = rect.Width * mmPerPixel

'                    Dim center = centroids(i)
'                    dst1.Circle(center, If(ocvb.parms.resolution = resMed, 6, 10), cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
'                    dst1.Circle(center, If(ocvb.parms.resolution = resMed, 3, 5), cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
'                    Dim text = "depth=" + Format(minDistanceFromCamera / 1000, "#0.0") + "-" + Format(maxDistanceFromCamera / 1000, "0.0") +
'                               "m Width=" + Format(objectWidth / 1000, "#0.0") + " m"
'                    dst1.Rectangle(rect, cv.Scalar.Red, 1)
'                    Dim pt = New cv.Point2f(rect.X, rect.Y - 10)
'                    cv.Cv2.PutText(dst1, text, pt, cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
'                Next
'            End If
'        End If
'    End Sub
'End Class








Public Class PointCloud_PixelFormula_TopView
    Inherits ocvbClass
    Public measure As PointCloud_Measured_TopView
    Public viewObjects As New SortedList(Of Integer, viewObject)(New compareAllowIdenticalIntInverted)
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        measure = New PointCloud_Measured_TopView(ocvb)

        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "Distance from camera in mm", 1, 4000, 1500)
        ocvb.desc = "Validate the formula for pixel width as a function of distance"
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        measure.Run(ocvb)
        label1 = measure.label1

        sliders.trackbar(0).Maximum = measure.maxZ * 1000

        dst1 = measure.cMats.CameraLocationBot(ocvb, measure.dst1, measure.maxZ)
        Dim cameraPoint = measure.cMats.cameraPoint
        Dim pixeldistance = src.Height * ((sliders.trackbar(0).Value / 1000) / measure.maxZ)
        Dim FOV = measure.cMats.hFOVangles(ocvb.parms.cameraIndex)

        Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * pixeldistance)
        Dim xpt1 = New cv.Point(cameraPoint.X - lineHalf, src.Height - pixeldistance)
        Dim xpt2 = New cv.Point(cameraPoint.X + lineHalf, src.Height - pixeldistance)

        If standalone Then dst1.Line(xpt1, xpt2, cv.Scalar.Red, 3)

        viewObjects.Clear()
        For i = 0 To measure.pTrack.viewObjects.Count - 1
            Dim r = measure.pTrack.viewObjects.Values(i).rect
            lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * (src.Height - (r.Y + r.Height)))
            If lineHalf = 0 Then Continue For
            pixeldistance = src.Height - r.Y - r.Height
            xpt1 = New cv.Point(cameraPoint.X - lineHalf, src.Height - pixeldistance)
            xpt2 = New cv.Point(cameraPoint.X + lineHalf, src.Height - pixeldistance)
            Dim pt1 = New cv.Point(cameraPoint.X - lineHalf, r.Y + r.Height)
            Dim pt2 = New cv.Point(cameraPoint.X + lineHalf, r.Y + r.Height)
            Dim leftX = Math.Max(Math.Max(xpt1.X, r.X), pt1.X)
            Dim rightX = Math.Min(Math.Min(xpt2.X, r.X + r.Width), pt2.X)
            dst1.Line(New cv.Point(leftX, pt1.Y), New cv.Point(rightX, pt1.Y), cv.Scalar.Yellow, 3)
            Dim addlen As Single
            If cameraPoint.X > r.X And cameraPoint.X < r.X + r.Width Then
                addlen = 0 ' r is unchanged.
            Else
                ' need to add a small amount to the object width in pixels based on the angle of the camera.
                ' additional pixels = r.height * tan(angle to camera of back corner) - first find which corner is nearest the centerline.
                If r.X > cameraPoint.X Then
                    addlen = r.Height * Math.Abs(r.X - cameraPoint.X) / (src.Height - r.Y)
                    dst1.Line(New cv.Point(r.X, r.Y + r.Height), New cv.Point(r.X - addlen, r.Y + r.Height), cv.Scalar.Yellow, 3)
                    r = New cv.Rect(r.X - addlen, r.Y, addlen + Math.Abs(rightX - leftX), r.Height)
                Else
                    addlen = r.Height * (cameraPoint.X - (r.X + r.Width)) / (src.Height - r.Y)
                    dst1.Line(New cv.Point(r.X + r.Width, r.Y + r.Height), New cv.Point(r.X + r.Width + addLen, r.Y + r.Height), cv.Scalar.Yellow, 3)
                    r = New cv.Rect(r.X, r.Y, addlen + Math.Abs(rightX - leftX), r.Height)
                End If
            End If
            Dim vo = measure.pTrack.viewObjects.Values(i)
            vo.rect = r
            viewObjects.Add(vo.centroid.X, vo)
        Next
    End Sub
End Class








Public Class PointCloud_PixelFormula_SideView
    Inherits ocvbClass
    Public measure As PointCloud_Measured_SideView
    Public viewObjects As New SortedList(Of Integer, viewObject)(New compareAllowIdenticalIntInverted)
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        measure = New PointCloud_Measured_SideView(ocvb)

        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "Distance from camera in mm", 1, 4000, 1500)

        ocvb.desc = "Validate the formula for pixel height as a function of distance"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        Dim maxZ = inRangeSlider.Value / 1000

        measure.Run(ocvb)
        label1 = measure.label1

        dst1 = measure.cMats.CameraLocationSide(ocvb, measure.dst1, maxZ)
        Dim cameraPoint = measure.cMats.cameraPoint ' camerapoint is a tricky computation for side views.  Use the original and hopefully only one.
        Static distanceSlider = findSlider("Distance from camera in mm")
        Dim pixeldistance = src.Height * ((distanceSlider.Value / 1000) / maxZ)
        Dim FOV = measure.cMats.vFOVangles(ocvb.parms.cameraIndex)

        Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * pixeldistance)
        Dim xpt1 = New cv.Point(CInt(cameraPoint.X + pixeldistance), CInt(cameraPoint.Y - lineHalf))
        Dim xpt2 = New cv.Point(CInt(cameraPoint.X + pixeldistance), CInt(cameraPoint.Y + lineHalf))

        If standalone Then dst1.Line(xpt1, xpt2, cv.Scalar.Red, 3)

        viewObjects.Clear()
        For i = 0 To measure.pTrack.viewObjects.Count - 1
            Dim r = measure.pTrack.viewObjects.Values(i).rect
            lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * (r.X - cameraPoint.X))
            If lineHalf = 0 Then Continue For
            pixeldistance = r.X - cameraPoint.X
            xpt1 = New cv.Point(CInt(cameraPoint.X + pixeldistance), CInt(cameraPoint.Y - lineHalf))
            xpt2 = New cv.Point(CInt(cameraPoint.X + pixeldistance), CInt(cameraPoint.Y + lineHalf))
            Dim pt1 = New cv.Point(r.X, cameraPoint.Y - lineHalf)
            Dim pt2 = New cv.Point(r.X, cameraPoint.Y + lineHalf)
            Dim topY = Math.Max(Math.Max(xpt1.Y, r.Y), pt1.Y)
            Dim botY = Math.Min(Math.Min(xpt2.Y, r.Y + r.Height), pt2.Y)
            dst1.Line(New cv.Point(r.X, topY), New cv.Point(r.X, botY), cv.Scalar.Yellow, 3)
            Dim addlen As Single
            If cameraPoint.Y > r.Y And cameraPoint.Y < r.Y + r.Height Then
                addlen = 0
            Else
                ' need to add a small amount to the object width in pixels based on the angle of the camera.
                ' additional pixels = r.height * tan(angle to camera of back corner) - first find which corner is nearest the centerline.
                If r.Y > cameraPoint.Y Then
                    addlen = r.Width * (r.Y - cameraPoint.Y) / (r.X + r.Width - cameraPoint.X)
                    dst1.Line(New cv.Point(r.X, r.Y), New cv.Point(r.X, r.Y - addlen), cv.Scalar.Yellow, 3)
                    r = New cv.Rect(r.X, r.Y - addlen, r.Width, Math.Abs(botY - topY) + addlen)

                Else
                    addlen = r.Width * (cameraPoint.Y - r.Y) / (r.X + r.Width - cameraPoint.X)
                    dst1.Line(New cv.Point(r.X, r.Y + r.Height), New cv.Point(r.X, r.Y + r.Height + addlen), cv.Scalar.Yellow, 3)
                    r = New cv.Rect(r.X, r.Y + addlen, r.Width, Math.Abs(botY - topY) + addlen)
                End If
            End If
            Dim vo = measure.pTrack.viewObjects.Values(i)
            vo.rect = r
            viewObjects.Add(vo.centroid.X, vo)
        Next
    End Sub
End Class







'Public Class PointCloud_BackProject_TopView
'    Inherits ocvbClass
'    Dim clipped As PointCloud_PixelClipped_BothViews
'    Dim inrange As Depth_InRange
'    Public Sub New(ocvb As AlgorithmData)
'        setCaller(ocvb)
'        clipped = New PointCloud_PixelClipped_BothViews(ocvb)
'        inrange = New Depth_InRange(ocvb)
'        ocvb.desc = "BackProject the top view into the front looking view"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
'        Dim maxZ = inRangeSlider.value / 1000

'        clipped.Run(ocvb)
'        dst1 = clipped.dst1
'        dst2.SetTo(0)
'        Dim depth32f = getDepth32f(ocvb)
'        Dim mask As New cv.Mat
'        For i = 0 To clipped.topView.pTrack.viewObjects.Count - 1
'            Dim r = clipped.topView.pTrack.viewObjects.Values(i).rect
'            Dim mindepth = 1000 * maxZ * (src.Height - r.Y - r.Height) / src.Height
'            Dim maxDepth = 1000 * maxZ * (src.Height - r.Y) / src.Height
'            Dim topRect = New cv.Rect(r.X, 0, r.Width, src.Height)
'            If topRect.X + topRect.Width >= src.Width Then topRect.Width = src.Width - topRect.X
'            If topRect.Width > 0 Then
'                cv.Cv2.InRange(depth32f(topRect), cv.Scalar.All(mindepth), cv.Scalar.All(maxDepth), mask)
'                dst2(topRect).SetTo(scalarColors(i), mask)
'            End If
'        Next
'    End Sub
'End Class










'Public Class PointCloud_BackProject_SideView
'    Inherits ocvbClass
'    Dim clipped As PointCloud_PixelClipped_BothViews
'    Dim inrange As Depth_InRange
'    Public Sub New(ocvb As AlgorithmData)
'        setCaller(ocvb)
'        clipped = New PointCloud_PixelClipped_BothViews(ocvb)
'        inrange = New Depth_InRange(ocvb)
'        ocvb.desc = "BackProject the side view into the front looking view"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
'        Dim maxZ = inRangeSlider.value / 1000

'        clipped.Run(ocvb)
'        dst1 = clipped.dst1
'        dst2.SetTo(0)
'        Dim depth32f = getDepth32f(ocvb)
'        Dim mask As New cv.Mat
'        For i = 0 To clipped.sideView.pTrack.viewObjects.Count - 1
'            Dim r = clipped.sideView.pTrack.viewObjects.Values(i).rect
'            Dim mindepth = 1000 * maxZ * (src.Height - r.Y - r.Height) / src.Height
'            Dim maxDepth = 1000 * maxZ * (src.Height - r.Y) / src.Height
'            Dim topRect = New cv.Rect(r.X, 0, r.Width, src.Height)
'            If topRect.X + topRect.Width >= src.Width Then topRect.Width = src.Width - topRect.X
'            If topRect.Width > 0 Then
'                cv.Cv2.InRange(depth32f(topRect), cv.Scalar.All(mindepth), cv.Scalar.All(maxDepth), mask)
'                dst2(topRect).SetTo(scalarColors(i), mask)
'            End If
'        Next
'    End Sub
'End Class









Public Class PointCloud_BothViews
    Inherits ocvbClass
    Public topView As PointCloud_Measured_TopView
    Public sideView As PointCloud_Measured_SideView
    Public topPixel As PointCloud_PixelFormula_TopView
    Public sidePixel As PointCloud_PixelFormula_SideView
    Dim levelCheck As IMU_IsCameraLevel
    Public clickOffset As Integer
    Public detailText As String
    Public bpTop As New cv.Mat
    Public bpSide As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        topPixel = New PointCloud_PixelFormula_TopView(ocvb)
        sidePixel = New PointCloud_PixelFormula_SideView(ocvb)

        topView = New PointCloud_Measured_TopView(ocvb)
        sideView = New PointCloud_Measured_SideView(ocvb)

        levelCheck = New IMU_IsCameraLevel(ocvb)

        label1 = "Top View - click for details"
        label2 = "Side View - click for details"
        ocvb.desc = "Find the actual width in pixels for the objects detected in the top view"
    End Sub
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
    Public Sub Run(ocvb As AlgorithmData)
        topPixel.Run(ocvb)
        sidePixel.Run(ocvb)

        topView.Run(ocvb)
        sideView.Run(ocvb)

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

            ocvb.trueText(New TTtext(accMsg1 + vbCrLf + instructions, New cv.Point(10, src.Height - 50)))
            ocvb.trueText(New TTtext(accMsg2 + vbCrLf + instructions, New cv.Point(src.Width + 10, src.Height - 50)))
        End If

        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        Dim maxZ = inRangeSlider.Value / 1000

        dst1 = topView.cMats.CameraLocationBot(ocvb, topView.dst1, maxZ)
        dst2 = sideView.cMats.CameraLocationSide(ocvb, sideView.dst1, maxZ)

        Dim fontsize = If(ocvb.parms.resolution = resHigh, 1.0, 0.6)
        detailText = ""
        Dim textPoint As cv.Point
        Static detailPoint As cv.Point
        If ocvb.mouseClickFlag Then detailPoint = New cv.Point(ocvb.mouseClickPoint.X, ocvb.mouseClickPoint.Y)
        Dim vwTop = topPixel.viewObjects
        Dim vwSide = sidePixel.viewObjects
        If detailPoint.X < src.Width Then
            Dim pixelPerMeter = topPixel.measure.pixelsPerMeter
            If vwTop.Count > 0 And detailPoint.X > 0 Then
                Dim minIndex = setDetails(detailPoint, vwTop)
                Dim vo = vwTop.Values(minIndex)
                Dim r = vo.rect
                detailText = "Clicked: " + Format(maxZ * (src.Height - r.Y - r.Height) / src.Height, "#0.0") + "-" + Format(maxZ * (src.Height - r.Y) / src.Height, "#0.0") + "m & " +
                            CStr(vo.rect.Width) + " pixels wide or " + Format(r.Width / pixelPerMeter, "0.0") + "m"
                textPoint = New cv.Point(r.X + clickOffset, r.Y) ' Add src.width to r.x to make this appear in dst2...
                label1 = detailText
            End If
        Else
            Dim pixelPerMeter = sidePixel.measure.pixelsPerMeter
            Dim cameraX = (src.Width - src.Height) / 2

            If vwSide.Count > 0 And detailPoint.X > 0 Then
                Dim minIndex = setDetails(New cv.Point(detailPoint.X - src.Width, detailPoint.Y), vwSide)
                Dim vo = vwSide.Values(minIndex)
                Dim r = vo.rect
                detailText = "Clicked: " + Format(maxZ * (r.X - cameraX) / src.Height, "#0.0") + "-" + Format(maxZ * (r.X + r.Width - cameraX) / src.Height, "#0.0") + "m & " +
                                CStr(vo.rect.Width) + " pixels wide or " + Format(r.Height / pixelPerMeter, "0.0") + "m"
                textPoint = New cv.Point(r.X + src.Width, r.Y) ' Add src.width to r.x to make this appear in dst2...
                label2 = detailText
            End If
        End If

        bpTop = New cv.Mat(src.Size(), cv.MatType.CV_8UC3, 0)
        Dim depth32f = getDepth32f(ocvb)
        Dim mask As New cv.Mat
        For i = 0 To vwTop.Count - 1
            Dim r = vwTop.Values(i).rect
            Dim mindepth = 1000 * maxZ * (src.Height - r.Y - r.Height) / src.Height
            Dim maxDepth = 1000 * maxZ * (src.Height - r.Y) / src.Height
            Dim topRect = New cv.Rect(r.X, 0, r.Width, src.Height)
            If topRect.X + topRect.Width >= src.Width Then topRect.Width = src.Width - topRect.X
            If topRect.Width > 0 Then
                cv.Cv2.InRange(depth32f(topRect), cv.Scalar.All(mindepth), cv.Scalar.All(maxDepth), mask)
                bpTop(topRect).SetTo(scalarColors(i), mask)
            End If
        Next

        bpSide = New cv.Mat(src.Size(), cv.MatType.CV_8UC3, 0)
        For i = 0 To vwSide.Count - 1
            Dim r = vwSide.Values(i).rect
            Dim mindepth = 1000 * maxZ * (src.Height - r.Y - r.Height) / src.Height
            Dim maxDepth = 1000 * maxZ * (src.Height - r.Y) / src.Height
            Dim sideRect = New cv.Rect(0, r.Y, src.Width, r.Y + r.Height)
            If sideRect.Y + sideRect.Height >= src.Height Then sideRect.Height = src.Height - sideRect.Y
            If sideRect.Width > 0 Then
                cv.Cv2.InRange(depth32f(sideRect), cv.Scalar.All(mindepth), cv.Scalar.All(maxDepth), mask)
                bpSide(sideRect).SetTo(scalarColors(i), mask)
            End If
        Next

        ocvb.trueText(New TTtext(detailText, textPoint))
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
        label1 = "Click any quadrant at left to view it below"
        label2 = "Click any centroid to display details"
        ocvb.desc = "Backproject the selected object"
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        Static quadrant As Integer
        If ocvb.mouseClickFlag Then
            If ocvb.mouseClickPoint.X < src.Width Then
                quadrant = clickQuadrant(ocvb)
                ocvb.mouseClickFlag = False ' they clicked on a quadrant so both can ignore
            Else
                If quadrant = 0 Then
                    ocvb.mouseClickPoint.X -= src.Width
                    both.clickOffset = src.Width
                Else
                    both.clickOffset = 0
                End If
            End If
        End If
        both.Run(ocvb)
        mats.mat(0) = both.dst1
        mats.mat(1) = both.dst2
        mats.mat(2) = both.bpTop
        mats.mat(3) = both.bpSide
        mats.Run(ocvb)
        dst1 = mats.dst1
        dst2 = mats.mat(quadrant)
        label2 = both.detailText
    End Sub
End Class








Public Class PointCloud_Measured_TopView
    Inherits ocvbClass
    Public cMats As PointCloud_Colorize
    Public pTrack As Kalman_PointTracker
    Public flood As FloodFill_Projection
    Public view As PointCloud_TopView
    Public pixelsPerMeter As Single ' pixels per meter at the distance requested.
    Public maxZ As Single
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        flood = New FloodFill_Projection(ocvb)
        flood.sliders.trackbar(0).Value = 100
        cMats = New PointCloud_Colorize(ocvb)
        pTrack = New Kalman_PointTracker(ocvb)
        view = New PointCloud_TopView(ocvb)

        ocvb.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000

        view.src = src
        view.Run(ocvb)
        dst1 = view.dst1
        Static sliderHistThreshold = findSlider("Histogram threshold")
        flood.src = view.hist.histOutput.Threshold(sliderHistThreshold?.Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        flood.Run(ocvb)

        pTrack.queryPoints = New List(Of cv.Point2f)(flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.masks)
        pTrack.Run(ocvb)

        dst1 = cMats.CameraLocationBot(ocvb, pTrack.dst1, maxZ)
        Dim FOV = cMats.hFOVangles(ocvb.parms.cameraIndex)
        Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * src.Height)
        pixelsPerMeter = lineHalf / (Math.Tan(FOV / 2 * 0.0174533) * maxZ)
        label1 = Format(pixelsPerMeter, "0") + " pixels per meter with maxZ at " + Format(maxZ, "0.0") + " meters"
    End Sub
End Class






Public Class PointCloud_Measured_SideView
    Inherits ocvbClass
    Public cMats As PointCloud_Colorize
    Public flood As FloodFill_Projection
    Public view As PointCloud_SideView
    Public pTrack As Kalman_PointTracker
    Public pixelsPerMeter As Single ' pixels per meter at the distance requested.
    Public maxZ As Single
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        cMats = New PointCloud_Colorize(ocvb)
        flood = New FloodFill_Projection(ocvb)
        flood.sliders.trackbar(0).Value = 100
        view = New PointCloud_SideView(ocvb)
        pTrack = New Kalman_PointTracker(ocvb)

        ocvb.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000

        view.src = src
        view.Run(ocvb)
        dst1 = view.dst1
        Static sliderHistThreshold = findSlider("Histogram threshold")
        flood.src = view.hist.histOutput.Threshold(sliderHistThreshold?.Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        flood.Run(ocvb)

        pTrack.queryPoints = New List(Of cv.Point2f)(flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.masks)
        pTrack.Run(ocvb)

        dst1 = cMats.CameraLocationSide(ocvb, pTrack.dst1, maxZ)
        Dim FOV = cMats.vFOVangles(ocvb.parms.cameraIndex)
        Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * src.Height)
        pixelsPerMeter = lineHalf / (Math.Tan(FOV / 2 * 0.0174533) * maxZ)
        label1 = Format(pixelsPerMeter, "0") + " pixels per meter at " + Format(maxZ, "0.0") + " meters"
    End Sub
End Class








'Public Class PointCloud_Object_TopView
'    Inherits ocvbClass
'    Public flood As FloodFill_Projection
'    Public view As PointCloud_TopView
'    Public Sub New(ocvb As AlgorithmData)
'        setCaller(ocvb)

'        flood = New FloodFill_Projection(ocvb)
'        flood.sliders.trackbar(0).Value = 100
'        view = New PointCloud_TopView(ocvb)

'        label1 = "Isolated objects"
'        ocvb.desc = "Threshold the histogram to find the significant 3D objects in the field of view (not floors or ceilings)"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        view.src = src
'        view.Run(ocvb)
'        dst1 = view.dst1
'        Static sliderHistThreshold = findSlider("Histogram threshold")
'        flood.src = view.hist.histOutput.Threshold(sliderHistThreshold?.Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
'        flood.Run(ocvb)
'        dst2 = flood.dst2
'        label2 = flood.label2
'    End Sub
'End Class






'Public Class PointCloud_Object_SideView
'    Inherits ocvbClass
'    Public flood As FloodFill_Projection
'    Public view As PointCloud_SideView
'    Public Sub New(ocvb As AlgorithmData)
'        setCaller(ocvb)

'        flood = New FloodFill_Projection(ocvb)
'        flood.sliders.trackbar(0).Value = 100
'        view = New PointCloud_SideView(ocvb)

'        label1 = "Isolated objects"
'        ocvb.desc = "Floodfill the histogram to find the significant 3D objects in the field of view (not floors or ceilings)"
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        view.src = src
'        view.Run(ocvb)
'        dst1 = view.dst1
'        Static sliderHistThreshold = findSlider("Histogram threshold")
'        flood.src = view.hist.histOutput.Threshold(sliderHistThreshold?.Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
'        flood.Run(ocvb)
'        dst2 = flood.dst2
'        label2 = flood.label2
'    End Sub
'End Class

