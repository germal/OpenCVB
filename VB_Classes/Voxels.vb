Imports cv = OpenCvSharp
Public Class Voxels_Basics_MT
    Inherits ocvbClass
    Public trim As Depth_InRange
    Public grid As Thread_Grid
    Public voxels() As Double
    Public voxelMat As cv.Mat
    Public minDepth As Double
    Public maxDepth As Double
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Display intermediate results"
        check.Box(0).Checked = True

        trim = New Depth_InRange(ocvb)
        trim.sliders.trackbar(1).Value = 5000

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Histogram Bins", 2, 200, 100)

        grid = New Thread_Grid(ocvb)
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 16
        gridHeightSlider.Value = 16

        label2 = "Voxels labeled with their median distance"
        setDescription(ocvb, "Use multi-threading to get median depth values as voxels.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.src = getDepth32f(ocvb)
        trim.Run(ocvb)
        minDepth = trim.sliders.trackbar(0).Value
        maxDepth = trim.sliders.trackbar(1).Value

        grid.Run(ocvb)

        Static saveVoxelCount As Int32 = -1
        If saveVoxelCount <> grid.roiList.Count Then
            saveVoxelCount = grid.roiList.Count
            ReDim voxels(saveVoxelCount - 1)
        End If

        Dim bins = sliders.trackbar(0).Value
        Dim depth32f = getDepth32f(ocvb)
        ' putting the calcHist into a parallel.for (inside computeMedian below) seems to cause a memory leak.  Avoiding it here...
        'Parallel.For(0, grid.roiList.Count,
        'Sub(i)
        For i = 0 To grid.roiList.Count - 1
            Dim roi = grid.roiList(i)
            Dim count = trim.Mask(roi).CountNonZero()
            If count > 0 Then
                voxels(i) = computeMedian(depth32f(roi), trim.Mask(roi), count, bins, minDepth, maxDepth)
            Else
                voxels(i) = 0
            End If
        Next
        'End Sub)
        voxelMat = New cv.Mat(voxels.Length, 1, cv.MatType.CV_64F, voxels)
        If check.Box(0).Checked Then ' do they want to display results?
            dst1 = ocvb.RGBDepth.Clone()
            dst1.SetTo(cv.Scalar.White, grid.gridMask)
            Dim nearColor = cv.Scalar.Yellow
            Dim farColor = cv.Scalar.Blue
            dst2.SetTo(0)
            Parallel.For(0, grid.roiList.Count,
                Sub(i)
                    Dim roi = grid.roiList(i)
                    Dim v = voxels(i)
                    If v > 0 And v < 256 Then
                        Dim color = New cv.Scalar(((256 - v) * nearColor(0) + v * farColor(0)) >> 8,
                                                  ((256 - v) * nearColor(1) + v * farColor(1)) >> 8,
                                                  ((256 - v) * nearColor(2) + v * farColor(2)) >> 8)
                        dst2(roi).SetTo(color, trim.Mask(roi))
                    End If
                End Sub)
        End If
        voxelMat *= 255 / (maxDepth - minDepth) ' do the normalize manually to use the min and max Depth (more stable image)
    End Sub
End Class
