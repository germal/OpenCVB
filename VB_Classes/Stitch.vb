Imports cv = OpenCvSharp
' https://github.com/shimat/opencvsharp/blob/master/test/OpenCvSharp.Tests/stitching/StitchingTest.cs
Public Class Stitch_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public src As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Number of random images", 10, 50, 10)
        sliders.setupTrackBar2(ocvb, "Rectangle width", ocvb.color.Width / 4, ocvb.color.Width - 1, ocvb.color.Width / 2)
        sliders.setupTrackBar3(ocvb, "Rectangle height", ocvb.color.Height / 4, ocvb.color.Height - 1, ocvb.color.Height / 2)
        sliders.Show()
        ocvb.desc = "Stitch together random parts of a color image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mats As New List(Of cv.Mat)
        Dim autoRand As New Random()
        Dim width = sliders.TrackBar2.Value
        Dim height = sliders.TrackBar3.Value
        If externalUse = False Then src = ocvb.color.Clone()
        For i = 0 To sliders.TrackBar1.Value - 1
            Dim x1 = CInt(autoRand.NextDouble() * (src.Width - width))
            Dim x2 = CInt(autoRand.NextDouble() * (src.Height - height))
            Dim rect = New cv.Rect(x1, x2, width, height)
            ocvb.result1.Rectangle(rect, cv.Scalar.Red, 2)
            mats.Add(src(rect).Clone())
        Next

        Dim stitcher = cv.Stitcher.Create(cv.Stitcher.Mode.Scans)
        Dim pano As New cv.Mat
        Dim status = stitcher.Stitch(mats, pano) ' stitcher may fail with an external exception if you make width and height too small.
        ocvb.result2.SetTo(0)
        If status = cv.Stitcher.Status.OK Then
            pano.CopyTo(ocvb.result2(New cv.Rect(0, 0, pano.Width, pano.Height)))
        Else
            If status = cv.Stitcher.Status.ErrorNeedMoreImgs Then
                ocvb.result2.PutText("Need more images", New cv.Point(10, 60), cv.HersheyFonts.HersheySimplex, 0.5, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Stitch_Gray : Implements IDisposable
    Dim stitch As Stitch_Basics
    Public Sub New(ocvb As AlgorithmData)
        stitch = New Stitch_Basics(ocvb)
        stitch.externalUse = True
        ocvb.desc = "Stitch together random parts of a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        stitch.src = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        stitch.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        stitch.Dispose()
    End Sub
End Class
