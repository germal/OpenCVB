Imports cv = OpenCvSharp
Public Class FloodFill_Basics_MT : Implements IDisposable
    Public sliders As New OptionsSliders
    Dim grid As Thread_Grid
    Public srcGray As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        sliders.setupTrackBar1(ocvb, "FloodFill Minimum Size", 1, 500, 50)
        sliders.setupTrackBar2(ocvb, "FloodFill LoDiff", 1, 255, 5)
        sliders.setupTrackBar3(ocvb, "FloodFill HiDiff", 1, 255, 5)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Use floodfill to build image segments with a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim minFloodSize = sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(sliders.TrackBar3.Value)

        If externalUse = False Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.result2 = srcGray.Clone()
        grid.Run(ocvb)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            For y = roi.Y To roi.Y + roi.Height - 1
                For x = roi.X To roi.X + roi.Width - 1
                    Dim nextByte = srcGray.At(Of Byte)(y, x)
                    If nextByte <> 255 And nextByte > 0 Then
                        Dim count = cv.Cv2.FloodFill(srcGray, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange)
                        If count > minFloodSize Then
                            count = cv.Cv2.FloodFill(ocvb.result2, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange)
                        End If
                    End If
                Next
            Next
        End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        grid.Dispose()
    End Sub
End Class




Public Class FloodFill_Color_MT : Implements IDisposable
    Dim flood As FloodFill_Basics_MT
    Dim grid As Thread_Grid
    Public src As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        flood = New FloodFill_Basics_MT(ocvb)

        ocvb.desc = "Use floodfill to build image segments in an RGB image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim minFloodSize = flood.sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(flood.sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(flood.sliders.TrackBar3.Value)

        If externalUse = False Then src = ocvb.color.Clone()
        ocvb.result2 = src.Clone()
        grid.Run(ocvb)
        Dim vec255 = New cv.Vec3b(255, 255, 255)
        Dim vec0 = New cv.Vec3b(0, 0, 0)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            For y = roi.Y To roi.Y + roi.Height - 1
                For x = roi.X To roi.X + roi.Width - 1
                    Dim vec = src.At(Of cv.Vec3b)(y, x)
                    If vec <> vec255 And vec <> vec0 Then
                        Dim count = cv.Cv2.FloodFill(src, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange + cv.FloodFillFlags.Link4)
                        If count > minFloodSize Then
                            count = cv.Cv2.FloodFill(ocvb.result2, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange + cv.FloodFillFlags.Link4)
                        End If
                    End If
                Next
            Next
        End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
        flood.Dispose()
    End Sub
End Class




Public Class FloodFill_DCT : Implements IDisposable
    Dim flood As FloodFill_Color_MT
    Dim dct As DCT_FeatureLess_MT
    Public Sub New(ocvb As AlgorithmData)
        flood = New FloodFill_Color_MT(ocvb)
        flood.externalUse = True

        dct = New DCT_FeatureLess_MT(ocvb)
        ocvb.desc = "Find surfaces that lack any texture with DCT (highest frequency removed) and use floodfill to isolate those surfaces."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dct.Run(ocvb)
        flood.src = ocvb.result2.Clone()
        Dim mask As New cv.Mat
        cv.Cv2.BitwiseNot(ocvb.result1, mask)
        flood.src.SetTo(0, mask)
        flood.Run(ocvb)
        ocvb.result2.SetTo(0, mask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        dct.Dispose()
        flood.Dispose()
    End Sub
End Class
