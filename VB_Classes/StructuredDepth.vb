Imports cv = OpenCvSharp
Public Class StructuredDepth_BasicsH
    Inherits VBparent
    Public side2D As Histogram_SideData
    Dim inrange As Depth_InRange
    Public floorRun As Boolean
    Public inputYCoordinate As Integer
    Dim histThresholdSlider As Windows.Forms.TrackBar
    Dim cushionSlider As Windows.Forms.TrackBar
    Public maskPlane As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        side2D = New Histogram_SideData(ocvb)
        inrange = New Depth_InRange(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Structured Depth slice thickness in pixels", 1, 100, 1)
        sliders.setupTrackBar(1, "Offset for the slice", 0, src.Width - 1, src.Height / 2)

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

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Reload the IMU PointCloud"
        check.Box(0).Checked = True

        label2 = "Yellow bar is ceiling.  Yellow line is camera level."
        ocvb.desc = "Find and isolate planes (floor and ceiling) in a side view histogram."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static reloadCheck = findCheckBox("Reload the IMU PointCloud")
        If reloadCheck.checked Then side2D.Run(ocvb)
        dst2 = side2D.dst2

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

        Dim metersPerPixel = Math.Abs(side2D.meterMax - side2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel
        inrange.minVal = planeY - thicknessMeters
        inrange.maxVal = planeY + thicknessMeters
        inrange.src = side2D.split(1).Clone
        inrange.Run(ocvb)
        maskPlane = inrange.depth32f.Resize(src.Size).ConvertScaleAbs(255).Threshold(1, 255, cv.ThresholdTypes.Binary)

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane)
        label2 = side2D.label2
    End Sub
End Class







Public Class StructuredDepth_BasicsV
    Inherits VBparent
    Public top2D As Histogram_TopData
    Dim inrange As Depth_InRange
    Dim sideStruct As StructuredDepth_BasicsH
    Dim cushionSlider As Windows.Forms.TrackBar
    Dim offsetSlider As Windows.Forms.TrackBar
    Public maskPlane As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        top2D = New Histogram_TopData(ocvb)
        inrange = New Depth_InRange(ocvb)
        sideStruct = New StructuredDepth_BasicsH(ocvb)

        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        offsetSlider = findSlider("Offset for the slice")
        offsetSlider.Value = src.Width / 2

        ocvb.desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim xCoordinate = offsetSlider.Value
        Static reloadCheck = findCheckBox("Reload the IMU PointCloud")
        If reloadCheck.checked Then top2D.Run(ocvb)
        dst2 = top2D.dst2

        Dim cushion = cushionSlider.Value
        dst2.Line(New cv.Point(xCoordinate, 0), New cv.Point(xCoordinate, dst2.Height), cv.Scalar.Yellow, cushion)

        Dim planeX = top2D.meterMin * (top2D.cameraLevel - xCoordinate) / top2D.cameraLevel
        If xCoordinate > top2D.cameraLevel Then planeX = top2D.meterMax * (xCoordinate - top2D.cameraLevel) / (dst2.Width - top2D.cameraLevel)

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        inrange.minVal = planeX - thicknessMeters
        inrange.maxVal = planeX + thicknessMeters
        inrange.src = top2D.split(0).Clone
        inrange.Run(ocvb)

        maskPlane = inrange.depth32f.Resize(src.Size).ConvertScaleAbs(255).Threshold(1, 255, cv.ThresholdTypes.Binary)

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane)
        label2 = top2D.label2
    End Sub
End Class








Public Class StructuredDepth_Floor
    Inherits VBparent
    Dim structD As StructuredDepth_BasicsH
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_BasicsH(ocvb)
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
    Public structD As StructuredDepth_BasicsH
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_BasicsH(ocvb)
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
    Public structD As StructuredDepth_BasicsH
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_BasicsH(ocvb)
        ocvb.desc = "Take a slice through the side2d projection and show it in a side view."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static sliceSlider = findSlider("Offset for the slice")
        structD.inputYCoordinate = sliceSlider.value
        structD.Run(ocvb)
        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class






Public Class StructuredDepth_SliceV
    Inherits VBparent
    Public structD As StructuredDepth_BasicsV
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        structD = New StructuredDepth_BasicsV(ocvb)
        ocvb.desc = "Take a slice through the top2d projection and show it in a top-down view."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        structD.Run(ocvb)
        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class








Public Class StructuredDepth_LineSweep
    Inherits VBparent
    Dim dlines As StructuredDepth_LineDetect
    Dim addW As AddWeighted_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        addW = New AddWeighted_Basics(ocvb)
        dlines = New StructuredDepth_LineDetect(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Slice step size in pixels", 1, 100, 50)

        ocvb.desc = "Compute a 3D slope for detected lines"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static reloadCheck = findCheckBox("Reload the IMU PointCloud")
        reloadCheck.checked = True
        dlines.Run(ocvb)
        reloadCheck.checked = False
        Static offsetSlider = findSlider("Offset for the slice")
        Static stepSlider = findSlider("Slice step size")
        Dim stepsize = stepSlider.value
        Dim offset = ocvb.frameCount Mod stepsize
        For i = offset To offsetSlider.maximum - 1 Step stepsize
            offsetSlider.Value = i
            dlines.Run(ocvb)
        Next

        dst2 = dlines.dst2
        label1 = dlines.label1

        addW.src1 = ocvb.color
        addW.src2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        addW.Run(ocvb)
        dst1 = addW.dst1
    End Sub
End Class








Public Class StructuredDepth_LineDetect3D
    Inherits VBparent
    Dim dlines As StructuredDepth_LineDetect
    Dim addW As AddWeighted_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        addW = New AddWeighted_Basics(ocvb)
        dlines = New StructuredDepth_LineDetect(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Slice step size in pixels", 1, 100, 50)

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Restart the search for lines"

        ocvb.desc = "Combine a few detected lines to form a plane - needs work"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        If dlines.p1.Count = 0 Or check.Box(0).Checked Then
            check.Box(0).Checked = False
            Static vertRadio = findRadio("Horizontal Slice")
            Static offsetSlider = findSlider("Offset for the slice")
            Static stepSlider = findSlider("Slice step size")
            Dim stepsize = stepSlider.value
            Dim offset = ocvb.frameCount Mod stepsize
            dlines.p1.Clear()
            dlines.p2.Clear()
            For i = offset To offsetSlider.maximum - 1 Step stepsize
                offsetSlider.Value = i
                dlines.Run(ocvb)
                If dlines.p1.Count > 10 Then Exit For
            Next

            Dim imuPC = dlines.sliceV.structD.top2D.gCloud.imuPointCloud
            If vertRadio.checked Then imuPC = dlines.sliceH.structD.side2D.gCloud.imuPointCloud

            Dim minDistance = Single.MaxValue
            Dim p1 As cv.Point2f
            Dim p2 As cv.Point2f
            For i = 0 To dlines.p1.Count - 1
                Dim pt1 = dlines.p1(i)
                Dim z1 = imuPC.Get(Of cv.Point3f)(pt1.X, pt1.Y)
                For j = i + 1 To dlines.p1.Count - 1
                    Dim pt2 = dlines.p1(j)
                    Dim z2 = imuPC.Get(Of cv.Point3f)(pt2.X, pt2.Y)
                    Dim dist = Math.Sqrt((z1.X - z2.X) * (z1.X - z2.X) + (z1.Y - z2.Y) * (z1.Y - z2.Y) + (z1.Z - z2.Z) * (z1.Z - z2.Z))
                    If dist < minDistance Then
                        minDistance = dist
                        p1 = pt1
                        p2 = pt2
                    End If
                Next
            Next
            dst2.SetTo(0)
            dlines.drawLinesAndSave(dst2, cv.Scalar.White)
            dst2.Line(p1, p2, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
        End If

        label1 = dlines.label1

        dst1 = ocvb.color
        dst1.SetTo(cv.Scalar.White, dst2)
    End Sub
End Class







Public Class StructuredDepth_LineDetect
    Inherits VBparent
    Public sliceH As StructuredDepth_SliceH
    Public sliceV As StructuredDepth_SliceV
    Public ldetect As LineDetector_Basics
    Public p1 As New List(Of cv.Point2f)
    Public p2 As New List(Of cv.Point2f)
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ldetect = New LineDetector_Basics(ocvb)
        ldetect.drawLines = True

        sliceH = New StructuredDepth_SliceH(ocvb)
        sliceV = New StructuredDepth_SliceV(ocvb)

        Dim cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        cushionSlider.Value = 1

        radio.Setup(ocvb, caller, 2)
        radio.check(0).Text = "Horizontal Slice"
        radio.check(1).Text = "Vertical Slice"
        radio.check(1).Checked = True

        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        ocvb.desc = "Use the line detector on the output of the structuredDepth_Slice algorithms"
    End Sub
    Public Sub drawLinesAndSave(dst As cv.Mat, color As cv.Scalar)
        Static thicknessSlider = findSlider("Line thickness")
        Dim thickness = thicknessSlider.Value
        For Each v In ldetect.sortlines
            Dim pt1 = New cv.Point(CInt(v.Value(0)), CInt(v.Value(1)))
            Dim pt2 = New cv.Point(CInt(v.Value(2)), CInt(v.Value(3)))
            p1.Add(pt1)
            p2.Add(pt2)
            dst.Line(pt1, pt2, color, thickness, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        Static sliceRadio = findRadio("Vertical Slice")
        Static saveRadio As Boolean = sliceRadio.Checked
        Static offsetSlider = findSlider("Offset for the slice")
        If saveRadio <> sliceRadio.Checked Then
            saveRadio = sliceRadio.Checked
            offsetSlider.Value = If(sliceRadio.Checked, src.Width / 2, src.Height / 2)
            dst2.SetTo(0)
        End If
        If radio.check(0).Checked Then
            sliceH.Run(ocvb)
            ldetect.src = sliceH.structD.maskPlane.Clone
        Else
            sliceV.Run(ocvb)
            ldetect.src = sliceV.structD.maskPlane.Clone
        End If

        ldetect.src.SetTo(0, dst2)
        ldetect.Run(ocvb)

        dst2.SetTo(0)
        drawLinesAndSave(dst2, cv.Scalar.White)

        dst1 = If(radio.check(0).Checked, sliceH.dst1, sliceV.dst1)
        label1 = "Detected line count = " + CStr(ldetect.sortlines.Count) + " total lines = " + CStr(p1.Count)
    End Sub
End Class






Public Class StructuredDepth_MultiSliceH
    Inherits VBparent
    Public side2D As Histogram_SideData
    Dim inrange As Depth_InRange
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        side2D = New Histogram_SideData(ocvb)
        inrange = New Depth_InRange(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Structured Depth slice thickness in pixels", 1, 100, 1)
        sliders.setupTrackBar(1, "Slice step size in pixels", 1, 100, 20)

        ocvb.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        side2D.Run(ocvb)
        dst2 = side2D.dst2

        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Dim cushion = cushionSlider.Value

        Dim metersPerPixel = Math.Abs(side2D.meterMax - side2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        Static stepSlider = findSlider("Slice step size")
        Dim stepsize = stepSlider.value

        Dim maskPlane = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For yCoordinate = 0 To src.Height - 1 Step stepsize
            Dim planeY = side2D.meterMin * (side2D.cameraLevel - yCoordinate) / side2D.cameraLevel
            If yCoordinate > side2D.cameraLevel Then planeY = side2D.meterMax * (yCoordinate - side2D.cameraLevel) / (dst2.Height - side2D.cameraLevel)
            inrange.minVal = planeY - thicknessMeters
            inrange.maxVal = planeY + thicknessMeters
            inrange.src = side2D.split(1).Clone
            inrange.Run(ocvb)
            maskPlane.SetTo(255, inrange.depth32f.Resize(dst1.Size).ConvertScaleAbs(255))
        Next

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane)
        label2 = side2D.label2
    End Sub
End Class






Public Class StructuredDepth_MultiSliceV
    Inherits VBparent
    Public top2D As Histogram_TopData
    Dim multiH As StructuredDepth_MultiSliceH
    Dim inrange As Depth_InRange
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        top2D = New Histogram_TopData(ocvb)
        inrange = New Depth_InRange(ocvb)
        multiH = New StructuredDepth_MultiSliceH(ocvb)

        ocvb.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        top2D.Run(ocvb)
        dst2 = top2D.dst2

        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Dim cushion = cushionSlider.Value

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        Static stepSlider = findSlider("Slice step size")
        Dim stepsize = stepSlider.value

        Dim maskPlane = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For xCoordinate = 0 To src.Width - 1 Step stepsize
            Dim planeX = top2D.meterMin * (top2D.cameraLevel - xCoordinate) / top2D.cameraLevel
            If xCoordinate > top2D.cameraLevel Then planeX = top2D.meterMax * (xCoordinate - top2D.cameraLevel) / (dst2.Width - top2D.cameraLevel)
            inrange.minVal = planeX - thicknessMeters
            inrange.maxVal = planeX + thicknessMeters
            inrange.src = top2D.split(0).Clone
            inrange.Run(ocvb)
            maskPlane.SetTo(255, inrange.depth32f.Resize(dst1.Size).ConvertScaleAbs(255))
        Next

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane)
        label2 = top2D.label2
    End Sub
End Class






Public Class StructuredDepth_MultiSlice
    Inherits VBparent
    Public top2D As Histogram_TopData
    Public side2D As Histogram_SideData
    Dim multiH As StructuredDepth_MultiSliceH
    Dim inrange As Depth_InRange
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        side2D = New Histogram_SideData(ocvb)
        top2D = New Histogram_TopData(ocvb)
        inrange = New Depth_InRange(ocvb)
        multiH = New StructuredDepth_MultiSliceH(ocvb)

        ocvb.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        top2D.Run(ocvb)
        side2D.Run(ocvb)
        ' dst2 = top2D.dst2

        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Dim cushion = cushionSlider.Value

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        Static stepSlider = findSlider("Slice step size")
        Dim stepsize = stepSlider.value

        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For xCoordinate = 0 To src.Width - 1 Step stepsize
            Dim planeX = top2D.meterMin * (top2D.cameraLevel - xCoordinate) / top2D.cameraLevel
            If xCoordinate > top2D.cameraLevel Then planeX = top2D.meterMax * (xCoordinate - top2D.cameraLevel) / (dst2.Width - top2D.cameraLevel)
            inrange.minVal = planeX - thicknessMeters
            inrange.maxVal = planeX + thicknessMeters
            inrange.src = top2D.split(0).Clone
            inrange.Run(ocvb)
            dst2.SetTo(255, inrange.depth32f.Resize(dst1.Size).ConvertScaleAbs(255))
        Next

        For yCoordinate = 0 To src.Height - 1 Step stepsize
            Dim planeY = side2D.meterMin * (side2D.cameraLevel - yCoordinate) / side2D.cameraLevel
            If yCoordinate > side2D.cameraLevel Then planeY = side2D.meterMax * (yCoordinate - side2D.cameraLevel) / (dst2.Height - side2D.cameraLevel)
            inrange.minVal = planeY - thicknessMeters
            inrange.maxVal = planeY + thicknessMeters
            inrange.src = side2D.split(1).Clone
            inrange.Run(ocvb)
            dst2.SetTo(255, inrange.depth32f.Resize(dst1.Size).ConvertScaleAbs(255))
        Next

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, dst2)
    End Sub
End Class







Public Class StructuredDepth_MultiSliceLines
    Inherits VBparent
    Dim multi As StructuredDepth_MultiSlice
    Public ldetect As LineDetector_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ldetect = New LineDetector_Basics(ocvb)
        Dim lenSlider = findSlider("Line length threshold in pixels")
        lenSlider.Value = lenSlider.Maximum ' don't need the yellow line...
        multi = New StructuredDepth_MultiSlice(ocvb)
        ocvb.desc = "Detect lines in the multiSlice output"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        multi.Run(ocvb)
        cv.Cv2.BitwiseNot(multi.dst2, dst2)
        ldetect.src = multi.dst2
        ldetect.Run(ocvb)
        dst1 = ldetect.dst1
    End Sub
End Class







Public Class StructuredDepth_MultiSlicePolygon
    Inherits VBparent
    Dim multi As StructuredDepth_MultiSlice
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        multi = New StructuredDepth_MultiSlice(ocvb)
        label1 = "Input to FindContours"
        label2 = "ApproxPolyDP 4-corner object from FindContours input"
        ocvb.desc = "Detect rectangles in the multiSlice output"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        multi.Run(ocvb)
        cv.Cv2.BitwiseNot(multi.dst2, dst1)

        Dim rawContours = cv.Cv2.FindContoursAsArray(dst1, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim contours(rawContours.Length - 1)() As cv.Point
        For j = 0 To rawContours.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(rawContours(j), 3, True)
        Next

        dst2.SetTo(0)
        For i = 0 To contours.Length - 1
            If contours(i).Length >= 4 And contours(i).Length <= 8 Then
                cv.Cv2.DrawContours(dst2, contours, i, New cv.Scalar(0, 255, 255), 2, cv.LineTypes.AntiAlias)
            End If
        Next
    End Sub
End Class