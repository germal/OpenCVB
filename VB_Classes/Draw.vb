Imports cv = OpenCvSharp

Module Draw_Exports
    Dim rng As System.Random
    Public Sub drawRotatedRectangle(rotatedRect As cv.RotatedRect, dst1 As cv.Mat, color As cv.Scalar)
        Dim vertices2f(3) As cv.Point2f
        vertices2f = rotatedRect.Points()
        Dim vertices(vertices2f.Length - 1) As cv.Point
        For j = 0 To vertices2f.Length - 1
            vertices(j) = New cv.Point(CInt(vertices2f(j).X), CInt(vertices2f(j).Y))
        Next
        cv.Cv2.FillConvexPoly(dst1, vertices, color, cv.LineTypes.AntiAlias)
    End Sub
    Public Function initRandomRect(w As Integer, h As Integer, margin As Integer) As cv.Rect

        Dim width As Integer, x As Integer, height As Integer, y As Integer
        While width < 5 ' don't let the w/h get too small...
            x = (w - margin * 2) * Rnd() + margin
            width = (w - x - margin * 2) * Rnd()
        End While

        While height < 5 ' don't let the w/h get too small...
            y = (h - margin * 2) * Rnd() + margin
            height = (h - y - margin * 2) * Rnd()
        End While

        Return New cv.Rect(x, y, width, height)
    End Function
End Module




Public Class Draw_rectangles
    Inherits ocvbClass
    Public updateFrequency = 30
    Public drawRotatedRectangles As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Rectangle Count", 1, 255, 3)
        ocvb.desc = "Draw the requested number of rotated rectangles."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            dst1.SetTo(cv.Scalar.White)
            For i = 0 To sliders.TrackBar1.Value - 1
                Dim nPoint = New cv.Point2f(ocvb.ms_rng.Next(src.Cols / 4, src.Cols * 3 / 4), ocvb.ms_rng.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim width = ocvb.ms_rng.Next(0, src.Cols - nPoint.X - 1)
                Dim height = ocvb.ms_rng.Next(0, src.Rows - nPoint.Y - 1)
                Dim eSize = New cv.Size2f(CSng(ocvb.ms_rng.Next(0, src.Cols - nPoint.X - 1)), CSng(ocvb.ms_rng.Next(0, src.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(ocvb.ms_rng.Next(0, 1000) / 1000.0F)
                Dim rotatedRect = New cv.RotatedRect(nPoint, eSize, angle)

                Dim nextColor = New cv.Scalar(ocvb.rColors(i).Item0, ocvb.rColors(i).Item1, ocvb.rColors(i).Item2)
                If drawRotatedRectangles Then
                    drawRotatedRectangle(rotatedRect, dst1, nextColor)
                Else
                    cv.Cv2.Rectangle(dst1, New cv.Rect(nPoint.X, nPoint.Y, width, height), nextColor, -1)
                End If
            Next
        End If
    End Sub
End Class



Public Class Draw_Noise
    Inherits ocvbClass
    Public maxNoiseWidth As Int32 = 3
    Public addRandomColor As Boolean
    Public noiseMask As cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Noise Count", 1, 1000, 100)
        sliders.setupTrackBar2(ocvb, caller, "Noise Width", 1, 10, 3)
        ocvb.desc = "Add Noise to the color image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        maxNoiseWidth = sliders.TrackBar2.Value
        src.CopyTo(dst1)
        noiseMask = New cv.Mat(src.Size(), cv.MatType.CV_8UC1).SetTo(0)
        For n = 0 To sliders.TrackBar1.Value
            Dim i = ocvb.ms_rng.Next(0, src.Cols - 1)
            Dim j = ocvb.ms_rng.Next(0, src.Rows - 1)
            Dim center = New cv.Point2f(i, j)
            Dim c = New cv.Scalar(ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255))
            If addRandomColor = False Then c = cv.Scalar.Black
            Dim noiseWidth = ocvb.ms_rng.Next(1, maxNoiseWidth)
            dst1.Circle(center, noiseWidth, c, -1, cv.LineTypes.AntiAlias)
            noiseMask.Circle(center, noiseWidth, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class



Public Class Draw_rotatedRectangles
    Inherits ocvbClass
    Public rect As Draw_rectangles
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        rect = New Draw_rectangles(ocvb, caller)
        rect.drawRotatedRectangles = True
        ocvb.desc = "Draw the requested number of rectangles."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        rect.src = src
        rect.Run(ocvb)
        dst1 = rect.dst1
    End Sub
End Class



Public Class Draw_Ellipses
    Inherits ocvbClass
    Public updateFrequency = 30
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Ellipse Count", 1, 255, 3)
        ocvb.desc = "Draw the requested number of ellipses."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            dst1.SetTo(cv.Scalar.White)
            For i = 0 To sliders.TrackBar1.Value - 1
                Dim nPoint = New cv.Point2f(ocvb.ms_rng.Next(src.Cols / 4, src.Cols * 3 / 4), ocvb.ms_rng.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim eSize = New cv.Size2f(CSng(ocvb.ms_rng.Next(0, src.Cols - nPoint.X - 1)), CSng(ocvb.ms_rng.Next(0, src.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(ocvb.ms_rng.Next(0, 1000) / 1000.0F)
                Dim nextColor = New cv.Scalar(ocvb.rColors(i).Item0, ocvb.rColors(i).Item1, ocvb.rColors(i).Item2)
                dst1.Ellipse(New cv.RotatedRect(nPoint, eSize, angle), nextColor, -1,)
            Next
        End If
    End Sub
End Class



Public Class Draw_Circles
    Inherits ocvbClass
    Public updateFrequency = 30
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Circle Count", 1, 255, 3)
        ocvb.desc = "Draw the requested number of circles."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            dst1.SetTo(cv.Scalar.White)
            For i = 0 To sliders.TrackBar1.Value - 1
                Dim nPoint = New cv.Point2f(ocvb.ms_rng.Next(src.Cols / 4, src.Cols * 3 / 4), ocvb.ms_rng.Next(src.Rows / 4, src.Rows * 3 / 4))
                Dim radius = ocvb.ms_rng.Next(10, 10 + ocvb.ms_rng.Next(src.Cols / 4))
                Dim nextColor = New cv.Scalar(ocvb.rColors(i).Item0, ocvb.rColors(i).Item1, ocvb.rColors(i).Item2)
                dst1.Circle(nPoint, radius, nextColor, -1,)
            Next
        End If
    End Sub
End Class



Public Class Draw_Line
    Inherits ocvbClass
    Public updateFrequency = 30
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Line Count", 1, 255, 1)
        ocvb.desc = "Draw the requested number of Lines."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency Then Exit Sub
        dst1.SetTo(cv.Scalar.White)
        For i = 0 To sliders.TrackBar1.Value - 1
            Dim nPoint1 = New cv.Point2f(ocvb.ms_rng.Next(src.Cols / 4, src.Cols * 3 / 4), ocvb.ms_rng.Next(src.Rows / 4, src.Rows * 3 / 4))
            Dim nPoint2 = New cv.Point2f(ocvb.ms_rng.Next(src.Cols / 4, src.Cols * 3 / 4), ocvb.ms_rng.Next(src.Rows / 4, src.Rows * 3 / 4))
            Dim thickness = ocvb.ms_rng.Next(1, 10)
            Dim nextColor = New cv.Scalar(ocvb.rColors(i).Item0, ocvb.rColors(i).Item1, ocvb.rColors(i).Item2)
            dst1.Line(nPoint1, nPoint2, nextColor, thickness, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class



Public Class Draw_Polygon
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Poly Count", 1, 255, 1)
        ocvb.desc = "Draw Polygon figures"
        label2 = "Convex Hull for the same polygon"

        radio.Setup(ocvb, caller, 2) ' ask for 2 radio buttons
        radio.check(0).Text = "Polygon Outline"
        radio.check(1).Text = "Polygon Filled"
        radio.check(0).Checked = True
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim h = src.Height / 8
        Dim w = src.Width / 8
        Dim polyColor = New cv.Scalar(ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255))
        dst1.SetTo(cv.Scalar.White)
        dst2 = dst1.Clone()
        For i = 0 To sliders.TrackBar1.Value - 1
            Dim points = New List(Of cv.Point)
            Dim listOfPoints = New List(Of List(Of cv.Point))
            For j = 0 To 10
                points.Add(New cv.Point(CInt(ocvb.ms_rng.Next(w, w * 7)), CInt(ocvb.ms_rng.Next(h, h * 7))))
            Next
            listOfPoints.Add(points)
            If radio.check(0).Checked Then
                cv.Cv2.Polylines(dst1, listOfPoints, True, polyColor, 2, cv.LineTypes.AntiAlias)
            Else
                dst1.FillPoly(listOfPoints, New cv.Scalar(0, 0, 255))
            End If

            Dim hull() As cv.Point
            hull = cv.Cv2.ConvexHull(points, True)
            listOfPoints = New List(Of List(Of cv.Point))
            points = New List(Of cv.Point)
            For j = 0 To hull.Count - 1
                points.Add(New cv.Point(hull(j).X, hull(j).Y))
            Next
            listOfPoints.Add(points)
            dst2.SetTo(cv.Scalar.White)
            If radio.check(0).Checked Then
                cv.Cv2.DrawContours(dst2, listOfPoints, 0, polyColor, 2)
            Else
                cv.Cv2.DrawContours(dst2, listOfPoints, 0, polyColor, -1)
            End If
        Next
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class Draw_RngImage
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Use RNG to draw the same set of shapes every time"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rng As New cv.RNG
        Dim offsetX = 50, offsetY = 25, lineLength = 50, thickness = 2

        dst1.SetTo(cv.Scalar.White)
        For i = 1 To 256
            dst1.Line(New cv.Point(thickness * i + offsetX, offsetY), New cv.Point(thickness * i + offsetX, offsetY + lineLength), New cv.Scalar(i, i, i), thickness)
        Next
        For i = 1 To 256
            Dim color = New cv.Scalar(rng.Uniform(0, 255), rng.Uniform(0, 255), rng.Uniform(0, 255))
            Select Case rng.Uniform(0, 3)
                Case 0 ' circle
                    Dim center = New cv.Point(rng.Uniform(offsetX, dst1.Cols - offsetX), rng.Uniform(offsetY + lineLength, dst1.Rows - offsetY))
                    Dim radius = rng.Uniform(1, Math.Min(offsetX, offsetY))
                    dst1.Circle(center, radius, color, -1, cv.LineTypes.Link8)
                Case 1 ' Rectangle
                    Dim center = New cv.Point(rng.Uniform(offsetX, dst1.Cols - offsetX), rng.Uniform(offsetY + lineLength, dst1.Rows - offsetY))
                    Dim width = rng.Uniform(1, Math.Min(offsetX, offsetY))
                    Dim height = rng.Uniform(1, Math.Min(offsetX, offsetY))
                    Dim rc = New cv.Rect(center.X - width, center.Y - height / 2, width, height)
                    dst1.Rectangle(rc, color, -1, cv.LineTypes.Link8)
                Case 2 ' Ellipse
                    Dim center = New cv.Point(rng.Uniform(offsetX, dst1.Cols - offsetX), rng.Uniform(offsetY + lineLength, dst1.Rows - offsetY))
                    Dim width = rng.Uniform(1, Math.Min(offsetX, offsetY))
                    Dim height = rng.Uniform(1, Math.Min(offsetX, offsetY))
                    Dim angle = rng.Uniform(0, 180)
                    dst1.Ellipse(center, New cv.Size(width / 2, height / 2), angle, 0, 360, color, -1, cv.LineTypes.Link8)
            End Select
        Next
    End Sub
End Class





Public Class Draw_SymmetricalShapes
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Number of points", 200, 1000, 500)
        sliders.setupTrackBar2(ocvb, caller, "Radius 1", 1, ocvb.color.Rows / 2, ocvb.color.Rows / 4)
        sliders.setupTrackBar3(ocvb, caller, "Radius 2", 1, ocvb.color.Rows / 2, ocvb.color.Rows / 8)
        sliders.setupTrackBar4(ocvb, caller, "nGenPer", 1, 500, 100)

        check.Setup(ocvb, caller, 5)
        check.Box(0).Text = "Symmetric Ripple"
        check.Box(1).Text = "Only Regular Shapes"
        check.Box(2).Text = "Filled Shapes"
        check.Box(3).Text = "Reverse In/Out"
        check.Box(4).Text = "Use demo mode"
        check.Box(4).Checked = True
        ocvb.desc = "Generate shapes programmatically"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static rotateAngle As Single = 0
        Static fillColor = cv.Scalar.Red
        If check.Box(4).Checked Then
            If ocvb.frameCount Mod 30 = 0 Then
                If sliders.TrackBar1.Value < sliders.TrackBar1.Maximum - 17 Then sliders.TrackBar1.Value += 17 Else sliders.TrackBar1.Value = sliders.TrackBar1.Minimum
                If sliders.TrackBar2.Value < sliders.TrackBar2.Maximum - 10 Then sliders.TrackBar2.Value += 10 Else sliders.TrackBar2.Value = 1
                If sliders.TrackBar3.Value > 13 Then sliders.TrackBar3.Value -= 13 Else sliders.TrackBar3.Value = sliders.TrackBar3.Maximum
                If sliders.TrackBar4.Value > 27 Then sliders.TrackBar4.Value -= 27 Else sliders.TrackBar4.Value = sliders.TrackBar4.Maximum
                fillColor = ocvb.scalarColors(ocvb.frameCount Mod 255)
            End If
            If ocvb.frameCount Mod 37 = 0 Then check.Box(0).Checked = Not check.Box(0).Checked
            If ocvb.frameCount Mod 222 = 0 Then check.Box(1).Checked = Not check.Box(1).Checked
            If ocvb.frameCount Mod 77 = 0 Then check.Box(2).Checked = Not check.Box(2).Checked
            If ocvb.frameCount Mod 100 = 0 Then check.Box(3).Checked = Not check.Box(3).Checked
            rotateAngle += 1

        End If

        dst1.SetTo(cv.Scalar.White)
        Dim numPoints = sliders.TrackBar1.Value
        Dim nGenPer = sliders.TrackBar4.Value
        If check.Box(1).Checked Then numPoints = CInt(numPoints / nGenPer) * nGenPer ' harmonize
        Dim radius1 = sliders.TrackBar2.Value
        Dim radius2 = sliders.TrackBar3.Value
        Dim dTheta = 2 * cv.Cv2.PI / numPoints
        Dim symmetricRipple = check.Box(0).Checked
        Dim reverseInOut = check.Box(3).Checked
        Dim pt As New cv.Point
        Dim center As New cv.Point(src.Width / 2, src.Height / 2)
        Dim points As New List(Of cv.Point)

        For i = 0 To numPoints - 1
            Dim theta = i * dTheta
            Dim ripple = radius2 * Math.Cos(nGenPer * theta)
            If symmetricRipple = False Then ripple = Math.Abs(ripple)
            If reverseInOut Then ripple = -ripple
            pt.X = Math.Truncate(center.X + (radius1 + ripple) * Math.Cos(theta + rotateAngle) + 0.5)
            pt.Y = Math.Truncate(center.Y - (radius1 + ripple) * Math.Sin(theta + rotateAngle) + 0.5)
            points.Add(pt)
        Next

        For i = 0 To numPoints - 1
            dst1.Line(points.ElementAt(i), points.ElementAt((i + 1) Mod numPoints), ocvb.scalarColors(i Mod ocvb.scalarColors.Count), 2, cv.LineTypes.AntiAlias)
        Next

        If check.Box(2).Checked Then dst1.FloodFill(center, fillColor)
    End Sub
End Class





Public Class Draw_ClipLine
    Inherits ocvbClass
    Dim kalman As Kalman_Basics
    Dim flow As Font_FlowText
    Dim lastRect As cv.Rect
    Dim pt1 As cv.Point
    Dim pt2 As cv.Point
    Dim rect As cv.Rect
    Private Sub setup()
        ReDim kalman.input(8)
        Dim r = initRandomRect(dst1.Width, dst1.Height, 25)
        pt1 = New cv.Point(r.X, r.Y)
        pt2 = New cv.Point(r.X + r.Width, r.Y + r.Height)
        rect = initRandomRect(dst1.Width, dst1.Height, 25)
        flow.msgs.Add("--------------------------- setup run -------------------------")
    End Sub
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)

        flow = New Font_FlowText(ocvb, caller)
        flow.result1or2 = RESULT2

        kalman = New Kalman_Basics(ocvb, caller)
        setup()

        label2 = "test"
        ocvb.desc = "Demonstrate the use of the ClipLine function in OpenCV. NOTE: when clipline returns true, p1/p2 are clipped by the rectangle"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1 = src
        kalman.input = {pt1.X, pt1.Y, pt2.X, pt2.Y, rect.X, rect.Y, rect.Width, rect.Height}
        kalman.Run(ocvb)
        Dim p1 = New cv.Point(kalman.output(0), kalman.output(1))
        Dim p2 = New cv.Point(kalman.output(2), kalman.output(3))

        If kalman.output(6) < 5 Then kalman.output(6) = 5 ' don't let the w/h get too small...
        If kalman.output(7) < 5 Then kalman.output(7) = 5
        Dim r = New cv.Rect(kalman.output(4), kalman.output(5), kalman.output(6), kalman.output(7))

        Dim clipped = cv.Cv2.ClipLine(r, p1, p2) ' Returns false when the line and the rectangle don't intersect.
        dst1.Line(p1, p2, If(clipped, cv.Scalar.White, cv.Scalar.Black), 2, cv.LineTypes.AntiAlias)
        dst1.Rectangle(r, If(clipped, cv.Scalar.Yellow, cv.Scalar.Red), 2, cv.LineTypes.AntiAlias)

        flow.msgs.Add("line " + If(clipped, "interects rectangle", "does not intersect rectangle"))
        If r = rect Then setup()
        flow.Run(ocvb)
    End Sub
End Class
