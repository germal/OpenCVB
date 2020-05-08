Imports cv = OpenCvSharp
Imports System.Text.RegularExpressions
Public Class FloodFill_Basics
    Inherits ocvbClass
    Public srcGray As New cv.Mat
    
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "FloodFill Minimum Size", 1, 5000, 2500)
        sliders.setupTrackBar2(ocvb, caller, "FloodFill LoDiff", 1, 255, 5)
        sliders.setupTrackBar3(ocvb, caller, "FloodFill HiDiff", 1, 255, 5)
        sliders.setupTrackBar4(ocvb, caller, "Step Size", 1, ocvb.color.Width / 2, 20)

        ocvb.label1 = "Input image to floodfill"
        ocvb.desc = "Use floodfill to build image segments in a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        minFloodSize = sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(sliders.TrackBar3.Value)
        Dim stepSize = sliders.TrackBar4.Value

        if standalone Then
            srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        initialMask = New cv.Mat(srcGray.Size, cv.MatType.CV_8U, 0)
        ocvb.result1 = srcGray.Clone()
        Dim rect As New cv.Rect
        Dim maskPlus = New cv.Mat(New cv.Size(srcGray.Width + 2, srcGray.Height + 2), cv.MatType.CV_8UC1)
        Dim maskRect = New cv.Rect(1, 1, maskPlus.Width - 2, maskPlus.Height - 2)

        masks.Clear()
        maskSizes.Clear()
        maskRects.Clear()

        maskPlus.SetTo(0)
        Dim ignoreMasks = initialMask.Clone()

        For y = 0 To srcGray.Height - 1 Step stepSize
            For x = 0 To srcGray.Width - 1 Step stepSize
                If srcGray.Get(Of Byte)(y, x) > 0 Then
                    Dim count = cv.Cv2.FloodFill(srcGray, maskPlus, New cv.Point(x, y), cv.Scalar.White, rect, loDiff, hiDiff, floodFlag Or (255 << 8))
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

        if standalone Then
            ocvb.result2.SetTo(0)
            For i = 0 To masks.Count - 1
                Dim maskIndex = maskSizes.ElementAt(i).Value
                Dim nextColor = ocvb.colorScalar(i Mod 255)
                ocvb.result2.SetTo(nextColor, masks(maskIndex))
            Next
        End If
        ocvb.label2 = CStr(masks.Count) + " regions > " + CStr(minFloodSize) + " pixels"
    End Sub
End Class





Public Class FloodFill_Top16_MT
    Inherits ocvbClass
    Dim grid As Thread_Grid
    Public srcGray As New cv.Mat
        Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        grid = New Thread_Grid(ocvb, caller)
        sliders.setupTrackBar1(ocvb, caller, "FloodFill Minimum Size", 1, 500, 50)
        sliders.setupTrackBar2(ocvb, caller, "FloodFill LoDiff", 1, 255, 5)
        sliders.setupTrackBar3(ocvb, caller, "FloodFill HiDiff", 1, 255, 5)

        ocvb.desc = "Use floodfill to build image segments with a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim minFloodSize = sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(sliders.TrackBar3.Value)

        if standalone Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.result2 = srcGray.Clone()
        grid.Run(ocvb)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            For y = roi.Y To roi.Y + roi.Height - 1
                For x = roi.X To roi.X + roi.Width - 1
                    Dim nextByte = srcGray.Get(Of Byte)(y, x)
                    If nextByte <> 255 And nextByte > 0 Then
                        Dim count = cv.Cv2.FloodFill(srcGray, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange)
                        If count > minFloodSize Then
                            count = cv.Cv2.FloodFill(ocvb.result2, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange)
                        End If
                    End If
                Next
            Next
        End Sub)
    End Sub
    Public Sub MyDispose()
        grid.Dispose()
    End Sub
End Class




Public Class FloodFill_Color_MT
    Inherits ocvbClass
    Dim flood As FloodFill_Top16_MT
    Dim grid As Thread_Grid
    Public src As New cv.Mat
        Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        grid = New Thread_Grid(ocvb, caller)
        flood = New FloodFill_Top16_MT(ocvb, caller)

        ocvb.desc = "Use floodfill to build image segments in an RGB image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim minFloodSize = flood.sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(flood.sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(flood.sliders.TrackBar3.Value)

        if standalone Then src = ocvb.color.Clone()
        ocvb.result2 = src.Clone()
        grid.Run(ocvb)
        Dim vec255 = New cv.Vec3b(255, 255, 255)
        Dim vec0 = New cv.Vec3b(0, 0, 0)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            For y = roi.Y To roi.Y + roi.Height - 1
                For x = roi.X To roi.X + roi.Width - 1
                    Dim vec = src.Get(Of cv.Vec3b)(y, x)
                    If vec <> vec255 And vec <> vec0 Then
                        Dim count = cv.Cv2.FloodFill(src, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange + cv.FloodFillFlags.Link4)
                        If count > minFloodSize Then
                            count = cv.Cv2.FloodFill(ocvb.result2, New cv.Point(x, y), cv.Scalar.White, roi, loDiff, hiDiff, cv.FloodFillFlags.FixedRange + cv.FloodFillFlags.Link4)
                        End If
                    End If
                Next
            Next
        End Sub)
    End Sub
    Public Sub MyDispose()
        grid.Dispose()
        flood.Dispose()
    End Sub
End Class




Public Class FloodFill_DCT
    Inherits ocvbClass
    Dim flood As FloodFill_Color_MT
    Dim dct As DCT_FeatureLess_MT
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        flood = New FloodFill_Color_MT(ocvb, caller)
        flood.standalone = True

        dct = New DCT_FeatureLess_MT(ocvb, caller)
        ocvb.desc = "Find surfaces that lack any texture with DCT (highest frequency removed) and use floodfill to isolate those surfaces."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dct.Run(ocvb)
        flood.src = ocvb.result2.Clone()
        Dim mask As New cv.Mat
        cv.Cv2.BitwiseNot(ocvb.result1, mask)
        flood.src.SetTo(0, mask)
        flood.Run(ocvb)
        ocvb.result2.SetTo(0, mask)
    End Sub
    Public Sub MyDispose()
        dct.Dispose()
        flood.Dispose()
    End Sub
End Class





Public Class FloodFill_WithDepth
    Inherits ocvbClass
    Dim range As FloodFill_RelativeRange
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        shadow = New Depth_Holes(ocvb, caller)
        shadow.standalone = True

        range = New FloodFill_RelativeRange(ocvb, caller)
        range.fBasics.standalone = True

        ocvb.desc = "Floodfill only the areas where there is depth"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)

        range.fBasics.srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        range.fBasics.initialMask = shadow.holeMask
        range.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        range.Dispose()
        shadow.Dispose()
    End Sub
End Class





Public Class FloodFill_CComp
    Inherits ocvbClass
    Dim ccomp As CComp_Basics
    Dim range As FloodFill_RelativeRange
    Dim shadow As Depth_Holes
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        shadow = New Depth_Holes(ocvb, caller)
        shadow.standalone = True

        ccomp = New CComp_Basics(ocvb, caller)
        ccomp.standalone = True

        range = New FloodFill_RelativeRange(ocvb, caller)
        range.fBasics.standalone = True

        ocvb.desc = "Use Floodfill with the output of the connected components to stabilize the colors used."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb)

        ccomp.srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(ocvb)

        range.fBasics.srcGray = ccomp.dstGray
        range.fBasics.initialMask = shadow.holeMask
        range.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        ccomp.Dispose()
        range.Dispose()
        shadow.Dispose()
    End Sub
End Class





Public Class FloodFill_RelativeRange
    Inherits ocvbClass
    Public fBasics As FloodFill_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        fBasics = New FloodFill_Basics(ocvb, caller)
        check.Setup(ocvb, caller, 3)
        check.Box(0).Text = "Use Fixed range - when off, it means use relative range "
        check.Box(1).Text = "Use 4 nearest pixels (Link4) - when off, it means use 8 nearest pixels (Link8)"
        check.Box(1).Checked = True ' link4 produces better results.
        check.Box(2).Text = "Use 'Mask Only'"
        ocvb.desc = "Experiment with 'relative' range option to floodfill.  Compare to fixed range option."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        fBasics.floodFlag = 0
        If check.Box(0).Checked Then fBasics.floodFlag += cv.FloodFillFlags.FixedRange
        If check.Box(1).Checked Then fBasics.floodFlag += cv.FloodFillFlags.Link4 Else fBasics.floodFlag += cv.FloodFillFlags.Link8
        If check.Box(2).Checked Then fBasics.floodFlag += cv.FloodFillFlags.MaskOnly
        fBasics.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        fBasics.Dispose()
    End Sub
End Class




Public Class FloodFill_Top16
    Inherits ocvbClass
    Public flood As FloodFill_Basics
    Public srcGray As New cv.Mat
    
    Public thumbNails As New cv.Mat
    Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Show (up to) the first 16 largest objects in view (in order of size)"

        flood = New FloodFill_Basics(ocvb, caller)
        flood.standalone = True

        ocvb.label1 = "Input image to floodfill"
        ocvb.desc = "Use floodfill to build image segments in a grayscale image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        if standalone Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        flood.srcGray = srcGray

        thumbNails = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U, 0)
        Dim allSize = New cv.Size(thumbNails.Width / 4, thumbNails.Height / 4) ' show the first 16 masks

        flood.Run(ocvb)

        Dim thumbCount As Int32
        Dim allRect = New cv.Rect(0, 0, allSize.Width, allSize.Height)
        For i = 0 To flood.masks.Count - 1
            Dim maskIndex = flood.maskSizes.ElementAt(i).Value
            Dim nextColor = ocvb.colorScalar(i Mod 255)
            ocvb.result2.SetTo(nextColor, flood.masks(maskIndex))
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
        If check.Box(0).Checked Then ocvb.result2 = thumbNails.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.label2 = CStr(flood.masks.Count) + " regions > " + CStr(flood.minFloodSize) + " pixels"
    End Sub
End Class





Public Class FloodFill_Projection
    Inherits ocvbClass
    Public srcGray As New cv.Mat
    Public dst As cv.Mat
        Public floodFlag As cv.FloodFillFlags = cv.FloodFillFlags.FixedRange
    Public objectRects As New List(Of cv.Rect)
    Public minFloodSize As Integer
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "FloodFill Minimum Size", 1, 5000, 2500)
        sliders.setupTrackBar2(ocvb, caller, "FloodFill LoDiff", 1, 255, 5)
        sliders.setupTrackBar3(ocvb, caller, "FloodFill HiDiff", 1, 255, 5)
        sliders.setupTrackBar4(ocvb, caller, "Step Size", 1, ocvb.color.Width / 2, 20)

        ocvb.label1 = "Input image to floodfill"
        ocvb.desc = "Use floodfill on a projection to determine how many objects and where they are."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        minFloodSize = sliders.TrackBar1.Value
        Dim loDiff = cv.Scalar.All(sliders.TrackBar2.Value)
        Dim hiDiff = cv.Scalar.All(sliders.TrackBar3.Value)
        Dim stepSize = sliders.TrackBar4.Value

        if standalone Then srcGray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.result1 = srcGray.Clone()
        Dim maskPlus = New cv.Mat(New cv.Size(srcGray.Width + 2, srcGray.Height + 2), cv.MatType.CV_8UC1)
        Dim maskRect = New cv.Rect(1, 1, maskPlus.Width - 2, maskPlus.Height - 2)

        objectRects.Clear()
        dst = New cv.Mat(srcGray.Size(), cv.MatType.CV_8UC3, 0)
        cv.Cv2.BitwiseNot(srcGray, srcGray) ' floodfill where there are zeros.
        For y = 0 To srcGray.Height - 1 Step stepSize
            For x = 0 To srcGray.Width - 1 Step stepSize
                If srcGray.Get(Of Byte)(y, x) = 0 Then
                    Dim rect As New cv.Rect
                    maskPlus.SetTo(0)
                    Dim count = cv.Cv2.FloodFill(srcGray, maskPlus, New cv.Point(x, y), cv.Scalar.White, rect, loDiff, hiDiff, floodFlag Or (255 << 8))
                    If count > minFloodSize Then
                        Dim nextColor = ocvb.colorScalar(objectRects.Count Mod 255)
                        objectRects.Add(rect)
                        dst(rect).SetTo(nextColor, maskPlus(rect))
                    End If
                End If
            Next
        Next

        if standalone Then
            ocvb.result2 = dst
            ocvb.label2 = CStr(objectRects.Count) + " regions > " + CStr(minFloodSize) + " pixels"
        End If
    End Sub
End Class