Imports cv = OpenCvSharp

Public Class Contours_Basics
    Inherits VB_Class
    Public rotatedRect As Draw_rotatedRectangles
    Public externalUse As Boolean
    Public src As New cv.Mat
    Public dst As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        radio.Setup(ocvb, callerName,5)
        radio.Text = "Retrieval Mode Options"
        radio.check(0).Text = "CComp"
        radio.check(1).Text = "External"
        radio.check(2).Text = "FloodFill"
        radio.check(3).Text = "List"
        radio.check(4).Text = "Tree"
        radio.check(4).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()

        radio1.Setup(ocvb, callerName, 4)
        radio1.Text = "ContourApproximation Mode"
        radio1.check(0).Text = "ApproxNone"
        radio1.check(1).Text = "ApproxSimple"
        radio1.check(2).Text = "ApproxTC89KCOS"
        radio1.check(3).Text = "ApproxTC89L1"
        radio1.check(1).Checked = True
        If ocvb.parms.ShowOptions Then radio1.Show()

        rotatedRect = New Draw_rotatedRectangles(ocvb, callerName)
        rotatedRect.rect.sliders.TrackBar1.Value = 5
        ocvb.desc = "Demo options on FindContours."
        ocvb.label2 = "FindContours output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim retrievalMode As cv.RetrievalModes
        Dim ApproximationMode As cv.ContourApproximationModes
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                retrievalMode = Choose(i + 1, cv.RetrievalModes.CComp, cv.RetrievalModes.External, cv.RetrievalModes.FloodFill, cv.RetrievalModes.List, cv.RetrievalModes.Tree)
                Exit For
            End If
        Next
        For i = 0 To radio1.check.Count - 1
            If radio1.check(i).Checked Then
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
    Public Sub MyDispose()
        rotatedRect.Dispose()
        radio.Dispose()
        radio1.Dispose()
    End Sub
End Class



Public Class Contours_FindandDraw
    Inherits VB_Class
    Dim rotatedRect As Draw_rotatedRectangles
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        rotatedRect = New Draw_rotatedRectangles(ocvb, callerName)
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
    Public Sub MyDispose()
        rotatedRect.Dispose()
    End Sub
End Class



Public Class Contours_Depth
    Inherits VB_Class
    Public trim As Depth_InRange
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        trim = New Depth_InRange(ocvb, callerName)
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
    Public Sub MyDispose()
        trim.Dispose()
    End Sub
End Class



Public Class Contours_RGB
    Inherits VB_Class
    Dim trim As Depth_InRange
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        trim = New Depth_InRange(ocvb, callerName)
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
    Public Sub MyDispose()
        trim.Dispose()
    End Sub
End Class
