Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO
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





    Public Function showDistances(ocvb As AlgorithmData, dst As cv.Mat, rects As List(Of cv.Rect), maxZ As Single, imuActive As Boolean) As cv.Mat
        If dst.Channels = 1 Then dst = dst.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If imuActive Then
            ocvb.putText(New TTtext("The rotated and projected point cloud image does not have valid distance.", 10, 60, RESULT2))
        Else
            Dim fontSize As Single = 1.0
            If ocvb.parms.resolution = resMed Then fontSize = 0.6
            Dim mmPerPixel = maxZ * 1000 / dst.Height
            For i = 0 To rects.Count - 1
                Dim rect = rects(i)
                Dim minDistanceFromCamera = (dst.Height - rect.Y - rect.Height) * mmPerPixel
                Dim maxDistanceFromCamera = (dst.Height - rect.Y) * mmPerPixel
                Dim objectWidth = rect.Width * mmPerPixel

                dst.Circle(New cv.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), If(ocvb.parms.resolution = resMed, 6, 10),
                           cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
                dst.Circle(New cv.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), If(ocvb.parms.resolution = resMed, 3, 5),
                           cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
                Dim text = "depth=" + Format(minDistanceFromCamera / 1000, "#0.0") + "-" + Format(maxDistanceFromCamera / 1000, "0.0") +
                           "m Width=" + Format(objectWidth / 1000, "#0.0") + " m"
                dst.Rectangle(rect, cv.Scalar.Red, 1)
                Dim pt = New cv.Point(rect.X, rect.Y - 10)
                cv.Cv2.PutText(dst, text, pt, cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            Next
        End If
        Return dst
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
    Public pixelsPerMeter As Single
    Dim fontSize As Single
    Dim radius As Integer
    Dim arcSize As Integer = 100
    Dim hFOVangles() As Single = {45, 0, 40, 51, 55, 55, 47}  ' T265 has no point cloud so there is a 0 where it would have been.
    Dim vFOVangles() As Single = {60, 0, 55, 65, 69, 67, 60}  ' T265 has no point cloud so there is a 0 where it would have been.
    Public Function CameraLocationBot(ocvb As AlgorithmData, mask As cv.Mat, maxZ As Single) As cv.Mat
        Dim dst As New cv.Mat(mask.Size, cv.MatType.CV_8UC3, 0)
        dst1.CopyTo(dst, mask)
        Dim cameraPt = New cv.Point(dst.Height, dst.Height)
        Dim cameraLocation = New cv.Point(shift + dst.Height / 2, dst.Height - 5)
        dst.Circle(cameraPt, radius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        For i = maxZ - 1 To 0 Step -1
            Dim ymeter = dst.Height * i / maxZ
            dst.Line(New cv.Point(shift, ymeter), New cv.Point(dst.Width - shift, ymeter), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst, CStr(maxZ - i) + "m", New cv.Point(10, ymeter - 10), cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next

        ' draw the arc showing the camera FOV
        Dim startAngle = sliders.trackbar(0).Value
        Dim x = dst.Height / Math.Tan(startAngle * cv.Cv2.PI / 180)
        Dim xloc = cameraPt.X + x

        Dim fovRight = New cv.Point(xloc, 0)
        Dim fovLeft = New cv.Point(cameraPt.X - x, fovRight.Y)

        dst.Ellipse(cameraPt, New cv.Size(arcSize, arcSize), -startAngle, startAngle, 0, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        dst.Ellipse(cameraPt, New cv.Size(arcSize, arcSize), 0, 180, 180 + startAngle, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        dst.Line(cameraPt, fovLeft, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Dim labelLocation = New cv.Point(dst.Width / 2 + labelShift * 3 / 4, dst.Height * 7 / 8)
        If ocvb.parms.resolution = resHigh Then labelLocation = New cv.Point(dst.Width / 2 + labelShift * 3 / 8, dst.Height * 15 / 16)
        cv.Cv2.PutText(dst, CStr(startAngle) + " degrees" + " FOV=" + CStr(180 - startAngle * 2), labelLocation, cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        dst.Line(cameraPt, fovRight, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Return dst
    End Function
    Public Function CameraLocationSide(ocvb As AlgorithmData, ByRef mask As cv.Mat, maxZ As Single) As cv.Mat
        Dim dst As New cv.Mat(mask.Size, cv.MatType.CV_8UC3, 0)
        dst2.CopyTo(dst, mask)
        Dim cameraPt = New cv.Point(shift, src.Height - (src.Width - src.Height) / 2)
        dst.Rectangle(New cv.Rect(shift, 0, src.Height, src.Height), cv.Scalar.White, 1)
        dst.Circle(cameraPt, radius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        For i = 0 To maxZ
            Dim xmeter = dst.Height * i / maxZ
            dst.Line(New cv.Point(shift + xmeter, 0), New cv.Point(shift + xmeter, dst.Height), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst, CStr(i) + "m", New cv.Point(shift + xmeter + 10, dst.Height - 10), cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next

        ' draw the arc showing the camera FOV
        Dim startAngle = sliders.trackbar(1).Value
        Dim y = (dst.Width - shift) / Math.Tan(startAngle * cv.Cv2.PI / 180)
        Dim yloc = cameraPt.Y - y

        Dim fovTop = New cv.Point(dst.Width, yloc)
        Dim fovBot = New cv.Point(dst.Width, cameraPt.Y + y)

        dst.Ellipse(cameraPt, New cv.Size(arcSize, arcSize), -startAngle + 90, startAngle, 0, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        dst.Ellipse(cameraPt, New cv.Size(arcSize, arcSize), 90, 180, 180 + startAngle, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        dst.Line(cameraPt, fovTop, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Dim labelLocation = New cv.Point(10, dst.Height * 3 / 4)
        ' If ocvb.parms.resolution = resHigh Then labelLocation = New cv.Point(labelShift * 3 / 8, dst.Height * 15 / 16)
        cv.Cv2.PutText(dst, CStr(startAngle) + " degrees" + " FOV=" + CStr(180 - startAngle * 2), labelLocation, cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        dst.Line(cameraPt, fovBot, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Return dst
    End Function
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        fontSize = 1.0
        If ocvb.parms.resolution = resMed Then fontSize = 0.6
        radius = If(ocvb.parms.resolution = resMed, 5, 12)
        shift = (src.Width - src.Height) / 2
        labelShift = shift

        palette = New Palette_Gradient(ocvb)
        palette.color1 = cv.Scalar.Yellow
        palette.color2 = cv.Scalar.Blue
        palette.frameModulo = 1
        If ocvb.parms.resolution <> resHigh Then arcSize = 50

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Top View angle for FOV", 0, 180, hFOVangles(ocvb.parms.cameraIndex))
        sliders.setupTrackBar(1, "Side View angle for FOV", 0, 180, vFOVangles(ocvb.parms.cameraIndex))

        palette.Run(ocvb)
        dst1 = palette.dst1
        dst2 = dst1.Clone
        rect = New cv.Rect(shift, 0, dst1.Height, dst1.Height)
        cv.Cv2.Rotate(dst1(rect), dst2(rect), cv.RotateFlags.Rotate90Clockwise)

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
        grid.sliders.trackbar(0).Value = 64
        grid.sliders.trackbar(1).Value = 32

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
        grid.sliders.trackbar(0).Value = 64
        grid.sliders.trackbar(1).Value = 32

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






Public Class PointCloud_Plane_Walls
    Inherits ocvbClass
    Dim objects As PointCloud_TopView_Distances
    Dim lines As lineDetector_FLD_CPP
    Dim dilate As DilateErode_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        lines = New lineDetector_FLD_CPP(ocvb)
        objects = New PointCloud_TopView_Distances(ocvb)
        dilate = New DilateErode_Basics(ocvb)

        label1 = "Top View: walls in red"
        ocvb.desc = "Use the top down view to detect walls with a line detector algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        objects.src = src
        objects.Run(ocvb)
        dst1 = objects.dst1
        dst2 = objects.dst2
        label2 = objects.label2

        dilate.src = dst1
        dilate.Run(ocvb)

        'lines.src = If(dilate.dst1.Channels = 3, dilate.dst1.Clone(), dilate.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
        'lines.Run(ocvb)
        'dst1 = lines.dst1.Clone()
    End Sub
End Class






Public Class PointCloud_Planes_CeilingFloor
    Inherits ocvbClass
    Dim objects As PointCloud_SideView_Objects
    Dim lines As lineDetector_FLD_CPP
    Dim dilate As DilateErode_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        lines = New lineDetector_FLD_CPP(ocvb)
        objects = New PointCloud_SideView_Objects(ocvb)
        dilate = New DilateErode_Basics(ocvb)

        label1 = "Top View: walls in red"
        ocvb.desc = "Use the side view to detect ceilings and floors with a line detector algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        objects.src = src
        objects.Run(ocvb)
        dst1 = objects.dst1
        dst2 = objects.dst2
        label2 = objects.label2

        'dilate.src = dst1
        'dilate.Run(ocvb)

        'lines.src = dilate.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        'lines.Run(ocvb)
        'dst1 = lines.dst1.Clone()
    End Sub
End Class






Public Class PointCloud_SideView
    Inherits ocvbClass
    Public hist As Histogram_2D_SideView
    Public cMats As PointCloud_Colorize
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        cMats = New PointCloud_Colorize(ocvb)
        cMats.sliders.Visible = standalone
        cMats.shift = (src.Width - src.Height) / 2
        cMats.Run(ocvb)

        hist = New Histogram_2D_SideView(ocvb)
        hist.histOpts.check.Box(0).Checked = False

        ocvb.desc = "Display the histogram with and without adjusting for gravity"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.Run(ocvb)
        If hist.histOpts.check.Box(0).Checked Then
            dst1 = hist.dst1
        Else
            dst1 = cMats.CameraLocationSide(ocvb, hist.dst1.ConvertScaleAbs(255), hist.histOpts.sliders.trackbar(1).Value / 1000)
        End If
    End Sub
End Class






Public Class PointCloud_SideView_Objects
    Inherits ocvbClass
    Public flood As FloodFill_Projection
    Public view As PointCloud_SideView
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        view = New PointCloud_SideView(ocvb)
        view.hist.histOpts.check.Box(0).Checked = True ' we want the IMU to rotate the data.
        view.hist.histOpts.sliders.trackbar(0).Value = 5 ' a better default for flood fill
        flood = New FloodFill_Projection(ocvb)
        flood.sliders.trackbar(0).Value = 100

        label1 = "Isolated objects"
        ocvb.desc = "Floodfill the histogram to find the significant 3D objects in the field of view (not floors or ceilings)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex = T265Camera Then
            ocvb.putText(New TTtext("There is no point cloud available on the T265 camera", 10, 60, RESULT1))
            Exit Sub
        End If

        view.src = src
        view.Run(ocvb)
        dst1 = view.dst1
        flood.src = view.hist.histOutput.Threshold(view.hist.histOpts.sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        flood.Run(ocvb)
        dst2 = flood.dst2
        label2 = flood.label2
    End Sub
End Class







Public Class PointCloud_SideView_Distances
    Inherits ocvbClass
    Public gVec As PointCloud_SideView_Objects
    Public maxZ As Single
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        gVec = New PointCloud_SideView_Objects(ocvb)

        ocvb.desc = "Show identified clusters of depth data in a side view"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        gVec.Run(ocvb)
        maxZ = gVec.view.hist.histOpts.sliders.trackbar(1).Value / 1000
        dst1 = showDistances(ocvb, gVec.dst1, gVec.flood.rects, maxZ, gVec.view.hist.histOpts.check.Box(0).Checked)
        label1 = CStr(CInt(gVec.flood.rects.Count)) + " objects > " + CStr(gVec.flood.minFloodSize) + " pixels"
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
        If hist.histOpts.check.Box(0).Checked Then
            dst1 = hist.dst1
        Else
            dst1 = cMats.CameraLocationBot(ocvb, hist.dst1.ConvertScaleAbs(255), hist.histOpts.sliders.trackbar(1).Value / 1000)
        End If
    End Sub
End Class






Public Class PointCloud_TopView_Objects
    Inherits ocvbClass
    Public flood As FloodFill_Projection
    Public view As PointCloud_TopView
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        view = New PointCloud_TopView(ocvb)
        view.hist.histOpts.check.Box(0).Checked = True ' we want the IMU to rotate the data.
        view.hist.histOpts.sliders.trackbar(0).Value = 5 ' a better default for flood fill
        flood = New FloodFill_Projection(ocvb)
        flood.sliders.trackbar(0).Value = 100

        label1 = "Isolated objects"
        ocvb.desc = "Threshold the histogram to find the significant 3D objects in the field of view (not floors or ceilings)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex = T265Camera Then
            ocvb.putText(New TTtext("There is no point cloud available on the T265 camera", 10, 60, RESULT1))
            Exit Sub
        End If

        view.src = src
        view.Run(ocvb)
        dst1 = view.dst1
        flood.src = view.hist.histOutput.Threshold(view.hist.histOpts.sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        flood.Run(ocvb)
        dst2 = flood.dst2
        label2 = flood.label2
    End Sub
End Class







Public Class PointCloud_TopView_Distances
    Inherits ocvbClass
    Public gVec As PointCloud_TopView_Objects
    Public gSide As PointCloud_SideView_Distances
    Public maxZ As Single
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        gVec = New PointCloud_TopView_Objects(ocvb)
        gVec.view.hist.histOpts.check.Box(0).Checked = False ' no imu means the distance can be obtained from the image representation.
        ocvb.suppressOptions = True
        gSide = New PointCloud_SideView_Distances(ocvb)

        ocvb.desc = "Show identified clusters of depth data in a top view"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        gVec.Run(ocvb)
        maxZ = gVec.view.hist.histOpts.sliders.trackbar(1).Value / 1000
        dst1 = showDistances(ocvb, gVec.dst1, gVec.flood.rects, maxZ, gVec.view.hist.histOpts.check.Box(0).Checked)
        label1 = CStr(CInt(gVec.flood.rects.Count)) + " objects > " + CStr(gVec.flood.minFloodSize) + " pixels"
    End Sub
End Class






Public Class PointCloud_TrimDepth
    Inherits ocvbClass
    Public hist As Histogram_2D_TopView
    Public cMats As PointCloud_Colorize
    Dim objects As PointCloud_TopView_Distances
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ' ocvb.suppressOptions = True
        cMats = New PointCloud_Colorize(ocvb)
        cMats.sliders.Visible = standalone
        cMats.shift = 0
        cMats.Run(ocvb)

        hist = New Histogram_2D_TopView(ocvb)
        hist.histOpts.check.Box(0).Checked = False

        objects = New PointCloud_TopView_Distances(ocvb)
        objects.gVec.flood.sliders.trackbar(0).Value = 1 ' we want all possible objects in view.
        objects.gVec.view.hist.histOpts.sliders.trackbar(0).Value = 20 ' should be substantial object...

        ocvb.desc = "Trim the depth data to identified depth objects"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.Run(ocvb)
        objects.Run(ocvb)
        dst2 = objects.dst1
        dst1 = cMats.CameraLocationBot(ocvb, hist.dst1.ConvertScaleAbs(255), hist.histOpts.sliders.trackbar(1).Value / 1000)
    End Sub
End Class