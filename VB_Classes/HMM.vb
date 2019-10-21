
Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module HMM_CPP_Module
    <DllImport(("CPP_Algorithms.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function HMM_Open() As IntPtr
    End Function
    <DllImport(("CPP_Algorithms.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub HMM_Close(HMMPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Algorithms.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function HMM_Run(HMMPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32, channels As Int32) As IntPtr
    End Function
End Module



'https://github.com/omidsakhi/cv-hmm
Public Class HMM_Example_CPP : Implements IDisposable
    Dim HMM As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        HMM = HMM_Open()
        ocvb.label1 = "HMM - see Visual Studio Output for results"
        ocvb.desc = "Simple test of Hidden Markov Model - text output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim src = ocvb.color
        Dim srcData(src.Total * src.ElemSize) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = HMM_Run(HMM, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            ocvb.result1 = New cv.Mat(src.Rows, src.Cols, IIf(src.Channels = 3, cv.MatType.CV_8UC3, cv.MatType.CV_8UC1), dstData)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        HMM_Close(HMM)
    End Sub
End Class
