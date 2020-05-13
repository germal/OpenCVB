Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Depth_Median
    Inherits ocvbClass
    Dim median As Math_Median_CDF
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        median = New Math_Median_CDF(ocvb, caller)
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
        dst2 = ocvb.color.EmptyClone.SetTo(0)
        ocvb.RGBDepth.CopyTo(dst2, mask)
        dst2.SetTo(0, zeroMask)
        label2 = "Median Depth > " + Format(median.medianVal, "#0.0")
    End Sub
End Class




Public Class Depth_Flatland
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "shadowRect Min Size", 1, 20000, 2000)

        shadow = New Depth_Holes(ocvb, caller)

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
                Dim nextColor = New cv.Scalar(ocvb.rColors(i Mod 255).Item0, ocvb.rColors(i Mod 255).Item1, ocvb.rColors(i Mod 255).Item2)
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        trim = New Depth_InRange(ocvb, caller)
        kalman = New Kalman_Basics(ocvb, caller)
        ReDim kalman.input(4 - 1) ' cv.rect...
        label1 = "Blue is current, red is kalman, green is trusted"
        ocvb.desc = "Demonstrate the use of mean shift algorithm.  Use depth to find the top of the head and then meanshift to the face."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        dst1 = trim.dst1.Clone()
        ' find the largest blob and use that as the body.  Head is highest in the image.
        Dim blobSize As New List(Of Int32)
        Dim blobLocation As New List(Of cv.Point)
        For y = 0 To trim.dst1.Height - 1
            For x = 0 To trim.dst1.Width - 1
                Dim nextByte = trim.dst1.Get(Of Byte)(y, x)
                If nextByte <> 0 Then
                    Dim count = trim.dst1.FloodFill(New cv.Point(x, y), 0)
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
            Dim rectSize = 50
            If ocvb.color.Width > 1000 Then rectSize = 250
            Dim xx = blobLocation.Item(maxIndex).X - rectSize / 2
            Dim yy = blobLocation.Item(maxIndex).Y
            If xx < 0 Then xx = 0
            If xx + rectSize / 2 > ocvb.color.Width Then xx = ocvb.color.Width - rectSize
            dst1 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

            kalman.input = {xx, yy, rectSize, rectSize}
            kalman.Run(ocvb)
            ocvb.drawRect = New cv.Rect(xx, yy, rectSize, rectSize)
            Dim kRect = New cv.Rect(kalman.output(0), kalman.output(1), kalman.output(2), kalman.output(3))
            dst1.Rectangle(kRect, cv.Scalar.Red, 2)
            dst1.Rectangle(ocvb.drawRect, cv.Scalar.Blue, 2)
            If Math.Abs(kRect.X - ocvb.drawRect.X) < rectSize / 4 And Math.Abs(kRect.Y - ocvb.drawRect.Y) < rectSize / 4 Then
                trustedRect = kRect
                dst1.Rectangle(trustedRect, cv.Scalar.Green, 5)
            End If
        End If
    End Sub
End Class






Public Class Depth_FlatData
    Inherits ocvbClass
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        shadow = New Depth_Holes(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "FlatData Region Count", 1, 250, 200)

        label1 = "Reduced resolution RGBDepth"
        label2 = "Contours of the Depth Shadow"
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        shadow = New Depth_Holes(ocvb, caller)
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
        ocvb.color.CopyTo(dst1, zeroMask)
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        xyzFrame = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32FC3)
        ocvb.desc = "Create 32-bit XYZ format from depth data (to slow to be useful.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim depth32f = (getDepth32f(ocvb) * 0.001).ToMat ' convert to meters.
        Dim xy As New cv.Point3f
        For xy.Y = 0 To xyzFrame.Height - 1
            For xy.X = 0 To xyzFrame.Width - 1
                xy.Z = depth32f.Get(Of Single)(xy.Y, xy.X)
                If xy.Z <> 0 Then
                    Dim w = getWorldCoordinatesD(ocvb, xy)
                    xyzFrame.Set(Of cv.Point3f)(xy.Y, xy.X, w)
                End If
            Next
        Next
        ocvb.putText(New ActiveClass.TrueType("OpenGL data prepared.", 10, 50, RESULT1))
    End Sub
End Class






Public Class Depth_WorldXYZ_MT
    Inherits ocvbClass
    Dim grid As Thread_Grid
    Dim trim As Depth_InRange
    Public xyzFrame As cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32

        trim = New Depth_InRange(ocvb, caller)

        xyzFrame = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32FC3)
        ocvb.desc = "Create OpenGL point cloud from depth data (too slow to be useful)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
                        Dim w = getWorldCoordinatesD(ocvb, xy)
                        xyzFrame.Set(Of cv.Point3f)(xy.Y, xy.X, w)
                    End If
                Next
            Next
        End Sub)
        ocvb.putText(New ActiveClass.TrueType("OpenGL data prepared.", 10, 50, RESULT1))
    End Sub
End Class




Public Class Depth_WorldXYZ_CPP
    Inherits ocvbClass
    Public pointCloud As cv.Mat
    Dim DepthXYZ As IntPtr
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        DepthXYZ = Depth_XYZ_OpenMP_Open(ocvb.parms.intrinsicsLeft.ppx, ocvb.parms.intrinsicsLeft.ppy,
                                         ocvb.parms.intrinsicsLeft.fx, ocvb.parms.intrinsicsLeft.fy)
        label1 = "xyzFrame is built"
        ocvb.desc = "Get the X, Y, Depth in the image coordinates (not the 3D image coordinates.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim depth32f = getDepth32f(ocvb) ' the C++ code will convert it to meters.
        Dim depthData(depth32f.Total * depth32f.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned) ' pin it for the duration...
        Marshal.Copy(depth32f.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_XYZ_OpenMP_Run(DepthXYZ, handleSrc.AddrOfPinnedObject(), depth32f.Rows, depth32f.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then pointCloud = New cv.Mat(depth32f.Rows, depth32f.Cols, cv.MatType.CV_32FC3, imagePtr)
        ocvb.putText(New ActiveClass.TrueType("OpenGL data prepared.", 10, 50, RESULT1))
    End Sub
    Public Sub Close()
        Depth_XYZ_OpenMP_Close(DepthXYZ)
    End Sub
End Class





Public Class Depth_MeanStdev_MT
    Inherits ocvbClass
    Dim grid As Thread_Grid
    Dim meanSeries As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        grid = New Thread_Grid(ocvb, caller)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 64

        sliders.setupTrackBar1(ocvb, caller, "MeanStdev Max Depth Range", 1, 20000, 3500)
        sliders.setupTrackBar2(ocvb, caller, "MeanStdev Frame Series", 1, 100, 5)
        ocvb.desc = "Collect a time series of depth and measure where the stdev is unstable.  Plan is to avoid depth where unstable."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        dst1 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8U)
        dst2 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8U)

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
            cv.Cv2.MinMaxLoc(meanValues, minVal, maxVal, minPt, maxPt, meanmask)
            Dim stdMask = stdValues.Threshold(0.001, maxDepth, cv.ThresholdTypes.Binary).ConvertScaleAbs() ' volatile region is x cm stdev.
            cv.Cv2.MinMaxLoc(stdValues, minStdVal, maxStdVal, minPt, maxPt, stdMask)

            Parallel.For(0, grid.roiList.Count - 1,
            Sub(i)
                Dim roi = grid.roiList(i)
                ' this marks all the regions where the depth is volatile.
                dst2(roi).SetTo(255 * (stdValues.Get(Of Single)(i, 0) - minStdVal) / (maxStdVal - minStdVal))
                dst2(roi).SetTo(0, outOfRangeMask(roi))

                dst1(roi).SetTo(255 * (meanValues.Get(Of Single)(i, 0) - minVal) / (maxVal - minVal))
                dst1(roi).SetTo(0, outOfRangeMask(roi))
            End Sub)
            cv.Cv2.BitwiseOr(dst2, grid.gridMask, dst2)
            label2 = "ROI Stdev: Min " + Format(minStdVal, "#0.0") + " Max " + Format(maxStdVal, "#0.0")
        End If

        label1 = "ROI Means: Min " + Format(minVal, "#0.0") + " Max " + Format(maxVal, "#0.0")
    End Sub
End Class


Public Class Depth_MeanStdevPlot
    Inherits ocvbClass
    Dim shadow As Depth_Holes
    Dim plot1 As Plot_OverTime
    Dim plot2 As Plot_OverTime
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        shadow = New Depth_Holes(ocvb, caller)

        plot1 = New Plot_OverTime(ocvb, caller)
        plot1.dst1 = dst1
        plot1.maxScale = 2000
        plot1.plotCount = 1

        plot2 = New Plot_OverTime(ocvb, caller)
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
        plot2.plotData = New cv.Scalar(stdev, 0, 0)
        plot2.Run(ocvb)
        label1 = "Plot of mean depth = " + Format(mean, "#0.0")
        label2 = "Plot of depth stdev = " + Format(stdev, "#0.0")
    End Sub
End Class




Public Class Depth_Uncertainty
    Inherits ocvbClass
    Dim retina As Retina_Basics_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        retina = New Retina_Basics_CPP(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "Uncertainty threshold", 1, 255, 100)

        ocvb.desc = "Use the bio-inspired retina algorithm to determine depth uncertainty."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        retina.src = ocvb.RGBDepth
        retina.Run(ocvb)
        dst2 = dst2.Threshold(sliders.TrackBar1.Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Public Class Depth_Palette
    Inherits ocvbClass
    Public trim As Depth_InRange
    Dim customColorMap As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        trim = New Depth_InRange(ocvb, caller)
        trim.sliders.TrackBar2.Value = 5000

        customColorMap = colorTransition(cv.Scalar.Blue, cv.Scalar.Yellow, 256)
        ocvb.desc = "Use a palette to display depth from the raw depth data.  Will it be faster Depth_Colorizer?  (Of course)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        trim.Run(ocvb)
        Dim minDepth = trim.sliders.TrackBar1.Value
        Dim maxDepth = trim.sliders.TrackBar2.Value

        Dim depthNorm = trim.depth32f
        depthNorm *= 255 / (maxDepth - minDepth) ' do the normalize manually to use the min and max Depth (more stable)
        Dim depth As New cv.Mat
        depthNorm.ConvertTo(depth, cv.MatType.CV_8U)

        depth = depth.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst1 = Palette_Custom_Apply(depth, customColorMap)
        dst1.SetTo(0, trim.zeroMask)
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


Public Class Depth_Colorizer_CPP
    Inherits ocvbClass
    Dim dcPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Min Depth", 200, 1000, 200)
        sliders.setupTrackBar2(ocvb, caller, "Max Depth", 200, 10000, 1400)
        ocvb.desc = "Manually show depth with varying min and max depths."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If sliders.TrackBar1.Value >= sliders.TrackBar2.Value Then sliders.TrackBar2.Value = sliders.TrackBar1.Value + 1
        Dim minDepth = sliders.TrackBar1.Value
        Dim maxDepth = sliders.TrackBar2.Value
        dst1 = getDepth32f(ocvb)
        Mask = dst1.Threshold(maxDepth, 255, cv.ThresholdTypes.BinaryInv).ConvertScaleAbs()

        Dim maskMin As New cv.Mat
        maskMin = dst1.Threshold(minDepth, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
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






Public Class Depth_InRange
    Inherits ocvbClass
    Public Mask As New cv.Mat
    Public zeroMask As New cv.Mat
    Public depth32f As New cv.Mat
    Public minDepth As Double
    Public maxDepth As Double
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "InRange Min Depth", 200, 1000, 200)
        sliders.setupTrackBar2(ocvb, caller, "InRange Max Depth", 200, 10000, 1400)
        ocvb.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If sliders.TrackBar1.Value >= sliders.TrackBar2.Value Then sliders.TrackBar2.Value = sliders.TrackBar1.Value + 1
        minDepth = cv.Scalar.All(sliders.TrackBar1.Value)
        maxDepth = cv.Scalar.All(sliders.TrackBar2.Value)
        depth32f = getDepth32f(ocvb)
        cv.Cv2.InRange(depth32f, minDepth, maxDepth, Mask)
        cv.Cv2.BitwiseNot(Mask, zeroMask)
        dst1 = Mask
        dst2 = zeroMask
        depth32f.SetTo(0, zeroMask)
    End Sub
End Class






Public Class Depth_ColorizerFastFade_CPP
    Inherits ocvbClass
    Dim trim As Depth_InRange
    Dim dcPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        dcPtr = Depth_Colorizer2_Open()

        trim = New Depth_InRange(ocvb, caller)

        label2 = "Mask from Depth_InRange"
        ocvb.desc = "Display depth data with inrange trim.  Higher contrast than others - yellow to blue always present."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Min Depth", 0, 1000, 0)
        sliders.setupTrackBar2(ocvb, caller, "Max Depth", 1001, 10000, 4000)

        grid = New Thread_Grid(ocvb, caller)

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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Min Depth", 100, 1000, 100)
        sliders.setupTrackBar2(ocvb, caller, "Max Depth", 1001, 10000, 4000)

        grid = New Thread_Grid(ocvb, caller)

        ocvb.desc = "Colorize normally uses CDF to stabilize the colors.  Just using sliders here - stabilized but not optimal range."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        If standalone Then src = getDepth32f(ocvb)
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
             dst1(roi) = New cv.Mat(depth.Rows, depth.Cols, cv.MatType.CV_8UC3, rgbdata)
         End Sub)
        dst1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class





Public Class Depth_LocalMinMax_MT
    Inherits ocvbClass
    Public grid As Thread_Grid
    Public ptListX() As Single
    Public ptListY() As Single
    Public pointList() As cv.Point2f
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        grid = New Thread_Grid(ocvb, caller)

        label1 = "Red is min distance"
        ocvb.desc = "Find min and max depth in each segment."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        Dim depth32f = getDepth32f(ocvb)

        Dim mask = depth32f.Threshold(1, 5000, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8UC1)

        If standalone Then
            ocvb.color.CopyTo(dst1)
            dst1.SetTo(cv.Scalar.White, grid.gridMask)
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

            If standalone Then
                cv.Cv2.Circle(dst1(roi), minPt, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                cv.Cv2.Circle(dst1(roi), maxPt, 5, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
            End If
        End Sub)


        If standalone Then
            Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height))
            For i = 0 To ptListX.Count - 1
                If ptListX(i) <> 0 And ptListY(i) <> 0 Then subdiv.Insert(New cv.Point2f(ptListX(i), ptListY(i)))
            Next
            paint_voronoi(ocvb, dst2, subdiv)
        End If
    End Sub
End Class





Public Class Depth_LocalMinMax_Kalman_MT
    Inherits ocvbClass
    Dim minmax As Depth_LocalMinMax_MT
    Dim kalman As Kalman_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        minmax = New Depth_LocalMinMax_MT(ocvb, caller)
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
            kalman = New Kalman_Basics(ocvb, caller)
            ReDim kalman.input(minmax.grid.roiList.Count - 1)
            saveCount = kalman.input.Count
        End If

        For i = 0 To kalman.input.Count - 1 Step 2
            kalman.input(i) = minmax.pointList(i).X
            kalman.input(i + 1) = minmax.pointList(i).Y
        Next
        kalman.Run(ocvb)

        dst1 = ocvb.color.Clone()
        dst1.SetTo(cv.Scalar.White, minmax.grid.gridMask)

        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height))
        For i = 0 To kalman.output.Length - 1 Step 2
            If kalman.output(i) >= ocvb.color.Width Then kalman.output(i) = ocvb.color.Width - 1
            If kalman.output(i) < 0 Then kalman.output(i) = 0
            If kalman.output(i + 1) >= ocvb.color.Height Then kalman.output(i + 1) = ocvb.color.Height - 1
            If kalman.output(i + 1) < 0 Then kalman.output(i + 1) = 0
            subdiv.Insert(New cv.Point2f(kalman.output(i), kalman.output(i + 1)))

            ' just show the minimum (closest) point
            cv.Cv2.Circle(dst1, New cv.Point(kalman.output(i), kalman.output(i + 1)), 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Next
        paint_voronoi(ocvb, dst2, subdiv)
    End Sub
End Class





Public Class Depth_Decreasing
    Inherits ocvbClass
    Public Increasing As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Threshold in millimeters", 0, 1000, 8)

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
        dst1 = diff.Threshold(thresholdCentimeters, 0, cv.ThresholdTypes.Tozero).Threshold(0, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs()
        lastDepth = depth32f
    End Sub
End Class





Public Class Depth_Increasing
    Inherits ocvbClass
    Dim depth As Depth_Decreasing
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        depth = New Depth_Decreasing(ocvb, caller)
        depth.Increasing = True
        ocvb.desc = "Identify where depth is increasing - retreating from the camera."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        depth.Run(ocvb)
    End Sub
End Class






Public Class Depth_Punch
    Inherits ocvbClass
    Dim depth As Depth_Decreasing
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        depth = New Depth_Decreasing(ocvb, caller)
        ocvb.desc = "Identify the largest blob in the depth decreasing output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        depth.Run(ocvb)
    End Sub
End Class





Public Class Depth_ColorMap
    Inherits ocvbClass
    Dim Palette As Palette_ColorMap
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Depth ColorMap Alpha X100", 1, 100, 3)

        Palette = New Palette_ColorMap(ocvb, caller)
        ocvb.desc = "Display the depth as a color map"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim alpha = sliders.TrackBar1.Value / 100
        cv.Cv2.ConvertScaleAbs(getDepth32f(ocvb), Palette.src, alpha)
        Palette.Run(ocvb)
    End Sub
End Class



Public Class Depth_Holes
    Inherits ocvbClass
    Public holeMask As New cv.Mat
    Public borderMask As New cv.Mat
    Dim element As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
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
            dst2 = ocvb.color.EmptyClone.SetTo(0)
            ocvb.RGBDepth.CopyTo(dst2, borderMask)
        End If
    End Sub
End Class





Public Class Depth_Stable
    Inherits ocvbClass
    Public mog As BGSubtract_Basics_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)

        ' sliders.setupTrackBar1(ocvb, caller, "")
        mog = New BGSubtract_Basics_CPP(ocvb, caller)

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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)

        mean = New Mean_Basics(ocvb, caller)
        colorize = New Depth_Colorizer_CPP(ocvb, caller)
        stable = New Depth_Stable(ocvb, caller)

        ocvb.desc = "Use the mask of stable depth (using RGBDepth) to stabilize the depth at any individual point."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        stable.Run(ocvb)

        mean.src = getDepth32f(ocvb)
        mean.src.SetTo(0, stable.dst1)
        mean.Run(ocvb)

        If standalone Then
            'colorize.src = mean.dst
            'colorize.Run(ocvb)
            'dst1 = colorize.dst
        End If
    End Sub
End Class
