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
                    If borderMask.Get(of Byte)(y, x) Then
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
                        If holeMask.Get(of Byte)(y, x) Then
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
    Inherits VB_Class
    Dim shadow As Depth_Holes
    Dim grid As Thread_Grid
    Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        colorizer = New Depth_Colorizer_CPP(ocvb, "ML_FillRGBDepth_MT")
        colorizer.externalUse = True
        grid = New Thread_Grid(ocvb, "ML_FillRGBDepth_MT")
        grid.sliders.TrackBar1.Value = ocvb.color.Width / 2 ' change this higher to see the memory leak (or comment prediction loop above - it is the problem.)
        grid.sliders.TrackBar2.Value = ocvb.color.Height / 4
        grid.externalUse = True ' we don't need any results.
        shadow = New Depth_Holes(ocvb, "ML_FillRGBDepth_MT")
        ocvb.label1 = "ML filled shadow"
        ocvb.label2 = ""
        ocvb.desc = "Predict depth based on color and colorize depth to confirm correctness of model.  NOTE: memory leak occurs if more multi-threading is used!"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        grid.Run(ocvb)
        Dim depth32f = getDepth32f(ocvb)
        Dim minLearnCount = 5
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            depth32f(roi) = detectAndFillShadow(shadow.holeMask(roi), shadow.borderMask(roi), depth32f(roi), ocvb.color(roi), minLearnCount)
        End Sub)

        colorizer.src = depth32f
        colorizer.Run(ocvb)
        ocvb.result1 = colorizer.dst.Clone()
        ocvb.result1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
    Public Sub MyDispose()
        shadow.Dispose()
        grid.Dispose()
        colorizer.Dispose()
    End Sub
End Class


Public Class ML_FillRGBDepth
    Inherits VB_Class
    Dim shadow As Depth_Holes
        Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        colorizer = New Depth_Colorizer_CPP(ocvb, "ML_FillRGBDepth")
        colorizer.externalUse = True

        sliders.setupTrackBar1(ocvb, callerName, "ML Min Learn Count", 2, 100, 5)
        
        shadow = New Depth_Holes(ocvb, "ML_FillRGBDepth")
        shadow.sliders.TrackBar1.Value = 3

        ocvb.label2 = "ML filled shadow"
        ocvb.desc = "Predict depth based on color and display colorized depth to confirm correctness of model."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        Dim minLearnCount = sliders.TrackBar1.Value
        ocvb.RGBDepth.CopyTo(ocvb.result1)
        Dim depth32f = getDepth32f(ocvb)
        depth32f = detectAndFillShadow(shadow.holeMask, shadow.borderMask, depth32f, ocvb.color, minLearnCount)
        colorizer.src = depth32f
        colorizer.Run(ocvb)
        ocvb.result2 = colorizer.dst
    End Sub
    Public Sub MyDispose()
        shadow.Dispose()
                colorizer.Dispose()
    End Sub
End Class


Public Class ML_DepthFromColor_MT
    Inherits VB_Class
    Dim colorizer As Depth_Colorizer_CPP
    Dim grid As Thread_Grid
    Dim dilate As DilateErode_Basics
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        colorizer = New Depth_Colorizer_CPP(ocvb, "ML_DepthFromColor_MT")
        colorizer.externalUse = True

        dilate = New DilateErode_Basics(ocvb, "ML_DepthFromColor_MT")
        dilate.externalUse = True
        dilate.sliders.TrackBar2.Value = 2

        sliders.setupTrackBar1(ocvb, callerName, "Prediction Max Depth", 500, 5000, 1000)
        
        grid = New Thread_Grid(ocvb, "ML_DepthFromColor_MT")
        grid.sliders.TrackBar1.Value = 16
        grid.sliders.TrackBar2.Value = 16
        grid.externalUse = True

        ocvb.label1 = "Predicted Depth"
        ocvb.label2 = "Mask of color and depth input"
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
        colorizer.src = predictedDepth
        colorizer.Run(ocvb)
        ocvb.result1 = colorizer.dst
    End Sub
    Public Sub MyDispose()
                dilate.Dispose()
        grid.Dispose()
        colorizer.Dispose()
    End Sub
End Class



Public Class ML_DepthFromColor
    Inherits VB_Class
    Dim colorizer As Depth_Colorizer_CPP
    Dim mats As Mat_4to1
    Dim shadow As Depth_Holes
    Dim resized As Resize_Percentage
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        colorizer = New Depth_Colorizer_CPP(ocvb, "ML_DepthFromColor")
        colorizer.externalUse = True

        mats = New Mat_4to1(ocvb, "ML_DepthFromColor")
        mats.externalUse = True

        shadow = New Depth_Holes(ocvb, "ML_DepthFromColor")

        sliders.setupTrackBar1(ocvb, callerName, "Prediction Max Depth", 1000, 5000, 1500)
        
        resized = New Resize_Percentage(ocvb, "ML_DepthFromColor")
        resized.externalUse = True
        resized.sliders.TrackBar1.Value = 2 ' 2% of the image.

        ocvb.desc = "Use RGB to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        mats.mat(0) = shadow.holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat

        resized.src = ocvb.color.Clone()
        resized.Run(ocvb)

        Dim colorROI As New cv.Rect(0, 0, resized.resizeOptions.newSize.Width, resized.resizeOptions.newSize.Height)
        resized.dst.ConvertTo(color32f, cv.MatType.CV_32FC3)
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
        mats.mat(3) = colorizer.dst.Clone()

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
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
        Dim predictedDepth = output.Reshape(1, ocvb.color.Height)

        colorizer.src = predictedDepth
        colorizer.Run(ocvb)
        ocvb.result1 = colorizer.dst.Clone()

        mats.Run(ocvb)
        ocvb.result2 = mats.dst
        ocvb.label1 = "Predicted Depth"
        ocvb.label2 = "shadow, empty, Depth Mask < " + CStr(sliders.TrackBar1.Value) + ", Learn Input"
    End Sub
    Public Sub MyDispose()
                shadow.Dispose()
        mats.Dispose()
        resized.Dispose()
        colorizer.Dispose()
    End Sub
End Class



Public Class ML_DepthFromXYColor
    Inherits VB_Class
    Dim mats As Mat_4to1
    Dim shadow As Depth_Holes
    Dim resized As Resize_Percentage
        Dim colorizer As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        colorizer = New Depth_Colorizer_CPP(ocvb, "ML_DepthFromXYColor")
        colorizer.externalUse = True

        mats = New Mat_4to1(ocvb, "ML_DepthFromXYColor")
        mats.externalUse = True

        shadow = New Depth_Holes(ocvb, "ML_DepthFromXYColor")

        sliders.setupTrackBar1(ocvb, callerName, "Prediction Max Depth", 1000, 5000, 1500)
        
        resized = New Resize_Percentage(ocvb, "ML_DepthFromXYColor")
        resized.externalUse = True
        resized.sliders.TrackBar1.Value = 2

        ocvb.label1 = "Predicted Depth"
        ocvb.desc = "Use RGB to predict depth across the entire image, maxDepth = slider value, resize % as well."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)
        mats.mat(0) = shadow.holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Dim color32f As New cv.Mat

        resized.src = ocvb.color.Clone()
        resized.Run(ocvb)

        Dim colorROI As New cv.Rect(0, 0, resized.resizeOptions.newSize.Width, resized.resizeOptions.newSize.Height)
        resized.dst.ConvertTo(color32f, cv.MatType.CV_32FC3)
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
        mats.mat(3) = colorizer.dst.Clone()

        mask = depth32f.Threshold(1, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim maskCount = mask.CountNonZero()
        ocvb.result1 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

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

        ocvb.color.ConvertTo(color32f, cv.MatType.CV_32FC3)
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
        Dim predictedDepth = output.Reshape(1, ocvb.color.Height)

        colorizer.src = predictedDepth
        colorizer.Run(ocvb)
        ocvb.result1 = colorizer.dst.Clone()

        mats.Run(ocvb)
        ocvb.result2 = mats.dst
        ocvb.label2 = "shadow, empty, Depth Mask < " + CStr(sliders.TrackBar1.Value) + ", Learn Input"
    End Sub
    Public Sub MyDispose()
                shadow.Dispose()
        mats.Dispose()
        resized.Dispose()
        colorizer.Dispose()
    End Sub
End Class




Public Class ML_EdgeDepth
    Inherits VB_Class
    Dim colorizer As Depth_Colorizer_CPP
    Dim grid As Thread_Grid
    Dim dilate As DilateErode_Basics
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        colorizer = New Depth_Colorizer_CPP(ocvb, "ML_EdgeDepth")
        colorizer.externalUse = True

        dilate = New DilateErode_Basics(ocvb, "ML_EdgeDepth")
        dilate.externalUse = True
        dilate.sliders.TrackBar2.Value = 5

        sliders.setupTrackBar1(ocvb, callerName, "Prediction Max Depth", 500, 5000, 1000)
        
        grid = New Thread_Grid(ocvb, "ML_EdgeDepth")
        grid.sliders.TrackBar1.Value = 16
        grid.sliders.TrackBar2.Value = 16
        grid.externalUse = True

        ocvb.label1 = "Depth Shadow (inverse of color and depth)"
        ocvb.label2 = "Predicted Depth"
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
        colorizer.src = predictedDepth
        colorizer.Run(ocvb)
        ocvb.result2 = colorizer.dst
    End Sub
    Public Sub MyDispose()
                dilate.Dispose()
        grid.Dispose()
        colorizer.Dispose()
    End Sub
End Class