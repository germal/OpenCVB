Imports cv = OpenCvSharp
Public Class TextureFlow_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public src As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Texture Flow Delta", 2, 100, 12)
        sliders.setupTrackBar2(ocvb, "Texture Eigen BlockSize", 1, 100, 20)
        sliders.setupTrackBar3(ocvb, "Texture Eigen Ksize", 1, 15, 1)
        sliders.Show()

        ocvb.desc = "Find and mark the texture flow in an image - see texture_flow.py.  Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim TFdelta = sliders.TrackBar1.Value
        Dim TFblockSize = sliders.TrackBar2.Value * 2 + 1
        Dim TFksize = sliders.TrackBar3.Value * 2 + 1
        If externalUse = False Then src = ocvb.color
        Dim gray = src.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY)
        ocvb.result1 = src.Clone
        Dim eigen = gray.CornerEigenValsAndVecs(TFblockSize, TFksize)
        Dim split = eigen.Split()
        Dim d2 = TFdelta / 2
        For y = d2 To ocvb.result1.Height - 1 Step d2
            For x = d2 To ocvb.result1.Width - 1 Step d2
                Dim delta = New cv.Point2f(split(4).At(Of Single)(y, x), split(5).At(Of Single)(y, x)) * TFdelta
                Dim p1 = New cv.Point(x - delta.X, y - delta.Y)
                Dim p2 = New cv.Point(x + delta.X, y + delta.Y)
                ocvb.result1.Line(p1, p2, cv.Scalar.Black, 1, cv.LineTypes.AntiAlias)
            Next
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class TextureFlow_Depth : Implements IDisposable
    Dim texture As TextureFlow_Basics
    Public Sub New(ocvb As AlgorithmData)
        texture = New TextureFlow_Basics(ocvb)
        texture.externalUse = True
        ocvb.desc = "Display texture flow in the depth data"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        texture.src = ocvb.depthRGB
        texture.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        texture.Dispose()
    End Sub
End Class