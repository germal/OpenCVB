Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Depth_WorldXYZ_MT : Implements IDisposable
    Dim grid As Thread_Grid
    Public xyzFrame As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32
        xyzFrame = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32FC3)
        ocvb.desc = "Create OpenGL point cloud from depth data (too slow to be useful)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        xyzFrame.SetTo(0)
        Dim depth32f = getDepth32f(ocvb)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim xy As New cv.Point3f
            For xy.Y = roi.Y To roi.Y + roi.Height - 1
                For xy.X = roi.X To roi.X + roi.Width - 1
                    xy.Z = depth32f.Get(of UInt16)(xy.Y, xy.X)
                    If xy.Z <> 0 Then
                        Dim w = getWorldCoordinatesD(ocvb, xy)
                        xyzFrame.Set(Of cv.Point3f)(xy.Y, xy.X, w)
                    End If
                Next
            Next
        End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
    End Sub
End Class




Public Class Depth_Median : Implements IDisposable
    Dim median As Math_Median_CDF
    Public Sub New(ocvb As AlgorithmData)
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
        ocvb.result1.SetTo(0)
        ocvb.RGBDepth.CopyTo(ocvb.result1, mask)

        Dim zeroMask = median.src.Equals(0)
        cv.Cv2.ConvertScaleAbs(zeroMask, zeroMask.ToMat)
        ocvb.result1.SetTo(0, zeroMask)

        ocvb.label1 = "Median Depth < " + Format(median.medianVal, "#0.0")

        cv.Cv2.BitwiseNot(mask, mask)
        ocvb.result2.SetTo(0)
        ocvb.RGBDepth.CopyTo(ocvb.result2, mask)
        ocvb.result2.SetTo(0, zeroMask)
        ocvb.label2 = "Median Depth > " + Format(median.medianVal, "#0.0")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        median.Dispose()
    End Sub
End Class




Public Class Depth_Flatland : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Region Count", 1, 250, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.label2 = "Grayscale version"
        ocvb.desc = "Attempt to stabilize the depth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim reductionFactor = sliders.TrackBar1.Maximum - sliders.TrackBar1.Value
        ocvb.result1 = ocvb.RGBDepth / reductionFactor
        ocvb.result1 *= reductionFactor
        ocvb.result2 = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.result2 = ocvb.result2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Depth_FirstLastDistance : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Monitor the first and last depth distances"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim depth32f = getDepth32f(ocvb)
        Dim mask = depth32f.Threshold(1, 20000, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        Dim minVal As Double, maxVal As Double
        Dim minPt As cv.Point, maxPt As cv.Point
        cv.Cv2.MinMaxLoc(depth32f, minVal, maxVal, minPt, maxPt, mask)
        ocvb.RGBDepth.CopyTo(ocvb.result1)
        ocvb.RGBDepth.CopyTo(ocvb.result2)
        ocvb.label1 = "Min Depth " + CStr(minVal) + " mm"
        ocvb.result1.Circle(minPt, 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        ocvb.label2 = "Max Depth " + CStr(maxVal) + " mm"
        ocvb.result2.Circle(maxPt, 10, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class Depth_HolesRect : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "shadowRect Min Size", 1, 20000, 2000)
        If ocvb.parms.ShowOptions Then sliders.Show()

        shadow = New Depth_Holes(ocvb)
        shadow.externalUse = True

        ocvb.desc = "Identify the minimum rectangles of contours of the depth shadow"
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)

        ocvb.result1.SetTo(0)
        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(shadow.borderMask, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        Dim minEllipse(contours.Length - 1) As cv.RotatedRect
        For i = 0 To contours.Length - 1
            Dim minRect = cv.Cv2.MinAreaRect(contours(i))
            If minRect.Size.Width * minRect.Size.Height > sliders.TrackBar1.Value Then
                Dim nextColor = New cv.Scalar(ocvb.rColors(i Mod 255).Item0, ocvb.rColors(i Mod 255).Item1, ocvb.rColors(i Mod 255).Item2)
                drawRotatedRectangle(minRect, ocvb.result1, nextColor)
                If contours(i).Length >= 5 Then
                    minEllipse(i) = cv.Cv2.FitEllipse(contours(i))
                End If
            End If
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        shadow.Dispose()
        sliders.Dispose()
    End Sub
End Class



Public Class Depth_Foreground : Implements IDisposable
    Public trim As Depth_InRange
    Public Sub New(ocvb As AlgorithmData)
        trim = New Depth_InRange(ocvb)
        ocvb.desc = "Demonstrate the use of mean shift algorithm.  Use depth to find the top of the head and then meanshift to the face."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        ocvb.result1.CopyTo(ocvb.result2)
        Dim gray = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)

        ' find the largest blob and use that as the body.  Head is highest in the image.
        Dim blobSize As New List(Of Int32)
        Dim blobLocation As New List(Of cv.Point)
        For y = 0 To gray.Height - 1
            For x = 0 To gray.Width - 1
                Dim nextByte = gray.Get(of Byte)(y, x)
                If nextByte <> 0 Then
                    Dim count = gray.FloodFill(New cv.Point(x, y), 0)
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

        If maxIndex >= 0 Then
            Dim rectSize = 150
            If ocvb.color.Width > 1000 Then rectSize = 250
            Dim xx = blobLocation.Item(maxIndex).X - rectSize / 2
            Dim yy = blobLocation.Item(maxIndex).Y - rectSize / 2
            If xx < 0 Then xx = 0
            If yy < 0 Then yy = 0
            If xx + rectSize > ocvb.color.Width Then xx = ocvb.color.Width - rectSize
            If yy + rectSize > ocvb.color.Height Then yy = ocvb.color.Height - rectSize
            ocvb.drawRect = New cv.Rect(xx, yy, rectSize, rectSize)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        trim.Dispose()
    End Sub
End Class






Public Class Depth_FlatData : Implements IDisposable
    Dim shadow As Depth_Holes
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        shadow = New Depth_Holes(ocvb)

        sliders.setupTrackBar1(ocvb, "FlatData Region Count", 1, 250, 200)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.label1 = "Reduced resolution RGBDepth"
        ocvb.label2 = "Contours of the Depth Shadow"
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

        ocvb.result1 = gray8u.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        shadow.Dispose()
    End Sub
End Class



Public Class Depth_FlatBackground : Implements IDisposable
    Dim shadow As Depth_Holes
    Dim sliders As New OptionsSliders
    Public dst As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        shadow = New Depth_Holes(ocvb)
        sliders.setupTrackBar1(ocvb, "FlatBackground Max Depth", 200, 10000, 2000)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Simplify the depth image with a flat background"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb) ' get where depth is zero
        Dim mask As New cv.Mat
        Dim maxDepth = cv.Scalar.All(sliders.TrackBar1.Value)
        Dim tmp As New cv.Mat
        dst = getDepth32f(ocvb)
        cv.Cv2.InRange(dst, 0, maxDepth, tmp)
        cv.Cv2.ConvertScaleAbs(tmp, mask)

        Dim zeroMask As New cv.Mat
        cv.Cv2.BitwiseNot(mask, zeroMask)
        dst.SetTo(0, zeroMask)

        ocvb.result1.SetTo(0)
        ocvb.RGBDepth.CopyTo(ocvb.result1, mask)
        zeroMask.SetTo(255, shadow.holeMask)
        ocvb.color.CopyTo(ocvb.result1, zeroMask)
        dst.SetTo(maxDepth, zeroMask) ' set the depth to the maxdepth for any background
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        shadow.Dispose()
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
Public Class Depth_WorldXYZ : Implements IDisposable
    Public xyzFrame As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        xyzFrame = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32FC3)
        ocvb.desc = "Create 32-bit XYZ format from depth data (to slow to be useful.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim depth32f = getDepth32f(ocvb)
        Dim xy As New cv.Point3f
        For xy.Y = 0 To xyzFrame.Height - 1
            For xy.X = 0 To xyzFrame.Width - 1
                xy.Z = depth32f.Get(of Single)(xy.Y, xy.X)
                If xy.Z <> 0 Then
                    Dim w = getWorldCoordinatesD(ocvb, xy)
                    xyzFrame.Set(Of cv.Point3f)(xy.Y, xy.X, w)
                End If
            Next
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class



Public Class Depth_WorldXYZ_CPP : Implements IDisposable
    Public pointCloud As cv.Mat
    Dim DepthXYZ As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "xyzFrame is built"
        ocvb.desc = "Get the X, Y, Depth in the image coordinates (not the 3D image coordinates.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount = 0 Then DepthXYZ = Depth_XYZ_OpenMP_Open(ocvb.parms.intrinsicsLeft.ppx, ocvb.parms.intrinsicsLeft.ppy,
                                                                     ocvb.parms.intrinsicsLeft.fx, ocvb.parms.intrinsicsLeft.fy)

        Dim depth32f = getDepth32f(ocvb)
        Dim depthData(depth32f.Total * depth32f.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned) ' pin it for the duration...
        Marshal.Copy(depth32f.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_XYZ_OpenMP_Run(DepthXYZ, handleSrc.AddrOfPinnedObject(), depth32f.Rows, depth32f.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(depth32f.Total * 3 * 4 - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            pointCloud = New cv.Mat(depth32f.Rows, depth32f.Cols, cv.MatType.CV_32FC3, dstData)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Depth_XYZ_OpenMP_Close(DepthXYZ)
    End Sub
End Class


Public Class Depth_MeanStdev_MT : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim grid As Thread_Grid
    Dim meanSeries As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 64

        sliders.setupTrackBar1(ocvb, "MeanStdev Max Depth Range", 1, 20000, 3500)
        sliders.setupTrackBar2(ocvb, "MeanStdev Frame Series", 1, 100, 5)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Collect a time series of depth and measure where the stdev is unstable.  Plan is to avoid depth where unstable."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        ocvb.result1 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8U)
        ocvb.result2 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8U)

        Dim maxDepth = sliders.TrackBar1.Value
        Dim meanCount = sliders.TrackBar2.Value

        Static lastMeanCount As Int32
        If grid.roiList.Count <> meanSeries.Rows Or meanCount <> lastMeanCount Then
            meanSeries = New cv.Mat(grid.roiList.Count - 1, meanCount, cv.MatType.CV_32F, 0)
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
        Parallel.For(0, grid.roiList.Count - 1,
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
            cv.Cv2.MinMaxLoc(meanValues, minVal, maxVal, minPt, maxPt, meanMask)
            Dim stdMask = stdValues.Threshold(0.001, maxDepth, cv.ThresholdTypes.Binary).ConvertScaleAbs() ' volatile region is x cm stdev.
            cv.Cv2.MinMaxLoc(stdValues, minStdVal, maxStdVal, minPt, maxPt, stdMask)

            Parallel.For(0, grid.roiList.Count - 1,
            Sub(i)
                Dim roi = grid.roiList(i)
                ' this marks all the regions where the depth is volatile.
                ocvb.result2(roi).SetTo(255 * (stdValues.Get(of Single)(i, 0) - minStdVal) / (maxStdVal - minStdVal))
                ocvb.result2(roi).SetTo(0, outOfRangeMask(roi))

                ocvb.result1(roi).SetTo(255 * (meanValues.Get(of Single)(i, 0) - minVal) / (maxVal - minVal))
                ocvb.result1(roi).SetTo(0, outOfRangeMask(roi))
            End Sub)
            cv.Cv2.BitwiseOr(ocvb.result2, grid.gridMask, ocvb.result2)
            ocvb.label2 = "ROI Stdev: Min " + Format(minStdVal, "#0.0") + " Max " + Format(maxStdVal, "#0.0")
        End If

        ocvb.label1 = "ROI Means: Min " + Format(minVal, "#0.0") + " Max " + Format(maxVal, "#0.0")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        grid.Dispose()
    End Sub
End Class


Public Class Depth_MeanStdevPlot : Implements IDisposable
    Dim shadow As Depth_Holes
    Dim plot1 As Plot_OverTime
    Dim plot2 As Plot_OverTime
    Public Sub New(ocvb As AlgorithmData)
        shadow = New Depth_Holes(ocvb)
        shadow.externalUse = True

        plot1 = New Plot_OverTime(ocvb)
        plot1.externalUse = True
        plot1.dst = ocvb.result1
        plot1.maxScale = 2000
        plot1.plotCount = 1

        plot2 = New Plot_OverTime(ocvb)
        plot2.externalUse = True
        plot2.dst = ocvb.result2
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
        plot2.plotData = New cv.Scalar(stdev, 0, 0)
        plot2.Run(ocvb)
        ocvb.label1 = "Plot of mean depth = " + Format(mean, "#0.0")
        ocvb.label2 = "Plot of depth stdev = " + Format(stdev, "#0.0")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        plot1.Dispose()
        plot2.Dispose()
    End Sub
End Class




Public Class Depth_Uncertainty : Implements IDisposable
    Dim retina As Retina_Basics_CPP
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        retina = New Retina_Basics_CPP(ocvb)
        retina.externalUse = True

        sliders.setupTrackBar1(ocvb, "Uncertainty threshold", 1, 255, 100)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Use the bio-inspired retina algorithm to determine depth uncertainty."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        retina.src = ocvb.RGBDepth
        retina.Run(ocvb)
        ocvb.result2 = ocvb.result2.Threshold(sliders.TrackBar1.Value, 255, cv.ThresholdTypes.Binary)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        retina.Dispose()
        sliders.Dispose()
    End Sub
End Class





Public Class Depth_Stable : Implements IDisposable
    Dim mog As BGSubtract_Basics_CPP
    Public Sub New(ocvb As AlgorithmData)
        mog = New BGSubtract_Basics_CPP(ocvb)
        mog.radio.check(1).Checked = True
        mog.externalUse = True

        ocvb.label2 = "Stable (non-zero) Depth"
        ocvb.desc = "Collect X frames, compute stable depth and color pixels using thresholds"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        mog.src = ocvb.RGBDepth
        mog.Run(ocvb)

        cv.Cv2.BitwiseNot(ocvb.result1, ocvb.result2)
        ocvb.label1 = "Unstable Depth" + " using " + mog.radio.check(mog.currMethod).Text + " method"
        Dim zeroDepth = getDepth32f(ocvb).Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs(1)
        ocvb.result2.SetTo(0, zeroDepth)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        mog.Dispose()
    End Sub
End Class




Public Class Depth_Palette : Implements IDisposable
    Public trim As Depth_InRange
    Dim customColorMap As New cv.Mat
    Dim depth As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        trim = New Depth_InRange(ocvb)
        trim.externalUse = True
        trim.sliders.TrackBar2.Value = 5000

        customColorMap = colorTransition(cv.Scalar.Blue, cv.Scalar.Yellow, 256)
        ocvb.desc = "Use a palette to display depth from the raw depth data.  Will it be faster Depth_Colorizer?  (Of course)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        Dim minDepth = trim.sliders.TrackBar1.Value
        Dim maxDepth = trim.sliders.TrackBar2.Value

        Dim depthNorm = getDepth32f(ocvb)
        depthNorm *= 255 / (maxDepth - minDepth) ' do the normalize manually to use the min and max Depth (more stable)
        depthNorm.ConvertTo(depth, cv.MatType.CV_8U)

        depth = depth.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.result1 = Palette_Custom_Apply(depth, customColorMap)
        ocvb.result1.SetTo(0, trim.zeroMask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        trim.Dispose()
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
        If ocvb.parms.lowResolution Then Return depth32f.Resize(ocvb.color.Size())
        Return depth32f
    End Function
End Module


Public Class Depth_Colorizer_CPP : Implements IDisposable
    Public dst As New cv.Mat
    Public src As New cv.Mat
    Public externalUse As Boolean
    Dim dcPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        dcPtr = Depth_Colorizer_Open()
        ocvb.desc = "Display Depth image using C++ instead of VB.Net"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)

        If externalUse = False Then src = getDepth32f(ocvb) Else dst = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)

        Dim depthData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_Colorizer_Run(dcPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then
            If dst.Rows = 0 Then dst = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
            Dim dstData(dst.Total * dst.ElemSize - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            If externalUse = False Then
                ocvb.result1 = New cv.Mat(ocvb.result1.Rows, ocvb.result1.Cols, cv.MatType.CV_8UC3, dstData)
            End If

            dst = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, dstData)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Depth_Colorizer_Close(dcPtr)
    End Sub
End Class





Public Class Depth_ManualTrim : Implements IDisposable
    Public Mask As New cv.Mat
    Public sliders As New OptionsSliders
    Public externalUse As Boolean
    Public dst As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Min Depth", 200, 1000, 200)
        sliders.setupTrackBar2(ocvb, "Max Depth", 200, 10000, 1400)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Manually show depth with varying min and max depths."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If sliders.TrackBar1.Value >= sliders.TrackBar2.Value Then sliders.TrackBar2.Value = sliders.TrackBar1.Value + 1
        Dim minDepth = sliders.TrackBar1.Value
        Dim maxDepth = sliders.TrackBar2.Value
        dst = getDepth32f(ocvb)
        Mask = dst.Threshold(maxDepth, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()

        Dim maskMin As New cv.Mat
        maskMin = dst.Threshold(minDepth, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        cv.Cv2.BitwiseAnd(Mask, maskMin, Mask)

        If externalUse = False Then
            ocvb.result1.SetTo(0)
            ocvb.RGBDepth.CopyTo(ocvb.result1, Mask)
        Else
            Dim notMask As New cv.Mat
            cv.Cv2.BitwiseNot(Mask, notMask)
            dst.SetTo(0, notMask)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class






Public Class Depth_InRange : Implements IDisposable
    Public Mask As New cv.Mat
    Public zeroMask As New cv.Mat
    Public dst As New cv.Mat
    Public depth32f As New cv.Mat
    Public externalUse As Boolean
    Public sliders As New OptionsSliders
    Public minDepth As Double
    Public maxDepth As Double
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "InRange Min Depth", 200, 1000, 200)
        sliders.setupTrackBar2(ocvb, "InRange Max Depth", 200, 10000, 1400)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If sliders.TrackBar1.Value >= sliders.TrackBar2.Value Then sliders.TrackBar2.Value = sliders.TrackBar1.Value + 1
        minDepth = cv.Scalar.All(sliders.TrackBar1.Value)
        maxDepth = cv.Scalar.All(sliders.TrackBar2.Value)
        depth32f = getDepth32f(ocvb)
        cv.Cv2.InRange(depth32f, minDepth, maxDepth, Mask)
        cv.Cv2.BitwiseNot(Mask, zeroMask)
        dst = depth32f.Clone()
        dst.SetTo(0, zeroMask)

        If externalUse = False Then ocvb.result1 = dst.ConvertScaleAbs(255).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class






Public Class Depth_ColorizerFastFade_CPP : Implements IDisposable
    Dim trim As Depth_InRange
    Public dst As New cv.Mat
    Public src As New cv.Mat
    Public externalUse As Boolean
    Dim dcPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        dcPtr = Depth_Colorizer2_Open()

        trim = New Depth_InRange(ocvb)
        trim.externalUse = True

        ocvb.label2 = "Mask from Depth_InRange"
        ocvb.desc = "Display depth data with inrange trim.  Higher contrast than others - yellow to blue always present."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)

        If externalUse = False Then src = trim.dst Else dst = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        ocvb.result2 = trim.Mask
        Dim depthData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_Colorizer2_Run(dcPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, trim.maxDepth)
        handleSrc.Free()

        If imagePtr <> 0 Then
            If dst.Rows = 0 Then dst = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
            Dim dstData(dst.Total * dst.ElemSize - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            If externalUse = False Then
                ocvb.result1 = New cv.Mat(ocvb.result1.Rows, ocvb.result1.Cols, cv.MatType.CV_8UC3, dstData)
            Else
                dst = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, dstData)
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Depth_Colorizer2_Close(dcPtr)
        trim.Dispose()
    End Sub
End Class




' this algorithm is only intended to show how the depth can be colorized.  It is very slow.  Use the C++ version of this code nearby.
Public Class Depth_ColorizerVB : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
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
        ocvb.result1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, rgbdata)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class





Public Class Depth_ColorizerVB_MT : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim grid As Thread_Grid
    Public src As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Min Depth", 0, 1000, 0)
        sliders.setupTrackBar2(ocvb, "Max Depth", 1001, 10000, 4000)
        If ocvb.parms.ShowOptions Then sliders.Show()

        grid = New Thread_Grid(ocvb)
        grid.externalUse = True

        ocvb.desc = "Colorize depth manually with multi-threading."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        If externalUse = False Then src = getDepth32f(ocvb)
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
               ocvb.result1(roi) = New cv.Mat(depth.Rows, depth.Cols, cv.MatType.CV_8UC3, rgbdata)
           End Sub)

        End If
        ocvb.result1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        grid.Dispose()
    End Sub
End Class





Public Class Depth_Colorizer_MT : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim grid As Thread_Grid
    Public src As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Min Depth", 100, 1000, 100)
        sliders.setupTrackBar2(ocvb, "Max Depth", 1001, 10000, 4000)
        If ocvb.parms.ShowOptions Then sliders.Show()

        grid = New Thread_Grid(ocvb)
        grid.externalUse = True

        ocvb.desc = "Colorize normally uses CDF to stabilize the colors.  Just using sliders here - stabilized but not optimal range."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        If externalUse = False Then src = getDepth32f(ocvb)
        If src.Width <> ocvb.color.Width Then src = src.Resize(ocvb.color.Size())
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
             ocvb.result1(roi) = New cv.Mat(depth.Rows, depth.Cols, cv.MatType.CV_8UC3, rgbdata)
         End Sub)
        ocvb.result1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
        sliders.Dispose()
    End Sub
End Class





Public Class Depth_LocalMinMax_MT : Implements IDisposable
    Public grid As Thread_Grid
    Public ptListX() As Single
    Public ptListY() As Single
    Public pointList() As cv.Point2f
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        ocvb.callerName = "Depth_LocalMinMax_MT"
        grid = New Thread_Grid(ocvb)
        grid.externalUse = True

        ocvb.label1 = "Red is min distance"
        ocvb.desc = "Find min and max depth in each segment."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        Dim depth32f = getDepth32f(ocvb)

        Dim mask = depth32f.Threshold(1, 5000, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8UC1)

        If externalUse = False Then
            ocvb.color.CopyTo(ocvb.result1)
            ocvb.result1.SetTo(cv.Scalar.White, grid.gridMask)
        End If

        ReDim pointList(grid.roiList.Count - 1)
        ReDim ptListX(grid.roiList.Count - 1)
        ReDim ptListY(grid.roiList.Count - 1)
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim minVal As Double, maxVal As Double
            Dim minPt As cv.Point, maxPt As cv.Point
            cv.Cv2.MinMaxLoc(depth32f(roi), minVal, maxVal, minPt, maxPt, mask(roi))
            If minPt.X < 0 Or minPt.Y < 0 Then minPt = New cv.Point2f(0, 0)
            ptListX(i) = minPt.X + roi.X
            ptListY(i) = minPt.Y + roi.Y
            pointList(i) = New cv.Point(ptListX(i), ptListY(i))

            If externalUse = False Then
                cv.Cv2.Circle(ocvb.result1(roi), minPt, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                cv.Cv2.Circle(ocvb.result1(roi), maxPt, 5, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
            End If
        End Sub)


        If externalUse = False Then
            Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height))
            For i = 0 To ptListX.Count - 1
                If ptListX(i) <> 0 And ptListY(i) <> 0 Then subdiv.Insert(New cv.Point2f(ptListX(i), ptListY(i)))
            Next
            paint_voronoi(ocvb, ocvb.result2, subdiv)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
    End Sub
End Class





Public Class Depth_LocalMinMax_Kalman_MT : Implements IDisposable
    Dim minmax As Depth_LocalMinMax_MT
    Dim kalman As Kalman_Basics
    Public Sub New(ocvb As AlgorithmData)
        minmax = New Depth_LocalMinMax_MT(ocvb)
        minmax.externalUse = True
        minmax.grid.sliders.TrackBar1.Value = 32
        minmax.grid.sliders.TrackBar2.Value = 32
        ocvb.parms.ShowOptions = False

        ocvb.desc = "Find minimum depth in each segment."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static saveCount As Integer
        minmax.Run(ocvb)

        If minmax.grid.roiList.Count <> saveCount Then
            If kalman IsNot Nothing Then kalman.Dispose()
            kalman = New Kalman_Basics(ocvb)
            ReDim kalman.src(minmax.grid.roiList.Count - 1)
            saveCount = kalman.src.Count
            kalman.externalUse = True
        End If

        For i = 0 To kalman.src.Count - 1 Step 2
            kalman.src(i) = minmax.pointList(i).X
            kalman.src(i + 1) = minmax.pointList(i).Y
        Next
        kalman.Run(ocvb)

        ocvb.result1 = ocvb.color.Clone()
        ocvb.result1.SetTo(cv.Scalar.White, minmax.grid.gridMask)

        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height))
        For i = 0 To kalman.dst.Length - 1 Step 2
            If kalman.dst(i) >= ocvb.color.Width Then kalman.dst(i) = ocvb.color.Width - 1
            If kalman.dst(i) < 0 Then kalman.dst(i) = 0
            If kalman.dst(i + 1) >= ocvb.color.Height Then kalman.dst(i + 1) = ocvb.color.Height - 1
            If kalman.dst(i + 1) < 0 Then kalman.dst(i + 1) = 0
            subdiv.Insert(New cv.Point2f(kalman.dst(i), kalman.dst(i + 1)))

            ' just show the minimum (closest) point 
            cv.Cv2.Circle(ocvb.result1, New cv.Point(kalman.dst(i), kalman.dst(i + 1)), 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Next
        paint_voronoi(ocvb, ocvb.result2, subdiv)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        kalman.Dispose()
        minmax.Dispose()
    End Sub
End Class







Public Class Depth_Decreasing : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public externaluse As Boolean
    Public Increasing As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Threshold in millimeters", 0, 1000, 8)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Identify where depth is decreasing - coming toward the camera."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim depth32f = getDepth32f(ocvb)
        Static lastDepth As cv.Mat = depth32f.Clone()

        Dim thresholdCentimeters = sliders.TrackBar1.Value
        Dim diff As New cv.Mat
        If Increasing Then
            cv.Cv2.Subtract(depth32f, lastDepth, diff)
        Else
            cv.Cv2.Subtract(lastDepth, depth32f, diff)
        End If
        ocvb.result1 = diff.Threshold(thresholdCentimeters, 0, cv.ThresholdTypes.Tozero).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        lastDepth = depth32f
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class







Public Class Depth_Increasing : Implements IDisposable
    Dim depth As Depth_Decreasing
    Public Sub New(ocvb As AlgorithmData)
        depth = New Depth_Decreasing(ocvb)
        depth.externaluse = True
        depth.Increasing = True
        ocvb.desc = "Identify where depth is increasing - retreating from the camera."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        depth.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        depth.Dispose()
    End Sub
End Class








Public Class Depth_Punch : Implements IDisposable
    Dim depth As Depth_Decreasing
    Public Sub New(ocvb As AlgorithmData)
        depth = New Depth_Decreasing(ocvb)
        depth.externaluse = True
        ocvb.desc = "Identify the largest blob in the depth decreasing output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        depth.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        depth.Dispose()
    End Sub
End Class





Public Class Depth_ColorMap : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim Palette As Palette_ColorMap
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Depth ColorMap Alpha X100", 1, 100, 3)
        If ocvb.parms.ShowOptions Then sliders.Show()

        Palette = New Palette_ColorMap(ocvb)
        Palette.externalUse = True
        ocvb.desc = "Display the depth as a color map"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim alpha = sliders.TrackBar1.Value / 100
        cv.Cv2.ConvertScaleAbs(getDepth32f(ocvb), Palette.src, alpha)
        Palette.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        Palette.Dispose()
    End Sub
End Class



Public Class Depth_Holes : Implements IDisposable
    Public holeMask As New cv.Mat
    Public borderMask As New cv.Mat
    Public externalUse = False
    Dim element As New cv.Mat
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Amount of dilation around depth holes", 1, 10, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.label2 = "Shadow borders"
        element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(5, 5))
        ocvb.desc = "Identify holes in the depth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse Then sliders.Visible = False ' probably don't need this option except when running standalone.
        holeMask = getDepth32f(ocvb).Threshold(1, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()
        If externalUse = False Then ocvb.result1 = holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        borderMask = New cv.Mat
        borderMask = holeMask.Dilate(element, Nothing, sliders.TrackBar1.Value)
        borderMask.SetTo(0, holeMask)
        If externalUse = False Then
            ocvb.result2.SetTo(0)
            ocvb.RGBDepth.CopyTo(ocvb.result2, borderMask)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        holeMask.Dispose()
        borderMask.Dispose()
        element.Dispose()
        sliders.Dispose()
    End Sub
End Class
