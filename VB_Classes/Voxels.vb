Imports cv = OpenCvSharp
Public Class Voxels_Basics : Implements IDisposable
    Public grid As Thread_Grid
    Public voxels() As Math_Median_CDF
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32
        grid.externalUse = True

        ocvb.desc = "Use multi-threading to get center-weighted depth values as voxels."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        Static saveVoxelCount As Int32 = -1
        If saveVoxelCount <> grid.roiList.Count Then
            saveVoxelCount = grid.roiList.Count
            ReDim voxels(saveVoxelCount - 1)
            ocvb.parms.ShowOptions = False ' too many sliders otherwise.
            For i = 0 To saveVoxelCount - 1
                voxels(i) = New Math_Median_CDF(ocvb)
                voxels(i).rangeMax = 5000 ' 5 meters
                voxels(i).rangeMin = 50
                voxels(i).externalUse = True
            Next
        End If
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            voxels(i).src = ocvb.depth(roi)
            voxels(i).Run(ocvb)
        End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
    End Sub
End Class