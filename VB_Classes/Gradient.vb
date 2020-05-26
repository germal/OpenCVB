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
        If ocvb.drawRect.Width > 0 Then sobel.src = ocvb.RGBDepth(ocvb.drawRect) Else sobel.src = ocvb.RGBDepth.Clone()
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
        grade.src = src
        grade.Run(ocvb)
        dst2 = grade.dst2
    End Sub
End Class








Public Class Gradient_DepthSmoothing
    Inherits ocvbClass
    Dim grad As Gradient_Depth
    Dim cmat As Depth_Colorizer_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        cmat = New Depth_Colorizer_CPP(ocvb, caller)

        grad = New Gradient_Depth(ocvb, caller)

        ocvb.desc = "Use gradient to smooth the depth values"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.drawRect.Width > 0 Then grad.src = src(ocvb.drawRect) Else grad.src = src
        grad.Run(ocvb)
        dst1 = grad.dst2.Resize(dst1.Size())

        Dim depth32f = getDepth32f(ocvb)
        If ocvb.drawRect.Width > 0 Then cmat.src = depth32f(ocvb.drawRect) Else cmat.src = depth32f
        cmat.Run(ocvb)
        dst2 = cmat.dst1.Resize(dst2.Size())
    End Sub
End Class