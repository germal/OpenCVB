Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module SuperPixel_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixel_Open(width As Int32, height As Int32, channels As Int32, num_superpixels As Int32, num_levels As Int32, prior As Int32) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub SuperPixel_Close(spPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixel_Run(spPtr As IntPtr, rgbPtr As IntPtr) As IntPtr
    End Function
End Module





Public Class SuperPixel_Basics_CPP : Implements IDisposable
    Dim spPtr As IntPtr = 0
    Public sliders As New OptionsSliders
    Public src As cv.Mat
    Public dst1 As cv.Mat
    Public dst2 As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)

        sliders.setupTrackBar1(ocvb, "Number of SuperPixels", 1, 1000, 400)
        sliders.setupTrackBar2(ocvb, "Iterations", 1, 10, 4)
        sliders.setupTrackBar3(ocvb, "Prior", 1, 10, 2)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.label2 = "Mask of SuperPixels"
        ocvb.desc = "Sub-divide the image into super pixels."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static numSuperPixels As Int32
        Static numIterations As Int32
        Static prior As Int32
        If externalUse = False Then
            src = ocvb.color
            dst1 = ocvb.result1
            dst2 = ocvb.result2
        End If
        If numSuperPixels <> sliders.TrackBar1.Value Or numIterations <> sliders.TrackBar2.Value Or prior <> sliders.TrackBar3.Value Then
            numSuperPixels = sliders.TrackBar1.Value
            numIterations = sliders.TrackBar2.Value
            prior = sliders.TrackBar3.Value
            If spPtr <> 0 Then SuperPixel_Close(spPtr)
            spPtr = SuperPixel_Open(src.Width, src.Height, src.Channels, numSuperPixels, numIterations, prior)
        End If

        Dim srcData(src.Total * src.ElemSize) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = SuperPixel_Run(spPtr, handleSrc.AddrOfPinnedObject())
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(src.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            If externalUse Then
                dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
                dst1 = src
                dst1.SetTo(cv.Scalar.White, dst2)
            Else
                ocvb.result2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
                ocvb.result1 = src
                ocvb.result1.SetTo(cv.Scalar.White, dst2)
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        SuperPixel_Close(spPtr)
        sliders.Dispose()
    End Sub
End Class






Public Class SuperPixel_Depth : Implements IDisposable
    Dim pixels As SuperPixel_Basics_CPP
    Public Sub New(ocvb As AlgorithmData)
        pixels = New SuperPixel_Basics_CPP(ocvb)
        pixels.externalUse = True

        ocvb.desc = "Create SuperPixels using DepthRGB image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        pixels.src = ocvb.depthRGB
        pixels.Run(ocvb)
        ocvb.result1 = pixels.dst1
        ocvb.result2 = pixels.dst2
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        pixels.Dispose()
    End Sub
End Class





Public Class SuperPixel_Basics_MT : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim grid As Thread_Grid
    Dim pixels(0) As SuperPixel_Basics_CPP
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.externalUse = True
        grid.sliders.TrackBar1.Value = ocvb.color.Width / 2
        grid.sliders.TrackBar2.Value = ocvb.color.Height / 2
        grid.sliders.Hide()

        sliders.setupTrackBar1(ocvb, "Number of SuperPixels", 1, 1000, 100)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.parms.ShowOptions = True ' turn off sliders in superpixels_basics_cpp
        ocvb.desc = "Use multi-threading to get superpixels"
        ocvb.label1 = "Not Working!  C++ code not reentrant?"
        ocvb.label2 = "Note symmetry in the 4 quadrants.  Not working."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        Static threadCount As Int32
        If threadCount <> grid.roiList.Count Then
            For i = 0 To threadCount - 1
                If pixels(i) IsNot Nothing Then pixels(i).Dispose()
            Next
            threadCount = grid.roiList.Count
            ReDim pixels(threadCount - 1)
            For i = 0 To threadCount - 1
                pixels(i) = New SuperPixel_Basics_CPP(ocvb)
                pixels(i).externalUse = True
            Next
        End If

        Dim numPixels = sliders.TrackBar1.Value
        ocvb.result2 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U)
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            pixels(i).sliders.TrackBar1.Value = numPixels
            pixels(i).src = ocvb.color(roi)
            pixels(i).Run(ocvb)
            ocvb.result1(roi) = pixels(i).dst1
            ocvb.result2(roi) = pixels(i).dst2
        End Sub)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
        For i = 0 To pixels.Count - 1
            If pixels(i) IsNot Nothing Then pixels(i).Dispose()
        Next
        sliders.Dispose()
    End Sub
End Class