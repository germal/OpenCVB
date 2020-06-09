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





' https://www.intelrealsense.com/depth-camera-d435i/
' https://docs.microsoft.com/en-us/azure/kinect-dk/hardware-specification
' https://www.stereolabs.com/zed/
' https://www.mynteye.com/pages/mynt-eye-d
Public Class Projection_ColorizeMat
    Inherits ocvbClass
    Dim palette As Palette_Gradient
    Public rect As cv.Rect
    Public shift As Integer
    Public labelShift As Integer
    Public pixelsPerMeter As Single
    Dim fontSize As Single
    Dim radius As Integer
    Dim arcSize As Integer = 100
    Dim hFOVangles() As Single = {55, 45, 0, 40, 51}  ' T265 has no point cloud so there is a 0 where it would have been.
    Dim vFOVangles() As Single = {69, 60, 0, 55, 65}  ' T265 has no point cloud so there is a 0 where it would have been.
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
        Dim startAngle = sliders.TrackBar1.Value
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
        Dim startAngle = sliders.TrackBar2.Value
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

        sliders.setupTrackBar1(ocvb, caller, "Top View angle for FOV", 0, 180, hFOVangles(ocvb.parms.cameraIndex))
        sliders.setupTrackBar2("Side View angle for FOV", 0, 180, vFOVangles(ocvb.parms.cameraIndex))

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




Public Class Projection_Raw_CPP
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





Public Class Projection_Raw
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
        gCloud.imu.showLog = False
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

        Dim threshold = 1 ' cMats.sliders.TrackBar1.Value
        topMask = histTop.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        sideMask = histSide.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        label1 = "Top View after threshold"
        label2 = "Side View after threshold"
        handleXYZ.Free()

        Dim fontSize As Single = 1.0
        If ocvb.parms.resolution = resMed Then fontSize = 0.6
        If standalone Then
            dst1 = cMats.CameraLocationBot(ocvb, topMask, 4)
            dst2 = cMats.CameraLocationSide(ocvb, sideMask, 4)
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
                    Dim x = Math.Truncate(cMats.shift + dFactor * (zHalf + fx))
                    Dim y = Math.Truncate(h - dPixel)
                    If x > 0 And x < topView.Width And y >= 0 And y < topView.Height Then
                        Dim count = topView.Get(Of Byte)(y, x) + 1
                        topView.Set(Of Byte)(y, x, If(count < 255, count, 255))
                    End If


                    Dim fy = gCloud.xyz(i * 3 + 1)
                    If fy > -zHalf And fy < zHalf Then
                        x = Math.Truncate(cMats.shift + dPixel)
                        y = Math.Truncate(dFactor * (zHalf + fy))
                        If x > 0 And x < topView.Width And y >= 0 And y < topView.Height Then
                            Dim count = sideView.Get(Of Byte)(y, x) + 1
                            sideView.Set(Of Byte)(y, x, If(count < 255, count, 255))
                        End If
                    End If
                End If
            End Sub)

        If standalone Then
            Dim threshold = 1 ' cMats.sliders.TrackBar1.Value
            Dim topMask = topView.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
            Dim sideMask = sideView.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

            dst1 = cMats.CameraLocationBot(ocvb, topMask, 4)
            dst2 = cMats.CameraLocationSide(ocvb, sideMask, 4)
        End If
    End Sub
End Class







Public Class Projection_NoGravity_SideView
    Inherits ocvbClass
    Dim hist As Histogram_2D_SideView
    Public cMats As Projection_ColorizeMat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        cMats = New Projection_ColorizeMat(ocvb)
        cMats.shift = (src.Width - src.Height) / 2
        cMats.Run(ocvb)

        hist = New Histogram_2D_SideView(ocvb)

        ocvb.desc = "Display the histogram without adjusting for gravity"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.Run(ocvb)
        dst1 = cMats.CameraLocationSide(ocvb, hist.dst1.ConvertScaleAbs(255), hist.hist.trimPC.sliders.TrackBar1.Value / 1000)
    End Sub
End Class







Public Class Projection_NoGravity_TopView
    Inherits ocvbClass
    Dim hist As Histogram_2D_TopView
    Public cMats As Projection_ColorizeMat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        cMats = New Projection_ColorizeMat(ocvb)
        cMats.shift = 0
        cMats.Run(ocvb)

        hist = New Histogram_2D_TopView(ocvb)

        ocvb.desc = "Display the histogram without adjusting for gravity"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist.Run(ocvb)
        dst1 = cMats.CameraLocationBot(ocvb, hist.dst1.ConvertScaleAbs(255), hist.trimPC.sliders.TrackBar1.Value / 1000)
    End Sub
End Class