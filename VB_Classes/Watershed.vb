Imports cv = OpenCvSharp
Public Class Watershed_Basics
    Inherits ocvbClass
    Public markerMask As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        label1 = "Draw with left-click to select region."
        label2 = "Mask for watershed (selected regions)."
        ocvb.desc = "Watershed API experiment.  Draw on the image to test."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.drawRect.Width > 0 And ocvb.drawRect.Height > 0 Then
            cv.Cv2.Rectangle(src, ocvb.drawRect, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
            ocvb.drawRect = New cv.Rect(0, 0, 0, 0)
        Else
            dst2 = src
        End If

        Dim gray = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If gray.CountNonZero() Then
            cv.Cv2.Rectangle(markerMask, ocvb.drawRect, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)

            Dim contours()() As cv.Point = Nothing
            Dim hierarchyIndexes() As cv.HierarchyIndex = Nothing
            cv.Cv2.FindContours(markerMask, contours, hierarchyIndexes, cv.RetrievalModes.CComp, cv.ContourApproximationModes.ApproxSimple)
            If contours.Length > 0 Then
                Dim markers = New cv.Mat(markerMask.Size(), cv.MatType.CV_32S, cv.Scalar.All(0))
                Dim componentCount = 0
                Dim contourIndex = 0
                While contourIndex >= 0
                    cv.Cv2.DrawContours(markers, contours, contourIndex, cv.Scalar.All(componentCount + 1), -1, cv.LineTypes.Link8, hierarchyIndexes)
                    componentCount += 1
                    contourIndex = hierarchyIndexes(contourIndex).Next
                End While

                cv.Cv2.Watershed(src, markers)
                For y = 0 To dst1.Rows - 1
                    For x = 0 To dst1.Cols - 1
                        Dim idx = markers.Get(Of Int32)(y, x)
                        If idx = -1 Then
                            dst1.Set(Of cv.Vec3b)(y, x, New cv.Vec3b(255, 255, 255))
                        ElseIf idx <= 0 Or idx > componentCount Then
                            ' already marked zero...
                        Else
                            dst1.Set(Of cv.Vec3b)(y, x, rColors((idx - 1) Mod 255))
                        End If
                    Next
                Next
                dst1 = dst1 * 0.5 + src * 0.5
            End If
        Else
            dst1 = src
        End If
    End Sub
End Class



Public Class Watershed_DepthAuto
    Inherits ocvbClass
    Dim watershed As Watershed_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        watershed = New Watershed_Basics(ocvb)
        ocvb.desc = "Watershed the depth image using shadow, close, and far points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1 = ocvb.RGBDepth / 64
        dst1 *= 64

        ' erode the blobs at distinct depths to keep them separate
        Dim morphShape = cv.MorphShapes.Cross
        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(5, 5))
        dst2 = dst1.Erode(element, New cv.Point(3, 3), 5)

        watershed.src = dst2
        watershed.Run(ocvb)
        dst1 = watershed.dst1
    End Sub
End Class




Public Class Watershed_RGBSimpleAuto
    Inherits ocvbClass
    Dim watershed As Watershed_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        dst2.SetTo(0)

        Dim topLeft = New cv.Rect(0, 0, 100, 100)
        Dim topRight = New cv.Rect(ocvb.color.cols - 100, 0, 100, 100)
        Dim botLeft = New cv.Rect(0, ocvb.color.Rows - 100, 100, 100)
        Dim botRight = New cv.Rect(ocvb.color.cols - 100, ocvb.color.Rows - 100, 100, 100)

        cv.Cv2.Rectangle(dst2, topLeft, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        cv.Cv2.Rectangle(dst2, topRight, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        cv.Cv2.Rectangle(dst2, botLeft, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        cv.Cv2.Rectangle(dst2, botRight, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)

        watershed = New Watershed_Basics(ocvb)
        watershed.markerMask = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        ocvb.desc = "Watershed the depth image using shadow, close, and far points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        watershed.src = src
        watershed.Run(ocvb)
        dst1 = watershed.dst1
    End Sub
End Class




Public Class Watershed_RGBDepthAuto
    Inherits ocvbClass
    Dim watershed As Watershed_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        Dim topLeft = New cv.Rect(0, 0, 100, 100)
        Dim topRight = New cv.Rect(ocvb.color.cols - 100, 0, 100, 100)
        Dim botLeft = New cv.Rect(0, ocvb.color.Rows - 100, 100, 100)
        Dim botRight = New cv.Rect(ocvb.color.cols - 100, ocvb.color.Rows - 100, 100, 100)

        cv.Cv2.Rectangle(dst2, topLeft, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        cv.Cv2.Rectangle(dst2, topRight, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        cv.Cv2.Rectangle(dst2, botLeft, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        cv.Cv2.Rectangle(dst2, botRight, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)

        watershed = New Watershed_Basics(ocvb)
        watershed.markerMask = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        ocvb.desc = "Watershed the depth image using shadow, close, and far points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        watershed.src = ocvb.RGBDepth
        watershed.Run(ocvb)
        dst1 = watershed.dst1
    End Sub
End Class


