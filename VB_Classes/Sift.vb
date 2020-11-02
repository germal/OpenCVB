Imports cv = OpenCvSharp
Imports CS_Classes

' https://docs.opencv.org/3.0-beta/doc/py_tutorials/py_feature2d/py_surf_intro/py_surf_intro.html
Public Class Sift_Basics_CS
    Inherits VBparent
    Dim siftCS As New CS_SiftBasics
    Dim fisheye As FishEye_Rectified
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fisheye = New FishEye_Rectified(ocvb)

        radio.Setup(ocvb, caller, 2)
        radio.check(0).Text = "Use BF Matcher"
        radio.check(1).Text = "Use Flann Matcher"
        radio.check(0).Checked = True

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Points to Match", 1, 1000, 200)

        ocvb.desc = "Compare 2 images to get a homography.  We will use left and right images."
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim doubleSize As New cv.Mat(ocvb.leftView.Rows, ocvb.leftView.Cols * 2, cv.MatType.CV_8UC3)

        siftCS.Run(ocvb.leftView, ocvb.rightView, doubleSize, radio.check(0).Checked, sliders.trackbar(0).Value)

        doubleSize(New cv.Rect(0, 0, dst1.Width, dst1.Height)).CopyTo(dst1)
        doubleSize(New cv.Rect(dst1.Width, 0, dst1.Width, dst1.Height)).CopyTo(dst2)

        label1 = If(radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
End Class




Public Class Sift_Basics_CS_MT
    Inherits VBparent
    Dim grid As Thread_Grid
    Dim siftCS As New CS_SiftBasics
    Dim siftBasics As Sift_Basics_CS
    Dim fisheye As FishEye_Rectified
    Dim numPointSlider As System.Windows.Forms.TrackBar
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        fisheye = New FishEye_Rectified(ocvb)

        grid = New Thread_Grid(ocvb)
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Maximum = ocvb.color.Cols * 2
        gridWidthSlider.Value = ocvb.color.Cols * 2 ' we are just taking horizontal slices of the image.
        gridHeightSlider.Value = 10

        grid.Run(ocvb)

        siftBasics = New Sift_Basics_CS(ocvb)
        numPointSlider = findSlider("Points to Match")
        numPointSlider.Value = 1

        ocvb.desc = "Compare 2 images to get a homography.  We will use left and right images - needs more work"
    End Sub
    Public Sub Run(ocvb As VBocvb)
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim leftView As cv.Mat
        Dim rightView As cv.Mat

        leftView = ocvb.leftView
        rightView = ocvb.rightView
        grid.Run(ocvb)

        Dim output As New cv.Mat(src.Rows, src.Cols * 2, cv.MatType.CV_8UC3)

        Dim numFeatures = numPointSlider.Value
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim left = leftView(roi).Clone()  ' sift wants the inputs to be continuous and roi-modified Mats are not continuous.
            Dim right = rightView(roi).Clone()
            Dim dstROI = New cv.Rect(roi.X, roi.Y, roi.Width * 2, roi.Height)
            Dim dstTmp = output(dstROI).Clone()
            siftCS.Run(left, right, dstTmp, siftBasics.radio.check(0).Checked, numFeatures)
            dstTmp.CopyTo(output(dstROI))
        End Sub)

        dst1 = output(New cv.Rect(0, 0, src.Width, src.Height))
        dst2 = output(New cv.Rect(src.Width, 0, src.Width, src.Height))

        label1 = If(siftBasics.radio.check(0).Checked, "BF Matcher output", "Flann Matcher output")
    End Sub
End Class


