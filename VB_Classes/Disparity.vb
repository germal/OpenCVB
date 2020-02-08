Imports cv = OpenCvSharp
Public Class Disparity_Basics : Implements IDisposable
    Dim colorizer As Depth_Colorizer_1_CPP
    Public Sub New(ocvb As AlgorithmData)
        colorizer = New Depth_Colorizer_1_CPP(ocvb)
        colorizer.externalUse = True

        ocvb.desc = "Show disparity from RealSense camera"
        If ocvb.parms.cameraIndex = D400Cam Then
            ocvb.label1 = "Disparity Image (not depth)"
        Else
            ocvb.label1 = "Kinect Camera Disparity is also the Depth"
        End If
        ocvb.label2 = "Left View"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.disparity.ConvertTo(colorizer.src, cv.MatType.CV_16U)
        colorizer.Run(ocvb)
        ocvb.result1 = colorizer.dst
        ocvb.result2 = ocvb.leftView.Resize(ocvb.color.Size())
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        colorizer.Dispose()
    End Sub
End Class
