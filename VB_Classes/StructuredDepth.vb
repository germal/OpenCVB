Imports cv = OpenCvSharp
Public Class StructuredDepth_BasicsH
    Inherits VBparent
    Public side2D As Histogram_SideData
    Dim inrange As Depth_InRange
    Public histThresholdSlider As Windows.Forms.TrackBar
    Public cushionSlider As Windows.Forms.TrackBar
    Public offsetSlider As Windows.Forms.TrackBar
    Public maskPlane As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        side2D = New Histogram_SideData(ocvb)
        inrange = New Depth_InRange(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Structured Depth slice thickness in pixels", 1, 100, 1)
        sliders.setupTrackBar(1, "Offset for the slice", 0, src.Width - 1, src.Height / 2)
        sliders.setupTrackBar(2, "Slice step size in pixels (multi-slice option only)", 1, 100, 20)

        histThresholdSlider = findSlider("Histogram threshold")
        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        offsetSlider = findSlider("Offset for the slice")

        label2 = "Yellow bar is ceiling.  Yellow line is camera level."
        ocvb.desc = "Find and isolate planes (floor and ceiling) in a side view histogram."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        side2D.Run(ocvb)
        dst2 = side2D.dst2

        Static offsetSlider = findSlider("Offset for the slice")
        Dim yCoordinate = CInt(offsetSlider.Value)

        Dim planeY = side2D.meterMin * (side2D.cameraLoc - yCoordinate) / side2D.cameraLoc
        If yCoordinate > side2D.cameraLoc Then planeY = side2D.meterMax * (yCoordinate - side2D.cameraLoc) / (dst2.Height - side2D.cameraLoc)

        Dim metersPerPixel = Math.Abs(side2D.meterMax - side2D.meterMin) / dst2.Height
        Dim cushion = cushionSlider.Value
        Dim thicknessMeters = cushion * metersPerPixel
        inrange.minVal = planeY - thicknessMeters
        inrange.maxVal = planeY + thicknessMeters
        inrange.src = side2D.split(1).Clone
        inrange.Run(ocvb)
        maskPlane = inrange.depth32f.Resize(src.Size).ConvertScaleAbs(255).Threshold(1, 255, cv.ThresholdTypes.Binary)

        label1 = "At offset " + CStr(yCoordinate) + " y = " + Format((inrange.maxVal + inrange.minVal) / 2, "#0.00") + " with " +
                 Format(Math.Abs(inrange.maxVal - inrange.minVal) * 100, "0.00") + " cm width"

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane)
        label2 = side2D.label2

        dst2 = dst2.ConvertScaleAbs(255).Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2.Circle(New cv.Point(0, side2D.cameraLoc), ocvb.dotSize, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Dim offset = If(offsetSlider.value < dst2.Height - cushion, CInt(offsetSlider.Value), dst2.Height - cushion - 1)
        dst2.Line(New cv.Point(0, offset), New cv.Point(dst2.Width, offset), cv.Scalar.Yellow, cushion)
    End Sub
End Class







Public Class StructuredDepth_BasicsV
    Inherits VBparent
    Public top2D As Histogram_TopData
    Public inrange As Depth_InRange
    Dim sideStruct As StructuredDepth_BasicsH
    Public cushionSlider As Windows.Forms.TrackBar
    Public offsetSlider As Windows.Forms.TrackBar
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
        top2D.Run(ocvb)
        dst2 = top2D.dst2

        Dim planeX = top2D.meterMin * (top2D.cameraLoc - xCoordinate) / top2D.cameraLoc
        If xCoordinate > top2D.cameraLoc Then planeX = top2D.meterMax * (xCoordinate - top2D.cameraLoc) / (dst2.Width - top2D.cameraLoc)

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim cushion = cushionSlider.Value
        Dim thicknessMeters = cushion * metersPerPixel

        inrange.minVal = planeX - thicknessMeters
        inrange.maxVal = planeX + thicknessMeters
        inrange.src = top2D.split(0).Clone
        inrange.Run(ocvb)
        maskPlane = inrange.depth32f.Resize(src.Size).ConvertScaleAbs(255).Threshold(1, 255, cv.ThresholdTypes.Binary)

        label1 = "At offset " + CStr(xCoordinate) + " x = " + Format((inrange.maxVal + inrange.minVal) / 2, "#0.00") + " with " +
                 Format(Math.Abs(inrange.maxVal - inrange.minVal) * 100, "0.00") + " cm width"

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskPlane)
        label2 = top2D.label2

        dst2 = dst2.Normalize(0, 255, cv.NormTypes.MinMax)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2.Circle(New cv.Point(top2D.cameraLoc, dst2.Height), ocvb.dotSize, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Dim offset = CInt(offsetSlider.Value)
        dst2.Line(New cv.Point(offset, 0), New cv.Point(offset, dst2.Height), cv.Scalar.Yellow, cushion)
    End Sub
End Class








Public Class StructuredDepth_Floor
    Inherits VBparent
    Public structD As StructuredDepth_BasicsH
    Dim kalman As Kalman_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.kInput(0)

        structD = New StructuredDepth_BasicsH(ocvb)
        structD.histThresholdSlider.Value = 10 ' some cameras can show data below ground level...
        structD.cushionSlider.Value = 10 ' floor runs can use a thinner slice that ceilings...

        ocvb.desc = "Find the floor plane"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        structD.Run(ocvb)

        Dim yCoordinate = dst2.Height
        Dim lastSum = dst2.Row(dst2.Height - 1).Sum()
        For yCoordinate = dst2.Height - 1 To 0 Step -1
            Dim nextSum = dst2.Row(yCoordinate).Sum()
            If nextSum.Item(0) - lastSum.Item(0) > 1000 Then Exit For
            lastSum = nextSum
        Next

        kalman.kInput(0) = (yCoordinate + kalman.kOutput(0)) / 2
        kalman.Run(ocvb)

        structD.offsetSlider.Value = If(kalman.kOutput(0) >= 0, kalman.kOutput(0), dst2.Height)

        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class








Public Class StructuredDepth_Ceiling
    Inherits VBparent
    Public structD As StructuredDepth_BasicsH
    Dim kalman As Kalman_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.kInput(0)

        structD = New StructuredDepth_BasicsH(ocvb)
        structD.cushionSlider.Value = 10
        ocvb.desc = "Find the ceiling plane"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me

        structD.Run(ocvb)

        Dim yCoordinate As Integer
        Dim lastSum = dst2.Row(yCoordinate).Sum()
        For yCoordinate = 1 To dst2.Height - 1
            Dim nextSum = dst2.Row(yCoordinate).Sum()
            If nextSum.Item(0) - lastSum.Item(0) > 1000 Then Exit For
            lastSum = nextSum
        Next

        kalman.kInput(0) = yCoordinate
        kalman.Run(ocvb)
        structD.offsetSlider.Value = If(kalman.kOutput(0) >= 0, kalman.kOutput(0), 0)

        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class






Public Class StructuredDepth_MultiSliceH
    Inherits VBparent
    Public side2D As Histogram_SideData
    Public structD As StructuredDepth_BasicsH
    Dim inrange As Depth_InRange
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        side2D = New Histogram_SideData(ocvb)
        inrange = New Depth_InRange(ocvb)
        structD = New StructuredDepth_BasicsH(ocvb)

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

        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        Dim stepsize = stepSlider.value

        Dim maskPlane = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For yCoordinate = 0 To src.Height - 1 Step stepsize
            Dim planeY = side2D.meterMin * (side2D.cameraLoc - yCoordinate) / side2D.cameraLoc
            If yCoordinate > side2D.cameraLoc Then planeY = side2D.meterMax * (yCoordinate - side2D.cameraLoc) / (dst2.Height - side2D.cameraLoc)
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
    Public structD As StructuredDepth_BasicsH
    Dim inrange As Depth_InRange
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        top2D = New Histogram_TopData(ocvb)
        inrange = New Depth_InRange(ocvb)
        structD = New StructuredDepth_BasicsH(ocvb)

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

        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        Dim stepsize = stepSlider.value

        Dim maskPlane = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For xCoordinate = 0 To src.Width - 1 Step stepsize
            Dim planeX = top2D.meterMin * (top2D.cameraLoc - xCoordinate) / top2D.cameraLoc
            If xCoordinate > top2D.cameraLoc Then planeX = top2D.meterMax * (xCoordinate - top2D.cameraLoc) / (dst2.Width - top2D.cameraLoc)
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
    Dim struct As StructuredDepth_BasicsV
    Public inrange As Depth_InRange
    Public maskPlane As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        side2D = New Histogram_SideData(ocvb)
        top2D = New Histogram_TopData(ocvb)
        inrange = New Depth_InRange(ocvb)
        struct = New StructuredDepth_BasicsV(ocvb)

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

        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        Dim stepsize = stepSlider.value

        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For xCoordinate = 0 To src.Width - 1 Step stepsize
            Dim planeX = top2D.meterMin * (top2D.cameraLoc - xCoordinate) / top2D.cameraLoc
            If xCoordinate > top2D.cameraLoc Then planeX = top2D.meterMax * (xCoordinate - top2D.cameraLoc) / (dst2.Width - top2D.cameraLoc)
            inrange.minVal = planeX - thicknessMeters
            inrange.maxVal = planeX + thicknessMeters
            inrange.src = top2D.split(0).Clone
            inrange.Run(ocvb)
            maskPlane = inrange.depth32f.Resize(src.Size).ConvertScaleAbs(255).Threshold(1, 255, cv.ThresholdTypes.Binary)
            dst2.SetTo(255, maskPlane)
        Next

        For yCoordinate = 0 To src.Height - 1 Step stepsize
            Dim planeY = side2D.meterMin * (side2D.cameraLoc - yCoordinate) / side2D.cameraLoc
            If yCoordinate > side2D.cameraLoc Then planeY = side2D.meterMax * (yCoordinate - side2D.cameraLoc) / (dst2.Height - side2D.cameraLoc)
            inrange.minVal = planeY - thicknessMeters
            inrange.maxVal = planeY + thicknessMeters
            inrange.src = side2D.split(1).Clone
            inrange.Run(ocvb)
            Dim tmp = inrange.depth32f.Resize(src.Size).ConvertScaleAbs(255).Threshold(1, 255, cv.ThresholdTypes.Binary)
            cv.Cv2.BitwiseOr(tmp, maskPlane, maskPlane)
            dst2.SetTo(255, maskPlane)
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

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Max number of sides in the identified polygons", 3, 100, 4)
        ocvb.desc = "Detect polygons in the multiSlice output"
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
        Dim sidesSlider = findSlider("Max number of sides in the identified polygons")
        Dim maxSides = sidesSlider.Value
        For i = 0 To contours.Length - 1
            If contours(i).Length = 2 Then Continue For
            If contours(i).Length <= maxSides Then
                cv.Cv2.DrawContours(dst2, contours, i, New cv.Scalar(0, 255, 255), 2, cv.LineTypes.AntiAlias)
            End If
        Next
    End Sub
End Class






Public Class StructuredDepth_SliceXPlot
    Inherits VBparent
    Dim multi As StructuredDepth_MultiSlice
    Dim structD As StructuredDepth_BasicsV
    Dim cushionSlider As Windows.Forms.TrackBar
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        multi = New StructuredDepth_MultiSlice(ocvb)
        structD = New StructuredDepth_BasicsV(ocvb)
        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        cushionSlider.Value = 25
        ocvb.desc = "Find any plane around a peak value in the top-down histogram"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        structD.Run(ocvb)
        dst2 = structD.dst2
        multi.Run(ocvb)

        Static offsetSlider = findSlider("Offset for the slice")
        Dim col = CInt(offsetSlider.value)

        Dim cushion = cushionSlider.Value
        Dim rect = New cv.Rect(col, 0, cushion, dst2.Height - 1)
        Dim minVal As Double, maxVal As Double
        Dim minLoc As cv.Point, maxLoc As cv.Point
        multi.top2D.histOutput(rect).MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

        dst2.Circle(New cv.Point(col, maxLoc.Y), 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Dim filterZ = (dst2.Height - maxLoc.Y) / dst2.Height * ocvb.maxZ

        Dim maskZplane As New cv.Mat(multi.top2D.split(0).Size, cv.MatType.CV_8U, 255)
        If filterZ > 0 Then
            multi.inrange.minVal = filterZ - 0.05 ' a 10 cm buffer surrounding the z value
            multi.inrange.maxVal = filterZ + 0.05
            multi.inrange.src = multi.top2D.split(2)
            multi.inrange.Run(ocvb)
            maskZplane = multi.inrange.depth32f.Resize(src.Size).ConvertScaleAbs(255).Threshold(1, 255, cv.ThresholdTypes.Binary)
        End If

        If filterZ > 0 Then cv.Cv2.BitwiseAnd(multi.maskPlane, maskZplane, maskZplane)

        dst1 = ocvb.color.Clone
        dst1.SetTo(cv.Scalar.White, maskZplane)

        Dim pixelsPerMeter = dst2.Height / ocvb.maxZ
        label2 = "Peak histogram count (" + Format(maxVal, "#0") + ") at " + Format(filterZ, "#0.00") + " meters +-" + Format(10 / pixelsPerMeter, "#0.00") + " m"
    End Sub
End Class







Public Class StructuredDepth_LinearizeFloor
    Inherits VBparent
    Dim floor As StructuredDepth_Floor
    Dim kalmanCheck As Windows.Forms.CheckBox
    Dim kalman As Kalman_Basics
    Dim kalmanVB As Kalman_VB_Basics
    Public imuPointCloud As cv.Mat
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        kalman = New Kalman_Basics(ocvb)
        kalmanVB = New Kalman_VB_Basics(ocvb)
        floor = New StructuredDepth_Floor(ocvb)
        kalmanCheck = findCheckBox("Turn Kalman filtering on")
        kalmanCheck.Checked = False ' initially it is turned off...

        check.Setup(ocvb, caller, 4)
        check.Box(0).Text = "Smooth in X-direction"
        check.Box(1).Text = "Smooth in Y-direction" ' only the Y-direction smoothing provides more accuracy.
        check.Box(2).Text = "Smooth in Z-direction"
        check.Box(3).Text = "Use VB Kalman"
        check.Box(1).Checked = True
        ocvb.desc = "Using the mask for the floor create a better representation of the floor plane"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount = 5 Then kalmanCheck.Checked = True
        floor.Run(ocvb)
        dst1 = floor.dst1
        dst2 = floor.dst2
        Dim mask = floor.structD.maskPlane
        If mask.CountNonZero() > 0 Then
            Dim nonFloorMask As New cv.Mat
            cv.Cv2.BitwiseNot(mask, nonFloorMask)
            Dim imuPC = floor.structD.side2D.gCloud.imuPointCloud.Clone
            imuPointCloud = imuPC.Clone
            imuPC.SetTo(0, nonFloorMask)

            Dim split = imuPC.Split()
            Dim minVal As Double, maxVal As Double
            Dim minLoc As cv.Point, maxLoc As cv.Point
            If check.Box(0).Checked Then
                split(0).MinMaxLoc(minVal, maxVal, minLoc, maxLoc, mask)

                Dim firstCol As Integer, lastCol As Integer
                For firstCol = 0 To mask.Width - 1
                    If mask.Col(firstCol).CountNonZero() > 0 Then Exit For
                Next
                For lastCol = mask.Width - 1 To 0 Step -1
                    If mask.Col(lastCol).CountNonZero() Then Exit For
                Next

                Dim xIncr = (maxVal - minVal) / (lastCol - firstCol)
                For i = firstCol To lastCol
                    Dim maskCol = mask.Col(i)
                    If maskCol.CountNonZero > 0 Then split(0).Col(i).SetTo(minVal + xIncr * i, maskCol)
                Next
            End If

            If check.Box(1).Checked Then
                split(1).MinMaxLoc(minVal, maxVal, minLoc, maxLoc, mask)
                Static vbCheck = findCheckBox("Use VB Kalman")
                If vbCheck.checked = False Then
                    kalman.kInput(0) = minVal
                    kalman.kInput(1) = maxVal
                    kalman.Run(ocvb)
                    split(1).SetTo((kalman.kOutput(0) + kalman.kOutput(1)) / 2, mask)
                Else
                    kalmanVB.kInput = (minVal + maxVal) / 2
                    kalmanVB.Run(ocvb)
                    split(1).SetTo(kalmanVB.kOutput, mask)
                End If
            End If

            If check.Box(2).Checked Then
                Dim firstRow As Integer, lastRow As Integer
                For firstRow = 0 To mask.Height - 1
                    If mask.Row(firstRow).CountNonZero() > 20 Then Exit For
                Next
                For lastRow = mask.Height - 1 To 0 Step -1
                    If mask.Row(lastRow).CountNonZero() > 20 Then Exit For
                Next

                If lastRow >= 0 And firstRow < mask.Height Then
                    Dim meanMin = split(2).Row(lastRow).Mean(mask.Row(lastRow))
                    Dim meanMax = split(2).Row(firstRow).Mean(mask.Row(firstRow))
                    Dim zIncr = (meanMax.Item(0) - meanMin.Item(0)) / Math.Abs(lastRow - firstRow)
                    For i = firstRow To lastRow
                        Dim maskRow = mask.Row(i)
                        Dim mean = split(2).Row(i).Mean(maskRow)
                        If maskRow.CountNonZero > 0 Then
                            split(2).Row(i).SetTo(mean.Item(0))
                            'Dim xy = New cv.Point3f(0, i, mean.Item(0))
                            'For xy.X = 0 To split(2).Width - 1
                            '    Dim xyz = getWorldCoordinates(ocvb, xy)
                            '    imuPC.Set(Of cv.Point3f)(i, xy.X, xyz)
                            'Next
                        End If
                    Next
                    dst1.Line(New cv.Point(0, firstRow), New cv.Point(dst1.Width, firstRow), cv.Scalar.Yellow, 2)
                    dst1.Line(New cv.Point(0, lastRow), New cv.Point(dst1.Width, lastRow), cv.Scalar.Yellow, 2)
                End If
            End If

            cv.Cv2.Merge(split, imuPC)

            imuPC.CopyTo(imuPointCloud, mask)
        End If
    End Sub
End Class