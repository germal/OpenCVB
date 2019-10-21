Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports CS_Classes

' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Surf_Basics_CS : Implements IDisposable
    Dim radio As New OptionsRadioButtons
    Dim sliders As New OptionsSliders
    Dim CS_SurfBasics As New CS_SurfBasics
    Public Sub New(ocvb As AlgorithmData)
        radio.Setup(ocvb, 2)
        radio.check(0).Text = "Use BF Matcher"
        radio.check(1).Text = "Use Flann Matcher"
        radio.check(0).Checked = True
        If ocvb.parms.ShowOptions Then radio.show()

        sliders.setupTrackBar1(ocvb, "Hessian threshold", 1, 5000, 2000)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Compare 2 images to get a homography.  We will use left and right infrared images."
        ocvb.label1 = "BF Matcher output"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim dst As New cv.Mat(ocvb.redLeft.Rows, ocvb.redLeft.Cols * 2, cv.MatType.CV_8UC3)

        CS_SurfBasics.Run(ocvb.redLeft, ocvb.redRight, dst, sliders.TrackBar1.Value, radio.check(0).Checked)

        dst(New cv.Rect(0, 0, ocvb.result1.Width, ocvb.result1.Height)).CopyTo(ocvb.result1)
        dst(New cv.Rect(ocvb.result1.Width, 0, ocvb.result1.Width, ocvb.result1.Height)).CopyTo(ocvb.result2)

        ocvb.label1 = If(radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
    End Sub
End Class

