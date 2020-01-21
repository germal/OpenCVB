Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports CS_Classes

' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Sift_Basics_CS : Implements IDisposable
    Dim radio As New OptionsRadioButtons
    Dim sliders As New OptionsSliders
    Dim CS_SiftBasics As New CS_SiftBasics
    Public Sub New(ocvb As AlgorithmData)
        radio.Setup(ocvb, 2)
        radio.check(0).Text = "Use BF Matcher"
        radio.check(1).Text = "Use Flann Matcher"
        radio.check(0).Checked = True
        radio.show()

        sliders.setupTrackBar1(ocvb, "Points to Match", 1, 1000, 200)
        sliders.show()

        ocvb.desc = "Compare 2 images to get a homography.  We will use left and right infrared images."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim dst As New cv.Mat(ocvb.redLeft.Rows, ocvb.redLeft.Cols * 2, cv.MatType.CV_8UC3)

        CS_SiftBasics.Run(ocvb.redLeft, ocvb.redRight, dst, radio.check(0).Checked, sliders.TrackBar1.Value)

        dst(New cv.Rect(0, 0, ocvb.result1.Width, ocvb.result1.Height)).CopyTo(ocvb.result1)
        dst(New cv.Rect(ocvb.result1.Width, 0, ocvb.result1.Width, ocvb.result1.Height)).CopyTo(ocvb.result2)

        ocvb.label1 = If(radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
    End Sub
End Class




Public Class Sift_Basics_CS_MT : Implements IDisposable
    Dim grid As Thread_Grid
    Dim radio As New OptionsRadioButtons
    Dim sliders As New OptionsSliders
    Dim CS_SiftBasics As New CS_SiftBasics
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = ocvb.color.Width - 1 ' we are just taking horizontal slices of the image.
        grid.sliders.TrackBar2.Value = ocvb.color.Height / 2

        radio.Setup(ocvb, 2)
        radio.check(0).Text = "Use BF Matcher"
        radio.check(1).Text = "Use Flann Matcher"
        radio.check(0).Checked = True
        radio.show()

        sliders.setupTrackBar1(ocvb, "Points to Match", 1, 1000, 100)
        sliders.show()

        ocvb.desc = "Compare 2 images to get a homography.  We will use left and right infrared images."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)

        Dim dst As New cv.Mat(ocvb.redLeft.Rows, ocvb.redLeft.Cols * 2, cv.MatType.CV_8UC3)

        Dim numFeatures = sliders.TrackBar1.Value
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim left = ocvb.redLeft(roi).Clone()  ' sift wants the inputs to be continuous and roi-modified Mats are not continuous.
            Dim right = ocvb.redRight(roi).Clone()
            Dim dstROI = New cv.Rect(roi.X, roi.Y, roi.Width * 2, roi.Height)
            Dim dstTmp = dst(dstROI).Clone()
            CS_SiftBasics.Run(left, right, dstTmp, radio.check(0).Checked, numFeatures)
            dstTmp.CopyTo(dst(dstROI))
        End Sub)

        dst(New cv.Rect(0, 0, ocvb.result1.Width, ocvb.result1.Height)).CopyTo(ocvb.result1)
        dst(New cv.Rect(ocvb.result1.Width, 0, ocvb.result1.Width, ocvb.result1.Height)).CopyTo(ocvb.result2)

        ocvb.label1 = If(radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
        sliders.Dispose()
        radio.Dispose()
    End Sub
End Class
