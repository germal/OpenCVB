Imports cv = OpenCvSharp

' http://opencvexamples.blogspot.com/
Public Class WarpPerspective_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Warped Width", 0, ocvb.color.Cols, ocvb.color.Cols - 50)
        sliders.setupTrackBar2(ocvb, caller, "Warped Height", 0, ocvb.color.Rows, ocvb.color.Rows - 50)
        sliders.setupTrackBar3(ocvb, caller, "Warped Angle", 0, 360, 0)
        ocvb.desc = "Use WarpPerspective to transform input images."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim src(3) As cv.Point2f
        src(0) = New cv.Point2f(0, 0)
        src(1) = New cv.Point2f(0, ocvb.color.Height)
        src(2) = New cv.Point2f(ocvb.color.Width, 0)
        src(3) = New cv.Point2f(ocvb.color.Width, ocvb.color.Height)

        Dim dst(3) As cv.Point2f
        dst(0) = New cv.Point2f(0, 0)
        dst(1) = New cv.Point2f(0, ocvb.color.Height)
        dst(2) = New cv.Point2f(ocvb.color.Width, 0)
        dst(3) = New cv.Point2f(sliders.TrackBar1.Value, sliders.TrackBar2.Value)

        Dim perpectiveTranx = cv.Cv2.GetPerspectiveTransform(src, dst)
        cv.Cv2.WarpPerspective(ocvb.color, ocvb.result1, perpectiveTranx, New cv.Size(ocvb.color.Cols, ocvb.color.Rows), cv.InterpolationFlags.Cubic,
                               cv.BorderTypes.Constant, cv.Scalar.White)

        Dim center = New cv.Point2f(ocvb.color.Cols / 2, ocvb.color.Rows / 2)
        Dim angle = sliders.TrackBar3.Value
        Dim rotationMatrix = cv.Cv2.GetRotationMatrix2D(center, angle, 1.0)
        cv.Cv2.WarpAffine(ocvb.result1, ocvb.result2, rotationMatrix, ocvb.color.Size(), cv.InterpolationFlags.Nearest)
    End Sub
End Class
