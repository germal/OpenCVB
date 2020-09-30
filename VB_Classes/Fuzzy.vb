Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Fuzzy_Basics
    Inherits VBparent
    Dim Fuzzy As IntPtr
    Dim reduction As Reduction_Simple
    Public palette As Palette_Basics
    Public gray As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        reduction = New Reduction_Simple(ocvb)
        Fuzzy = Fuzzy_Open()
        palette = New Palette_Basics(ocvb)
        label1 = "Solid regions"
        label2 = "Fuzzy pixels - not solid"
        ocvb.desc = "That which is not solid is fuzzy"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)
        dst1 = reduction.dst1
        dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim srcData(dst1.Total) As Byte
        Marshal.Copy(dst1.Data, srcData, 0, srcData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = Fuzzy_Run(Fuzzy, handleSrc.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(dst1.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            gray = New cv.Mat(dst1.Rows, dst1.Cols, cv.MatType.CV_8UC1, dstData)
        End If
        dst2 = gray.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        palette.src = gray
        palette.Run(ocvb)
        dst1 = palette.dst1
        dst1.SetTo(0, dst2)
    End Sub
    Public Sub Close()
        Fuzzy_Close(Fuzzy)
    End Sub
End Class







Public Class Fuzzy_Basics_VB
    Inherits VBparent
    Dim reduction As Reduction_Simple
    Dim hist As Histogram_KalmanSmoothed
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        reduction = New Reduction_Simple(ocvb)
        hist = New Histogram_KalmanSmoothed(ocvb)
        ocvb.desc = "That which is not solid is fuzzy."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        reduction.src = src
        reduction.Run(ocvb)
        dst1 = reduction.dst1

        Dim gray = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = New cv.Mat(gray.Size, cv.MatType.CV_8U, 0)
        For y = 1 To gray.Rows - 3
            For x = 1 To gray.Cols - 3
                Dim pixel = gray.Get(Of Byte)(y, x)
                Dim r = New cv.Rect(x, y, 3, 3)
                Dim pSum = cv.Cv2.Sum(gray(r))
                If pSum = 9 * pixel Then dst2.Set(Of Byte)(y + 1, x + 1, 255)
            Next
        Next
    End Sub
End Class







Module Fuzzy_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Fuzzy_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Fuzzy_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Fuzzy_Run(cPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function
End Module







Public Class Fuzzy_Depth
    Inherits VBparent
    Public basics As Fuzzy_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        basics = New Fuzzy_Basics(ocvb)

        label1 = "Solid regions in depth"
        label2 = "Fuzzy pixels - not solid"
        ocvb.desc = "Find solids in the depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        basics.src = ocvb.RGBDepth
        basics.Run(ocvb)
        dst1 = basics.dst1
        dst2 = basics.dst2
    End Sub
End Class






Public Class Fuzzy_Depth2
    Inherits VBparent
    Dim fuzzy As Fuzzy_Basics
    Dim depth As Depth_Colorizer_CPP
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fuzzy = New Fuzzy_Basics(ocvb)
        depth = New Depth_Colorizer_CPP(ocvb)

        label1 = "Solid regions in depth"
        label2 = "Fuzzy pixels - not solid"
        ocvb.desc = "Find solids in the depth data and show that colorizing manually does not alter the outcome."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        depth.src = getDepth32f(ocvb)
        depth.Run(ocvb)

        fuzzy.src = depth.dst1
        fuzzy.Run(ocvb)
        dst1 = fuzzy.dst1
        dst2 = fuzzy.dst2
    End Sub
End Class





Public Class Fuzzy_FloodFill
    Inherits VBparent
    Dim fuzzy As Fuzzy_Basics
    Dim flood As FloodFill_8bit
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fuzzy = New Fuzzy_Basics(ocvb)
        flood = New FloodFill_8bit(ocvb)

        ocvb.desc = "FloodFill the regions defined as solid"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        fuzzy.src = src
        fuzzy.Run(ocvb)
        dst2 = fuzzy.dst1

        flood.src = fuzzy.dst1
        flood.Run(ocvb)
        dst1 = flood.dst1
    End Sub
End Class







Public Class Fuzzy_Contours
    Inherits VBparent
    Dim options As Contours_Basics
    Public fuzzy As Fuzzy_Basics
    Public contours As cv.Point()()
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        options = New Contours_Basics(ocvb) ' we need all the options
        fuzzy = New Fuzzy_Basics(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Threshold of contour points", 1, 500, 20)

        ocvb.desc = "Use contours to outline solids"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        options.setOptions()
        fuzzy.src = src
        fuzzy.Run(ocvb)

        contours = cv.Cv2.FindContoursAsArray(fuzzy.dst2, options.retrievalMode, options.ApproximationMode)

        dst1 = fuzzy.dst1
        dst2 = fuzzy.dst1.Clone
        Static pointThreshold = findSlider("Threshold of contour points")
        Dim maxPoint = pointThreshold.value
        For i = 0 To contours.Length - 1
            If contours(i).Length > maxPoint Then
                Dim len = contours(i).Length
                For j = 0 To len
                    dst2.Line(contours(i)(j Mod len), contours(i)((j + 1) Mod len), cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
                Next
            End If
        Next
        dst2.SetTo(0, fuzzy.dst2)
    End Sub
End Class






Public Class Fuzzy_ContoursDepth
    Inherits VBparent
    Dim options As Contours_Basics
    Public fuzzyD As Fuzzy_Depth
    Public sortContours As New SortedList(Of Integer, cv.Vec2i)(New compareAllowIdenticalIntegerInverted)
    Public contours As cv.Point()()
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        options = New Contours_Basics(ocvb) ' we need all the options
        fuzzyD = New Fuzzy_Depth(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Threshold of contour points", 1, 500, 20)

        ocvb.desc = "Use contours to outline solids in the depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        options.setOptions()
        fuzzyD.Run(ocvb)

        contours = cv.Cv2.FindContoursAsArray(fuzzyD.dst2, options.retrievalMode, options.ApproximationMode)

        sortContours.Clear()
        dst1 = fuzzyD.dst1
        dst2 = fuzzyD.dst1.Clone
        Static pointThreshold = findSlider("Threshold of contour points")
        Dim maxPoint = pointThreshold.value
        For i = 0 To contours.Length - 1
            If contours(i).Length > maxPoint Then
                Dim len = contours(i).Length
                For j = 0 To len
                    dst2.Line(contours(i)(j Mod len), contours(i)((j + 1) Mod len), cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
                Next
                Dim maskID As Integer = 0
                Dim pt = contours(i)(0)
                For y = pt.Y - 1 To pt.Y + 1
                    For x = pt.X - 1 To pt.X + 1
                        If x < src.Width And y < src.Height Then
                            Dim val = fuzzyD.basics.gray.Get(Of Byte)(y, x)
                            If val <> 0 Then maskID = val
                        End If
                    Next
                Next
                sortContours.Add(len, New cv.Point(i, maskID))
            End If
        Next
    End Sub
End Class






Public Class Fuzzy_PointTracker
    Inherits VBparent
    Dim fuzzy As Fuzzy_Basics
    Dim pTrack As Kalman_PointTracker
    Dim flood As FloodFill_8bit
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fuzzy = New Fuzzy_Basics(ocvb)
        flood = New FloodFill_8bit(ocvb)
        pTrack = New Kalman_PointTracker(ocvb)

        ocvb.desc = "FloodFill the regions defined as solid"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        fuzzy.src = src
        fuzzy.Run(ocvb)
        dst2 = fuzzy.dst1

        flood.src = fuzzy.dst1
        flood.Run(ocvb)

        pTrack.queryPoints = flood.basics.centroids
        pTrack.queryRects = flood.basics.rects
        pTrack.queryMasks = flood.basics.masks
        pTrack.Run(ocvb)

        label2 = CStr(pTrack.viewObjects.Count) + " regions were found"
        dst1 = pTrack.dst1
    End Sub
End Class





Public Class Fuzzy_Tracker
    Inherits VBparent
    Public fuzzy As Fuzzy_ContoursDepth
    Public pTrack As Kalman_PointTracker
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        pTrack = New Kalman_PointTracker(ocvb)
        fuzzy = New Fuzzy_ContoursDepth(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Desired number of objects", 1, 50, 10)

        ocvb.desc = "Create centroids and rect's for solid regions and track them - tracker"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        fuzzy.src = src
        fuzzy.Run(ocvb)
        dst1 = fuzzy.dst1

        pTrack.queryRects.Clear()
        pTrack.queryPoints.Clear()
        Dim minX As Double, maxX As Double
        Dim minY As Double, maxY As Double
        For Each c In fuzzy.sortContours
            Dim contours = fuzzy.contours(c.Value.Item0)
            Dim points = New cv.Mat(contours.Length, 1, cv.MatType.CV_32SC2, contours.ToArray)
            Dim center = points.Sum()
            points = New cv.Mat(contours.Length, 2, cv.MatType.CV_32S, contours.ToArray)
            points.Col(0).MinMaxIdx(minX, maxX)
            points.Col(1).MinMaxIdx(minY, maxY)
            pTrack.queryPoints.Add(New cv.Point2f(center.Item(0) / contours.Length, center.Item(1) / contours.Length))
            pTrack.queryRects.Add(New cv.Rect(minX, minY, maxX - minX, maxY - minY))
            pTrack.queryColors.Add(c.Value.Item1) ' this is the gray scale color of the mask...
            pTrack.queryContourMats.Add(points.Clone) ' this is the index into the contours that will be used to outline the region...
        Next
        pTrack.dst1 = fuzzy.dst1
        pTrack.Run(ocvb)
        label1 = CStr(pTrack.viewObjects.Count) + " regions were found in the image."

        Static contourSlider = findSlider("Threshold of contour points")
        Dim desired = sliders.trackbar(0).Value
        If pTrack.viewObjects.Count > desired Then
            contourSlider.value += 1
        Else
            If desired - pTrack.viewObjects.Count > 3 Then contourSlider.value -= 1
        End If
    End Sub
End Class







Public Class Fuzzy_NeighborProof
    Inherits VBparent
    Dim fuzzy As Fuzzy_Contours
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fuzzy = New Fuzzy_Contours(ocvb)
        ocvb.desc = "Prove that every contour point has at least one and only one neighbor with the mask ID and that the rest are zero"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Static proofFailed As Boolean = False
        If proofFailed Then Exit Sub
        fuzzy.src = src
        fuzzy.Run(ocvb)
        dst1 = fuzzy.fuzzy.gray
        For i = 0 To fuzzy.contours.Length - 1
            Dim len = fuzzy.contours(i).Length
            For j = 0 To len - 1
                Dim pt = fuzzy.contours(i)(j)
                Dim maskID As Integer = 0
                For y = pt.Y - 1 To pt.Y + 1
                    For x = pt.X - 1 To pt.X + 1
                        If x < src.Width And y < src.Height Then
                            Dim val = dst1.Get(Of Byte)(y, x)
                            If val <> 0 Then maskID = val
                            If maskID <> 0 And val <> 0 And maskID <> val Then
                                MsgBox("Proof has failed!  There is more than one mask ID identified by this contour point.")
                                proofFailed = True
                                Exit Sub
                            End If
                        End If
                    Next
                Next
            Next
        Next
        ocvb.trueText("Mask ID's for all contour point in each region identified only one region.", 10, 50, 3)
    End Sub
End Class