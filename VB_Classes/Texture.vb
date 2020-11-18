Imports cv = OpenCvSharp
Imports System.Threading
Public Class Texture_Basics
    Inherits VBparent
    Dim grid As Thread_Grid
    Dim ellipse As Draw_Ellipses
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        grid = New Thread_Grid(ocvb)
        Dim gridWidthSlider = findSlider("ThreadGrid Width")
        Dim gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 32
        gridHeightSlider.Value = 32
        grid.Run(ocvb)

        ellipse = New Draw_Ellipses(ocvb)
        ocvb.desc = "Use multi-threading to find the best sample 256x256 texture of a mask"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        If standalone Or src.Channels <> 1 Then
            ellipse.Run(ocvb)
            dst1 = ellipse.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst1 = dst1.ConvertScaleAbs(255)
            dst2 = ellipse.dst1.Clone
            dst2.SetTo(cv.Scalar.Yellow, grid.gridMask)
        Else
            dst1 = src
        End If

        Dim sortcounts As New SortedList(Of Integer, cv.Rect)(New compareAllowIdenticalIntegerInverted)
        Dim lock As New Mutex(True, "textureLock")
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            SyncLock lock
                sortcounts.Add(dst1(grid.roiList(i)).CountNonZero(), grid.roiList(i))
            End SyncLock
        End Sub)

        dst2.Rectangle(sortcounts.ElementAt(0).Value, cv.Scalar.White, 2)
    End Sub
End Class






Public Class Texture_Flow
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Texture Flow Delta", 2, 100, 12)
        sliders.setupTrackBar(1, "Texture Eigen BlockSize", 1, 100, 20)
        sliders.setupTrackBar(2, "Texture Eigen Ksize", 1, 15, 1)

        ocvb.desc = "Find and mark the texture flow in an image - see texture_flow.py.  Painterly Effect"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim TFdelta = sliders.trackbar(0).Value
        Dim TFblockSize = sliders.trackbar(1).Value * 2 + 1
        Dim TFksize = sliders.trackbar(2).Value * 2 + 1
        Dim gray = src.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY)
        dst1 = src.Clone
        Dim eigen = gray.CornerEigenValsAndVecs(TFblockSize, TFksize)
        Dim split = eigen.Split()
        Dim d2 = TFdelta / 2
        For y = d2 To dst1.Height - 1 Step d2
            For x = d2 To dst1.Width - 1 Step d2
                Dim delta = New cv.Point2f(split(4).Get(Of Single)(y, x), split(5).Get(Of Single)(y, x)) * TFdelta
                Dim p1 = New cv.Point(x - delta.X, y - delta.Y)
                Dim p2 = New cv.Point(x + delta.X, y + delta.Y)
                dst1.Line(p1, p2, cv.Scalar.Black, 1, cv.LineTypes.AntiAlias)
            Next
        Next
    End Sub
End Class





Public Class Texture_Flow_Depth
    Inherits VBparent
    Dim texture As Texture_Flow
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        texture = New Texture_Flow(ocvb)
        ocvb.desc = "Display texture flow in the depth data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        texture.src = ocvb.RGBDepth
        texture.Run(ocvb)
        dst1 = texture.dst1
    End Sub
End Class
