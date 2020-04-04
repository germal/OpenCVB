Imports cv = OpenCvSharp
Public Class Projections_SideAndDown : Implements IDisposable
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        foreground = New Depth_ManualTrim(ocvb)
        foreground.sliders.TrackBar1.Value = 300  ' fixed distance to keep the images stable.
        foreground.sliders.TrackBar2.Value = 1200 ' fixed distance to keep the images stable.
        ocvb.label1 = "Top View"
        ocvb.label2 = "Side View"
        ocvb.desc = "Project the depth data onto a top view and side view."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        foreground.Run(ocvb)

        ocvb.result1.SetTo(cv.Scalar.White)
        ocvb.result2.SetTo(cv.Scalar.White)

        Dim h = ocvb.result1.Height
        Dim w = ocvb.result1.Width
        Dim desiredMin = foreground.sliders.TrackBar1.Value
        Dim desiredMax = foreground.sliders.TrackBar2.Value
        Dim range = desiredMax - desiredMin

        Parallel.ForEach(Of cv.Rect)(grid.roiList,
         Sub(roi)
             For y = roi.Y To roi.Y + roi.Height - 1
                 For x = roi.X To roi.X + roi.Width - 1
                     Dim m = foreground.Mask.At(Of Byte)(y, x)
                     If m > 0 Then
                         Dim depth = ocvb.depth16.Get(Of UShort)(y, x)
                         If depth > 0 Then
                             Dim dy = Math.Round(h * (depth - desiredMin) / range)
                             If dy < h And dy > 0 Then ocvb.result1.Set(Of cv.Vec3b)(h - dy, x, ocvb.color.At(Of cv.Vec3b)(y, x))
                             Dim dx = Math.Round(w * (depth - desiredMin) / range)
                             If dx < w And dx > 0 Then ocvb.result2.Set(Of cv.Vec3b)(y, dx, ocvb.color.At(Of cv.Vec3b)(y, x))
                         End If
                     End If
                 Next
             Next
         End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        foreground.Dispose()
        grid.Dispose()
    End Sub
End Class
