Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Depth_ManualTrim : Implements IDisposable
    Public Mask As New cv.Mat
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Min Depth", 200, 1000, 200)
        sliders.setupTrackBar2(ocvb, "Max Depth", 200, 10000, 1400)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Manually show depth with varying min and max depths."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim minDepth = sliders.TrackBar1.Value
        Dim maxDepth = sliders.TrackBar2.Value
        Mask = ocvb.depth16.Threshold(maxDepth, 255, cv.ThresholdTypes.BinaryInv)
        Mask.ConvertTo(Mask, cv.MatType.CV_8U)

        Dim maskMin As New cv.Mat
        maskMin = ocvb.depth16.Threshold(minDepth, 255, cv.ThresholdTypes.Binary)
        maskMin.ConvertTo(maskMin, cv.MatType.CV_8U)
        cv.Cv2.BitwiseAnd(Mask, maskMin, Mask)

        ocvb.result1.SetTo(0)
        If ocvb.parms.lowResolution Then Mask = Mask.Resize(ocvb.color.Size())
        ocvb.RGBDepth.CopyTo(ocvb.result1, Mask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class


Public Class Depth_Projections : Implements IDisposable
    Dim foreground As Depth_ManualTrim
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 64
        grid.sliders.TrackBar2.Value = 32

        foreground = New Depth_ManualTrim(ocvb)
        foreground.sliders.TrackBar1.Value = 300  ' fixed distance to keep the images stable.
        foreground.sliders.TrackBar2.Value = 1200 ' fixed distance to keep the images stable.
        ocvb.label1 = "Top View"
        ocvb.label2 = "Side View"
        ocvb.desc = "Project the depth data onto a top view and side view."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        foreground.Run(ocvb)

        ocvb.result1.SetTo(cv.Scalar.White)
        ocvb.result2.SetTo(cv.Scalar.White)

        Dim h = ocvb.result1.Height
        Dim w = ocvb.result1.Width
        Dim desiredMin = foreground.sliders.TrackBar1.Value
        Dim desiredMax = foreground.sliders.TrackBar2.Value
        Dim range = desiredMax - desiredMin

        Parallel.ForEach(Of cv.Rect)(grid.roiList,
         Sub(roi)
             For y = roi.Y To roi.Y + roi.Height - 1
                 For x = roi.X To roi.X + roi.Width - 1
                     Dim m = foreground.Mask.At(Of Byte)(y, x)
                     If m > 0 Then
                         Dim depth = ocvb.depth16.Get(Of UShort)(y, x)
                         If depth > 0 Then
                             Dim dy = Math.Round(h * (depth - desiredMin) / range)
                             If dy < h And dy > 0 Then ocvb.result1.Set(Of cv.Vec3b)(h - dy, x, ocvb.color.At(Of cv.Vec3b)(y, x))
                             Dim dx = Math.Round(w * (depth - desiredMin) / range)
                             If dx < w And dx > 0 Then ocvb.result2.Set(Of cv.Vec3b)(y, dx, ocvb.color.At(Of cv.Vec3b)(y, x))
                         End If
                     End If
                 Next
             Next
         End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        foreground.Dispose()
        grid.Dispose()
    End Sub
End Class


Public Class Depth_WorldXYZ_MT : Implements IDisposable
    Dim grid As Thread_Grid
    Public xyzFrame As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32
        xyzFrame = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32FC3)
        ocvb.desc = "Create 32-bit XYZ format from depth data."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        xyzFrame.SetTo(0)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim xy As New cv.Point3f
            For xy.Y = roi.Y To roi.Y + roi.Height - 1
                For xy.X = roi.X To roi.X + roi.Width - 1
                    xy.Z = ocvb.depth16.At(Of UInt16)(xy.Y, xy.X)
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
        ocvb.depth16.ConvertTo(median.src, cv.MatType.CV_32F)
        median.Run(ocvb)

        Dim mask As cv.Mat
        mask = median.src.LessThan(median.medianVal)
        ocvb.result1.SetTo(0)
        ocvb.RGBDepth.CopyTo(ocvb.result1, mask)

        Dim zeroMask = ocvb.depth16.Equals(0)
        cv.Cv2.ConvertScaleAbs(zeroMask, zeroMask.ToMat)
        If ocvb.parms.lowResolution Then zeroMask = zeroMask.ToMat.Resize(ocvb.result1.Size())
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
        Dim mask = ocvb.depth16.Threshold(1, 20000, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8UC1)
        Dim minVal As Double, maxVal As Double
        Dim minPt As cv.Point, maxPt As cv.Point
        cv.Cv2.MinMaxLoc(ocvb.depth16, minVal, maxVal, minPt, maxPt, mask)
        ocvb.RGBDepth.CopyTo(ocvb.result1)
        ocvb.RGBDepth.CopyTo(ocvb.result2)
        ocvb.label1 = "Min Depth " + CStr(minVal) + " mm"
        ocvb.result1.Circle(minPt, 10, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        ocvb.label2 = "Max Depth " + CStr(maxVal) + " mm"
        ocvb.result2.Circle(maxPt, 10, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class



Public Class Depth_Holes : Implements IDisposable
    Public holeMask As New cv.Mat
    Public borderMask As New cv.Mat
    Public externalUse = False
    Dim element As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label2 = "Shadow borders"
        element = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(5, 5))
        ocvb.desc = "Identify holes in the depth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mask = ocvb.depth16.Threshold(1, 20000, cv.ThresholdTypes.BinaryInv)
        If ocvb.parms.lowResolution Then mask = mask.Resize(ocvb.color.Size)
        holeMask = New cv.Mat
        mask.ConvertTo(holeMask, cv.MatType.CV_8UC1)
        If externalUse = False Then ocvb.result1 = holeMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        borderMask = New cv.Mat
        borderMask = holeMask.Dilate(element, Nothing, 1)
        borderMask.SetTo(0, holeMask)
        If externalUse = False Then ocvb.result2 = borderMask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        holeMask.Dispose()
        borderMask.Dispose()
        element.Dispose()
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
        Dim gray = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        gray = gray.Threshold(1, 255, cv.ThresholdTypes.Binary)

        ' find the largest blob and use that as the body.  Head is highest in the image.
        Dim blobSize As New List(Of Int32)
        Dim blobLocation As New List(Of cv.Point)
        For y = 0 To gray.Height - 1
            For x = 0 To gray.Width - 1
                Dim nextByte = gray.At(Of Byte)(y, x)
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



Public Class Depth_ToLeftView : Implements IDisposable
    Dim red As LeftRightView_Basics
    Dim sliders As New OptionsSliders
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData)
        red = New LeftRightView_Basics(ocvb)

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Save Current Yellow Rectangle"
        If ocvb.parms.ShowOptions Then check.Show()

        Dim top As Int32, left As Int32, bot As Int32, right As Int32
        If ocvb.parms.cameraIndex = D400Cam Then
            top = GetSetting("OpenCVB", "DepthToLeftViewTop", "DepthToLeftViewTop", 1 / 8)
            left = GetSetting("OpenCVB", "DepthToInfraleftView", "DepthToInfraleftView", 1 / 8)
            bot = GetSetting("OpenCVB", "DepthToLeftViewBot", "DepthToLeftViewBot", 7 / 8)
            right = GetSetting("OpenCVB", "DepthToInfrarightView", "DepthToInfrarightView", 7 / 8)
        Else
            top = 0
            left = 0
            bot = 1
            right = 1
        End If
        sliders.setupTrackBar1(ocvb, "Color Image Top in leftView", 0, ocvb.color.Height / 2, top * ocvb.color.Height)
        sliders.setupTrackBar2(ocvb, "Color Image Left in leftView", 0, ocvb.color.Width / 2, left * ocvb.color.Width)
        sliders.setupTrackBar3(ocvb, "Color Image Bot in leftView", ocvb.color.Height / 2, ocvb.color.Height, bot * ocvb.color.Height)
        sliders.setupTrackBar4(ocvb, "Color Image Right in leftView", ocvb.color.Width / 2, ocvb.color.Width, right * ocvb.color.Width)
        If ocvb.parms.ShowOptions Then sliders.Show()
        Select Case ocvb.parms.cameraIndex
            Case D400Cam, StereoLabsZED2
                ocvb.label1 = "Color + LeftView (overlay)"
                ocvb.label2 = "Aligned leftView and color"
            Case Kinect4AzureCam
                ocvb.label1 = "Aligning LeftView with color is not useful on Kinect"
                ocvb.label2 = ""
            Case T265Camera
                ocvb.label1 = "Aligning LeftView with color is not useful on T265"
                ocvb.label2 = ""
        End Select
        ocvb.desc = "Map the depth image into the LeftView images"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex = D400Cam Or ocvb.parms.cameraIndex = StereoLabsZED2 Then
            red.Run(ocvb)
            If ocvb.result1.Size <> ocvb.color.Size Then ocvb.result1 = ocvb.result1.Resize(ocvb.color.Size())
            ocvb.result2 = ocvb.result1
            Dim rHeight = sliders.TrackBar3.Value - sliders.TrackBar1.Value
            Dim rWidth = sliders.TrackBar4.Value - sliders.TrackBar2.Value
            Dim rect = New cv.Rect(sliders.TrackBar2.Value, sliders.TrackBar1.Value, rWidth, rHeight)
            ocvb.result2.Rectangle(rect, cv.Scalar.Yellow, 1)
            ocvb.result1 = ocvb.result2(rect).Resize(ocvb.result1.Size())
            If ocvb.result1.Channels = 1 Then ocvb.result1 = ocvb.result1.CvtColor(OpenCvSharp.ColorConversionCodes.GRAY2BGR)
            ocvb.result1 = ocvb.color + ocvb.result1
            If check.Box(0).Checked Then
                check.Box(0).Checked = False
                SaveSetting("OpenCVB", "DepthToLeftViewTop", "DepthToLeftViewTop", sliders.TrackBar1.Value / ocvb.color.Height)
                SaveSetting("OpenCVB", "DepthToInfraleftView", "DepthToInfraleftView", sliders.TrackBar2.Value / ocvb.color.Width)
                SaveSetting("OpenCVB", "DepthToLeftViewBot", "DepthToLeftViewBot", sliders.TrackBar3.Value / ocvb.color.Height)
                SaveSetting("OpenCVB", "DepthToInfrarightView", "DepthToInfrarightView", sliders.TrackBar4.Value / ocvb.color.Width)
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        red.Dispose()
        sliders.Dispose()
        check.Dispose()
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
        If ocvb.parms.lowResolution Then mask = mask.Resize(ocvb.depth16.Size())
        gray = ocvb.depth16.Normalize(0, 255, cv.NormTypes.MinMax, -1, mask)
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
        Dim tmp16 As New cv.Mat
        cv.Cv2.InRange(ocvb.depth16, 0, maxDepth, tmp16)
        cv.Cv2.ConvertScaleAbs(tmp16, mask)

        Dim zeroMask As New cv.Mat
        cv.Cv2.BitwiseNot(mask, zeroMask)
        ocvb.depth16.SetTo(0, zeroMask)

        ocvb.result1.SetTo(0)
        If ocvb.parms.lowResolution Then mask = mask.Resize(ocvb.color.Size())
        ocvb.RGBDepth.CopyTo(ocvb.result1, mask)
        If ocvb.parms.lowResolution Then zeroMask = zeroMask.Resize(ocvb.color.Size())
        zeroMask.SetTo(255, shadow.holeMask)
        ocvb.color.CopyTo(ocvb.result1, zeroMask)
        If ocvb.parms.lowResolution Then zeroMask = zeroMask.Resize(ocvb.depth16.Size())
        ocvb.depth16.SetTo(maxDepth, zeroMask) ' set the depth to the maxdepth for any background
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        shadow.Dispose()
    End Sub
End Class


' Use the C++ version of this algorithm - this is way too slow...
Public Class Depth_WorldXYZ : Implements IDisposable
    Public xyzFrame As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        xyzFrame = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32FC3)
        ocvb.desc = "Create 32-bit XYZ format from depth data."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim xy As New cv.Point3f
        For xy.Y = 0 To xyzFrame.Height - 1
            For xy.X = 0 To xyzFrame.Width - 1
                xy.Z = ocvb.depth16.At(Of UInt16)(xy.Y, xy.X)
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


Public Class Depth_XYZ_OpenMP_CPP : Implements IDisposable
    Public pointCloud As cv.Mat
    Dim DepthXYZ As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "xyzFrame is built"
        ocvb.desc = "Get the X, Y, Depth in the image coordinates (not the 3D image coordinates.)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ' can't do this in the constructor because intrinsics were not initialized yet (because zinput was not initialized until algorithm thread starts.
        If ocvb.frameCount = 0 Then DepthXYZ = Depth_XYZ_OpenMP_Open(ocvb.parms.intrinsicsLeft.ppx, ocvb.parms.intrinsicsLeft.ppy, ocvb.parms.intrinsicsLeft.fx, ocvb.parms.intrinsicsLeft.fy)

        Dim depthData(ocvb.depth16.Total * ocvb.depth16.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned) ' pin it for the duration...
        Marshal.Copy(ocvb.depth16.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_XYZ_OpenMP_Run(DepthXYZ, handleSrc.AddrOfPinnedObject(), ocvb.depth16.Rows, ocvb.depth16.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(ocvb.depth16.Total * 3 * 4 - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            pointCloud = New cv.Mat(ocvb.depth16.Rows, ocvb.depth16.Cols, cv.MatType.CV_32FC3, dstData)
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
        cv.Cv2.InRange(ocvb.depth16, 1, maxDepth, tmp16)
        cv.Cv2.ConvertScaleAbs(tmp16, mask)
        Dim outOfRangeMask As New cv.Mat
        cv.Cv2.BitwiseNot(mask, outOfRangeMask)

        Dim minVal As Double, maxVal As Double
        Dim minPt As cv.Point, maxPt As cv.Point
        cv.Cv2.MinMaxLoc(ocvb.depth16, minVal, maxVal, minPt, maxPt, mask)

        Dim meanIndex = ocvb.frameCount Mod meanCount
        Dim meanValues As New cv.Mat(grid.roiList.Count - 1, 1, cv.MatType.CV_32F)
        Dim stdValues As New cv.Mat(grid.roiList.Count - 1, 1, cv.MatType.CV_32F)
        Parallel.For(0, grid.roiList.Count - 1,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim mean As Single = 0, stdev As Single = 0
            cv.Cv2.MeanStdDev(ocvb.depth16(roi), mean, stdev, mask(roi))
            meanSeries.Set(Of Single)(i, meanIndex, mean)
            If ocvb.frameCount >= meanCount - 1 Then
                cv.Cv2.MeanStdDev(meanSeries.Row(i), mean, stdev)
                meanValues.Set(Of Single)(i, 0, mean)
                stdValues.Set(Of Single)(i, 0, stdev)
            End If
        End Sub)

        If ocvb.frameCount >= meanCount Then
            Dim minStdVal As Double, maxStdVal As Double
            Dim meanMask As New cv.Mat, stdMask As New cv.Mat

            cv.Cv2.Threshold(meanValues, meanMask, 1, maxDepth, cv.ThresholdTypes.Binary)
            meanMask.ConvertTo(meanMask, cv.MatType.CV_8U)
            cv.Cv2.MinMaxLoc(meanValues, minVal, maxVal, minPt, maxPt, meanMask)
            cv.Cv2.Threshold(stdValues, stdMask, 0.001, maxDepth, cv.ThresholdTypes.Binary) ' volatile region is x cm stdev.
            stdMask.ConvertTo(stdMask, cv.MatType.CV_8U)
            cv.Cv2.MinMaxLoc(stdValues, minStdVal, maxStdVal, minPt, maxPt, stdMask)

            Parallel.For(0, grid.roiList.Count - 1,
            Sub(i)
                Dim roi = grid.roiList(i)
                ' this marks all the regions where the depth is volatile.
                ocvb.result2(roi).SetTo(255 * (stdValues.At(Of Single)(i, 0) - minStdVal) / (maxStdVal - minStdVal))
                ocvb.result2(roi).SetTo(0, outOfRangeMask(roi))

                ocvb.result1(roi).SetTo(255 * (meanValues.At(Of Single)(i, 0) - minVal) / (maxVal - minVal))
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
        If ocvb.parms.lowResolution Then mask = mask.Resize(ocvb.depth16.Size())
        cv.Cv2.MeanStdDev(ocvb.depth16, mean, stdev, mask)

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
        cv.Cv2.ConvertScaleAbs(ocvb.depth16, Palette.src, alpha)
        Palette.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        Palette.Dispose()
    End Sub
End Class




Public Class Depth_Stable : Implements IDisposable
    Dim mog As BGSubtract_Basics_CPP
    Public Sub New(ocvb As AlgorithmData)
        mog = New BGSubtract_Basics_CPP(ocvb)
        mog.radio.check(4).Checked = True
        mog.externalUse = True

        ocvb.label2 = "Stable (non-zero) Depth"
        ocvb.desc = "Collect X frames, compute stable depth and color pixels using thresholds"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        mog.src = ocvb.RGBDepth
        mog.Run(ocvb)
        cv.Cv2.BitwiseNot(ocvb.result1, ocvb.result2)
        ocvb.label1 = "Stable (non-zero) Depth" + " using " + mog.radio.check(mog.currMethod).Text + " method"
        Dim zeroDepth = ocvb.depth16.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        zeroDepth = zeroDepth.ConvertScaleAbs(1)
        Dim mask = ocvb.result1.Clone()
        ocvb.result1 = ocvb.color.Clone()
        ocvb.result1.SetTo(0, mask)
        If ocvb.color.Size <> zeroDepth.Size Then zeroDepth = zeroDepth.Resize(ocvb.color.Size) ' depth is always at full resolution.
        ocvb.result1.SetTo(0, zeroDepth)
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

        Dim depthNorm16 = ocvb.depth16
        depthNorm16 *= 255 / (maxDepth - minDepth) ' do the normalize manually to use the min and max Depth (more stable
        depthNorm16.ConvertTo(depth, cv.MatType.CV_8U)
        depth = depth.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.result1 = Palette_ApplyCustom(depth, customColorMap)
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
End Module


Public Class Depth_Colorizer_1_CPP : Implements IDisposable
    Public dst As New cv.Mat
    Public src As New cv.Mat
    Public externalUse As Boolean
    Dim dcPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        dcPtr = Depth_Colorizer_Open()
        ocvb.desc = "Display 16 bit image using C++ instead of VB.Net"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then src = ocvb.depth16 Else dst = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)

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
                If ocvb.parms.lowResolution Then
                    Dim tmp = New cv.Mat(ocvb.depth16.Rows, ocvb.depth16.Cols, cv.MatType.CV_8UC3, dstData)
                    ocvb.result1 = tmp.Resize(ocvb.result1.Size())
                Else
                    ocvb.result1 = New cv.Mat(ocvb.result1.Rows, ocvb.result1.Cols, cv.MatType.CV_8UC3, dstData)
                End If
            End If

            dst = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, dstData)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Depth_Colorizer_Close(dcPtr)
    End Sub
End Class






Public Class Depth_InRange : Implements IDisposable
    Public Mask As New cv.Mat
    Public zeroMask As New cv.Mat
    Public dst As New cv.Mat
    Public externalUse As Boolean
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "InRange Min Depth", 200, 1000, 200)
        sliders.setupTrackBar2(ocvb, "InRange Max Depth", 200, 10000, 1400)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Show depth with OpenCV using varying min and max depths."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If sliders.TrackBar1.Value >= sliders.TrackBar2.Value Then sliders.TrackBar2.Value = sliders.TrackBar1.Value + 1
        Dim minDepth = cv.Scalar.All(sliders.TrackBar1.Value)
        Dim maxDepth = cv.Scalar.All(sliders.TrackBar2.Value)
        cv.Cv2.InRange(ocvb.depth16, minDepth, maxDepth, Mask)
        cv.Cv2.BitwiseNot(Mask, zeroMask)
        dst = ocvb.depth16.Clone()
        dst.SetTo(0, zeroMask)

        If externalUse = False Then
            ocvb.result1.SetTo(0)
            If ocvb.RGBDepth.Width <> Mask.Width Then Mask = Mask.Resize(ocvb.RGBDepth.Size())
            ocvb.RGBDepth.CopyTo(ocvb.result1, Mask)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class






Public Class Depth_Colorizer_2_CPP : Implements IDisposable
    Dim inrange As Depth_InRange
    Public dst As New cv.Mat
    Public src As New cv.Mat
    Public externalUse As Boolean
    Dim dcPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        dcPtr = Depth_Colorizer2_Open()

        inrange = New Depth_InRange(ocvb)
        inrange.sliders.TrackBar2.Value = 4000 ' a better default
        inrange.externalUse = True

        ocvb.desc = "Display 16-bit depth data with inrange trim.  Higher contrast than others - yellow to blue always present."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        inrange.Run(ocvb)
        Dim minDepth = inrange.sliders.TrackBar1.Value
        Dim maxDepth = inrange.sliders.TrackBar2.Value
        Dim histSize = maxDepth - minDepth

        If externalUse = False Then src = inrange.dst Else dst = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)

        Dim depthData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(depthData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, depthData, 0, depthData.Length)
        Dim imagePtr = Depth_Colorizer2_Run(dcPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, histSize)
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
        inrange.Dispose()
    End Sub
End Class




' this algorithm is only intended to show how the depth can be colorized.  It is very slow.  Use the C++ version of this code nearby.
Public Class Depth_ColorizerVB : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Colorize depth manually."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim src = ocvb.depth16
        Dim nearColor = New Byte() {0, 255, 255}
        Dim farColor = New Byte() {255, 0, 0}

        Dim histogram(256 * 256 - 1) As Int32
        For y = 0 To src.Rows - 1
            For x = 0 To src.Cols - 1
                Dim pixel = src.Get(Of UInt16)(y, x)
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
                Dim pixel = src.Get(Of UInt16)(y, x)
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
        sliders.setupTrackBar1(ocvb, "Min Depth", 100, 1000, 100)
        sliders.setupTrackBar2(ocvb, "Max Depth", 1001, 10000, 4000)
        If ocvb.parms.ShowOptions Then sliders.Show()

        grid = New Thread_Grid(ocvb)
        grid.externalUse = True

        ocvb.desc = "Colorize depth manually."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        If externalUse = False Then src = ocvb.depth16
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
        Marshal.Copy(hist.Data, histogram, 0, histogram.Length - 1)
        For i = 1 To histogram.Length - 1
            histogram(i) += histogram(i - 1)
        Next
        Dim maxHist = histogram(histSize - 1)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
       Sub(roi)
           Dim depth = src(roi)
           Dim rgbdata(src.Total) As cv.Vec3b
           Dim rgbIndex As Int32
           For y = 0 To depth.Rows - 1
               For x = 0 To depth.Cols - 1
                   Dim pixel = depth.Get(Of UInt16)(y, x)
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

        If externalUse = False Then src = ocvb.depth16
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
                     Dim pixel = depth.Get(Of UInt16)(y, x)
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
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.externalUse = True

        ocvb.label1 = "Red is min distance, Blue is max distance"
        ocvb.desc = "Find min and max depth in each segment."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        Dim mask = ocvb.depth16.Threshold(1, 5000, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8UC1)

        ocvb.color.CopyTo(ocvb.result1)
        'ocvb.result1.SetTo(0, mask)
        ocvb.result1.SetTo(cv.Scalar.White, grid.gridMask)

        ReDim ptListX(grid.roiList.Count - 1)
        ReDim ptListY(grid.roiList.Count - 1)
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            Dim minVal As Double, maxVal As Double
            Dim minPt As cv.Point, maxPt As cv.Point
            cv.Cv2.MinMaxLoc(ocvb.depth16(roi), minVal, maxVal, minPt, maxPt, mask(roi))
            If minPt.X < 0 Or minPt.Y < 0 Then minPt = New cv.Point2f(0, 0)
            ptListX(i) = minPt.X + roi.X
            ptListY(i) = minPt.Y + roi.Y

            cv.Cv2.Circle(ocvb.result1(roi), minPt, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            'cv.Cv2.Circle(ocvb.result1(roi), maxPt, 5, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
            'ptList(i * 2 + 1) = New cv.Point2f(maxPt.X + roi.X, maxPt.Y + roi.Y)
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
    Dim kalmanX As Kalman_GeneralPurpose
    Dim kalmanY As Kalman_GeneralPurpose
    Public Sub New(ocvb As AlgorithmData)
        minmax = New Depth_LocalMinMax_MT(ocvb)
        minmax.externalUse = True
        minmax.grid.sliders.TrackBar1.Value = 32
        minmax.grid.sliders.TrackBar2.Value = 32
        ocvb.parms.ShowOptions = False

        ocvb.desc = "Find minimum depth in each segment."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static gridWidth As Int32
        Static gridHeight As Int32
        minmax.Run(ocvb)

        If gridWidth <> minmax.grid.sliders.TrackBar1.Value Or gridHeight <> minmax.grid.sliders.TrackBar2.Value Then
            If kalmanX IsNot Nothing Then kalmanX.Dispose()
            If kalmanY IsNot Nothing Then kalmanY.Dispose()
            gridWidth = minmax.grid.sliders.TrackBar1.Value
            gridHeight = minmax.grid.sliders.TrackBar2.Value
            kalmanX = New Kalman_GeneralPurpose(ocvb)
            kalmanX.externalUse = True
            kalmanY = New Kalman_GeneralPurpose(ocvb)
            kalmanY.externalUse = True
        End If

        kalmanX.src = minmax.ptListX
        kalmanX.Run(ocvb)
        kalmanY.src = minmax.ptListY
        kalmanY.Run(ocvb)

        Dim subdiv As New cv.Subdiv2D(New cv.Rect(0, 0, ocvb.color.Width, ocvb.color.Height))
        For i = 0 To kalmanX.dst.Length - 1
            If kalmanX.dst(i) >= ocvb.color.Width Then kalmanX.dst(i) = ocvb.color.Width - 1
            If kalmanX.dst(i) < 0 Then kalmanX.dst(i) = 0
            If kalmanY.dst(i) >= ocvb.color.Height Then kalmanY.dst(i) = ocvb.color.Height - 1
            If kalmanY.dst(i) < 0 Then kalmanY.dst(i) = 0
            subdiv.Insert(New cv.Point2f(kalmanX.dst(i), kalmanY.dst(i)))
        Next
        paint_voronoi(ocvb, ocvb.result2, subdiv)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        kalmanX.Dispose()
        kalmanY.Dispose()
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
        Dim depth32f As New cv.Mat
        ocvb.depth16.ConvertTo(depth32f, cv.MatType.CV_32F)
        Static lastDepth As cv.Mat = depth32f.Clone()

        Dim thresholdCentimeters = sliders.TrackBar1.Value
        Dim diff As New cv.Mat
        If Increasing Then
            cv.Cv2.Subtract(depth32f, lastDepth, diff)
        Else
            cv.Cv2.Subtract(lastDepth, depth32f, diff)
        End If
        diff = diff.Threshold(thresholdCentimeters, 0, cv.ThresholdTypes.Tozero)
        diff = diff.Threshold(0, 255, cv.ThresholdTypes.Binary)
        ocvb.result1 = diff.ConvertScaleAbs()
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