
Imports cv = OpenCvSharp
'https://gist.github.com/kendricktan/93f0da88d0b25087d751ed2244cf770c
'https://medium.com/@anuj_shah/through-the-eyes-of-gabor-filter-17d1fdb3ac97
Public Class Gabor_Basics
    Inherits VB_Class
    Public gKernel As New cv.Mat
    Public src As New cv.Mat
    Public dst As New cv.Mat
    Public externalUse As Boolean
    Public ksize As Double
    Public Sigma As Double
    Public theta As Double
    Public lambda As Double
    Public gamma As Double
    Public phaseOffset As Double
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders1.setupTrackBar1(ocvb, "Gabor gamma X10", 0, 10, 5)
        sliders1.setupTrackBar2(ocvb, "Gabor Phase offset X100", 0, 100, 0)
        If ocvb.parms.ShowOptions Then sliders1.Show()

        sliders.setupTrackBar1(ocvb, "Gabor Kernel Size", 0, 50, 15)
        sliders.setupTrackBar2(ocvb, "Gabor Sigma", 0, 100, 5)
        sliders.setupTrackBar3(ocvb, "Gabor Theta (degrees)", 0, 180, 90)
        sliders.setupTrackBar4(ocvb, "Gabor lambda", 0, 100, 10)

        ocvb.desc = "Explore Gabor kernel - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            src = ocvb.color
            ksize = sliders.TrackBar1.Value * 2 + 1
            Sigma = sliders.TrackBar2.Value
            lambda = sliders.TrackBar4.Value
            gamma = sliders1.TrackBar1.Value / 10
            phaseOffset = sliders1.TrackBar2.Value / 1000
        End If
        theta = Math.PI * sliders.TrackBar3.Value / 180

        gKernel = cv.Cv2.GetGaborKernel(New cv.Size(ksize, ksize), Sigma, theta, lambda, gamma, phaseOffset, cv.MatType.CV_32F)
        Dim multiplier = gKernel.Sum()
        gKernel /= 1.5 * multiplier.Item(0)
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If externalUse Then
            dst = src.Filter2D(cv.MatType.CV_8UC3, gKernel)
        Else
            ocvb.result1 = src.Filter2D(cv.MatType.CV_8UC3, gKernel)
            ocvb.result2.SetTo(0)
            ocvb.result2 = gKernel.Resize(ocvb.color.Size(), 0, 0, cv.InterpolationFlags.Cubic)
        End If
    End Sub
    Public Sub VBdispose()
        sliders1.Dispose()
    End Sub
End Class





Public Class Gabor_Basics_MT
    Inherits VB_Class
    Dim grid As Thread_Grid
    Dim gabor(31) As Gabor_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.label2 = "The 32 kernels used"
        grid = New Thread_Grid(ocvb, "Gabor_Basics_MT")
        grid.sliders.TrackBar1.Value = ocvb.color.Width / 8 ' we want 4 rows of 8 or 32 regions for this example.
        grid.sliders.TrackBar2.Value = ocvb.color.Height / 4
        grid.Run(ocvb)

        sliders1.setupTrackBar1(ocvb, "Gabor gamma X10", 0, 10, 5)
        sliders1.setupTrackBar2(ocvb, "Gabor Phase offset X100", 0, 100, 0)
        If ocvb.parms.ShowOptions Then sliders1.Show()

        sliders.setupTrackBar1(ocvb, "Gabor Kernel Size", 0, 50, 15)
        sliders.setupTrackBar2(ocvb, "Gabor Sigma", 0, 100, 4)
        sliders.setupTrackBar3(ocvb, "Gabor Theta (degrees)", 0, 180, 90)
        sliders.setupTrackBar4(ocvb, "Gabor lambda", 0, 100, 10)

        ocvb.parms.ShowOptions = False ' no  options for the Gabor_Basics algorithm needed - just need them for the parent thread.
        For i = 0 To gabor.Length - 1
            gabor(i) = New Gabor_Basics(ocvb, "Gabor_Basics_MT")
            gabor(i).sliders.TrackBar3.Value = i * 180 / gabor.Length
            gabor(i).externalUse = True
        Next
        ocvb.desc = "Apply multiple Gabor filters sweeping through different values of theta - Painterly Effect."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result2 = New cv.Mat(ocvb.result2.Size(), cv.MatType.CV_32FC1, 0)

        ' theta is not set here but in the constructor above.
        Dim ksize = sliders.TrackBar1.Value * 2 + 1
        Dim Sigma = sliders.TrackBar2.Value
        Dim lambda = sliders.TrackBar4.Value
        Dim gamma = sliders1.TrackBar1.Value / 10
        Dim phaseOffset = sliders1.TrackBar2.Value / 1000

        Dim accum = ocvb.color.Clone()
        Parallel.For(0, gabor.Length,
        Sub(i)
            gabor(i).ksize = ksize
            gabor(i).Sigma = Sigma
            gabor(i).lambda = lambda
            gabor(i).gamma = gamma
            gabor(i).phaseOffset = phaseOffset
            gabor(i).src = ocvb.color
            gabor(i).Run(ocvb)
            Dim roi = grid.roiList(i)
            SyncLock accum
                cv.Cv2.Max(accum, gabor(i).dst, accum)
                ocvb.result2(roi) = gabor(i).gKernel.Resize(New cv.Size(roi.Width, roi.Height), 0, 0, cv.InterpolationFlags.Cubic)
            End SyncLock
        End Sub)
        ocvb.result1 = accum
    End Sub
    Public Sub VBdispose()
        sliders1.Dispose()
    End Sub
End Class