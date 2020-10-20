Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Depth_Median
    Inherits VBparent
    Dim median As Math_Median_CDF
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        median = New Math_Median_CDF(ocvb)
        median.src = New cv.Mat
        median.rangeMax = 10000
        median.rangeMin = 1 ' ignore depth of zero as it is not known.
        ocvb.desc = "Divide the depth image ahead and behind the median."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        median.src = getDepth32f(ocvb)
        median.Run(ocvb)

        Dim mask As cv.Mat
        mask = median.src.LessThan(median.medianVal)
        ocvb.RGBDepth.CopyTo(dst1, mask)

        Dim zeroMask = median.src.Equals(0)
        cv.Cv2.ConvertScaleAbs(zeroMask, zeroMask.ToMat)
        dst1.SetTo(0, zeroMask)

        label1 = "Median Depth < " + Format(median.medianVal, "#0.0")

        cv.Cv2.BitwiseNot(mask, mask)
        dst2.SetTo(0)
        ocvb.RGBDepth.CopyTo(dst2, mask)
        dst2.SetTo(0, zeroMask)
        label2 = "Median Depth > " + Format(median.medianVal, "#0.0")
    End Sub
End Class




Public Class Depth_Flatland
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Region Count", 1, 250, 10)

        label2 = "Grayscale version"
        ocvb.desc = "Attempt to stabilize the depth image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim reductionFactor = sliders.trackbar(0).Maximum - sliders.trackbar(0).Value
        dst1 = ocvb.RGBDepth / reductionFactor
        dst1 *= reductionFactor
        dst2 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class



Public Class Depth_FirstLastDistance
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Monitor the first and last depth distances"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim depth32f = getDepth32f(ocvb)
        Dim mask = depth32f.Threshold(1, 20000, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim minVal As Double, maxVal As Double
        Dim minPt As cv.Point, maxPt As cv.Point
        cv.Cv2.MinMaxLoc(depth32f, minVal, maxVal, minPt, maxPt, mask)
        ocvb.RGBDepth.CopyTo(dst1)
        ocvb.RGBDepth.CopyTo(dst2)
        label1 = "Min Depth " + CStr(minVal) + " mm"
        dst1.Circle(minPt, 10, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        label2 = "Max Depth " + CStr(maxVal) + " mm"
        dst2.Circle(maxPt, 10, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
    End Sub
End Class





Public Class Depth_HolesRect
    Inherits VBparent
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "shadowRect Min Size", 1, 20000, 2000)

        shadow = New Depth_Holes(ocvb)

        ocvb.desc = "Identify the minimum rectangles of contours of the depth shadow"
    End Sub

    Public Sub Run(ocvb As VBocvb)
        shadow.Run(ocvb)

        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(shadow.borderMask, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        Dim minEllipse(contours.Length - 1) As cv.RotatedRect
        For i = 0 To contours.Length - 1
            Dim minRect = cv.Cv2.MinAreaRect(contours(i))
            If minRect.Size.Width * minRect.Size.Height > sliders.trackbar(0).Value Then
                Dim nextColor = New cv.Scalar(ocvb.vecColors(i Mod 255).Item0, ocvb.vecColors(i Mod 255).Item1, ocvb.vecColors(i Mod 255).Item2)
                drawRotatedRectangle(minRect, dst1, nextColor)
                If contours(i).Length >= 5 Then
                    minEllipse(i) = cv.Cv2.FitEllipse(contours(i))
                End If
            End If
        Next
        cv.Cv2.AddWeighted(dst1, 0.5, ocvb.RGBDepth, 0.5, 0, dst1)
    End Sub
End Class





Public Class Depth_Foreground
    Inherits VBparent
    Public trim As Depth_InRange
    Public kalman As Kalman_Basics
    Public trustedRect As cv.Rect
    Public trustworthy As Boolean
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        trim = New Depth_InRange(ocvb)

        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.input(4 - 1) ' cv.rect...
        hideForm("Kalman_Basics CheckBox Options")

        label1 = "Blue is current, red is kalman, green is trusted"
        ocvb.desc = "Demonstrate the use of mean shift algorithm.  Use depth to find the top of the head and then meanshift to the face."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        trim.src = getDepth32f(ocvb)
        trim.Run(ocvb)
        dst1 = trim.dst1.ConvertScaleAbs(255)
        Dim tmp = trim.dst1.ConvertScaleAbs(255)
        ' find the largest blob and use that as the body.  Head is highest in the image.
        Dim blobSize As New List(Of Integer)
        Dim blobLocation As New List(Of cv.Point)
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
        Dim maxIndex As Integer = -1
        For i = 0 To blobSize.Count - 1
            If maxBlob < blobSize.Item(i) Then
                maxBlob = blobSize.Item(i)
                maxIndex = i
            End If
        Next

        trustworthy = False
        If maxIndex >= 0 Then
            Dim rectSize = 50
            If src.Width > 1000 Then rectSize = 250
            Dim xx = blobLocation.Item(maxIndex).X - rectSize / 2
            Dim yy = blobLocation.Item(maxIndex).Y
            If xx < 0 Then xx = 0
            If xx + rectSize / 2 > src.Width Then xx = src.Width - rectSize
            dst1 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            kalman.input = {xx, yy, rectSize, rectSize}
            kalman.Run(ocvb)
            Dim nextRect = New cv.Rect(xx, yy, rectSize, rectSize)
            Dim kRect = New cv.Rect(kalman.output(0), kalman.output(1), kalman.output(2), kalman.output(3))
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






Public Class Depth_FlatData
    Inherits VBparent
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        shadow = New Depth_Holes(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "FlatData Region Count", 1, 250, 200)

        label1 = "Reduced resolution RGBDepth"
        ocvb.desc = "Attempt to stabilize the depth image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        shadow.Run(ocvb) ' get where depth is zero

        Dim mask As New cv.Mat
        Dim gray As New cv.Mat
        Dim gray8u As New cv.Mat

        cv.Cv2.BitwiseNot(shadow.holeMask, mask)
        gray = getDepth32f(ocvb).Normalize(0, 255, cv.NormTypes.MinMax, -1, mask)
        gray.ConvertTo(gray8u, cv.MatType.CV_8U)

        Dim reductionFactor = sliders.trackbar(0).Maximum - sliders.trackbar(0).Value
        gray8u = gray8u / reductionFactor
        gray8u *= reductionFactor

        dst1 = gray8u.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class





Public Class Depth_Zero
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Depth_Zero Max Depth", 200, 10000, 4000)

        label2 = "Mask for depth zero or out-of-range"
        ocvb.desc = "Create a mask for zero depth - depth shadow and depth out-of-range"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        cv.Cv2.InRange(getDepth32f(ocvb), 1, sliders.trackbar(0).Value, dst2)
        dst1.SetTo(0)
        ocvb.RGBDepth.CopyTo(dst1, dst2)
        cv.Cv2.BitwiseNot(dst2, dst2)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        grid = New Thread_Grid(ocvb)
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 64
        gridHeightSlider.Value = 40

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "MeanStdev Max Depth Range", 1, 20000, 3500)
        sliders.setupTrackBar(1, "MeanStdev Frame Series", 1, 100, 5)
        ocvb.desc = "Collect a time series of depth and measure where the stdev is unstable.  Plan is to avoid depth where unstable."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        grid.Run(ocvb)
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
        Dim depth32f = getDepth32f(ocvb)
        cv.Cv2.InRange(depth32f, 1, maxDepth, tmp16)
        cv.Cv2.ConvertScaleAbs(tmp16, mask)
        Dim outOfRangeMask As New cv.Mat
        cv.Cv2.BitwiseNot(mask, outOfRangeMask)

        Dim minVal As Double, maxVal As Double
        Dim minPt As cv.Point, maxPt As cv.Point
        cv.Cv2.MinMaxLoc(depth32f, minVal, maxVal, minPt, maxPt, mask)

        Dim meanIndex = ocvb.frameCount Mod meanCount
        Dim meanValues As New cv.Mat(grid.roiList.Count - 1, 1, cv.MatType.CV_32F)
        Dim stdValues As New cv.Mat(grid.roiList.Count - 1, 1, cv.MatType.CV_32F)
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim mean As Single = 0, stdev As Single = 0
            cv.Cv2.MeanStdDev(depth32f(roi), mean, stdev, mask(roi))
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
    Dim shadow As Depth_Holes
    Dim plot1 As Plot_OverTime
    Dim plot2 As Plot_OverTime
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        shadow = New Depth_Holes(ocvb)

        plot1 = New Plot_OverTime(ocvb)
        plot1.dst1 = dst1
        plot1.maxScale = 2000
        plot1.plotCount = 1

        plot2 = New Plot_OverTime(ocvb)
        plot2.dst1 = dst2
        plot2.maxScale = 1000
        plot2.plotCount = 1

        ocvb.desc = "Plot the mean and stdev of the depth image"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        shadow.Run(ocvb)

        Dim mean As Single = 0, stdev As Single = 0
        Dim mask As New cv.Mat
        cv.Cv2.BitwiseNot(shadow.holeMask, mask)
        cv.Cv2.MeanStdDev(getDepth32f(ocvb), mean, stdev, mask)

        If mean > plot1.maxScale Then plot1.maxScale = mean + 1000 - (mean + 1000) Mod 1000
        If stdev > plot2.maxScale Then plot2.maxScale = stdev + 1000 - (stdev + 1000) Mod 1000

        plot1.plotData = New cv.Scalar(mean, 0, 0)
        plot1.Run(ocvb)
        dst1 = plot1.dst1

        plot2.plotData = New cv.Scalar(stdev, 0, 0)
        plot2.Run(ocvb)
        dst2 = plot2.dst1

        label1 = "Plot of mean depth = " + Format(mean, "#0.0")
        label2 = "Plot of depth stdev = " + Format(stdev, "#0.0")
    End Sub
End Class




Public Class Depth_Uncertainty
    Inherits VBparent
    Dim retina As Retina_Basics_CPP
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        retina = New Retina_Basics_CPP(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Uncertainty threshold", 1, 255, 100)

        label2 = "Mask of areas with unstable depth"
        ocvb.desc = "Use the bio-inspired retina algorithm to determine depth uncertainty."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        retina.src = ocvb.RGBDepth
        retina.Run(ocvb)
        dst1 = retina.dst1
        dst2 = retina.dst2.Threshold(sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Depth_Palette
    Inherits VBparent
    Public trim As Depth_InRange
    Dim customColorMap As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        trim = New Depth_InRange(ocvb)
        trim.sliders.trackbar(1).Value = 5000

        customColorMap = colorTransition(cv.Scalar.Blue, cv.Scalar.Yellow, 256)
        ocvb.desc = "Use a palette to display depth from the raw depth data."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        trim.src = getDepth32f(ocvb)
        trim.Run(ocvb)
        Dim minDepth = trim.sliders.trackbar(0).Value
        Dim maxDepth = trim.sliders.trackbar(1).Value

        Dim depthNorm = (trim.depth32f * 255 / (maxDepth - minDepth)).ToMat ' do the normalize manually to use the min and max Depth (more stable)
        depthNorm.ConvertTo(depthNorm, cv.MatType.CV_8U)
        dst1 = Palette_Custom_Apply(depthNorm.CvtColor(cv.ColorConversionCodes.GRAY2BGR), customColorMap).SetTo(0, trim.zeroMask)
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
    Public Function getDepth32f(ocvb As VBocvb) As cv.Mat
        Dim depth32f As New cv.Mat
        ocvb.depth16.ConvertTo(depth32f, cv.MatType.CV_32F)
        If depth32f.Size <> ocvb.color.Size Then Return depth32f.Resize(ocvb.color.Size())
        Return depth32f
    End Function
End Module




Public Class Depth_Colorizer_CPP
    Inherits VBparent
    Dim dcPtr As IntPtr
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        dcPtr = Depth_Colorizer_Open()
        ocvb.desc = "Display Depth image using C++ instead of VB.Net"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Type <> cv.MatType.CV_32F Then
            If standalone Then src = getDepth32f(ocvb) Else dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
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





Public Class Depth_ManualTrim
    Inherits VBparent
    Public Mask As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Min Depth", 200, 1000, 200)
        sliders.setupTrackBar(1, "Max Depth", 200, 10000, 1400)
        ocvb.desc = "Manually show depth with varying min and max depths."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If sliders.trackbar(0).Value >= sliders.trackbar(1).Value Then sliders.trackbar(1).Value = sliders.trackbar(0).Value + 1
        Dim minDepth = sliders.trackbar(0).Value
        Dim maxDepth = sliders.trackbar(1).Value
        dst1 = getDepth32f(ocvb)
        Mask = dst1.Threshold(maxDepth, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()

        Dim maskMin = dst1.Threshold(minDepth, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        cv.Cv2.BitwiseAnd(Mask, maskMin, Mask)

        If standalone Then
            ocvb.RGBDepth.CopyTo(dst1, Mask)
        Else
            Dim notMask As New cv.Mat
            cv.Cv2.BitwiseNot(Mask, notMask)
            dst1.SetTo(0, notMask)
        End If
    End Sub
End Class






Public Class Depth_ColorizerFastFade_CPP
    Inherits VBparent
    Public trim As Depth_InRange
    Dim dcPtr As IntPtr
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        dcPtr = Depth_Colorizer2_Open()

        trim = New Depth_InRange(ocvb)

        label2 = "Mask from Depth_InRange"
        ocvb.desc = "Display depth data with inrange trim.  Higher contrast than others - yellow to blue always present."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        trim.src = getDepth32f(ocvb)
        trim.Run(ocvb)
        dst2 = trim.Mask

        If standalone Then src = trim.depth32f Else dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        Dim depthData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_Colorizer2_Run(dcPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, trim.maxDepth)
        handleSrc.Free()

        If imagePtr <> 0 Then dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)
    End Sub
    Public Sub Close()
        Depth_Colorizer2_Close(dcPtr)
    End Sub
End Class




' this algorithm is only intended to show how the depth can be colorized.  It is very slow.  Use the C++ version of this code nearby.
Public Class Depth_ColorizerVB
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Colorize depth manually."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim src = getDepth32f(ocvb)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Min Depth", 0, 1000, 0)
        sliders.setupTrackBar(1, "Max Depth", 1001, 10000, 4000)

        grid = New Thread_Grid(ocvb)

        ocvb.desc = "Colorize depth manually with multi-threading."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        grid.Run(ocvb)

        If standalone Then src = getDepth32f(ocvb)
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
            Parallel.ForEach(Of cv.Rect)(grid.roiList,
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Min Depth", 100, 1000, 100)
        sliders.setupTrackBar(1, "Max Depth", 1001, 10000, 4000)

        grid = New Thread_Grid(ocvb)

        ocvb.desc = "Colorize normally uses CDF to stabilize the colors.  Just using sliders here - stabilized but not optimal range."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        grid.Run(ocvb)

        If standalone Then src = getDepth32f(ocvb)
        Dim nearColor = New Single() {0, 1, 1}
        Dim farColor = New Single() {1, 0, 0}

        Dim minDepth = sliders.trackbar(0).Value
        Dim maxDepth = sliders.trackbar(1).Value

        Parallel.ForEach(Of cv.Rect)(grid.roiList,
         Sub(roi)
             Dim depth = src(roi)
             Dim stride = depth.Width * 3
             Dim rgbdata(stride * depth.Height) As Byte
             For y = 0 To depth.Rows - 1
                 For x = 0 To depth.Cols - 1
                     Dim pixel = depth.Get(Of Single)(y, x)
                     If pixel > minDepth And pixel <= maxDepth Then
                         Dim t = (pixel - minDepth) / (maxDepth - minDepth)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        grid = New Thread_Grid(ocvb)

        label1 = "Red is min distance, blue is max distance"
        ocvb.desc = "Find min and max depth in each segment."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        grid.Run(ocvb)
        Dim depth32f = getDepth32f(ocvb)

        Dim mask = depth32f.Threshold(1, 5000, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8UC1)

        If standalone Then
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
            cv.Cv2.MinMaxLoc(depth32f(roi), minVal, maxVal, minPt, maxPt, mask(roi))
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        grid = New Thread_Grid(ocvb)
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 128
        gridHeightSlider.Value = 90
        grid.Run(ocvb)

        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.input(grid.roiList.Count * 4 - 1)

        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.input(4 - 1)

        label1 = "Red is min distance, blue is max distance"
        ocvb.desc = "Find minimum depth in each segment."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        grid.Run(ocvb)
        Dim depth32f = getDepth32f(ocvb)
        Dim mask = depth32f.Threshold(1, 5000, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8UC1)

        If grid.roiList.Count * 4 <> kalman.input.Length Then
            If kalman IsNot Nothing Then kalman.Dispose()
            kalman = New Kalman_Basics(ocvb)
            ReDim kalman.input(grid.roiList.Count * 4 - 1)
        End If

        dst1 = src.Clone()
        dst1.SetTo(cv.Scalar.White, grid.gridMask)

        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim minVal As Double, maxVal As Double
            Dim minPt As cv.Point, maxPt As cv.Point
            cv.Cv2.MinMaxLoc(depth32f(roi), minVal, maxVal, minPt, maxPt, mask(roi))
            If minPt.X < 0 Or minPt.Y < 0 Then minPt = New cv.Point2f(0, 0)
            kalman.input(i * 4) = minPt.X
            kalman.input(i * 4 + 1) = minPt.Y
            kalman.input(i * 4 + 2) = maxPt.X
            kalman.input(i * 4 + 3) = maxPt.Y
        End Sub)

        kalman.Run(ocvb)

        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, src.Width, src.Height))
        Dim radius = 5
        For i = 0 To grid.roiList.Count - 1
            Dim roi = grid.roiList(i)
            Dim ptmin = New cv.Point2f(kalman.output(i * 4) + roi.X, kalman.output(i * 4 + 1) + roi.Y)
            Dim ptmax = New cv.Point2f(kalman.output(i * 4 + 2) + roi.X, kalman.output(i * 4 + 3) + roi.Y)
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
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        Palette = New Palette_Basics(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Depth ColorMap Alpha X100", 1, 100, 5)
        sliders.setupTrackBar(1, "Depth ColorMap Beta", 1, 100, 3)

        ocvb.desc = "Display the depth as a color map"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim alpha = sliders.trackbar(0).Value / 100
        Dim beta = sliders.trackbar(1).Value
        cv.Cv2.ConvertScaleAbs(getDepth32f(ocvb), Palette.src, alpha, beta)
        Palette.Run(ocvb)
        dst1 = Palette.dst1
        dst2 = Palette.dst2
    End Sub
End Class






Public Class Depth_Stable
    Inherits VBparent
    Public mog As BGSubtract_Basics_CPP
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        mog = New BGSubtract_Basics_CPP(ocvb)

        label2 = "Stable (non-zero) Depth"
        ocvb.desc = "Collect X frames, compute stable depth using the RGB and Depth image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If standalone Then src = ocvb.RGBDepth
        mog.src = src
        mog.Run(ocvb)
        dst1 = mog.dst1
        cv.Cv2.BitwiseNot(mog.dst1, dst2)
        label1 = "Unstable Depth" + " using " + mog.radio.check(mog.currMethod).Text + " method"
        Dim zeroDepth = getDepth32f(ocvb).Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(1)
        dst2.SetTo(0, zeroDepth)
    End Sub
End Class





Public Class Depth_Stabilizer
    Inherits VBparent
    Public stable As Depth_Stable
    Public mean As Mean_Basics
    Public colorize As Depth_Colorizer_CPP
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        mean = New Mean_Basics(ocvb)
        colorize = New Depth_Colorizer_CPP(ocvb)
        stable = New Depth_Stable(ocvb)

        ocvb.desc = "Use the mask of stable depth (using RGBDepth) to stabilize the depth at any individual point."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        stable.src = src
        stable.Run(ocvb)

        mean.src = getDepth32f(ocvb)
        mean.src.SetTo(0, stable.dst1)
        mean.Run(ocvb)

        If standalone Then
            colorize.src = mean.dst1.Threshold(256 * 256 - 1, 256 * 256 - 1, cv.ThresholdTypes.Trunc)
            colorize.Run(ocvb)
            dst1 = colorize.dst1
        End If
    End Sub
End Class






Public Class Depth_Decreasing
    Inherits VBparent
    Public Increasing As Boolean
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Threshold in millimeters", 0, 1000, 8)

        ocvb.desc = "Identify where depth is decreasing - coming toward the camera."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim depth32f = If(standalone, getDepth32f(ocvb), src)
        Static lastDepth As cv.Mat = depth32f.Clone()
        If lastDepth.Size <> depth32f.Size Then lastDepth = depth32f

        Dim mmThreshold = sliders.trackbar(0).Value
        If Increasing Then
            cv.Cv2.Subtract(depth32f, lastDepth, dst1)
        Else
            cv.Cv2.Subtract(lastDepth, depth32f, dst1)
        End If
        dst1 = dst1.Threshold(mmThreshold, 0, cv.ThresholdTypes.Tozero).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        lastDepth = depth32f
    End Sub
End Class





Public Class Depth_Increasing
    Inherits VBparent
    Public depth As Depth_Decreasing
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        depth = New Depth_Decreasing(ocvb)
        depth.Increasing = True
        ocvb.desc = "Identify where depth is increasing - retreating from the camera."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        depth.src = src
        depth.Run(ocvb)
        dst1 = depth.dst1
    End Sub
End Class






Public Class Depth_Punch
    Inherits VBparent
    Dim depth As Depth_Decreasing
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        depth = New Depth_Decreasing(ocvb)
        ocvb.desc = "Identify the largest blob in the depth decreasing output"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        depth.src = getDepth32f(ocvb)
        depth.Run(ocvb)
        dst1 = depth.dst1
    End Sub
End Class







Public Class Depth_SmoothingMat
    Inherits VBparent
    Public trim As Depth_InRange
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        trim = New Depth_InRange(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Threshold in millimeters", 1, 1000, 10)
        label2 = "Depth pixels after smoothing"
        ocvb.desc = "Use depth rate of change to smooth the depth values beyond close range"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If standalone Then src = getDepth32f(ocvb)
        Dim rect = If(ocvb.drawRect.Width <> 0, ocvb.drawRect, New cv.Rect(0, 0, src.Width, src.Height))
        trim.src = src(rect)
        trim.Run(ocvb)
        Static lastDepth = trim.dst2 ' the far depth needs to be smoothed
        If lastDepth.Size <> trim.dst2.Size Then lastDepth = trim.dst2

        cv.Cv2.Subtract(lastDepth, trim.dst2, dst1)

        Static thresholdSlider = findSlider("Threshold in millimeters")
        Dim mmThreshold = CSng(thresholdSlider.Value)
        dst1 = dst1.Threshold(mmThreshold, 0, cv.ThresholdTypes.TozeroInv).Threshold(-mmThreshold, 0, cv.ThresholdTypes.Tozero)
        cv.Cv2.Add(trim.dst2, dst1, dst2)
        lastDepth = trim.dst2

        Static inrangeMinSlider = findSlider("InRange Min Depth")
        Static inrangeMaxSlider = findSlider("InRange Max Depth")
        label1 = "Smoothing Mat: range from " + CStr(inrangeMinSlider.Value) + " to +" + CStr(inrangeMaxSlider.Value)
    End Sub
End Class





Public Class Depth_Smoothing
    Inherits VBparent
    Dim smooth As Depth_SmoothingMat
    Dim reduction As Reduction_Basics
    Public reducedDepth As New cv.Mat
    Public mats As Mat_4to1
    Public colorize As Depth_ColorMap
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        colorize = New Depth_ColorMap(ocvb)
        mats = New Mat_4to1(ocvb)
        reduction = New Reduction_Basics(ocvb)
        Dim reductionRadio = findRadio("Use bitwise reduction")
        reductionRadio.Checked = True
        smooth = New Depth_SmoothingMat(ocvb)
        label2 = "Mask of depth that is smooth"
        ocvb.desc = "This attempt to get the depth data to 'calm' down is not working well enough to be useful - needs more work"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        smooth.src = getDepth32f(ocvb)
        smooth.Run(ocvb)
        Dim input = smooth.dst1.Normalize(0, 255, cv.NormTypes.MinMax)
        input.ConvertTo(mats.mat(0), cv.MatType.CV_8UC1)
        Dim tmp As New cv.Mat
        cv.Cv2.Add(smooth.dst2, smooth.dst1, tmp)
        mats.mat(1) = tmp.InRange(0, 255)

        reduction.src = smooth.src
        reduction.Run(ocvb)
        reduction.dst1.ConvertTo(reducedDepth, cv.MatType.CV_32F)
        colorize.src = reducedDepth
        colorize.Run(ocvb)
        dst1 = colorize.dst1
        mats.Run(ocvb)
        dst2 = mats.dst1
        label1 = smooth.label1
    End Sub
End Class






Public Class Depth_InRange
    Inherits VBparent
    Public Mask As New cv.Mat
    Public zeroMask As New cv.Mat
    Public depth32f As New cv.Mat
    Public minDepth As Double
    Public maxDepth As Double
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "InRange Min Depth", 0, 1000, 200)
        sliders.setupTrackBar(1, "InRange Max Depth", 200, 10000, 1400)
        label1 = "Depth values that are in-range"
        label2 = "Depth values that are out of range (and < 8m)"
        ocvb.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If sliders.trackbar(0).Value >= sliders.trackbar(1).Value Then sliders.trackbar(1).Value = sliders.trackbar(0).Value + 1
        minDepth = sliders.trackbar(0).Value
        maxDepth = sliders.trackbar(1).Value
        If src.Type = cv.MatType.CV_32F Then depth32f = src Else depth32f = getDepth32f(ocvb)
        cv.Cv2.InRange(depth32f, cv.Scalar.All(minDepth), cv.Scalar.All(maxDepth), Mask)
        cv.Cv2.BitwiseNot(Mask, zeroMask)
        dst1 = depth32f.Clone.SetTo(0, zeroMask)
        dst2 = depth32f.Clone.SetTo(0, Mask)
        If standalone Then
            depth32f.SetTo(0, zeroMask)
            dst2 = dst2.Threshold(8000, 8000, cv.ThresholdTypes.Trunc)
        End If
    End Sub
End Class









Public Class Depth_Edges
    Inherits VBparent
    Dim edges As Edges_Laplacian
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        edges = New Edges_Laplacian(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Threshold for depth disparity", 0, 255, 200)
        ocvb.desc = "Find edges in depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        edges.src = src
        edges.Run(ocvb)
        dst1 = edges.dst2
        dst2 = edges.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Depth_HolesOverTime
    Inherits VBparent
    Dim holes As Depth_Holes
    Dim recentImages As New List(Of cv.Mat)
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        holes = New Depth_Holes(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Number of images to retain", 0, 10, 3)

        label2 = "Latest hole mask"
        ocvb.desc = "Integrate memory holes over time to identify unstable depth"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        holes.Run(ocvb)
        recentImages.Add(holes.holeMask)

        dst2 = recentImages.ElementAt(0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For Each img In recentImages
            cv.Cv2.BitwiseOr(dst1, img, dst1)
        Next
        label1 = "Depth holes integrated over the past " + CStr(recentImages.Count) + " images"
        If recentImages.Count >= sliders.trackbar(0).Value Then
            recentImages.RemoveAt(0)
        End If
    End Sub
End Class








Public Class Depth_Holes
    Inherits VBparent
    Public holeMask As New cv.Mat
    Public borderMask As New cv.Mat
    Dim element As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Amount of dilation of borderMask", 1, 10, 1)
        sliders.setupTrackBar(1, "Amount of dilation of holeMask", 0, 10, 0)

        label2 = "Shadow Edges (use sliders to expand)"
        element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(5, 5))
        ocvb.desc = "Identify holes in the depth image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        holeMask = getDepth32f(ocvb).Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        holeMask = holeMask.Dilate(element, Nothing, sliders.trackbar(1).Value)
        dst1 = holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        borderMask = holeMask.Dilate(element, Nothing, sliders.trackbar(0).Value)
        cv.Cv2.BitwiseXor(borderMask, holeMask, borderMask)
        If standalone Then
            dst2.SetTo(0)
            ocvb.RGBDepth.CopyTo(dst2, borderMask)
        End If
    End Sub
End Class






Public Class Depth_TooClose
    Inherits VBparent
    Public holes As Depth_Holes
    Public minVal As Double
    Public depth32f As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        holes = New Depth_Holes(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Amount of depth padded to minimum depth (mm)", 1, 4000, 1000)

        label2 = "Non-Zero depth mask"
        ocvb.desc = "Tests to determine if the camera is too close"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        holes.Run(ocvb)
        cv.Cv2.BitwiseNot(holes.holeMask, dst2)
        depth32f = getDepth32f(ocvb)
        Dim maxval As Double
        Dim minLoc As cv.Point, maxLoc As cv.Point
        depth32f.MinMaxLoc(minVal, maxval, minLoc, maxLoc, dst2)
        cv.Cv2.InRange(depth32f, cv.Scalar.All(minVal), cv.Scalar.All(minVal + sliders.trackbar(0).Value), dst1)

        depth32f.MinMaxLoc(minVal, maxval, minLoc, maxLoc, dst1)
        label1 = "Min Z = " + Format(minVal, "#0") + " Max Z = " + Format(maxval, "#0")
    End Sub
End Class






Public Class Depth_NoiseRemovalMask
    Inherits VBparent
    Public noise As Depth_TooClose
    Public flood As FloodFill_8bit
    Dim padSlider As System.Windows.Forms.TrackBar
    Public depth32fNoiseRemoved As New cv.Mat
    Public noiseMask As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        flood = New FloodFill_8bit(ocvb)
        noise = New Depth_TooClose(ocvb)
        padSlider = findSlider("Amount of depth padded to minimum depth (mm)")
        hideForm("Palette_BuildGradientColorMap Slider Options")
        hideForm("Palette_Basics Radio Options")

        ocvb.desc = "Use the 'Too Close' test to remove (some) noisy depth"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        noise.Run(ocvb)
        dst1 = noise.dst1

        flood.src = dst1
        flood.Run(ocvb)
        dst2 = flood.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)

        depth32fNoiseRemoved = noise.depth32f
        noiseMask = dst1.Clone
        noiseMask.SetTo(0, dst2)
        depth32fNoiseRemoved.SetTo(0, noiseMask)

        label1 = "Depth values between " + Format(noise.minVal, "#0") + " and " + Format(padSlider.Value + noise.minVal, "#0")
        label2 = "Mask of solid depth < " + CStr(padSlider.Value)
    End Sub
End Class







Public Class Depth_TooCloseCentroids
    Inherits VBparent
    Dim depth As Depth_NoiseRemovalMask
    Public tooClosePoints As New List(Of cv.Point2f)
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        depth = New Depth_NoiseRemovalMask(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Size of rejected rects that are likely too close", 1, 500, 250)
        sliders.setupTrackBar(1, "Percent of zero depth in rejected rect", 1, 100, 20) ' Empircally determined - subject to change!

        ocvb.desc = "Plot the rejected centroids and rects in FloodFill - search for points that are too close"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        depth.Run(ocvb)
        dst2 = depth.noiseMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst1 = depth.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        For Each pt In depth.flood.basics.rejectedCentroids
            dst1.Circle(pt, 5, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Next

        Static maxPixelSlider = findSlider("Size of rejected rects that are likely too close")
        Static percentSlider = findSlider("Percent of zero depth in rejected rect")
        Dim maxPixels = maxPixelSlider.value
        Dim holes = depth.noise.holes.holeMask
        Dim percentThreshold = percentSlider.value / 100
        tooClosePoints.Clear()
        For Each r In depth.flood.basics.rejectedRects
            If r.Width * r.Height < maxPixels And r.Width > 0 And r.Height > 0 Then
                ' if the rect is surrounded by largely zero depth, then it is likely noise from being too close
                Dim percentZero = holes(r).CountNonZero() / (r.Width * r.Height)
                If percentZero > percentThreshold Then
                    dst2.Rectangle(r, cv.Scalar.Yellow, -1)
                    tooClosePoints.Add(New cv.Point2f(r.X + r.Width / 2, r.Y + r.Height / 2))
                End If
            End If
        Next
    End Sub
End Class






Public Class Depth_TooCloseCluster
    Inherits VBparent
    Dim rejects As Depth_TooCloseCentroids
    Dim knn2d As KNN_Point2d
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        knn2d = New KNN_Point2d(ocvb)
        rejects = New Depth_TooCloseCentroids(ocvb)
        label2 = "Red are recent rejects, white older"
        ocvb.desc = "Cluster rejected rect's in area too close to the camera"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        rejects.Run(ocvb)
        dst1 = rejects.dst2

        knn2d.knn.knnQT.trainingPoints = New List(Of cv.Point2f)(knn2d.knn.knnQT.queryPoints)
        knn2d.knn.knnQT.queryPoints = New List(Of cv.Point2f)(rejects.tooClosePoints)
        knn2d.Run(ocvb)
        If ocvb.frameCount Mod 10 = 0 Then dst2.SetTo(0)
        For i = 0 To knn2d.knn.knnQT.queryPoints.Count - 1
            Dim qPoint = knn2d.knn.knnQT.queryPoints.ElementAt(i)
            cv.Cv2.Circle(dst2, qPoint, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias, 0)
            Dim pt = knn2d.knn.knnQT.trainingPoints.ElementAt(knn2d.knn.neighbors.Get(Of Single)(i, 0))
            cv.Cv2.Circle(dst2, pt, 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias, 0)
            Dim distance = Math.Sqrt((pt.X - qPoint.X) * (pt.X - qPoint.X) + (pt.Y - qPoint.Y) * (pt.Y - qPoint.Y))
            If distance < src.Width / 10 Then dst2.Line(pt, qPoint, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class






Public Class Depth_NoiseRemovedAndColorized
    Inherits VBparent
    Dim colorize As Depth_Colorizer_CPP
    Dim depth As Depth_NoiseRemovalMask
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        colorize = New Depth_Colorizer_CPP(ocvb)
        depth = New Depth_NoiseRemovalMask(ocvb)
        label2 = "Solid depth (white) with likely noise (white pixels)"
        ocvb.desc = "Colorize Depth after some noise has been removed."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        depth.Run(ocvb)
        dst2 = depth.dst1

        colorize.src = depth.depth32fNoiseRemoved
        colorize.run(ocvb)
        dst1 = colorize.dst1
    End Sub
End Class







Public Class Depth_WorldXYZ
    Inherits VBparent
    Public xyzFrame As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        xyzFrame = New cv.Mat(src.Size(), cv.MatType.CV_32FC3)
        ocvb.desc = "Create 32-bit XYZ format from depth data (to slow to be useful.)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim depth32f = (getDepth32f(ocvb) * 0.001).ToMat ' convert to meters.
        Dim xy As New cv.Point3f
        For xy.Y = 0 To xyzFrame.Height - 1
            For xy.X = 0 To xyzFrame.Width - 1
                xy.Z = depth32f.Get(Of Single)(xy.Y, xy.X)
                If xy.Z <> 0 Then
                    Dim xyz = getWorldCoordinates(ocvb, xy)
                    xyzFrame.Set(Of cv.Point3f)(xy.Y, xy.X, xyz)
                End If
            Next
        Next
        If standalone Then ocvb.trueText("OpenGL data prepared.")
    End Sub
    Public Sub Close()
        xyzFrame.Dispose()
    End Sub
End Class






Public Class Depth_WorldXYZ_MT
    Inherits VBparent
    Dim grid As Thread_Grid
    Dim trim As Depth_InRange
    Public xyzFrame As cv.Mat
    Public depthUnitsMeters = False
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        grid = New Thread_Grid(ocvb)
        trim = New Depth_InRange(ocvb)

        xyzFrame = New cv.Mat(src.Size(), cv.MatType.CV_32FC3)
        ocvb.desc = "Create OpenGL point cloud from depth data (too slow to be useful)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        trim.src = src
        If trim.src.Type <> cv.MatType.CV_32F Then trim.src = getDepth32f(ocvb)
        trim.Run(ocvb)
        grid.Run(ocvb)

        xyzFrame.SetTo(0)
        Dim depth32f = If(depthUnitsMeters, trim.depth32f, (trim.depth32f * 0.001).ToMat) ' convert to meters.
        Dim multX = ocvb.pointCloud.Width / depth32f.Width
        Dim multY = ocvb.pointCloud.Height / depth32f.Height
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
              Sub(roi)
                  Dim xy As New cv.Point3f
                  For y = roi.Y To roi.Y + roi.Height - 1
                      For x = roi.X To roi.X + roi.Width - 1
                          xy.X = x * multX
                          xy.Y = y * multY
                          xy.Z = depth32f.Get(Of Single)(y, x)
                          If xy.Z <> 0 Then
                              Dim xyz = getWorldCoordinates(ocvb, xy)
                              xyzFrame.Set(Of cv.Point3f)(y, x, xyz)
                          End If
                      Next
                  Next
              End Sub)
        If standalone Then ocvb.trueText("OpenGL data prepared.")
    End Sub
End Class






' https://stackoverflow.com/questions/19093728/rotate-image-around-x-y-z-axis-in-opencv
' https://stackoverflow.com/questions/7019407/translating-and-rotating-an-image-in-3d-using-opencv
Public Class Depth_PointCloud_IMU
    Inherits VBparent
    Public histOpts As Histogram_ProjectionOptions
    Public Mask As New cv.Mat
    Public pointCloud As cv.Mat
    Public imu As IMU_GVector
    Public reduction As Reduction_Depth
    Public gMatrix(,) As Single
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        reduction = New Reduction_Depth(ocvb)
        imu = New IMU_GVector(ocvb)
        histOpts = New Histogram_ProjectionOptions(ocvb)
        If standalone Then histOpts.check.Visible = False
        If standalone Then imu.kalman.check.Visible = False

        label1 = "Mask for depth values that are in-range"
        ocvb.desc = "Rotate the PointCloud around the X-axis and the Z-axis using the gravity vector from the IMU."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim input = src
        If input.Type <> cv.MatType.CV_32FC3 Then input = ocvb.pointCloud

        Static rangeSlider = findSlider("InRange Max Depth (mm)")
        maxZ = rangeSlider.Value / 1000

        If ocvb.parms.IMU_Present = False Then
            ocvb.trueText("IMU unavailable for this camera")
        Else
            imu.Run(ocvb)
            Dim cx As Double = 1, sx As Double = 0, cy As Double = 1, sy As Double = 0, cz As Double = 1, sz As Double = 0
            '[cos(a) -sin(a)    0]
            '[sin(a)  cos(a)    0]
            '[0       0         1] rotate the point cloud around the z-axis.
            Static zCheckbox = findCheckBox("Z-Rotation with gravity vector")
            If zCheckbox.Checked Then
                cx = Math.Cos(imu.angleX)
                sx = Math.Sin(imu.angleX)
            End If

            '[1       0         0      ] rotate the point cloud around the x-axis.
            '[0       cos(a)    -sin(a)]
            '[0       sin(a)    cos(a) ]
            Static xCheckbox = findCheckBox("X-Rotation with gravity vector")
            If xCheckbox.Checked Then
                cz = Math.Cos(imu.angleZ)
                sz = Math.Sin(imu.angleZ)
            End If

            ' could use OpenCV for this but this makes it clearer.
            '[cx -sx    0]  [1  0   0 ] 
            '[sx  cx    0]  [0  cz -sz]
            '[0   0     1]  [0  sz  cz]
            gMatrix = {{cx * 1 + -sx * 0 + 0 * 0, cx * 0 + -sx * cz + 0 * sz, cx * 0 + -sx * -sz + 0 * cz},
                      {sx * 1 + cx * 0 + 0 * 0, sx * 0 + cx * cz + 0 * sz, sx * 0 + cx * -sz + 0 * cz},
                      {0 * 1 + 0 * 0 + 1 * 0, 0 * 0 + 0 * cz + 1 * sz, 0 * 0 + 0 * -sz + 1 * cz}}

            ' These 4 points will mark a 1-meter distance plane with or without rotation
            Dim z = 3.0
            Dim pt1 = New cv.Point3f(0, 0, z)
            Dim pt2 = New cv.Point3f(0, input.Height - 1, z)
            Dim pt3 = New cv.Point3f(input.Width - 1, input.Height - 1, z)
            Dim pt4 = New cv.Point3f(0, input.Height, z)
            input.Set(Of cv.Point3f)(pt1.Y, pt1.X, getWorldCoordinates(ocvb, pt1))
            input.Set(Of cv.Point3f)(pt2.Y, pt2.X, getWorldCoordinates(ocvb, pt2))
            input.Set(Of cv.Point3f)(pt3.Y, pt3.X, getWorldCoordinates(ocvb, pt3))
            input.Set(Of cv.Point3f)(pt4.Y, pt4.X, getWorldCoordinates(ocvb, pt4))

            For i = 0 To input.Height - 1
                pt1 = New cv.Point3f(0, i, z)
                input.Set(Of cv.Point3f)(pt1.Y, pt1.X, getWorldCoordinates(ocvb, pt1))
                pt1 = New cv.Point3f(input.Width - 1, i, z)
                input.Set(Of cv.Point3f)(pt1.Y, pt1.X, getWorldCoordinates(ocvb, pt1))
            Next

            For i = 0 To input.Width - 1
                pt1 = New cv.Point3f(i, 0, z)
                input.Set(Of cv.Point3f)(pt1.Y, pt1.X, getWorldCoordinates(ocvb, pt1))
                pt1 = New cv.Point3f(i, input.Height - 1, z)
                input.Set(Of cv.Point3f)(pt1.Y, pt1.X, getWorldCoordinates(ocvb, pt1))
            Next

            Static imuCheckBox = findCheckBox("Use IMU gravity vector")
            Dim changeRequested = True
            If xCheckbox.checked = False And zCheckbox.checked = False Then changeRequested = False
            Dim split = cv.Cv2.Split(input)
            If imuCheckBox.checked And changeRequested Then
                Dim mask As New cv.Mat
                cv.Cv2.InRange(split(2), 0.01, maxZ, dst1)
                cv.Cv2.BitwiseNot(dst1, mask)
                input.SetTo(0, mask)
                If standalone Then dst1 = dst1.Resize(dst1.Size)

                Dim gMat = New cv.Mat(3, 3, cv.MatType.CV_32F, gMatrix)
                Dim gInput = input.Reshape(1, input.Rows * input.Cols)
                Dim gOutput = (gInput * gMat).ToMat
                pointCloud = gOutput.Reshape(3, input.Rows)
            Else
                pointCloud = input.Clone
            End If

            Static reductionRadio = findRadio("No reduction")
            If reductionRadio.checked = False Then
                Split(2) *= 1000
                Split(2).ConvertTo(reduction.src, cv.MatType.CV_32S)
                reduction.Run(ocvb)
                split(2) = reduction.reducedDepth32F / 1000
                cv.Cv2.Merge(split, pointCloud)
            End If
        End If
    End Sub
End Class

