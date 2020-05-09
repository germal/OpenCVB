Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module fastLineDetector_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function lineDetectorFast_Run(image As IntPtr, rows As Int32, cols As Int32, length_threshold As Int32, distance_threshold As Single, canny_th1 As Int32, canny_th2 As Int32,
                                             canny_aperture_size As Int32, do_merge As Boolean) As Int32
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function lineDetector_Run(image As IntPtr, rows As Int32, cols As Int32) As Int32
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function lineDetector_Lines() As IntPtr
    End Function

    Public Sub find3DLineSegment(ocvb As AlgorithmData, _mask As cv.Mat, _depth32f As cv.Mat, aa As cv.Vec6f, maskLineWidth As Int32)
        Dim pt1 = New cv.Point(aa(0), aa(1))
        Dim pt2 = New cv.Point(aa(2), aa(3))
        Dim centerPoint = New cv.Point((aa(0) + aa(2)) / 2, (aa(1) + aa(3)) / 2)
        _mask.Line(pt1, pt2, New cv.Scalar(1), maskLineWidth, cv.LineTypes.AntiAlias)
        ocvb.result2.Line(pt1, pt2, cv.Scalar.Red, 3, cv.LineTypes.AntiAlias)

        Dim roi = New cv.Rect(Math.Min(aa(0), aa(2)), Math.Min(aa(1), aa(3)), Math.Abs(aa(0) - aa(2)), Math.Abs(aa(1) - aa(3)))

        Dim worldDepth As New List(Of cv.Vec6f)
        If roi.Width = 0 Then roi.Width = 1
        If roi.Height = 0 Then roi.Height = 1
        If roi.X + roi.Width >= _mask.Width Then roi.Width = _mask.Width - roi.X - 1
        If roi.Y + roi.Height >= _mask.Height Then roi.Height = _mask.Height - roi.Y - 1
        Dim mask = _mask(roi).Clone()
        Dim depth32f = _depth32f(roi).Clone()
        Dim totalPoints As Int32
        Dim skipPoints As Int32
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
                cv.Cv2.PutText(ocvb.result2, Format(lenBD / 1000, "0.00") + "m", centerPoint, cv.HersheyFonts.HersheyTriplex, 0.4, cv.Scalar.White, 1,
                                   cv.LineTypes.AntiAlias)
                If endPoints(0).Item2 = endPoints(1).Item2 Then endPoints(0).Item2 += 1 ' prevent NaN
                cv.Cv2.PutText(ocvb.result2, Format((endPoints(1).Item1 - endPoints(0).Item1) / (endPoints(1).Item2 - endPoints(0).Item2), "0.00") + "y/z",
                                   New cv.Point(centerPoint.X, centerPoint.Y + 10), cv.HersheyFonts.HersheyTriplex, 0.4, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                ' show the final endpoints in xy projection.
                ocvb.result2.Circle(New cv.Point(b.Item3, b.Item4), 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                ocvb.result2.Circle(New cv.Point(d.Item3, d.Item4), 2, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
            End If
        End If
    End Sub
    Public Function segment3D(worldDepth As List(Of cv.Vec6f), ByRef skipPoints As Int32) As cv.Vec6f()
        ' by construction, x and y are already on a line.  Compute the average z delta.  Eliminate outliers with that average.
        Dim sum As Double = 0
        Dim midPoint As Double
        For i = 1 To worldDepth.Count - 1
            midPoint += worldDepth(i).Item2
            sum += Math.Abs(worldDepth(i).Item2 - worldDepth(i - 1).Item2)
        Next
        Dim avgDelta = sum / worldDepth.Count * 3
        midPoint /= worldDepth.Count
        Dim midIndex As Int32 = -1
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
    Public Function drawSegments(dst As cv.Mat, lineCount As Int32, factor As Int32, lineMask As cv.Mat) As SortedList(Of cv.Vec6f, Integer)
        Dim sortedLines As New SortedList(Of cv.Vec6f, Integer)(New CompareVec6f)

        Dim lines(lineCount * 4 - 1) As Single ' there are 4 floats per line
        Dim linePtr = lineDetector_Lines()
        If linePtr = 0 Then Return Nothing ' it happened!
        Marshal.Copy(linePtr, lines, 0, lines.Length)

        Dim lineMat = New cv.Mat(lineCount, 1, cv.MatType.CV_32FC4, lines)
        Dim v6 As New cv.Vec6f
        For i = 0 To lineCount - 1
            Dim v = lineMat.Get(Of cv.Vec4f)(i)
            ' make sure that none are negative - how could any be negative?  Usually just fractionally less than zero.
            For j = 0 To 3
                If v(j) < 0 Then v(j) = 0
            Next

            v6(0) = v(0) * factor
            v6(1) = v(1) * factor
            v6(2) = v(2) * factor
            v6(3) = v(3) * factor
            v6(4) = Math.Sqrt((v(0) - v(2)) * (v(0) - v(2)) + (v(1) - v(3)) * (v(1) - v(3))) ' vector carries the length in pixels with it.
            v6(5) = 0 ' unused...

            ' add this line to the sorted list
            If sortedLines.ContainsKey(v6) Then
                sortedLines(v6) = sortedLines(v6) + 1
            Else
                sortedLines.Add(v6, 1)
            End If
        Next

        For i = sortedLines.Count - 1 To 0 Step -1
            Dim v = sortedLines.ElementAt(i).Key
            If v(0) >= 0 And v(0) <= dst.Cols And v(1) >= 0 And v(1) <= dst.Rows And
                   v(2) >= 0 And v(2) <= dst.Cols And v(3) >= 0 And v(3) <= dst.Rows Then
                Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                dst.Line(pt1, pt2, cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
                lineMask.Line(pt1, pt2, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
            End If
        Next
        Return sortedLines
    End Function
End Module




' https://docs.opencv.org/3.4.3/d1/d9e/fld_lines_8cpp-example.html
Public Class lineDetector_FLD
    Inherits ocvbClass
    Public sortedLines As SortedList(Of cv.Vec6f, Integer)
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        radio.Setup(ocvb, caller, 3)
        radio.check(0).Text = "Low resolution - Factor 4"
        radio.check(1).Text = "Low resolution - Factor 2"
        radio.check(2).Text = "Low resolution - Factor 1"
        radio.check(1).Checked = True

        sliders2.setupTrackBar1(ocvb, caller, "FLD - canny Threshold1", 1, 100, 50)
        sliders2.setupTrackBar2(ocvb, caller, "FLD - canny Threshold2", 1, 100, 50)
        If ocvb.parms.ShowOptions Then sliders2.Show()

        sliders.setupTrackBar1(ocvb, caller, "FLD - Min Length", 1, 200, 30)
        sliders.setupTrackBar2(ocvb, caller, "FLD - max distance", 1, 100, 14)
        sliders.setupTrackBar3(ocvb, caller, "FLD - Canny Aperture", 3, 7, 7)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "FLD - incremental merge"
        check.Box(0).Checked = True
        ocvb.desc = "Basics for a Fast Line Detector"

        sortedLines = New SortedList(Of cv.Vec6f, Integer)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        sortedLines.Clear()

        Dim length_threshold = sliders.TrackBar1.Value
        Dim distance_threshold = sliders.TrackBar2.Value / 10
        Dim canny_aperture_size = sliders.TrackBar3.Value
        If canny_aperture_size Mod 2 = 0 Then canny_aperture_size += 1
        Dim canny_th1 = sliders2.TrackBar1.Value
        Dim canny_th2 = sliders2.TrackBar2.Value
        Dim do_merge = check.Box(0).Checked

        if standalone Then src = ocvb.color

        Dim factor As Int32 = 4
        If radio.check(0).Checked Then
            factor = 4
        ElseIf radio.check(1).Checked Then
            factor = 2
        Else
            factor = 1
        End If
        Dim cols = src.Width / factor
        Dim rows = src.Height / factor
        Dim grayInput = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Resize(New cv.Size(cols, rows))

        Dim data(grayInput.Total - 1) As Byte

        Marshal.Copy(grayInput.Data, data, 0, data.Length)
        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        Dim lineCount = lineDetectorFast_Run(handle.AddrOfPinnedObject, rows, cols, length_threshold, distance_threshold, canny_th1, canny_th2, canny_aperture_size, do_merge)
        handle.Free()

        src.CopyTo(dst)
        If lineCount > 0 Then sortedLines = drawSegments(dst, lineCount, factor, dst)
        if standalone Then dst.CopyTo(ocvb.result1)
    End Sub
End Class



' https://docs.opencv.org/3.4.3/d1/d9e/fld_lines_8cpp-example.html
Public Class LineDetector_LSD
    Inherits ocvbClass
    Dim data() As Byte
    Public sortedLines As SortedList(Of cv.Vec6f, Integer)
    Public factor As Int32 = 4
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Fast Line Detector from the OpenCV contrib code base."
        Dim size = ocvb.color.Rows * ocvb.color.Cols
        ReDim data(size - 1)
        ocvb.label2 = "Mask of lines detected"
        sortedLines = New SortedList(Of cv.Vec6f, Integer)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.OpenCV_Version_ID <> "401" Then
            ocvb.result1.SetTo(0)
            ocvb.putText(New ActiveClass.TrueType("LSD linedetector is available only with OpenCV 4.01 - IP issue?", 10, 100, RESULT1))
            Exit Sub
        End If

        Dim cols = ocvb.color.Width / factor
        Dim rows = ocvb.color.Height / factor
        If data.Length <> rows * cols Then ReDim data(rows * cols * 3 - 1)
        Dim grayInput = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Resize(New cv.Size(rows, cols))

        Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
        Dim tmp As New cv.Mat(rows, cols, cv.MatType.CV_8U, data)
        grayInput.CopyTo(tmp)
        Dim lineCount = lineDetector_Run(tmp.Data, rows, cols)
        handle.Free()

        ocvb.color.CopyTo(ocvb.result1)
        sortedLines.Clear()
        If lineCount > 0 Then sortedLines = drawSegments(ocvb.result1, lineCount, factor, ocvb.result1)
    End Sub
End Class




Public Class LineDetector_3D_LongestLine
    Inherits ocvbClass
    Dim lines As lineDetector_FLD
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        lines = New lineDetector_FLD(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "Mask Line Width", 1, 20, 1)

        ocvb.desc = "Identify planes using the lines present in the rgb image."
        ocvb.label2 = ""
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 30 Then Exit Sub
        lines.Run(ocvb)
        ocvb.color.CopyTo(ocvb.result2)

        Dim depth32f = getDepth32f(ocvb)

        If lines.sortedLines.Count > 0 Then
            ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will get full length.
            Dim maskLineWidth As Int32 = sliders.TrackBar1.Value
            Dim mask = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8U, 0)
            find3DLineSegment(ocvb, mask, depth32f, lines.sortedLines.ElementAt(lines.sortedLines.Count - 1).Key, maskLineWidth)
        End If
    End Sub
    Public Sub MyDispose()
        lines.Dispose()
    End Sub
End Class




Public Class LineDetector_3D_FLD_MT
    Inherits ocvbClass
    Dim lines As lineDetector_FLD
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        lines = New lineDetector_FLD(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "Mask Line Width", 1, 20, 1)

        ocvb.desc = "Measure 3d line segments using a multi-threaded Fast Line Detector."
        ocvb.label2 = ""
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 30 Then Exit Sub
        lines.Run(ocvb)
        ocvb.color.CopyTo(ocvb.result2)

        Dim depth32f = getDepth32f(ocvb)

        ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will get full length.
        Dim maskLineWidth As Int32 = sliders.TrackBar1.Value
        Dim mask = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8U, 0)
        Parallel.For(0, lines.sortedLines.Count - 1,
            Sub(i)
                find3DLineSegment(ocvb, mask, depth32f, lines.sortedLines.ElementAt(i).Key, maskLineWidth)
            End Sub)
    End Sub
    Public Sub MyDispose()
        lines.Dispose()
    End Sub
End Class



Public Class LineDetector_3D_LSD_MT
    Inherits ocvbClass
    Dim lines As LineDetector_LSD
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        lines = New LineDetector_LSD(ocvb, caller)
        sliders.setupTrackBar1(ocvb, caller, "Mask Line Width", 1, 20, 1)

        ocvb.desc = "Measure 3D line segments using a multi-threaded Line Stream Detector"
        ocvb.label2 = ""
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.OpenCV_Version_ID <> "401" Then
            ocvb.result1.SetTo(0)
            ocvb.putText(New ActiveClass.TrueType("LSD linedetector is available only with OpenCV 4.01 - IP issue.", 10, 100, RESULT1))
            Exit Sub
        End If

        If ocvb.frameCount Mod 30 Then Exit Sub
        lines.Run(ocvb)
        ocvb.color.CopyTo(ocvb.result2)

        Dim depth32f = getDepth32f(ocvb)

        ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will get full length.
        Dim maskLineWidth As Int32 = sliders.TrackBar1.Value
        Dim mask = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8U, 0)
        Parallel.For(0, lines.sortedLines.Count - 1,
            Sub(i)
                find3DLineSegment(ocvb, mask, depth32f, lines.sortedLines.ElementAt(i).Key, maskLineWidth)
            End Sub)
    End Sub
    Public Sub MyDispose()
        lines.Dispose()
    End Sub
End Class




Public Class LineDetector_3D_FitLineZ
    Inherits ocvbClass
    Dim linesFLD As lineDetector_FLD
    Dim linesLSD As LineDetector_LSD
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        linesFLD = New lineDetector_FLD(ocvb, caller)
        linesLSD = New LineDetector_LSD(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "Mask Line Width", 1, 20, 3)
        sliders.setupTrackBar2(ocvb, caller, "Point count threshold", 5, 500, 50)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Fitline using x and z (unchecked it will use y and z)"
        check.Box(1).Text = "display output only once a second (to be readable)"
        check.Box(1).Checked = True

        radio.Setup(ocvb, caller, 4)
        radio.check(0).Text = "Use Fast LineDetector"
        radio.check(1).Text = "Use Line Stream Detector"
        radio.check(2).Text = "Debug FLD longest line"
        radio.check(3).Text = "Debug LSD longest line"
        radio.check(2).Checked = True

        ocvb.desc = "Use Fitline with the sparse Z data and X or Y (in RGB pixels)."
        ocvb.label2 = ""
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If check.Box(1).Checked And ocvb.frameCount Mod 30 Then Exit Sub
        Dim useX As Boolean = check.Box(0).Checked
        Dim useLSD As Boolean = True
        If radio.check(0).Checked Or radio.check(2).Checked Then useLSD = False
        If useLSD And ocvb.parms.OpenCV_Version_ID <> "401" Then
            ocvb.result1.SetTo(0)
            ocvb.result2.SetTo(0)
            ocvb.putText(New ActiveClass.TrueType("LSD linedetector is available only with OpenCV 4.01 - IP issue.", 10, 100, RESULT1))
            Exit Sub
        End If
        If useLSD Then linesLSD.Run(ocvb) Else linesFLD.Run(ocvb)
        ocvb.color.CopyTo(ocvb.result2)

        Dim depth32f = getDepth32f(ocvb)

        Dim sortedlines As SortedList(Of cv.Vec6f, Integer)
        If useLSD Then sortedlines = linesLSD.sortedLines Else sortedlines = linesFLD.sortedLines

        If sortedlines.Count > 0 Then
            ' how big to make the mask that will be used to find the depth data.  Small is more accurate.  Larger will likely get full length.
            Dim maskLineWidth As Int32 = sliders.TrackBar1.Value
            Dim mask = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8U, 0)

            Dim longestLineOnly As Boolean = radio.check(2).Checked Or radio.check(3).Checked
            Dim pointCountThreshold = sliders.TrackBar2.Value
            Parallel.For(0, sortedlines.Count,
                Sub(i)
                    If longestLineOnly And i < sortedlines.Count - 1 Then Exit Sub
                    Dim aa = sortedlines.ElementAt(i).Key
                    Dim pt1 = New cv.Point(aa(0), aa(1))
                    Dim pt2 = New cv.Point(aa(2), aa(3))
                    ocvb.result2.Line(pt1, pt2, cv.Scalar.Red, 2, cv.LineTypes.AntiAlias)
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
                    cv.Cv2.PutText(ocvb.result2, Format(lenBD / 1000, "#0.00") + "m", textPoint, cv.HersheyFonts.HersheyComplexSmall, 0.5, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                    If endPoints(0).Item2 = endPoints(1).Item2 Then endPoints(0).Item2 += 1 ' prevent NaN
                    cv.Cv2.PutText(ocvb.result2, Format((endPoints(1).Item1 - endPoints(0).Item1) / (endPoints(1).Item2 - endPoints(0).Item2), "#0.00") + If(useX, "x/z", "y/z"),
                                    New cv.Point(textPoint.X, textPoint.Y + 10), cv.HersheyFonts.HersheyComplexSmall, 0.5, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)

                    ' show the final endpoints in xy projection.
                    ocvb.result2.Circle(New cv.Point(b.Item3, b.Item4), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                    ocvb.result2.Circle(New cv.Point(d.Item3, d.Item4), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                End Sub)
        End If
    End Sub
    Public Sub MyDispose()
        linesFLD.Dispose()
        linesLSD.Dispose()
    End Sub
End Class




Public Class LineDetector_Basics
    Inherits ocvbClass
    Dim ld As cv.XImgProc.FastLineDetector
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "LineDetector thickness of line", 1, 20, 2)

        ld = cv.XImgProc.CvXImgProc.CreateFastLineDetector
        ocvb.label1 = "Manually drawn with thickness"
        ocvb.desc = "Use ximgproc to find all the lines present."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim vectors = ld.Detect(gray)
        ocvb.color.CopyTo(ocvb.result1)
        ocvb.color.CopyTo(ocvb.result2)
        Dim thickness = sliders.TrackBar1.Value

        if standalone Then dst = ocvb.result1
        For Each v In vectors
            If v(0) >= 0 And v(0) <= dst.Cols And v(1) >= 0 And v(1) <= dst.Rows And
                   v(2) >= 0 And v(2) <= dst.Cols And v(3) >= 0 And v(3) <= dst.Rows Then
                Dim pt1 = New cv.Point(CInt(v(0)), CInt(v(1)))
                Dim pt2 = New cv.Point(CInt(v(2)), CInt(v(3)))
                dst.Line(pt1, pt2, cv.Scalar.Red, thickness, cv.LineTypes.AntiAlias)
            End If
        Next
        if standalone Then
            ocvb.label2 = "Drawn with DrawSegment (thickness=1)"
            ld.DrawSegments(ocvb.result2, vectors, False)
        End If
    End Sub
    Public Sub MyDispose()
        ld.Dispose()
    End Sub
End Class