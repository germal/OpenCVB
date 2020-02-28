Imports System
Imports System.Numerics
Public Class Quaterion_Basics : Implements IDisposable
    Dim imu As IMU_Time
    Public Sub New(ocvb As AlgorithmData)
        imu = New IMU_Time(ocvb)
        imu.externalUse = True

        ocvb.desc = "Exploring System.Numerics.Quaternion"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        imu.Run(ocvb)
        ' ocvb.putText(New ActiveClass.TrueType("Delta ms = " + Format(imu.deltaTime, "#0.000000"), 10, 80))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        imu.Dispose()
    End Sub
End Class