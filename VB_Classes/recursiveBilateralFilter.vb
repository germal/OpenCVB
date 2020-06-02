Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module RecursiveBilateralFilter_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RecursiveBilateralFilter_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub RecursiveBilateralFilter_Close(rbf As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function RecursiveBilateralFilter_Run(rbf As IntPtr, inputPtr As IntPtr, rows As Int32, cols As Int32, recursions As Int32) As IntPtr
    End Function
End Module


' https://github.com/ufoym
Public Class RecursiveBilateralFilter_CPP
    Inherits ocvbClass
    Dim srcData(0) As Byte
    Dim rbf As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "RBF Recursion count", 1, 20, 2)
        rbf = RecursiveBilateralFilter_Open()
        ocvb.desc = "Apply the recursive bilateral filter"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If srcData.Length <> src.Total * src.ElemSize Then ReDim srcData(src.Total * src.ElemSize - 1)
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = RecursiveBilateralFilter_Run(rbf, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, sliders.TrackBar1.Value)
        handleSrc.Free() ' free the pinned memory...

        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)
    End Sub
    Public Sub Close()
        RecursiveBilateralFilter_Close(rbf)
    End Sub
End Class

