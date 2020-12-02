Imports cv = OpenCvSharp

Public Class DilateErode_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Dilate/Erode Kernel Size", 1, 32, 5)
        sliders.setupTrackBar(1, "Erode (-) to Dilate (+)", -32, 32, 1)
        task.desc = "Dilate and Erode the RGB and Depth image."

        radio.Setup(caller, 4)
        radio.check(0).Text = "Dilate/Erode shape: Cross"
        radio.check(1).Text = "Dilate/Erode shape: Ellipse"
        radio.check(2).Text = "Dilate/Erode shape: Rect"
        radio.check(3).Text = "Dilate/Erode shape: None"
        radio.check(0).Checked = True
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim iterations = sliders.trackbar(1).Value
        Dim kernelsize = sliders.trackbar(0).Value
        If kernelsize Mod 2 = 0 Then kernelsize += 1
        Dim morphShape = cv.MorphShapes.Cross
        If radio.check(1).Checked Then morphShape = cv.MorphShapes.Ellipse
        If radio.check(2).Checked Then morphShape = cv.MorphShapes.Rect
        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelsize, kernelsize))

        If radio.check(3).Checked Then
            dst1 = src
        Else
            If iterations >= 0 Then
                src.Dilate(element, Nothing, iterations).CopyTo(dst1)
            Else
                src.Erode(element, Nothing, -iterations).CopyTo(dst1)
            End If
        End If


        If standalone Then
            If iterations >= 0 Then
                dst2 = task.RGBDepth.Dilate(element, Nothing, iterations)
                label1 = "Dilate RGB " + CStr(iterations) + " times"
                label2 = "Dilate Depth " + CStr(iterations) + " times"
            Else
                dst2 = task.RGBDepth.Erode(element, Nothing, -iterations)
                label1 = "Erode RGB " + CStr(-iterations) + " times"
                label2 = "Erode Depth " + CStr(-iterations) + " times"
            End If
        End If
    End Sub
End Class





Public Class DilateErode_DepthSeed
    Inherits VBparent
    Dim dilate As DilateErode_Basics
    Public Sub New()
        initParent()
        dilate = New DilateErode_Basics()

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "DepthSeed flat depth", 1, 200, 100)
        sliders.setupTrackBar(1, "DepthSeed max Depth", 1, 5000, 3000)
        task.desc = "Erode depth to build a depth mask for inrange data."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim iterations = dilate.sliders.trackbar(1).Value
        Dim kernelsize = If(dilate.sliders.trackbar(0).Value Mod 2, dilate.sliders.trackbar(0).Value, dilate.sliders.trackbar(0).Value + 1)
        Dim morphShape = cv.MorphShapes.Cross
        If dilate.radio.check(0).Checked Then morphShape = cv.MorphShapes.Cross
        If dilate.radio.check(1).Checked Then morphShape = cv.MorphShapes.Ellipse
        If dilate.radio.check(2).Checked Then morphShape = cv.MorphShapes.Rect
        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelsize, kernelsize))

        Dim depth32f = getDepth32f()
        Dim mat As New cv.Mat
        cv.Cv2.Erode(depth32f, mat, element)
        mat = depth32f - mat
        Dim seeds = mat.LessThan(sliders.trackbar(0).Value).ToMat
        dst2 = seeds

        Dim validImg = depth32f.GreaterThan(0).ToMat
        validImg.SetTo(0, depth32f.GreaterThan(sliders.trackbar(1).Value)) ' max distance
        cv.Cv2.BitwiseAnd(seeds, validImg, seeds)
        dst1.SetTo(0)
        task.RGBDepth.CopyTo(dst1, seeds)
    End Sub
End Class



Public Class DilateErode_OpenClose
    Inherits VBparent
    Public Sub New()
        initParent()
        radio.Setup(caller, 3)
        radio.check(0).Text = "Open/Close shape: Cross"
        radio.check(1).Text = "Open/Close shape: Ellipse"
        radio.check(2).Text = "Open/Close shape: Rect"
        radio.check(2).Checked = True

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Dilate Open/Close Iterations", -10, 10, 10)
        task.desc = "Erode and dilate with MorphologyEx on the RGB and Depth image."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim n = sliders.trackbar(0).Value
        Dim an As integer = If(n > 0, n, -n)
        Dim morphShape = cv.MorphShapes.Rect
        If radio.check(0).Checked Then morphShape = cv.MorphShapes.Cross
        If radio.check(1).Checked Then morphShape = cv.MorphShapes.Ellipse
        If radio.check(2).Checked Then morphShape = cv.MorphShapes.Rect

        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(an * 2 + 1, an * 2 + 1), New cv.Point(an, an))
        If n < 0 Then
            cv.Cv2.MorphologyEx(task.RGBDepth, dst2, cv.MorphTypes.Open, element)
            cv.Cv2.MorphologyEx(src, dst1, cv.MorphTypes.Open, element)
        Else
            cv.Cv2.MorphologyEx(task.RGBDepth, dst2, cv.MorphTypes.Close, element)
            cv.Cv2.MorphologyEx(src, dst1, cv.MorphTypes.Close, element)
        End If
    End Sub
End Class



