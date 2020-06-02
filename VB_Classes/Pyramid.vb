Imports cv = OpenCvSharp
' https://docs.opencv.org/3.3.1/d6/d73/Pyramids_8cpp-example.html
Public Class Pyramid_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Zoom in and out", -1, 1, 0)
        ocvb.desc = "Use pyrup and pyrdown to zoom in and out of an image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim zoom = sliders.TrackBar1.Value
        If zoom <> 0 Then
            If zoom < 0 Then
                Dim tmp = src.PyrDown(New cv.Size(src.Cols / 2, src.Rows / 2))
                Dim roi = New cv.Rect((src.Cols - tmp.Cols) / 2, (src.Rows - tmp.Rows) / 2, tmp.Width, tmp.Height)
                dst1(roi) = tmp
            Else
                Dim tmp = src.PyrUp(New cv.Size(src.Cols * 2, src.Rows * 2))
                Dim roi = New cv.Rect((tmp.Cols - src.Cols) / 2, (tmp.Rows - src.Rows) / 2, src.Width, src.Height)
                dst1 = tmp(roi)
            End If
        Else
            src.CopyTo(dst1)
        End If
    End Sub
End Class

