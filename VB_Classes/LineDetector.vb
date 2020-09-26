Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports MathNet.Spatial.Euclidean
Public Class LineDetector_Basics
    Inherits VBparent
    Dim ld As cv.XImgProc.FastLineDetector
    Public lines As cv.Vec4f()
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Line thickness", 1, 20, 2)
        sliders.setupTrackBar(1, "Line length threshold (mm)", 1, 2000, 100) ' not used in Run below but externally...
        sliders.setupTrackBar(2, "Line length threshold in pixels", 1, src.Width + src.Height, 100) ' not used in Run below but externally...
        sliders.setupTrackBar(3, "Depth search radius in pixels", 1, 20, 5) ' not used in Run below but externally...

        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        label1 = "Manually drawn"
        desc = "Use FastLineDetector (OpenCV Contrib) to find all the lines present."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        lines = ld.Detect(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        src.CopyTo(dst1)
        src.CopyTo(dst2)
        Dim thickness = sliders.trackbar(0).Value

        For Each v In lines
            If v(0) >= 0 And v(0) <= dst1.Cols And v(1) >= 0 And v(1) <= dst1.Rows And
                   v(2) >= 0 And v(2) <= dst1.Cols And v(3) >= 0 And v(3) <= dst1.Rows Then
                Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                dst1.Line(pt1, pt2, cv.Scalar.Red, thickness, cv.LineTypes.AntiAlias)
            End If
        Next
        If standalone Then
            label2 = "Drawn with DrawSegment (thickness=1)"
            ld.DrawSegments(dst2, lines, False)
        End If
    End Sub
End Class






Module fastLineDetector_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function lineDetectorFast_Run(image As IntPtr, rows As integer, cols As integer, length_threshold As integer, distance_threshold As Single, canny_th1 As integer, canny_th2 As integer,
                                             canny_aperture_size As integer, do_merge As Boolean) As integer
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function lineDetector_Run(image As IntPtr, rows As integer, cols As integer) As integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function lineDetector_Lines() As IntPtr
    End Function

    Public Sub find3DLineSegment(ocvb As VBocvb, dst2 As cv.Mat, _mask As cv.Mat, _depth32f As cv.Mat, aa As cv.Vec6f, maskLineWidth As integer)
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
        Dim totalPoints As integer
        Dim skipPoints As integer
        For y = 0 To roi.Height - 1
            For x = 0 To roi.Width - 1
                If mask.Get(Of Byte)(y, x) = 1 Then
                    totalPoints += 1
                    Dim w = getWorldCoordinatesD6(ocvb, New cv.Point3f(x + roi.X, y + roi.Y, depth32f.Get(Of Single)(y, x)))
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
    Public Function segment3D(worldDepth As List(Of cv.Vec6f), ByRef skipPoints As integer) As cv.Vec6f()
        ' by construction, x and y are already on a line.  Compute the average z delta.  Eliminate outliers with that average.
        Dim sum As Double = 0
        Dim midPoint As Double
        For i = 1 To worldDepth.Count - 1
            midPoint += worldDepth(i).Item2
            sum += Math.Abs(worldDepth(i).Item2 - worldDepth(i - 1).Item2)
        Next
        Dim avgDelta = sum / worldDepth.Count * 3
        midPoint /= worldDepth.Count
        Dim midIndex As integer = -1
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
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        sliders.Setup(ocvb, caller, 6)
        sliders.setupTrackBar(0, "FLD - Min Length", 1, 200, 30)
        sliders.setupTrackBar(1, "FLD - max distance", 1, 100, 14)
        sliders.setupTrackBar(2, "FLD - Canny Aperture", 3, 7, 7)
        sliders.setupTrackBar(3, "FLD - Line Thickness", 1, 7, 3)
        sliders.setupTrackBar(4, "FLD - canny Threshold1", 1, 100, 50)
        sliders.setupTrackBar(5, "FLD - canny Threshold2", 1, 100, 50)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "FLD - incremental merge"
        check.Box(0).Checked = True
        check.Box(1).Text = "FLD - Draw lines on input image"
        check.Box(1).Checked = True

        desc = "Basics for a Fast Line Detector"
    End Sub
    Public Sub Run(ocvb As VBocvb)
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
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        lines = New lineDetector_FLD_CPP(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Mask Line Width", 1, 20, 1)
        sliders.setupTrackBar(1, "Update frequency (in frames)", 1, 100, 1)

        desc = "Identify planes using the lines present in the rgb image."
        label2 = ""
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.frameCount Mod sliders.trackbar(1).Value Then Exit Sub
        lines.src = src
        lines.Run(ocvb)
        src.CopyTo(dst1)

        Dim depth32f = getDepth32f(ocvb)

        If lines.sortedLines.Count > 0 Then
            ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will get full length.
            Dim maskLineWidth As integer = sliders.trackbar(0).Value
            Dim mask = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, 0)
            find3DLineSegment(ocvb, dst1, mask, depth32f, lines.sortedLines.ElementAt(lines.sortedLines.Count - 1).Key, maskLineWidth)
        End If
    End Sub
End Class




Public Class LineDetector_3D_FLD_MT
    Inherits VBparent
    Dim lines As lineDetector_FLD_CPP
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        lines = New lineDetector_FLD_CPP(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Mask Line Width", 1, 20, 1)
        sliders.setupTrackBar(1, "Update frequency (in frames)", 1, 100, 1)

        desc = "Measure 3d line segments using a multi-threaded Fast Line Detector."
        label2 = ""
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.frameCount Mod sliders.trackbar(1).Value Then Exit Sub
        lines.src = src
        lines.Run(ocvb)
        src.CopyTo(dst1)

        Dim depth32f = getDepth32f(ocvb)

        ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will get full length.
        Dim maskLineWidth As integer = sliders.trackbar(0).Value
        Dim mask = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, 0)
        Parallel.For(lines.sortedLines.Count - 20, lines.sortedLines.Count,
            Sub(i)
                find3DLineSegment(ocvb, dst1, mask, depth32f, lines.sortedLines.ElementAt(i).Key, maskLineWidth)
            End Sub)
        label1 = "Showing the " + CStr(Math.Min(lines.sortedLines.Count, 20)) + " longest lines out of " + CStr(lines.sortedLines.Count)
    End Sub
End Class




Public Class LineDetector_3D_FitLineZ
    Inherits VBparent
    Dim linesFLD As lineDetector_FLD_CPP
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        linesFLD = New lineDetector_FLD_CPP(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Mask Line Width", 1, 20, 3)
        sliders.setupTrackBar(1, "Point count threshold", 5, 500, 50)
        sliders.setupTrackBar(2, "Update frequency (in frames)", 1, 100, 1)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Fitline using x and z (unchecked it will use y and z)"
        check.Box(1).Text = "Display only the longest line"
        check.Box(1).Checked = True

        desc = "Use Fitline with the sparse Z data and X or Y (in RGB pixels)."
        label2 = ""
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.frameCount Mod sliders.trackbar(2).Value Then Exit Sub
        Dim useX As Boolean = check.Box(0).Checked
        linesFLD.src = src
        linesFLD.Run(ocvb)
        src.CopyTo(dst1)

        Dim depth32f = getDepth32f(ocvb)

        Dim sortedlines As SortedList(Of cv.Vec6f, Integer)
        sortedlines = linesFLD.sortedLines

        If sortedlines.Count > 0 Then
            ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will likely get full length.
            Dim maskLineWidth As integer = sliders.trackbar(0).Value
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
                                Dim w = getWorldCoordinatesD6(ocvb, New cv.Point3f(x + roi.X, y + roi.Y, depth32f.Get(Of Single)(y, x)))
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
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        sliders.Setup(ocvb, caller, 6)
        sliders.setupTrackBar(0, "FLD - Min Length", 1, 200, 30)
        sliders.setupTrackBar(1, "FLD - max distance", 1, 100, 14)
        sliders.setupTrackBar(2, "FLD - Canny Aperture", 3, 7, 7)
        sliders.setupTrackBar(3, "FLD - Line Thickness", 1, 7, 3)
        sliders.setupTrackBar(4, "FLD - canny Threshold1", 1, 100, 50)
        sliders.setupTrackBar(5, "FLD - canny Threshold2", 1, 100, 50)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "FLD - incremental merge"
        check.Box(0).Checked = True
        desc = "A Fast Line Detector"
    End Sub
    Public Sub Run(ocvb As VBocvb)
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
        If standalone Then
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
    Dim lDetect As LineDetector_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        lDetect = New LineDetector_Basics(ocvb)

        label1 = "Longest lines using depth data"
        label2 = "Longest lines in pixels (yellow)"
        desc = "Find and measure the longest x number of lines in actual length (not in pixels)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        lDetect.src = src
        lDetect.Run(ocvb)
        dst1 = src.Clone
        dst2 = src.Clone

        Static thicknessSlider = findSlider("Line thickness")
        Dim thickness = thicknessSlider.value
        Static lenSlider = findSlider("Line length threshold (mm)")
        Dim mmThreshold = lenSlider.value
        Static pixelSlider = findSlider("Line length threshold in pixels")
        Dim pixelThreshold = pixelSlider.value
        Static radiusSlider = findSlider("Depth search radius in pixels")
        Dim pixelRadius = radiusSlider.value

        Dim sortPixels As New SortedList(Of Single, cv.Vec4f)(New compareAllowIdenticalIntInverted)
        Dim sortWithDepth As New SortedList(Of Single, cv.Vec4f)(New compareAllowIdenticalIntInverted)
        For Each v In lDetect.lines
            If v(0) >= 0 And v(0) <= dst1.Cols And v(1) >= 0 And v(1) <= dst1.Rows And
                   v(2) >= 0 And v(2) <= dst1.Cols And v(3) >= 0 And v(3) <= dst1.Rows Then
                Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                Dim pixelLen = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                If pixelLen > pixelThreshold Then
                    sortPixels.Add(pixelLen, v)
                    dst2.Line(pt1, pt2, cv.Scalar.Red, thickness, cv.LineTypes.AntiAlias)
                Else
                    dst2.Line(pt1, pt2, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)
                End If

                Dim r1 = New cv.Rect(Math.Max(0, pt1.X - pixelRadius), Math.Max(0, pt1.Y - pixelRadius), pixelRadius * 2, pixelRadius * 2)
                Dim mean1 = ocvb.pointCloud(r1).Mean()

                Dim r2 = New cv.Rect(Math.Max(pt2.X - pixelRadius, 0), Math.Max(0, pt2.Y - pixelRadius), pixelRadius * 2, pixelRadius * 2)
                Dim mean2 = ocvb.pointCloud(r2).Mean()

                'Dim m1 = New Point3D(mean1.Item(0) * 1000, mean1.Item(1) * 1000, mean1.Item(2) * 1000)
                'Dim m2 = New Point3D(mean2.Item(0) * 1000, mean2.Item(1) * 1000, mean2.Item(2) * 1000)

                'If m1 <> New Point3D And m2 <> New Point3D Then
                '    Dim distance = m1.DistanceTo(m2)
                '    If distance > mmThreshold Then
                '        sortWithDepth.Add(distance, v)
                '        dst1.Line(pt1, pt2, cv.Scalar.Red, thickness, cv.LineTypes.AntiAlias)
                '    End If
                'End If
            End If
        Next
    End Sub
End Class






Public Class LineDetector_Reduction
    Inherits VBparent
    Dim lDetect As LineDetector_Basics
    Dim reduction As Reduction_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        lDetect = New LineDetector_Basics(ocvb)
        reduction = New Reduction_Basics(ocvb)

        label1 = "Output of line detection using reduced input"
        label2 = "Output of reduction_basics"
        desc = "Use the reduced rgb image as input to the line detector"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)
        dst2 = lDetect.dst1

        lDetect.src = reduction.dst1
        lDetect.Run(ocvb)
        dst1 = lDetect.dst1
    End Sub
End Class