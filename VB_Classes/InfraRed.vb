Imports cv = OpenCvSharp
Public Class InfraRed_Basics : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "brightness", 0, 255, 100)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Show the infrared images from the Intel RealSense Camera"
        ocvb.label1 = "Infrared Left Image"
        If ocvb.parms.UsingIntelCamera Then
            ocvb.label2 = "Infrared Right Image"
        Else
            ocvb.label2 = "There is only one infrared image on Kinect cameras"
            sliders.TrackBar1.Value = 0
        End If
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.redLeft += sliders.TrackBar1.Value
        ocvb.redLeft.CopyTo(ocvb.result1)
        ocvb.redRight += sliders.TrackBar1.Value
        ocvb.redRight.CopyTo(ocvb.result2)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class InfraRed_Features : Implements IDisposable
    Dim features As Features_GoodFeatures
    Public Sub New(ocvb As AlgorithmData)
        features = New Features_GoodFeatures(ocvb)
        features.externalUse = True

        ocvb.desc = "Find GoodFeatures in the left and right depalettized infrared images"
        ocvb.label1 = "Infrared Left Image"
        ocvb.label2 = "Infrared Right Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        features.gray = ocvb.redRight
        features.Run(ocvb)
        ocvb.result1.CopyTo(ocvb.result2) ' save the right image

        features.gray = ocvb.redLeft
        features.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        features.Dispose()
    End Sub
End Class




Public Class InfraRed_Palettized : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Add color to the 8-bit infrared images."
        ocvb.label1 = "Infrared Left Image"
        ocvb.label2 = "Infrared Right Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = ocvb.redLeft.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.ApplyColorMap(ocvb.result1, ocvb.result1, cv.ColormapTypes.Rainbow)

        ocvb.result2 = ocvb.redRight.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.ApplyColorMap(ocvb.result2, ocvb.result2, cv.ColormapTypes.Rainbow)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class InfraRed_BRISK : Implements IDisposable
    Dim brisk As BRISK_Basics
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Add color to the 8-bit infrared images."
        ocvb.label1 = "Infrared Left Image"
        ocvb.label2 = "Infrared Right Image"

        brisk = New BRISK_Basics(ocvb)
        brisk.externalUse = True
        brisk.sliders.TrackBar1.Value = 20
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        brisk.src = ocvb.redRight.Clone()
        ocvb.result2 = ocvb.redRight.Clone()
        brisk.Run(ocvb)

        For Each pt In brisk.features
            ocvb.result2.Circle(pt, 2, cv.Scalar.Green, -1, cv.LineTypes.AntiAlias)
        Next

        brisk.src = ocvb.redLeft.Clone()
        ocvb.result1 = ocvb.redLeft.Clone()
        brisk.Run(ocvb)

        For Each pt In brisk.features
            ocvb.result1.Circle(pt, 2, cv.Scalar.Green, -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        brisk.Dispose()
    End Sub
End Class


