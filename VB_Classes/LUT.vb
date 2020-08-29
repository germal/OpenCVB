

Imports cv = OpenCvSharp
Public Class LUT_Gray
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "LUT zero through xxx", 1, 255, 65)
        sliders.setupTrackBar(1, "LUT xxx through yyy", 1, 255, 110)
        sliders.setupTrackBar(2, "LUT xxx through yyy", 1, 255, 160)
        sliders.setupTrackBar(3, "LUT xxx through 255", 1, 255, 210)
        setDescription(ocvb, "Use an OpenCV Lookup Table to define 5 regions in a grayscale image - Painterly Effect.")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        sliders.sLabels(0).Text = "LUT zero through " + CStr(sliders.trackbar(0).Value)
        sliders.sLabels(1).Text = "LUT " + CStr(sliders.trackbar(0).Value) + " through " + CStr(sliders.trackbar(1).Value)
        sliders.sLabels(2).Text = "LUT " + CStr(sliders.trackbar(1).Value) + " through " + CStr(sliders.trackbar(2).Value)
        sliders.sLabels(3).Text = "LUT " + CStr(sliders.trackbar(2).Value) + " through 255"
        Dim splits = {sliders.trackbar(0).Value, sliders.trackbar(1).Value, sliders.trackbar(2).Value, sliders.trackbar(3).Value, 255}
        Dim vals = {0, sliders.trackbar(0).Value, sliders.trackbar(1).Value, sliders.trackbar(2).Value, 255}
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim myLut As New cv.Mat(1, 256, cv.MatType.CV_8U)
        Dim splitIndex As Int32
        For i = 0 To 255
            myLut.Set(Of Byte)(0, i, vals(splitIndex))
            If i >= splits(splitIndex) Then splitIndex += 1
        Next
        dst1 = gray.LUT(myLut)
    End Sub
End Class




' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class LUT_Color
    Inherits ocvbClass
    Public paletteMap(256) As cv.Vec3b
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        paletteMap = rColors
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Reduction for color image", 1, 256, 32)
        setDescription(ocvb, "Build and use a custom color palette - Painterly Effect")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim reduction = sliders.trackbar(0).Value
        If standalone Then
            src /= reduction
            src *= reduction
        End If
        Dim colorMat = New cv.Mat(1, 256, cv.MatType.CV_8UC3, paletteMap)
        dst1 = src.LUT(colorMat)
        If standalone Then dst2 = colorMat.Resize(src.Size())
    End Sub
End Class






' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
' https://docs.opencv.org/2.4/modules/core/doc/operations_on_arrays.html
Public Class LUT_Basics
    Inherits ocvbClass
    Public paletteMap(256 - 1) As Byte
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        For i = 0 To paletteMap.Count - 1
            paletteMap(i) = i
        Next
        setDescription(ocvb, "Rebuild any grayscale image with a 256 element Look-Up Table")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim lut = New cv.Mat(1, 256, cv.MatType.CV_8U, paletteMap)
        dst1 = src.LUT(lut)
        If standalone Then dst2 = lut.Resize(src.Size())
    End Sub
End Class
