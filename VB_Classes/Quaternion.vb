Imports System
Imports System.Numerics
Public Class Quaterion_Basics : Implements IDisposable
    Dim sliders1 As New OptionsSliders
    Dim sliders2 As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders1.setupTrackBar1(ocvb, "quaternion A.x X100", -100, 100, 0)
        sliders1.setupTrackBar2(ocvb, "quaternion A.y X100", -100, 100, 0)
        sliders1.setupTrackBar3(ocvb, "quaternion A.z X100", -100, 100, 0)
        sliders1.setupTrackBar4(ocvb, "quaternion Theta X100", -100, 100, 100)
        If ocvb.parms.ShowOptions Then sliders1.Show()

        sliders2.setupTrackBar1(ocvb, "quaternion B.x X100", -100, 100, 0)
        sliders2.setupTrackBar2(ocvb, "quaternion B.y X100", -100, 100, 0)
        sliders2.setupTrackBar3(ocvb, "quaternion B.z X100", -100, 100, 0)
        sliders2.setupTrackBar4(ocvb, "quaternion Theta X100", -100, 100, 100)
        If ocvb.parms.ShowOptions Then sliders2.Show()

        ocvb.desc = "Use the quaternion values to multiply and compute conjugate"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim q1 = New Quaternion(CSng(sliders1.TrackBar1.Value / 100), CSng(sliders1.TrackBar2.Value / 100),
                                CSng(sliders1.TrackBar3.Value / 100), CSng(sliders1.TrackBar4.Value / 100))
        Dim q2 = New Quaternion(CSng(sliders2.TrackBar1.Value / 100), CSng(sliders2.TrackBar2.Value / 100),
                                CSng(sliders2.TrackBar3.Value / 100), CSng(sliders2.TrackBar4.Value / 100))

        Dim quatmul = Quaternion.Multiply(q1, q2)
        ocvb.putText(New ActiveClass.TrueType("q1 = " + q1.ToString(), 10, 60))
        ocvb.putText(New ActiveClass.TrueType("q2 = " + q2.ToString(), 10, 80))
        ocvb.putText(New ActiveClass.TrueType("Multiply q1 * q2" + quatmul.ToString(), 10, 100))

    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class





Public Class Quaterion_IMUPrediction : Implements IDisposable
    Dim imu As IMU_Time
    Public Sub New(ocvb As AlgorithmData)
        imu = New IMU_Time(ocvb)
        imu.check.Hide()
        imu.plot.sliders.Hide()
        imu.externalUse = True

        ocvb.desc = "IMU arrives at the CPU after a delay.  Predict changes to the image based on delay and motion data."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        imu.Run(ocvb)

        Dim dt = imu.smoothedDelta ' this is the time from IMU measurement to the time the CPU got the pose data.
        ocvb.putText(New ActiveClass.TrueType("Pose quaternion = " + ocvb.parms.IMU_Rotation.ToString(), 10, 60))


        '    rs2_pose P = pose;
        'P.translation.x = dt_s * (dt_s / 2 * pose.acceleration.x + pose.velocity.x) + pose.translation.x;
        'P.translation.y = dt_s * (dt_s / 2 * pose.acceleration.y + pose.velocity.y) + pose.translation.y;
        'P.translation.z = dt_s * (dt_s / 2 * pose.acceleration.z + pose.velocity.z) + pose.translation.z;
        'rs2_vector W = {
        '        dt_s * (dt_s / 2 * pose.angular_acceleration.x + pose.angular_velocity.x),
        '        dt_s * (dt_s / 2 * pose.angular_acceleration.y + pose.angular_velocity.y),
        '        dt_s * (dt_s / 2 * pose.angular_acceleration.z + pose.angular_velocity.z),
        '};
        'P.rotation = quaternion_multiply(quaternion_exp(W), pose.rotation);
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        imu.Dispose()
    End Sub
End Class