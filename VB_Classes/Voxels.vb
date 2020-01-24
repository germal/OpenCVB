Imports cv = OpenCvSharp
Public Class Voxels_Basics_MT : Implements IDisposable
    Public trim As Depth_InRangeTrim
    Dim sliders As New OptionsSliders
    Public grid As Thread_Grid
    Public voxels() As Double
    Public Sub New(ocvb As AlgorithmData)
        trim = New Depth_InRangeTrim(ocvb)
        trim.externalUse = True
        trim.sliders.TrackBar2.Value = 5000

        sliders.setupTrackBar1(ocvb, "Histogram Bins", 2, 200, 100)
        If ocvb.parms.ShowOptions Then sliders.Show()

        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 16
        grid.sliders.TrackBar2.Value = 16
        grid.externalUse = True

        ocvb.label2 = "Voxels labeled with their median distance"
        ocvb.desc = "Use multi-threading to get median depth values as voxels."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        Dim minDepth = trim.sliders.TrackBar1.Value
        Dim maxDepth = trim.sliders.TrackBar2.Value

        grid.Run(ocvb)

        Static saveVoxelCount As Int32 = -1
        If saveVoxelCount <> grid.roiList.Count Then
            saveVoxelCount = grid.roiList.Count
            ReDim voxels(saveVoxelCount - 1)
        End If

        Dim bins = sliders.TrackBar1.Value
        Dim gridCount = grid.roiList.Count
        Parallel.For(0, gridCount,
        Sub(i)
            Dim roi = grid.roiList(i)
            If ocvb.depth(roi).CountNonZero() Then
                voxels(i) = computeMedian(ocvb.depth(roi), trim.Mask(roi), bins, minDepth, maxDepth)
            End If
        End Sub)
        ocvb.result1 = ocvb.depthRGB.Clone()
        ocvb.result1.SetTo(cv.Scalar.White, grid.gridMask)

        Dim voxelMat = New cv.Mat(voxels.Length - 1, 1, cv.MatType.CV_64F, voxels)
        voxelMat *= 255 / (maxDepth - minDepth) ' do the normalize manually to use the min and max Depth (more stable image)

        Dim nearColor = cv.Scalar.Yellow
        Dim farColor = cv.Scalar.Blue
        ocvb.result2.SetTo(0)
        Parallel.For(0, gridCount,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim v = voxelMat.At(Of Double)(i)
            If v > 0 And v < 256 Then
                Dim color = New cv.Scalar(((256 - v) * nearColor(0) + v * farColor(0)) >> 8,
                                          ((256 - v) * nearColor(1) + v * farColor(1)) >> 8,
                                          ((256 - v) * nearColor(2) + v * farColor(2)) >> 8)
                ocvb.result2(roi).SetTo(color, trim.Mask(roi))
            End If
        End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
        sliders.Dispose()
        trim.Dispose()
    End Sub
End Class