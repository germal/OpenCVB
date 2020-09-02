
Imports cv = OpenCvSharp
'https://gist.github.com/kendricktan/93f0da88d0b25087d751ed2244cf770c
'https://medium.com/@anuj_shah/through-the-eyes-of-gabor-filter-17d1fdb3ac97
Public Class Gabor_Basics
    Inherits ocvbClass
    Public gKernel As cv.Mat
    Public ksize As Double
    Public Sigma As Double
    Public theta As Double
    Public lambda As Double
    Public gamma As Double
    Public phaseOffset As Double
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        sliders.Setup(ocvb, caller, 6)
        sliders.setupTrackBar(0, "Gabor Kernel Size", 0, 50, 15)
        sliders.setupTrackBar(1, "Gabor Sigma", 0, 100, 4)
        sliders.setupTrackBar(2, "Gabor Theta (degrees)", 0, 180, 90)
        sliders.setupTrackBar(3, "Gabor lambda", 0, 100, 10)
        sliders.setupTrackBar(4, "Gabor gamma X10", 0, 10, 5)
        sliders.setupTrackBar(5, "Gabor Phase offset X100", 0, 100, 0)

        desc = "Explore Gabor kernel - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            ksize = sliders.trackbar(0).Value * 2 + 1
            Sigma = sliders.trackbar(1).Value
            lambda = sliders.trackbar(3).Value
            gamma = sliders.trackbar(4).Value / 10
            phaseOffset = sliders.trackbar(5).Value / 1000
            theta = Math.PI * sliders.trackbar(2).Value / 180
        End If
        gKernel = cv.Cv2.GetGaborKernel(New cv.Size(ksize, ksize), Sigma, theta, lambda, gamma, phaseOffset, cv.MatType.CV_32F)
        Dim multiplier = gKernel.Sum()
        gKernel /= 1.5 * multiplier.Item(0)
        dst1 = src.Filter2D(cv.MatType.CV_8UC3, gKernel)
    End Sub
End Class





Public Class Gabor_Basics_MT
    Inherits ocvbClass
    Dim grid As Thread_Grid
    Dim gabor(31) As Gabor_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        label2 = "The 32 kernels used"
        grid = New Thread_Grid(ocvb)
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = ocvb.color.Width / 8 ' we want 4 rows of 8 or 32 regions for this example.
        gridHeightSlider.Value = ocvb.color.Height / 4

        grid.Run(ocvb) ' we only run this one time!  It needs to be 32 Gabor filters only.
        grid.sliders.Visible = False

        For i = 0 To gabor.Length - 1
            gabor(i) = New Gabor_Basics(ocvb)
            gabor(i).sliders.trackbar(2).Value = i * 180 / gabor.Length
        Next

        gabor(0).sliders.Visible = True
        desc = "Apply multiple Gabor filters sweeping through different values of theta - Painterly Effect."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        For i = 0 To gabor.Count - 1
            gabor(i).ksize = gabor(0).sliders.trackbar(0).Value * 2 + 1
            gabor(i).Sigma = gabor(0).sliders.trackbar(1).Value
            gabor(i).lambda = gabor(0).sliders.trackbar(3).Value
            gabor(i).gamma = gabor(0).sliders.trackbar(4).Value / 10
            gabor(i).phaseOffset = gabor(0).sliders.trackbar(5).Value / 1000
            gabor(i).theta = Math.PI * i / gabor.Length
        Next

        Dim accum = src.Clone()
        Dim dst32f = New cv.Mat(ocvb.color.Height, ocvb.color.Width, cv.MatType.CV_32F, 0)
        Parallel.For(0, grid.roiList.Count,
        Sub(i)
            Dim roi = grid.roiList(i)
            gabor(i).src = src
            gabor(i).Run(ocvb)
            SyncLock accum
                cv.Cv2.Max(accum, gabor(i).dst1, accum)
                dst32f(roi) = gabor(i).gKernel.Normalize(0, 255, cv.NormTypes.MinMax).Resize(New cv.Size(roi.Width, roi.Height))
            End SyncLock
        End Sub)
        dst1 = accum
        dst2 = dst32f ' ocvbclass will convert this to 8uc3
    End Sub
End Class
