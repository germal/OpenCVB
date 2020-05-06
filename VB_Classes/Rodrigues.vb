Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module Rodrigues_Basics_Exports
    <DllImport(("Cam_Kinect4.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function KinectRodrigues() As IntPtr
    End Function
End Module


Public Class Rodrigues_ValidateKinect
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.desc = "Validate the Rodrigues calibration for Kinect camera (only)"
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
    Public Sub VBdispose()
    End Sub
End Class




Public Class Rodrigues_ValidateVector
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.desc = "Validate the Rodrigues calibration for Stereolabs Zed 2 camera (only)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex <> StereoLabsZED2 And ocvb.parms.cameraIndex <> T265Camera Then
            ocvb.result2.SetTo(0)
            ocvb.putText(New ActiveClass.TrueType("Only the StereoLabs Zed 2 and Intel T265 cameras are supported for this Rodrigues validation", 10, 140, RESULT1))
            Exit Sub
        End If

        Dim rot = ocvb.parms.IMU_RotationMatrix
        Dim output As String = "IMU Rotation Matrix for Zed 2 camera" + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(rot(i * 3), "#0.000000") + vbTab + Format(rot(i * 3 + 1), "#0.0000000") + vbTab + Format(rot(i * 3 + 2), "#0.0000000") + vbCrLf
        Next
        ocvb.putText(New ActiveClass.TrueType(output, 10, 90, RESULT1))

        Dim src As New cv.Mat(3, 3, cv.MatType.CV_32F, ocvb.parms.IMU_RotationMatrix)
        Dim dst As New cv.Mat(3, 1, src.Type)
        cv.Cv2.Rodrigues(src, dst)

        output = "Rotation matrix produces the following Rotation Vector after Rodrigues: " + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(dst.Get(Of Single)(i), "#0.000000000") + vbTab
        Next
        ocvb.putText(New ActiveClass.TrueType(output, 10, 150, RESULT1))

        output = "Rotation Vector from IMU: " + vbCrLf
        output += vbTab + Format(ocvb.parms.IMU_RotationVector.X, "#0.000000000") + vbTab
        output += vbTab + Format(ocvb.parms.IMU_RotationVector.Y, "#0.000000000") + vbTab
        output += vbTab + Format(ocvb.parms.IMU_RotationVector.Z, "#0.000000000") + vbTab
        ocvb.putText(New ActiveClass.TrueType(output, 10, 190, RESULT1))

        If ocvb.parms.cameraIndex = T265Camera Then
            ocvb.putText(New ActiveClass.TrueType("The T265 does not provide the Rotation Matrix but it is calculated from the Rotation Vector.", 10, 220, RESULT1))
        End If
    End Sub
    Public Sub VBdispose()
    End Sub
End Class





Public Class Rodrigues_RotationMatrix
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.desc = "Display the contents of the IMU Rotation Matrix"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rot = ocvb.parms.IMU_RotationMatrix
        Dim output As String = "IMU Rotation Matrix (rotate the camera to see if it is working)" + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(rot(i * 3), "#0.000000") + vbTab + Format(rot(i * 3 + 1), "#0.0000000") + vbTab + Format(rot(i * 3 + 2), "#0.0000000") + vbCrLf
        Next
        ocvb.putText(New ActiveClass.TrueType(output, 10, 90, RESULT1))

        Dim src As New cv.Mat(3, 3, cv.MatType.CV_32F, ocvb.parms.IMU_RotationMatrix)
        Dim dst As New cv.Mat(3, 1, src.Type, 3)
        cv.Cv2.Rodrigues(src, dst)

        output = "Rotation matrix produces the following Rotation Vector after Rodrigues: " + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(dst.Get(Of Single)(i), "#0.000000000") + vbTab
        Next
        ocvb.putText(New ActiveClass.TrueType(output, 10, 150, RESULT1))
    End Sub
    Public Sub VBdispose()
    End Sub
End Class







Public Class Rodrigues_Extrinsics
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.desc = "Convert Camera extrinsics array to a Vector with Rodrigues"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rot = ocvb.parms.extrinsics.rotation
        Dim output As String = "Extrinsics Rotation Matrix" + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(rot(i * 3), "#0.00") + vbTab + Format(rot(i * 3 + 1), "#0.00") + vbTab + Format(rot(i * 3 + 2), "#0.00") + vbCrLf
        Next
        ocvb.putText(New ActiveClass.TrueType(output, 10, 90, RESULT1))

        Dim src32f As New cv.Mat(3, 3, cv.MatType.CV_32F, ocvb.parms.extrinsics.rotation)
        Dim src As New cv.Mat
        src32f.ConvertTo(src, cv.MatType.CV_64F)
        Dim Jacobian As New cv.Mat(9, 3, src.Type, 0)
        Dim dst As New cv.Mat(3, 1, src.Type, 3)
        cv.Cv2.Rodrigues(src, dst)

        output = "Extrinsic Rotation matrix produces the following Rotation Vector after Rodrigues: " + vbCrLf
        For i = 0 To 2
            output += vbTab + Format(dst.Get(of Double)(i), "#0.000000000") + vbTab
        Next
        ocvb.putText(New ActiveClass.TrueType(output, 10, 150, RESULT1))
    End Sub
    Public Sub VBdispose()
    End Sub
End Class
