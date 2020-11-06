Imports cv = OpenCvSharp
Public Class StructuredDepth_Basics
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
        histThresholdSlider = findSlider("Histogram threshold")
        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        histThresholdSlider = findSlider("Histogram threshold")
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
        ocvb.desc = "Using the point cloud histogram, calculate a sum for each row."
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

        Dim cushion = cushionSlider.value
        dst2.Line(New cv.Point(0, yCoordinate), New cv.Point(dst2.Width, yCoordinate), cv.Scalar.Yellow, cushion)

        Dim pixelsPerMeterV = Math.Abs(side2D.meterMaxY - side2D.meterMinY) / dst2.Height
        Dim planeHeight = cushion * pixelsPerMeterV

        Dim planeY = side2D.meterMinY * (side2D.cameraLevel - yCoordinate) / side2D.cameraLevel
        If yCoordinate > side2D.cameraLevel Then planeY = side2D.meterMaxY * (yCoordinate - side2D.cameraLevel) / (dst2.Height - side2D.cameraLevel)

        inrange.minVal = planeY - planeHeight
        inrange.maxVal = planeY + planeHeight
        inrange.src = side2D.split(1)
        inrange.Run(ocvb)
        Dim maskPlane = inrange.depth32f.ConvertScaleAbs(255)

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane.Resize(src.Size))
    End Sub
End Class








Public Class StructuredDepth_Floor
    Inherits VBparent
    Dim structD As StructuredDepth_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_Basics(ocvb)
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
    Dim structD As StructuredDepth_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_Basics(ocvb)
        ocvb.desc = "A complementary algorithm to StructuredDepth_Floor..."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        structD.Run(ocvb)
        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class