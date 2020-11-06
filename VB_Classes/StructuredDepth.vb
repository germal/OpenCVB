Imports cv = OpenCvSharp
Public Class StructuredDepth_Basics
    Inherits VBparent
    Dim gLine As PointCloud_GVectorLine
    Dim inrange As Depth_InRange
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        inrange = New Depth_InRange(ocvb)
        gLine = New PointCloud_GVectorLine(ocvb)
        Dim inrangeSlider = findSlider("InRange Max Depth (mm)")
        inrangeSlider.Value = 8000

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Structured Depth slice thickness in pixels", 1, 100, 1)
        sliders.setupTrackBar(1, "Structured Depth Y-coordinate in pixels", 0, src.Height, 0)
        ocvb.desc = "Use slices of depth to discover attributes much like what structured light does"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        gLine.Run(ocvb)
        dst2 = gLine.dst1

        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Static ySlider = findSlider("Structured Depth Y-coordinate in pixels")

        Dim cushion = cushionSlider.value

        Dim cam = ocvb.sideCameraPoint
        Dim yCoordinate = ySlider.value - cushion
        If yCoordinate < 0 Then yCoordinate = 0

        Dim planePoint1 = New cv.Point(0, CInt(yCoordinate))
        Dim planePoint2 = New cv.Point(dst2.Width, planePoint1.Y)
        dst2.Line(planePoint1, planePoint2, cv.Scalar.Yellow, cushion)

        Dim split = gLine.sideIMU.sideView.gCloudIMU.imuPointCloud.Split()
        Dim maskPlane As New cv.Mat(split(1).Size, cv.MatType.CV_8U, 0)
        Dim planeHeight = cushion / ocvb.pixelsPerMeterV
        Static counts(src.Height) As Integer
        Dim planeY = (yCoordinate - cam.Y) / ocvb.pixelsPerMeterV
        inrange.minVal = planeY - planeHeight
        inrange.maxVal = planeY + planeHeight
        inrange.src = split(1).Clone
        inrange.Run(ocvb)
        maskPlane = inrange.depth32f.ConvertScaleAbs(255)
        counts(yCoordinate) = maskPlane.CountNonZero()

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane.Resize(src.Size))
    End Sub
End Class








Public Class StructuredDepth_RowSums
    Inherits VBparent
    Public side2D As Histogram_2D_Side
    Dim inrange As Depth_InRange
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        side2D = New Histogram_2D_Side(ocvb)
        inrange = New Depth_InRange(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Structured Depth slice thickness in pixels", 1, 100, 15)
        ' This camera is less accurate and needs wider height to describe the ceiling.
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D455 Then sliders.trackbar(0).Value = 25
        ' This camera is less accurate 
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.D435i Then sliders.trackbar(0).Value = 50
        ' This camera is less accurate 
        If ocvb.parms.cameraName = VB_Classes.ActiveTask.algParms.camNames.MyntD1000 Then sliders.trackbar(0).Value = 50

        label2 = "Yellow bar is ceiling.  Yellow line is camera level."
        ocvb.desc = "Using the point cloud histogram, calculate a sum for each row."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        side2D.Run(ocvb)
        dst2 = side2D.dst1

        Dim pixelsPerMeterV = Math.Abs(side2D.meterMaxY - side2D.meterMinY) / dst2.Height

        Dim yCoordinate As Integer
        Dim lastSum = dst2.Row(yCoordinate).Sum()
        For yCoordinate = 1 To dst2.Height - 1
            Dim nextSum = dst2.Row(yCoordinate).Sum()
            If nextSum.Item(0) - lastSum.Item(0) > 5000 Then Exit For
        Next

        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Dim cushion = cushionSlider.value
        dst2.Line(New cv.Point(0, yCoordinate), New cv.Point(dst2.Width, yCoordinate), cv.Scalar.Yellow, cushion)

        Dim planeHeight = cushion * pixelsPerMeterV

        Dim planeY = side2D.meterMinY * (side2D.cameraLevel - yCoordinate) / side2D.cameraLevel
        If yCoordinate > side2D.cameraLevel Then planeY = side2D.meterMaxY * (yCoordinate - side2D.cameraLevel) / side2D.cameraLevel

        inrange.minVal = planeY - planeHeight
        inrange.maxVal = planeY + planeHeight
        inrange.src = side2D.split(1)
        inrange.Run(ocvb)
        Dim maskPlane = inrange.depth32f.ConvertScaleAbs(255)

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane.Resize(src.Size))
    End Sub
End Class