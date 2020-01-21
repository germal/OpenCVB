Imports cv = OpenCvSharp
' https://docs.opencv.org/3.4/js_contour_features_fitLine.html
Public Class Fitline_Basics : Implements IDisposable
    Dim draw As Draw_Line
    Public Sub New(ocvb As AlgorithmData)
        draw = New Draw_Line(ocvb)
        draw.sliders.TrackBar1.Value = 2
        ocvb.desc = "Show how Fitline API works.  When overlapping (single contour), the lines are not correctly found."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        draw.Run(ocvb)

        Dim gray = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        gray = gray.Threshold(254, 255, cv.ThresholdTypes.BinaryInv)
        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(gray, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        ocvb.result2 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For i = 0 To contours.Length - 1
            Dim cnt = contours(i)
            Dim line2d = cv.Cv2.FitLine(cnt, cv.DistanceTypes.L2, 0, 0, 0.01)
            Dim lefty = Math.Round((-line2d.X1 * line2d.Vy / line2d.Vx) + line2d.Y1)
            Dim righty = Math.Round(((ocvb.result2.Cols - line2d.X1) * line2d.Vy / line2d.Vx) + line2d.Y1)
            Dim p1 = New cv.Point(ocvb.result2.Cols - 1, righty)
            Dim p2 = New cv.Point(0, lefty)
            ocvb.result2.Line(p1, p2, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        draw.Dispose()
    End Sub
End Class



Public Class Fitline_3DBasics_MT : Implements IDisposable
    Dim hlines As Hough_Lines_MT
    Public Sub New(ocvb As AlgorithmData)
        hlines = New Hough_Lines_MT(ocvb)
        ocvb.desc = "Use visual lines to find 3D lines."
        ocvb.label2 = "White is featureless RGB, blue depth shadow"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hlines.Run(ocvb)
        Dim gray = ocvb.result2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim mask = gray.Threshold(1, 255, cv.ThresholdTypes.Binary)
        ocvb.result2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.color.CopyTo(ocvb.result1)

        Parallel.ForEach(Of cv.Rect)(hlines.grid.roiList,
        Sub(roi)
            Dim depth = ocvb.depth(roi).Clone()
            Dim fMask = mask(roi).Clone()
            Dim points As New List(Of cv.Point3f)
            Dim rows = ocvb.color.Rows, cols = ocvb.color.Cols
            For y = 0 To roi.Height - 1
                For x = 0 To roi.Width - 1
                    If fMask.At(Of Byte)(y, x) > 0 Then
                        Dim d = depth.At(Of UShort)(y, x)
                        If d > 0 And d < 10000 Then
                            points.Add(New cv.Point3f(x / rows, y / cols, d / 10000))
                        End If
                    End If
                Next
            Next
            If points.Count = 0 Then
                ' save the average color for this roi
                Dim mean = ocvb.depthRGB(roi).Mean()
                mean(0) = 255 - mean(0)
                ocvb.result2.Rectangle(roi, mean, -1, cv.LineTypes.AntiAlias)
                Exit Sub
            End If
            Dim fitArray = points.ToArray()
            Dim line = cv.Cv2.FitLine(fitArray, cv.DistanceTypes.L2, 0, 0, 0.01)
            If line IsNot Nothing Then
                HoughShowLines3D(ocvb.result1(roi), line)
            End If
        End Sub)
        ocvb.result1.SetTo(cv.Scalar.White, hlines.grid.gridMask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        hlines.Dispose()
    End Sub
End Class



Public Class Fitline_RawInput : Implements IDisposable
    Public sliders As New OptionsSliders
    Public check As New OptionsCheckbox
    Public points As List(Of cv.Point2f)
    Public m As Single
    Public bb As Single
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Random point count", 0, 500, 100)
        sliders.setupTrackBar2(ocvb, "Line Point Count", 0, 500, 20)
        sliders.setupTrackBar3(ocvb, "Line Noise", 1, 100, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()

        check.Setup(ocvb, 2)
        check.Box(0).Text = "Highlight Line Data"
        check.Box(1).Text = "Recompute with new random data"
        check.Box(0).Checked = True
        check.Box(1).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()

        ocvb.desc = "Generate a noisy line in a field of random data."
        Randomize()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If check.Box(1).Checked Or ocvb.frameCount = 0 Then
            ocvb.result1 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8UC1, 0)
            Dim dotSize = 2
            Dim w = ocvb.color.Width
            Dim h = ocvb.color.Height

            points = New List(Of cv.Point2f)
            For i = 0 To sliders.TrackBar1.Value - 1
                Dim pt = New cv.Point2f(Rnd() * w, Rnd() * h)
                If pt.X < 0 Then pt.X = 0
                If pt.X > w Then pt.X = w
                If pt.Y < 0 Then pt.Y = 0
                If pt.Y > h Then pt.Y = h
                points.Add(pt)
                ocvb.result1.Circle(points(i), dotSize, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
            Next

            Dim p1 As cv.Point2f, p2 As cv.Point2f
            If Rnd() * 2 - 1 >= 0 Then
                p1 = New cv.Point(Rnd() * w, 0)
                p2 = New cv.Point(Rnd() * w, h)
            Else
                p1 = New cv.Point(0, Rnd() * h)
                p2 = New cv.Point(w, Rnd() * h)
            End If

            If p1.X = p2.X Then p1.X += 1
            If p1.Y = p2.Y Then p1.Y += 1
            m = (p2.Y - p1.Y) / (p2.X - p1.X)
            bb = p2.Y - p2.X * m
            Dim startx = Math.Min(p1.X, p2.X)
            Dim incr = (Math.Max(p1.X, p2.X) - startx) / sliders.TrackBar2.Value
            Dim highLight = cv.Scalar.White
            If check.Box(0).Checked Then
                highLight = cv.Scalar.Gray
                dotSize = 5
            End If
            For i = 0 To sliders.TrackBar2.Value - 1
                Dim noiseOffsetX = (Rnd() * 2 - 1) * sliders.TrackBar3.Value
                Dim noiseOffsetY = (Rnd() * 2 - 1) * sliders.TrackBar3.Value
                Dim pt = New cv.Point(startx + i * incr + noiseOffsetX, Math.Max(0, Math.Min(m * (startx + i * incr) + bb + noiseOffsetY, h)))
                If pt.X < 0 Then pt.X = 0
                If pt.X > w Then pt.X = w
                If pt.Y < 0 Then pt.Y = 0
                If pt.Y > h Then pt.Y = h
                points.Add(pt)
                ocvb.result1.Circle(pt, dotSize, highLight, -1, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        check.Dispose()
    End Sub
End Class




' http://www.cs.cmu.edu/~youngwoo/doc/lineFittingTest.cpp
Public Class Fitline_EigenFit : Implements IDisposable
    Dim noisyLine As Fitline_RawInput
    Public Sub New(ocvb As AlgorithmData)
        noisyLine = New Fitline_RawInput(ocvb)
        noisyLine.sliders.TrackBar1.Value = 30
        noisyLine.sliders.TrackBar2.Value = 400
        ocvb.label1 = "Raw input (use sliders below to explore)"
        ocvb.label2 = "blue=GT, red=fitline, yellow=EigenFit"
        ocvb.desc = "Remove outliers when trying to fit a line.  Fitline and the Eigen computation below produce the same result."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 30 Then Exit Sub ' it updates too much to review the results without this.

        Static noisePointCount As Int32
        Static linePointCount As Int32
        Static lineNoise As Int32
        Static highlight As Boolean
        'If noisyLine.sliders.TrackBar1.Value <> noisePointCount Or noisyLine.sliders.TrackBar2.Value <> linePointCount Or
        '    noisyLine.sliders.TrackBar3.Value <> lineNoise Or noisyLine.check.Box(0).Checked <> highlight Or noisyLine.check.Box(1).Checked Then
        noisyLine.check.Box(1).Checked = True
        noisyLine.Run(ocvb)
        noisyLine.check.Box(1).Checked = False
        'End If

        noisePointCount = noisyLine.sliders.TrackBar1.Value
        linePointCount = noisyLine.sliders.TrackBar2.Value
        lineNoise = noisyLine.sliders.TrackBar3.Value
        highlight = noisyLine.check.Box(0).Checked

        Dim w = ocvb.color.Width
        ocvb.result2.SetTo(0)

        Dim line = cv.Cv2.FitLine(noisyLine.points, cv.DistanceTypes.L2, 1, 0.01, 0.01)
        Dim m = line.Vy / line.Vx
        Dim bb = line.Y1 - m * line.X1
        Dim p1 = New cv.Point(0, bb)
        Dim p2 = New cv.Point(w, m * w + bb)
        ocvb.result2.Line(p1, p2, cv.Scalar.Red, 20, cv.LineTypes.AntiAlias)

        Dim pointMat = New cv.Mat(noisyLine.points.Count, 1, cv.MatType.CV_32FC2, noisyLine.points.ToArray)
        Dim mean = pointMat.Mean()
        Dim split() = pointMat.Split()
        Dim minX As Single, maxX As Single, minY As Single, maxY As Single
        split(0).MinMaxLoc(minX, maxX)
        split(1).MinMaxLoc(minY, maxY)

        Dim eigenVec As New cv.Mat(2, 2, cv.MatType.CV_32F, 0), eigenVal As New cv.Mat(2, 2, cv.MatType.CV_32F, 0)

        Dim eigenInput As New cv.Vec4f
        For i = 0 To noisyLine.points.Count - 1
            Dim pt = noisyLine.points.Item(i)
            Dim x = pt.X - mean.Val0
            Dim y = pt.Y - mean.Val1
            eigenInput.Item0 += x * x
            eigenInput.Item1 += x * y
            eigenInput.Item3 += y * y
        Next
        eigenInput.Item2 = eigenInput.Item1

        Dim vec4f As New List(Of cv.Point2f)
        vec4f.Add(New cv.Point2f(eigenInput.Item0, eigenInput.Item1))
        vec4f.Add(New cv.Point2f(eigenInput.Item1, eigenInput.Item3))

        Dim D = New cv.Mat(2, 2, cv.MatType.CV_32FC1, vec4f.ToArray)
        cv.Cv2.Eigen(D, eigenVal, eigenVec)
        Dim theta = Math.Atan2(eigenVec.At(Of Single)(1, 0), eigenVec.At(Of Single)(0, 0))

        Dim Len = Math.Sqrt(Math.Pow(maxX - minX, 2) + Math.Pow(maxY - minY, 2))

        p1 = New cv.Point2f(mean.Val0 - Math.Cos(theta) * Len / 2, mean.Val1 - Math.Sin(theta) * Len / 2)
        p2 = New cv.Point2f(mean.Val0 + Math.Cos(theta) * Len / 2, mean.Val1 + Math.Sin(theta) * Len / 2)
        Dim m2 = (p2.Y - p1.Y) / (p2.X - p1.X)

        If Math.Abs(m2) > 1.0 Then
            ocvb.result2.Line(p1, p2, cv.Scalar.Yellow, 10, cv.LineTypes.AntiAlias)
        Else
            p1 = New cv.Point2f(mean.Val0 - Math.Cos(-theta) * Len / 2, mean.Val1 - Math.Sin(-theta) * Len / 2)
            p2 = New cv.Point2f(mean.Val0 + Math.Cos(-theta) * Len / 2, mean.Val1 + Math.Sin(-theta) * Len / 2)
            m2 = (p2.Y - p1.Y) / (p2.X - p1.X)
            ocvb.result2.Line(p1, p2, cv.Scalar.Yellow, 10, cv.LineTypes.AntiAlias)
        End If

        ocvb.putText(New ActiveClass.TrueType("GT m = " + Format(noisyLine.m, "#0.00") + " eigen m = " + Format(m2, "#0.00") + "    len = " + CStr(CInt(Len)), 10, 22, RESULT2))
        ocvb.putText(New ActiveClass.TrueType("Confidence = " + Format(eigenVal.At(Of Single)(0, 0) / eigenVal.At(Of Single)(1, 0), "#0.0"), 10, 40, RESULT2))
        ocvb.putText(New ActiveClass.TrueType("theta: atan2(" + Format(eigenVec.At(Of Single)(1, 0), "#0.0") + ", " + Format(eigenVec.At(Of Single)(0, 0), "#0.0") + ") = " +
                                            Format(theta, "#0.0000"), 10, 60, RESULT2))

        p1 = New cv.Point(0, noisyLine.bb)
        p2 = New cv.Point(w, noisyLine.m * w + noisyLine.bb)
        ocvb.result2.Line(p1, p2, cv.Scalar.Blue, 3, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        noisyLine.Dispose()
    End Sub
End Class



