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
    Inherits VB_Class
        Dim srcData() As Byte
    Dim rbf As IntPtr
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, "RBF Recursion count", 1, 20, 2)
        
        ReDim srcData(ocvb.color.Total * ocvb.color.ElemSize - 1)

        rbf = RecursiveBilateralFilter_Open()
        ocvb.desc = "Apply the recursive bilateral filter"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Marshal.Copy(ocvb.color.Data, srcData, 0, srcData.Length)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = RecursiveBilateralFilter_Run(rbf, handleSrc.AddrOfPinnedObject(), ocvb.color.Rows, ocvb.color.Cols, sliders.TrackBar1.Value)
        handleSrc.Free() ' free the pinned memory...

        Dim dstData(ocvb.color.Total * ocvb.color.ElemSize - 1) As Byte
        Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
        ocvb.result1 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8UC3, dstData)
    End Sub
    Public Sub VBdispose()
        RecursiveBilateralFilter_Close(rbf)
            End Sub
End Class
