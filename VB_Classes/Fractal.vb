Imports System.Numerics
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
            ocvb.drawRect.Height = ocvb.drawRect.Width * src.Height / src.Width ' maintain aspect ratio across zooms...
            Console.WriteLine("before startx = " + CStr(startX) + " endx = " + CStr(endX))
            startX = startX + (endX - startX) * ocvb.drawRect.X / src.Width
            endX = startX + (endX - startX) * (ocvb.drawRect.X + ocvb.drawRect.Width) / src.Width
            startY = startY + (endY - startY) * ocvb.drawRect.Y / src.Height
            endY = startY + (endY - startY) * (ocvb.drawRect.Y + ocvb.drawRect.Height) / src.Height
            Console.WriteLine("after startx = " + CStr(startX) + " endx = " + CStr(endX))
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
        label1 = If(endX - startX >= 3.999, "Mandelbrot Zoom - draw anywhere", "Mandelbrot Zoom = " + Format(CInt(4 / (endX - startX)), "###,###") + "X zoom")
    End Sub
End Class






Public Class Fractal_MandelbrotZoomColor
    Inherits ocvbClass
    Dim mandel As Fractal_MandelbrotZoom
    Dim palette As Palette_ColorMap
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        mandel = New Fractal_MandelbrotZoom(ocvb)
        palette = New Palette_ColorMap(ocvb)
        ocvb.desc = "New class description"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        mandel.Run(ocvb)
        palette.src = mandel.dst1
        palette.Run(ocvb)
        dst1 = palette.dst1
        label1 = mandel.label1
    End Sub
End Class
