Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class LineDetector_Basics
    Inherits VBparent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lines As cv.Vec4f()
    Public drawLines = False
    Public sortlines As New SortedList(Of Integer, cv.Vec4f)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Line thickness", 1, 20, 2)
            sliders.setupTrackBar(1, "Line length threshold (mm)", 1, 2000, 100) ' not used in Run below but externally...
            sliders.setupTrackBar(2, "Line length threshold in pixels", 1, src.Width + src.Height, 50)
            sliders.setupTrackBar(3, "Depth search radius in pixels", 1, 20, 2) ' not used in Run below but externally...
        End If
        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        label1 = "Yellow > length threshold, red < length threshold"
        task.desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines present."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        lines = ld.Detect(src)
        src.CopyTo(dst2)
        dst1 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Static thicknessSlider = findSlider("Line thickness")
        Dim thickness = thicknessSlider.Value
        Static pixelSlider = findSlider("Line length threshold in pixels")
        Dim pixelThreshold = pixelSlider.value

        sortlines.Clear()

        For Each v In lines
            If v(0) >= 0 And v(0) <= dst1.Cols And v(1) >= 0 And v(1) <= dst1.Rows And
                   v(2) >= 0 And v(2) <= dst1.Cols And v(3) >= 0 And v(3) <= dst1.Rows Then
                Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                dst1.Line(pt1, pt2, cv.Scalar.Red, thickness, cv.LineTypes.AntiAlias)
                Dim pixelLen = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                If pixelLen > pixelThreshold Then
                    dst1.Line(pt1, pt2, cv.Scalar.Yellow, thickness, cv.LineTypes.AntiAlias)
                    sortlines.Add(pixelLen, v)
                End If
            End If
        Next
        If standalone Or drawLines Then
            label2 = "Drawn with DrawSegment (thickness=1)"
            ld.DrawSegments(dst2, lines, False)
        End If
    End Sub
End Class






Module fastLineDetector_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function lineDetectorFast_Run(image As IntPtr, rows As Integer, cols As Integer, length_threshold As Integer, distance_threshold As Single, canny_th1 As Integer, canny_th2 As Integer,
                                             canny_aperture_size As Integer, do_merge As Boolean) As Integer
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function lineDetector_Run(image As IntPtr, rows As Integer, cols As Integer) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function lineDetector_Lines() As IntPtr
    End Function

    Public Sub find3DLineSegment( dst2 As cv.Mat, _mask As cv.Mat, _depth32f As cv.Mat, aa As cv.Vec6f, maskLineWidth As Integer)
        Dim pt1 = New cv.Point(aa(0), aa(1))
        Dim pt2 = New cv.Point(aa(2), aa(3))
        Dim centerPoint = New cv.Point((aa(0) + aa(2)) / 2, (aa(1) + aa(3)) / 2)
        _mask.Line(pt1, pt2, New cv.Scalar(1), maskLineWidth, cv.LineTypes.AntiAlias)
        dst2.Line(pt1, pt2, cv.Scalar.Red, 3, cv.LineTypes.AntiAlias)

        Dim roi = New cv.Rect(Math.Min(aa(0), aa(2)), Math.Min(aa(1), aa(3)), Math.Abs(aa(0) - aa(2)), Math.Abs(aa(1) - aa(3)))

        Dim worldDepth As New List(Of cv.Vec6f)
        If roi.Width = 0 Then roi.Width = 1
        If roi.Height = 0 Then roi.Height = 1
        If roi.X + roi.Width >= _mask.Width Then roi.Width = _mask.Width - roi.X - 1
        If roi.Y + roi.Height >= _mask.Height Then roi.Height = _mask.Height - roi.Y - 1
        Dim mask = _mask(roi).Clone()
        Dim depth32f = _depth32f(roi).Clone()
        Dim totalPoints As Integer
        Dim skipPoints As Integer
        For y = 0 To roi.Height - 1
            For x = 0 To roi.Width - 1
                If mask.Get(Of Byte)(y, x) = 1 Then
                    totalPoints += 1
                    Dim w = getWorldCoordinatesD6(New cv.Point3f(x + roi.X, y + roi.Y, depth32f.Get(Of Single)(y, x)))
                    worldDepth.Add(w)
                End If
            Next
        Next
        Dim endPoints(2) As cv.Vec6f
        ' we need more than a few points...so 50
        If worldDepth.Count > 50 Then
            endPoints = segment3D(worldDepth, skipPoints)

            ' if the sample is large enough (at least 20% of possible points), then project the line for the full length of the RGB line.
            ' Note: when using RGB to determine a projected length, the line is defined by pixel coordinate for y. (z_depth = m * y_pixel + bb)
            If skipPoints / totalPoints < 0.5 Then
                If endPoints(0).Item2 = endPoints(1).Item2 Then endPoints(0).Item2 += 1 ' prevent NaN
                Dim m = (endPoints(0).Item4 - endPoints(1).Item4) / (endPoints(0).Item2 - endPoints(1).Item2)
                Dim bb = endPoints(0).Item2 - m * endPoints(0).Item4
                endPoints(0) = worldDepth(0)
                endPoints(0).Item2 = m * pt1.Y + bb
                endPoints(1) = worldDepth(worldDepth.Count - 1)
                endPoints(1).Item2 = m * pt2.Y + bb
            End If

            ' we need more than a few points...so 10
            Dim zero = New cv.Vec6f(0, 0, 0, 0, 0, 0)
            If endPoints(0) <> zero And endPoints(1) <> zero Then
                Dim b = endPoints(0)
                Dim d = endPoints(1)
                Dim lenBD = Math.Sqrt((b.Item0 - d.Item0) * (b.Item0 - d.Item0) + (b.Item1 - d.Item1) * (b.Item1 - d.Item1) + (b.Item2 - d.Item2) * (b.Item2 - d.Item2))
                cv.Cv2.PutText(dst2, Format(lenBD / 1000, "0.00") + "m", centerPoint, cv.HersheyFonts.HersheyTriplex, 0.4, cv.Scalar.White, 1,
                                   cv.LineTypes.AntiAlias)
                If endPoints(0).Item2 = endPoints(1).Item2 Then endPoints(0).Item2 += 1 ' prevent NaN
                cv.Cv2.PutText(dst2, Format((endPoints(1).Item1 - endPoints(0).Item1) / (endPoints(1).Item2 - endPoints(0).Item2), "0.00") + "y/z",
                                   New cv.Point(centerPoint.X, centerPoint.Y + 10), cv.HersheyFonts.HersheyTriplex, 0.4, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                ' show the final endpoints in xy projection.
                dst2.Circle(New cv.Point(b.Item3, b.Item4), 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                dst2.Circle(New cv.Point(d.Item3, d.Item4), 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
            End If
        End If
    End Sub
    Public Function segment3D(worldDepth As List(Of cv.Vec6f), ByRef skipPoints As Integer) As cv.Vec6f()
        ' by construction, x and y are already on a line.  Compute the average z delta.  Eliminate outliers with that average.
        Dim sum As Double = 0
        Dim midPoint As Double
        For i = 1 To worldDepth.Count - 1
            midPoint += worldDepth(i).Item2
            sum += Math.Abs(worldDepth(i).Item2 - worldDepth(i - 1).Item2)
        Next
        Dim avgDelta = sum / worldDepth.Count * 3
        midPoint /= worldDepth.Count
        Dim midIndex As Integer = -1
        ' find a point which is certain to be on the line - something close the centroid
        For i = worldDepth.Count / 4 To worldDepth.Count - 1
            If Math.Abs(worldDepth(i).Item2 - midPoint) < avgDelta Then
                midIndex = i
                Exit For
            End If
        Next
        Dim endPoints(2) As cv.Vec6f
        If midIndex > 0 Then
            endPoints(0) = worldDepth(midIndex) ' we start with a known centroid on the line.
            Dim delta As Single
            For i = midIndex - 1 To 1 Step -1
                delta = Math.Abs(endPoints(0).Item2 - worldDepth(i).Item2)
                If delta < avgDelta Then endPoints(0) = worldDepth(i) Else skipPoints += 1
            Next

            endPoints(1) = worldDepth(midIndex) ' we start with a known good point on the line.
            For i = midIndex + 1 To worldDepth.Count - 2
                delta = Math.Abs(endPoints(1).Item2 - worldDepth(i).Item2)
                If delta < avgDelta Then endPoints(1) = worldDepth(i) Else skipPoints += 1
            Next
        End If
        Return endPoints
    End Function

    Public Class CompareVec6f : Implements IComparer(Of cv.Vec6f)
        Public Function Compare(ByVal a As cv.Vec6f, ByVal b As cv.Vec6f) As Integer Implements IComparer(Of cv.Vec6f).Compare
            If a(4) > b(4) Then Return 1
            Return -1 ' never returns equal because the lines are always distinct but may have equal length
        End Function
    End Class

    ' there is a drawsegments in the contrib library but this code will operate on the full size of the image - not the small copy passed to the C++ code
    ' But, more importantly, this code uses anti-alias for the lines.  It adds the lines to a mask that may be useful with depth data.
    Public Function drawSegments(dst1 As cv.Mat, lineCount As Integer, thickness As Integer, drawLines As Boolean, ByRef lineMat As cv.Mat) As SortedList(Of cv.Vec6f, Integer)
        Dim sortedLines As New SortedList(Of cv.Vec6f, Integer)(New CompareVec6f)

        Dim lines(lineCount * 4 - 1) As Single
        Dim linePtr = lineDetector_Lines()
        If linePtr = 0 Then Return Nothing ' it happened!
        Marshal.Copy(linePtr, lines, 0, lines.Length)

        lineMat = New cv.Mat(lineCount, 1, cv.MatType.CV_32FC4, lines)
        Dim v6 As New cv.Vec6f
        For i = 0 To lineCount - 1
            Dim v = lineMat.Get(Of cv.Vec4f)(i)
            ' make sure that none are negative - how could any be negative?  Usually just fractionally less than zero.
            For j = 0 To 3
                If v(j) < 0 Then v(j) = 0
            Next

            v6(0) = v(0)
            v6(1) = v(1)
            v6(2) = v(2)
            v6(3) = v(3)
            v6(4) = Math.Sqrt((v(0) - v(2)) * (v(0) - v(2)) + (v(1) - v(3)) * (v(1) - v(3))) ' vector carries the length in pixels with it.
            v6(5) = 0 ' unused...

            ' add this line to the sorted list
            If sortedLines.ContainsKey(v6) Then
                sortedLines(v6) = sortedLines(v6) + 1
            Else
                sortedLines.Add(v6, 1)
            End If
        Next

        If drawLines Then
            For i = sortedLines.Count - 1 To 0 Step -1
                Dim v = sortedLines.ElementAt(i).Key
                If v(0) >= 0 And v(0) <= dst1.Cols And v(1) >= 0 And v(1) <= dst1.Rows And v(2) >= 0 And v(2) <= dst1.Cols And v(3) >= 0 And v(3) <= dst1.Rows Then
                    Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                    Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                    dst1.Line(pt1, pt2, cv.Scalar.Red, thickness, cv.LineTypes.AntiAlias)
                End If
            Next
        End If
        Return sortedLines
    End Function
End Module




' https://docs.opencv.org/3.4.3/d1/d9e/fld_lines_8cpp-example.html
Public Class lineDetector_FLD_CPP
    Inherits VBparent
    Public sortedLines As New SortedList(Of cv.Vec6f, Integer)
    Public lineMat As New cv.Mat
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 6)
            sliders.setupTrackBar(0, "FLD - Min Length", 1, 200, 30)
            sliders.setupTrackBar(1, "FLD - max distance", 1, 100, 14)
            sliders.setupTrackBar(2, "FLD - Canny Aperture", 3, 7, 7)
            sliders.setupTrackBar(3, "FLD - Line Thickness", 1, 7, 3)
            sliders.setupTrackBar(4, "FLD - canny Threshold1", 1, 100, 50)
            sliders.setupTrackBar(5, "FLD - canny Threshold2", 1, 100, 50)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "FLD - incremental merge"
            check.Box(0).Checked = True
            check.Box(1).Text = "FLD - Draw lines on input image"
            check.Box(1).Checked = True
        End If
        task.desc = "Basics for a Fast Line Detector"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        sortedLines.Clear()

        Dim length_threshold = sliders.trackbar(0).Value
        Dim distance_threshold = sliders.trackbar(1).Value / 10
        Dim canny_aperture_size = sliders.trackbar(2).Value
        If canny_aperture_size Mod 2 = 0 Then canny_aperture_size += 1
        Dim canny_th1 = sliders.trackbar(4).Value
        Dim canny_th2 = sliders.trackbar(5).Value
        Dim do_merge = check.Box(0).Checked

        src.CopyTo(dst1)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim data(src.Total - 1) As Byte

        Marshal.Copy(src.Data, data, 0, data.Length)
        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        Dim lineCount = lineDetectorFast_Run(handle.AddrOfPinnedObject, src.Height, src.Width, length_threshold, distance_threshold, canny_th1, canny_th2, canny_aperture_size, do_merge)
        handle.Free()

        If lineCount > 0 Then sortedLines = drawSegments(dst1, lineCount, sliders.trackbar(3).Value, check.Box(1).Checked, lineMat)
    End Sub
End Class






Public Class LineDetector_3D_LongestLine
    Inherits VBparent
    Dim lines As lineDetector_FLD_CPP
    Public Sub New()
        initParent()
        lines = New lineDetector_FLD_CPP()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Mask Line Width", 1, 20, 1)
            sliders.setupTrackBar(1, "Update frequency (in frames)", 1, 100, 1)
        End If

        task.desc = "Identify planes using the lines present in the rgb image."
        label2 = ""
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount Mod sliders.trackbar(1).Value Then Exit Sub
        lines.src = src
        lines.Run()
        src.CopyTo(dst1)

        If lines.sortedLines.Count > 0 Then
            ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will get full length.
            Dim maskLineWidth As Integer = sliders.trackbar(0).Value
            Dim mask = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, 0)
            find3DLineSegment(dst1, mask, task.depth32f, lines.sortedLines.ElementAt(lines.sortedLines.Count - 1).Key, maskLineWidth)
        End If
    End Sub
End Class




Public Class LineDetector_3D_FLD_MT
    Inherits VBparent
    Dim lines As lineDetector_FLD_CPP
    Public Sub New()
        initParent()
        lines = New lineDetector_FLD_CPP()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Mask Line Width", 1, 20, 1)
            sliders.setupTrackBar(1, "Update frequency (in frames)", 1, 100, 1)
        End If
        task.desc = "Measure 3d line segments using a multi-threaded Fast Line Detector."
        label2 = ""
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount Mod sliders.trackbar(1).Value Then Exit Sub
        lines.src = src
        lines.Run()
        src.CopyTo(dst1)

        ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will get full length.
        Dim maskLineWidth As Integer = sliders.trackbar(0).Value
        Dim mask = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, 0)
        Dim lineCount = Math.Max(lines.sortedLines.Count - 20, 0)
        Parallel.For(lineCount, lines.sortedLines.Count,
            Sub(i)
                find3DLineSegment(dst1, mask, task.depth32f, lines.sortedLines.ElementAt(i).Key, maskLineWidth)
            End Sub)
        label1 = "Showing the " + CStr(Math.Min(lines.sortedLines.Count, 20)) + " longest lines out of " + CStr(lines.sortedLines.Count)
    End Sub
End Class




Public Class LineDetector_3D_FitLineZ
    Inherits VBparent
    Dim linesFLD As lineDetector_FLD_CPP
    Public Sub New()
        initParent()
        linesFLD = New lineDetector_FLD_CPP()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Mask Line Width", 1, 20, 3)
            sliders.setupTrackBar(1, "Point count threshold", 5, 500, 50)
            sliders.setupTrackBar(2, "Update frequency (in frames)", 1, 100, 1)
        End If
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "Fitline using x and z (unchecked it will use y and z)"
            check.Box(1).Text = "Display only the longest line"
            check.Box(1).Checked = True
        End If

        task.desc = "Use Fitline with the sparse Z data and X or Y (in RGB pixels)."
        label2 = ""
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount Mod sliders.trackbar(2).Value Then Exit Sub
        Dim useX As Boolean = check.Box(0).Checked
        linesFLD.src = src
        linesFLD.Run()
        src.CopyTo(dst1)

        Dim sortedlines As SortedList(Of cv.Vec6f, Integer)
        sortedlines = linesFLD.sortedLines

        If sortedlines.Count > 0 Then
            ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will likely get full length.
            Dim maskLineWidth As Integer = sliders.trackbar(0).Value
            Dim mask = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, 0)

            Dim longestLineOnly As Boolean = check.Box(1).Checked
            Dim pointCountThreshold = sliders.trackbar(1).Value
            Parallel.For(0, sortedlines.Count,
                Sub(i)
                    If longestLineOnly And i < sortedlines.Count - 1 Then Exit Sub
                    Dim aa = sortedlines.ElementAt(i).Key
                    Dim pt1 = New cv.Point(aa(0), aa(1))
                    Dim pt2 = New cv.Point(aa(2), aa(3))
                    dst1.Line(pt1, pt2, cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
                    mask.Line(pt1, pt2, New cv.Scalar(i), maskLineWidth, cv.LineTypes.AntiAlias)

                    Dim roi = New cv.Rect(Math.Min(aa(0), aa(2)), Math.Min(aa(1), aa(3)), Math.Abs(aa(0) - aa(2)), Math.Abs(aa(1) - aa(3)))

                    Dim worldDepth As New List(Of cv.Vec6f)
                    If roi.Width = 0 Then roi.Width = 1
                    If roi.Height = 0 Then roi.Height = 1
                    If roi.X + roi.Width >= mask.Width Then roi.Width = mask.Width - roi.X - 1
                    If roi.Y + roi.Height >= mask.Height Then roi.Height = mask.Height - roi.Y - 1

                    Dim _mask = mask(roi).Clone()
                    Dim points As New List(Of cv.Point2f)
                    For y = 0 To roi.Height - 1
                        For x = 0 To roi.Width - 1
                            If _mask.Get(Of Byte)(y, x) = i Then
                                Dim w = getWorldCoordinatesD6(New cv.Point3f(x + roi.X, y + roi.Y, task.depth32f.Get(Of Single)(y, x)))
                                points.Add(New cv.Point(If(useX, w.Item0, w.Item1), w.Item2))
                                worldDepth.Add(w)
                            End If
                        Next
                    Next

                    ' without a sufficient number of points, the results can vary widely.
                    If points.Count < pointCountThreshold Then Exit Sub

                    Dim line = cv.Cv2.FitLine(points, cv.DistanceTypes.L2, 1, 0.01, 0.01)
                    Dim mm = line.Vy / line.Vx
                    Dim bb = line.Y1 - mm * line.X1
                    Dim endPoints(2) As cv.Vec6f
                    Dim lastW = worldDepth.Count - 1
                    endPoints(0) = worldDepth(0)
                    endPoints(1) = worldDepth(lastW)
                    endPoints(0).Item2 = bb + mm * If(useX, worldDepth(0).Item3, worldDepth(0).Item4)
                    endPoints(1).Item2 = bb + mm * If(useX, worldDepth(lastW).Item3, worldDepth(lastW).Item4)

                    Dim b = endPoints(0)
                    Dim d = endPoints(1)
                    Dim lenBD = Math.Sqrt((b.Item0 - d.Item0) * (b.Item0 - d.Item0) + (b.Item1 - d.Item1) * (b.Item1 - d.Item1) + (b.Item2 - d.Item2) * (b.Item2 - d.Item2))

                    Dim ptIndex = (i / sortedlines.Count) * (worldDepth.Count - 1)
                    Dim textPoint = New cv.Point(worldDepth(ptIndex).Item3, worldDepth(ptIndex).Item4)
                    If textPoint.X > mask.Width - 50 Then textPoint.X = mask.Width - 50
                    If textPoint.Y > mask.Height - 50 Then textPoint.Y = mask.Height - 50
                    cv.Cv2.PutText(dst1, Format(lenBD / 1000, "#0.00") + "m", textPoint, cv.HersheyFonts.HersheyComplexSmall, 0.5, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                    If endPoints(0).Item2 = endPoints(1).Item2 Then endPoints(0).Item2 += 1 ' prevent NaN
                    cv.Cv2.PutText(dst1, Format((endPoints(1).Item1 - endPoints(0).Item1) / (endPoints(1).Item2 - endPoints(0).Item2), "#0.00") + If(useX, "x/z", "y/z"),
                                    New cv.Point(textPoint.X, textPoint.Y + 10), cv.HersheyFonts.HersheyComplexSmall, 0.5, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

                    ' show the final endpoints in xy projection.
                    dst1.Circle(New cv.Point(b.Item3, b.Item4), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                    dst1.Circle(New cv.Point(d.Item3, d.Item4), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                End Sub)
        End If
    End Sub
End Class





' https://docs.opencv.org/3.4.3/d1/d9e/fld_lines_8cpp-example.html
Public Class lineDetector_FLD
    Inherits VBparent
    Public lines As New List(Of cv.Vec4f)
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller, 6)
            sliders.setupTrackBar(0, "FLD - Min Length", 1, 200, 30)
            sliders.setupTrackBar(1, "FLD - max distance", 1, 100, 14)
            sliders.setupTrackBar(2, "FLD - Canny Aperture", 3, 7, 7)
            sliders.setupTrackBar(3, "FLD - Line Thickness", 1, 7, 3)
            sliders.setupTrackBar(4, "FLD - canny Threshold1", 1, 100, 50)
            sliders.setupTrackBar(5, "FLD - canny Threshold2", 1, 100, 50)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "FLD - incremental merge"
            check.Box(0).Checked = True
        End If
        task.desc = "A Fast Line Detector"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        lines.Clear()

        Dim length_threshold = sliders.trackbar(0).Value
        Dim distance_threshold = sliders.trackbar(1).Value / 10
        Dim canny_aperture_size = sliders.trackbar(2).Value
        If canny_aperture_size Mod 2 = 0 Then canny_aperture_size += 1
        Dim canny_th1 = sliders.trackbar(4).Value
        Dim canny_th2 = sliders.trackbar(5).Value
        Dim do_merge = check.Box(0).Checked

        src.CopyTo(dst1)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim cols = src.Width
        Dim rows = src.Height
        Dim data(src.Total - 1) As Byte

        Marshal.Copy(src.Data, data, 0, data.Length)
        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        Dim lineCount = lineDetectorFast_Run(handle.AddrOfPinnedObject, rows, cols, length_threshold, distance_threshold, canny_th1, canny_th2, canny_aperture_size, do_merge)
        handle.Free()

        If lineCount > 0 Then
            Dim pts(4 * lineCount - 1) As Single
            Dim linePtr = lineDetector_Lines()
            If linePtr <> 0 Then
                Marshal.Copy(linePtr, pts, 0, pts.Length)
                For i = 0 To lineCount - 1
                    lines.Add(New cv.Vec4f(pts(i), pts(i + 1), pts(i + 2), pts(i + 3)))
                Next
            End If
        End If
        If standalone or task.intermediateReview = caller Then
            For j = 0 To lines.Count - 1 Step 4
                Dim v = lines(j)
                Dim pt1 = New cv.Point(v(0), v(1))
                dst1.Line(New cv.Point(v(0), v(1)), New cv.Point(v(2), v(3)), cv.Scalar.Red, 3, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
End Class





Public Class LineDetector_LongLines
    Inherits VBparent
    Public lDetect As LineDetector_Basics
    Public sortlines As New SortedList(Of Integer, cv.Vec4f)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        initParent()
        lDetect = New LineDetector_Basics()

        label1 = "Longest lines in pixels (yellow)"
        task.desc = "Find and measure the longest x number of lines in actual length (not in pixels)"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        lDetect.src = src
        lDetect.Run()
        dst1 = src.Clone

        Static thicknessSlider = findSlider("Line thickness")
        Dim thickness = thicknessSlider.value
        Static pixelSlider = findSlider("Line length threshold in pixels")
        Dim pixelThreshold = pixelSlider.value

        sortlines.Clear()
        For Each v In lDetect.lines
            If v(0) >= 0 And v(0) <= dst1.Cols And v(1) >= 0 And v(1) <= dst1.Rows And
                   v(2) >= 0 And v(2) <= dst1.Cols And v(3) >= 0 And v(3) <= dst1.Rows Then
                Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                Dim pixelLen = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                If pixelLen > pixelThreshold Then
                    dst1.Line(pt1, pt2, cv.Scalar.Yellow, thickness, cv.LineTypes.AntiAlias)
                    sortlines.Add(pixelLen, v)
                End If
            End If
        Next
    End Sub
End Class






Public Class LineDetector_Reduction
    Inherits VBparent
    Dim lDetect As LineDetector_Basics
    Dim reduction As Reduction_Basics
    Public Sub New()
        initParent()
        lDetect = New LineDetector_Basics()
        reduction = New Reduction_Basics()
        reduction.radio.check(0).Checked = True

        label1 = "Output of line detection using reduced input"
        label2 = "Output of reduction_basics"
        task.desc = "Use the reduced rgb image as input to the line detector"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        reduction.src = src
        reduction.Run()
        dst2 = reduction.dst1

        lDetect.src = reduction.dst1
        lDetect.Run()
        dst1 = lDetect.dst1
    End Sub
End Class





Public Class LineDetector_Depth
    Inherits VBparent
    Dim longline As LineDetector_LongLines
    Dim sideView As Histogram_SideView2D
    Dim topView As Histogram_TopView2D
    Dim mats As Mat_4to1
    Public Sub New()
        initParent()
        mats = New Mat_4to1()
        sideView = New Histogram_SideView2D()
        topView = New Histogram_TopView2D()
        longline = New LineDetector_LongLines()

        task.desc = "Detect the lines in the depth data before trying to model the line in 3D space"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        longline.src = task.RGBDepth
        longline.Run()
        dst1 = longline.dst1

        Static thicknessSlider = findSlider("Line thickness")
        Dim thickness = thicknessSlider.value
        Static lenSlider = findSlider("Line length threshold (mm)")
        Dim mmThreshold = lenSlider.value
        Static radiusSlider = findSlider("Depth search radius in pixels")
        Dim pixelRadius = radiusSlider.value

        sideView.src = New cv.Mat(src.Size, cv.MatType.CV_32FC3, 0)
        topView.src = New cv.Mat(src.Size, cv.MatType.CV_32FC3, 0)
        For i = 0 To longline.sortlines.Count - 1
            Dim line = longline.sortlines.ElementAt(i).Value
            Dim p1 = New cv.Point(CInt(line(0)), CInt(line(1)))
            Dim p2 = New cv.Point(CInt(line(2)), CInt(line(3)))

            Dim topleft = New cv.Point(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y))
            Dim width = Math.Max(p1.X, p2.X) - topleft.X
            Dim height = Math.Max(p1.Y, p2.Y) - topleft.Y

            If width < pixelRadius * 2 Then
                width = Math.Min(src.Width - topleft.X, pixelRadius * 2)
                height = Math.Min(src.Height - topleft.Y, pixelRadius * 2)
            End If
            Dim rect = New cv.Rect(topleft.X, topleft.Y, width, height)
            If rect.Width > 0 And rect.Height > 0 Then task.pointCloud(rect).CopyTo(sideView.src(rect))
        Next
        sideView.Run()
        topView.Run()
        mats.mat(0) = sideView.dst1
        mats.mat(1) = topView.dst1
        mats.Run()
        dst2 = mats.dst1
    End Sub
End Class

