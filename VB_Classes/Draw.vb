Imports cv = OpenCvSharp

Module Draw_Exports
    Dim rng As System.Random
    Public Sub drawRotatedRectangle(rotatedRect As cv.RotatedRect, dst As cv.Mat, color As cv.Scalar)
        Dim vertices2f(3) As cv.Point2f
        vertices2f = rotatedRect.Points()
        Dim vertices(vertices2f.Length - 1) As cv.Point
        For j = 0 To vertices2f.Length - 1
            vertices(j) = New cv.Point(CInt(vertices2f(j).X), CInt(vertices2f(j).Y))
        Next
        cv.Cv2.FillConvexPoly(dst, vertices, color, cv.LineTypes.AntiAlias)
    End Sub
End Module
Public Class Draw_rectangles : Implements IDisposable
    Public sliders As New OptionsSliders
    Public updateFrequency = 30
    Public drawRotatedRectangles As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Rectangle Count", 1, 255, 3)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Draw the requested number of rotated rectangles."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            ocvb.result1.SetTo(cv.Scalar.White)
            For i = 0 To sliders.TrackBar1.Value - 1
                Dim nPoint = New cv.Point2f(ocvb.ms_rng.Next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.Next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
                Dim width = ocvb.ms_rng.Next(0, ocvb.color.Cols - nPoint.X - 1)
                Dim height = ocvb.ms_rng.Next(0, ocvb.color.Rows - nPoint.Y - 1)
                Dim eSize = New cv.Size2f(CSng(ocvb.ms_rng.Next(0, ocvb.color.Cols - nPoint.X - 1)), CSng(ocvb.ms_rng.Next(0, ocvb.color.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(ocvb.ms_rng.Next(0, 1000) / 1000.0F)
                Dim rotatedRect = New cv.RotatedRect(nPoint, eSize, angle)

                Dim nextColor = New cv.Scalar(ocvb.rColors(i).Item0, ocvb.rColors(i).Item1, ocvb.rColors(i).Item2)
                If drawRotatedRectangles Then
                    drawRotatedRectangle(rotatedRect, ocvb.result1, nextColor)
                Else
                    cv.Cv2.Rectangle(ocvb.result1, New cv.Rect(nPoint.X, nPoint.Y, width, height), nextColor, -1)
                End If
            Next
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Draw_Noise : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public maxNoiseWidth As Int32 = 3
    Public addRandomColor As Boolean
    Public noiseMask As cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Noise Count", 1, 1000, 100)
        sliders.setupTrackBar2(ocvb, "Noise Width", 1, 10, 3)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Add Noise to the color image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        maxNoiseWidth = sliders.TrackBar2.Value
        ocvb.color.CopyTo(ocvb.result1)
        noiseMask = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1).SetTo(0)
        For n = 0 To sliders.TrackBar1.Value
            Dim i = ocvb.ms_rng.Next(0, ocvb.color.Cols - 1)
            Dim j = ocvb.ms_rng.Next(0, ocvb.color.Rows - 1)
            Dim center = New cv.Point2f(i, j)
            Dim c = New cv.Scalar(ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255))
            If addRandomColor = False Then c = cv.Scalar.Black
            Dim noiseWidth = ocvb.ms_rng.Next(1, maxNoiseWidth)
            ocvb.result1.Circle(center, noiseWidth, c, -1, cv.LineTypes.AntiAlias)
            noiseMask.Circle(center, noiseWidth, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Draw_rotatedRectangles : Implements IDisposable
    Public rect As Draw_rectangles
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        rect = New Draw_rectangles(ocvb, "Draw_rotatedRectangles")
        rect.drawRotatedRectangles = True
        ocvb.desc = "Draw the requested number of rectangles."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        rect.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        rect.Dispose()
    End Sub
End Class



Public Class Draw_Ellipses : Implements IDisposable
    Public sliders As New OptionsSliders
    Public updateFrequency = 30
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Ellipse Count", 1, 255, 3)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Draw the requested number of ellipses."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            ocvb.result1.SetTo(cv.Scalar.White)
            For i = 0 To sliders.TrackBar1.Value - 1
                Dim nPoint = New cv.Point2f(ocvb.ms_rng.Next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.Next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
                Dim eSize = New cv.Size2f(CSng(ocvb.ms_rng.Next(0, ocvb.color.Cols - nPoint.X - 1)), CSng(ocvb.ms_rng.Next(0, ocvb.color.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(ocvb.ms_rng.Next(0, 1000) / 1000.0F)
                Dim nextColor = New cv.Scalar(ocvb.rColors(i).Item0, ocvb.rColors(i).Item1, ocvb.rColors(i).Item2)
                ocvb.result1.Ellipse(New cv.RotatedRect(nPoint, eSize, angle), nextColor, -1,)
            Next
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Draw_Circles : Implements IDisposable
    Public sliders As New OptionsSliders
    Public updateFrequency = 30
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Circle Count", 1, 255, 3)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Draw the requested number of circles."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            ocvb.result1.SetTo(cv.Scalar.White)
            For i = 0 To sliders.TrackBar1.Value - 1
                Dim nPoint = New cv.Point2f(ocvb.ms_rng.Next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.Next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
                Dim radius = ocvb.ms_rng.Next(10, 10 + ocvb.ms_rng.Next(ocvb.color.Cols / 4))
                Dim nextColor = New cv.Scalar(ocvb.rColors(i).Item0, ocvb.rColors(i).Item1, ocvb.rColors(i).Item2)
                ocvb.result1.Circle(nPoint, radius, nextColor, -1,)
            Next
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Draw_Line : Implements IDisposable
    Public sliders As New OptionsSliders
    Public updateFrequency = 30
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Line Count", 1, 255, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Draw the requested number of Lines."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency Then Exit Sub
        ocvb.result1.SetTo(cv.Scalar.White)
        For i = 0 To sliders.TrackBar1.Value - 1
            Dim nPoint1 = New cv.Point2f(ocvb.ms_rng.Next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.Next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
            Dim nPoint2 = New cv.Point2f(ocvb.ms_rng.Next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.Next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
            Dim thickness = ocvb.ms_rng.Next(1, 10)
            Dim nextColor = New cv.Scalar(ocvb.rColors(i).Item0, ocvb.rColors(i).Item1, ocvb.rColors(i).Item2)
            ocvb.result1.Line(nPoint1, nPoint2, nextColor, thickness, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Draw_Polygon : Implements IDisposable
    Public radio As New OptionsRadioButtons
    Public updateFrequency = 30
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Poly Count", 1, 255, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Draw Polygon figures"
        ocvb.label2 = "Convex Hull for the same polygon"

        radio.Setup(ocvb, 2) ' ask for 2 radio buttons
        radio.check(0).Text = "Polygon Outline"
        radio.check(1).Text = "Polygon Filled"
        radio.check(0).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim h = ocvb.color.Height / 8
        Dim w = ocvb.color.Width / 8
        Dim polyColor = New cv.Scalar(ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255), ocvb.ms_rng.Next(0, 255))
        'If ocvb.frameCount Mod updateFrequency = 0 Then
        ocvb.result1.SetTo(cv.Scalar.White)
        For i = 0 To sliders.TrackBar1.Value - 1
            Dim points = New List(Of cv.Point)
            Dim listOfPoints = New List(Of List(Of cv.Point))
            For j = 0 To 10
                points.Add(New cv.Point(CInt(ocvb.ms_rng.Next(w, w * 7)), CInt(ocvb.ms_rng.Next(h, h * 7))))
            Next
            listOfPoints.Add(points)
            If radio.check(0).Checked Then
                cv.Cv2.Polylines(ocvb.result1, listOfPoints, True, polyColor, 2, cv.LineTypes.AntiAlias)
            Else
                ocvb.result1.FillPoly(listOfPoints, New cv.Scalar(0, 0, 255))
            End If

            Dim hull() As cv.Point
            hull = cv.Cv2.ConvexHull(points, True)
            listOfPoints = New List(Of List(Of cv.Point))
            points = New List(Of cv.Point)
            For j = 0 To hull.Count - 1
                points.Add(New cv.Point(hull(j).X, hull(j).Y))
            Next
            listOfPoints.Add(points)
            ocvb.result2.SetTo(cv.Scalar.White)
            If radio.check(0).Checked Then
                cv.Cv2.DrawContours(ocvb.result2, listOfPoints, 0, polyColor, 2)
            Else
                cv.Cv2.DrawContours(ocvb.result2, listOfPoints, 0, polyColor, -1)
            End If
        Next
        'End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class Draw_RngImage : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        ocvb.desc = "Use RNG to draw the same set of shapes every time"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rng As New cv.RNG
        Dim offsetX = 50, offsetY = 25, lineLength = 50, thickness = 2

        ocvb.result1.SetTo(0)
        For i = 1 To 256
            ocvb.result1.Line(New cv.Point(thickness * i + offsetX, offsetY), New cv.Point(thickness * i + offsetX, offsetY + lineLength), New cv.Scalar(i, i, i), thickness)
        Next
        For i = 1 To 256
            Dim color = New cv.Scalar(rng.Uniform(0, 255), rng.Uniform(0, 255), rng.Uniform(0, 255))
            Select Case rng.Uniform(0, 3)
                Case 0 ' circle
                    Dim center = New cv.Point(rng.Uniform(offsetX, ocvb.result1.Cols - offsetX), rng.Uniform(offsetY + lineLength, ocvb.result1.Rows - offsetY))
                    Dim radius = rng.Uniform(1, Math.Min(offsetX, offsetY))
                    ocvb.result1.Circle(center, radius, color, -1, cv.LineTypes.Link8)
                Case 1 ' Rectangle
                    Dim center = New cv.Point(rng.Uniform(offsetX, ocvb.result1.Cols - offsetX), rng.Uniform(offsetY + lineLength, ocvb.result1.Rows - offsetY))
                    Dim width = rng.Uniform(1, Math.Min(offsetX, offsetY))
                    Dim height = rng.Uniform(1, Math.Min(offsetX, offsetY))
                    Dim rc = New cv.Rect(center.X - width, center.Y - height / 2, width, height)
                    ocvb.result1.Rectangle(rc, color, -1, cv.LineTypes.Link8)
                Case 2 ' Ellipse
                    Dim center = New cv.Point(rng.Uniform(offsetX, ocvb.result1.Cols - offsetX), rng.Uniform(offsetY + lineLength, ocvb.result1.Rows - offsetY))
                    Dim width = rng.Uniform(1, Math.Min(offsetX, offsetY))
                    Dim height = rng.Uniform(1, Math.Min(offsetX, offsetY))
                    Dim angle = rng.Uniform(0, 180)
                    ocvb.result1.Ellipse(center, New cv.Size(width / 2, height / 2), angle, 0, 360, color, -1, cv.LineTypes.Link8)
            End Select
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class





Public Class Draw_SymmetricalShapes : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "Number of points", 200, 1000, 500)
        sliders.setupTrackBar2(ocvb, "Radius 1", 1, ocvb.color.Height / 2, ocvb.color.Height / 4)
        sliders.setupTrackBar3(ocvb, "Radius 2", 1, ocvb.color.Height / 2, ocvb.color.Height / 8)
        sliders.setupTrackBar4(ocvb, "nGenPer", 1, 500, 100)
        If ocvb.parms.ShowOptions Then sliders.Show()

        check.Setup(ocvb, 5)
        check.Box(0).Text = "Symmetric Ripple"
        check.Box(1).Text = "Only Regular Shapes"
        check.Box(2).Text = "Filled Shapes"
        check.Box(3).Text = "Reverse In/Out"
        check.Box(4).Text = "Use demo mode"
        check.Box(4).Checked = True
        If ocvb.parms.ShowOptions Then check.Show()
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
                fillColor = ocvb.colorScalar(ocvb.frameCount Mod 255)
            End If
            If ocvb.frameCount Mod 37 = 0 Then check.Box(0).Checked = Not check.Box(0).Checked
            If ocvb.frameCount Mod 222 = 0 Then check.Box(1).Checked = Not check.Box(1).Checked
            If ocvb.frameCount Mod 77 = 0 Then check.Box(2).Checked = Not check.Box(2).Checked
            If ocvb.frameCount Mod 100 = 0 Then check.Box(3).Checked = Not check.Box(3).Checked
            rotateAngle += 1

        End If

        ocvb.result1.SetTo(0)
        Dim numPoints = sliders.TrackBar1.Value
        Dim nGenPer = sliders.TrackBar4.Value
        If check.Box(1).Checked Then numPoints = CInt(numPoints / nGenPer) * nGenPer ' harmonize
        Dim radius1 = sliders.TrackBar2.Value
        Dim radius2 = sliders.TrackBar3.Value
        Dim dTheta = 2 * cv.Cv2.PI / numPoints
        Dim symmetricRipple = check.Box(0).Checked
        Dim reverseInOut = check.Box(3).Checked
        Dim pt As New cv.Point
        Dim center As New cv.Point(ocvb.color.Width / 2, ocvb.color.Height / 2)
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

        ocvb.result1.SetTo(0)
        For i = 0 To numPoints - 1
            ocvb.result1.Line(points.ElementAt(i), points.ElementAt((i + 1) Mod numPoints), ocvb.colorScalar(i Mod ocvb.colorScalar.Count), 2, cv.LineTypes.AntiAlias)
        Next

        If check.Box(2).Checked Then ocvb.result1.FloodFill(center, fillColor)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        check.Dispose()
    End Sub
End Class