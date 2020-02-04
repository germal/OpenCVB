
Imports cv = OpenCvSharp
Public Class FishEye_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use OpenCV's FishEye API to undistort a fisheye lens input"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.cameraIndex = T265Camera Then

        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class