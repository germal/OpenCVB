Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module Rodrigues_Basics_Exports
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectRodrigues() As IntPtr
    End Function
End Module


Public Class Rodrigues_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Compute Rodrigues calibration for Kinect camera (only)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex <> Kinect4AzureCam Then
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





Public Class Rodrigues_RotationMatrix : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Display the contents of the IMU Rotation Matrix"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rot = ocvb.parms.IMU_RotationMatrix
        Dim output As String = "IMU Rotation Matrix (rotate the camera to see if it is working)" + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(rot(i * 3), "#0.00") + vbTab + Format(rot(i * 3 + 1), "#0.00") + vbTab + Format(rot(i * 3 + 2), "#0.00") + vbCrLf
        Next
        ocvb.putText(New ActiveClass.TrueType(output, 10, 90, RESULT1))

        ' Dim src(,) As Double = {{rot(0), rot(1), rot(2)}, {rot(3), rot(4), rot(5)}, {rot(6), rot(7), rot(8)}}
        Dim src32f As New cv.Mat(3, 3, cv.MatType.CV_32F, ocvb.parms.IMU_RotationMatrix)
        Dim src64f As New cv.Mat
        src32f.ConvertTo(src64f, cv.MatType.CV_64F)
        Dim Jacobian As New cv.Mat(9, 3, cv.MatType.CV_32F, 0)
        Dim dst As New cv.Mat(3, 1, cv.MatType.CV_32F, 3)
        cv.Cv2.Rodrigues(src32f, dst, Jacobian)

        'cv.Cv2.GetRotationMatrix2D()
        output = "Rotation matrix produces the following Rotation Vector after Rodrigues: " + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(dst.At(Of Double)(i), "#0.00") + vbTab
        Next
        ocvb.putText(New ActiveClass.TrueType(output, 10, 150, RESULT1))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class