Imports cv = OpenCvSharp

Public Class Contours_Basics : Implements IDisposable
    Public rotatedRect As Draw_rotatedRectangles
    Public radio1 As New OptionsRadioButtons
    Public radio2 As New OptionsRadioButtons
    Public externalUse As Boolean
    Public src As New cv.Mat
    Public dst As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        radio1.Setup(ocvb, 5)
        radio1.Text = "Retrieval Mode Options"
        radio1.check(0).Text = "CComp"
        radio1.check(1).Text = "External"
        radio1.check(2).Text = "FloodFill"
        radio1.check(3).Text = "List"
        radio1.check(4).Text = "Tree"
        radio1.check(4).Checked = True
        If ocvb.parms.ShowOptions Then radio1.Show()

        radio2.Setup(ocvb, 4)
        radio2.Text = "ContourApproximation Mode"
        radio2.check(0).Text = "ApproxNone"
        radio2.check(1).Text = "ApproxSimple"
        radio2.check(2).Text = "ApproxTC89KCOS"
        radio2.check(3).Text = "ApproxTC89L1"
        radio2.check(1).Checked = True
        If ocvb.parms.ShowOptions Then radio2.Show()

        rotatedRect = New Draw_rotatedRectangles(ocvb, "Contours_Basics")
        rotatedRect.rect.sliders.TrackBar1.Value = 5
        ocvb.desc = "Demo options on FindContours."
        ocvb.label2 = "FindContours output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim retrievalMode As cv.RetrievalModes
        Dim ApproximationMode As cv.ContourApproximationModes
        For i = 0 To radio1.check.Count - 1
            If radio1.check(i).Checked Then
                retrievalMode = Choose(i + 1, cv.RetrievalModes.CComp, cv.RetrievalModes.External, cv.RetrievalModes.FloodFill, cv.RetrievalModes.List, cv.RetrievalModes.Tree)
                Exit For
            End If
        Next
        For i = 0 To radio2.check.Count - 1
            If radio2.check(i).Checked Then
                ApproximationMode = Choose(i + 1, cv.ContourApproximationModes.ApproxNone, cv.ContourApproximationModes.ApproxSimple,
                                              cv.ContourApproximationModes.ApproxTC89KCOS, cv.ContourApproximationModes.ApproxTC89L1)
                Exit For
            End If
        Next

        If externalUse = False Then
            src = New cv.Mat(ocvb.result1.Size(), cv.MatType.CV_8UC1)
            rotatedRect.Run(ocvb)
            src = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(254, 255, cv.ThresholdTypes.BinaryInv)
        End If

        dst = New cv.Mat(ocvb.result2.Size(), cv.MatType.CV_8UC1, 0)

        Dim contours0 As cv.Point()()
        If retrievalMode = cv.RetrievalModes.FloodFill Then
            Dim img32sc1 As New cv.Mat
            src.ConvertTo(img32sc1, cv.MatType.CV_32SC1)
            contours0 = cv.Cv2.FindContoursAsArray(img32sc1, retrievalMode, ApproximationMode)
            img32sc1.ConvertTo(dst, cv.MatType.CV_8UC1)
        Else
            contours0 = cv.Cv2.FindContoursAsArray(src, retrievalMode, ApproximationMode)
        End If

        Dim contours()() As cv.Point = Nothing
        ReDim contours(contours0.Length - 1)
        For j = 0 To contours0.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(contours0(j), 3, True)
        Next

        cv.Cv2.DrawContours(dst, contours, 0, New cv.Scalar(0, 255, 255), 2, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        rotatedRect.Dispose()
        radio1.Dispose()
        radio2.Dispose()
    End Sub
End Class



Public Class Contours_FindandDraw : Implements IDisposable
    Dim rotatedRect As Draw_rotatedRectangles
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        rotatedRect = New Draw_rotatedRectangles(ocvb, "Contours_FindandDraw")
        rotatedRect.rect.sliders.TrackBar1.Value = 5
        ocvb.label1 = "FindandDraw input"
        ocvb.label2 = "FindandDraw output"
        ocvb.desc = "Demo the use of FindContours, ApproxPolyDP, and DrawContours."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim img As New cv.Mat(ocvb.result1.Size(), cv.MatType.CV_8UC1)
        rotatedRect.Run(ocvb)
        img = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(254, 255, cv.ThresholdTypes.BinaryInv)

        Dim contours0 = cv.Cv2.FindContoursAsArray(img, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim contours()() As cv.Point = Nothing
        ReDim contours(contours0.Length - 1)
        For j = 0 To contours0.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(contours0(j), 3, True)
        Next

        ocvb.result2.SetTo(0)
        cv.Cv2.DrawContours(ocvb.result2, contours, 0, New cv.Scalar(0, 255, 255), 2, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        rotatedRect.Dispose()
    End Sub
End Class



Public Class Contours_Depth : Implements IDisposable
    Public trim As Depth_InRange
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        trim = New Depth_InRange(ocvb, "Contours_Depth")
        ocvb.desc = "Find and draw the contour of the depth foreground."
        ocvb.label1 = "DepthContour input"
        ocvb.label2 = "DepthContour output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        ocvb.result2.SetTo(0)
        Dim contours0 = cv.Cv2.FindContoursAsArray(trim.Mask, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim maxIndex As Int32
        Dim maxNodes As Int32
        For i = 0 To contours0.Length - 1
            Dim contours = cv.Cv2.ApproxPolyDP(contours0(i), 3, True)
            If maxNodes < contours.Length Then
                maxIndex = i
                maxNodes = contours.Length
            End If
        Next
        cv.Cv2.DrawContours(ocvb.result2, contours0, maxIndex, New cv.Scalar(0, 255, 255), -1)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        trim.Dispose()
    End Sub
End Class



Public Class Contours_RGB : Implements IDisposable
    Dim trim As Depth_InRange
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        Dim callerName = caller
        If callerName = "" Then callerName = Me.GetType.Name Else callerName += "-->" + Me.GetType.Name
        trim = New Depth_InRange(ocvb, "Contours_RGB")
        ocvb.desc = "Find and draw the contour of the largest foreground RGB contour."
        ocvb.label2 = "Background"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        Dim img = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        img.SetTo(0, trim.zeroMask)

        Dim contours0 = cv.Cv2.FindContoursAsArray(img, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim maxIndex As Int32
        Dim maxNodes As Int32
        For i = 0 To contours0.Length - 1
            Dim contours = cv.Cv2.ApproxPolyDP(contours0(i), 3, True)
            If maxNodes < contours.Length Then
                maxIndex = i
                maxNodes = contours.Length
            End If
        Next

        If contours0(maxIndex).Length = 0 Then Exit Sub

        ocvb.result1.SetTo(New cv.Scalar(0))
        Dim hull() = cv.Cv2.ConvexHull(contours0(maxIndex), True)
        Dim listOfPoints = New List(Of List(Of cv.Point))
        Dim points = New List(Of cv.Point)
        For i = 0 To hull.Count - 1
            points.Add(New cv.Point(hull(i).X, hull(i).Y))
        Next
        listOfPoints.Add(points)
        cv.Cv2.DrawContours(ocvb.result1, listOfPoints, 0, New cv.Scalar(255, 0, 0), -1)
        cv.Cv2.DrawContours(ocvb.result1, contours0, maxIndex, New cv.Scalar(0, 255, 255), -1)
        ocvb.result2.SetTo(0)
        ocvb.color.CopyTo(ocvb.result2, trim.zeroMask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        trim.Dispose()
    End Sub
End Class
