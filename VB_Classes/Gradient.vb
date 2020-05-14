Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class Gradient_Basics
    Inherits ocvbClass
    Dim sobel As Edges_Sobel
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sobel = New Edges_Sobel(ocvb, caller)
        ocvb.desc = "Use phase to compute gradient"
        label2 = "Phase Output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then src = ocvb.color
        sobel.src = src
        sobel.Run(ocvb)
        Dim angle = New cv.Mat
        Dim x32f As New cv.Mat
        Dim y32f As New cv.Mat
        sobel.grayX.ConvertTo(x32f, cv.MatType.CV_32F)
        sobel.grayY.ConvertTo(y32f, cv.MatType.CV_32F)
        cv.Cv2.Phase(x32f, y32f, angle)
        Dim gray = angle.Normalize(255, 0, cv.NormTypes.MinMax)
        gray.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst1 = sobel.dst1
    End Sub
End Class




Public Class Gradient_Depth
    Inherits ocvbClass
    Dim sobel As Edges_Sobel
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sobel = New Edges_Sobel(ocvb, caller)
        ocvb.desc = "Use phase to compute gradient on depth image"
        label2 = "Phase Output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        sobel.src = ocvb.RGBDepth.Clone()
        sobel.Run(ocvb)
        Dim angle = New cv.Mat
        Dim x32f As New cv.Mat
        Dim y32f As New cv.Mat
        sobel.grayX.ConvertTo(x32f, cv.MatType.CV_32F)
        sobel.grayY.ConvertTo(y32f, cv.MatType.CV_32F)
        cv.Cv2.Phase(x32f, y32f, angle)
        Dim gray = angle.Normalize(255, 0, cv.NormTypes.MinMax)
        gray.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst1 = sobel.dst1
    End Sub
End Class






Public Class Gradient_Flatland
    Inherits ocvbClass
    Dim grade As Gradient_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        grade = New Gradient_Basics(ocvb, caller)
        sliders.setupTrackBar1(ocvb, caller, "Reduction Factor", 1, 64, 16)
        ocvb.desc = "Reduced grayscale shows isobars in depth."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim reductionFactor = sliders.TrackBar1.Maximum - sliders.TrackBar1.Value
        dst1 = ocvb.RGBDepth.Clone()
        dst1 /= reductionFactor
        dst1 *= reductionFactor
        grade.Run(ocvb)
    End Sub
End Class

