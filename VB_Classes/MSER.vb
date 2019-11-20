Imports cv = OpenCvSharp
'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_Basics : Implements IDisposable
    Public src As cv.Mat
    Public externalUse As Boolean
    Public sliders As New OptionsSliders
    Public sliders1 As New OptionsSliders
    Public sliders2 As New OptionsSliders
    Public zone() As cv.Rect = Nothing
    Public region()() As cv.Point = Nothing
    Public check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData)
        sliders2.setupTrackBar1(ocvb, "MSER Edge Blursize", 1, 20, 5)
        If ocvb.parms.ShowOptions Then sliders2.Show()

        sliders1.setupTrackBar1(ocvb, "Min Diversity", 0, 100, 20)
        sliders1.setupTrackBar2(ocvb, "MSER Max Evolution", 1, 1000, 200)
        sliders1.setupTrackBar3(ocvb, "MSER Area Threshold", 1, 101, 101)
        sliders1.setupTrackBar4(ocvb, "MSER Min Margin", 1, 100, 3)
        If ocvb.parms.ShowOptions Then sliders1.Show()

        sliders.setupTrackBar1(ocvb, "MSER Delta", 1, 100, 5)
        sliders.setupTrackBar2(ocvb, "MSER Min Area", 1, 10000, 60)
        sliders.setupTrackBar3(ocvb, "MSER Max Area", 1000, 100000, 100000)
        sliders.setupTrackBar4(ocvb, "MSER Max Variation", 1, 100, 25)
        If ocvb.parms.ShowOptions Then sliders.show()

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Pass2Only"
        check.Box(0).Checked = True
        If ocvb.parms.ShowOptions Then check.show()

        ocvb.desc = "Extract the Maximally Stable Extremal Region (MSER) for an image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim delta = sliders.TrackBar1.Value
        Dim minArea = sliders.TrackBar2.Value
        Dim maxArea = sliders.TrackBar3.Value
        Dim maxVariation = sliders.TrackBar4.Value / 100

        Dim minDiversity = sliders1.TrackBar1.Value / 100
        Dim maxEvolution = sliders1.TrackBar2.Value
        Dim areaThreshold = sliders1.TrackBar3.Value / 100
        Dim minMargin = sliders1.TrackBar4.Value / 1000

        Dim edgeBlurSize = sliders2.TrackBar1.Value
        If edgeBlurSize Mod 2 = 0 Then edgeBlurSize += 1 ' must be odd.

        Dim mser = cv.MSER.Create(delta, minArea, maxArea, maxVariation, minDiversity, maxEvolution, areaThreshold, minMargin, edgeBlurSize)

        Dim keyImg As List(Of cv.KeyPoint) = Nothing
        mser.Pass2Only = check.Box(0).Checked
        If externalUse = False Then src = ocvb.color.Clone()
        src = src.Blur(New cv.Size(edgeBlurSize, edgeBlurSize))
        mser.DetectRegions(src, region, zone)

        If externalUse = False Then
            Dim pixels As Int32
            Dim regionCount As Int32
            ocvb.result1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC3, 0)
            For i = 0 To region.Length - 1
                regionCount += 1
                Dim nextRegion = region(i)
                For Each pt In nextRegion
                    ocvb.result1.Set(Of cv.Vec3b)(pt.Y, pt.X, ocvb.depthRGB.At(Of cv.Vec3b)(pt.Y, pt.X))
                    pixels += 1
                Next
            Next
            ocvb.label1 = CStr(regionCount) + " Regions had " + CStr(pixels) + " pixels"
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        sliders1.Dispose()
        sliders2.Dispose()
        check.Dispose()
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_Synthetic : Implements IDisposable
    Private Sub addNestedRectangles(img As cv.Mat, p0 As cv.Point, width() As Int32, color() As Int32, n As Int32)
        For i = 0 To n - 1
            img.Rectangle(New cv.Rect(p0.X, p0.Y, width(i), width(i)), color(i), 1)
            p0 += New cv.Point((width(i) - width(i + 1)) / 2, (width(i) - width(i + 1)) / 2)
            img.FloodFill(p0, color(i))
        Next
    End Sub
    Private Sub addNestedCircles(img As cv.Mat, p0 As cv.Point, width() As Int32, color() As Int32, n As Int32)
        For i = 0 To n - 1
            img.Circle(p0, width(i) / 2, color(i), 1)
            img.FloodFill(p0, color(i))
        Next
    End Sub
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Build a synthetic image for MSER (Maximal Stable Extremal Regions) testing"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim img = New cv.Mat(800, 800, cv.MatType.CV_8U, 0)
        Dim width() = {390, 380, 300, 290, 280, 270, 260, 250, 210, 190, 150, 100, 80, 70}
        Dim color1() = {80, 180, 160, 140, 120, 100, 90, 110, 170, 150, 140, 100, 220}
        Dim color2() = {81, 181, 161, 141, 121, 101, 91, 111, 171, 151, 141, 101, 221}
        Dim color3() = {175, 75, 95, 115, 135, 155, 165, 145, 85, 105, 115, 155, 35}
        Dim color4() = {173, 73, 93, 113, 133, 153, 163, 143, 83, 103, 113, 153, 33}

        addNestedRectangles(img, New cv.Point(10, 10), width, color1, 13)
        addNestedCircles(img, New cv.Point(200, 600), width, color2, 13)

        addNestedRectangles(img, New cv.Point(410, 10), width, color3, 13)
        addNestedCircles(img, New cv.Point(600, 600), width, color4, 13)

        img = img.Resize(New cv.Size(ocvb.color.Height, ocvb.color.Height))
        ocvb.result1(New cv.Rect(0, 0, ocvb.color.Height, ocvb.color.Height)) = img.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_TestSynthetic : Implements IDisposable
    Dim mser As MSER_Basics
    Dim synth As MSER_Synthetic
    Private Function testSynthetic(ocvb As AlgorithmData, img As cv.Mat, pass2Only As Boolean, delta As Int32) As String
        mser.check.Box(0).Checked = pass2Only
        mser.sliders.TrackBar1.Value = delta
        mser.src = img.Clone()
        mser.Run(ocvb)

        Dim pixels As Int32
        Dim regionCount As Int32
        For i = 0 To mser.region.Length - 1
            regionCount += 1
            Dim nextRegion = mser.region(i)
            For Each pt In nextRegion
                img.Set(Of cv.Vec3b)(pt.Y, pt.X, ocvb.rColors(i Mod ocvb.rColors.Length))
                pixels += 1
            Next
        Next
        Return CStr(regionCount) + " Regions had " + CStr(pixels) + " pixels"
    End Function
    Public Sub New(ocvb As AlgorithmData)
        mser = New MSER_Basics(ocvb)
        mser.externalUse = True
        mser.sliders.TrackBar1.Value = 10
        mser.sliders.TrackBar2.Value = 100
        mser.sliders.TrackBar3.Value = 5000
        mser.sliders.TrackBar4.Value = 2
        mser.sliders1.TrackBar1.Value = 0

        synth = New MSER_Synthetic(ocvb)
        ocvb.desc = "Test MSER with the synthetic image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        synth.Run(ocvb)
        ocvb.result2 = ocvb.result1.Clone()

        testSynthetic(ocvb, ocvb.result1, False, 10)
        testSynthetic(ocvb, ocvb.result2, True, 100)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        mser.Dispose()
        synth.Dispose()
    End Sub
End Class




' https://github.com/shimat/opencvsharp/wiki/MSER
' Results are surprisingly different from those in the example above.  
' Code Is identical And so Is the input image - resize does not affect results (use an imshow to test it.)
Public Class MSER_CPPStyle : Implements IDisposable
    Dim gray As cv.Mat
    Dim image As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = "Contour regions from MSER"
        ocvb.label2 = "Box regions from MSER"
        ocvb.desc = "Maximally Stable Extremal Regions example - still image"
        image = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/01.jpg", cv.ImreadModes.Color)
        gray = image.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mser = cv.MSER.Create()
        Dim msers()() As cv.Point = Nothing
        Dim boxes() As cv.Rect = Nothing
        mser.DetectRegions(image, msers, boxes)
        Dim mat = image.Clone()
        For Each pts In msers
            Dim color = cv.Scalar.RandomColor
            For Each pt In pts
                mat.Circle(pt, 1, color)
            Next
        Next
        ocvb.result1 = mat.Resize(ocvb.result1.Size())

        mat = image.Clone()
        For Each box In boxes
            Dim color = cv.Scalar.RandomColor
            mat.Rectangle(box, color, -1, cv.LineTypes.AntiAlias)
        Next
        ocvb.result2 = mat.Resize(ocvb.result2.Size())
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class