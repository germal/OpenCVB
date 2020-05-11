Imports cv = OpenCvSharp

Public Class DilateErode_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Dilate/Erode Kernel Size", 1, 32, 5)
        sliders.setupTrackBar2(ocvb, caller, "Erode (-) to Dilate (+)", -32, 32, 1)
        ocvb.desc = "Dilate and Erode the RGB and Depth image."

        radio.Setup(ocvb, caller, 3)
        radio.check(0).Text = "Dilate/Erode shape: Cross"
        radio.check(1).Text = "Dilate/Erode shape: Ellipse"
        radio.check(2).Text = "Dilate/Erode shape: Rect"
        radio.check(0).Checked = True
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            src = ocvb.color
        End If

        Dim iterations = sliders.TrackBar2.Value
        Dim kernelsize = sliders.TrackBar1.Value
        If kernelsize Mod 2 = 0 Then kernelsize += 1
        Dim morphShape = cv.MorphShapes.Cross
        If radio.check(1).Checked Then morphShape = cv.MorphShapes.Ellipse
        If radio.check(2).Checked Then morphShape = cv.MorphShapes.Rect
        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelsize, kernelsize))

        If iterations >= 0 Then
            src.Dilate(element, Nothing, iterations).CopyTo(dst)
        Else
            src.Erode(element, Nothing, -iterations).CopyTo(dst)
        End If

        if standalone Then
            If iterations >= 0 Then
                dst2 = ocvb.RGBDepth.Dilate(element, Nothing, iterations)
                ocvb.label1 = "Dilate RGB " + CStr(iterations) + " times"
                ocvb.label2 = "Dilate Depth " + CStr(iterations) + " times"
            Else
                dst2 = ocvb.RGBDepth.Erode(element, Nothing, -iterations)
                ocvb.label1 = "Erode RGB " + CStr(-iterations) + " times"
                ocvb.label2 = "Erode Depth " + CStr(-iterations) + " times"
            End If
        End If
    End Sub
End Class



Public Class DilateErode_DepthSeed
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Erode depth to build a depth mask for inrange data."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mat As New cv.Mat
        Dim element5x5 = cv.Cv2.GetStructuringElement(cv.MorphShapes.Rect, New cv.Size(5, 5))
        Dim depth32f = getDepth32f(ocvb)
        cv.Cv2.Erode(depth32f, mat, element5x5)
        mat = depth32f - mat
        Dim flatDepth As Single = 100
        Dim seeds As New cv.Mat
        seeds = mat.LessThan(flatDepth)

        Dim validImg As New cv.Mat
        validImg = depth32f.GreaterThan(0)
        validImg.SetTo(0, depth32f.GreaterThan(3000)) ' max distance
        cv.Cv2.BitwiseAnd(seeds, validImg, seeds)
        dst.SetTo(0)
        ocvb.RGBDepth.CopyTo(dst, seeds)
    End Sub
End Class





Public Class DilateErode_OpenClose
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        radio.Setup(ocvb, caller, 3)
        radio.check(0).Text = "Open/Close shape: Cross"
        radio.check(1).Text = "Open/Close shape: Ellipse"
        radio.check(2).Text = "Open/Close shape: Rect"
        radio.check(2).Checked = True

        sliders.setupTrackBar1(ocvb, caller, "Dilate Open/Close Iterations", -10, 10, 10)
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
            cv.Cv2.MorphologyEx(ocvb.color, dst, cv.MorphTypes.Open, element)
        Else
            cv.Cv2.MorphologyEx(ocvb.RGBDepth, ocvb.result2, cv.MorphTypes.Close, element)
            cv.Cv2.MorphologyEx(ocvb.color, dst, cv.MorphTypes.Close, element)
        End If
    End Sub
End Class


