Imports cv = OpenCvSharp
Public Class Watershed_Basics
    Inherits ocvbClass
    Public useDepthImage As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        label1 = "Draw with left-click to select region."
        label2 = "Mask for watershed (selected regions)."
        dst2.SetTo(0)
        ocvb.desc = "Watershed API experiment.  Draw on the image to test."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim src = ocvb.color
        If useDepthImage = True Then src = ocvb.RGBDepth
        If ocvb.drawRect.Width > 0 And ocvb.drawRect.Height > 0 Then
            cv.Cv2.Rectangle(dst2, ocvb.drawRect, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
            ocvb.drawRect = New cv.Rect(0, 0, 0, 0)
        End If

        Dim gray = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If gray.CountNonZero() Then
            Dim markerMask As New cv.Mat
            markerMask = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
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
                dst1.SetTo(0)
                For y = 0 To dst1.Rows - 1
                    For x = 0 To dst1.Cols - 1
                        Dim idx = markers.Get(Of Int32)(y, x)
                        If idx = -1 Then
                            dst1.Set(Of cv.Vec3b)(y, x, New cv.Vec3b(255, 255, 255))
                        ElseIf idx <= 0 Or idx > componentCount Then
                            ' already marked zero...
                        Else
                            dst1.Set(Of cv.Vec3b)(y, x, ocvb.rColors((idx - 1) Mod 255))
                        End If
                    Next
                Next
                dst1 = dst1 * 0.5 + src * 0.5
            End If
        Else
            dst1 = ocvb.color
        End If
    End Sub
End Class



Public Class Watershed_DepthAuto
    Inherits ocvbClass
    Dim watershed As Watershed_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        dst2.SetTo(0)
        watershed = New Watershed_Basics(ocvb, caller)
        watershed.useDepthImage = True
        ocvb.desc = "Watershed the depth image using shadow, close, and far points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1 = ocvb.RGBDepth / 64
        dst1 *= 64

        ' erode the blobs at distinct depths to keep them separate
        Dim morphShape = cv.MorphShapes.Cross
        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(5, 5))
        dst2 = dst1.Erode(element, New cv.Point(3, 3), 5)

        watershed.Run(ocvb)
    End Sub
End Class




Public Class Watershed_RGBSimpleAuto
    Inherits ocvbClass
    Dim watershed As Watershed_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        dst2.SetTo(0)

        watershed = New Watershed_Basics(ocvb, caller)

        Dim topLeft = New cv.Rect(0, 0, 100, 100)
        Dim topRight = New cv.Rect(ocvb.color.Width - 100, 0, 100, 100)
        Dim botLeft = New cv.Rect(0, ocvb.color.Height - 100, 100, 100)
        Dim botRight = New cv.Rect(ocvb.color.Width - 100, ocvb.color.Height - 100, 100, 100)

        cv.Cv2.Rectangle(dst2, topLeft, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        cv.Cv2.Rectangle(dst2, topRight, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        cv.Cv2.Rectangle(dst2, botLeft, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        cv.Cv2.Rectangle(dst2, botRight, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        ocvb.desc = "Watershed the depth image using shadow, close, and far points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        watershed.Run(ocvb)
    End Sub
End Class




Public Class Watershed_RGBDepthAuto
    Inherits ocvbClass
    Dim watershed As Watershed_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        dst2.SetTo(0)

        watershed = New Watershed_Basics(ocvb, caller)

        Dim topLeft = New cv.Rect(0, 0, 100, 100)
        Dim topRight = New cv.Rect(ocvb.color.Width - 100, 0, 100, 100)
        Dim botLeft = New cv.Rect(0, ocvb.color.Height - 100, 100, 100)
        Dim botRight = New cv.Rect(ocvb.color.Width - 100, ocvb.color.Height - 100, 100, 100)

        cv.Cv2.Rectangle(dst2, topRight, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        cv.Cv2.Rectangle(dst2, botLeft, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        cv.Cv2.Rectangle(dst2, botRight, cv.Scalar.All(255), -1, cv.LineTypes.AntiAlias)
        ocvb.desc = "Watershed the depth image using shadow, close, and far points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        watershed.Run(ocvb)
    End Sub
End Class


