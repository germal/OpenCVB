Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Fuzzy_Basics
    Inherits VBparent
    Dim Fuzzy As IntPtr
    Dim reduction As Reduction_Simple
    Public palette As Palette_Basics
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
            dst1 = New cv.Mat(dst1.Rows, dst1.Cols, cv.MatType.CV_8UC1, dstData)
        End If
        dst2 = dst1.Threshold(1, 255, cv.ThresholdTypes.BinaryInv)
        palette.src = dst1
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
    Dim fuzzy As Fuzzy_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fuzzy = New Fuzzy_Basics(ocvb)

        label1 = "Solid regions in depth"
        label2 = "Fuzzy pixels - not solid"
        ocvb.desc = "Find solids in the depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        fuzzy.src = ocvb.RGBDepth
        fuzzy.Run(ocvb)
        dst1 = fuzzy.dst1
        dst2 = fuzzy.dst2
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






Public Class Fuzzy_Contours
    Inherits VBparent
    Dim options As Contours_Basics
    Dim fuzzy As Fuzzy_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        options = New Contours_Basics(ocvb) ' we need all the options
        fuzzy = New Fuzzy_Basics(ocvb)
        ocvb.desc = "Use contours to outline solids"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        options.setOptions()
        fuzzy.src = src
        fuzzy.Run(ocvb)

        'Dim contours0 As cv.Point()()
        'If options.retrievalMode = cv.RetrievalModes.FloodFill Then
        '    '    Dim img32sc1 As New cv.Mat
        '    '    src.ConvertTo(img32sc1, cv.MatType.CV_32SC1)
        '    '    contours0 = cv.Cv2.FindContoursAsArray(img32sc1, retrievalMode, ApproximationMode)
        '    '    img32sc1.ConvertTo(dst1, cv.MatType.CV_8UC1)
        '    contours0 = cv.Cv2.FindContoursAsArray(fuzzy.dst2, cv.RetrievalModes.Tree, options.ApproximationMode)
        'Else
        '    contours0 = cv.Cv2.FindContoursAsArray(fuzzy.dst2, options.retrievalMode, options.ApproximationMode)
        'End If

        'Dim contours()() As cv.Point = Nothing
        'ReDim contours(contours0.Length - 1)
        'Dim filterCount As Integer
        'For j = 0 To contours0.Length - 1
        '    If contours0(j).Length > 10 Then
        '        contours(filterCount) = cv.Cv2.ApproxPolyDP(contours0(j), contours0(j).Length, True)
        '        filterCount += 1
        '    End If
        'Next
        'ReDim Preserve contours(filterCount)
        'dst1 = fuzzy.dst1
        'dst2.SetTo(0)
        'If options.retrievalMode = cv.RetrievalModes.FloodFill Then
        '    cv.Cv2.DrawContours(dst2, contours, 0, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        'Else
        '    cv.Cv2.DrawContours(dst2, contours, 0, cv.Scalar.Yellow, 2, cv.LineTypes.AntiAlias)
        'End If
    End Sub
End Class