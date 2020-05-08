Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module SuperPixel_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixel_Open(width As Int32, height As Int32, num_superpixels As Int32, num_levels As Int32, prior As Int32) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub SuperPixel_Close(spPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixel_GetLabels(spPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixel_Run(spPtr As IntPtr, rgbPtr As IntPtr) As IntPtr
    End Function
End Module





Public Class SuperPixel_Basics_CPP
    Inherits ocvbClass
    Dim spPtr As IntPtr = 0
        Public src As cv.Mat
    Public dst1 As cv.Mat
    Public dst2 As cv.Mat
        Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Number of SuperPixels", 1, 1000, 400)
        sliders.setupTrackBar2(ocvb, caller, "Iterations", 0, 10, 4)
        sliders.setupTrackBar3(ocvb, caller,"Prior", 1, 10, 2)

        ocvb.label2 = "Superpixel label data (0-255)"
        ocvb.desc = "Sub-divide the image into super pixels."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static numSuperPixels As Int32
        Static numIterations As Int32
        Static prior As Int32
        if standalone Then
            src = ocvb.color.Clone()
            dst1 = ocvb.result1
        End If
        If numSuperPixels <> sliders.TrackBar1.Value Or numIterations <> sliders.TrackBar2.Value Or prior <> sliders.TrackBar3.Value Then
            numSuperPixels = sliders.TrackBar1.Value
            numIterations = sliders.TrackBar2.Value
            prior = sliders.TrackBar3.Value
            If spPtr <> 0 Then SuperPixel_Close(spPtr)
            spPtr = SuperPixel_Open(src.Width, src.Height, numSuperPixels, numIterations, prior)
        End If

        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = SuperPixel_Run(spPtr, handleSrc.AddrOfPinnedObject())
        handleSrc.Free()

        If imagePtr <> 0 Then

            Dim dstData(src.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)

            If standalone Then
                dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
                dst1 = src
                dst1.SetTo(cv.Scalar.White, dst2)
            Else
                Dim tmp = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
                ocvb.result1 = src
                ocvb.result1.SetTo(cv.Scalar.White, tmp)
            End If
        End If

        Dim labelData(src.Total * 4 - 1) As Byte ' labels are 32-bit integers.
        Dim labelPtr = SuperPixel_GetLabels(spPtr)
        Marshal.Copy(labelPtr, labelData, 0, labelData.Length)
        Dim labels = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_32S, labelData)
        If numSuperPixels < 255 Then labels *= 255 / numSuperPixels
        labels.ConvertTo(ocvb.result2, cv.MatType.CV_8U)
    End Sub
    Public Sub MyDispose()
        SuperPixel_Close(spPtr)
            End Sub
End Class






Public Class SuperPixel_Depth
    Inherits ocvbClass
    Dim pixels As SuperPixel_Basics_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        pixels = New SuperPixel_Basics_CPP(ocvb, caller)

        ocvb.desc = "Create SuperPixels using RGBDepth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        pixels.src = ocvb.RGBDepth.Clone()
        pixels.Run(ocvb)
        ocvb.result1 = pixels.dst1
        ocvb.result2 = pixels.dst2
    End Sub
    Public Sub MyDispose()
        pixels.Dispose()
    End Sub
End Class






Public Class SuperPixel_WithCanny
    Inherits ocvbClass
    Dim pixels As SuperPixel_Basics_CPP
    Dim edges As Edges_Canny
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        edges = New Edges_Canny(ocvb, caller)

        pixels = New SuperPixel_Basics_CPP(ocvb, caller)

        ocvb.desc = "Create SuperPixels using RGBDepth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        edges.src = ocvb.color.Clone()
        edges.Run(ocvb)
        pixels.src = ocvb.color.Clone()
        pixels.src.SetTo(cv.Scalar.White, edges.dst)
        pixels.Run(ocvb)
        ocvb.result1 = pixels.dst1
        ocvb.result2 = pixels.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.result2.SetTo(cv.Scalar.Red, edges.dst)
        ocvb.label2 = "Edges provided by Canny in red"
    End Sub
    Public Sub MyDispose()
        pixels.Dispose()
    End Sub
End Class






Public Class SuperPixel_WithLineDetector
    Inherits ocvbClass
    Dim pixels As SuperPixel_Basics_CPP
    Dim lines As LineDetector_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        lines = New LineDetector_Basics(ocvb, caller)

        pixels = New SuperPixel_Basics_CPP(ocvb, caller)

        ocvb.desc = "Create SuperPixels using RGBDepth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        lines.dst = ocvb.result1
        lines.Run(ocvb)
        pixels.src = ocvb.result1.Clone()
        ocvb.result2 = ocvb.result1.Clone()
        ocvb.label2 = "Input to superpixel basics."
        pixels.Run(ocvb)
        ocvb.result1 = pixels.dst1
        ' ocvb.result2 = pixels.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ' ocvb.result2.SetTo(cv.Scalar.Red, lines.dst)
        ' ocvb.label2 = "Edges provided by Canny in red"
    End Sub
    Public Sub MyDispose()
        pixels.Dispose()
    End Sub
End Class
