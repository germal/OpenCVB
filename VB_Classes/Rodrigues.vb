Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module Rodrigues_Basics_Exports
    <DllImport(("Camera_Kinect4Azure.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectRodrigues() As IntPtr
    End Function
End Module


Public Class Rodrigues_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Compute Rodrigues calibration for Kinect camera (only)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.UsingIntelCamera Then
            ocvb.result2.SetTo(0)
            ocvb.putText(New ActiveClass.TrueType("Only the Kinect4Azure camera is currently supported for the Rodrigues calibration", 10, 140, RESULT1))
            Exit Sub
        End If

        Dim out As IntPtr = KinectRodrigues()
        Dim msg = Marshal.PtrToStringAnsi(out)
        Dim split As String() = msg.Split(vbLf)

        For i = 0 To split.Length - 1
            ocvb.putText(New ActiveClass.TrueType(split(i), 10, 90 + i * 20, RESULT1))
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class