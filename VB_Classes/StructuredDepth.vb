Imports cv = OpenCvSharp
Public Class StructuredDepth_BasicsSide
    Inherits VBparent
    Public side2D As Histogram_SideData
    Dim inrange As Depth_InRange
    Public floorRun As Boolean
    Public inputYCoordinate As Integer
    Dim histThresholdSlider As Windows.Forms.TrackBar
    Dim cushionSlider As Windows.Forms.TrackBar
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        side2D = New Histogram_SideData(ocvb)
        inrange = New Depth_InRange(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Structured Depth slice thickness in pixels", 1, 100, 15)
        sliders.setupTrackBar(1, "Y-coordinate for the slice", 0, src.Height - 1, src.Height / 2)

        histThresholdSlider = findSlider("Histogram threshold")
        cushionSlider = findSlider("Structured Depth slice thickness in pixels")

        ' Some cameras are less accurate and need a fatter slice or a histogram threshold to identify the ceiling or floor...
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D455 Then
            cushionSlider.Value = 25
            histThresholdSlider.Value = 5
        End If
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D435i Then
            cushionSlider.Value = 50
            histThresholdSlider.Value = 5
        End If
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.MyntD1000 Then cushionSlider.Value = 30
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2 Then
            cushionSlider.Value = 30
            histThresholdSlider.Value = 10 ' this camera is showing a lot of data below the ground plane.
        End If

        label2 = "Yellow bar is ceiling.  Yellow line is camera level."
        ocvb.desc = "Find and isolate planes (floor and ceiling) in a side view histogram."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        side2D.Run(ocvb)
        dst2 = side2D.dst1

        Dim yCoordinate = inputYCoordinate ' if zero, find the ycoordinate.
        If inputYCoordinate = 0 Then
            If floorRun Then
                Dim lastSum = dst2.Row(dst2.Height - 1).Sum()
                For yCoordinate = dst2.Height - 1 To 0 Step -1
                    Dim nextSum = dst2.Row(yCoordinate).Sum()
                    If nextSum.Item(0) - lastSum.Item(0) > 3000 Then Exit For
                Next
            Else
                Dim lastSum = dst2.Row(yCoordinate).Sum()
                For yCoordinate = 1 To dst2.Height - 1
                    Dim nextSum = dst2.Row(yCoordinate).Sum()
                    If nextSum.Item(0) - lastSum.Item(0) > 3000 Then Exit For
                Next
            End If
        End If

        Dim cushion = cushionSlider.Value
        dst2.Line(New cv.Point(0, yCoordinate), New cv.Point(dst2.Width, yCoordinate), cv.Scalar.Yellow, cushion)

        Dim planeY = side2D.meterMin * (side2D.cameraLevel - yCoordinate) / side2D.cameraLevel
        If yCoordinate > side2D.cameraLevel Then planeY = side2D.meterMax * (yCoordinate - side2D.cameraLevel) / (dst2.Height - side2D.cameraLevel)

        Dim pixelsPerMeterV = Math.Abs(side2D.meterMax - side2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * pixelsPerMeterV
        inrange.minVal = planeY - thicknessMeters
        inrange.maxVal = planeY + thicknessMeters
        inrange.src = side2D.split(1)
        inrange.Run(ocvb)
        Dim maskPlane = inrange.depth32f.ConvertScaleAbs(255)

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane.Resize(src.Size))
    End Sub
End Class







Public Class StructuredDepth_BasicsTop
    Inherits VBparent
    Public top2D As Histogram_TopData
    Dim inrange As Depth_InRange
    Dim histThresholdSlider As Windows.Forms.TrackBar
    Dim cushionSlider As Windows.Forms.TrackBar
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        top2D = New Histogram_TopData(ocvb)
        inrange = New Depth_InRange(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Structured Depth slice thickness in pixels", 1, 100, 15)
        sliders.setupTrackBar(1, "X-coordinate for the slice", 0, src.Width - 1, src.Width / 2)

        histThresholdSlider = findSlider("Histogram threshold")
        cushionSlider = findSlider("Structured Depth slice thickness in pixels")

        ocvb.desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static xSliceSlider = findSlider("X-coordinate for the slice")
        Dim xCoordinate = CInt(xSliceSlider.value)
        top2D.Run(ocvb)
        dst2 = top2D.dst1

        Dim cushion = cushionSlider.Value
        dst2.Line(New cv.Point(xCoordinate, 0), New cv.Point(xCoordinate, dst2.Height), cv.Scalar.Yellow, cushion)

        Dim pixelsPerMeterV = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * pixelsPerMeterV

        Dim planeX = top2D.meterMin * (top2D.cameraLevel - xCoordinate) / top2D.cameraLevel
        If xCoordinate > top2D.cameraLevel Then planeX = top2D.meterMax * (xCoordinate - top2D.cameraLevel) / (dst2.Width - top2D.cameraLevel)

        inrange.minVal = planeX - thicknessMeters
        inrange.maxVal = planeX + thicknessMeters
        inrange.src = top2D.split(0)
        inrange.Run(ocvb)
        Dim maskPlane = inrange.depth32f.ConvertScaleAbs(255)

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane.Resize(src.Size))
    End Sub
End Class








Public Class StructuredDepth_Floor
    Inherits VBparent
    Dim structD As StructuredDepth_BasicsSide
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_BasicsSide(ocvb)
        structD.floorRun = True
        Static histThresholdSlider = findSlider("Histogram threshold")
        histThresholdSlider.value = 10 ' some cameras can show data below ground level...
        Dim cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        cushionSlider.Value = 5 ' floor runs can use a thinner slice that ceilings...

        ' this camera is less precise and needs a fatter slice of the floor.  The IMU looks to be the culprit.
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D435i Then cushionSlider.Value = 20
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.MyntD1000 Then cushionSlider.Value = 10
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2 Then cushionSlider.Value = 10

        ocvb.desc = "Find the floor plane"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        structD.Run(ocvb)
        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class








Public Class StructuredDepth_Ceiling
    Inherits VBparent
    Dim structD As StructuredDepth_BasicsSide
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_BasicsSide(ocvb)
        ocvb.desc = "A complementary algorithm to StructuredDepth_Floor..."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        structD.Run(ocvb)
        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class






Public Class StructuredDepth_SliceH
    Inherits VBparent
    Dim structD As StructuredDepth_BasicsSide
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_BasicsSide(ocvb)
        ocvb.desc = "Take a slice through the side2d projection and show it in a top-down view."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static sliceSlider = findSlider("Y-coordinate for the slice")
        structD.inputYCoordinate = sliceSlider.value
        structD.Run(ocvb)
        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class






Public Class StructuredDepth_SliceV
    Inherits VBparent
    Dim structD As StructuredDepth_BasicsTop
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_BasicsTop(ocvb)
        ocvb.desc = "Take a slice through the top2d projection and show it in a side view."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        structD.Run(ocvb)
        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class