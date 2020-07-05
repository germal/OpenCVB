Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Depth_Median
    Inherits ocvbClass
    Dim median As Math_Median_CDF
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        median = New Math_Median_CDF(ocvb)
        median.src = New cv.Mat
        median.rangeMax = 10000
        median.rangeMin = 1 ' ignore depth of zero as it is not known.
        ocvb.desc = "Divide the depth image ahead and behind the median."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Region Count", 1, 250, 10)

        label2 = "Grayscale version"
        ocvb.desc = "Attempt to stabilize the depth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim reductionFactor = sliders.TrackBar1.Maximum - sliders.TrackBar1.Value
        dst1 = ocvb.RGBDepth / reductionFactor
        dst1 *= reductionFactor
        dst2 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class



Public Class Depth_FirstLastDistance
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Monitor the first and last depth distances"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "shadowRect Min Size", 1, 20000, 2000)

        shadow = New Depth_Holes(ocvb)

        ocvb.desc = "Identify the minimum rectangles of contours of the depth shadow"
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)

        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(shadow.borderMask, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        Dim minEllipse(contours.Length - 1) As cv.RotatedRect
        For i = 0 To contours.Length - 1
            Dim minRect = cv.Cv2.MinAreaRect(contours(i))
            If minRect.Size.Width * minRect.Size.Height > sliders.TrackBar1.Value Then
                Dim nextColor = New cv.Scalar(rColors(i Mod 255).Item0, rColors(i Mod 255).Item1, rColors(i Mod 255).Item2)
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
    Inherits ocvbClass
    Public trim As Depth_InRange
    Public kalman As Kalman_Basics
    Public trustedRect As cv.Rect
    Public trustworthy As Boolean
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        trim = New Depth_InRange(ocvb)
        kalman = New Kalman_Basics(ocvb)
        kalman.check.Visible = False ' we don't allow turning off kalman with this algorithm.
        ReDim kalman.input(4 - 1) ' cv.rect...
        label1 = "Blue is current, red is kalman, green is trusted"
        ocvb.desc = "Demonstrate the use of mean shift algorithm.  Use depth to find the top of the head and then meanshift to the face."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.src = getDepth32f(ocvb)
        trim.Run(ocvb)
        dst1 = trim.dst1.ConvertScaleAbs(255)
        Dim tmp = trim.dst1.ConvertScaleAbs(255)
        ' find the largest blob and use that as the body.  Head is highest in the image.
        Dim blobSize As New List(Of Int32)
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
        Dim maxBlob As Int32
        Dim maxIndex As Int32 = -1
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
    Inherits ocvbClass
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        shadow = New Depth_Holes(ocvb)

        sliders.setupTrackBar1(ocvb, caller, "FlatData Region Count", 1, 250, 200)

        label1 = "Reduced resolution RGBDepth"
        ocvb.desc = "Attempt to stabilize the depth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb) ' get where depth is zero

        Dim mask As New cv.Mat
        Dim gray As New cv.Mat
        Dim gray8u As New cv.Mat

        cv.Cv2.BitwiseNot(shadow.holeMask, mask)
        gray = getDepth32f(ocvb).Normalize(0, 255, cv.NormTypes.MinMax, -1, mask)
        gray.ConvertTo(gray8u, cv.MatType.CV_8U)

        Dim reductionFactor = sliders.TrackBar1.Maximum - sliders.TrackBar1.Value
        gray8u = gray8u / reductionFactor
        gray8u *= reductionFactor

        dst1 = gray8u.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class





Public Class Depth_FlatBackground
    Inherits ocvbClass
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        shadow = New Depth_Holes(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "FlatBackground Max Depth", 200, 10000, 2000)

        ocvb.desc = "Simplify the depth image with a flat background"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb) ' get where depth is zero
        Dim mask As New cv.Mat
        Dim maxDepth = cv.Scalar.All(sliders.TrackBar1.Value)
        Dim tmp As New cv.Mat
        dst1 = getDepth32f(ocvb)
        cv.Cv2.InRange(dst1, 0, maxDepth, tmp)
        cv.Cv2.ConvertScaleAbs(tmp, mask)

        Dim zeroMask As New cv.Mat
        cv.Cv2.BitwiseNot(mask, zeroMask)
        dst1.SetTo(0, zeroMask)

        ocvb.RGBDepth.CopyTo(dst1, mask)
        zeroMask.SetTo(255, shadow.holeMask)
        src.CopyTo(dst1, zeroMask)
        dst1.SetTo(maxDepth, zeroMask) ' set the depth to the maxdepth for any background
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
    Public Function Depth_XYZ_OpenMP_Run(DepthXYZPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function
End Module



' Use the C++ version below of this algorithm - this is way too slow...
Public Class Depth_WorldXYZ
    Inherits ocvbClass
    Public xyzFrame As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        xyzFrame = New cv.Mat(src.Size(), cv.MatType.CV_32FC3)
        ocvb.desc = "Create 32-bit XYZ format from depth data (to slow to be useful.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim depth32f = (getDepth32f(ocvb) * 0.001).ToMat ' convert to meters.
        Dim xy As New cv.Point3f
        For xy.Y = 0 To xyzFrame.Height - 1
            For xy.X = 0 To xyzFrame.Width - 1
                xy.Z = depth32f.Get(Of Single)(xy.Y, xy.X)
                If xy.Z <> 0 Then
                    Dim width = getWorldCoordinatesD(ocvb, xy)
                    xyzFrame.Set(Of cv.Point3f)(xy.Y, xy.X, width)
                End If
            Next
        Next
        ocvb.putText(New TTtext("OpenGL data prepared.", 10, 50, RESULT1))
    End Sub
End Class






Public Class Depth_WorldXYZ_MT
    Inherits ocvbClass
    Dim grid As Thread_Grid
    Dim trim As Depth_InRange
    Public xyzFrame As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32

        trim = New Depth_InRange(ocvb)

        xyzFrame = New cv.Mat(src.Size(), cv.MatType.CV_32FC3)
        ocvb.desc = "Create OpenGL point cloud from depth data (too slow to be useful)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.src = getDepth32f(ocvb)
        trim.Run(ocvb)
        grid.Run(ocvb)

        xyzFrame.SetTo(0)
        Dim depth32f = (trim.depth32f * 0.001).ToMat ' convert to meters.
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim xy As New cv.Point3f
            For xy.Y = roi.Y To roi.Y + roi.Height - 1
                For xy.X = roi.X To roi.X + roi.Width - 1
                    xy.Z = depth32f.Get(Of Single)(xy.Y, xy.X)
                    If xy.Z <> 0 Then
                        Dim width = getWorldCoordinatesD(ocvb, xy)
                        xyzFrame.Set(Of cv.Point3f)(xy.Y, xy.X, width)
                    End If
                Next
            Next
        End Sub)
        ocvb.putText(New TTtext("OpenGL data prepared.", 10, 50, RESULT1))
    End Sub
End Class






Public Class Depth_MeanStdev_MT
    Inherits ocvbClass
    Dim grid As Thread_Grid
    Dim meanSeries As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 40

        sliders.setupTrackBar1(ocvb, caller, "MeanStdev Max Depth Range", 1, 20000, 3500)
        sliders.setupTrackBar2("MeanStdev Frame Series", 1, 100, 5)
        ocvb.desc = "Collect a time series of depth and measure where the stdev is unstable.  Plan is to avoid depth where unstable."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U)
        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U)

        Dim maxDepth = sliders.TrackBar1.Value
        Dim meanCount = sliders.TrackBar2.Value

        Static lastMeanCount As Int32
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
    Inherits ocvbClass
    Dim shadow As Depth_Holes
    Dim plot1 As Plot_OverTime
    Dim plot2 As Plot_OverTime
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
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
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
    Dim retina As Retina_Basics_CPP
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        retina = New Retina_Basics_CPP(ocvb)

        sliders.setupTrackBar1(ocvb, caller, "Uncertainty threshold", 1, 255, 100)

        label2 = "Mask of areas with unstable depth"
        ocvb.desc = "Use the bio-inspired retina algorithm to determine depth uncertainty."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        retina.src = ocvb.RGBDepth
        retina.Run(ocvb)
        dst1 = retina.dst1
        dst2 = retina.dst2.Threshold(sliders.TrackBar1.Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Depth_Palette
    Inherits ocvbClass
    Public trim As Depth_InRange
    Dim customColorMap As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        trim = New Depth_InRange(ocvb)
        trim.sliders.TrackBar2.Value = 5000

        customColorMap = colorTransition(cv.Scalar.Blue, cv.Scalar.Yellow, 256)
        ocvb.desc = "Use a palette to display depth from the raw depth data."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.src = getDepth32f(ocvb)
        trim.Run(ocvb)
        Dim minDepth = trim.sliders.TrackBar1.Value
        Dim maxDepth = trim.sliders.TrackBar2.Value

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
    Public Function Depth_Colorizer_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer2_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_Colorizer2_Close(Depth_ColorizerPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer2_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32, histSize As Int32) As IntPtr
    End Function


    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer32f_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_Colorizer32f_Close(Depth_ColorizerPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer32f_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer32f2_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Depth_Colorizer32f2_Close(Depth_ColorizerPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Depth_Colorizer32f2_Run(Depth_ColorizerPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32, histSize As Int32) As IntPtr
    End Function
    Public Function getDepth32f(ocvb As AlgorithmData) As cv.Mat
        Dim depth32f As New cv.Mat
        ocvb.depth16.ConvertTo(depth32f, cv.MatType.CV_32F)
        If ocvb.parms.resolution = resMed Then Return depth32f.Resize(ocvb.color.Size())
        Return depth32f
    End Function
End Module


Public Class Depth_Colorizer_CPP
    Inherits ocvbClass
    Dim dcPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        dcPtr = Depth_Colorizer_Open()
        ocvb.desc = "Display Depth image using C++ instead of VB.Net"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then src = getDepth32f(ocvb) Else dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)

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
    Inherits ocvbClass
    Public Mask As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Min Depth", 200, 1000, 200)
        sliders.setupTrackBar2("Max Depth", 200, 10000, 1400)
        ocvb.desc = "Manually show depth with varying min and max depths."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If sliders.TrackBar1.Value >= sliders.TrackBar2.Value Then sliders.TrackBar2.Value = sliders.TrackBar1.Value + 1
        Dim minDepth = sliders.TrackBar1.Value
        Dim maxDepth = sliders.TrackBar2.Value
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
    Inherits ocvbClass
    Public trim As Depth_InRange
    Dim dcPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        dcPtr = Depth_Colorizer2_Open()

        trim = New Depth_InRange(ocvb)

        label2 = "Mask from Depth_InRange"
        ocvb.desc = "Display depth data with inrange trim.  Higher contrast than others - yellow to blue always present."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Colorize depth manually."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim src = getDepth32f(ocvb)
        Dim nearColor = New Byte() {0, 255, 255}
        Dim farColor = New Byte() {255, 0, 0}

        Dim histogram(256 * 256 - 1) As Int32
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
    Inherits ocvbClass
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Min Depth", 0, 1000, 0)
        sliders.setupTrackBar2("Max Depth", 1001, 10000, 4000)

        grid = New Thread_Grid(ocvb)

        ocvb.desc = "Colorize depth manually with multi-threading."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        If standalone Then src = getDepth32f(ocvb)
        Dim nearColor = New Single() {0, 1, 1}
        Dim farColor = New Single() {1, 0, 0}

        Dim minDepth = sliders.TrackBar1.Value
        Dim maxDepth = sliders.TrackBar2.Value
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
               Dim rgbIndex As Int32
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
    Inherits ocvbClass
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Min Depth", 100, 1000, 100)
        sliders.setupTrackBar2("Max Depth", 1001, 10000, 4000)

        grid = New Thread_Grid(ocvb)

        ocvb.desc = "Colorize normally uses CDF to stabilize the colors.  Just using sliders here - stabilized but not optimal range."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        If standalone Then src = getDepth32f(ocvb)
        If src.Width <> src.Width Then src = src.Resize(src.Size())
        Dim nearColor = New Single() {0, 1, 1}
        Dim farColor = New Single() {1, 0, 0}

        Dim minDepth = sliders.TrackBar1.Value
        Dim maxDepth = sliders.TrackBar2.Value

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
    Inherits ocvbClass
    Public grid As Thread_Grid
    Public minPoint(0) As cv.Point2f
    Public maxPoint(0) As cv.Point2f
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)

        label1 = "Red is min distance, blue is max distance"
        ocvb.desc = "Find min and max depth in each segment."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
    Dim kalman As Kalman_Basics
    Public grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 128
        grid.sliders.TrackBar2.Value = 90

        kalman = New Kalman_Basics(ocvb)

        label1 = "Red is min distance, blue is max distance"
        ocvb.desc = "Find minimum depth in each segment."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
        paint_voronoi(scalarColors, dst2, subdiv)
    End Sub
End Class





Public Class Depth_ColorMap
    Inherits ocvbClass
    Dim Palette As Palette_ColorMap
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Depth ColorMap Alpha X100", 1, 100, 3)

        Palette = New Palette_ColorMap(ocvb)
        ocvb.desc = "Display the depth as a color map"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim alpha = sliders.TrackBar1.Value / 100
        cv.Cv2.ConvertScaleAbs(getDepth32f(ocvb), Palette.src, alpha)
        Palette.src = ocvb.RGBDepth
        Palette.Run(ocvb)
        dst1 = Palette.dst1
        dst2 = Palette.dst2
    End Sub
End Class



Public Class Depth_Holes
    Inherits ocvbClass
    Public holeMask As New cv.Mat
    Public borderMask As New cv.Mat
    Dim element As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Amount of dilation around depth holes", 1, 10, 1)

        label2 = "Shadow Edges (use sliders to expand)"
        element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(5, 5))
        ocvb.desc = "Identify holes in the depth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        holeMask = getDepth32f(ocvb).Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        If standalone Then dst1 = holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        borderMask = holeMask.Dilate(element, Nothing, sliders.TrackBar1.Value)
        cv.Cv2.BitwiseXor(borderMask, holeMask, borderMask)
        If standalone Then
            dst2.SetTo(0)
            ocvb.RGBDepth.CopyTo(dst2, borderMask)
        End If
    End Sub
End Class





Public Class Depth_Stable
    Inherits ocvbClass
    Public mog As BGSubtract_Basics_CPP
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        ' sliders.setupTrackBar1(ocvb, caller, "")
        mog = New BGSubtract_Basics_CPP(ocvb)

        label2 = "Stable (non-zero) Depth"
        ocvb.desc = "Collect X frames, compute stable depth using the RGB and Depth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
    Public stable As Depth_Stable
    Public mean As Mean_Basics
    Public colorize As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        mean = New Mean_Basics(ocvb)
        colorize = New Depth_Colorizer_CPP(ocvb)
        stable = New Depth_Stable(ocvb)

        ocvb.desc = "Use the mask of stable depth (using RGBDepth) to stabilize the depth at any individual point."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
    Public Increasing As Boolean
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Threshold in millimeters", 0, 1000, 8)

        ocvb.desc = "Identify where depth is decreasing - coming toward the camera."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim depth32f = If(standalone, getDepth32f(ocvb), src)
        Static lastDepth As cv.Mat = depth32f.Clone()
        If lastDepth.Size <> depth32f.Size Then lastDepth = depth32f

        Dim mmThreshold = sliders.TrackBar1.Value
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
    Inherits ocvbClass
    Public depth As Depth_Decreasing
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        depth = New Depth_Decreasing(ocvb)
        depth.Increasing = True
        ocvb.desc = "Identify where depth is increasing - retreating from the camera."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        depth.src = src
        depth.Run(ocvb)
        dst1 = depth.dst1
    End Sub
End Class






Public Class Depth_Punch
    Inherits ocvbClass
    Dim depth As Depth_Decreasing
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        depth = New Depth_Decreasing(ocvb)
        ocvb.desc = "Identify the largest blob in the depth decreasing output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        depth.src = getDepth32f(ocvb)
        depth.Run(ocvb)
        dst1 = depth.dst1
    End Sub
End Class







Public Class Depth_SmoothingMat
    Inherits ocvbClass
    Public trim As Depth_InRange
    Public inputInMeters As Boolean
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        trim = New Depth_InRange(ocvb)

        sliders.setupTrackBar1(ocvb, caller, "Threshold in millimeters", 1, 1000, 100)
        label2 = "Depth pixels after smoothing"
        ocvb.desc = "Use depth rate of change to smooth the depth values beyond close range"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then src = getDepth32f(ocvb)
        trim.inputInMeters = inputInMeters
        Dim rect = If(ocvb.drawRect.Width <> 0, ocvb.drawRect, New cv.Rect(0, 0, src.Width, src.Height))
        trim.src = getDepth32f(ocvb)
        trim.src = trim.src(rect)
        trim.Run(ocvb)
        Static lastDepth = trim.dst2 ' the far depth needs to be smoothed
        If lastDepth.Size <> trim.dst2.Size Then lastDepth = trim.dst2

        dst1 = New cv.Mat
        cv.Cv2.Subtract(lastDepth, trim.dst2, dst1)

        Dim mmThreshold = CSng(sliders.TrackBar1.Value)
        If inputInMeters Then mmThreshold /= 1000
        dst1 = dst1.Threshold(mmThreshold, 0, cv.ThresholdTypes.TozeroInv)
        dst1 = dst1.Threshold(-mmThreshold, 0, cv.ThresholdTypes.Tozero)
        cv.Cv2.Add(trim.dst2, dst1, dst2)
        lastDepth = trim.dst2
        label1 = "Smoothing Mat: range from -" + CStr(sliders.TrackBar1.Value) + " to +" + CStr(sliders.TrackBar1.Value)
    End Sub
End Class





Public Class Depth_Smoothing
    Inherits ocvbClass
    Dim smooth As Depth_SmoothingMat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        smooth = New Depth_SmoothingMat(ocvb)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Smooth the dst2 output "
        check.Box(0).Checked = True

        ocvb.desc = "This attempt to get the depth data to 'calm' down (for the D435i) is not working well enough to be useful - needs more work"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        smooth.src = getDepth32f(ocvb)
        smooth.Run(ocvb)
        dst1 = smooth.dst1
        If check.Box(0).Checked Then
            cv.Cv2.Add(smooth.dst2, dst1, dst2)
            label2 = "Depth with smoothing applied"
        Else
            dst2 = smooth.dst2
            label2 = "Depth without smoothing "
        End If
    End Sub
End Class






Public Class Depth_InRange
    Inherits ocvbClass
    Public Mask As New cv.Mat
    Public zeroMask As New cv.Mat
    Public depth32f As New cv.Mat
    Public minDepth As Double
    Public maxDepth As Double
    Public inputInMeters As Boolean
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "InRange Min Depth", 0, 1000, 200)
        sliders.setupTrackBar2("InRange Max Depth", 200, 10000, 1400)
        label1 = "Depth values that are in-range"
        label2 = "Depth values that are out of range (and < 8m)"
        ocvb.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If sliders.TrackBar1.Value >= sliders.TrackBar2.Value Then sliders.TrackBar2.Value = sliders.TrackBar1.Value + 1
        minDepth = cv.Scalar.All(sliders.TrackBar1.Value)
        maxDepth = cv.Scalar.All(sliders.TrackBar2.Value)
        If inputInMeters Then
            minDepth /= 1000
            maxDepth /= 1000
        End If
        If src.Type = cv.MatType.CV_32F Then depth32f = src Else depth32f = getDepth32f(ocvb)
        cv.Cv2.InRange(depth32f, minDepth, maxDepth, Mask)
        cv.Cv2.BitwiseNot(Mask, zeroMask)
        dst1 = depth32f.Clone.SetTo(0, zeroMask)
        dst2 = depth32f.Clone.SetTo(0, Mask)
        depth32f.SetTo(0, zeroMask)
        dst2 = dst2.Threshold(8000, 8000, cv.ThresholdTypes.Trunc) ' the data beyond 8 meters is suspect and will distort the image output
    End Sub
End Class






Public Class Depth_PointCloudInRange
    Inherits ocvbClass
    Public histOpts As Histogram_ProjectionOptions
    Public Mask As New cv.Mat
    Public maxMeters As Double
    Public split() As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        If standalone Then histOpts = New Histogram_ProjectionOptions(ocvb)
        label1 = "Mask for depth values that are in-range"
        ocvb.desc = "Show PointCloud while varying the max depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        maxMeters = histOpts.sliders.TrackBar2.Value / 1000

        split = cv.Cv2.Split(ocvb.pointCloud)

        Dim tmp As New cv.Mat
        cv.Cv2.InRange(split(2), cv.Scalar.All(0), cv.Scalar.All(maxMeters), Mask)
        Dim zeroDepth = split(2).Threshold(0.001, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)
        Mask = Mask.SetTo(0, zeroDepth)

        dst1 = Mask
    End Sub
End Class









' https://stackoverflow.com/questions/19093728/rotate-image-around-x-y-z-axis-in-opencv
' https://stackoverflow.com/questions/7019407/translating-and-rotating-an-image-in-3d-using-opencv
Public Class Depth_PointCloudInRange_IMU
    Inherits ocvbClass
    Public histOpts As Histogram_ProjectionOptions
    Public Mask As New cv.Mat
    Public maxMeters As Double
    Public split(3 - 1) As cv.Mat
    Public imu As IMU_GVector
    Public xRotation As Boolean = True
    Public zRotation As Boolean = True
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        imu = New IMU_GVector(ocvb)
        If standalone = False Then imu.result = RESULT2

        If standalone Then histOpts = New Histogram_ProjectionOptions(ocvb)
        label2 = "Mask for depth values that are in-range"
        ocvb.desc = "Rotate the PointCloud around the X-axis and the Z-axis using the gravity vector from the IMU."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        maxMeters = histOpts.sliders.TrackBar2.Value / 1000
        Dim tSplit = cv.Cv2.Split(ocvb.pointCloud)
        split = tSplit

        If ocvb.parms.cameraIndex = T265Camera Or ocvb.parms.cameraIndex = L515 Then
            ocvb.putText(New TTtext("IMU unavailable (?) for this camera", 10, 50, RESULT1))
        Else
            imu.Run(ocvb)
            If zRotation Then
                '[cos(a) -sin(a) 0]
                '[sin(a)  cos(a) 0]
                '[0      0       1] rotate the point cloud around the z-axis.
                Dim yZ(,) = {{Math.Cos(-imu.angleX), -Math.Sin(-imu.angleX), 0}, {Math.Sin(-imu.angleX), Math.Cos(-imu.angleX), 0}, {0, 0, 1}}

                split(0) = yZ(0, 0) * tSplit(0) + yZ(0, 1) * tSplit(1)
                split(1) = yZ(1, 0) * tSplit(0) + yZ(1, 1) * tSplit(1)
                split(2) = tSplit(2)
                tSplit = split
            End If

            If xRotation Then
                '[1      0       0] rotate the point cloud around the x-axis.
                '[0 cos(a) -sin(a)]
                '[0 sin(a)  cos(a)]
                Dim xZ(,) = {{1, 0, 0}, {0, Math.Cos(-imu.angleZ), -Math.Sin(-imu.angleZ)}, {0, Math.Sin(-imu.angleZ), Math.Cos(-imu.angleZ)}}

                split(0) = tSplit(0)
                split(1) = xZ(1, 1) * tSplit(1) + xZ(1, 2) * tSplit(2)
                split(2) = xZ(2, 1) * tSplit(1) + xZ(2, 2) * tSplit(2)
            End If

            cv.Cv2.InRange(split(2), cv.Scalar.All(0), cv.Scalar.All(maxMeters), Mask)
            Dim zeroDepth = split(2).Threshold(0.001, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(255)
            Mask = Mask.SetTo(0, zeroDepth)
            If standalone Then dst1 = Mask
        End If
    End Sub
End Class
