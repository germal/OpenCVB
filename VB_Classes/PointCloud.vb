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
    Public Function Project_GravityHist_Run(cPtr As IntPtr, xyzPtr As IntPtr, maxZ As Single, rows As Integer, cols As Integer) As IntPtr
    End Function

    Public Class compareAllowIdenticalSingleInverted : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Class compareAllowIdenticalIntegerInverted : Implements IComparer(Of Integer)
        Public Function Compare(ByVal a As Integer, ByVal b As Integer) As Integer Implements IComparer(Of Integer).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Class compareAllowIdenticalInteger : Implements IComparer(Of Integer)
        Public Function Compare(ByVal a As Integer, ByVal b As Integer) As Integer Implements IComparer(Of Integer).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a >= b Then Return 1
            Return -1
        End Function
    End Class
    Public Function findNearestPoint(detailPoint As cv.Point, viewObjects As SortedList(Of Single, viewObject)) As Integer
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
    Public Function findNearestCentroid(detailPoint As cv.Point, centroids As List(Of cv.Point)) As Integer
        Dim minIndex As Integer
        Dim minDistance As Single = Single.MaxValue
        For i = 0 To centroids.Count - 1
            Dim pt = centroids.ElementAt(i)
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
    Inherits VBparent
    Dim palette As Palette_Gradient
    Public rect As cv.Rect
    Public shift As Integer
    Dim centroidRadius As Integer
    Dim arcSize As Integer
    Public startangle As Integer

    Public Function CameraLocationBot(ocvb As VBocvb, dst As cv.Mat, rotationFactor As Single) As cv.Mat
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000
        Dim fsize = fontsize * 1.5
        dst.Circle(topCameraPoint, centroidRadius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        For i = maxZ - 1 To 0 Step -1
            Dim ymeter = CInt(dst.Height * i / (maxZ * rotationFactor))
            dst.Line(New cv.Point(0, ymeter), New cv.Point(dst.Width, ymeter), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst, CStr(maxZ - i) + "m", New cv.Point(10, ymeter - 10), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
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

        Dim shift = (src.Width - src.Height) / 2
        Dim labelLocation = New cv.Point(dst.Width / 2 + shift, dst.Height * 15 / 16)
        cv.Cv2.PutText(dst, "hFOV=" + CStr(180 - startAngle * 2) + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst, CStr(startAngle) + " deg.", New cv.Point(topCameraPoint.X - shift, topCameraPoint.Y - 5), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst, CStr(startAngle) + " deg.", New cv.Point(topCameraPoint.X + shift, topCameraPoint.Y - 5), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        dst.Line(topCameraPoint, fovRight, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        Return dst
    End Function
    Public Function CameraLocationSide(ocvb As VBocvb, ByRef dst As cv.Mat, rotationFactor As Single) As cv.Mat
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000
        Dim fsize = fontsize * 1.5

        Dim shift = (src.Width - src.Height) / 2
        dst.Circle(sideCameraPoint, centroidRadius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        For i = 0 To maxZ
            Dim xmeter = CInt(dst.Height * i / (maxZ * rotationFactor))
            dst.Line(New cv.Point(shift + xmeter, 0), New cv.Point(shift + xmeter, dst.Height), cv.Scalar.AliceBlue, 1)
            cv.Cv2.PutText(dst, CStr(i) + "m", New cv.Point(shift + xmeter + 10, dst.Height - 10), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
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

        Dim labelLocation = New cv.Point(src.Width * 0.02, sideCameraPoint.Y)
        cv.Cv2.PutText(dst, "vFOV=" + CStr(180 - startAngle * 2) + " deg.", labelLocation, cv.HersheyFonts.HersheyComplexSmall, fsize,
                       cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst, CStr(startAngle) + " deg.", New cv.Point(labelLocation.X, sideCameraPoint.Y + shift / 2), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        cv.Cv2.PutText(dst, CStr(startAngle) + " deg.", New cv.Point(labelLocation.X, sideCameraPoint.Y - shift / 2), cv.HersheyFonts.HersheyComplexSmall, fsize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        dst.Line(sideCameraPoint, fovBot, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

        Return dst
    End Function
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

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
    Public Sub Run(ocvb As VBocvb)
    End Sub
End Class




Public Class PointCloud_Raw_CPP
    Inherits VBparent
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
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
    Public Sub Run(ocvb As VBocvb)
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
    Inherits VBparent
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Dim cPtr As IntPtr
    Dim depthBytes() As Byte
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
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
    Public Sub Run(ocvb As VBocvb)
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






Public Class PointCloud_Objects
    Inherits VBparent
    Dim measureSide As PointCloud_Kalman_SideView
    Dim measureTop As PointCloud_Kalman_TopView
    Public measure As Object
    Public viewObjects As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Dim cmats As PointCloud_Colorize
    Public SideViewFlag As Boolean = True
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        cmats = New PointCloud_Colorize(ocvb)
        measureSide = New PointCloud_Kalman_SideView(ocvb)
        measureTop = New PointCloud_Kalman_TopView(ocvb)
        Dim imuCheck = findCheckBox("Use IMU gravity vector")
        imuCheck.Checked = False

        If standalone Then
            sliders.Setup(ocvb, caller, 1)
            sliders.setupTrackBar(0, "Test Bar Distance from camera in mm", 1, 4000, 1500)
        End If
        ocvb.desc = "Validate the formula for pixel height as a function of distance"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim saveSideViewFlag As Boolean
        If measure Is Nothing Or saveSideViewFlag <> SideViewFlag Then
            saveSideViewFlag = SideViewFlag
            measure = If(SideViewFlag, measureSide, measureTop)
        End If
        Static showRectanglesCheck = findCheckBox("Draw rectangle and centroid for each mask")
        Dim drawLines = showRectanglesCheck.checked
        measure.Run(ocvb)
        dst1 = measure.dst1
        label1 = measure.label1

        Dim FOV = If(SideViewFlag, vFOVangles(ocvb.parms.cameraIndex), hFOVangles(ocvb.parms.cameraIndex))

        Dim xpt1 As cv.Point2f, xpt2 As cv.Point2f
        If standalone Then
            Static distanceSlider = findSlider("Test Bar Distance from camera in mm")
            Dim pixeldistance = src.Height * ((distanceSlider.Value / 1000) / measure.maxZ)
            Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * pixeldistance)

            If SideViewFlag Then
                xpt1 = New cv.Point2f(sideCameraPoint.X + pixeldistance, sideCameraPoint.Y - lineHalf)
                xpt2 = New cv.Point2f(sideCameraPoint.X + pixeldistance, sideCameraPoint.Y + lineHalf)
            Else
                xpt1 = New cv.Point2f(topCameraPoint.X - lineHalf, src.Height - pixeldistance)
                xpt2 = New cv.Point2f(topCameraPoint.X + lineHalf, src.Height - pixeldistance)
            End If
            distanceSlider.Maximum = measure.maxZ * 1000
            If drawLines Then dst1.Line(xpt1, xpt2, cv.Scalar.Red, 3)
        End If

        viewObjects.Clear()
        For i = 0 To measure.pTrack.drawRC.viewObjects.Count - 1
            Dim r = measure.pTrack.drawRC.viewObjects.Values(i).rectView

            Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * (src.Height - (r.Y + r.Height)))
            Dim pixeldistance = src.Height - r.Y - r.Height
            xpt1 = New cv.Point2f(topCameraPoint.X - lineHalf, src.Height - pixeldistance)
            xpt2 = New cv.Point2f(topCameraPoint.X + lineHalf, src.Height - pixeldistance)
            Dim coneleft = Math.Max(Math.Max(xpt1.X, r.X), topCameraPoint.X - lineHalf)
            Dim coneRight = Math.Min(Math.Min(xpt2.X, r.X + r.Width), topCameraPoint.X + lineHalf)
            Dim drawPt1 = New cv.Point2f(coneleft, r.Y + r.Height)
            Dim drawpt2 = New cv.Point2f(coneRight, r.Y + r.Height)

            If SideViewFlag Then
                lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * (r.X - sideCameraPoint.X))
                pixeldistance = r.X - sideCameraPoint.X
                xpt1 = New cv.Point2f(sideCameraPoint.X + pixeldistance, sideCameraPoint.Y - lineHalf)
                xpt2 = New cv.Point2f(sideCameraPoint.X + pixeldistance, sideCameraPoint.Y + lineHalf)

                coneleft = Math.Max(Math.Max(xpt1.Y, r.Y), sideCameraPoint.Y - lineHalf)
                coneRight = Math.Min(Math.Min(xpt2.Y, r.Y + r.Height), sideCameraPoint.Y + lineHalf)
                drawPt1 = New cv.Point2f(r.X, coneleft)
                drawpt2 = New cv.Point2f(r.X, coneRight)
            End If

            If lineHalf = 0 Then Continue For
            If drawLines Then dst1.Line(drawPt1, drawpt2, cv.Scalar.Yellow, 3)

            Dim vo = measure.pTrack.drawRC.viewObjects.Values(i)
            Dim addlen As Single = 0
            ' need to add a small amount to the object width in pixels based on the angle to the camera of the back edge
            If SideViewFlag Then
                If Not (sideCameraPoint.Y > r.Y And sideCameraPoint.Y < r.Y + r.Height) Then
                    If r.Y > sideCameraPoint.Y Then
                        addlen = r.Width * (r.Y - sideCameraPoint.Y) / (r.X + r.Width - sideCameraPoint.X)
                        If drawLines Then dst1.Line(New cv.Point2f(r.X, r.Y), New cv.Point2f(r.X, r.Y - addlen), cv.Scalar.Yellow, 3)
                        r = New cv.Rect(r.X, r.Y - addlen, r.Width, coneRight - coneleft - addlen)
                        If coneRight - addlen >= xpt2.Y Then coneRight -= addlen
                    Else
                        addlen = r.Width * (sideCameraPoint.Y - r.Y) / (r.X + r.Width - sideCameraPoint.X)
                        If drawLines Then dst1.Line(New cv.Point2f(r.X, r.Y + r.Height), New cv.Point2f(r.X, r.Y + r.Height + addlen), cv.Scalar.Yellow, 3)
                        r = New cv.Rect(r.X, r.Y + addlen, r.Width, coneRight - coneleft + addlen)
                        coneleft += addlen
                    End If
                End If
                Dim newY = (coneleft - xpt1.Y) * src.Height / (lineHalf * 2)
                Dim newHeight = src.Height * (addlen + coneRight - coneleft) / (lineHalf * 2)
                vo.rectFront = New cv.Rect(r.X, newY, r.Width, newHeight)
            Else
                If Not (topCameraPoint.X > r.X And topCameraPoint.X < r.X + r.Width) Then
                    If r.X > topCameraPoint.X Then
                        addlen = r.Height * Math.Abs(r.X - topCameraPoint.X) / (src.Height - r.Y)
                        If drawLines Then dst1.Line(New cv.Point2f(r.X, r.Y + r.Height), New cv.Point2f(r.X - addlen, r.Y + r.Height), cv.Scalar.Yellow, 3)
                        coneleft -= addlen
                    Else
                        addlen = r.Height * (topCameraPoint.X - (r.X + r.Width)) / (src.Height - r.Y)
                        If drawLines Then dst1.Line(New cv.Point2f(r.X + r.Width, r.Y + r.Height), New cv.Point2f(r.X + r.Width + addlen, r.Y + r.Height), cv.Scalar.Yellow, 3)
                        If coneleft - addlen >= xpt1.X Then coneleft -= addlen
                    End If
                End If
                Dim newX = (coneleft - xpt1.X) * src.Width / (lineHalf * 2)
                Dim newWidth = src.Width * (addlen + coneRight - coneleft) / (lineHalf * 2)
                vo.rectFront = New cv.Rect(newX, r.Y, newWidth, r.Height)
            End If
            viewObjects.Add(vo.rectFront.Width * vo.rectFront.Height, vo)
        Next
        dst1 = If(SideViewFlag, cmats.CameraLocationSide(ocvb, dst1, 1), cmats.CameraLocationBot(ocvb, dst1, 1))
    End Sub
End Class







Public Class PointCloud_Objects_TopView
    Inherits VBparent
    Dim view As PointCloud_Objects
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        view = New PointCloud_Objects(ocvb)
        view.SideViewFlag = False

        ocvb.desc = "Display only the top view of the depth data - with and without the IMU active"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        view.src = src
        view.Run(ocvb)
        dst1 = view.dst1
    End Sub
End Class





Public Class PointCloud_Objects_SideView
    Inherits VBparent
    Dim view As PointCloud_Objects
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        view = New PointCloud_Objects(ocvb)
        view.SideViewFlag = True

        ocvb.desc = "Display only the side view of the depth data - with and without the IMU active"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        view.src = src
        view.Run(ocvb)
        dst1 = view.dst1
    End Sub
End Class






Public Class PointCloud_Kalman_TopView
    Inherits VBparent
    Public pTrack As KNN_PointTracker
    Public flood As FloodFill_8bit
    Public histogram As Histogram_2D_TopView
    Public pixelsPerMeter As Single ' pixels per meter at the distance requested.
    Dim cmats As PointCloud_Colorize
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        cmats = New PointCloud_Colorize(ocvb)
        flood = New FloodFill_8bit(ocvb)
        flood.basics.sliders.trackbar(0).Value = 100
        pTrack = New KNN_PointTracker(ocvb)
        histogram = New Histogram_2D_TopView(ocvb)

        ocvb.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000

        histogram.src = src
        histogram.Run(ocvb)

        Static sliderHistThreshold = findSlider("Histogram threshold")
        flood.src = histogram.histOutput.Threshold(sliderHistThreshold.Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        flood.Run(ocvb)

        If flood.dst1.Channels = 3 Then pTrack.src = flood.dst1 Else pTrack.src = flood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        pTrack.queryPoints = New List(Of cv.Point2f)(flood.basics.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.basics.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.basics.masks)
        pTrack.Run(ocvb)
        dst1 = pTrack.dst1

        Static checkIMU = findCheckBox("Use IMU gravity vector")
        If checkIMU.Checked = False Then dst1 = cmats.CameraLocationBot(ocvb, dst1, 1)
        Dim FOV = hFOVangles(ocvb.parms.cameraIndex)
        Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * src.Height)
        pixelsPerMeter = lineHalf / (Math.Tan(FOV / 2 * 0.0174533) * maxZ)
        label1 = Format(pixelsPerMeter, "0") + " pixels per meter with maxZ at " + Format(maxZ, "0.0") + " meters"
    End Sub
End Class






Public Class PointCloud_Kalman_SideView
    Inherits VBparent
    Public flood As Floodfill_Identifiers
    Public histogram As Histogram_2D_SideView
    Public pTrack As KNN_PointTracker
    Public pixelsPerMeter As Single ' pixels per meter at the distance requested.
    Dim cmats As PointCloud_Colorize
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        cmats = New PointCloud_Colorize(ocvb)
        flood = New Floodfill_Identifiers(ocvb)
        flood.basics.sliders.trackbar(0).Value = 100
        histogram = New Histogram_2D_SideView(ocvb)
        pTrack = New KNN_PointTracker(ocvb)

        ocvb.desc = "Measure each object found in a Centroids view and provide pixel width as well"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000

        histogram.src = src
        histogram.Run(ocvb)

        Static sliderHistThreshold = findSlider("Histogram threshold")
        flood.src = histogram.histOutput.Threshold(sliderHistThreshold.Value, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(255)
        flood.Run(ocvb)

        pTrack.src = flood.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        pTrack.queryPoints = New List(Of cv.Point2f)(flood.centroids)
        pTrack.queryRects = New List(Of cv.Rect)(flood.rects)
        pTrack.queryMasks = New List(Of cv.Mat)(flood.masks)
        pTrack.Run(ocvb)
        dst1 = pTrack.dst1

        Static checkIMU = findCheckBox("Use IMU gravity vector")
        If checkIMU.Checked = False Then dst1 = cmats.CameraLocationSide(ocvb, dst1, 1)
        Dim FOV = vFOVangles(ocvb.parms.cameraIndex)
        Dim lineHalf = CInt(Math.Tan(FOV / 2 * 0.0174533) * src.Height)
        pixelsPerMeter = lineHalf / (Math.Tan(FOV / 2 * 0.0174533) * maxZ)
        label1 = Format(pixelsPerMeter, "0") + " pixels per meter at " + Format(maxZ, "0.0") + " meters"
    End Sub
End Class










Public Class PointCloud_BackProject
    Inherits VBparent
    Dim both As PointCloud_BothViews
    Dim mats As Mat_4to1
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        both = New PointCloud_BothViews(ocvb)
        mats = New Mat_4to1(ocvb)
        label1 = "Click any quadrant below to enlarge it"
        label2 = "Click any centroid to display details"
        ocvb.desc = "Backproject the selected object"
    End Sub

    Public Sub Run(ocvb As VBocvb)
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
    Inherits VBparent
    Public topPixel As PointCloud_Objects
    Public sidePixel As PointCloud_Objects
    Dim levelCheck As IMU_IsCameraLevel
    Public detailText As String
    Public backMat As New cv.Mat
    Public backMatMask As New cv.Mat
    Public vwTop As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Public vwSide As New SortedList(Of Single, viewObject)(New compareAllowIdenticalSingleInverted)
    Dim cmats As PointCloud_Colorize
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        levelCheck = New IMU_IsCameraLevel(ocvb)
        topPixel = New PointCloud_Objects(ocvb)
        topPixel.SideViewFlag = False
        sidePixel = New PointCloud_Objects(ocvb)
        sidePixel.SideViewFlag = True
        cmats = New PointCloud_Colorize(ocvb)

        backMat = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        backMatMask = New cv.Mat(src.Size(), cv.MatType.CV_8UC1)

        ocvb.desc = "Find the actual width in pixels for the objects detected in the top view"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static showRectanglesCheck = findCheckBox("Draw rectangle and centroid for each mask")
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

            Dim pad = CInt(src.Width / 15)
            ocvb.trueText(accMsg1 + vbCrLf + instructions, 10, src.Height - pad)
            ocvb.trueText(accMsg2 + vbCrLf + instructions, 10, src.Height - pad, 3)
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
        Dim vw As New SortedList(Of Single, viewObject)
        Dim topActive = If(standalone, True, (activeView = QUAD0 Or activeView = QUAD2))
        Dim sideActive = If(standalone, True, (activeView = QUAD1 Or activeView = QUAD3))

        Dim widthInfo As String = ""
        If vwTop.Count And topActive Then
            minIndex = findNearestPoint(ocvb.mouseClickPoint, vwTop)
            Dim rView = vwTop.Values(minIndex).rectView
            detailPoint = New cv.Point(CInt(rView.X), CInt(rView.Y))
            Dim rFront = vwTop.Values(minIndex).rectFront

            minDepth = maxZ * (topCameraPoint.Y - rView.Y - rView.Height) / src.Height
            maxDepth = maxZ * (topCameraPoint.Y - rView.Y) / src.Height
            Dim pixelPerMeter = topPixel.measure.pixelsPerMeter
            If pixelPerMeter > 0 Then
                widthInfo = " & " + CStr(rView.Width) + " pixels wide or " + Format(rView.Width / pixelPerMeter, "0.0") + "m"
            End If
            detailText = Format(minDepth, "#0.0") + "-" + Format(maxDepth, "#0.0") + "m" + widthInfo

            roi = New cv.Rect(rFront.X, 0, rFront.Width, src.Height)
            vw = vwTop
            If showDetails Then
                ocvb.trueText(detailText, detailPoint.X, detailPoint.Y, picTag:=If(standalone, 2, 3))
                If standalone Then label1 = "Clicked: " + detailText Else label2 = "Clicked: " + detailText
            End If
        End If

        If vwSide.Count And sideActive Then
            minIndex = findNearestPoint(ocvb.mouseClickPoint, vwSide)
            Dim rView = vwSide.Values(minIndex).rectView
            detailPoint = New cv.Point(CInt(rView.X), CInt(rView.Y))
            Dim rFront = vwSide.Values(minIndex).rectFront
            minDepth = maxZ * (rView.X - sideCameraPoint.X) / src.Height
            maxDepth = maxZ * (rView.X + rView.Width - sideCameraPoint.X) / src.Height

            Dim pixelPerMeter = sidePixel.measure.pixelsPerMeter
            If pixelPerMeter > 0 Then
                widthInfo = " & " + CStr(rView.Width) + " pixels wide or " + Format(rView.Height / pixelPerMeter, "0.0") + "m"
            End If
            detailText = Format(minDepth, "#0.0") + "-" + Format(maxDepth, "#0.0") + "m " + widthInfo

            roi = New cv.Rect(0, rFront.Y, src.Width, rFront.Y + rFront.Height)
            vw = vwSide
            If showDetails Then
                ocvb.trueText(detailText, detailPoint.X, detailPoint.Y, 3)
                label2 = "Clicked: " + detailText
            End If
        End If

        If vw.Count > 0 Then
            If roi.X + roi.Width > src.Width Then roi.Width = src.Width - roi.X
            If roi.Y + roi.Height > src.Height Then roi.Height = src.Height - roi.Y
            If roi.Width > 0 And roi.Height > 0 Then
                backMatMask.SetTo(0)
                cv.Cv2.InRange(depth32f(roi), cv.Scalar.All(minDepth * 1000), cv.Scalar.All(maxDepth * 1000), backMatMask(roi))

                backMat.SetTo(0)
                backMat(roi).SetTo(vw.Values(minIndex).LayoutColor, backMatMask(roi))
            End If
        End If

        Static checkIMU = findCheckBox("Use IMU gravity vector")
        If checkIMU.Checked = False Then
            dst1 = cmats.CameraLocationBot(ocvb, dst1, 1)
            dst2 = cmats.CameraLocationSide(ocvb, dst2, 1)
        End If
    End Sub
End Class





Public Class PointCloud_HistBothViews
    Inherits VBparent
    Dim topView As Histogram_2D_TopView
    Dim sideView As Histogram_2D_SideView
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        topView = New Histogram_2D_TopView(ocvb)
        sideView = New Histogram_2D_SideView(ocvb)

        label1 = "Histogram Top View"
        label2 = "Histogram Side View"
        ocvb.desc = "Show the histogram for both the side and top views"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        topView.src = src
        topView.Run(ocvb)
        dst1 = topView.dst1

        sideView.src = src
        sideView.Run(ocvb)
        dst2 = sideView.dst1
    End Sub
End Class






Public Class PointCloud_FrustrumSide
    Inherits VBparent
    Public fakePC As New cv.Mat
    Dim sideView As Histogram_2D_SideView
    Dim cmats As PointCloud_Colorize
    Dim inRangeSlider As System.Windows.Forms.TrackBar
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sideView = New Histogram_2D_SideView(ocvb)
        Dim histSlider = findSlider("Histogram threshold")
        histSlider.Value = 0
        cmats = New PointCloud_Colorize(ocvb)

        inRangeSlider = findSlider("InRange Max Depth (mm)")
        inRangeSlider.Value = 4362

        ocvb.desc = "Translate only the frustrum with gravity"
    End Sub
    Public Sub Buildfrustrum1(split() As cv.Mat)
        maxZ = inRangeSlider.Value / 1000
        Dim xminVal = -1.84, xmaxVal = 2.63
        Dim yminVal = -2.2, ymaxVal = 0.9

        Dim xIncr = (xmaxVal - xminVal) / split(0).Height
        Dim yIncr = (ymaxVal - yminVal) / (split(0).Height - 100)
        Dim zIncr = maxZ / sideCameraPoint.Y
        For i = 0 To sideCameraPoint.Y - 1
            Dim r = New cv.Rect(i, i, CInt(split(2).Width - i * 2), CInt(sideCameraPoint.Y * 2 - i * 2))
            split(2).Rectangle(r, cv.Scalar.All(maxZ - i * zIncr), 1)
        Next

        For i = 0 To split(0).Width - 1
            split(0).Set(Of Single)(0, i, xminVal + xIncr * i)
        Next
        For i = 1 To split(0).Height - 1
            split(0).Row(0).CopyTo(split(0).Row(i))
        Next

        For i = 0 To split(0).Height - 1
            split(1).Set(Of Single)(i, 0, yminVal + yIncr * i)
        Next
        For i = 1 To split(0).Width - 1
            split(1).Col(0).CopyTo(split(1).Col(i))
        Next

        split(2).ConvertTo(dst2, cv.MatType.CV_8U, 255 / maxZ)
        cv.Cv2.Merge(split, fakePC)
    End Sub
    Public Sub Buildfrustrum(split() As cv.Mat)
        maxZ = inRangeSlider.Value / 1000
        Dim xminVal = -1.84, xmaxVal = 2.63
        Dim yminVal = -2.2, ymaxVal = 0.9

        Dim minval As Double, maxval As Double
        split(0).MinMaxLoc(minval, maxval)
        split(1).MinMaxLoc(minval, maxval)

        Dim xIncr = (xmaxVal - xminVal) / split(0).Width
        Dim yIncr = (ymaxVal - yminVal) / split(0).Height
        Dim zIncr = maxZ / (split(0).Height / 2)

        Dim depth32f = New cv.Mat(split(2).Height, split(2).Height, cv.MatType.CV_32F, 0)
        For i = 0 To depth32f.Height - 1
            Dim r = New cv.Rect(i, i, CInt(depth32f.Width - i * 2), CInt(depth32f.Height - i * 2))
            depth32f.Rectangle(r, cv.Scalar.All(maxZ - i * zIncr), 1)
        Next
        split(2) = depth32f.Resize(split(2).Size)

        'For i = 0 To split(0).Width - 1
        '    split(0).Set(Of Single)(0, i, xminVal + xIncr * i)
        'Next
        'For i = 1 To split(0).Height - 1
        '    split(0).Row(0).CopyTo(split(0).Row(i))
        'Next

        'For i = 0 To split(0).Height - 1
        '    split(1).Set(Of Single)(i, 0, yminVal + yIncr * i)
        'Next
        'For i = 1 To split(0).Width - 1
        '    split(1).Col(0).CopyTo(split(1).Col(i))
        'Next

        split(2).ConvertTo(dst2, cv.MatType.CV_8U, 255 / maxZ)
        cv.Cv2.Merge(split, fakePC)
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim split = ocvb.pointCloud.Split()
        Buildfrustrum(split)

        'split(0).ConvertTo(dst2, cv.MatType.CV_8U, 255 / (xmaxVal - xminVal), -xminVal * 255 / (xmaxVal - xminVal))
        'split(1).ConvertTo(dst1, cv.MatType.CV_8U, 255 / (ymaxVal - yminVal), -yminVal * 255 / (ymaxVal - yminVal))

        sideView.src = fakePC
        sideView.Run(ocvb)
        dst1 = sideView.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR).Resize(src.Size)
        dst1 = cmats.CameraLocationSide(ocvb, dst1, Math.Cos(sideView.gCloudIMU.imu.angleZ))
    End Sub
End Class






Public Class PointCloud_FrustrumTop
    Inherits VBparent
    Public fakePC As New cv.Mat
    Dim frustrum As PointCloud_FrustrumSide
    Dim topView As Histogram_2D_TopView
    Dim cmats As PointCloud_Colorize
    Dim inRangeSlider As System.Windows.Forms.TrackBar
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        frustrum = New PointCloud_FrustrumSide(ocvb)

        topView = New Histogram_2D_TopView(ocvb)
        Dim histSlider = findSlider("Histogram threshold")
        histSlider.Value = 0
        cmats = New PointCloud_Colorize(ocvb)

        inRangeSlider = findSlider("InRange Max Depth (mm)")
        inRangeSlider.Value = 4362
        maxZ = inRangeSlider.Value / 1000

        ocvb.desc = "Translate only the frustrum with gravity"
    End Sub
    Public Sub Buildfrustrum(split() As cv.Mat)
        maxZ = inRangeSlider.Value / 1000
        Dim xminVal = -1.84, xmaxVal = 2.63
        Dim yminVal = -2.2, ymaxVal = 0.9

        'Dim minval As Double, maxval As Double
        'For i = 0 To split(0).Rows - 1
        '    split(0).Row(i).MinMaxLoc(minval, maxval)
        '    Console.WriteLine("min = " + Format(minval, "#0.00") + " max = " + Format(maxval, "#0.00"))
        'Next
        'split(1).MinMaxLoc(minval, maxval)

        Dim xIncr = (xmaxVal - xminVal) / split(0).Width
        Dim yIncr = (ymaxVal - yminVal) / split(0).Height
        Dim zIncr = maxZ / (split(0).Height / 2)

        Dim depth32f = New cv.Mat(split(2).Height, split(2).Height, cv.MatType.CV_32F, 0)
        For i = 0 To depth32f.Height - 1
            Dim r = New cv.Rect(i, i, CInt(depth32f.Width - i * 2), CInt(depth32f.Height - i * 2))
            depth32f.Rectangle(r, cv.Scalar.All(maxZ - i * zIncr), 1)
        Next
        split(2) = depth32f.Resize(split(2).Size)

        'For i = 0 To split(0).Width - 1
        '    split(0).Set(Of Single)(0, i, xminVal + xIncr * i)
        'Next
        'For i = 1 To split(0).Height - 1
        '    split(0).Row(0).CopyTo(split(0).Row(i))
        'Next

        'For i = 0 To split(0).Height - 1
        '    split(1).Set(Of Single)(i, 0, yminVal + yIncr * i)
        'Next
        'For i = 1 To split(0).Width - 1
        '    split(1).Col(0).CopyTo(split(1).Col(i))
        'Next

        split(2).ConvertTo(dst2, cv.MatType.CV_8U, 255 / maxZ)
        cv.Cv2.Merge(split, fakePC)
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim split = ocvb.pointCloud.Split()

        Buildfrustrum(split)

        topView.src = fakePC
        topView.Run(ocvb)
        dst1 = topView.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR).Resize(src.Size)
        dst1 = cmats.CameraLocationBot(ocvb, dst1, 1)
    End Sub
End Class






Public Class PointCloud_DistanceClick
    Inherits VBparent
    Dim inverse As Mat_Inverse
    Dim sideIMU As PointCloud_IMU_SideView
    Dim points As New List(Of cv.Point)
    Dim clicks As New List(Of cv.Point)
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        inverse = New Mat_Inverse(ocvb)
        sideIMU = New PointCloud_IMU_SideView(ocvb)
        label1 = "Click anywhere to get distance from camera and x dist"
        ocvb.desc = "Click to find distance from the camera"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static saveMaxZ As Single
        Static inRangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = inRangeSlider.Value / 1000

        If maxZ <> saveMaxZ Then
            clicks.Clear()
            points.Clear()
            saveMaxZ = maxZ
        End If

        sideIMU.Run(ocvb)
        dst1 = sideIMU.dst1
        dst2 = sideIMU.dst2

        If ocvb.mouseClickFlag Then
            clicks.Add(ocvb.mouseClickPoint)
            Dim invertMat = sideIMU.sideView.gCloudIMU.gInverted
            Dim vec = New cv.Mat(3, 1, cv.MatType.CV_32F, {ocvb.mouseClickPoint.X, ocvb.mouseClickPoint.Y, 0})
            Dim origLoc = (invertMat * vec).ToMat
            Dim newLoc = New cv.Point(CInt(origLoc.Get(Of Integer)(0, 0)), CInt(origLoc.Get(Of Integer)(0, 1)))
            points.Add(newLoc)
        End If

        For Each pt In points
            dst2.Circle(pt, 10, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Next
        For Each pt In clicks
            dst1.Circle(pt, 10, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
            dst2.Circle(pt, 10, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
            Dim pixelsPerMeter = dst2.Height / maxZ
            Dim side1 = (pt.X - sideCameraPoint.X)
            Dim side2 = (pt.Y - sideCameraPoint.Y)
            Dim cameraDistance = Math.Sqrt(side1 * side1 + side2 * side2) / pixelsPerMeter
            ocvb.trueText(Format(cameraDistance, "#0.00") + "m xdist = " + Format(side1 / pixelsPerMeter, "#0.00"), pt)
        Next

        dst1.Circle(sideCameraPoint, 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        dst1.Line(New cv.Point(sideCameraPoint.X, 0), New cv.Point(sideCameraPoint.X, dst1.Height), cv.Scalar.White, 1)
    End Sub
End Class






Public Class PointCloud_IMU_TopView
    Inherits VBparent
    Public topView As Histogram_2D_TopView
    Public kTopView As PointCloud_Kalman_TopView
    Public lDetect As LineDetector_Basics
    Dim rotate As Transform_Rotate
    Dim angleSlider As System.Windows.Forms.TrackBar
    Dim cmats As PointCloud_Colorize
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        cmats = New PointCloud_Colorize(ocvb)
        rotate = New Transform_Rotate(ocvb)
        angleSlider = findSlider("Angle")
        angleSlider.Value = 0

        kTopView = New PointCloud_Kalman_TopView(ocvb)
        topView = New Histogram_2D_TopView(ocvb)
        Dim reductionRadio = findRadio("No reduction")
        reductionRadio.Checked = True

        Dim histSlider = findSlider("Histogram threshold")
        histSlider.Value = 20

        lDetect = New LineDetector_Basics(ocvb)
        label1 = "Top view aligned using the IMU gravity vector"
        label2 = "Top view aligned without using the IMU gravity vector"
        ocvb.desc = "Present the top view with and without the IMU filter."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static imuCheck = findCheckBox("Use IMU gravity vector")
        imuCheck.checked = True
        topView.Run(ocvb)
        dst1 = topView.dst1.Clone()
        lDetect.src = topView.dst1.Resize(src.Size).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        lDetect.Run(ocvb)
        dst1 = lDetect.dst1

        Dim angle = angleSlider.Value
        Static xSlider = findSlider("Rotation center X")
        Static ySlider = findSlider("Rotation center Y")
        xSlider.value = topCameraPoint.X
        ySlider.value = topCameraPoint.Y
        rotate.src = dst1
        rotate.Run(ocvb)
        dst1 = rotate.dst1
        dst1 = cmats.CameraLocationBot(ocvb, dst1, Math.Cos(topView.gCloudIMU.imu.angleZ))

        imuCheck.checked = False
        kTopView.Run(ocvb)
        dst2 = kTopView.dst1
    End Sub
End Class







Public Class PointCloud_IMU_SideView
    Inherits VBparent
    Public sideView As Histogram_2D_SideView
    Public kSideView As PointCloud_Kalman_SideView
    Public lDetect As LineDetector_Basics
    Dim cmats As PointCloud_Colorize
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        lDetect = New LineDetector_Basics(ocvb)
        cmats = New PointCloud_Colorize(ocvb)
        kSideView = New PointCloud_Kalman_SideView(ocvb)
        sideView = New Histogram_2D_SideView(ocvb)
        Dim reductionRadio = findRadio("No reduction")
        reductionRadio.Checked = True

        Dim histSlider = findSlider("Histogram threshold")
        histSlider.Value = 20

        label1 = "side view aligned using the IMU gravity vector"
        label2 = "side view aligned without using the IMU gravity vector"
        ocvb.desc = "Present the side view with and without the IMU filter."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static imuCheck = findCheckBox("Use IMU gravity vector")
        imuCheck.checked = True
        sideView.Run(ocvb)
        dst1 = sideView.dst1.Clone()
        lDetect.src = sideView.dst1.Resize(ocvb.color.Size).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        lDetect.Run(ocvb)
        dst1 = lDetect.dst1
        dst1 = cmats.CameraLocationSide(ocvb, dst1, Math.Cos(sideView.gCloudIMU.imu.angleZ))

        imuCheck.checked = False
        kSideView.Run(ocvb)
        dst2 = kSideView.dst1
    End Sub
End Class






Public Class PointCloud_FindFloor
    Inherits VBparent
    Dim sideIMU As PointCloud_IMU_SideView
    Public floorRun As Boolean = True ' the default is to look for a floor...  Set to False to look for ceiling....
    Public gleftPoint As cv.Point2f
    Public grightPoint As cv.Point2f
    Dim kalman As Kalman_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        kalman = New Kalman_Basics(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Threshold for length of line", 1, 50, 10)
        sliders.setupTrackBar(1, "Threshold for y-displacement of line", 1, 50, 5)
        sideIMU = New PointCloud_IMU_SideView(ocvb)

        hideForm("Histogram_ProjectOptions CheckBox Options")
        label1 = "Side View oriented toward gravity"
        label2 = "Side View without gravity vector"
        ocvb.desc = "Find the floor in a side view oriented by gravity vector"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim input = src
        If input.Type <> cv.MatType.CV_32FC3 Then input = ocvb.pointCloud

        Static saveFrameCount = -1
        If saveFrameCount <> ocvb.frameCount Then
            saveFrameCount = ocvb.frameCount
            sideIMU.src = input
            sideIMU.Run(ocvb)
            dst1 = sideIMU.dst1
            dst2 = sideIMU.dst2
            Dim gInverted = sideIMU.sideView.gCloudIMU.gInverted
            Dim lines = sideIMU.lDetect.lines
        End If

        Static angleSlider = findSlider("Threshold for y-displacement of line")
        Static lenSlider = findSlider("Threshold for length of line")
        Dim angleTest = angleSlider.value
        Dim lengthTest = lenSlider.value

        If sideIMU.lDetect.lines.Count > 0 Then
            Dim leftPoints As New List(Of cv.Point2f)
            Dim rightPoints As New List(Of cv.Point2f)
            Dim sortedLines = New SortedList(Of Integer, cv.Vec4f)(New compareAllowIdenticalIntegerInverted)
            If floorRun = False Then sortedLines = New SortedList(Of Integer, cv.Vec4f)(New compareAllowIdenticalInteger)
            For Each line In sideIMU.lDetect.lines
                sortedLines.Add(line.Item1, line)
            Next
            For i = 0 To sortedLines.Count - 1
                Dim line = sortedLines.ElementAt(i).Value
                Dim pf1 = New cv.Point2f(line.Item0, line.Item1)
                Dim pf2 = New cv.Point2f(line.Item2, line.Item3)
                If Math.Abs(pf1.X - pf2.X) > lengthTest And Math.Abs(pf1.Y - pf2.Y) < angleTest Then
                    If pf1.X < pf2.X Then
                        leftPoints.Add(pf1)
                        rightPoints.Add(pf2)
                    Else
                        leftPoints.Add(pf2)
                        rightPoints.Add(pf1)
                    End If
                Else
                    Exit For
                End If
            Next

            If leftPoints.Count > 0 Then
                Dim leftMat = New cv.Mat(leftPoints.Count, 1, cv.MatType.CV_32FC2, leftPoints.ToArray)
                Dim rightMat = New cv.Mat(rightPoints.Count, 1, cv.MatType.CV_32FC2, rightPoints.ToArray)
                Dim meanLeft = leftMat.Mean()
                Dim meanRight = rightMat.Mean()
                gleftPoint = New cv.Point2f(meanLeft.Item(0) - 50, meanLeft.Item(1))
                grightPoint = New cv.Point2f(meanLeft.Item(0) + 200, meanRight.Item(1))

                kalman.input(0) = gleftPoint.X
                kalman.Run(ocvb)
                gleftPoint.X = kalman.output(0)
                grightPoint.X = kalman.output(0) + 200

                dst1.Line(gleftPoint, grightPoint, cv.Scalar.Yellow, 4, cv.LineTypes.AntiAlias)
            End If
        End If
    End Sub
End Class







Public Class PointCloud_FindCeiling
    Inherits VBparent
    Public floor As PointCloud_FindFloor
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        floor = New PointCloud_FindFloor(ocvb)
        floor.floorRun = False ' we are looking for ceilings.
        label1 = floor.label1
        label2 = floor.label2
        ocvb.desc = "Find the Ceiling in a side view oriented by gravity vector"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        floor.Run(ocvb)

        dst1 = floor.dst1
        dst2 = floor.dst2
    End Sub
End Class








Public Class PointCloud_FindCeilingAndFloor
    Inherits VBparent
    Public floor As PointCloud_FindFloor
    Public floorLeft As cv.Point2f
    Public floorRight As cv.Point2f
    Public ceilingLeft As cv.Point2f
    Public ceilingRight As cv.Point2f
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        floor = New PointCloud_FindFloor(ocvb)
        label1 = floor.label1
        label2 = floor.label2
        ocvb.desc = "Find the Ceiling in a side view oriented by gravity vector"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        floor.floorRun = True ' we are looking for ceilings.
        floor.Run(ocvb)
        floorLeft = floor.gleftPoint
        floorRight = floor.grightPoint

        dst1 = floor.dst1.Clone
        dst2 = floor.dst2.Clone

        floor.floorRun = False ' we are looking for ceilings.
        floor.Run(ocvb)
        ceilingLeft = floor.gleftPoint
        ceilingRight = floor.grightPoint

        dst1.Line(floor.gleftPoint, floor.grightPoint, cv.Scalar.Yellow, 4, cv.LineTypes.AntiAlias)
    End Sub
End Class






Public Class PointCloud_FindFloorInverse
    Inherits VBparent
    Dim floor As PointCloud_FindCeilingAndFloor
    Dim inverse As Mat_Inverse
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        floor = New PointCloud_FindCeilingAndFloor(ocvb)
        inverse = New Mat_Inverse(ocvb)

        ocvb.desc = "Find the floor and ceiling in the unrotated histogram using the inverse transform."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        floor.Run(ocvb)
        dst1 = floor.dst1
        dst2 = floor.dst2
    End Sub
End Class