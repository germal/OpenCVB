Imports cv = OpenCvSharp
'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_Basics
    Inherits VBparent
    Public zone() As cv.Rect = Nothing
    Public region()() As cv.Point = Nothing
    Dim saveParms() As integer
    Dim mser As cv.MSER
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller, 9)

        sliders.setupTrackBar(0, "MSER Delta", 1, 100, 9)
        sliders.setupTrackBar(1, "MSER Min Area", 1, 10000, 60)
        sliders.setupTrackBar(2, "MSER Max Area", 1000, 100000, 100000)
        sliders.setupTrackBar(3, "MSER Max Variation", 1, 100, 25)
        sliders.setupTrackBar(4, "Min Diversity", 0, 100, 20)
        sliders.setupTrackBar(5, "MSER Max Evolution", 1, 1000, 200)
        sliders.setupTrackBar(6, "MSER Area Threshold", 1, 101, 101)
        sliders.setupTrackBar(7, "MSER Min Margin", 1, 100, 3)
        sliders.setupTrackBar(8, "MSER Edge Blursize", 1, 20, 5)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Pass2Only"
        check.Box(1).Text = "Use Grayscale, not color input (default)"
        check.Box(0).Checked = True
        check.Box(1).Checked = True

        ReDim saveParms(11 - 1) ' 4 sliders + 4 sliders + 1 slider + 2 checkboxes
        desc = "Extract the Maximally Stable Extremal Region (MSER) for an image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim delta = sliders.trackbar(0).Value
        Dim minArea = sliders.trackbar(1).Value
        Dim maxArea = sliders.trackbar(2).Value
        Dim maxVariation = sliders.trackbar(3).Value / 100

        Dim minDiversity = sliders.trackbar(4).Value / 100
        Dim maxEvolution = sliders.trackbar(5).Value
        Dim areaThreshold = sliders.trackbar(6).Value / 100
        Dim minMargin = sliders.trackbar(7).Value / 1000

        Dim edgeBlurSize = sliders.trackbar(8).Value
        If edgeBlurSize Mod 2 = 0 Then edgeBlurSize += 1 ' must be odd.

        Dim changedParms As Boolean
        For i = 0 To saveParms.Length - 1
            Dim nextVal = Choose(i + 1, sliders.trackbar(0).Value, sliders.trackbar(1).Value, sliders.trackbar(2).Value, sliders.trackbar(3).Value,
                                          sliders.trackbar(4).Value, sliders.trackbar(5).Value, sliders.trackbar(6).Value, sliders.trackbar(7).Value,
                                          sliders.trackbar(8).Value, check.Box(0).Checked)
            If nextVal <> saveParms(i) Then changedParms = True
            saveParms(i) = nextVal
        Next

        If changedParms Then
            mser = cv.MSER.Create(delta, minArea, maxArea, maxVariation, minDiversity, maxEvolution, areaThreshold, minMargin, edgeBlurSize)
            mser.Pass2Only = check.Box(0).Checked
        End If

        src = src.Blur(New cv.Size(edgeBlurSize, edgeBlurSize))
        If check.Box(1).Checked Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        mser.DetectRegions(src, region, zone)

        If standalone Then
            Dim pixels As integer
            dst1.SetTo(0)
            For i = 0 To region.Length - 1
                Dim nextRegion = region(i)
                pixels += nextRegion.Length
                For Each pt In nextRegion
                    dst1.Set(Of cv.Vec3b)(pt.Y, pt.X, ocvb.RGBDepth.Get(Of cv.Vec3b)(pt.Y, pt.X))
                Next
            Next
            label1 = CStr(region.Length) + " Regions " + Format(pixels / region.Length, "#0.0") + " pixels/region (avg)"
        End If
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_Synthetic
    Inherits VBparent
    Private Sub addNestedRectangles(img As cv.Mat, p0 As cv.Point, width() As integer, color() As integer, n As integer)
        For i = 0 To n - 1
            img.Rectangle(New cv.Rect(p0.X, p0.Y, width(i), width(i)), color(i), 1)
            p0 += New cv.Point((width(i) - width(i + 1)) / 2, (width(i) - width(i + 1)) / 2)
            img.FloodFill(p0, color(i))
        Next
    End Sub
    Private Sub addNestedCircles(img As cv.Mat, p0 As cv.Point, width() As integer, color() As integer, n As integer)
        For i = 0 To n - 1
            img.Circle(p0, width(i) / 2, color(i), 1)
            img.FloodFill(p0, color(i))
        Next
    End Sub
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        desc = "Build a synthetic image for MSER (Maximal Stable Extremal Regions) testing"
    End Sub
    Public Sub Run(ocvb As VBocvb)
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

        img = img.Resize(New cv.Size(src.Rows, src.Rows))
        dst1(New cv.Rect(0, 0, src.Rows, src.Rows)) = img.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_TestSynthetic
    Inherits VBparent
    Dim mser As MSER_Basics
    Dim synth As MSER_Synthetic
    Private Function testSynthetic(ocvb As VBocvb, img As cv.Mat, pass2Only As Boolean, delta As integer) As String
        mser.check.Box(0).Checked = pass2Only
        mser.sliders.trackbar(0).Value = delta
        mser.src = img
        mser.Run(ocvb)

        Dim pixels As integer
        Dim regionCount As integer
        For i = 0 To mser.region.Length - 1
            regionCount += 1
            Dim nextRegion = mser.region(i)
            For Each pt In nextRegion
                img.Set(Of cv.Vec3b)(pt.Y, pt.X, rColors(i Mod rColors.Length))
                pixels += 1
            Next
        Next
        Return CStr(regionCount) + " Regions had " + CStr(pixels) + " pixels"
    End Function
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        mser = New MSER_Basics(ocvb)
        mser.sliders.trackbar(0).Value = 10
        mser.sliders.trackbar(1).Value = 100
        mser.sliders.trackbar(2).Value = 5000
        mser.sliders.trackbar(3).Value = 2
        mser.sliders.trackbar(4).Value = 0
        mser.check.Box(1).Checked = False ' the grayscale result is quite unimpressive.

        synth = New MSER_Synthetic(ocvb)
        label1 = "Input image to MSER"
        label1 = "Output image from MSER"
        desc = "Test MSER with the synthetic image."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        synth.Run(ocvb)
        dst1 = synth.dst1.Clone()
        dst2 = synth.dst1

        testSynthetic(ocvb, dst2, True, 100)
    End Sub
End Class




' https://github.com/shimat/opencvsharp/wiki/MSER
Public Class MSER_CPPStyle
    Inherits VBparent
    Dim gray As cv.Mat
    Dim image As cv.Mat
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        label1 = "Contour regions from MSER"
        label2 = "Box regions from MSER"
        desc = "Maximally Stable Extremal Regions example - still image"
        image = cv.Cv2.ImRead(ocvb.homeDir + "Data/MSERtestfile.jpg", cv.ImreadModes.Color)
        gray = image.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
    Public Sub Run(ocvb As VBocvb)
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
        dst1 = mat.Resize(dst1.Size())

        mat = image.Clone()
        For Each box In boxes
            Dim color = cv.Scalar.RandomColor
            mat.Rectangle(box, color, -1, cv.LineTypes.AntiAlias)
        Next
        dst2 = mat.Resize(dst2.Size())
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/python/mser.py
Public Class MSER_Contours
    Inherits VBparent
    Dim mser As MSER_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        mser = New MSER_Basics(ocvb)
        mser.sliders.trackbar(1).Value = 4000
        desc = "Use MSER but show the contours of each region."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        mser.src = src
        mser.Run(ocvb)

        Dim pixels As integer
        dst1 = src
        Dim hull() As cv.Point
        For i = 0 To mser.region.Length - 1
            Dim nextRegion = mser.region(i)
            pixels += nextRegion.Length
            hull = cv.Cv2.ConvexHull(nextRegion, True)
            Dim listOfPoints = New List(Of List(Of cv.Point))
            Dim points = New List(Of cv.Point)
            For j = 0 To hull.Count - 1
                points.Add(hull(j))
            Next
            listOfPoints.Add(points)
            dst1.DrawContours(listOfPoints, 0, cv.Scalar.Yellow, 1)
        Next

        label1 = CStr(mser.region.Length) + " Regions " + Format(pixels / mser.region.Length, "#0.0") + " pixels/region (avg)"
    End Sub
End Class
