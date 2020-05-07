

Imports cv = OpenCvSharp
Public Class LUT_Gray
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "LUT zero through xxx", 1, 255, 65)
        sliders.setupTrackBar2(ocvb, caller, "LUT xxx through yyy", 1, 255, 110)
        sliders.setupTrackBar3(ocvb, caller, "LUT xxx through yyy", 1, 255, 160)
        sliders.setupTrackBar4(ocvb, caller, "LUT xxx through 255", 1, 255, 210)
        ocvb.desc = "Use an OpenCV Lookup Table to define 5 regions in a grayscale image - Painterly Effect."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        sliders.LabelSlider1.Text = "LUT zero through " + CStr(sliders.TrackBar1.Value)
        sliders.LabelSlider2.Text = "LUT " + CStr(sliders.TrackBar1.Value) + " through " + CStr(sliders.TrackBar2.Value)
        sliders.LabelSlider3.Text = "LUT " + CStr(sliders.TrackBar2.Value) + " through " + CStr(sliders.TrackBar3.Value)
        sliders.LabelSlider4.Text = "LUT " + CStr(sliders.TrackBar3.Value) + " through 255"
        Dim splits = {sliders.TrackBar1.Value, sliders.TrackBar2.Value, sliders.TrackBar3.Value, sliders.TrackBar4.Value, 255}
        Dim vals = {0, sliders.TrackBar1.Value, sliders.TrackBar2.Value, sliders.TrackBar3.Value, 255}
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim myLut As New cv.Mat(1, 256, cv.MatType.CV_8U)
        Dim splitIndex As Int32
        For i = 0 To 255
            myLut.Set(Of Byte)(0, i, vals(splitIndex))
            If i >= splits(splitIndex) Then splitIndex += 1
        Next
        ocvb.result1 = gray.LUT(myLut)
    End Sub
End Class




' https://github.com/opencv/opencv/blob/master/samples/cpp/falsecolor.cpp
Public Class LUT_Color
    Inherits ocvbClass
    Public paletteMap(256) As cv.Vec3b
    Public src As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Build and use a custom color palette - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            paletteMap = ocvb.rColors
            src = ocvb.color
            src /= 64
            src *= 64
        End If
        Dim colorMat = New cv.Mat(1, 256, cv.MatType.CV_8UC3, paletteMap)
        ocvb.result1 = src.LUT(colorMat)
        ocvb.result2 = colorMat.Resize(src.Size())
    End Sub
End Class