Imports cv = OpenCvSharp
Public Class Brightness_Clahe : Implements IDisposable ' Contrast Limited Adaptive Histogram Equalization (CLAHE)
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Clip Limit", 1, 100, 10)
        sliders.setupTrackBar2(ocvb, "Grid Size", 1, 100, 8)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Show a Contrast Limited Adaptive Histogram Equalization image (CLAHE)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim imgGray = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
        Dim imgClahe = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
        cv.Cv2.CvtColor(ocvb.color, imgGray, cv.ColorConversionCodes.BGR2GRAY)

        Dim claheObj = cv.Cv2.CreateCLAHE()
        ' claheObj.SetTilesGridSize(New cv.Size(sliders.TrackBar1.Value, sliders.TrackBar2.Value))
        ' claheObj.SetClipLimit(sliders.TrackBar1.Value)
        claheObj.TilesGridSize() = New cv.Size(sliders.TrackBar1.Value, sliders.TrackBar2.Value)
        claheObj.ClipLimit = sliders.TrackBar1.Value
        claheObj.Apply(imgGray, imgClahe)

        ocvb.label1 = "GrayScale"
        ocvb.label2 = "CLAHE Result"
        cv.Cv2.CvtColor(imgGray, ocvb.result1, cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.CvtColor(imgClahe, ocvb.result2, cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Brightness_Contrast : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Brightness", 1, 100, 50)
        sliders.setupTrackBar2(ocvb, "Contrast", 1, 100, 50)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Show image with vary contrast and brightness."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.color.ConvertTo(ocvb.result1, -1, sliders.TrackBar2.Value / 50, sliders.TrackBar1.Value)
        ocvb.label1 = "Brightness/Contrast"
        ocvb.label2 = ""
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Brightness_hue : Implements IDisposable
    Public hsv_planes(2) As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Show hue (Result1) and Saturation (Result2)."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim imghsv = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC3)
        cv.Cv2.CvtColor(ocvb.color, imghsv, cv.ColorConversionCodes.RGB2HSV)
        cv.Cv2.Split(imghsv, hsv_planes)

        ocvb.label1 = "Hue"
        ocvb.label2 = "Saturation"
        cv.Cv2.CvtColor(hsv_planes(0), ocvb.result1, cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.CvtColor(hsv_planes(1), ocvb.result2, cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class



Public Class Brightness_AlphaBeta : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use alpha and beta with ConvertScaleAbs."
        sliders.setupTrackBar1(ocvb, "Brightness Alpha (contrast)", 0, 500, 300)
        sliders.setupTrackBar2(ocvb, "Brightness Beta (brightness)", -100, 100, 0)
        If ocvb.parms.ShowOptions Then sliders.show()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = ocvb.color.ConvertScaleAbs(sliders.TrackBar1.Value / 500, sliders.TrackBar2.Value)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Brightness_Gamma : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim lookupTable(255) As Byte
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use gamma with ConvertScaleAbs."
        sliders.setupTrackBar1(ocvb, "Brightness Gamma correction", 0, 200, 100)
        If ocvb.parms.ShowOptions Then sliders.show()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static lastGamma As Int32 = -1
        If lastGamma <> sliders.TrackBar1.Value Then
            lastGamma = sliders.TrackBar1.Value
            For i = 0 To lookupTable.Length - 1
                lookupTable(i) = Math.Pow(i / 255, sliders.TrackBar1.Value / 100) * 255
            Next
        End If
        ocvb.result1 = ocvb.color.LUT(lookupTable)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class

