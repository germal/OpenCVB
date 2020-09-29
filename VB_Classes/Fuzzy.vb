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
        palette.src = dst1
        palette.Run(ocvb)
        dst1 = palette.dst1
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