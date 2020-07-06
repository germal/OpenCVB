Imports cv = OpenCvSharp
Imports System.Threading
Public Class FloodFill_Basics
    Inherits ocvbClass
    Public masks As New List(Of cv.Mat)
    Public maskSizes As New SortedList(Of Int32, Int32)(New CompareMaskSize)
    Public maskRects As New List(Of cv.Rect)

    Public initialMask As New cv.Mat
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public minFloodSize As Integer
    Public Class CompareMaskSize : Implements IComparer(Of Int32)
        Public Function Compare(ByVal a As Int32, ByVal b As Int32) As Integer Implements IComparer(Of Int32).Compare
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "FloodFill Minimum Size", 1, 5000, 2500)
        sliders.setupTrackBar2("FloodFill LoDiff", 1, 255, 5)
        sliders.setupTrackBar3("FloodFill HiDiff", 1, 255, 5)
        sliders.setupTrackBar4("Step Size", 1, ocvb.color.cols / 2, 20)

        label1 = "Input image to floodfill"
        ocvb.desc = "Use floodfill to build image segments in a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        minFloodSize = sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(sliders.TrackBar3.Value)
        Dim stepSize = sliders.TrackBar4.Value

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src
        Dim maskPlus = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8UC1)
        Dim maskRect = New cv.Rect(1, 1, maskPlus.Width - 2, maskPlus.Height - 2)
        initialMask = src.EmptyClone().SetTo(0)

        masks.Clear()
        maskSizes.Clear()
        maskRects.Clear()

        maskPlus.SetTo(0)
        Dim ignoreMasks = initialMask.Clone()

        Dim gray = src.Clone()
        For y = 0 To gray.Height - 1 Step stepSize
            For x = 0 To gray.Width - 1 Step stepSize
                If gray.Get(Of Byte)(y, x) > 0 Then
                    Dim rect As New cv.Rect
                    Dim count = cv.Cv2.FloodFill(gray, maskPlus, New cv.Point(x, y), cv.Scalar.White, rect, loDiff, hiDiff, floodFlag Or (255 << 8))
                    If count > minFloodSize Then
                        masks.Add(maskPlus(maskRect).Clone().SetTo(0, ignoreMasks))
                        masks(masks.Count - 1).SetTo(0, initialMask) ' The initial mask is what should not be part of any mask.
                        maskSizes.Add(masks(masks.Count - 1).CountNonZero(), masks.Count - 1)
                        maskRects.Add(rect)
                    End If
                    ' Mask off any object that is too small or previously identified
                    cv.Cv2.BitwiseOr(ignoreMasks, maskPlus(maskRect), ignoreMasks)
                End If
            Next
        Next

        dst2.SetTo(0)
        For i = 0 To masks.Count - 1
            Dim maskIndex = maskSizes.ElementAt(i).Value
            Dim nextColor = scalarColors(i Mod 255)
            dst2.SetTo(nextColor, masks(maskIndex))
        Next
        label2 = CStr(masks.Count) + " regions > " + CStr(minFloodSize) + " pixels"
    End Sub
End Class





Public Class FloodFill_Top16_MT
    Inherits ocvbClass
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "FloodFill Minimum Size", 1, 5000, 2000)
        sliders.setupTrackBar2("FloodFill LoDiff", 1, 255, 5)
        sliders.setupTrackBar3("FloodFill HiDiff", 1, 255, 5)

        ocvb.desc = "Use floodfill to build image segments with a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim minFloodSize = sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(sliders.TrackBar3.Value)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src.Clone()
        grid.Run(ocvb)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            For y = roi.Y To roi.Y + roi.Height - 1
                For x = roi.X To roi.X + roi.Width - 1
                    Dim nextByte = src.Get(Of Byte)(y, x)
                    If nextByte <> 255 And nextByte > 0 Then
                        Dim count = cv.Cv2.FloodFill(src, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange)
                        If count > minFloodSize Then
                            count = cv.Cv2.FloodFill(dst1, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange)
                        End If
                    End If
                Next
            Next
        End Sub)
    End Sub
End Class




Public Class FloodFill_Color_MT
    Inherits ocvbClass
    Dim flood As FloodFill_Top16_MT
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)
        flood = New FloodFill_Top16_MT(ocvb)

        ocvb.desc = "Use floodfill to build image segments in an RGB image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim minFloodSize = flood.sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(flood.sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(flood.sliders.TrackBar3.Value)

        dst1 = src.Clone()
        grid.Run(ocvb)
        Dim vec255 = New cv.Vec3b(255, 255, 255)
        Dim vec0 = New cv.Vec3b(0, 0, 0)
        Dim regionCount As Integer = 0
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            For y = roi.Y To roi.Y + roi.Height - 1
                For x = roi.X To roi.X + roi.Width - 1
                    Dim vec = src.Get(Of cv.Vec3b)(y, x)
                    If vec <> vec255 And vec <> vec0 Then
                        Dim count = cv.Cv2.FloodFill(src, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange + cv.FloodFillFlags.Link4)
                        If count > minFloodSize Then
                            Interlocked.Increment(regionCount)
                            count = cv.Cv2.FloodFill(dst1, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange + cv.FloodFillFlags.Link4)
                        End If
                    End If
                Next
            Next
        End Sub)
        label1 = CStr(regionCount) + " regions were filled with Floodfill"
    End Sub
End Class




Public Class FloodFill_DCT
    Inherits ocvbClass
    Dim flood As FloodFill_Color_MT
    Dim dct As DCT_FeatureLess_MT
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        flood = New FloodFill_Color_MT(ocvb)

        dct = New DCT_FeatureLess_MT(ocvb)
        ocvb.desc = "Find surfaces that lack any texture with DCT (highest frequency removed) and use floodfill to isolate those surfaces."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dct.src = src
        dct.Run(ocvb)

        flood.src = dct.dst2.Clone()
        flood.Run(ocvb)
        dst1 = flood.dst1
    End Sub
End Class





Public Class FloodFill_WithDepth
    Inherits ocvbClass
    Dim range As FloodFill_RelativeRange
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        shadow = New Depth_Holes(ocvb)

        range = New FloodFill_RelativeRange(ocvb)

        label2 = "Floodfill results after removing unknown depth"
        ocvb.desc = "Floodfill only the areas where there is depth"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)

        range.src = src
        range.fBasics.initialMask = shadow.holeMask
        range.Run(ocvb)
        dst1 = range.dst1
        dst2 = range.dst2
    End Sub
End Class





Public Class FloodFill_CComp
    Inherits ocvbClass
    Dim ccomp As CComp_Basics
    Dim range As FloodFill_RelativeRange
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        shadow = New Depth_Holes(ocvb)

        ccomp = New CComp_Basics(ocvb)

        range = New FloodFill_RelativeRange(ocvb)

        label1 = "Input to Floodfill "
        ocvb.desc = "Use Floodfill with the output of the connected components to stabilize the colors used."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)

        ccomp.src = src
        ccomp.Run(ocvb)

        range.src = ccomp.dst1
        range.fBasics.initialMask = shadow.holeMask
        range.Run(ocvb)
        dst1 = range.dst1
        dst2 = range.dst2
        label2 = CStr(ccomp.connectedComponents.blobs.length) + " blobs found. " + CStr(range.fBasics.maskRects.Count) + " were more than " +
                      CStr(range.fBasics.sliders.TrackBar1.Value) + " pixels"
    End Sub
End Class





Public Class FloodFill_RelativeRange
    Inherits ocvbClass
    Public fBasics As FloodFill_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        fBasics = New FloodFill_Basics(ocvb)
        check.Setup(ocvb, caller, 3)
        check.Box(0).Text = "Use Fixed range - when off, it means use relative range "
        check.Box(1).Text = "Use 4 nearest pixels (Link4) - when off, it means use 8 nearest pixels (Link8)"
        check.Box(1).Checked = True ' link4 produces better results.
        check.Box(2).Text = "Use 'Mask Only'"
        label1 = "Input to floodfill basics"
        label2 = "Output of floodfill basics"
        ocvb.desc = "Experiment with 'relative' range option to floodfill.  Compare to fixed range option."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        fBasics.floodFlag = 0
        If check.Box(0).Checked Then fBasics.floodFlag += cv.FloodFillFlags.FixedRange
        If check.Box(1).Checked Then fBasics.floodFlag += cv.FloodFillFlags.Link4 Else fBasics.floodFlag += cv.FloodFillFlags.Link8
        If check.Box(2).Checked Then fBasics.floodFlag += cv.FloodFillFlags.MaskOnly
        fBasics.src = src
        fBasics.Run(ocvb)
        dst1 = src
        dst2 = fBasics.dst2
    End Sub
End Class




Public Class FloodFill_Top16
    Inherits ocvbClass
    Public flood As FloodFill_Basics

    Public thumbNails As New cv.Mat
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Show (up to) the first 16 largest objects in view (in order of size)"

        flood = New FloodFill_Basics(ocvb)

        label1 = "Input image to floodfill"
        ocvb.desc = "Use floodfill to build image segments in a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        flood.src = src

        thumbNails = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
        Dim allSize = New cv.Size(thumbNails.Width / 4, thumbNails.Height / 4) ' show the first 16 masks

        flood.Run(ocvb)

        dst1.SetTo(0)
        Dim thumbCount As Int32
        Dim allRect = New cv.Rect(0, 0, allSize.Width, allSize.Height)
        For i = 0 To flood.masks.Count - 1
            Dim maskIndex = flood.maskSizes.ElementAt(i).Value
            Dim nextColor = scalarColors(i Mod 255)
            dst1.SetTo(nextColor, flood.masks(maskIndex))
            If thumbCount < 16 Then
                thumbNails(allRect) = flood.masks(maskIndex).Resize(allSize).Threshold(0, 255, cv.ThresholdTypes.Binary)
                thumbNails.Rectangle(allRect, cv.Scalar.White, 1)
                allRect.X += allSize.Width
                If allRect.X >= thumbNails.Width Then
                    allRect.X = 0
                    allRect.Y += allSize.Height
                End If
                thumbCount += 1
            End If
        Next
        If check.Box(0).Checked Then dst1 = thumbNails.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        label1 = CStr(flood.masks.Count) + " regions > " + CStr(flood.minFloodSize) + " pixels"
    End Sub
End Class





Public Class FloodFill_Projection
    Inherits ocvbClass
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public rects As New List(Of cv.Rect)
    Public masks As New List(Of cv.Mat)
    Public minFloodSize As Integer
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "FloodFill Minimum Size", 1, 5000, 2500)
        sliders.setupTrackBar2("FloodFill LoDiff", 1, 255, 5)
        sliders.setupTrackBar3("FloodFill HiDiff", 1, 255, 5)
        sliders.setupTrackBar4("Step Size", 1, ocvb.color.Cols / 2, 20)

        label1 = "Input image to floodfill"
        ocvb.desc = "Use floodfill on a projection to determine how many objects and where they are - needs more work"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        minFloodSize = sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(sliders.TrackBar3.Value)
        Dim stepSize = sliders.TrackBar4.Value

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src.Clone()
        Dim maskPlus = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8UC1)

        rects.Clear()
        masks.Clear()
        dst2.SetTo(0)
        cv.Cv2.BitwiseNot(src, src)
        Dim nextColor As cv.Vec3b
        For y = 0 To src.Height - 1 Step stepSize
            For x = 0 To src.Width - 1 Step stepSize
                If src.Get(Of Byte)(y, x) < 255 Then
                    Dim rect As New cv.Rect
                    maskPlus.SetTo(0)
                    Dim count = cv.Cv2.FloodFill(src, maskPlus, New cv.Point(x, y), cv.Scalar.White, rect, loDiff, hiDiff, floodFlag Or (255 << 8))
                    If count > minFloodSize Then
                        rects.Add(rect)
                        masks.Add(maskPlus(rect).Clone())
                    End If
                End If
            Next
        Next

        For i = 0 To masks.Count - 1
            Dim rect = rects(i)
            nextColor = rColors(rects.Count Mod 255)
            dst2(rect).SetTo(nextColor, masks(i))
            dst2.Rectangle(rect, cv.Scalar.White, 1)
        Next
        label2 = CStr(rects.Count) + " regions > " + CStr(minFloodSize) + " pixels"
    End Sub
End Class
