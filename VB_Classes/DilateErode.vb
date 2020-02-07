Imports cv = OpenCvSharp

Public Class DilateErode_Basics : Implements IDisposable
    Public sliders As New OptionsSliders
    Public radio As New OptionsRadioButtons
    Public src As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "DilateErode Kernel Size", 1, 32, 5)
        sliders.setupTrackBar2(ocvb, "Minus (Erode) to Plus (Dilate)", -32, 32, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Dilate and Erode the RGB and Depth image."

        radio.Setup(ocvb, 3)
        radio.check(0).Text = "Cross"
        radio.check(1).Text = "Ellipse"
        radio.check(2).Text = "Rect"
        radio.check(0).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim iterations = sliders.TrackBar2.Value
        Dim kernelsize = sliders.TrackBar1.Value
        If kernelsize Mod 2 = 0 Then kernelsize += 1
        Dim morphShape = cv.MorphShapes.Cross
        If radio.check(1).Checked Then morphShape = cv.MorphShapes.Ellipse
        If radio.check(2).Checked Then morphShape = cv.MorphShapes.Rect
        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelsize, kernelsize))
        If externalUse = False Then
            If iterations >= 0 Then
                ocvb.result1 = ocvb.color.Dilate(element, Nothing, iterations)
                ocvb.result2 = ocvb.RGBDepth.Dilate(element, Nothing, iterations)
                ocvb.label1 = "Dilate RGB " + CStr(iterations) + " times"
                ocvb.label2 = "Dilate Depth " + CStr(iterations) + " times"
            Else
                ocvb.result1 = ocvb.color.Erode(element, Nothing, -iterations)
                ocvb.result2 = ocvb.RGBDepth.Erode(element, Nothing, -iterations)
                ocvb.label1 = "Erode RGB " + CStr(-iterations) + " times"
                ocvb.label2 = "Erode Depth " + CStr(-iterations) + " times"
            End If
        Else
            If iterations >= 0 Then
                ocvb.result1 = src.Dilate(element, Nothing, iterations)
            Else
                ocvb.result1 = src.Erode(element, Nothing, -iterations)
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
    End Sub
End Class



Public Class DilateErode_DepthSeed : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Erode depth to build a depth mask for inrange data."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mat16 As New cv.Mat
        Dim element5x5 = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(5, 5))
        cv.Cv2.Erode(ocvb.depth, mat16, element5x5)
        mat16 = ocvb.depth - mat16
        Dim flatDepth As Single = 100
        Dim seeds As New cv.Mat
        seeds = mat16.LessThan(flatDepth)

        Dim validImg As New cv.Mat
        validImg = ocvb.depth.GreaterThan(0)
        validImg.SetTo(0, ocvb.depth.GreaterThan(3000)) ' max distance
        cv.Cv2.BitwiseAnd(seeds, validImg, seeds)
        ocvb.result1.SetTo(0)
        ocvb.RGBDepth.CopyTo(ocvb.result1, seeds)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class





Public Class DilateErode_OpenClose : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim radio As New OptionsRadioButtons
    Public Sub New(ocvb As AlgorithmData)
        radio.Setup(ocvb, 3)
        radio.check(0).Text = "Cross"
        radio.check(1).Text = "Ellipse"
        radio.check(2).Text = "Rect"
        radio.check(2).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()

        sliders.setupTrackBar1(ocvb, "Dilate Open/Close Iterations", -10, 10, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.desc = "Erode and dilate with MorphologyEx on the RGB and Depth image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim n = sliders.TrackBar1.Value
        Dim an As Int32 = If(n > 0, n, -n)
        Dim elementShape = cv.MorphShapes.Rect
        For i = 0 To radio.check.Length - 1
            If radio.check(i).Checked Then
                elementShape = Choose(i + 1, cv.MorphShapes.Cross, cv.MorphShapes.Ellipse, cv.MorphShapes.Rect)
                Exit For
            End If
        Next
        Dim element = cv.Cv2.GetStructuringElement(elementShape, New cv.Size(an * 2 + 1, an * 2 + 1), New cv.Point(an, an))
        If n < 0 Then
            cv.Cv2.MorphologyEx(ocvb.RGBDepth, ocvb.result2, cv.MorphTypes.Open, element)
            cv.Cv2.MorphologyEx(ocvb.color, ocvb.result1, cv.MorphTypes.Open, element)
        Else
            cv.Cv2.MorphologyEx(ocvb.RGBDepth, ocvb.result2, cv.MorphTypes.Close, element)
            cv.Cv2.MorphologyEx(ocvb.color, ocvb.result1, cv.MorphTypes.Close, element)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
    End Sub
End Class

