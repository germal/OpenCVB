Imports cv = OpenCvSharp
Imports System.Threading
Module ML__Exports
    Private Class CompareVec3f : Implements IComparer(Of cv.Vec3f)
        Public Function Compare(ByVal a As cv.Vec3f, ByVal b As cv.Vec3f) As Integer Implements IComparer(Of cv.Vec3f).Compare
            If a(0) = b(0) And a(1) = b(1) And a(2) = b(2) Then Return 0
            Return If(a(0) < b(0), -1, 1)
        End Function
    End Class
    Public Sub detectAndFillShadow(holeMask As cv.Mat, borderMask As cv.Mat, grayDepth As cv.Mat, color As cv.Mat, minLearnCount As Int32)
        Dim learnData As New SortedList(Of cv.Vec3f, Single)(New CompareVec3f)
        Dim rng As New System.Random
        Dim holeCount = cv.Cv2.CountNonZero(holeMask)
        Dim borderCount = cv.Cv2.CountNonZero(borderMask)
        If holeCount > 0 And borderCount > minLearnCount Then
            Dim depthMat As New cv.Mat, color32f As New cv.Mat
            grayDepth.ConvertTo(depthMat, cv.MatType.CV_32F)
            color.ConvertTo(color32f, cv.MatType.CV_32FC3)

            For y = 0 To holeMask.Rows - 1
                For x = 0 To holeMask.Cols - 1
                    If borderMask.At(Of Byte)(y, x) Then
                        Dim vec = color32f.Get(Of cv.Vec3f)(y, x)
                        If learnData.ContainsKey(vec) = False Then
                            learnData.Add(vec, depthMat.Get(Of Single)(y, x)) ' keep out duplicates.
                        End If
                    End If
                Next
            Next

            Dim learnInput As New cv.Mat(learnData.Count, 3, cv.MatType.CV_32F)
            Dim depthResponse As New cv.Mat(learnData.Count, 1, cv.MatType.CV_32F)

            Dim indexTrain = 0
            For i = 0 To learnData.Count - 1
                learnInput.Set(Of cv.Vec3f)(indexTrain, 0, learnData.ElementAt(i).Key)
                depthResponse.Set(Of Single)(i, 0, learnData.ElementAt(i).Value)
            Next

            ' now learn what depths are associated with which colors.
            Using rtree = cv.ML.RTrees.Create()
                rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)

                ' now predict what the depth is based just on the color (and proximity to the region)
                Using predictMat As New cv.Mat(1, 3, cv.MatType.CV_32F)
                    For y = 0 To holeMask.Rows - 1
                        For x = 0 To holeMask.Cols - 1
                            If holeMask.At(Of Byte)(y, x) Then
                                predictMat.Set(Of cv.Vec3f)(0, 0, color32f.Get(Of cv.Vec3f)(y, x))
                                depthMat.Set(Of Single)(y, x, rtree.Predict(predictMat))
                            End If
                        Next
                    Next
                End Using
                depthMat.ConvertTo(grayDepth, cv.MatType.CV_8UC1)
            End Using
        End If
    End Sub
End Module


Public Class ML_FillDepthRGB_MT : Implements IDisposable
    Dim shadow As Depth_Shadow
    Dim grid As Thread_Grid

    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 160
        grid.sliders.TrackBar2.Value = 120
        grid.externalUse = True ' we don't need any results.
        shadow = New Depth_Shadow(ocvb)
        ocvb.label2 = "ML filled shadow"
        ocvb.desc = "Same as ML_FillDepth above but display grayscale depth to confirm correctness of model."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        grid.Run(ocvb)
        ocvb.depthRGB.CopyTo(ocvb.result1)
        ocvb.result1.SetTo(cv.Scalar.White, grid.gridMask)
        Dim grayDepth = ocvb.depthRGB.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim minLearnCount = 5
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            detectAndFillShadow(shadow.holeMask(roi), shadow.borderMask(roi), grayDepth(roi), ocvb.color(roi), minLearnCount)
        End Sub)
        ocvb.result2 = grayDepth.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        shadow.Dispose()
        grid.Dispose()
    End Sub
End Class


Public Class ML_FillDepthRGB : Implements IDisposable
    Dim shadow As Depth_Shadow
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "ML Min Learn Count", 2, 100, 5)
        If ocvb.parms.ShowOptions Then sliders.show()
        shadow = New Depth_Shadow(ocvb)
        ocvb.label2 = "ML filled shadow"
        ocvb.desc = "Same as ML_FillDepth above but display grayscale depth to confirm correctness of model."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        Dim minLearnCount = sliders.TrackBar1.Value
        ocvb.depthRGB.CopyTo(ocvb.result1)
        Dim grayDepth = ocvb.depthRGB.CvtColor(cv.ColorConversionCodes.bgr2gray)
        detectAndFillShadow(shadow.holeMask, shadow.borderMask, grayDepth, ocvb.color, minLearnCount)
        ocvb.result2 = grayDepth.CvtColor(cv.ColorConversionCodes.gray2bgr)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        shadow.Dispose()
        sliders.Dispose()
    End Sub
End Class


Public Class ML_DepthFromColor_MT : Implements IDisposable
    Dim disp16 As Depth_Colorizer_CPP
    Dim grid As Thread_Grid
    Dim dilate As DilateErode_Basics
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        disp16 = New Depth_Colorizer_CPP(ocvb)
        disp16.externalUse = True

        dilate = New DilateErode_Basics(ocvb)
        dilate.externalUse = True
        dilate.sliders.TrackBar2.Value = 2

        sliders.setupTrackBar1(ocvb, "Prediction Max Depth", 500, 5000, 1000)
        If ocvb.parms.ShowOptions Then sliders.show()

        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 16
        grid.sliders.TrackBar2.Value = 16
        grid.externalUse = True

        ocvb.label1 = "Predicted Depth"
        ocvb.label2 = "Mask of color and depth input"
        ocvb.desc = "Use RGB, X, and Y to predict depth across the entire image, maxDepth = slider value."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        Dim depth32f As New cv.Mat
        ocvb.depth.ConvertTo(depth32f, cv.MatType.CV_32F)

        Dim mask = depth32f.Threshold(sliders.TrackBar1.Value, sliders.TrackBar1.Value, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        depth32f.SetTo(sliders.TrackBar1.Value, mask)

        Dim predictedDepth As New cv.Mat(depth32f.Size(), cv.MatType.CV_32F, 0)

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        dilate.src = mask
        dilate.Run(ocvb)
        mask = ocvb.result1
        ocvb.result2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat
        ocvb.color.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim predictedRegions As Int32
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim maskCount = roi.Width * roi.Height - mask(roi).CountNonZero()
            If maskCount > 10 Then
                Interlocked.Add(predictedRegions, 1)
                Dim learnInput = color32f(roi).Clone()
                learnInput = learnInput.Reshape(1, roi.Width * roi.Height)
                Dim depthResponse = depth32f(roi).Clone()
                depthResponse = depthResponse.Reshape(1, roi.Width * roi.Height)

                Dim rtree = cv.ML.RTrees.Create()
                rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)
                rtree.Predict(learnInput, depthResponse)
                predictedDepth(roi) = depthResponse.Reshape(1, roi.Height)
            End If
        End Sub)
        ocvb.label2 = "Input region count = " + CStr(predictedRegions) + " of " + CStr(grid.roiList.Count)
        Dim depth16u As New cv.Mat
        predictedDepth.ConvertTo(depth16u, cv.MatType.CV_16U)
        disp16.src = depth16u
        disp16.Run(ocvb)
        ocvb.result1 = disp16.dst
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        dilate.Dispose()
        grid.Dispose()
        disp16.Dispose()
    End Sub
End Class



Public Class ML_DepthFromColor : Implements IDisposable
    Dim disp16 As Depth_Colorizer_CPP
    Dim mats As Mat_4to1
    Dim shadow As Depth_Shadow
    Dim resized As Resize_Percentage
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        disp16 = New Depth_Colorizer_CPP(ocvb)
        disp16.externalUse = True

        mats = New Mat_4to1(ocvb)
        mats.externalUse = True

        shadow = New Depth_Shadow(ocvb)

        sliders.setupTrackBar1(ocvb, "Prediction Max Depth", 1000, 5000, 1500)
        If ocvb.parms.ShowOptions Then sliders.show()

        resized = New Resize_Percentage(ocvb)
        resized.externalUse = True
        resized.resizePercent = 2 ' 2% of the image.

        ocvb.desc = "Use RGB to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        mats.mat(0) = shadow.holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim depth32f As New cv.Mat
        Dim color32f As New cv.Mat

        resized.src = ocvb.color.Clone()
        resized.Run(ocvb)

        Dim colorROI As New cv.Rect(0, 0, resized.resizeOptions.newSize.Width, resized.resizeOptions.newSize.Height)
        resized.dst.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim shadowSmall = mats.mat(0).Resize(color32f.Size()).Clone()
        color32f.SetTo(cv.Scalar.Black, shadowSmall) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Dim depth16 = ocvb.depth.Resize(color32f.Size())
        depth16.ConvertTo(depth32f, cv.MatType.CV_32F)

        Dim mask = depth32f.Threshold(sliders.TrackBar1.Value, sliders.TrackBar1.Value, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        mats.mat(2) = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        cv.Cv2.BitwiseNot(mask, mask)
        depth32f.SetTo(sliders.TrackBar1.Value, mask)
        depth32f.ConvertTo(depth16, cv.MatType.CV_16U)

        disp16.src = depth16
        disp16.Run(ocvb)
        mats.mat(3) = disp16.dst.Clone()

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        Dim maskCount = mask.CountNonZero()
        ocvb.result1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim learnInput = color32f.Reshape(1, color32f.Total)
        Dim depthResponse = depth32f.Reshape(1, depth32f.Total)

        ' now learn what depths are associated with which colors.
        Dim rtree = cv.ML.RTrees.Create()
        rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)

        ocvb.color.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim input = color32f.Reshape(1, color32f.Total) ' test the entire original image.
        Dim output As New cv.Mat
        rtree.Predict(input, output)
        Dim predictedDepth = output.Reshape(1, ocvb.depth.Height)

        predictedDepth.ConvertTo(depth16, cv.MatType.CV_16U)

        disp16.src = depth16
        disp16.Run(ocvb)
        ocvb.result1 = disp16.dst.Clone()

        mats.Run(ocvb)
        ocvb.label1 = "Predicted Depth"
        ocvb.label2 = "shadow, empty, Depth Mask < " + CStr(sliders.TrackBar1.Value) + ", Learn Input"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        shadow.Dispose()
        mats.Dispose()
        resized.Dispose()
        disp16.Dispose()
    End Sub
End Class



Public Class ML_DepthFromXYColor : Implements IDisposable
    Dim mats As Mat_4to1
    Dim shadow As Depth_Shadow
    Dim resized As Resize_Percentage
    Dim sliders As New OptionsSliders
    Dim disp16 As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData)
        disp16 = New Depth_Colorizer_CPP(ocvb)
        disp16.externalUse = True

        mats = New Mat_4to1(ocvb)
        mats.externalUse = True

        shadow = New Depth_Shadow(ocvb)

        sliders.setupTrackBar1(ocvb, "Prediction Max Depth", 1000, 5000, 1500)
        If ocvb.parms.ShowOptions Then sliders.show()

        resized = New Resize_Percentage(ocvb)
        resized.externalUse = True
        resized.resizePercent = 2

        ocvb.label1 = "Predicted Depth"
        ocvb.desc = "Use RGB to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        mats.mat(0) = shadow.holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim depth32f As New cv.Mat
        Dim color32f As New cv.Mat

        resized.src = ocvb.color.Clone()
        resized.Run(ocvb)

        Dim colorROI As New cv.Rect(0, 0, resized.resizeOptions.newSize.Width, resized.resizeOptions.newSize.Height)
        resized.dst.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim shadowSmall = shadow.holeMask.Resize(color32f.Size()).Clone()
        color32f.SetTo(cv.Scalar.Black, shadowSmall) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Dim depth16 = ocvb.depth.Resize(color32f.Size())
        depth16.ConvertTo(depth32f, cv.MatType.CV_32F)

        Dim mask = depth32f.Threshold(sliders.TrackBar1.Value, sliders.TrackBar1.Value, cv.ThresholdTypes.BinaryInv)
        mask.SetTo(0, shadowSmall) ' remove the unknown depth...
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        mats.mat(2) = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        cv.Cv2.BitwiseNot(mask, mask)
        depth32f.SetTo(sliders.TrackBar1.Value, mask)
        depth32f.ConvertTo(depth16, cv.MatType.CV_16U)

        disp16.src = depth16
        disp16.Run(ocvb)
        mats.mat(3) = disp16.dst.Clone()

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        Dim maskCount = mask.CountNonZero()
        ocvb.result1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim c = color32f.Reshape(1, color32f.Total)
        Dim depthResponse = depth32f.Reshape(1, depth32f.Total)

        Dim learnInput As New cv.Mat(c.Rows, 6, cv.MatType.CV_32F, 0)
        For y = 0 To c.Rows - 1
            For x = 0 To c.Cols - 1
                Dim v6 = New cv.Vec6f(c.At(Of Single)(y, x), c.At(Of Single)(y, x + 1), c.At(Of Single)(y, x + 2), x, y, 0)
                learnInput.Set(Of cv.Vec6f)(y, x, v6)
            Next
        Next

        ' Now learn what depths are associated with which colors.
        Dim rtree = cv.ML.RTrees.Create()
        rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)

        ocvb.color.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim allC = color32f.Reshape(1, color32f.Total) ' test the entire original image.
        Dim input As New cv.Mat(allC.Rows, 6, cv.MatType.CV_32F, 0)
        For y = 0 To allC.Rows - 1
            For x = 0 To allC.Cols - 1
                Dim v6 = New cv.Vec6f(allC.At(Of Single)(y, x), allC.At(Of Single)(y, x + 1), allC.At(Of Single)(y, x + 2), x, y, 0)
                input.Set(Of cv.Vec6f)(y, x, v6)
            Next
        Next

        Dim output As New cv.Mat
        rtree.Predict(input, output)
        Dim predictedDepth = output.Reshape(1, ocvb.depth.Height)

        predictedDepth.ConvertTo(depth16, cv.MatType.CV_16U)

        disp16.src = depth16
        disp16.Run(ocvb)
        ocvb.result1 = disp16.dst.Clone()

        mats.Run(ocvb)
        ocvb.label2 = "shadow, empty, Depth Mask < " + CStr(sliders.TrackBar1.Value) + ", Learn Input"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        shadow.Dispose()
        mats.Dispose()
        resized.Dispose()
        disp16.Dispose()
    End Sub
End Class




Public Class ML_EdgeDepth : Implements IDisposable
    Dim disp16 As Depth_Colorizer_CPP
    Dim grid As Thread_Grid
    Dim dilate As DilateErode_Basics
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        disp16 = New Depth_Colorizer_CPP(ocvb)
        disp16.externalUse = True

        dilate = New DilateErode_Basics(ocvb)
        dilate.externalUse = True
        dilate.sliders.TrackBar2.Value = 5

        sliders.setupTrackBar1(ocvb, "Prediction Max Depth", 500, 5000, 1000)
        If ocvb.parms.ShowOptions Then sliders.show()

        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 16
        grid.sliders.TrackBar2.Value = 16
        grid.externalUse = True

        ocvb.label1 = "Depth Shadow (inverse of color and depth)"
        ocvb.label2 = "Predicted Depth"
        ocvb.desc = "Use RGB to predict depth near edges."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        Dim depth32f As New cv.Mat
        ocvb.depth.ConvertTo(depth32f, cv.MatType.CV_32F)

        Dim mask = depth32f.Threshold(sliders.TrackBar1.Value, sliders.TrackBar1.Value, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        depth32f.SetTo(sliders.TrackBar1.Value, mask)

        Dim predictedDepth As New cv.Mat(depth32f.Size(), cv.MatType.CV_32F, 0)

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        dilate.src = mask
        dilate.Run(ocvb)
        ocvb.result1 = dilate.src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat
        ocvb.color.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim predictedRegions As Int32
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim maskCount = mask(roi).CountNonZero()
            If maskCount = 0 Then ' if no bad pixels, then learn and predict
                maskCount = mask(roi).Total() - maskCount
                Interlocked.Add(predictedRegions, 1)
                Dim learnInput = color32f(roi).Clone()
                learnInput = learnInput.Reshape(1, maskCount)
                Dim depthResponse = depth32f(roi).Clone()
                depthResponse = depthResponse.Reshape(1, maskCount)

                Dim rtree = cv.ML.RTrees.Create()
                rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)
                rtree.Predict(learnInput, depthResponse)
                predictedDepth(roi) = depthResponse.Reshape(1, roi.Height)
            End If
        End Sub)
        ocvb.label2 = "Input region count = " + CStr(predictedRegions) + " of " + CStr(grid.roiList.Count)
        Dim depth16u As New cv.Mat
        predictedDepth.ConvertTo(depth16u, cv.MatType.CV_16U)
        disp16.src = depth16u
        disp16.Run(ocvb)
        ocvb.result2 = disp16.dst
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        dilate.Dispose()
        grid.Dispose()
        disp16.Dispose()
    End Sub
End Class