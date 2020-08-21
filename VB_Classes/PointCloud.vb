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






Public Class PointCloud_WallPlane
    Inherits ocvbClass
    Dim objects As PointCloud_Distance_TopView
    Dim lines As lineDetector_FLD_CPP
    Dim dilate As DilateErode_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        dilate = New DilateErode_Basics(ocvb)
        lines = New lineDetector_FLD_CPP(ocvb)
        objects = New PointCloud_Distance_TopView(ocvb)
        ocvb.desc = "Use the top down view to detect walls with a line detector algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        objects.src = src
        objects.Run(ocvb)
        dst1 = objects.dst1
        dst2 = objects.dst2
        label2 = objects.label2

        Static checkIMU = findCheckBox("Use IMU gravity vector")
        If checkIMU?.Checked Then
            label1 = "Top View: walls in red"
            dilate.src = dst1
            dilate.Run(ocvb)

            lines.src = If(dilate.dst1.Channels = 3, dilate.dst1, dilate.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
            lines.Run(ocvb)
            dst1 = lines.dst1
        Else
            label1 = "Top View of pointcloud data"
        End If
    End Sub
End Class






Public Class PointCloud_FloorPlane
    Inherits ocvbClass
    Dim objects As PointCloud_Distance_SideView
    Dim lines As lineDetector_FLD_CPP
    Dim dilate As DilateErode_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        dilate = New DilateErode_Basics(ocvb)
        lines = New lineDetector_FLD_CPP(ocvb)
        objects = New PointCloud_Distance_SideView(ocvb)
        ocvb.desc = "Use the side view to detect ceilings and floors with a line detector algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        objects.src = src
        objects.Run(ocvb)
        dst1 = objects.dst1
        dst2 = objects.dst2
        label2 = objects.label2

        Static checkIMU = findCheckBox("Use IMU gravity vector")
        If checkIMU?.Checked Then
            label1 = "Top View: floors (and some walls) in red"
            dilate.src = dst1
            dilate.Run(ocvb)

            lines.src = If(dilate.dst1.Channels = 3, dilate.dst1, dilate.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
            lines.Run(ocvb)
            dst1 = lines.dst1
        Else
            label1 = "Side View of pointcloud data"
        End If
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








Public Class PointCloud_Object_TopView
    Inherits ocvbClass
    Public flood As FloodFill_Projection
    Public view As PointCloud_TopView
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        flood = New FloodFill_Projection(ocvb)
        flood.sliders.trackbar(0).Value = 100
        view = New PointCloud_TopView(ocvb)

        label1 = "Isolated objects"
        ocvb.desc = "Threshold the histogram to find the significant 3D objects in the field of view (not floors or ceilings)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        view.src = src
        view.Run(ocvb)
        dst1 = view.dst1
        Static sliderHistThreshold = findSlider("Histogram threshold")
        flood.src = view.hist.histOutput.Threshold(sliderHistThreshold?.Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        flood.Run(ocvb)
        dst2 = flood.dst2
        label2 = flood.label2
    End Sub
End Class






Public Class PointCloud_Object_SideView
    Inherits ocvbClass
    Public flood As FloodFill_Projection
    Public view As PointCloud_SideView
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        flood = New FloodFill_Projection(ocvb)
        flood.sliders.trackbar(0).Value = 100
        view = New PointCloud_SideView(ocvb)

        label1 = "Isolated objects"
        ocvb.desc = "Floodfill the histogram to find the significant 3D objects in the field of view (not floors or ceilings)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        view.src = src
        view.Run(ocvb)
        dst1 = view.dst1
        Static sliderHistThreshold = findSlider("Histogram threshold")
        flood.src = view.hist.histOutput.Threshold(sliderHistThreshold?.Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        flood.Run(ocvb)
        dst2 = flood.dst2
        label2 = flood.label2
    End Sub
End Class








Public Class PointCloud_Distance_TopView
    Inherits ocvbClass
    Public gVec As PointCloud_Object_TopView
    Dim showDist As PointCloud_ShowDistance
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        showDist = New PointCloud_ShowDistance(ocvb)
        gVec = New PointCloud_Object_TopView(ocvb)

        ocvb.desc = "Show identified clusters of depth data in a top view"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        gVec.Run(ocvb)

        showDist.src = gVec.dst1
        showDist.rects = gVec.flood.rects
        showDist.centroids = gVec.flood.centroids
        showDist.Run(ocvb)
        dst1 = showDist.dst1

        label1 = CStr(CInt(gVec.flood.rects.Count)) + " objects > " + CStr(gVec.flood.minFloodSize) + " pixels"
    End Sub
End Class







Public Class PointCloud_Distance_SideView
    Inherits ocvbClass
    Public gVec As PointCloud_Object_SideView
    Dim showDist As PointCloud_ShowDistance
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        showDist = New PointCloud_ShowDistance(ocvb)
        gVec = New PointCloud_Object_SideView(ocvb)

        ocvb.desc = "Show identified clusters of depth data in a side view"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        gVec.Run(ocvb)

        showDist.src = gVec.dst1
        showDist.rects = gVec.flood.rects
        showDist.centroids = gVec.flood.centroids
        showDist.Run(ocvb)
        dst1 = showDist.dst1

        label1 = CStr(CInt(gVec.flood.rects.Count)) + " objects > " + CStr(gVec.flood.minFloodSize) + " pixels"
    End Sub
End Class





Public Class PointCloud_TrimDepth
    Inherits ocvbClass
    Public hist As Histogram_2D_TopView
    Public cMats As PointCloud_Colorize
    Dim objects As PointCloud_Distance_TopView
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        cMats = New PointCloud_Colorize(ocvb)
        cMats.shift = 0
        cMats.Run(ocvb)

        objects = New PointCloud_Distance_TopView(ocvb)
        objects.gVec.flood.sliders.trackbar(0).Value = 1 ' we want all possible objects in view.
        objects.gVec.view.hist.histOpts.sliders.trackbar(0).Value = 20 ' should be substantial object...

        hist = New Histogram_2D_TopView(ocvb)
        hist.histOpts.check.Box(0).Checked = False

        ocvb.desc = "Trim the depth data to identified depth objects"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.Run(ocvb)
        objects.Run(ocvb)
        dst1 = objects.dst1
    End Sub
End Class







Public Class PointCloud_ShowDistance
    Inherits ocvbClass
    Dim distance As PointCloud_Distance_TopView
    Public rects As List(Of cv.Rect)
    Public centroids As List(Of cv.Point2f)
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        If standalone Then distance = New PointCloud_Distance_TopView(ocvb)
        ocvb.desc = "Given a list of centroids, display the results"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            distance.run(ocvb)
            dst1 = distance.dst1
            dst2 = distance.dst2
        Else
            src.CopyTo(dst1)
            If rects.Count = 0 Or centroids.Count = 0 Then Exit Sub
            Static imuCheckBox = findCheckBox("Use IMU gravity vector")
            If imuCheckBox.checked Then
                ocvb.trueText(New TTtext("Uncheck the 'Use IMU gravity vector' to see distances.", 10, 60))
                ocvb.trueText(New TTtext("Distance may look correct if camera is level but a rotated and projected image distorts distance.", 10, 90))
            Else
                Dim fontSize As Single = 1.0
                If ocvb.parms.resolution = resMed Then fontSize = 0.6
                Static sliderMaxDepth = findSlider("InRange Max Depth")
                Dim maxZ = sliderMaxDepth.Value / 1000
                Dim mmPerPixel = maxZ * 1000 / dst1.Height
                For i = 0 To centroids.Count - 1
                    Dim rect = rects(i)
                    Dim minDistanceFromCamera = (dst1.Height - rect.Y - rect.Height) * mmPerPixel
                    Dim maxDistanceFromCamera = (dst1.Height - rect.Y) * mmPerPixel
                    Dim objectWidth = rect.Width * mmPerPixel

                    Dim center = centroids(i)
                    dst1.Circle(center, If(ocvb.parms.resolution = resMed, 6, 10), cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                    dst1.Circle(center, If(ocvb.parms.resolution = resMed, 3, 5), cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
                    Dim text = "depth=" + Format(minDistanceFromCamera / 1000, "#0.0") + "-" + Format(maxDistanceFromCamera / 1000, "0.0") +
                               "m Width=" + Format(objectWidth / 1000, "#0.0") + " m"
                    dst1.Rectangle(rect, cv.Scalar.Red, 1)
                    Dim pt = New cv.Point2f(rect.X, rect.Y - 10)
                    cv.Cv2.PutText(dst1, text, pt, cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                Next
            End If
        End If
    End Sub
End Class









Public Class PointCloud_PixelClipped_BothViews
    Inherits ocvbClass
    Public topView As PointCloud_Measured_TopView
    Public sideView As PointCloud_Measured_SideView
    Public sidePixel As PointCloud_PixelFormula_SideView
    Public topPixel As PointCloud_PixelFormula_TopView
    Dim levelCheck As IMU_IsCameraLevel
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        sidePixel = New PointCloud_PixelFormula_SideView(ocvb)
        topPixel = New PointCloud_PixelFormula_TopView(ocvb)
        topView = New PointCloud_Measured_TopView(ocvb)
        sideView = New PointCloud_Measured_SideView(ocvb)
        levelCheck = New IMU_IsCameraLevel(ocvb)

        ocvb.desc = "Find the actual width in pixels for the objects detected in the top view"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        sidePixel.run(ocvb)
        topPixel.run(ocvb)
        topView.Run(ocvb)
        sideView.Run(ocvb)
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        Dim maxZ = inRangeSlider.Value / 1000

        Static useIMUcheckbox = findCheckBox("Use IMU gravity vector")
        If useIMUcheckbox Is Nothing Then useIMUcheckbox = findCheckBox("Use IMU gravity vector")
        label1 = "TopView - distances are accurate"
        label2 = "SideView - distances are accurate"
        If useIMUcheckbox.checked Then
            levelCheck.Run(ocvb)
            If levelCheck.cameraLevel Then
                label1 = "TopView - distances are APPROXIMATE - level cam"
                label2 = "SideView - distances are APPROXIMATE - level cam"
            Else
                label1 = "TopView - distances are NOT accurate"
                label2 = "SideView - distances are NOT accurate"
            End If
        End If
        dst1 = topView.cMats.CameraLocationBot(ocvb, topView.dst1, maxZ)
        For Each rect In topView.pTrack.matchedRects
            Dim p1 = New cv.Point(0, rect.Y)
            Dim p2 = New cv.Point(src.Width, rect.Y)
            Dim clipped = cv.Cv2.ClipLine(rect, p1, p2) ' Returns false when the line and the rectangle don't intersect.
            dst1.Line(p1, p2, If(clipped, cv.Scalar.Red, cv.Scalar.Black), 2)
        Next

        dst2 = sideView.cMats.CameraLocationSide(ocvb, sideView.dst1, maxZ)
        For Each rect In sideView.pTrack.matchedRects
            Dim p1 = New cv.Point(rect.X + rect.Width - 1, 0)
            Dim p2 = New cv.Point(rect.X + rect.Width - 1, src.Height)
            Dim clipped = cv.Cv2.ClipLine(rect, p1, p2) ' Returns false when the line and the rectangle don't intersect.
            dst2.Line(p1, p2, If(clipped, cv.Scalar.Red, cv.Scalar.Black), 2)
        Next
    End Sub
End Class







Public Class PointCloud_BackProject
    Inherits ocvbClass
    Dim clipped As PointCloud_PixelClipped_BothViews
    Dim mats As Mat_4to1
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        clipped = New PointCloud_PixelClipped_BothViews(ocvb)
        mats = New Mat_4to1(ocvb)
        label1 = "Top/Side views with corresponding backprojection"
        label2 = "Click any quadrant at left to view it below"
        ocvb.desc = "Backproject the selected object"
    End Sub
    Private Function setDetails(detailPoint As cv.Point, centroids As List(Of cv.Point2f)) As Integer
        Dim minIndex As Integer
        Dim minDistance As Single = Single.MaxValue
        For i = 0 To centroids.Count - 1
            Dim pt = centroids(i)
            Dim distance = Math.Sqrt((detailPoint.X - pt.X) * (detailPoint.X - pt.X) + (detailPoint.Y - pt.Y) * (detailPoint.Y - pt.Y))
            If distance < minDistance Then
                minIndex = i
                minDistance = distance
            End If
        Next
        Return minIndex
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        clipped.Run(ocvb)
        mats.mat(0) = clipped.dst1
        mats.mat(1) = clipped.dst2
        mats.Run(ocvb)
        dst1 = mats.dst1
        dst2 = mats.mat(clickQuadrant(ocvb))

        Static showDetails As Boolean
        Static detailPoint As cv.Point
        If ocvb.mouseClickFlag And ocvb.mouseClickPoint.X > src.Width Then
            showDetails = True
            ' we are using dst2 for the display and mouse click will have coordinates of the wide result2
            detailPoint = New cv.Point(ocvb.mouseClickPoint.X - src.Width, ocvb.mouseClickPoint.Y)
        End If
        ocvb.trueText(New TTtext("Click any centroid to get details", New cv.Point(src.Width + 10, src.Height - 50)))

        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        Dim maxZ = inRangeSlider.value / 1000
        Dim fontsize = If(ocvb.parms.resolution = resHigh, 1.0, 0.6)
        Dim dText As String = ""
        Dim textPoint As cv.Point
        Select Case clickQuadrant(ocvb)
            Case 0
                Dim pixelPerMeter = clipped.topPixel.measure.pixelsPerMeter
                Dim rects = New List(Of cv.Rect)(clipped.topView.pTrack.matchedRects)
                If rects.Count > 0 And showDetails Then
                    Dim minIndex = setDetails(detailPoint, clipped.topView.pTrack.matchedPoints)
                    Dim r = rects(minIndex)
                    dText = Format(maxZ * (src.Height - r.Y - r.Height) / src.Height, "#0.0") + "-" + Format(maxZ * (src.Height - r.Y) / src.Height, "#0.0") + "m & " +
                            CStr(r.Width) + " pixels or " + Format(r.Width / pixelPerMeter, "0.0") + "m"
                    textPoint = New cv.Point(r.X + src.Width, r.Y) ' Add src.width to r.x to make this appear in dst2...
                End If
            Case 1
                Dim pixelPerMeter = clipped.sidePixel.measure.pixelsPerMeter
                Dim rects = New List(Of cv.Rect)(clipped.sideView.pTrack.matchedRects)
                If rects.Count > 0 And showDetails Then
                    Dim minIndex = setDetails(detailPoint, clipped.sideView.pTrack.matchedPoints)
                    Dim r = rects(minIndex)
                    dText = Format(maxZ * (src.Height - r.Y - r.Height) / src.Height, "#0.0") + "-" + Format(maxZ * (src.Height - r.Y) / src.Height, "#0.0") + "m & " +
                            CStr(r.Width) + " pixels or " + Format(r.Width / pixelPerMeter, "0.0") + "m"
                    textPoint = New cv.Point(r.X + src.Width, r.Y) ' Add src.width to r.x to make this appear in dst2...
                End If
            Case Else
        End Select
        ocvb.trueText(New TTtext(dText, textPoint))
    End Sub
End Class






Public Class PointCloud_BackProject_TopView
    Inherits ocvbClass
    Dim clipped As PointCloud_PixelClipped_BothViews
    Dim inrange As Depth_InRange
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        clipped = New PointCloud_PixelClipped_BothViews(ocvb)
        inrange = New Depth_InRange(ocvb)
        ocvb.desc = "BackProject the top view into the front looking view"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        Dim maxZ = inRangeSlider.value / 1000

        clipped.Run(ocvb)
        dst1 = clipped.dst1
        dst2.SetTo(0)
        Dim depth32f = getDepth32f(ocvb)
        Dim mask As New cv.Mat
        For i = 0 To clipped.topView.pTrack.matchedRects.Count - 1
            Dim r = clipped.topView.pTrack.matchedRects(i)
            Dim mindepth = 1000 * maxZ * (src.Height - r.Y - r.Height) / src.Height
            Dim maxDepth = 1000 * maxZ * (src.Height - r.Y) / src.Height
            Dim topRect = New cv.Rect(r.X, 0, r.Width, src.Height)
            If topRect.X + topRect.Width >= src.Width Then topRect.Width = src.Width - topRect.X
            If topRect.Width > 0 Then
                cv.Cv2.InRange(depth32f(topRect), cv.Scalar.All(mindepth), cv.Scalar.All(maxDepth), mask)
                dst2(topRect).SetTo(clipped.topView.pTrack.matchedColors(i), mask)
            End If
        Next

    End Sub
End Class







Public Class PointCloud_PixelFormula_TopView
    Inherits ocvbClass
    Public measure As PointCloud_Measured_TopView
    Public matchedPixelWidth As New List(Of Integer) ' matches the ptrack.matchedxxx data.
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
        Dim cameraPoint = New cv.Point(src.Height, src.Height)
        Dim pixeldistance = src.Height * ((sliders.trackbar(0).Value / 1000) / measure.maxZ)
        Dim FOV = measure.cMats.hFOVangles(ocvb.parms.cameraIndex)
        Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * pixeldistance)
        Dim xpt1 = New cv.Point(cameraPoint.X - lineHalf, src.Height - pixeldistance)
        Dim xpt2 = New cv.Point(cameraPoint.X + lineHalf, src.Height - pixeldistance)
        dst1.Line(xpt1, xpt2, cv.Scalar.Red, 3)

        matchedPixelWidth.Clear()
        For i = 0 To measure.pTrack.matchedRects.Count - 1
            Dim r = measure.pTrack.matchedRects(i)
            lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * (src.Height - (r.Y + r.Height)))
            Dim pt1 = New cv.Point(cameraPoint.X - lineHalf, r.Y + r.Height)
            Dim pt2 = New cv.Point(cameraPoint.X + lineHalf, r.Y + r.Height)
            Dim leftX = Math.Max(pt1.X, r.X)
            Dim rightX = Math.Min(pt2.X, r.X + r.Width)
            dst1.Line(New cv.Point(leftX, pt1.Y), New cv.Point(rightX, pt1.Y), cv.Scalar.Yellow, 3)
            If Math.Abs(rightX - leftX) < lineHalf Then
                ' need to add a small amount based on the angle of the camera.
                ' additional pixels = r.height * tan(angle to camera of back corner) - first find which corner is nearest the centerline.
                Dim c1 = Math.Abs(cameraPoint.X - r.X)
                Dim c2 = Math.Abs(cameraPoint.X - r.X - r.Width)
                Dim cp1 = New cv.Point(r.X, r.Y)
                If c2 < c1 Then cp1 = New cv.Point(r.X + r.Width, r.Y)
                Dim addLen = r.Height * Math.Abs(cp1.X - cameraPoint.X) / (src.Height - cp1.Y) * If(cp1.X < cameraPoint.X, 1, -1)
                dst1.Line(New cv.Point(r.X, r.Y + r.Height), New cv.Point(cp1.X + addLen, r.Y + r.Height), cv.Scalar.Yellow, 3)
                dst1.Line(cameraPoint, cp1, cv.Scalar.Red, 1)
                matchedPixelWidth.Add(src.Width * (addLen + Math.Abs(rightX - leftX)) / pixeldistance)
            End If
        Next
    End Sub
End Class








Public Class PointCloud_PixelFormula_SideView
    Inherits ocvbClass
    Public measure As PointCloud_Measured_SideView
    Public top As PointCloud_PixelFormula_TopView
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        measure = New PointCloud_Measured_SideView(ocvb)
        top = New PointCloud_PixelFormula_TopView(ocvb)

        ocvb.desc = "Validate the formula for pixel height as a function of distance"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        Dim maxZ = inRangeSlider.Value / 1000

        measure.Run(ocvb)
        label1 = measure.label1

        top.sliders.trackbar(0).Maximum = maxZ * 1000
        dst1 = measure.cMats.CameraLocationSide(ocvb, measure.dst1, maxZ)
        Dim cameraPoint = measure.cMats.cameraPoint
        Dim pixeldistance = src.Height * ((top.sliders.trackbar(0).Value / 1000) / maxZ)
        Dim FOV = measure.cMats.vFOVangles(ocvb.parms.cameraIndex)
        Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * pixeldistance)

        Dim pt1 = New cv.Point(CInt(cameraPoint.X + pixeldistance), CInt(cameraPoint.Y - lineHalf))
        Dim pt2 = New cv.Point(CInt(cameraPoint.X + pixeldistance), CInt(cameraPoint.Y + lineHalf))
        dst1.Line(pt1, pt2, cv.Scalar.Red, 3)
    End Sub
End Class







Public Class PointCloud_Measured_TopView
    Inherits ocvbClass
    Public cMats As PointCloud_Colorize
    Public gvec As PointCloud_Object_TopView
    Public pTrack As Kalman_PointTracker
    Public pixelsPerMeter As Single ' pixels per meter at the distance requested.
    Public maxZ As Single
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        cMats = New PointCloud_Colorize(ocvb)
        gvec = New PointCloud_Object_TopView(ocvb)
        pTrack = New Kalman_PointTracker(ocvb)

        ocvb.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000

        gvec.Run(ocvb)

        For Each pt In gvec.flood.centroids
            gvec.dst2.Circle(pt, 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        pTrack.queryPoints = New List(Of cv.Point2f)(gvec.flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(gvec.flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(gvec.flood.masks)
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
    Public gvec As PointCloud_Object_SideView
    Public pTrack As Kalman_PointTracker
    Public pixelsPerMeter As Single ' pixels per meter at the distance requested.
    Public maxZ As Single
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        cMats = New PointCloud_Colorize(ocvb)
        gvec = New PointCloud_Object_SideView(ocvb)
        pTrack = New Kalman_PointTracker(ocvb)

        ocvb.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000

        gvec.Run(ocvb)

        For Each pt In gvec.flood.centroids
            gvec.dst2.Circle(pt, 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        pTrack.queryPoints = New List(Of cv.Point2f)(gvec.flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(gvec.flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(gvec.flood.masks)
        pTrack.Run(ocvb)

        dst1 = cMats.CameraLocationSide(ocvb, pTrack.dst1, maxZ)
        Dim FOV = cMats.vFOVangles(ocvb.parms.cameraIndex)
        Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * src.Height)
        pixelsPerMeter = lineHalf / (Math.Tan(FOV / 2 * 0.0174533) * maxZ)
        label1 = Format(pixelsPerMeter, "0") + " pixels per meter at " + Format(maxZ, "0.0") + " meters"
    End Sub
End Class

