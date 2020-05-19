Imports cv = OpenCvSharp
Imports System.Threading
Module ML__Exports
    Private Class CompareVec3f : Implements IComparer(Of cv.Vec3f)
        Public Function Compare(ByVal a As cv.Vec3f, ByVal b As cv.Vec3f) As Integer Implements IComparer(Of cv.Vec3f).Compare
            If a(0) = b(0) And a(1) = b(1) And a(2) = b(2) Then Return 0
            Return If(a(0) < b(0), -1, 1)
        End Function
    End Class
    Public Function detectAndFillShadow(holeMask As cv.Mat, borderMask As cv.Mat, depth32f As cv.Mat, color As cv.Mat, minLearnCount As Int32) As cv.Mat
        Dim learnData As New SortedList(Of cv.Vec3f, Single)(New CompareVec3f)
        Dim rng As New System.Random
        Dim holeCount = cv.Cv2.CountNonZero(holeMask)
        Dim borderCount = cv.Cv2.CountNonZero(borderMask)
        If holeCount > 0 And borderCount > minLearnCount Then
            Dim color32f As New cv.Mat
            color.ConvertTo(color32f, cv.MatType.CV_32FC3)

            Dim learnInputList As New List(Of cv.Vec3f)
            Dim responseInputList As New List(Of Single)

            For y = 0 To holeMask.Rows - 1
                For x = 0 To holeMask.Cols - 1
                    If borderMask.Get(Of Byte)(y, x) Then
                        Dim vec = color32f.Get(Of cv.Vec3f)(y, x)
                        If learnData.ContainsKey(vec) = False Then
                            learnData.Add(vec, depth32f.Get(Of Single)(y, x)) ' keep out duplicates.
                            learnInputList.Add(vec)
                            responseInputList.Add(depth32f.Get(Of Single)(y, x))
                        End If
                    End If
                Next
            Next

            Dim learnInput As New cv.Mat(learnData.Count, 3, cv.MatType.CV_32F, learnInputList.ToArray())
            Dim depthResponse As New cv.Mat(learnData.Count, 1, cv.MatType.CV_32F, responseInputList.ToArray())

            ' now learn what depths are associated with which colors.
            Dim rtree = cv.ML.RTrees.Create()
            rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)

            ' now predict what the depth is based just on the color (and proximity to the region)
            Using predictMat As New cv.Mat(1, 3, cv.MatType.CV_32F)
                For y = 0 To holeMask.Rows - 1
                    For x = 0 To holeMask.Cols - 1
                        If holeMask.Get(Of Byte)(y, x) Then
                            predictMat.Set(Of cv.Vec3f)(0, 0, color32f.Get(Of cv.Vec3f)(y, x))
                            depth32f.Set(Of Single)(y, x, rtree.Predict(predictMat))
                        End If
                    Next
                Next
            End Using
        End If
        Return depth32f
    End Function
End Module


Public Class ML_FillRGBDepth_MT
    Inherits ocvbClass
    Dim shadow As Depth_Holes
    Dim grid As Thread_Grid
    Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        colorizer = New Depth_Colorizer_CPP(ocvb, caller)
        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = colorCols / 2 ' change this higher to see the memory leak (or comment prediction loop above - it is the problem.)
        grid.sliders.TrackBar2.Value = colorRows / 4
        shadow = New Depth_Holes(ocvb, caller)
        label1 = "ML filled shadow"
        label2 = ""
        ocvb.desc = "Predict depth based on color and colorize depth to confirm correctness of model.  NOTE: memory leak occurs if more multi-threading is used!"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        grid.Run(ocvb)
        Dim depth32f = getDepth32f(ocvb)
        Dim minLearnCount = 5
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
            Sub(roi)
                depth32f(roi) = detectAndFillShadow(shadow.holeMask(roi), shadow.borderMask(roi), depth32f(roi), src(roi), minLearnCount)
            End Sub)

        colorizer.src = depth32f
        colorizer.Run(ocvb)
        dst1 = colorizer.dst1.Clone()
        dst1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class


Public Class ML_FillRGBDepth
    Inherits ocvbClass
    Dim shadow As Depth_Holes
    Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        colorizer = New Depth_Colorizer_CPP(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "ML Min Learn Count", 2, 100, 5)

        shadow = New Depth_Holes(ocvb, caller)
        shadow.sliders.TrackBar1.Value = 3

        label2 = "ML filled shadow"
        ocvb.desc = "Predict depth based on color and display colorized depth to confirm correctness of model."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        Dim minLearnCount = sliders.TrackBar1.Value
        ocvb.RGBDepth.CopyTo(dst1)
        Dim depth32f = getDepth32f(ocvb)
        depth32f = detectAndFillShadow(shadow.holeMask, shadow.borderMask, depth32f, src, minLearnCount)
        colorizer.src = depth32f
        colorizer.Run(ocvb)
        dst2 = colorizer.dst1
    End Sub
End Class


Public Class ML_DepthFromColor_MT
    Inherits ocvbClass
    Dim colorizer As Depth_Colorizer_CPP
    Dim grid As Thread_Grid
    Dim dilate As DilateErode_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        colorizer = New Depth_Colorizer_CPP(ocvb, caller)

        dilate = New DilateErode_Basics(ocvb, caller)
        dilate.sliders.TrackBar2.Value = 2

        sliders.setupTrackBar1(ocvb, caller, "Prediction Max Depth", 500, 5000, 1000)

        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = 16
        grid.sliders.TrackBar2.Value = 16

        label1 = "Predicted Depth"
        label2 = "Mask of color and depth input"
        ocvb.desc = "Use RGB, X, and Y to predict depth across the entire image, maxDepth = slider value."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        Dim depth32f = getDepth32f(ocvb)

        Dim mask = depth32f.Threshold(sliders.TrackBar1.Value, sliders.TrackBar1.Value, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        depth32f.SetTo(sliders.TrackBar1.Value, mask)

        Dim predictedDepth As New cv.Mat(depth32f.Size(), cv.MatType.CV_32F, 0)

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        dilate.src = mask
        dilate.Run(ocvb)
        mask = dilate.dst1
        dst2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat
        src.ConvertTo(color32f, cv.MatType.CV_32FC3)
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
        label2 = "Input region count = " + CStr(predictedRegions) + " of " + CStr(grid.roiList.Count)
        colorizer.src = predictedDepth
        colorizer.Run(ocvb)
        dst1 = colorizer.dst1
    End Sub
End Class



Public Class ML_DepthFromColor
    Inherits ocvbClass
    Dim colorizer As Depth_Colorizer_CPP
    Dim mats As Mat_4to1
    Dim shadow As Depth_Holes
    Dim resized As Resize_Percentage
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        colorizer = New Depth_Colorizer_CPP(ocvb, caller)

        mats = New Mat_4to1(ocvb, caller)

        shadow = New Depth_Holes(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "Prediction Max Depth", 1000, 5000, 1500)

        resized = New Resize_Percentage(ocvb, caller)
        resized.sliders.TrackBar1.Value = 2 ' 2% of the image.

        ocvb.desc = "Use RGB to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        mats.mat(0) = shadow.holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat

        resized.src = src
        resized.Run(ocvb)

        Dim colorROI As New cv.Rect(0, 0, resized.resizeOptions.newSize.Width, resized.resizeOptions.newSize.Height)
        resized.dst1.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim shadowSmall = mats.mat(0).Resize(color32f.Size()).Clone()
        color32f.SetTo(cv.Scalar.Black, shadowSmall) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Dim depth32f = getDepth32f(ocvb).Resize(color32f.Size())

        Dim mask = depth32f.Threshold(sliders.TrackBar1.Value, sliders.TrackBar1.Value, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        mats.mat(2) = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        cv.Cv2.BitwiseNot(mask, mask)
        depth32f.SetTo(sliders.TrackBar1.Value, mask)

        colorizer.src = depth32f
        colorizer.Run(ocvb)
        mats.mat(3) = colorizer.dst1.Clone()

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim maskCount = mask.CountNonZero()
        dst1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim learnInput = color32f.Reshape(1, color32f.Total)
        Dim depthResponse = depth32f.Reshape(1, depth32f.Total)

        ' now learn what depths are associated with which colors.
        Dim rtree = cv.ML.RTrees.Create()
        rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)

        src.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim input = color32f.Reshape(1, color32f.Total) ' test the entire original image.
        Dim output As New cv.Mat
        rtree.Predict(input, output)
        Dim predictedDepth = output.Reshape(1, src.Height)

        colorizer.src = predictedDepth
        colorizer.Run(ocvb)
        dst1 = colorizer.dst1.Clone()

        mats.Run(ocvb)
        dst2 = mats.dst1
        label1 = "Predicted Depth"
        label2 = "shadow, empty, Depth Mask < " + CStr(sliders.TrackBar1.Value) + ", Learn Input"
    End Sub
End Class



Public Class ML_DepthFromXYColor
    Inherits ocvbClass
    Dim mats As Mat_4to1
    Dim shadow As Depth_Holes
    Dim resized As Resize_Percentage
    Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        colorizer = New Depth_Colorizer_CPP(ocvb, caller)

        mats = New Mat_4to1(ocvb, caller)

        shadow = New Depth_Holes(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "Prediction Max Depth", 1000, 5000, 1500)

        resized = New Resize_Percentage(ocvb, caller)
        resized.sliders.TrackBar1.Value = 2

        label1 = "Predicted Depth"
        ocvb.desc = "Use RGB to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        mats.mat(0) = shadow.holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat

        resized.src = src.Clone()
        resized.Run(ocvb)

        Dim colorROI As New cv.Rect(0, 0, resized.resizeOptions.newSize.Width, resized.resizeOptions.newSize.Height)
        resized.dst1.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim shadowSmall = shadow.holeMask.Resize(color32f.Size()).Clone()
        color32f.SetTo(cv.Scalar.Black, shadowSmall) ' where depth is unknown, set to black (so we don't learn anything invalid, i.e. good color but missing depth.
        Dim depth32f = getDepth32f(ocvb).Resize(color32f.Size())

        Dim mask = depth32f.Threshold(sliders.TrackBar1.Value, sliders.TrackBar1.Value, cv.ThresholdTypes.BinaryInv)
        mask.SetTo(0, shadowSmall) ' remove the unknown depth...
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        mats.mat(2) = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        cv.Cv2.BitwiseNot(mask, mask)
        depth32f.SetTo(sliders.TrackBar1.Value, mask)

        colorizer.src = depth32f
        colorizer.Run(ocvb)
        mats.mat(3) = colorizer.dst1.Clone()

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim maskCount = mask.CountNonZero()
        dst1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim c = color32f.Reshape(1, color32f.Total)
        Dim depthResponse = depth32f.Reshape(1, depth32f.Total)

        Dim learnInput As New cv.Mat(c.Rows, 6, cv.MatType.CV_32F, 0)
        For y = 0 To c.Rows - 1
            For x = 0 To c.Cols - 1
                Dim v6 = New cv.Vec6f(c.Get(Of Single)(y, x), c.Get(Of Single)(y, x + 1), c.Get(Of Single)(y, x + 2), x, y, 0)
                learnInput.Set(Of cv.Vec6f)(y, x, v6)
            Next
        Next

        ' Now learn what depths are associated with which colors.
        Dim rtree = cv.ML.RTrees.Create()
        rtree.Train(learnInput, cv.ML.SampleTypes.RowSample, depthResponse)

        src.ConvertTo(color32f, cv.MatType.CV_32FC3)
        Dim allC = color32f.Reshape(1, color32f.Total) ' test the entire original image.
        Dim input As New cv.Mat(allC.Rows, 6, cv.MatType.CV_32F, 0)
        For y = 0 To allC.Rows - 1
            For x = 0 To allC.Cols - 1
                Dim v6 = New cv.Vec6f(allC.Get(Of Single)(y, x), allC.Get(Of Single)(y, x + 1), allC.Get(Of Single)(y, x + 2), x, y, 0)
                input.Set(Of cv.Vec6f)(y, x, v6)
            Next
        Next

        Dim output As New cv.Mat
        rtree.Predict(input, output)
        Dim predictedDepth = output.Reshape(1, src.Height)

        colorizer.src = predictedDepth
        colorizer.Run(ocvb)
        dst1 = colorizer.dst1.Clone()

        mats.Run(ocvb)
        dst2 = mats.dst1
        label2 = "shadow, empty, Depth Mask < " + CStr(sliders.TrackBar1.Value) + ", Learn Input"
    End Sub
End Class




Public Class ML_EdgeDepth
    Inherits ocvbClass
    Dim colorizer As Depth_Colorizer_CPP
    Dim grid As Thread_Grid
    Dim dilate As DilateErode_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        colorizer = New Depth_Colorizer_CPP(ocvb, caller)

        dilate = New DilateErode_Basics(ocvb, caller)
        dilate.sliders.TrackBar2.Value = 5

        sliders.setupTrackBar1(ocvb, caller, "Prediction Max Depth", 500, 5000, 1000)

        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = 16
        grid.sliders.TrackBar2.Value = 16

        label1 = "Depth Shadow (inverse of color and depth)"
        label2 = "Predicted Depth"
        ocvb.desc = "Use RGB to predict depth near edges."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        Dim depth32f = getDepth32f(ocvb)

        Dim mask = depth32f.Threshold(sliders.TrackBar1.Value, sliders.TrackBar1.Value, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        depth32f.SetTo(sliders.TrackBar1.Value, mask)

        Dim predictedDepth As New cv.Mat(depth32f.Size(), cv.MatType.CV_32F, 0)

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        dilate.src = mask
        dilate.Run(ocvb)
        dst1 = dilate.src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat
        src.ConvertTo(color32f, cv.MatType.CV_32FC3)
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
        label2 = "Input region count = " + CStr(predictedRegions) + " of " + CStr(grid.roiList.Count)
        colorizer.src = predictedDepth
        colorizer.Run(ocvb)
        dst2 = colorizer.dst1
    End Sub
End Class
