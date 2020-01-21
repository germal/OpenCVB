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
