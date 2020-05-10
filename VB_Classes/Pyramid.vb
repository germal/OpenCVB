Imports cv = OpenCvSharp
' https://docs.opencv.org/3.3.1/d6/d73/Pyramids_8cpp-example.html
Public Class Pyramid_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Zoom in and out", -1, 1, 0)
        ocvb.desc = "Use pyrup and pyrdown to zoom in and out of an image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim zoom = sliders.TrackBar1.Value
        If zoom <> 0 Then
            ocvb.result1.SetTo(0)
            If zoom < 0 Then
                Dim tmp = ocvb.color.PyrDown(New cv.Size(ocvb.color.Cols / 2, ocvb.color.Rows / 2))
                Dim roi = New cv.Rect((ocvb.color.Cols - tmp.Cols) / 2, (ocvb.color.Rows - tmp.Rows) / 2, tmp.Width, tmp.Height)
                ocvb.result1(roi) = tmp
            Else
                Dim tmp = ocvb.color.PyrUp(New cv.Size(ocvb.color.Cols * 2, ocvb.color.Rows * 2))
                Dim roi = New cv.Rect((tmp.Cols - ocvb.color.Cols) / 2, (tmp.Rows - ocvb.color.Rows) / 2, ocvb.color.Width, ocvb.color.Height)
                ocvb.result1 = tmp(roi)
            End If
        Else
            ocvb.color.CopyTo(ocvb.result1)
        End If
    End Sub
End Class

