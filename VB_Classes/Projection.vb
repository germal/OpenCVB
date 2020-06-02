Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO
Module Projection
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





Public Class Projection_ColorizeMat
    Inherits ocvbClass
    Dim palette As Palette_Gradient
    Public rect As cv.Rect
    Public shift As Integer
    Public colorizeMat As cv.Mat
    Public colorizeMatFlip As cv.Mat
    Dim fontSize As Single
    Dim radius As Integer
    Public Function CameraLocationBot(ocvb As AlgorithmData, mask As cv.Mat) As cv.Mat
        Dim dst As New cv.Mat(mask.Size, cv.MatType.CV_8UC3, 0)
        dst1.CopyTo(dst, mask)
        Dim shift = (dst.Width - dst.Height) / 2
        dst.Rectangle(New cv.Rect(shift, 0, dst.Height, dst.Height), cv.Scalar.White, 1)
        Dim cameraLocation = New cv.Point(shift + dst.Height / 2, dst.Height - 5)
        dst.Circle(New cv.Point(shift + dst.Height / 2, dst.Height - 5), radius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Dim maxZ = sliders.TrackBar1.Value / 1000
        For i = maxZ - 1 To 0 Step -1
            Dim y = dst.Height * i / maxZ
            dst.Line(New cv.Point(shift, y), New cv.Point(dst.Width - shift, y), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst, CStr(maxZ - i) + "m", New cv.Point(shift - 45, y + 10), cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next
        Dim angle As Single, startAngle As Single, endAngle As Single = 0 ' assume D435i
        startAngle = Choose(ocvb.parms.cameraIndex + 1, 55, 43, 0, 37, 48) ' T265 is not supported here...
        angle = -startAngle
        dst.Ellipse(New cv.Point(dst.Width / 2, dst.Height - 1), New cv.Size(100, 100), angle, startAngle, endAngle, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        Dim labelLocation = New cv.Point(dst.Width / 2 + shift * 3 / 4, dst.Height * 7 / 8)
        If ocvb.parms.resolution = resHigh Then labelLocation = New cv.Point(dst.Width / 2 + shift * 3 / 8, dst.Height * 15 / 16)
        cv.Cv2.PutText(dst, CStr(startAngle) + " degrees", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Return dst
    End Function
    Public Function CameraLocationLeft(ocvb As AlgorithmData, ByRef mask As cv.Mat) As cv.Mat
        Dim dst As New cv.Mat(mask.Size, cv.MatType.CV_8UC3, 0)
        dst2.CopyTo(dst, mask)
        Dim shift = (dst.Width - dst.Height) / 2
        dst.Rectangle(New cv.Rect(shift, 0, dst.Height, dst.Height), cv.Scalar.White, 1)
        dst.Circle(New cv.Point(shift, dst.Height / 2), radius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Dim maxZ = sliders.TrackBar1.Value / 1000
        For i = 1 To maxZ
            Dim x = (dst.Width - 2 * shift) * i / maxZ
            dst.Line(New cv.Point(shift + x, 0), New cv.Point(shift + x, dst.Height), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst, CStr(i) + "m", New cv.Point(shift + x + 10, dst.Height - 10), cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next
        Return dst
    End Function
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        fontSize = 1.0
        If ocvb.parms.resolution = resMed Then fontSize = 0.6
        radius = If(ocvb.parms.resolution = resMed, 5, 12)
        shift = (src.Width - src.Height) / 2
        rect = New cv.Rect(shift, 0, dst1.Height, dst1.Height)

        palette = New Palette_Gradient(ocvb)
        palette.color1 = cv.Scalar.Yellow
        palette.color2 = cv.Scalar.Blue
        palette.frameModulo = 1

        sliders.setupTrackBar1(ocvb, caller, "Gravity Transform Max Depth (in millimeters)", 0, 10000, 4000)
        sliders.setupTrackBar2("Threshold for histogram count", 0, 100, 3)

        ocvb.desc = "Create the colorizeMat's used for projections"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        palette.Run(ocvb)
        dst1 = palette.dst1
        dst2 = dst1.Clone
        cv.Cv2.Rotate(dst1(rect), dst2(rect), cv.RotateFlags.Rotate90Clockwise)
    End Sub
End Class




Public Class Projection_NoGravity_CPP
    Inherits ocvbClass
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        foreground = New Depth_ManualTrim(ocvb)
        foreground.sliders.TrackBar1.Value = 300  ' fixed distance to keep the images stable.
        foreground.sliders.TrackBar2.Value = 4000 ' fixed distance to keep the images stable.
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
        Dim desiredMin = CSng(foreground.sliders.TrackBar1.Value)
        Dim desiredMax = CSng(foreground.sliders.TrackBar2.Value)
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





Public Class Projection_NoGravity
    Inherits ocvbClass
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        foreground = New Depth_ManualTrim(ocvb)
        foreground.sliders.TrackBar1.Value = 300  ' fixed distance to keep the images stable.
        foreground.sliders.TrackBar2.Value = 4000 ' fixed distance to keep the images stable.
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
        Dim desiredMin = CSng(foreground.sliders.TrackBar1.Value)
        Dim desiredMax = CSng(foreground.sliders.TrackBar2.Value)
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






Public Class Projection_Gravity_CPP
    Inherits ocvbClass
    Dim gCloud As Transform_Gravity
    Public cMats As Projection_ColorizeMat
    Dim cPtr As IntPtr
    Dim xyzBytes() As Byte
    Public topMask As cv.Mat
    Public sideMask As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        gCloud = New Transform_Gravity(ocvb)
        cPtr = Project_GravityHist_Open()

        cMats = New Projection_ColorizeMat(ocvb)
        cMats.Run(ocvb)

        ocvb.desc = "Rotate the point cloud data with the gravity data and project a top down and side view"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim maxZ = cMats.sliders.TrackBar1.Value / 1000

        gCloud.Run(ocvb)
        Dim xyz As New cv.Mat
        cv.Cv2.Merge(gCloud.vertSplit, xyz)

        If xyzBytes Is Nothing Then ReDim xyzBytes(xyz.Total * xyz.ElemSize - 1)
        Marshal.Copy(xyz.Data, xyzBytes, 0, xyzBytes.Length)
        Dim handleXYZ = GCHandle.Alloc(xyzBytes, GCHandleType.Pinned)

        Dim imagePtr As IntPtr
        imagePtr = Project_GravityHist_Run(cPtr, handleXYZ.AddrOfPinnedObject, maxZ, xyz.Height, xyz.Width)

        Dim histTop = New cv.Mat(xyz.Rows, xyz.Cols, cv.MatType.CV_32F, imagePtr)
        Dim histSide = New cv.Mat(xyz.Rows, xyz.Cols, cv.MatType.CV_32F, Project_GravityHist_Side(cPtr))

        Dim threshold = cMats.sliders.TrackBar2.Value
        topMask = histTop.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        sideMask = histSide.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        label1 = "Top View after threshold"
        label2 = "Side View after threshold"
        handleXYZ.Free()

        Dim fontSize As Single = 1.0
        If ocvb.parms.resolution = resMed Then fontSize = 0.6
        If standalone Then
            dst1 = cMats.CameraLocationBot(ocvb, topMask)
            dst2 = cMats.CameraLocationLeft(ocvb, sideMask)
        Else
            dst1 = topMask
            dst2 = sideMask
        End If
    End Sub
    Public Sub Close()
        Project_GravityHist_Close(cPtr)
    End Sub
End Class







Public Class Projection_Wall
    Inherits ocvbClass
    Dim objects As Projection_Objects
    Dim lines As lineDetector_FLD_CPP
    Dim dilate As DilateErode_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        lines = New lineDetector_FLD_CPP(ocvb)
        objects = New Projection_Objects(ocvb)
        dilate = New DilateErode_Basics(ocvb)

        label1 = "Top View: walls in red"
        ocvb.desc = "Use the top down view to detect walls with a line detector algorithm - needs more work"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        objects.src = src
        objects.Run(ocvb)
        dst1 = objects.dst1

        dilate.src = dst1
        dilate.Run(ocvb)

        lines.src = dilate.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        lines.Run(ocvb)
        dst1 = lines.dst1.Clone()

        dst2 = objects.dst2
        label2 = objects.label2
    End Sub
End Class






Public Class Projection_Objects
    Inherits ocvbClass
    Dim flood As FloodFill_Projection
    Public fontSize As Single = 1.0
    Public gravity As Projection_Gravity_CPP
    Public maxZ As Single
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        sliders.setupTrackBar1(ocvb, caller, "epsilon for GroupRectangles X100", 0, 200, 80)

        gravity = New Projection_Gravity_CPP(ocvb)

        flood = New FloodFill_Projection(ocvb)
        flood.sliders.TrackBar1.Value = 100

        label1 = "Isolated objects - Red dot is camera"
        ocvb.desc = "Floodfill the histogram to find the significant 3D objects in the field of view (not floors or ceilings) - needs more work"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex = T265Camera Then
            ocvb.putText(New oTrueType("There is no point cloud available on the T265 camera", 10, 60, RESULT1))
            Exit Sub
        End If

        gravity.src = src
        gravity.Run(ocvb)
        flood.src = gravity.dst1
        flood.Run(ocvb)
        dst1 = flood.dst1

        dst2 = dst1.Clone()
        If ocvb.parms.resolution = resMed Then fontSize = 0.6
        maxZ = gravity.sliders.TrackBar1.Value / 1000
        Dim mmPerPixel = maxZ * 1000 / src.Height
        Dim maxCount = Math.Min(flood.objectRects.Count, 10)
        For i = 0 To maxCount - 1
            Dim rect = flood.objectRects(i)
            dst2.Rectangle(rect, cv.Scalar.White, 1)
            Dim minDistanceFromCamera = (src.Height - rect.Y - rect.Height) * mmPerPixel
            Dim maxDistanceFromCamera = (src.Height - rect.Y) * mmPerPixel
            Dim objectWidth = rect.Width * mmPerPixel

            dst2.Circle(New cv.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), If(ocvb.parms.resolution = resMed, 6, 10), cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
            dst2.Circle(New cv.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), If(ocvb.parms.resolution = resMed, 3, 5), cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
            Dim text = "depth=" + Format(minDistanceFromCamera / 1000, "#0.0") + "-" + Format(maxDistanceFromCamera / 1000, "0.0") + "m Width=" + Format(objectWidth / 1000, "#0.0") + " m"

            Dim pt = New cv.Point(rect.X, rect.Y - 10)
            cv.Cv2.PutText(dst2, text, pt, cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Next
        label2 = "Showing the top " + CStr(maxCount) + " out of " + CStr(flood.objectRects.Count) + " regions > " + CStr(flood.minFloodSize) + " pixels"
    End Sub
End Class





Public Class Projection_Backprojection
    Inherits ocvbClass
    Dim pFlood As Projection_Objects
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Use Projection_Objects to find objects and then backproject them into front-facing view."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
    End Sub
End Class








Public Class Projection_Gravity
    Inherits ocvbClass
    Dim gCloud As Transform_Gravity
    Dim grid As Thread_Grid
    Public cMats As Projection_ColorizeMat
    Public topView As cv.Mat
    Public sideView As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        gCloud = New Transform_Gravity(ocvb)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 40

        cMats = New Projection_ColorizeMat(ocvb)
        cMats.Run(ocvb)

        label1 = "View looking up from under floor - Red Dot is camera"
        label2 = "Side View - Red Dot is camera"
        ocvb.desc = "Rotate the point cloud data with the gravity data and project a top down and side view"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        gCloud.Run(ocvb)

        topView = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U, 0)
        sideView = topView.Clone()

        Dim white = New cv.Vec3b(255, 255, 255)
        Dim maxZ = cMats.sliders.TrackBar1.Value / 1000
        Dim w = CSng(src.Width)
        Dim h = CSng(src.Height)
        Dim dFactor = h / maxZ ' the scale in the x-Direction.
        Dim zHalf As Single = maxZ / 2

        Parallel.For(0, CInt(gCloud.xyz.Length / 3),
            Sub(i)
                Dim d = gCloud.xyz(i * 3 + 2)
                If d > 0 And d < maxZ Then
                    Dim t = CInt(255 * d / maxZ)

                    Dim dPixel = dFactor * d
                    Dim fx = gCloud.xyz(i * 3)
                    If fx > -zHalf And fx < zHalf Then
                        fx = dFactor * (zHalf + fx)
                        Dim count = topView.Get(Of Byte)(CInt(h - dPixel), CInt(cMats.shift + fx)) + 1
                        topView.Set(Of Byte)(CInt(h - dPixel), CInt(cMats.shift + fx), If(count < 255, count, 255))
                    End If

                    Dim fy = gCloud.xyz(i * 3 + 1)
                    If fy > -zHalf And fy < zHalf Then
                        fy = CInt(dFactor * (zHalf + fy))
                        Dim count = sideView.Get(Of Byte)(fy, CInt(cMats.shift + dPixel)) + 1
                        sideView.Set(Of Byte)(fy, CInt(cMats.shift + dPixel), If(count < 255, count, 255))
                    End If
                End If
            End Sub)

        If standalone Then
            Dim threshold = cMats.sliders.TrackBar2.Value
            Dim topMask = topView.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
            Dim sideMask = sideView.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

            dst1 = cMats.CameraLocationBot(ocvb, topMask)
            dst2 = cMats.CameraLocationLeft(ocvb, sideMask)
        End If
    End Sub
End Class
