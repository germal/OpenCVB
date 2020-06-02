Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module Dither_module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayer16(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayer8(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayer4(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayer3(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayer2(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgbNbpp(pixels As IntPtr, width As Int32, height As Int32, nColors As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgb3bpp(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgb6bpp(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgb9bpp(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgb12bpp(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgb15bpp(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherBayerRgb18bpp(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgbNbpp(pixels As IntPtr, width As Int32, height As Int32, nColors As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFS(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgb3bpp(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgb6bpp(pixels As IntPtr, width As Int32, height As Int32)
    End Sub

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgb9bpp(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgb12bpp(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgb15bpp(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherFSRgb18bpp(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherSierraLiteRgbNbpp(pixels As IntPtr, width As Int32, height As Int32, nColors As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherSierraLite(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherSierraRgbNbpp(pixels As IntPtr, width As Int32, height As Int32, nColors As Int32)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub ditherSierra(pixels As IntPtr, width As Int32, height As Int32)
    End Sub
End Module




' https://www.codeproject.com/Articles/5259216/Dither-Ordered-and-Floyd-Steinberg-Monochrome-Colo
Public Class Dither_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Bits per color plane (Nbpp only)", 1, 5, 1)

        radio.Setup(ocvb, caller, 24)
        For i = 0 To radio.check.Count - 1
            radio.check(i).Text = Choose(i + 1, "Bayer16", "Bayer8", "Bayer4", "Bayer3", "Bayer2", "BayerRgbNbpp", "BayerRgb3bpp", "BayerRgb6bpp",
                                     "BayerRgb9bpp", "BayerRgb12bpp", "BayerRgb15bpp", "BayerRgb18bpp", "FSRgbNbpp", "Floyd-Steinberg",
                                     "FSRgb3bpp", "FSRgb6bpp", "FSRgb9bpp", "FSRgb12bpp", "FSRgb15bpp", "FSRgb18bpp",
                                     "SierraLiteRgbNbpp", "SierraLite", "SierraRgbNbpp", "Sierra")
        Next
        radio.check(4).Checked = True ' this one was interesting...

        ocvb.desc = "Explore all the varieties of dithering"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim radioIndex As Integer
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then radioIndex = i
        Next
        Select Case radioIndex
            Case 5, 12, 20, 22
                sliders.TrackBar1.Enabled = True
            Case Else
                sliders.TrackBar1.Enabled = False
        End Select

        Dim w = dst1.Width
        Dim h = dst1.Height
        Dim nColors = Choose(sliders.TrackBar1.Value, 1, 3, 7, 15, 31) ' indicate 3, 6, 9, 12, 15 bits per pixel.
        Dim pixels(dst1.Total * dst1.ElemSize - 1) As Byte
        Dim hpixels = GCHandle.Alloc(pixels, GCHandleType.Pinned)
        For i = 0 To 1
            Dim src = Choose(i + 1, ocvb.color, ocvb.RGBDepth)
            Marshal.Copy(src.Data, pixels, 0, pixels.Length)
            Select Case radioIndex
                Case 0
                    ditherBayer16(hpixels.AddrOfPinnedObject, w, h)
                Case 1
                    ditherBayer8(hpixels.AddrOfPinnedObject, w, h)
                Case 2
                    ditherBayer4(hpixels.AddrOfPinnedObject, w, h)
                Case 3
                    ditherBayer3(hpixels.AddrOfPinnedObject, w, h)
                Case 4
                    ditherBayer2(hpixels.AddrOfPinnedObject, w, h)
                Case 5
                    ditherBayerRgbNbpp(hpixels.AddrOfPinnedObject, w, h, nColors)
                Case 6
                    ditherBayerRgb3bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 7
                    ditherBayerRgb6bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 8
                    ditherBayerRgb9bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 9
                    ditherBayerRgb12bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 10
                    ditherBayerRgb15bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 11
                    ditherBayerRgb18bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 12
                    ditherFSRgbNbpp(hpixels.AddrOfPinnedObject, w, h, nColors)
                Case 13
                    ditherFS(hpixels.AddrOfPinnedObject, w, h)
                Case 14
                    ditherFSRgb3bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 15
                    ditherFSRgb6bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 16
                    ditherFSRgb9bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 17
                    ditherFSRgb12bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 18
                    ditherFSRgb15bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 19
                    ditherFSRgb18bpp(hpixels.AddrOfPinnedObject, w, h)
                Case 20
                    ditherSierraLiteRgbNbpp(hpixels.AddrOfPinnedObject, w, h, nColors)
                Case 21
                    ditherSierraLite(hpixels.AddrOfPinnedObject, w, h)
                Case 22
                    ditherSierraRgbNbpp(hpixels.AddrOfPinnedObject, w, h, nColors)
                Case 23
                    ditherSierra(hpixels.AddrOfPinnedObject, w, h)
            End Select
            If i = 0 Then
                dst1 = New cv.Mat(ocvb.color.Height, ocvb.color.Width, cv.MatType.CV_8UC3, pixels).Clone()
            Else
                dst2 = New cv.Mat(ocvb.color.Height, ocvb.color.Width, cv.MatType.CV_8UC3, pixels).Clone()
            End If
        Next
        hpixels.Free()
    End Sub
End Class
