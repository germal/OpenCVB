Imports cv = OpenCvSharp
Public Class Watershed_Basics
    Inherits VBparent
    Dim weighted As AddWeighted_Basics
    Dim rects As New List(Of cv.Rect)
    Dim palette As Palette_Basics
    Public UseCorners As Boolean
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        palette = New Palette_Basics(ocvb)
        weighted = New AddWeighted_Basics(ocvb)
        label1 = "Draw rectangle to add another marker"
        label2 = "Mask for watershed (selected regions)."
        ocvb.desc = "Watershed API experiment.  Draw on the image to test."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.drawRect.Width > 0 And ocvb.drawRect.Height > 0 Then rects.Add(ocvb.drawRect)

        If (standalone Or UseCorners) And ocvb.frameCount = 0 Then
            For i = 0 To 4 - 1
                Dim r As New cv.Rect(0, 0, src.Width / 10, src.Height / 10)
                Select Case i
                    Case 1
                        r.X = src.Width - src.Width / 10
                    Case 2
                        r.X = src.Width - src.Width / 10
                        r.Y = src.Height - src.Height / 10
                    Case 3
                        r.Y = src.Height - src.Height / 10
                End Select
                rects.Add(r)
            Next
        End If

        If rects.Count > 0 Then
            Dim markers = New cv.Mat(src.Size(), cv.MatType.CV_32S, 0)
            For i = 0 To rects.Count - 1
                markers.Rectangle(rects.ElementAt(i), cv.Scalar.All(i + 1), -1)
            Next

            cv.Cv2.Watershed(src, markers)

            markers *= Math.Truncate(255 / rects.Count)
            markers.ConvertTo(palette.src, cv.MatType.CV_8U)
            palette.Run(ocvb)
            dst2 = palette.dst1

            weighted.src1 = src
            weighted.src2 = palette.dst1
            weighted.Run(ocvb)
            dst1 = weighted.dst1
        Else
            dst1 = src
        End If
        ocvb.drawRect = New cv.Rect
        label1 = "There were " + CStr(rects.Count) + " regions defined as input"
    End Sub
End Class







Public Class Watershed_DepthReduction
    Inherits VBparent
    Dim watershed As Watershed_Basics
    Dim reduction As Reduction_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        reduction = New Reduction_Basics(ocvb)
        watershed = New Watershed_Basics(ocvb)
        watershed.UseCorners = True
        label2 = "Reduction input to WaterShed"
        ocvb.desc = "Watershed the depth image using shadow, close, and far points."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = ocvb.RGBDepth
        reduction.Run(ocvb)
        dst2 = reduction.dst1

        watershed.src = reduction.dst1
        watershed.Run(ocvb)
        dst1 = watershed.dst1
        label1 = watershed.label1
    End Sub
End Class








Public Class Watershed_DepthAuto
    Inherits VBparent
    Dim watershed As Watershed_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        watershed = New Watershed_Basics(ocvb)
        watershed.UseCorners = True
        ocvb.desc = "Watershed the four corners of the depth image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        watershed.src = ocvb.RGBDepth
        watershed.Run(ocvb)
        dst1 = watershed.dst1
        label1 = watershed.label1
    End Sub
End Class
