Imports cv = OpenCvSharp
'https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_Basics
    Inherits ocvbClass
    Public zone() As cv.Rect = Nothing
    Public region()() As cv.Point = Nothing
    Dim saveParms() As Int32
    Dim mser As cv.MSER
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders2.setupTrackBar1(ocvb, caller, "MSER Edge Blursize", 1, 20, 5)
        If ocvb.parms.ShowOptions Then sliders2.Show()

        sliders1.setupTrackBar1(ocvb, caller, "Min Diversity", 0, 100, 20)
        sliders1.setupTrackBar2(ocvb, caller, "MSER Max Evolution", 1, 1000, 200)
        sliders1.setupTrackBar3(ocvb, caller, "MSER Area Threshold", 1, 101, 101)
        sliders1.setupTrackBar4(ocvb, caller, "MSER Min Margin", 1, 100, 3)
        If ocvb.parms.ShowOptions Then sliders1.Show()

        sliders.setupTrackBar1(ocvb, caller, "MSER Delta", 1, 100, 5)
        sliders.setupTrackBar2(ocvb, caller, "MSER Min Area", 1, 10000, 60)
        sliders.setupTrackBar3(ocvb, caller, "MSER Max Area", 1000, 100000, 100000)
        sliders.setupTrackBar4(ocvb, caller, "MSER Max Variation", 1, 100, 25)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Pass2Only"
        check.Box(1).Text = "Use Grayscale, not color input (default)"
        check.Box(0).Checked = True
        check.Box(1).Checked = True

        ReDim saveParms(10) ' 4 sliders + 4 sliders + 1 slider + 2 checkboxes
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

        Dim changedParms As Boolean
        For i = 0 To saveParms.Length - 1
            Dim nextVal = Choose(i + 1, sliders.TrackBar1.Value, sliders.TrackBar2.Value, sliders.TrackBar3.Value, sliders.TrackBar4.Value,
                                          sliders1.TrackBar1.Value, sliders1.TrackBar2.Value, sliders1.TrackBar3.Value, sliders1.TrackBar4.Value,
                                          sliders2.TrackBar1.Value, check.Box(0).Checked)
            If nextVal <> saveParms(i) Then changedParms = True
            saveParms(i) = nextVal
        Next

        If changedParms Then
            mser = cv.MSER.Create(delta, minArea, maxArea, maxVariation, minDiversity, maxEvolution, areaThreshold, minMargin, edgeBlurSize)
            mser.Pass2Only = check.Box(0).Checked
        End If

        if standalone Then src = ocvb.color.Clone()
        src = src.Blur(New cv.Size(edgeBlurSize, edgeBlurSize))
        If check.Box(1).Checked Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        mser.DetectRegions(src, region, zone)

        if standalone Then
            Dim pixels As Int32
            dst1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC3, 0)
            For i = 0 To region.Length - 1
                Dim nextRegion = region(i)
                pixels += nextRegion.Length
                For Each pt In nextRegion
                    dst1.Set(Of cv.Vec3b)(pt.Y, pt.X, ocvb.RGBDepth.Get(Of cv.Vec3b)(pt.Y, pt.X))
                Next
            Next
            ocvb.label1 = CStr(region.Length) + " Regions " + Format(pixels / region.Length, "#0.0") + " pixels/region (avg)"
        End If
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_Synthetic
    Inherits ocvbClass
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
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
        dst1(New cv.Rect(0, 0, ocvb.color.Height, ocvb.color.Height)) = img.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class




' https://github.com/opencv/opencv/blob/master/samples/cpp/detect_mser.cpp
Public Class MSER_TestSynthetic
    Inherits ocvbClass
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
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        mser = New MSER_Basics(ocvb, caller)
        mser.sliders.TrackBar1.Value = 10
        mser.sliders.TrackBar2.Value = 100
        mser.sliders.TrackBar3.Value = 5000
        mser.sliders.TrackBar4.Value = 2
        mser.sliders1.TrackBar1.Value = 0
        mser.check.Box(1).Checked = False ' the grayscale result is quite unimpressive.

        synth = New MSER_Synthetic(ocvb, caller)
        ocvb.desc = "Test MSER with the synthetic image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        synth.Run(ocvb)
        dst2 = dst1.Clone()

        'testSynthetic(ocvb, dst1, False, 10)
        testSynthetic(ocvb, dst2, True, 100)
    End Sub
End Class




' https://github.com/shimat/opencvsharp/wiki/MSER
Public Class MSER_CPPStyle
    Inherits ocvbClass
    Dim gray As cv.Mat
    Dim image As cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
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
    Inherits ocvbClass
    Dim mser As MSER_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        mser = New MSER_Basics(ocvb, caller)
        mser.sliders.TrackBar2.Value = 4000
        ocvb.desc = "Use MSER but show the contours of each region."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        mser.src = ocvb.color.Clone()
        mser.Run(ocvb)

        Dim pixels As Int32
        dst1 = ocvb.color
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

        ocvb.label1 = CStr(mser.region.Length) + " Regions " + Format(pixels / mser.region.Length, "#0.0") + " pixels/region (avg)"
    End Sub
End Class
