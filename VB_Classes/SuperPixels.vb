Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module SuperPixels_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixels_Open(width As Int32, height As Int32, channels As Int32, num_superpixels As Int32, num_levels As Int32, prior As Int32) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub SuperPixels_Close(spPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SuperPixels_Run(spPtr As IntPtr, rgbPtr As IntPtr) As IntPtr
    End Function
End Module





Public Class SuperPixels_Basics_CPP : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim spPtr As IntPtr = 0
    Public src As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Number of SuperPixels", 100, 1000, 400)
        sliders.setupTrackBar2(ocvb, "Iterations", 1, 10, 4)
        sliders.setupTrackBar3(ocvb, "Prior", 1, 10, 2)
        sliders.Show()

        ocvb.label2 = "Mask of SuperPixels"
        ocvb.desc = "Sub-divide the image into super pixels."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static numSuperPixels As Int32
        Static numIterations As Int32
        Static prior As Int32
        If externalUse = False Then src = ocvb.color
        If numSuperPixels <> sliders.TrackBar1.Value Or numIterations <> sliders.TrackBar2.Value Or prior <> sliders.TrackBar3.Value Then
            numSuperPixels = sliders.TrackBar1.Value
            numIterations = sliders.TrackBar2.Value
            prior = sliders.TrackBar3.Value
            If spPtr <> 0 Then SuperPixels_Close(spPtr)
            spPtr = SuperPixels_Open(src.Width, src.Height, src.Channels, numSuperPixels, numIterations, prior)
        End If

        Dim srcData(src.Total * src.ElemSize) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = SuperPixels_Run(spPtr, handleSrc.AddrOfPinnedObject())
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(src.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            ocvb.result2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
            ocvb.result1 = src
            ocvb.result1.SetTo(cv.Scalar.White, ocvb.result2)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        SuperPixels_Close(spPtr)
        sliders.Dispose()
    End Sub
End Class






Public Class SuperPixels_Depth : Implements IDisposable
    Dim pixels As SuperPixels_Basics_CPP
    Public Sub New(ocvb As AlgorithmData)
        pixels = New SuperPixels_Basics_CPP(ocvb)
        pixels.externalUse = True

        ocvb.desc = "Create SuperPixels using DepthRGB image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        pixels.src = ocvb.depthRGB
        pixels.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        pixels.Dispose()
    End Sub
End Class
