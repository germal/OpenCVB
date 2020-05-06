Imports cv = OpenCvSharp
Public Class Transform_Resize
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Resize Percent", 50, 1000, 50)
        ocvb.desc = "Resize an image based on the slider value."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim resizeFactor = sliders.TrackBar1.Value / 100
        Dim w = CInt(resizeFactor * ocvb.color.Width)
        Dim h = CInt(resizeFactor * ocvb.color.Height)
        If resizeFactor > 1 Then
            Dim tmp As New cv.Mat
            tmp = ocvb.color.Resize(New cv.Size(w, h), 0)
            Dim roi = New cv.Rect((w - ocvb.color.Width) / 2, (h - ocvb.color.Height) / 2, ocvb.color.Width, ocvb.color.Height)
            tmp(roi).CopyTo(ocvb.result1)
        Else
            Dim roi = New cv.Rect((ocvb.color.Width - w) / 2, (ocvb.color.Height - h) / 2, w, h)
            ocvb.result1.SetTo(0)
            ocvb.result1(roi) = ocvb.color.Resize(New cv.Size(w, h), 0)
        End If
    End Sub
End Class




Public Class Transform_Rotate
    Inherits VB_Class
    Public src As cv.Mat
    Public dst As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Angle", 0, 180, 30)
        sliders.setupTrackBar2(ocvb, callerName, "Scale Factor", 1, 100, 50)
        ocvb.desc = "Rotate and scale and image based on the slider values."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            src = ocvb.color
            dst = ocvb.result2
        End If
        Dim imageCenter = New cv.Point2f(src.Width / 2, src.Height / 2)
        Dim rotationMat = cv.Cv2.GetRotationMatrix2D(imageCenter, sliders.TrackBar1.Value, sliders.TrackBar2.Value / 100)
        cv.Cv2.WarpAffine(src, dst, rotationMat, New cv.Size())
    End Sub
End Class



Public Class Transform_Sort
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        radio.Setup(ocvb, callerName, 4)
        radio.check(0).Text = "Ascending"
        radio.check(0).Checked = True
        radio.check(1).Text = "Descending"
        radio.check(2).Text = "EveryColumn"
        radio.check(3).Text = "EveryRow"
        ocvb.desc = "Sort the pixels of a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim sortOption = cv.SortFlags.Ascending
        If radio.check(1).Checked Then sortOption = cv.SortFlags.Descending
        If radio.check(2).Checked Then sortOption = cv.SortFlags.EveryColumn
        If radio.check(3).Checked Then sortOption = cv.SortFlags.EveryRow
        Dim sorted = gray.Sort(sortOption + cv.SortFlags.EveryColumn)
        ocvb.result1 = sorted.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class



Public Class Transform_SortReshape
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        radio.Setup(ocvb, callerName, 2)
        radio.check(0).Text = "Ascending"
        radio.check(0).Checked = True
        radio.check(1).Text = "Descending"
        ocvb.desc = "Sort the pixels of a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.bgr2gray)
        Dim sortOption = cv.SortFlags.Ascending
        If radio.check(1).Checked Then sortOption = cv.SortFlags.Descending
        gray = gray.Reshape(1, gray.Rows * gray.Cols)
        Dim sorted = gray.Sort(sortOption + cv.SortFlags.EveryColumn)
        sorted = sorted.Reshape(1, ocvb.color.Rows)
        ocvb.result1 = sorted.CvtColor(cv.ColorConversionCodes.gray2bgr)
    End Sub
End Class
