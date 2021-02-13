Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Depth_Flatland
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Region Count", 1, 250, 10)
        End If
        label2 = "Grayscale version"
        task.desc = "Attempt to stabilize the depth image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim reductionFactor = sliders.trackbar(0).Maximum - sliders.trackbar(0).Value
        dst1 = task.RGBDepth / reductionFactor
        dst1 *= reductionFactor
        dst2 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class



Public Class Depth_FirstLastDistance
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Monitor the first and last depth distances"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim mask = task.depth32f.Threshold(1, 20000, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim minVal As Double, maxVal As Double
        Dim minPt As cv.Point, maxPt As cv.Point
        cv.Cv2.MinMaxLoc(task.depth32f, minVal, maxVal, minPt, maxPt, mask)
        task.RGBDepth.CopyTo(dst1)
        task.RGBDepth.CopyTo(dst2)
        label1 = "Min Depth " + CStr(minVal) + " mm"
        dst1.Circle(minPt, 10, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        label2 = "Max Depth " + CStr(maxVal) + " mm"
        dst2.Circle(maxPt, 10, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
    End Sub
End Class





Public Class Depth_HolesRect
    Inherits VBparent
    Dim shadow As Depth_Holes
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "shadowRect Min Size", 1, 20000, 2000)
        End If
        shadow = New Depth_Holes()

        task.desc = "Identify the minimum rectangles of contours of the depth shadow"
    End Sub

    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        shadow.Run()

        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(shadow.dst2, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        Dim minEllipse(contours.Length - 1) As cv.RotatedRect
        Static sizeSlider = findSlider("shadowRect Min Size")
        Dim minSize = sizeSlider.value
        For i = 0 To contours.Length - 1
            Dim minRect = cv.Cv2.MinAreaRect(contours(i))
            Dim size = minRect.Size.Width * minRect.Size.Height
            If size > minSize Then
                Dim nextColor = New cv.Scalar(ocvb.vecColors(i Mod 255).Item0, ocvb.vecColors(i Mod 255).Item1, ocvb.vecColors(i Mod 255).Item2)
                drawRotatedRectangle(minRect, dst1, nextColor)
                If contours(i).Length >= 5 Then
                    minEllipse(i) = cv.Cv2.FitEllipse(contours(i))
                End If
            End If
        Next
        cv.Cv2.AddWeighted(dst1, 0.5, task.RGBDepth, 0.5, 0, dst1)
    End Sub
End Class







Public Class Depth_FlatData
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "FlatData Region Count", 1, 250, 200)
        End If
        label1 = "Reduced resolution RGBDepth"
        task.desc = "Attempt to stabilize the depth image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim gray As New cv.Mat
        Dim gray8u As New cv.Mat

        Dim depthMask As cv.Mat = task.inrange.depthMask
        gray = task.depth32f.Normalize(0, 255, cv.NormTypes.MinMax, -1, depthMask)
        gray.ConvertTo(gray8u, cv.MatType.CV_8U)

        Dim reductionFactor = sliders.trackbar(0).Maximum - sliders.trackbar(0).Value
        gray8u = gray8u / reductionFactor
        gray8u *= reductionFactor

        dst1 = gray8u.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class








Module DepthXYZ_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_XYZ_OpenMP_Open(ppx As Single, ppy As Single, fx As Single, fy As Single) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_XYZ_OpenMP_Close(DepthXYZPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_XYZ_OpenMP_Run(DepthXYZPtr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function
End Module







Public Class Depth_MeanStdev_MT
    Inherits VBparent
    Dim grid As Thread_Grid
    Dim meanSeries As New cv.Mat
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 64
        gridHeightSlider.Value = 40

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "MeanStdev Max Depth Range", 1, 20000, 3500)
            sliders.setupTrackBar(1, "MeanStdev Frame Series", 1, 100, 5)
        End If
        task.desc = "Collect a time series of depth and measure where the stdev is unstable.  Plan is to avoid depth where unstable."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        grid.Run()
        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U)
        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U)

        Dim maxDepth = sliders.trackbar(0).Value
        Dim meanCount = sliders.trackbar(1).Value

        Static lastMeanCount As Integer
        If grid.roiList.Count <> meanSeries.Rows Or meanCount <> lastMeanCount Then
            meanSeries = New cv.Mat(grid.roiList.Count, meanCount, cv.MatType.CV_32F, 0)
            lastMeanCount = meanCount
        End If

        Dim mask As New cv.Mat, tmp16 As New cv.Mat
        cv.Cv2.InRange(task.depth32f, 1, maxDepth, tmp16)
        cv.Cv2.ConvertScaleAbs(tmp16, mask)
        Dim outOfRangeMask As New cv.Mat
        cv.Cv2.BitwiseNot(mask, outOfRangeMask)

        Dim minVal As Double, maxVal As Double
        Dim minPt As cv.Point, maxPt As cv.Point
        cv.Cv2.MinMaxLoc(task.depth32f, minVal, maxVal, minPt, maxPt, mask)

        Dim meanIndex = ocvb.frameCount Mod meanCount
        Dim meanValues As New cv.Mat(grid.roiList.Count - 1, 1, cv.MatType.CV_32F)
        Dim stdValues As New cv.Mat(grid.roiList.Count - 1, 1, cv.MatType.CV_32F)
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim mean As Single = 0, stdev As Single = 0
            cv.Cv2.MeanStdDev(task.depth32f(roi), mean, stdev, mask(roi))
            meanSeries.Set(Of Single)(i, meanIndex, mean)
            If ocvb.frameCount >= meanCount - 1 Then
                cv.Cv2.MeanStdDev(meanSeries.Row(i), mean, stdev)
                meanValues.Set(Of Single)(i, 0, mean)
                stdValues.Set(Of Single)(i, 0, stdev)
            End If
        End Sub)

        If ocvb.frameCount >= meanCount Then
            Dim minStdVal As Double, maxStdVal As Double
            Dim meanmask = meanValues.Threshold(1, maxDepth, cv.ThresholdTypes.Binary).ConvertScaleAbs()
            cv.Cv2.MinMaxLoc(meanValues, minVal, maxVal, minPt, maxPt, meanmask)
            Dim stdMask = stdValues.Threshold(0.001, maxDepth, cv.ThresholdTypes.Binary).ConvertScaleAbs() ' volatile region is x cm stdev.
            cv.Cv2.MinMaxLoc(stdValues, minStdVal, maxStdVal, minPt, maxPt, stdMask)

            Parallel.For(0, grid.roiList.Count,
            Sub(i)
                Dim roi = grid.roiList(i)
                ' this marks all the regions where the depth is volatile.
                dst2(roi).SetTo(255 * (stdValues.Get(Of Single)(i, 0) - minStdVal) / (maxStdVal - minStdVal))
                dst2(roi).SetTo(0, outOfRangeMask(roi))

                dst1(roi).SetTo(255 * (meanValues.Get(Of Single)(i, 0) - minVal) / (maxVal - minVal))
                dst1(roi).SetTo(0, outOfRangeMask(roi))
            End Sub)
            cv.Cv2.BitwiseOr(dst2, grid.gridMask, dst2)
            label2 = "Stdev for each ROI (normalized): Min " + Format(minStdVal, "#0.0") + " Max " + Format(maxStdVal, "#0.0")
        End If
        label1 = "Mean for each ROI (normalized): Min " + Format(minVal, "#0.0") + " Max " + Format(maxVal, "#0.0")
    End Sub
End Class






Public Class Depth_MeanStdevPlot
    Inherits VBparent
    Dim plot1 As Plot_OverTime
    Dim plot2 As Plot_OverTime
    Public Sub New()
        initParent()

        plot1 = New Plot_OverTime()
        plot1.dst1 = dst1
        plot1.maxScale = 2000
        plot1.plotCount = 1

        plot2 = New Plot_OverTime()
        plot2.dst1 = dst2
        plot2.maxScale = 1000
        plot2.plotCount = 1

        task.desc = "Plot the mean and stdev of the depth image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim mean As Single = 0, stdev As Single = 0
        Dim depthMask As cv.Mat = task.inrange.depthMask
        cv.Cv2.MeanStdDev(task.depth32f, mean, stdev, depthMask)

        If mean > plot1.maxScale Then plot1.maxScale = mean + 1000 - (mean + 1000) Mod 1000
        If stdev > plot2.maxScale Then plot2.maxScale = stdev + 1000 - (stdev + 1000) Mod 1000

        plot1.plotData = New cv.Scalar(mean, 0, 0)
        plot1.Run()
        dst1 = plot1.dst1

        plot2.plotData = New cv.Scalar(stdev, 0, 0)
        plot2.Run()
        dst2 = plot2.dst1

        label1 = "Plot of mean depth = " + Format(mean, "#0.0")
        label2 = "Plot of depth stdev = " + Format(stdev, "#0.0")
    End Sub
End Class




Public Class Depth_Uncertainty
    Inherits VBparent
    Dim retina As Retina_Basics_CPP
    Public Sub New()
        initParent()
        retina = New Retina_Basics_CPP()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Uncertainty threshold", 1, 255, 100)
        End If
        label2 = "Mask of areas with unstable depth"
        task.desc = "Use the bio-inspired retina algorithm to determine depth uncertainty."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        retina.src = task.RGBDepth
        retina.Run()
        dst1 = retina.dst1
        dst2 = retina.dst2.Threshold(sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Depth_Palette
    Inherits VBparent
    Dim customColorMap As New cv.Mat
    Public Sub New()
        initParent()

        customColorMap = colorTransition(cv.Scalar.Blue, cv.Scalar.Yellow, 256)
        task.desc = "Use a palette to display depth from the raw depth data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim minDepth = task.inrange.minVal
        Dim maxDepth = task.inrange.maxVal

        Dim depthNorm = (task.depth32f * 255 / (maxDepth - minDepth)).ToMat ' do the normalize manually to use the min and max Depth (more stable)
        depthNorm.ConvertTo(depthNorm, cv.MatType.CV_8U)
        dst1 = Palette_Custom_Apply(depthNorm.CvtColor(cv.ColorConversionCodes.GRAY2BGR), customColorMap).SetTo(0, task.inrange.noDepthMask)
    End Sub
End Class




Module Depth_Colorizer_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_Colorizer_Close(Depth_ColorizerPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer2_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_Colorizer2_Close(Depth_ColorizerPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer2_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer, histSize As Integer) As IntPtr
    End Function


    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer32f_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_Colorizer32f_Close(Depth_ColorizerPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer32f_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer32f2_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_Colorizer32f2_Close(Depth_ColorizerPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer32f2_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer, histSize As Integer) As IntPtr
    End Function
End Module




Public Class Depth_Colorizer_CPP
    Inherits VBparent
    Dim dcPtr As IntPtr
    Public Sub New()
        initParent()
        dcPtr = Depth_Colorizer_Open()
        task.desc = "Display Depth image using C++ instead of VB.Net"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If src.Type <> cv.MatType.CV_32F Then
            If standalone Or task.intermediateReview = caller Then src = task.depth32f Else dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        End If
        Dim depthData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_Colorizer_Run(dcPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)
    End Sub
    Public Sub Close()
        Depth_Colorizer_Close(dcPtr)
    End Sub
End Class







Public Class Depth_ColorizerFastFade_CPP
    Inherits VBparent
    Dim dcPtr As IntPtr
    Public Sub New()
        initParent()
        dcPtr = Depth_Colorizer2_Open()
        label2 = "Mask from Depth_InRange"
        task.desc = "Display depth data with InRange.  Higher contrast than others - yellow to blue always present."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.depth32f

        dst2 = task.inrange.nodepthMask

        Dim depthData(input.Total * input.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
        Marshal.Copy(input.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_Colorizer2_Run(dcPtr, handleSrc.AddrOfPinnedObject(), input.Rows, input.Cols, task.maxRangeSlider.Value)
        handleSrc.Free()

        If imagePtr <> 0 Then
            dst1 = New cv.Mat(input.Rows, input.Cols, cv.MatType.CV_8UC3, imagePtr)
            If standalone Or task.intermediateReview = caller Then dst1.SetTo(0, dst2)
        End If
    End Sub
    Public Sub Close()
        Depth_Colorizer2_Close(dcPtr)
    End Sub
End Class




' this algorithm is only intended to show how the depth can be colorized.  It is very slow.  Use the C++ version of this code nearby.
Public Class Depth_ColorizerVB
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Colorize depth manually."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim src = task.depth32f
        Dim nearColor = New Byte() {0, 255, 255}
        Dim farColor = New Byte() {255, 0, 0}

        Dim histogram(256 * 256 - 1) As Integer
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim pixel = Math.Truncate(src.Get(Of Single)(y, x))
                If pixel Then histogram(pixel) += 1
            Next
        Next
        For i = 1 To histogram.Length - 1
            histogram(i) += histogram(i - 1) + 1
        Next
        For i = 1 To histogram.Length - 1
            histogram(i) = (histogram(i) << 8) / histogram(256 * 256 - 1)
        Next

        Dim stride = src.Width * 3
        Dim rgbdata(stride * src.Height) As Byte
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim pixel = Math.Truncate(src.Get(Of Single)(y, x))
                If pixel Then
                    Dim t = histogram(pixel)
                    rgbdata(x * 3 + 0 + y * stride) = ((256 - t) * nearColor(0) + t * farColor(0)) >> 8
                    rgbdata(x * 3 + 1 + y * stride) = ((256 - t) * nearColor(1) + t * farColor(1)) >> 8
                    rgbdata(x * 3 + 2 + y * stride) = ((256 - t) * nearColor(2) + t * farColor(2)) >> 8
                End If
            Next
        Next
        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, rgbdata)
    End Sub
End Class





Public Class Depth_ColorizerVB_MT
    Inherits VBparent
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Min Depth", 0, 1000, 0)
            sliders.setupTrackBar(1, "Max Depth", 1001, 10000, 4000)
        End If
        grid = New Thread_Grid

        task.desc = "Colorize depth manually with multi-threading."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        grid.Run()

        If standalone Or task.intermediateReview = caller Then src = task.depth32f
        Dim nearColor = New Single() {0, 1, 1}
        Dim farColor = New Single() {1, 0, 0}

        Dim minDepth = sliders.trackbar(0).Value
        Dim maxDepth = sliders.trackbar(1).Value
        Dim histSize = maxDepth - minDepth

        Dim dimensions() = New Integer() {histSize}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minDepth, maxDepth)}

        Dim hist As New cv.Mat()
        cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {0}, New cv.Mat, hist, 1, dimensions, ranges)

        Dim histogram(histSize - 1) As Single
        Marshal.Copy(hist.Data, histogram, 0, histogram.Length)
        For i = 1 To histogram.Length - 1
            histogram(i) += histogram(i - 1)
        Next

        Dim maxHist = histogram(histSize - 1)
        If maxHist > 0 Then
            Parallel.ForEach(grid.roiList,
           Sub(roi)
               Dim depth = src(roi)
               Dim rgbdata(src.Total) As cv.Vec3b
               Dim rgbIndex As Integer
               For y = 0 To depth.Rows - 1
                   For x = 0 To depth.Cols - 1
                       Dim pixel = Math.Truncate(depth.Get(Of Single)(y, x))
                       If pixel > 0 And pixel < histSize Then
                           Dim t = histogram(pixel) / maxHist
                           rgbdata(rgbIndex) = New cv.Vec3b(((1 - t) * nearColor(0) + t * farColor(0)) * 255,
                                                            ((1 - t) * nearColor(1) + t * farColor(1)) * 255,
                                                            ((1 - t) * nearColor(2) + t * farColor(2)) * 255)
                       End If
                       rgbIndex += 1
                   Next
               Next
               dst1(roi) = New cv.Mat(depth.Rows, depth.Cols, cv.MatType.CV_8UC3, rgbdata)
           End Sub)

        End If
        dst1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class





Public Class Depth_Colorizer_MT
    Inherits VBparent
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        task.desc = "Colorize normally uses CDF to stabilize the colors.  Just using sliders here - stabilized but not optimal range."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        grid.Run()

        If standalone Or task.intermediateReview = caller Then src = task.depth32f
        Dim nearColor = New Single() {0, 1, 1}
        Dim farColor = New Single() {1, 0, 0}

        Dim range = task.inrange.maxval - task.inrange.minval
        Parallel.ForEach(grid.roiList,
         Sub(roi)
             Dim depth = src(roi)
             Dim stride = depth.Width * 3
             Dim rgbdata(stride * depth.Height) As Byte
             For y = 0 To depth.Rows - 1
                 For x = 0 To depth.Cols - 1
                     Dim pixel = depth.Get(Of Single)(y, x)
                     If pixel > task.inrange.minval And pixel <= task.inrange.maxval Then
                         Dim t = (pixel - task.inrange.minval) / range
                         rgbdata(x * 3 + 0 + y * stride) = ((1 - t) * nearColor(0) + t * farColor(0)) * 255
                         rgbdata(x * 3 + 1 + y * stride) = ((1 - t) * nearColor(1) + t * farColor(1)) * 255
                         rgbdata(x * 3 + 2 + y * stride) = ((1 - t) * nearColor(2) + t * farColor(2)) * 255
                     End If
                 Next
             Next
             dst1(roi) = New cv.Mat(depth.Rows, depth.Cols, cv.MatType.CV_8UC3, rgbdata)
         End Sub)
        dst1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class





Public Class Depth_LocalMinMax_MT
    Inherits VBparent
    Public grid As Thread_Grid
    Public minPoint(0) As cv.Point2f
    Public maxPoint(0) As cv.Point2f
    Public Sub New()
        initParent()
        grid = New Thread_Grid

        label1 = "Red is min distance, blue is max distance"
        task.desc = "Find min and max depth in each segment."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        grid.Run()

        Dim mask = task.depth32f.Threshold(1, 5000, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8UC1)

        If standalone Or task.intermediateReview = caller Then
            src.CopyTo(dst1)
            dst1.SetTo(cv.Scalar.White, grid.gridMask)
        End If

        If minPoint.Length <> grid.roiList.Count Then
            ReDim minPoint(grid.roiList.Count - 1)
            ReDim maxPoint(grid.roiList.Count - 1)
        End If
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim minVal As Double, maxVal As Double
            Dim minPt As cv.Point, maxPt As cv.Point
            cv.Cv2.MinMaxLoc(task.depth32f(roi), minVal, maxVal, minPt, maxPt, mask(roi))
            If minPt.X < 0 Or minPt.Y < 0 Then minPt = New cv.Point2f(0, 0)
            minPoint(i) = New cv.Point(minPt.X + roi.X, minPt.Y + roi.Y)
            maxPoint(i) = New cv.Point(maxPt.X + roi.X, maxPt.Y + roi.Y)

            cv.Cv2.Circle(dst1(roi), minPt, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            cv.Cv2.Circle(dst1(roi), maxPt, 5, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
        End Sub)
    End Sub
End Class





Public Class Depth_LocalMinMax_Kalman_MT
    Inherits VBparent
    Dim kalman As Kalman_Basics
    Public grid As Thread_Grid
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 128
        gridHeightSlider.Value = 90
        grid.Run()

        kalman = New Kalman_Basics()
        ReDim kalman.kInput(grid.roiList.Count * 4 - 1)

        label1 = "Red is min distance, blue is max distance"
        task.desc = "Find minimum depth in each segment."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        grid.Run()

        If grid.roiList.Count * 4 <> kalman.kInput.Length Then
            If kalman IsNot Nothing Then kalman.Dispose()
            kalman = New Kalman_Basics()
            ReDim kalman.kInput(grid.roiList.Count * 4 - 1)
        End If

        dst1 = src.Clone()
        dst1.SetTo(cv.Scalar.White, grid.gridMask)

        Dim depth32f As cv.Mat = task.depth32f
        Dim depthmask As cv.Mat = task.inrange.depthmask

        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim minVal As Double, maxVal As Double
            Dim minPt As cv.Point, maxPt As cv.Point
            cv.Cv2.MinMaxLoc(depth32f(roi), minVal, maxVal, minPt, maxPt, depthmask(roi))
            If minPt.X < 0 Or minPt.Y < 0 Then minPt = New cv.Point2f(0, 0)
            kalman.kInput(i * 4) = minPt.X
            kalman.kInput(i * 4 + 1) = minPt.Y
            kalman.kInput(i * 4 + 2) = maxPt.X
            kalman.kInput(i * 4 + 3) = maxPt.Y
        End Sub)

        kalman.Run()

        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, src.Width, src.Height))
        Dim radius = 5
        For i = 0 To grid.roiList.Count - 1
            Dim roi = grid.roiList(i)
            Dim ptmin = New cv.Point2f(kalman.kOutput(i * 4) + roi.X, kalman.kOutput(i * 4 + 1) + roi.Y)
            Dim ptmax = New cv.Point2f(kalman.kOutput(i * 4 + 2) + roi.X, kalman.kOutput(i * 4 + 3) + roi.Y)
            ptmin = validatePoint2f(ptmin)
            ptmax = validatePoint2f(ptmax)
            subdiv.Insert(ptmin)
            cv.Cv2.Circle(dst1, ptmin, radius, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            cv.Cv2.Circle(dst1, ptmax, radius, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
        Next
        paint_voronoi(ocvb.scalarColors, dst2, subdiv)
    End Sub
End Class





Public Class Depth_ColorMap
    Inherits VBparent
    Dim Palette As Palette_Basics
    Public Sub New()
        initParent()
        Palette = New Palette_Basics()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Depth ColorMap Alpha X100", 1, 100, 5)
            sliders.setupTrackBar(1, "Depth ColorMap Beta", 1, 100, 3)
        End If
        task.desc = "Display the depth as a color map"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim alpha = sliders.trackbar(0).Value / 100
        Dim beta = sliders.trackbar(1).Value
        cv.Cv2.ConvertScaleAbs(task.depth32f, Palette.src, alpha, beta)
        Palette.Run()
        dst1 = Palette.dst1
        dst2 = Palette.dst2
    End Sub
End Class






Public Class Depth_NotMissing
    Inherits VBparent
    Public mog As BGSubtract_Basics_CPP
    Public Sub New()
        initParent()

        mog = New BGSubtract_Basics_CPP()

        label2 = "Stable (non-zero) Depth"
        task.desc = "Collect X frames, compute stable depth using the RGB and Depth image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone Or task.intermediateReview = caller Then src = task.RGBDepth
        mog.src = src
        mog.Run()
        dst1 = mog.dst1
        cv.Cv2.BitwiseNot(mog.dst1, dst2)
        label1 = "Unstable Depth" + " using " + mog.radio.check(mog.currMethod).Text + " method"
        Dim zeroDepth = task.depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(1)
        dst2.SetTo(0, zeroDepth)
    End Sub
End Class








Public Class Depth_Median
    Inherits VBparent
    Dim median As Math_Median_CDF
    Public Sub New()
        initParent()
        median = New Math_Median_CDF()
        median.src = New cv.Mat
        median.rangeMax = 10000
        median.rangeMin = 1 ' ignore depth of zero as it is not known.
        task.desc = "Divide the depth image ahead and behind the median."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        median.src = task.depth32f
        median.Run()

        Dim mask As cv.Mat
        mask = median.src.LessThan(median.medianVal)
        task.RGBDepth.CopyTo(dst1, mask)

        Dim zeroMask = median.src.Equals(0)
        cv.Cv2.ConvertScaleAbs(zeroMask, zeroMask.ToMat)
        dst1.SetTo(0, zeroMask)

        label1 = "Median Depth < " + Format(median.medianVal, "#0.0")

        cv.Cv2.BitwiseNot(mask, mask)
        dst2.SetTo(0)
        task.RGBDepth.CopyTo(dst2, mask)
        dst2.SetTo(0, zeroMask)
        label2 = "Median Depth > " + Format(median.medianVal, "#0.0")
    End Sub
End Class






Public Class Depth_SmoothingMat
    Inherits VBparent
    Public inrange As Depth_InRange
    Public Sub New()
        initParent()
        inrange = New Depth_InRange()
        inrange.depth32fAfterMasking = True

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Threshold in millimeters", 1, 1000, 10)
        End If
        label2 = "Depth pixels after smoothing"
        task.desc = "Use depth rate of change to smooth the depth values beyond close range"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone Or task.intermediateReview = caller Then src = task.depth32f
        Dim rect = If(task.drawRect.Width <> 0, task.drawRect, New cv.Rect(0, 0, src.Width, src.Height))
        inrange.src = src(rect)
        inrange.Run()
        Static lastDepth = inrange.dst2 ' the far depth needs to be smoothed
        If lastDepth.Size <> inrange.dst1.Size Then lastDepth = inrange.dst1

        cv.Cv2.Subtract(lastDepth, inrange.dst1, dst1)

        Static thresholdSlider = findSlider("Threshold in millimeters")
        Dim mmThreshold = CSng(thresholdSlider.Value)
        dst1 = dst1.Threshold(mmThreshold, 0, cv.ThresholdTypes.TozeroInv).Threshold(-mmThreshold, 0, cv.ThresholdTypes.Tozero)
        cv.Cv2.Add(inrange.dst1, dst1, dst2)
        lastDepth = inrange.dst1

        label1 = "Smoothing Mat: range from " + CStr(task.inrange.minval) + " to +" + CStr(task.inrange.maxval)
    End Sub
End Class





Public Class Depth_Smoothing
    Inherits VBparent
    Dim smooth As Depth_SmoothingMat
    Dim reduction As Reduction_Basics
    Public reducedDepth As New cv.Mat
    Public mats As Mat_4to1
    Public colorize As Depth_ColorMap
    Public Sub New()
        initParent()

        colorize = New Depth_ColorMap()
        mats = New Mat_4to1()
        reduction = New Reduction_Basics()
        Dim reductionRadio = findRadio("Use bitwise reduction")
        reductionRadio.Checked = True
        smooth = New Depth_SmoothingMat()
        label2 = "Mask of depth that is smooth"
        task.desc = "This attempt to get the depth data to 'calm' down is not working well enough to be useful - needs more work"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        smooth.src = task.depth32f
        smooth.Run()
        Dim input = smooth.dst1.Normalize(0, 255, cv.NormTypes.MinMax)
        input.ConvertTo(mats.mat(0), cv.MatType.CV_8UC1)
        Dim tmp As New cv.Mat
        cv.Cv2.Add(smooth.dst2, smooth.dst1, tmp)
        mats.mat(1) = tmp.InRange(0, 255)

        reduction.src = smooth.src
        reduction.Run()
        reduction.dst1.ConvertTo(reducedDepth, cv.MatType.CV_32F)
        colorize.src = reducedDepth
        colorize.Run()
        dst1 = colorize.dst1
        mats.Run()
        dst2 = mats.dst1
        label1 = smooth.label1
    End Sub
End Class










Public Class Depth_Edges
    Inherits VBparent
    Dim edges As Edges_Laplacian
    Public Sub New()
        initParent()
        edges = New Edges_Laplacian()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Threshold for depth disparity", 0, 255, 200)
        End If
        task.desc = "Find edges in depth data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        edges.src = src
        edges.Run()
        dst1 = edges.dst2
        dst2 = edges.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Depth_HolesOverTime
    Inherits VBparent
    Dim recentImages As New List(Of cv.Mat)
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Number of images to retain", 0, 30, 3)
        End If
        label2 = "Latest hole mask"
        task.desc = "Integrate memory holes over time to identify unstable depth"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        recentImages.Add(task.inrange.noDepthMask.clone) ' To see the value of clone, remove it temporarily.  Only the most recent depth holes are added in.

        dst2 = task.inrange.noDepthMask
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For Each img In recentImages
            cv.Cv2.BitwiseOr(dst1, img, dst1)
        Next
        label1 = "Depth holes integrated over the past " + CStr(recentImages.Count) + " images"
        If recentImages.Count >= sliders.trackbar(0).Value Then recentImages.RemoveAt(0)
    End Sub
End Class








Public Class Depth_Holes
    Inherits VBparent
    Public holeMask As New cv.Mat
    Dim element As New cv.Mat
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Amount of dilation of borderMask", 1, 10, 1)
            sliders.setupTrackBar(1, "Amount of dilation of holeMask", 0, 10, 0)
        End If
        label2 = "Shadow Edges (use sliders to expand)"
        element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(5, 5))
        task.desc = "Identify holes in the depth image."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        holeMask = task.depth32f.Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)
        holeMask = holeMask.Dilate(element, Nothing, sliders.trackbar(1).Value)
        dst1 = holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        dst2 = holeMask.Dilate(element, Nothing, sliders.trackbar(0).Value)
        cv.Cv2.BitwiseXor(dst2, holeMask, dst2)
        If standalone Or task.intermediateReview = caller Then task.RGBDepth.CopyTo(dst2, dst2)
    End Sub
End Class






Public Class Depth_TooClose
    Inherits VBparent
    Public minVal As Double
    Public depth32f As cv.Mat
    Public Sub New()
        initParent()
        label2 = "Non-Zero depth mask"
        task.desc = "Tests to determine if the camera is too close"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        depth32f = task.depth32f
        Dim maxval As Double
        Dim minLoc As cv.Point, maxLoc As cv.Point
        dst1 = task.inrange.depthMask
        depth32f.MinMaxLoc(minVal, maxval, minLoc, maxLoc, dst1)
        label1 = "Min Z = " + Format(minVal, "#0") + " Max Z = " + Format(maxval, "#0")
    End Sub
End Class






Public Class Depth_NoiseRemovalMask
    Inherits VBparent
    Public noise As Depth_TooClose
    Public flood As FloodFill_8Bit
    Public Sub New()
        initParent()
        flood = New FloodFill_8Bit()
        noise = New Depth_TooClose()

        label1 = "Mask of all inrange depth"
        label1 = "Solid inrange depth - noise removed"
        task.desc = "Use the 'Too Close' test to remove (some) noisy depth"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        noise.Run()
        dst1 = noise.dst1

        flood.src = dst1
        flood.Run()
        dst2 = flood.basics.dst2
    End Sub
End Class







Public Class Depth_Noise
    Inherits VBparent
    Dim noiseRemover As Depth_NoiseRemovalMask
    Public Sub New()
        initParent()
        noiseRemover = New Depth_NoiseRemovalMask()
        label1 = "Just the noise in the depth"
        label2 = "Solid depth with noise removed"
        task.desc = "Show depth with and without the depth noise from being too close."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        noiseRemover.Run()
        dst1 = noiseRemover.dst1
        dst2 = noiseRemover.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1.SetTo(0, dst2)
    End Sub
End Class








Public Class Depth_WorldXYZ
    Inherits VBparent
    Public depthUnitsMeters = False
    Public Sub New()
        initParent()
        label2 = "dst2 = pointcloud"
        task.desc = "Create 32-bit XYZ format from depth data (to slow to be useful.)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = task.depth32f
        If depthUnitsMeters = False Then input = (input * 0.001).ToMat
        Dim xy As New cv.Point3f
        dst2 = New cv.Mat(task.pointCloud.Size(), cv.MatType.CV_32FC3, 0)
        For xy.Y = 0 To dst2.Height - 1
            For xy.X = 0 To dst2.Width - 1
                xy.Z = input.Get(Of Single)(xy.Y, xy.X)
                If xy.Z <> 0 Then
                    Dim xyz = getWorldCoordinates(xy)
                    dst2.Set(Of cv.Point3f)(xy.Y, xy.X, xyz)
                End If
            Next
        Next
        If standalone Or task.intermediateReview = caller Then ocvb.trueText("OpenGL data prepared.")
    End Sub
End Class






Public Class Depth_WorldXYZ_MT
    Inherits VBparent
    Dim grid As Thread_Grid
    Public depthUnitsMeters = False
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        label2 = "dst2 = pointcloud"
        task.desc = "Create OpenGL point cloud from depth data (slow)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = task.depth32f
        grid.src = input
        grid.Run()

        dst2 = New cv.Mat(task.pointCloud.Size(), cv.MatType.CV_32FC3, 0)
        If depthUnitsMeters = False Then input = (input * 0.001).ToMat
        Dim multX = task.pointCloud.Width / input.Width
        Dim multY = task.pointCloud.Height / input.Height
        Parallel.ForEach(grid.roiList,
              Sub(roi)
                  Dim xy As New cv.Point3f
                  For y = roi.Y To roi.Y + roi.Height - 1
                      For x = roi.X To roi.X + roi.Width - 1
                          xy.X = x * multX
                          xy.Y = y * multY
                          xy.Z = input.Get(Of Single)(y, x)
                          If xy.Z <> 0 Then
                              Dim xyz = getWorldCoordinates(xy)
                              dst2.Set(Of cv.Point3f)(y, x, xyz)
                          End If
                      Next
                  Next
              End Sub)
        If standalone Or task.intermediateReview = caller Then ocvb.trueText("OpenGL data prepared.")
    End Sub
End Class







Public Class Depth_InRange
    Inherits VBparent
    Public depthMask As New cv.Mat
    Public noDepthMask As New cv.Mat
    Public minVal As Single
    Public maxVal As Single
    Public depth32f As New cv.Mat
    Public depth32fAfterMasking As Boolean
    Public Sub New()
        initParent()
        label1 = "Depth values that are in-range"
        label2 = "Depth values that are out of range (and < 8m)"
        task.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim min = If(minVal <> 0, minVal, task.minRangeSlider.Value)
        Dim max = If(minVal <> 0, maxVal, task.maxRangeSlider.Value)
        If min >= max Then max = min + 1
        depth32f = src
        If depth32f.Type <> cv.MatType.CV_32FC1 Then depth32f = task.depth32f
        cv.Cv2.InRange(depth32f, min, max, depthMask)
        cv.Cv2.BitwiseNot(depthMask, noDepthMask)
        dst1 = depth32f.Clone.SetTo(0, noDepthMask)
        If standalone Or depth32fAfterMasking Then dst2 = depth32f.Clone.SetTo(0, depthMask)
    End Sub
End Class







Public Class Depth_LowQualityMask
    Inherits VBparent
    Dim dilate As DilateErode_Basics
    Public Sub New()
        initParent()

        dilate = New DilateErode_Basics
        Dim ellipseRadio = findRadio("Dilate/Erode shape: Ellipse")
        ellipseRadio.Checked = True

        label2 = "Dilated zero depth - reduces flyout particles"
        task.desc = "Monitor motion in the mask where depth is zero"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        dst1 = task.inrange.noDepthMask

        dilate.src = dst1
        dilate.Run()
        dst2 = dilate.dst1
    End Sub
End Class









Public Class Depth_PunchDecreasing
    Inherits VBparent
    Public Increasing As Boolean
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Threshold in millimeters", 0, 1000, 8)
        End If
        task.desc = "Identify where depth is decreasing - coming toward the camera."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim depth32f = If(src.Type = cv.MatType.CV_32F, src, task.depth32f)
        Static lastDepth As cv.Mat = depth32f

        Dim mmThreshold = sliders.trackbar(0).Value
        If Increasing Then
            cv.Cv2.Subtract(depth32f, lastDepth, dst1)
        Else
            cv.Cv2.Subtract(lastDepth, depth32f, dst1)
        End If
        dst1 = dst1.Threshold(mmThreshold, 0, cv.ThresholdTypes.Tozero).Threshold(0, 255, cv.ThresholdTypes.Binary)
        lastDepth = depth32f.Clone
    End Sub
End Class





Public Class Depth_PunchIncreasing
    Inherits VBparent
    Public depth As Depth_PunchDecreasing
    Public Sub New()
        initParent()
        depth = New Depth_PunchDecreasing
        depth.Increasing = True
        task.desc = "Identify where depth is increasing - retreating from the camera."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        depth.src = src
        depth.Run()
        dst1 = depth.dst1
    End Sub
End Class






Public Class Depth_PunchBlob
    Inherits VBparent
    Dim depthDec As Depth_PunchDecreasing
    Dim depthInc As Depth_PunchDecreasing
    Dim contours As Contours_Basics
    Public Sub New()
        initParent()
        contours = New Contours_Basics
        Dim areaSlider = findSlider("Contour minimum area")
        areaSlider.Value = 5000

        Dim maxSlider = findSlider("InRange Max Depth (mm)")
        maxSlider.Value = 2000 ' must be close to the camera.

        depthInc = New Depth_PunchDecreasing
        task.desc = "Identify the punch with a rectangle around the largest blob"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        depthInc.src = src
        depthInc.Run()
        dst1 = depthInc.dst1

        contours.src = dst1
        contours.Run()
        dst2 = contours.dst2

        Static lastContoursCount As Integer
        Static punchCount As Integer
        Static showMessage As Integer
        If contours.contourlist.Count > 0 Then showMessage = 30

        If showMessage = 30 And lastContoursCount = 0 Then punchCount += 1
        lastContoursCount = contours.contourlist.Count
        label2 = CStr(punchCount) + " Punches Thrown"

        If showMessage Then
            ocvb.trueText("Punched!!!", 10, 100, 3)
            showMessage -= 1
        End If

        Static showWarningInfo As Integer
        If contours.contourlist.Count > 3 Then showWarningInfo = 100

        If showWarningInfo Then
            showWarningInfo -= 1
            ocvb.trueText("Too many contours!  Reduce the Max Depth.", 10, 130, 3)
        End If
    End Sub
End Class








Public Class Depth_SmoothSurfaces
    Inherits VBparent
    Public pcValid As Motion_MinMaxPointCloud
    Dim histX As Histogram_Basics
    Dim histY As Histogram_Basics
    Dim mats As Mat_4to1
    Public Sub New()
        initParent()

        mats = New Mat_4to1
        histX = New Histogram_Basics
        histY = New Histogram_Basics
        pcValid = New Motion_MinMaxPointCloud

        label1 = "1)HistX 2)HistY 3)backProject histX 4)backP histY"
        label2 = "Likely smooth surfaces"
        task.desc = "Find planes using the pointcloud X and Y differences"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        pcValid.Run()
        Dim mask = pcValid.dst1.Threshold(0, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)

        Dim split = pcValid.dst2.Split()
        Dim xDiff = New cv.Mat(dst2.Size, cv.MatType.CV_32FC1, 0)
        Dim yDiff = New cv.Mat(dst2.Size, cv.MatType.CV_32FC1, 0)

        Dim r1 = New cv.Rect(0, 0, dst1.Width - 1, dst1.Height - 1)
        Dim r2 = New cv.Rect(1, 1, dst1.Width - 1, dst1.Height - 1)

        cv.Cv2.Subtract(split(0)(r1), split(0)(r2), xDiff(r1))
        cv.Cv2.Subtract(split(1)(r2), split(1)(r1), yDiff(r1))

        xDiff.SetTo(0, mask)
        yDiff.SetTo(0, mask)

        histX.src = xDiff.ConvertScaleAbs(255)
        histX.Run()
        mats.mat(0) = histX.dst1

        histY.src = yDiff.ConvertScaleAbs(255)
        histY.Run()
        mats.mat(1) = histY.dst1

        Dim ranges() = New cv.Rangef() {New cv.Rangef(1, 2)}
        Dim mat() As cv.Mat = {histX.src}
        Dim bins() = {0}
        cv.Cv2.CalcBackProject(mat, bins, histX.histogram, mats.mat(2), ranges)

        mat(0) = histY.src
        cv.Cv2.CalcBackProject(mat, bins, histX.histogram, mats.mat(3), ranges)

        mats.Run()
        dst1 = mats.dst1

        cv.Cv2.BitwiseOr(mats.mat(2), mats.mat(3), dst2)
    End Sub
End Class






Public Class Depth_TestMinFunction
    Inherits VBparent
    Public Sub New()
        initParent()
        label1 = "32-bit format brightened version of dst2"
        label2 = "32-bit format stable depth (if camera is stable)"
        task.desc = "Test min function with depth data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim depth32f As New cv.Mat
        task.depth16.ConvertTo(depth32f, cv.MatType.CV_32F)
        If ocvb.frameCount Mod 30 = 0 Then dst2 = depth32f
        cv.Cv2.Min(dst2, depth32f, dst2)
        dst1 = dst2.ConvertScaleAbs(180)
        dst1 += 64
    End Sub
End Class








' https://stackoverflow.com/questions/19093728/rotate-image-around-x-y-z-axis-in-opencv
' https://stackoverflow.com/questions/7019407/translating-and-rotating-an-image-in-3d-using-opencv
Public Class Depth_PointCloud_IMU
    Inherits VBparent
    Public Mask As New cv.Mat
    Public imu As IMU_GVector
    Public gMatrix(,) As Single
    Public Sub New()
        initParent()

        imu = New IMU_GVector

        task.desc = "Rotate the PointCloud around the X-axis and the Z-axis using the gravity vector from the IMU."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static xCheckbox = findCheckBox("Rotate pointcloud around X-axis using gravity vector angleZ")
        Static zCheckbox = findCheckBox("Rotate pointcloud around Z-axis using gravity vector angleX")

        Dim input = src
        If input.Type <> cv.MatType.CV_32FC3 Then input = task.pointCloud.Clone

        imu.Run()
        Dim cx As Double = 1, sx As Double = 0, cy As Double = 1, sy As Double = 0, cz As Double = 1, sz As Double = 0
        '[cos(a) -sin(a)    0]
        '[sin(a)  cos(a)    0]
        '[0       0         1] rotate the point cloud around
        '  the x-axis.
        If xCheckbox.Checked Then
            cz = Math.Cos(ocvb.angleZ)
            sz = Math.Sin(ocvb.angleZ)
        End If

        '[1       0         0      ] rotate the point cloud around the z-axis.
        '[0       cos(a)    -sin(a)]
        '[0       sin(a)    cos(a) ]
        If zCheckbox.Checked Then
            cx = Math.Cos(ocvb.angleX)
            sx = Math.Sin(ocvb.angleX)
        End If

        '[cx -sx    0]  [1  0   0 ] 
        '[sx  cx    0]  [0  cz -sz]
        '[0   0     1]  [0  sz  cz]
        Dim gM(,) As Single = {{cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                               {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                               {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz}}

        Static angleYslider = findSlider("Amount to rotate pointcloud around Y-axis (degrees)")
        Dim angleY = angleYslider.value
        '[cos(a) 0 -sin(a)]
        '[0      1       0]
        '[sin(a) 0   cos(a] rotate the point cloud around the y-axis.
        cy = Math.Cos(angleY * cv.Cv2.PI / 180)
        sy = Math.Sin(angleY * cv.Cv2.PI / 180)
        gM = {{gM(0, 0) * cy + gM(0, 1) * 0 + gM(0, 2) * sy}, {gM(0, 0) * 0 + gM(0, 1) * 1 + gM(0, 2) * 0}, {gM(0, 0) * -sy + gM(0, 1) * 0 + gM(0, 2) * cy},
              {gM(1, 0) * cy + gM(1, 1) * 0 + gM(1, 2) * sy}, {gM(1, 0) * 0 + gM(1, 1) * 1 + gM(1, 2) * 0}, {gM(1, 0) * -sy + gM(1, 1) * 0 + gM(1, 2) * cy},
              {gM(2, 0) * cy + gM(2, 1) * 0 + gM(2, 2) * sy}, {gM(2, 0) * 0 + gM(2, 1) * 1 + gM(2, 2) * 0}, {gM(2, 0) * -sy + gM(2, 1) * 0 + gM(2, 2) * cy}}

        gMatrix = gM
        If xCheckbox.Checked Or zCheckbox.Checked Or angleY <> 0 Then
            Dim gMat = New cv.Mat(3, 3, cv.MatType.CV_32F, gMatrix)
            Dim gInput = input.Reshape(1, input.Rows * input.Cols)
            Dim gOutput = (gInput * gMat).ToMat
            dst1 = gOutput.Reshape(3, input.Rows)
            label1 = "dst1 = pointcloud after rotation"
        Else
            dst1 = input.Clone
            label1 = "dst1 = pointcloud without rotation"
        End If

        ocvb.pixelsPerMeter = dst1.Width / ocvb.maxZ
    End Sub
End Class






Public Class Depth_SmoothAverage
    Inherits VBparent
    Dim dMin As Depth_SmoothMin
    Dim dMax As Depth_SmoothMax
    Dim colorize As Depth_ColorizerFastFade_CPP
    Public Sub New()
        initParent()

        colorize = New Depth_ColorizerFastFade_CPP
        dMin = New Depth_SmoothMin
        dMax = New Depth_SmoothMax

        label1 = "InRange average depth (low quality depth removed)"
        label2 = "32-bit format average stable depth"
        task.desc = "To reduce z-Jitter, use the average depth value at each pixel as long as the camera is stable"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        dMax.src = task.depth32f
        dMax.Run()
        dst1 = dMax.dMin.dst1

        cv.Cv2.AddWeighted(dMax.dMin.stableMin, 0.5, dMax.stableMax, 0.5, 0, dst2)
    End Sub
End Class







Public Class Depth_SmoothMin
    Inherits VBparent
    Public stableMin As cv.Mat
    Public motion As Motion_Basics
    Dim colorize As Depth_ColorizerFastFade_CPP
    Public Sub New()
        initParent()

        colorize = New Depth_ColorizerFastFade_CPP
        motion = New Motion_Basics

        label1 = "InRange depth with low quality depth removed."
        label2 = "Motion in the RGB image. Depth updated in rectangle."
        task.desc = "To reduce z-Jitter, use the closest depth value at each pixel as long as the camera is stable"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = task.depth32f

        motion.src = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        motion.Run()
        dst2 = If(motion.dst2.Channels = 1, motion.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR), motion.dst2.Clone)

        If motion.resetAll Or stableMin Is Nothing Then
            stableMin = input.Clone
        Else
            For Each rect In motion.intersect.enclosingRects
                If rect.Width And rect.Height Then input(rect).CopyTo(stableMin(rect))
                cv.Cv2.Min(input, stableMin, stableMin)
            Next
        End If

        If motion.intersect.inputRects.Count > 0 Then
            If dst2.Channels = 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            For Each r In motion.intersect.inputRects
                dst2.Rectangle(r, cv.Scalar.Yellow, 2)
            Next
            For Each rect In motion.intersect.enclosingRects
                dst2.Rectangle(rect, cv.Scalar.Red, 2)
            Next
        End If

        colorize.src = stableMin
        colorize.Run()
        dst1 = colorize.dst1
    End Sub
End Class






Public Class Depth_SmoothMax
    Inherits VBparent
    Public dMin As Depth_SmoothMin
    Dim colorize As Depth_ColorizerFastFade_CPP
    Public stableMax As cv.Mat
    Public Sub New()
        initParent()

        colorize = New Depth_ColorizerFastFade_CPP
        dMin = New Depth_SmoothMin

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Use SmoothMin to find zero depth pixels"
        End If

        label1 = "InRange depth with low quality depth removed."
        label2 = "32-bit format StableMax"
        task.desc = "To reduce z-Jitter, use the farthest depth value at each pixel as long as the camera is stable"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        dMin.src = src
        If dMin.src.Type <> cv.MatType.CV_32FC1 Then dMin.src = task.depth32f

        dMin.Run()

        If dMin.motion.resetAll Or stableMax Is Nothing Then
            stableMax = dMin.src.Clone
        Else
            For Each rect In dMin.motion.intersect.enclosingRects
                If rect.Width And rect.Height Then dMin.src(rect).CopyTo(stableMax(rect))
                cv.Cv2.Max(dMin.src, stableMax, stableMax)
            Next

            Static dMinCheck = findCheckBox("Use SmoothMin to find zero depth pixels")
            If dMinCheck.checked Then
                Dim zeroMask As New cv.Mat
                dMin.stableMin.Threshold(0, 255, cv.ThresholdTypes.BinaryInv).ConvertTo(zeroMask, cv.MatType.CV_8U)
                stableMax.SetTo(0, zeroMask)
            End If
        End If

        colorize.src = stableMax
        colorize.Run()
        dst1 = colorize.dst1
        dst2 = stableMax
    End Sub
End Class









Public Class Depth_Averaging
    Inherits VBparent
    Public avg As Math_ImageAverage
    Public colorize As Depth_Colorizer_CPP
    Public Sub New()
        initParent()

        avg = New Math_ImageAverage()
        colorize = New Depth_Colorizer_CPP()

        label2 = "32-bit format depth data"
        task.desc = "Take the average depth at each pixel but eliminate any pixels that had zero depth."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        avg.src = src
        If avg.src.Type <> cv.MatType.CV_32F Then avg.src = task.depth32f
        avg.Run()

        dst2 = avg.dst1
        colorize.src = dst2
        colorize.Run()
        dst1 = colorize.dst1
    End Sub
End Class







Public Class Depth_SmoothMinMax
    Inherits VBparent
    Dim colorize As Depth_ColorizerFastFade_CPP
    Public dMin As Depth_SmoothMin
    Public dMax As Depth_SmoothMax
    Public resetAll As Boolean
    Public Sub New()
        initParent()
        colorize = New Depth_ColorizerFastFade_CPP
        dMin = New Depth_SmoothMin
        dMax = New Depth_SmoothMax
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "Use farthest distance"
            radio.check(1).Text = "Use closest distance"
            radio.check(2).Text = "Use unchanged depth input"
            radio.check(1).Checked = True
        End If

        label1 = "Depth map colorized"
        label2 = "32-bit StableDepth"
        task.desc = "To reduce z-Jitter, use the closest or farthest point as long as the camera is stable"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = task.depth32f

        Dim radioVal As Integer
        Static frm As OptionsRadioButtons = findfrm("Depth_SmoothMinMax Radio Options")
        For radioVal = 0 To frm.check.Count - 1
            If frm.check(radioVal).Checked Then Exit For
        Next

        Static saveRadioVal = -1
        If radioVal <> saveRadioVal Then
            saveRadioVal = radioVal
            dst2 = task.depth32f
            resetAll = True
        Else
            Select Case saveRadioVal
                Case 0
                    dMax.src = input
                    dMax.Run()
                    dst2 = dMax.stableMax
                    dst1 = dMax.dst1
                    resetAll = dMax.dMin.motion.resetAll
                Case 1
                    dMin.src = input
                    dMin.Run()
                    dst2 = dMin.stableMin
                    dst1 = dMin.dst1
                    resetAll = dMin.motion.resetAll
                Case 2
                    dst2 = task.depth32f
                    colorize.src = dst2
                    colorize.Run()
                    dst1 = colorize.dst1
                    resetAll = True
            End Select
        End If
    End Sub
End Class








Public Class Depth_AveragingStable
    Inherits VBparent
    Dim dAvg As Depth_Averaging
    Dim extrema As Depth_SmoothMinMax
    Public Sub New()
        initParent()
        dAvg = New Depth_Averaging
        extrema = New Depth_SmoothMinMax
        Dim minMaxRadio = findRadio("Use farthest distance")
        minMaxRadio.Checked = True
        task.desc = "Use Depth_SmoothMax to remove the artifacts from the Depth_Averaging"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        extrema.src = src
        If extrema.src.Type <> cv.MatType.CV_32F Then extrema.src = task.depth32f
        extrema.Run()

        Static noAvgRadio = findRadio("Use unchanged depth input")
        If noAvgRadio.checked = False Then
            dAvg.src = extrema.dst2
            dAvg.Run()
            dst1 = dAvg.dst1
            dst2 = dAvg.dst2
        Else
            dst1 = extrema.dst1
            dst2 = extrema.dst2
        End If
    End Sub
End Class








Public Class Depth_Fusion
    Inherits VBparent
    Dim dMax As Depth_SmoothMax
    Public Sub New()
        initParent()
        dMax = New Depth_SmoothMax

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Number of frames to fuse", 1, 300, 5)
        End If

        task.desc = "Fuse the depth from the previous x frames."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim input = src
        If input.Type <> cv.MatType.CV_32FC1 Then input = task.depth32f

        Static fuseSlider = findSlider("Number of frames to fuse")
        Dim fuseCount = fuseSlider.value

        Static saveFuseCount = fuseCount
        Static fuseFrames As New List(Of cv.Mat)
        If saveFuseCount <> fuseCount Then
            fuseFrames = New List(Of cv.Mat)
            saveFuseCount = fuseCount
        End If

        fuseFrames.Add(input.Clone)
        If fuseFrames.Count > fuseCount Then fuseFrames.RemoveAt(0)

        dst1 = fuseFrames(0).Clone
        For i = 1 To fuseFrames.Count - 1
            cv.Cv2.Max(fuseFrames(i), dst1, dst1)
        Next
    End Sub
End Class









Public Class Depth_Dilate
    Inherits VBparent
    Dim dilate As DilateErode_Basics
    Public Sub New()
        initParent()
        dilate = New DilateErode_Basics
        task.desc = "Dilate the depth data to fill holes."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        dilate.src = task.depth32f
        dilate.Run()
        dst1 = dilate.dst1
    End Sub
End Class






Public Class Depth_Foreground
    Inherits VBparent
    Public blobLocation As New List(Of cv.Point)
    Public maxIndex As Integer
    Public Sub New()
        initParent()
        task.maxRangeSlider.Value = 1500

        task.desc = "Use depth to find an object in the foreground.  Use InRange Min Depth to define foreground"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        Dim tmp As cv.Mat = task.inrange.depthMask.Clone
        ' find the largest blob and use that define that to be the foreground object.
        Dim blobSize As New List(Of Integer)
        blobLocation.clear
        For y = 0 To tmp.Height - 1
            For x = 0 To tmp.Width - 1
                Dim nextByte = tmp.Get(Of Byte)(y, x)
                If nextByte <> 0 Then
                    Dim count = tmp.FloodFill(New cv.Point(x, y), 0)
                    If count > 10 Then
                        blobSize.Add(count)
                        blobLocation.Add(New cv.Point(x, y))
                    End If
                End If
            Next
        Next
        Dim maxBlob As Integer
        maxIndex = -1
        For i = 0 To blobSize.Count - 1
            If maxBlob < blobSize.Item(i) Then
                maxBlob = blobSize.Item(i)
                maxIndex = i
            End If
        Next
        dst1 = task.inrange.depthMask.clone
    End Sub
End Class







Public Class Depth_ForegroundHead
    Inherits VBparent
    Dim fgnd As Depth_Foreground
    Public kalman As Kalman_Basics
    Public trustedRect As cv.Rect
    Public trustworthy As Boolean
    Public Sub New()
        initParent()
        fgnd = New Depth_Foreground
        kalman = New Kalman_Basics()

        label1 = "Blue is current, red is kalman, green is trusted"
        task.desc = "Use Depth_ForeGround to find the foreground blob.  Then find the probable head of the person in front of the camera."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        fgnd.src = src
        fgnd.Run()

        trustworthy = False
        If fgnd.dst1.CountNonZero() Then
            Dim rectSize = 50
            If src.Width > 1000 Then rectSize = 250
            Dim xx = fgnd.blobLocation.Item(fgnd.maxIndex).X - rectSize / 2
            Dim yy = fgnd.blobLocation.Item(fgnd.maxIndex).Y
            If xx < 0 Then xx = 0
            If xx + rectSize / 2 > src.Width Then xx = src.Width - rectSize
            dst1 = fgnd.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            kalman.kInput = {xx, yy, rectSize, rectSize}
            kalman.Run()
            Dim nextRect = New cv.Rect(xx, yy, rectSize, rectSize)
            Dim kRect = New cv.Rect(kalman.kOutput(0), kalman.kOutput(1), kalman.kOutput(2), kalman.kOutput(3))
            dst1.Rectangle(kRect, cv.Scalar.Red, 2)
            dst1.Rectangle(nextRect, cv.Scalar.Blue, 2)
            If Math.Abs(kRect.X - nextRect.X) < rectSize / 4 And Math.Abs(kRect.Y - nextRect.Y) < rectSize / 4 Then
                trustedRect = validateRect(kRect)
                trustworthy = True
                dst1.Rectangle(trustedRect, cv.Scalar.Green, 5)
            End If
        End If
    End Sub
End Class

