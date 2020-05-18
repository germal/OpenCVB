Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Text
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
    Public Function Project_Gravity_Open(filename As String) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Project_Gravity_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Project_Gravity_Side(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Project_Gravity_Run(cPtr As IntPtr, xyzPtr As IntPtr, maxZ As Single, rows As Int32, cols As Int32) As IntPtr
    End Function




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



Public Class Projection_NoGravity_CPP
    Inherits ocvbClass
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        foreground = New Depth_ManualTrim(ocvb, caller)
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

        Dim h = ocvb.color.Height
        Dim w = ocvb.color.Width
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        foreground = New Depth_ManualTrim(ocvb, caller)
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

        Dim h = ocvb.color.Height
        Dim w = ocvb.color.Width
        Dim desiredMin = CSng(foreground.sliders.TrackBar1.Value)
        Dim desiredMax = CSng(foreground.sliders.TrackBar2.Value)
        Dim range = CSng(desiredMax - desiredMin)
        Dim depth32f = getDepth32f(ocvb)

        ' this VB.Net version is much slower than the optimized C++ version below.
        dst1 = ocvb.color.EmptyClone.SetTo(cv.Scalar.White)
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








Public Class Projection_GravityVB
    Inherits ocvbClass
    Dim imu As IMU_GVector
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        imu = New IMU_GVector(ocvb, caller)
        imu.result = RESULT2

        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        sliders.setupTrackBar1(ocvb, caller, "Gravity Transform Max Depth (in millimeters)", 0, 10000, 4000)

        ocvb.desc = "Rotate the point cloud data with the gravity data and project a top down and side view"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        imu.Run(ocvb)
        Dim split() = cv.Cv2.Split(ocvb.pointCloud)
        Dim vertSplit = split

        Dim zCos = Math.Cos(imu.angleZ)
        Dim zSin = Math.Sin(imu.angleZ)

        Dim xCos = Math.Cos(imu.angleX)
        Dim xSin = Math.Sin(imu.angleX)

        Dim xArray(,) As Single = {{1, 0, 0, 0}, {0, zCos, -zSin, 0}, {0, zSin, zCos, 0}, {0, 0, 0, 1}}
        Dim xRotate = New cv.Mat(4, 4, cv.MatType.CV_32F, xArray)

        Dim zArray(,) As Single = {{xCos, -xSin, 0, 0}, {xSin, xCos, 0, 0}, {0, 0, 1, 0}, {0, 0, 0, 1}}
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

        Dim mask = vertSplit(2).Threshold(0.001, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

        cv.Cv2.Normalize(vertSplit(1), vertSplit(1), 0, ocvb.color.Height, cv.NormTypes.MinMax, -1, mask)

        Dim minval As Double, maxval As Double
        Dim minloc As cv.Point, maxloc As cv.Point
        vertSplit(0).MinMaxLoc(minval, maxval, minloc, maxloc, mask)

        Dim black = New cv.Vec3b(0, 0, 0)
        dst1 = ocvb.color.SetTo(cv.Scalar.White)
        dst2 = dst1.Clone()
        Dim desiredMax = sliders.TrackBar1.Value / 1000
        Dim w = CSng(ocvb.color.Width)
        Dim h = CSng(ocvb.color.Height)
        Dim xFactor = w / (maxval - minval)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
             Sub(roi)
                 For y = roi.Y + roi.Height - 1 To roi.Y Step -1
                     For x = roi.X To roi.X + roi.Width - 1
                         Dim m = mask.Get(Of Byte)(h - y - 1, x)
                         If m > 0 Then
                             Dim depth = vertSplit(2).Get(Of Single)(h - y - 1, x)
                             If depth < desiredMax Then
                                 Dim dx = xFactor * (vertSplit(0).Get(Of Single)(h - y - 1, x) - minval)
                                 Dim dy = Math.Round(h * (desiredMax - depth) / desiredMax)
                                 If dy < h And dy > 0 Then dst1.Set(Of cv.Vec3b)(CInt(h - dy), CInt(dx), black)
                                 dy = Math.Round(vertSplit(1).Get(Of Single)(y, x))
                                 dx = Math.Round(w * (desiredMax - depth) / desiredMax)
                                 If dy < h And dy > 0 Then dst2.Set(Of cv.Vec3b)(CInt(h - dy), CInt(dx), black)
                             End If
                         End If
                     Next
                 Next
             End Sub)
        label1 = "View looking up from under floor"
        label2 = "Side View"
    End Sub
End Class







Public Class Projection_GravityHistogram
    Inherits ocvbClass
    Public gravity As Projection_G_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        gravity = New Projection_G_CPP(ocvb, caller)
        gravity.sliders.GroupBox2.Visible = True
        gravity.histogramRun = True

        ocvb.desc = "Use the top/down projection to create a histogram of 3D points"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        gravity.Run(ocvb)
    End Sub
End Class






Public Class Projection_G_CPP
    Inherits ocvbClass
    Dim imu As IMU_GVector
    Dim cPtr As IntPtr
    Dim histPtr As IntPtr
    Dim xyzBytes() As Byte
    Public histogramRun As Boolean
    Public maxZ As Single
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)

        imu = New IMU_GVector(ocvb, caller)
        imu.result = RESULT2

        sliders.setupTrackBar1(ocvb, caller, "Gravity Transform Max Depth (in millimeters)", 0, 10000, 4000)
        sliders.setupTrackBar2(ocvb, caller, "Threshold for histogram Count", 1, 100, 10)
        sliders.GroupBox2.Visible = False ' default is not a histogramrun

        Dim fileInfo As New FileInfo(ocvb.parms.OpenCVfullPath + "/../../../modules/imgproc/doc/pics/colormaps/colorscale_jet.jpg")
        If fileInfo.Exists = False Then
            MsgBox("The colormaps have moved!  Project_Gravity_CPP won't work." + vbCrLf + "Look for this file:" + fileInfo.FullName)
        End If
        cPtr = Project_Gravity_Open(fileInfo.FullName)
        histPtr = Project_GravityHist_Open()

        ocvb.desc = "Rotate the point cloud data with the gravity data and project a top down and side view"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex = T265Camera Then ocvb.putText(New ActiveClass.TrueType("T265 camera has no pointcloud data", 10, 125))
        imu.Run(ocvb)

        ' normally it is not desirable to resize the point cloud but it can be here because we are building a histogram.
        If standalone Then src = ocvb.pointCloud.Resize(ocvb.color.Size())
        Dim split() = cv.Cv2.Split(src)
        Dim vertSplit = split

        Dim zCos = Math.Cos(imu.angleZ)
        Dim zSin = Math.Sin(imu.angleZ)

        Dim xCos = Math.Cos(imu.angleX)
        Dim xSin = Math.Sin(imu.angleX)

        Dim xArray(,) As Single = {{1, 0, 0, 0}, {0, zCos, -zSin, 0}, {0, zSin, zCos, 0}, {0, 0, 0, 1}}
        Dim xRotate = New cv.Mat(4, 4, cv.MatType.CV_32F, xArray)

        Dim zArray(,) As Single = {{xCos, -xSin, 0, 0}, {xSin, xCos, 0, 0}, {0, 0, 1, 0}, {0, 0, 0, 1}}
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

        maxZ = sliders.TrackBar1.Value / 1000

        Dim xyz As New cv.Mat
        cv.Cv2.Merge(vertSplit, xyz)

        If xyzBytes Is Nothing Then ReDim xyzBytes(xyz.Total * xyz.ElemSize - 1)
        Marshal.Copy(xyz.Data, xyzBytes, 0, xyzBytes.Length)
        Dim handleXYZ = GCHandle.Alloc(xyzBytes, GCHandleType.Pinned)

        Dim imagePtr As IntPtr
        If histogramRun Then
            imagePtr = Project_GravityHist_Run(histPtr, handleXYZ.AddrOfPinnedObject, maxZ, xyz.Height, xyz.Width)

            Dim histTop = New cv.Mat(xyz.Rows, xyz.Cols, cv.MatType.CV_32F, imagePtr)
            Dim histSide = New cv.Mat(xyz.Rows, xyz.Cols, cv.MatType.CV_32F, Project_GravityHist_Side(histPtr))

            Dim threshold = sliders.TrackBar2.Value
            dst1 = histTop.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
            dst2 = histSide.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()

            label1 = "Top View after threshold"
            label2 = "Side View after threshold"
        Else
            imagePtr = Project_Gravity_Run(cPtr, handleXYZ.AddrOfPinnedObject, maxZ, xyz.Height, xyz.Width)

            dst1 = New cv.Mat(xyz.Rows, xyz.Cols, cv.MatType.CV_8UC3, imagePtr).Clone()
            dst2 = New cv.Mat(xyz.Rows, xyz.Cols, cv.MatType.CV_8UC3, Project_Gravity_Side(cPtr)).Clone()

            label1 = "Top View (looking down)"
            label2 = "Side View"
        End If
        handleXYZ.Free()

        If standalone Then
            Dim shift = CInt((xyz.Width - xyz.Height) / 2)
            ocvb.label1 += " - Red Dot is camera"
            ocvb.label2 += " - Red Dot is camera"
            dst1.Rectangle(New cv.Rect(shift, 0, xyz.Height, xyz.Height), cv.Scalar.White, 1)
            dst2.Rectangle(New cv.Rect(shift, 0, xyz.Height, xyz.Height), cv.Scalar.White, 1)
            Dim radius = 5
            dst1.Circle(New cv.Point(shift + xyz.Height / 2, xyz.Height - 15), radius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            dst2.Circle(New cv.Point(shift, xyz.Height / 2), radius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        End If
    End Sub
    Public Sub Close()
        Project_Gravity_Close(cPtr)
        Project_GravityHist_Close(histPtr)
    End Sub
End Class






Public Class Projection_Flood
    Inherits ocvbClass
    Dim flood As FloodFill_Projection
    Dim kalman As Kalman_Basics
    Public gravity As Projection_G_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        kalman = New Kalman_Basics(ocvb, caller)
        ReDim kalman.input(10 * 4 - 1) ' max 10 objects.

        sliders.setupTrackBar1(ocvb, caller, "epsilon for GroupRectangles X100", 0, 200, 80)

        gravity = New Projection_G_CPP(ocvb, caller)
        gravity.sliders.GroupBox2.Visible = True
        gravity.histogramRun = True

        flood = New FloodFill_Projection(ocvb, caller)
        flood.sliders.TrackBar1.Value = 100
        flood.sliders.TrackBar4.Value = 1

        ocvb.desc = "Floodfill the histogram to find the significant 3D objects in the field of view (not floors or ceilings)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        gravity.src = ocvb.pointCloud.Resize(ocvb.color.Size()) ' normally not a good idea to resize pointcloud but legit here because of histogram.
        gravity.Run(ocvb)
        flood.src = gravity.dst1
        flood.Run(ocvb)

        ' Combine rectangles that are overlaping or touching.
        Dim combinedRects As New List(Of cv.Rect)
        ' first duplicate all the current rectangles so all originals (by themselves) will be returned.
        For i = 0 To flood.objectRects.Count - 1
            combinedRects.Add(flood.objectRects(i))
            combinedRects.Add(flood.objectRects(i))
        Next

        Dim epsilon = sliders.TrackBar1.Value / 100
        cv.Cv2.GroupRectangles(combinedRects, 1, epsilon)

        For i = 0 To Math.Min(combinedRects.Count * 2, kalman.input.Count) - 1 Step 4
            Dim rIndex = i / 2
            kalman.input(i) = combinedRects(rIndex).X
            kalman.input(i + 1) = combinedRects(rIndex).Y
            kalman.input(i + 2) = combinedRects(rIndex).Width
            kalman.input(i + 3) = combinedRects(rIndex).Height
        Next
        kalman.Run(ocvb)
        Dim rects As New List(Of cv.Rect)
        For i = 0 To Math.Min(combinedRects.Count * 2, kalman.input.Count) - 1 Step 4
            Dim rect = combinedRects(i / 2)
            rects.Add(New cv.Rect(kalman.output(i), kalman.output(i + 1), kalman.output(i + 2), kalman.output(i + 3)))
        Next

        dst2 = flood.dst2.Resize(ocvb.color.Size())
        If standalone Then
            Dim fontSize As Single = 1.0
            If ocvb.parms.lowResolution Then fontSize = 0.6
            Dim maxDepth = gravity.sliders.TrackBar1.Value
            Dim mmPerPixel = maxDepth / ocvb.color.Height
            For Each rect In rects
                dst2.Rectangle(rect, cv.Scalar.White, 1)
                Dim distanceFromCamera = (ocvb.color.Height - rect.Y - rect.Height) * mmPerPixel
                Dim objectWidth = rect.Width * mmPerPixel
                Dim text = "depth=" + Format(distanceFromCamera / 1000, "#0.0") + "m Width=" + Format(objectWidth / 1000, "#0.0") + " m"

                Dim pt = New cv.Point(rect.X, rect.Y - 10)
                cv.Cv2.PutText(dst2, text, pt, cv.HersheyFonts.HersheyComplexSmall, fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            Next
            label2 = CStr(flood.objectRects.Count) + " objects combined into " + CStr(rects.Count) + " regions > " + CStr(flood.minFloodSize) + " pixels"
        End If
    End Sub
End Class






Public Class Projection_Wall
    Inherits ocvbClass
    Dim objects As Projection_Flood
    Dim lines As lineDetector_FLD_CPP
    Dim dilate As DilateErode_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)

        dilate = New DilateErode_Basics(ocvb, Me.GetType().Name)

        objects = New Projection_Flood(ocvb, Me.GetType().Name)

        lines = New lineDetector_FLD_CPP(ocvb, Me.GetType().Name)

        ocvb.desc = "Use the top down view to detect walls with a line detector algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        objects.Run(ocvb)

        dilate.src = dst2
        dilate.Run(ocvb)

        lines.src = dilate.dst1
        lines.Run(ocvb)
        lines.dst1.CopyTo(dst1)

        label1 = "Top View with lines in red"
        label2 = "Top View output without lines"
    End Sub
End Class


