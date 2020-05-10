Imports cv = OpenCvSharp
Imports System.Numerics

Module Quaternion_module
    Public Function quaternion_exp(v As cv.Point3f) As Quaternion
        v *= 0.5
        Dim theta2 = v.X * v.X + v.Y * v.Y + v.Z * v.Z
        Dim theta = Math.Sqrt(theta2)
        Dim c = Math.Cos(theta)
        Dim s = If(theta2 < Math.Sqrt(120 * Single.Epsilon), 1 - theta2 / 6, Math.Sin(theta) / theta2)
        Return New Quaternion(s * v.X, s * v.Y, s * v.Z, c)
    End Function
End Module

Public Class Quaterion_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders1.setupTrackBar1(ocvb, caller, "quaternion A.x X100", -100, 100, -50)
        sliders1.setupTrackBar2(ocvb, caller, "quaternion A.y X100", -100, 100, 10)
        sliders1.setupTrackBar3(ocvb, caller, "quaternion A.z X100", -100, 100, 20)
        sliders1.setupTrackBar4(ocvb, caller, "quaternion Theta X100", -100, 100, 100)

        sliders2.setupTrackBar1(ocvb, caller, "quaternion B.x X100", -100, 100, -10)
        sliders2.setupTrackBar2(ocvb, caller, "quaternion B.y X100", -100, 100, -10)
        sliders2.setupTrackBar3(ocvb, caller, "quaternion B.z X100", -100, 100, -10)
        sliders2.setupTrackBar4(ocvb, caller, "quaternion Theta X100", -100, 100, 100)

        ocvb.desc = "Use the quaternion values to multiply and compute conjugate"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim q1 = New Quaternion(CSng(sliders1.TrackBar1.Value / 100), CSng(sliders1.TrackBar2.Value / 100),
                                    CSng(sliders1.TrackBar3.Value / 100), CSng(sliders1.TrackBar4.Value / 100))
        Dim q2 = New Quaternion(CSng(sliders2.TrackBar1.Value / 100), CSng(sliders2.TrackBar2.Value / 100),
                                    CSng(sliders2.TrackBar3.Value / 100), CSng(sliders2.TrackBar4.Value / 100))

        Dim quatmul = Quaternion.Multiply(q1, q2)
        ocvb.putText(New ActiveClass.TrueType("q1 = " + q1.ToString() + vbCrLf +
                                                  "q2 = " + q2.ToString() + vbCrLf +
                                                  "Multiply q1 * q2" + quatmul.ToString(), 10, 60))
    End Sub
End Class




' https://github.com/IntelRealSense/librealsense/tree/master/examples/pose-predict
Public Class Quaterion_IMUPrediction
    Inherits ocvbClass
    Dim host As IMU_HostFrameTimes
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        host = New IMU_HostFrameTimes(ocvb, caller)

        ocvb.label1 = "Quaternion_IMUPrediction"
        ocvb.label2 = ""
        ocvb.desc = "IMU data arrives at the CPU after a delay.  Predict changes to the image based on delay and motion data."
    End Sub

    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 30 Then Exit Sub ' slow this down so it can be read!
        host.Run(ocvb)

        Dim dt = host.HostInterruptDelayEstimate

        Dim t = ocvb.parms.IMU_Translation
        Dim predictedTranslation = New cv.Point3f(dt * (dt / 2 * ocvb.parms.IMU_Acceleration.X + ocvb.parms.IMU_Velocity.X) + t.X,
                                                  dt * (dt / 2 * ocvb.parms.IMU_Acceleration.Y + ocvb.parms.IMU_Velocity.Y) + t.Y,
                                                  dt * (dt / 2 * ocvb.parms.IMU_Acceleration.Z + ocvb.parms.IMU_Velocity.Z) + t.Z)

        Dim predictedW = New cv.Point3f(dt * (dt / 2 * ocvb.parms.IMU_AngularAcceleration.X + ocvb.parms.IMU_AngularVelocity.X),
                                        dt * (dt / 2 * ocvb.parms.IMU_AngularAcceleration.Y + ocvb.parms.IMU_AngularVelocity.Y),
                                        dt * (dt / 2 * ocvb.parms.IMU_AngularAcceleration.Z + ocvb.parms.IMU_AngularVelocity.Z))

        Dim predictedRotation As New Quaternion
        predictedRotation = Quaternion.Multiply(quaternion_exp(predictedW), ocvb.parms.IMU_Rotation)

        Dim diffq = Quaternion.Subtract(ocvb.parms.IMU_Rotation, predictedRotation)

        ocvb.putText(New ActiveClass.TrueType("IMU_Acceleration = " + vbTab +
                                                  Format(ocvb.parms.IMU_Acceleration.X, "0.0000") + ", " + vbTab +
                                                  Format(ocvb.parms.IMU_Acceleration.Y, "0.0000") + ", " + vbTab +
                                                  Format(ocvb.parms.IMU_Acceleration.Z, "0.0000") + ", " + vbTab + vbCrLf +
                                                  "IMU_Velocity = " + vbTab + vbTab +
                                                  Format(ocvb.parms.IMU_Velocity.X, "0.0000") + ", " + vbTab +
                                                  Format(ocvb.parms.IMU_Velocity.Y, "0.0000") + ", " + vbTab +
                                                  Format(ocvb.parms.IMU_Velocity.Z, "0.0000") + ", " + vbTab + vbCrLf +
                                                  "IMU_AngularAccel. = " + vbTab +
                                                  Format(ocvb.parms.IMU_AngularAcceleration.X, "0.0000") + ", " + vbTab +
                                                  Format(ocvb.parms.IMU_AngularAcceleration.Y, "0.0000") + ", " + vbTab +
                                                  Format(ocvb.parms.IMU_AngularAcceleration.Z, "0.0000") + ", " + vbTab + vbCrLf +
                                                  "IMU_AngularVelocity = " + vbTab +
                                                  Format(ocvb.parms.IMU_AngularVelocity.X, "0.0000") + ", " + vbTab +
                                                  Format(ocvb.parms.IMU_AngularVelocity.Y, "0.0000") + ", " + vbTab +
                                                  Format(ocvb.parms.IMU_AngularVelocity.Z, "0.0000") + ", " + vbTab + vbCrLf +
                                                  "dt = " + dt.ToString() + vbCrLf +
                                                  "Pose quaternion = " + vbTab +
                                                  Format(ocvb.parms.IMU_Rotation.X, "0.0000") + ", " + vbTab +
                                                  Format(ocvb.parms.IMU_Rotation.Y, "0.0000") + ", " + vbTab +
                                                  Format(ocvb.parms.IMU_Rotation.Z, "0.0000") + ", " + vbTab + vbCrLf +
                                                  "Prediction Rotation = " + vbTab +
                                                  Format(predictedRotation.X, "0.0000") + ", " + vbTab +
                                                  Format(predictedRotation.Y, "0.0000") + ", " + vbTab +
                                                  Format(predictedRotation.Z, "0.0000") + ", " + vbTab + vbCrLf +
                                                  "difference = " + vbTab + vbTab +
                                                  Format(diffq.X, "0.0000") + ", " + vbTab +
                                                  Format(diffq.Y, "0.0000") + ", " + vbTab +
                                                  Format(diffq.Z, "0.0000") + ", " + vbTab, 10, 40))
    End Sub
End Class

