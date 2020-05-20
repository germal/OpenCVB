Imports cv = OpenCvSharp
Public Class Transform_Resize
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Resize Percent", 50, 1000, 50)
        ocvb.desc = "Resize an image based on the slider value."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim resizeFactor = sliders.TrackBar1.Value / 100
        Dim w = CInt(resizeFactor * src.Width)
        Dim h = CInt(resizeFactor * src.Height)
        If resizeFactor > 1 Then
            Dim tmp As New cv.Mat
            tmp = src.Resize(New cv.Size(w, h), 0)
            Dim roi = New cv.Rect((w - src.Width) / 2, (h - src.Height) / 2, src.Width, src.Height)
            tmp(roi).CopyTo(dst1)
        Else
            Dim roi = New cv.Rect((src.Width - w) / 2, (src.Height - h) / 2, w, h)
            dst1(roi) = src.Resize(New cv.Size(w, h), 0)
        End If
    End Sub
End Class




Public Class Transform_Rotate
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Angle", 0, 180, 30)
        sliders.setupTrackBar2(ocvb, caller, "Scale Factor", 1, 100, 50)
        ocvb.desc = "Rotate and scale and image based on the slider values."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim imageCenter = New cv.Point2f(src.Width / 2, src.Height / 2)
        Dim rotationMat = cv.Cv2.GetRotationMatrix2D(imageCenter, sliders.TrackBar1.Value, sliders.TrackBar2.Value / 100)
        cv.Cv2.WarpAffine(src, dst1, rotationMat, New cv.Size())
    End Sub
End Class



Public Class Transform_Sort
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        radio.Setup(ocvb, caller, 4)
        radio.check(0).Text = "Ascending"
        radio.check(0).Checked = True
        radio.check(1).Text = "Descending"
        radio.check(2).Text = "EveryColumn"
        radio.check(3).Text = "EveryRow"
        ocvb.desc = "Sort the pixels of a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim sortOption = cv.SortFlags.Ascending
        If radio.check(1).Checked Then sortOption = cv.SortFlags.Descending
        If radio.check(2).Checked Then sortOption = cv.SortFlags.EveryColumn
        If radio.check(3).Checked Then sortOption = cv.SortFlags.EveryRow
        dst1 = src.Sort(sortOption + cv.SortFlags.EveryColumn)
    End Sub
End Class



Public Class Transform_SortReshape
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        radio.Setup(ocvb, caller, 2)
        radio.check(0).Text = "Ascending"
        radio.check(0).Checked = True
        radio.check(1).Text = "Descending"
        ocvb.desc = "Sort the pixels of a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim sortOption = cv.SortFlags.Ascending
        If radio.check(1).Checked Then sortOption = cv.SortFlags.Descending
        src = src.Reshape(1, src.Rows * src.Cols)
        Dim sorted = src.Sort(sortOption + cv.SortFlags.EveryColumn)
        dst1 = sorted.Reshape(1, src.Rows)
    End Sub
End Class

