Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module SemiGlobalMatching_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SemiGlobalMatching_Open(rows As Int32, cols As Int32) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub SemiGlobalMatching_Close(SemiGlobalMatchingPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function SemiGlobalMatching_Run(SemiGlobalMatchingPtr As IntPtr, leftPtr As IntPtr, rightPtr As IntPtr, rows As Int32, cols As Int32,
                                           disparityRange As Int32) As IntPtr
    End Function
End Module





' https://github.com/epiception/SGM-Census
'Public Class SemiGlobalMatching_CPP
'    Inherits VB_Class
'    Dim leftData() As Byte
'    Dim rightData() As Byte
'    Dim SemiGlobalMatching As IntPtr
'     Public Sub New(ocvb As AlgorithmData, byVal caller as string)
' dim callerName = caller 
' If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
'        ReDim leftData(ocvb.color.Total - 1)
'        ReDim rightData(ocvb.color.Total - 1)
'        SemiGlobalMatching = SemiGlobalMatching_Open(ocvb.color.Rows, ocvb.color.Cols)
'        ocvb.desc = "Find depth using the semi-global matching algorithm."
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        If ocvb.frameCount < 10 Then Exit Sub
'        If ocvb.parms.cameraIndex = Kinect4AzureCam Then
'            ocvb.putText(New ActiveClass.TrueType("The left and right views are identical with the Microsoft Kinect 4 Azure camera.", 10, 60, RESULT1))
'            Exit Sub
'        End If
'        Marshal.Copy(ocvb.leftView.Data, leftData, 0, leftData.Length)
'        Marshal.Copy(ocvb.rightView.Data, rightData, 0, rightData.Length)
'        Dim handleLeft = GCHandle.Alloc(leftData, GCHandleType.Pinned)
'        Dim handleRight = GCHandle.Alloc(rightData, GCHandleType.Pinned)
'        Dim imagePtr = SemiGlobalMatching_Run(SemiGlobalMatching, handleLeft.AddrOfPinnedObject(), handleRight.AddrOfPinnedObject(),
'                                              ocvb.leftView.Rows, ocvb.leftView.Cols, 3)
'        handleLeft.Free() ' free the pinned memory...
'        handleRight.Free() ' free the pinned memory...
'        Dim dstData(ocvb.color.Total - 1) As Byte
'        Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
'        Dim dst = New cv.Mat(ocvb.leftView.Rows, ocvb.leftView.Cols, cv.MatType.CV_8U, dstData)
'        ocvb.result1 = dst.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
'    End Sub
'    Public Sub MyDispose()
'        SemiGlobalMatching_Close(SemiGlobalMatching)
'    End Sub
'End Class
