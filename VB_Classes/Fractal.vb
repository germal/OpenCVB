﻿Imports System.Numerics
Imports cv = OpenCvSharp
' https://medium.com/farouk-ounanes-home-on-the-internet/mandelbrot-set-in-c-from-scratch-c7ad6a1bf2d9
Public Class Fractal_Mandelbrot
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Mandelbrot iterations", 1, 50, 34)
        ocvb.desc = "Run the classic Mandalbrot algorithm"
        dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim iterations = sliders.TrackBar1.Value
        Static saveIterations As Integer
        If saveIterations <> iterations Then
            saveIterations = iterations
            dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
            For y = 0 To src.Height - 1
                For x = 0 To src.Width - 1
                    Dim c = New Complex(x * 4 / src.Width - 2, y * 3 / src.Height - 1.5)
                    Dim z = New Complex(0, 0)
                    Dim iter = 0
                    While Complex.Abs(z) < 2 And iter < iterations
                        z = z * z + c
                        iter += 1
                    End While
                    dst1.Set(Of Byte)(y, x, If(iter < iterations, 255 * iter / (iterations - 1), 0))
                Next
            Next
        End If
    End Sub
End Class





' https://medium.com/farouk-ounanes-home-on-the-internet/mandelbrot-set-in-c-from-scratch-c7ad6a1bf2d9
Public Class Fractal_Mandelbrot_MT
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Mandelbrot iterations", 1, 50, 34)
        ocvb.desc = "Run a multi-threaded version of the Mandalbrot algorithm"
        dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim iterations = sliders.TrackBar1.Value
        dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
        Parallel.For(0, src.Height,
        Sub(y)
            For x = 0 To src.Width - 1
                Dim c = New Complex(x * 4 / src.Width - 2, y * 3 / src.Height - 1.5)
                Dim z = New Complex(0, 0)
                Dim iter = 0
                While Complex.Abs(z) < 2 And iter < iterations
                    z = z * z + c
                    iter += 1
                End While
                dst1.Set(Of Byte)(y, x, If(iter < iterations, 255 * iter / (iterations - 1), 0))
            Next
        End Sub)
    End Sub
End Class






' https://medium.com/farouk-ounanes-home-on-the-internet/mandelbrot-set-in-c-from-scratch-c7ad6a1bf2d9
Public Class Fractal_MandelbrotZoom
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "Mandelbrot iterations", 1, 50, 34)
        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Reset to original Mandelbrot"
        ocvb.desc = "Run the classic Mandalbrot algorithm and allow zooming in"
        dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim iterations = sliders.TrackBar1.Value
        Static saveIterations As Integer

        Static startX As Double = -2.0, endX As Double = 2.0, startY As Double = -1.0, endY As Double = 1.0
        If check.Box(0).Checked Then
            startX = -2.0
            endX = 2.0
            startY = -1.0
            endY = 1.0
            saveIterations = 0
        End If
        If ocvb.drawRect.Width <> 0 Then
            Dim Height = ocvb.drawRect.Width * src.Height / src.Width ' maintain aspect ratio across zooms...
            Dim newStartX = startX + (endX - startX) * ocvb.drawRect.X / src.Width
            endX = startX + (endX - startX) * (ocvb.drawRect.X + ocvb.drawRect.Width) / src.Width
            startX = newStartX

            Dim newStartY = startY + (endY - startY) * ocvb.drawRect.Y / src.Height
            endY = startY + (endY - startY) * (ocvb.drawRect.Y + Height) / src.Height
            startY = newStartY

            saveIterations = 0
            ocvb.drawRectClear = True
        End If
        Dim incrX = CDbl(endX - startX) / src.Width, incrY = CDbl(endY - startY) / src.Height
        If saveIterations <> iterations Then
            saveIterations = iterations
            dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
            For y = 0 To src.Height - 1
                For x = 0 To src.Width - 1
                    Dim c = New Complex(startX + incrX * x, startY + incrY * y)
                    Dim z = New Complex(0, 0)
                    Dim iter = 0
                    While Complex.Abs(z) < 2 And iter < iterations
                        z = z * z + c
                        iter += 1
                    End While
                    dst1.Set(Of Byte)(y, x, If(iter < iterations, 255 * iter / (iterations - 1), 0))
                Next
            Next
            check.Box(0).Checked = False
        End If
        label1 = If(endX - startX >= 3.999, "Mandelbrot Zoom - draw anywhere", "Mandelbrot Zoom = " + Format(4 / (endX - startX), "###,###") + "X zoom")
    End Sub
End Class






Public Class Fractal_MandelbrotZoomColor
    Inherits ocvbClass
    Dim mandel As Fractal_MandelbrotZoom
    Public palette As Palette_ColorMap
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        mandel = New Fractal_MandelbrotZoom(ocvb)
        palette = New Palette_ColorMap(ocvb)
        palette.radio.check(5).Checked = True
        ocvb.desc = "Classic Mandelbrot in color"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        mandel.Run(ocvb)
        palette.src = mandel.dst1
        palette.Run(ocvb)
        dst1 = palette.dst1
        label1 = mandel.label1
    End Sub
End Class








' http://www.malinc.se/m/JuliaSets.php
' https://www.geeksforgeeks.org/julia-fractal-set-in-c-c-using-graphics/
Public Class Fractal_Julia
    Inherits ocvbClass
    Dim mandel As Fractal_MandelbrotZoomColor
    Dim rt As Double = 0.282
    Dim mt As Double = -0.58
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        mandel = New Fractal_MandelbrotZoomColor(ocvb)
        label2 = "Draw anywhere to select different Julia Sets"
        ocvb.desc = "Build Julia set from any point in the Mandelbrot fractal"
    End Sub
    Private Function julia_point(x As Single, y As Single, r As Integer, depth As Integer, max As Integer, c As Complex, z As Complex)
        If Complex.Abs(z) > r Then
            Dim mt = (255 * Math.Pow(max - depth, 2) Mod (max * max)) Mod 255
            dst1.Set(Of Byte)(y, x, 255 - mt)
            depth = 0
        End If
        If Math.Sqrt(Math.Pow(x - src.Width / 2, 2) + Math.Pow(y - src.Height / 2, 2)) > src.Height / 2 Then dst1.Set(Of Byte)(y, x, 0)
        If depth < max / 4 Then Return 0
        Return julia_point(x, y, r, depth - 1, max, c, Complex.Pow(z, 2) + c)
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        Static savedX As Integer = -1
        Static savedY As Integer = -1
        Dim restartComputation As Boolean
        If ocvb.drawRect.X <> savedX Or ocvb.drawRect.Y <> savedY Then
            restartComputation = True
            savedX = ocvb.drawRect.X
            savedY = ocvb.drawRect.Y
        End If
        If ocvb.drawRect.Width <> 0 Then
            rt = (ocvb.drawRect.X - src.Width / 2) / (src.Width / 4)
            mt = (ocvb.drawRect.Y - src.Height / 2) / (src.Height / 4)
            ocvb.drawRect = New cv.Rect
            ocvb.drawRectClear = True
        End If

        If restartComputation Then
            mandel.Run(ocvb)
            dst2 = mandel.dst1.Clone

            Dim detail = 1
            Dim depth = 100
            Dim r = 2
            Dim c = New Complex(rt, mt)
            dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
            For x = src.Width / 2 - src.Height / 2 To src.Width / 2 + src.Height / 2 - 1 Step detail
                For y = 0 To src.Height - 1 Step detail
                    Dim z = New Complex(2 * r * (x - src.Width / 2) / src.Height, 2 * r * (y - src.Height / 2) / src.Height)
                    julia_point(x, y, r, depth, depth, c, z)
                Next
            Next
            mandel.palette.src = dst1
            mandel.palette.Run(ocvb)
            dst1 = mandel.palette.dst1
        End If
    End Sub
End Class