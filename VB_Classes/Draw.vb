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
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Rectangle Count", 1, 255, 3)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Draw the requested number of rotated rectangles."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            ocvb.result1.SetTo(cv.scalar.white)
            For i = 0 To sliders.TrackBar1.Value - 1
                Dim nPoint = New cv.Point2f(ocvb.ms_rng.next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
                Dim width = ocvb.ms_rng.next(0, ocvb.color.Cols - nPoint.X - 1)
                Dim height = ocvb.ms_rng.next(0, ocvb.color.Rows - nPoint.Y - 1)
                Dim eSize = New cv.Size2f(CSng(ocvb.ms_rng.next(0, ocvb.color.Cols - nPoint.X - 1)), CSng(ocvb.ms_rng.next(0, ocvb.color.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(ocvb.ms_rng.next(0, 1000) / 1000.0F)
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
    Public Sub New(ocvb As AlgorithmData)
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
            Dim i = ocvb.ms_rng.next(0, ocvb.color.Cols - 1)
            Dim j = ocvb.ms_rng.next(0, ocvb.color.Rows - 1)
            Dim center = New cv.Point2f(i, j)
            Dim c = New cv.Scalar(ocvb.ms_rng.next(0, 255), ocvb.ms_rng.next(0, 255), ocvb.ms_rng.next(0, 255))
            If addRandomColor = False Then c = cv.Scalar.Black
            Dim noiseWidth = ocvb.ms_rng.next(1, maxNoiseWidth)
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
    Public Sub New(ocvb As AlgorithmData)
        rect = New Draw_rectangles(ocvb)
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
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Ellipse Count", 1, 255, 3)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Draw the requested number of ellipses."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            ocvb.result1.SetTo(cv.Scalar.White)
            For i = 0 To sliders.TrackBar1.Value - 1
                Dim nPoint = New cv.Point2f(ocvb.ms_rng.next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
                Dim eSize = New cv.Size2f(CSng(ocvb.ms_rng.next(0, ocvb.color.Cols - nPoint.X - 1)), CSng(ocvb.ms_rng.next(0, ocvb.color.Rows - nPoint.Y - 1)))
                Dim angle = 180.0F * CSng(ocvb.ms_rng.next(0, 1000) / 1000.0F)
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
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Circle Count", 1, 255, 3)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Draw the requested number of circles."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency = 0 Then
            ocvb.result1.SetTo(cv.Scalar.White)
            For i = 0 To sliders.TrackBar1.Value - 1
                Dim nPoint = New cv.Point2f(ocvb.ms_rng.next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
                Dim radius = ocvb.ms_rng.next(10, 10 + ocvb.ms_rng.next(ocvb.color.Cols / 4))
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
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Line Count", 1, 255, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Draw the requested number of Lines."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod updateFrequency Then Exit Sub
        ocvb.result1.SetTo(cv.Scalar.White)
        For i = 0 To sliders.TrackBar1.Value - 1
            Dim nPoint1 = New cv.Point2f(ocvb.ms_rng.next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
            Dim nPoint2 = New cv.Point2f(ocvb.ms_rng.next(ocvb.color.Cols / 4, ocvb.color.Cols * 3 / 4), ocvb.ms_rng.next(ocvb.color.Rows / 4, ocvb.color.Rows * 3 / 4))
            Dim thickness = ocvb.ms_rng.next(1, 10)
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
    Public Sub New(ocvb As AlgorithmData)
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
    Public Sub New(ocvb As AlgorithmData)
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