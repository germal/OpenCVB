Imports cv = OpenCvSharp
Public Class EigenLib_Manual : Implements IDisposable
    Dim imu As IMU_AnglesToGravity
    Public Sub New(ocvb As AlgorithmData)
        imu = New IMU_AnglesToGravity(ocvb)
        ocvb.label1 = "To set input, draw anywhere on the image."
        ocvb.desc = "Given a set of points, rotate and translate them with the gravity vector."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        imu.Run(ocvb)

        Dim zCos = Math.Cos(-imu.angleZ)
        Dim zSin = Math.Sin(-imu.angleZ)
        Dim split() = cv.Cv2.Split(ocvb.pointCloud)
        split(0) = zCos * split(0) - zSin * split(1)
        split(1) = zSin * split(0) + zCos * split(1)
        Dim newZmat As New cv.Mat
        cv.Cv2.Merge(split, newZmat)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        imu.Dispose()
    End Sub
End Class