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
        If standalone Then src = ocvb.color

        Dim iterations = sliders.TrackBar2.Value
        Dim kernelsize = sliders.TrackBar1.Value
        If kernelsize Mod 2 = 0 Then kernelsize += 1
        Dim morphShape = cv.MorphShapes.Cross
        If radio.check(0).Checked Then morphShape = cv.MorphShapes.Cross
        If radio.check(1).Checked Then morphShape = cv.MorphShapes.Ellipse
        If radio.check(2).Checked Then morphShape = cv.MorphShapes.Rect
        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelsize, kernelsize))

        If iterations >= 0 Then
            src.Dilate(element, Nothing, iterations).CopyTo(dst1)
        Else
            src.Erode(element, Nothing, -iterations).CopyTo(dst1)
        End If

        if standalone Then
            If iterations >= 0 Then
                dst2 = ocvb.RGBDepth.Dilate(element, Nothing, iterations)
                label1 = "Dilate RGB " + CStr(iterations) + " times"
                label2 = "Dilate Depth " + CStr(iterations) + " times"
            Else
                dst2 = ocvb.RGBDepth.Erode(element, Nothing, -iterations)
                label1 = "Erode RGB " + CStr(-iterations) + " times"
                label2 = "Erode Depth " + CStr(-iterations) + " times"
            End If
        End If
    End Sub
End Class





Public Class DilateErode_DepthSeed
    Inherits ocvbClass
    Dim dilate As DilateErode_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        dilate = New DilateErode_Basics(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "DepthSeed flat depth", 1, 200, 100)
        sliders.setupTrackBar2(ocvb, caller, "DepthSeed max Depth", 1, 5000, 3000)
        ocvb.desc = "Erode depth to build a depth mask for inrange data."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim iterations = dilate.sliders.TrackBar2.Value
        Dim kernelsize = If(dilate.sliders.TrackBar1.Value Mod 2, dilate.sliders.TrackBar1.Value, dilate.sliders.TrackBar1.Value + 1)
        Dim morphShape = cv.MorphShapes.Cross
        If dilate.radio.check(0).Checked Then morphShape = cv.MorphShapes.Cross
        If dilate.radio.check(1).Checked Then morphShape = cv.MorphShapes.Ellipse
        If dilate.radio.check(2).Checked Then morphShape = cv.MorphShapes.Rect
        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(kernelsize, kernelsize))

        Dim depth32f = getDepth32f(ocvb)
        Dim mat As New cv.Mat
        cv.Cv2.Erode(depth32f, mat, element)
        mat = depth32f - mat
        Dim seeds = mat.LessThan(sliders.TrackBar1.Value).ToMat
        dst2 = seeds

        Dim validImg = depth32f.GreaterThan(0).ToMat
        validImg.SetTo(0, depth32f.GreaterThan(sliders.TrackBar2.Value)) ' max distance
        cv.Cv2.BitwiseAnd(seeds, validImg, seeds)
        ocvb.result1.SetTo(0)
        ocvb.RGBDepth.CopyTo(ocvb.result1, seeds)
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
        Dim morphShape = cv.MorphShapes.Rect
        If radio.check(0).Checked Then morphShape = cv.MorphShapes.Cross
        If radio.check(1).Checked Then morphShape = cv.MorphShapes.Ellipse
        If radio.check(2).Checked Then morphShape = cv.MorphShapes.Rect

        Dim element = cv.Cv2.GetStructuringElement(morphShape, New cv.Size(an * 2 + 1, an * 2 + 1), New cv.Point(an, an))
        If n < 0 Then
            cv.Cv2.MorphologyEx(ocvb.RGBDepth, dst2, cv.MorphTypes.Open, element)
            cv.Cv2.MorphologyEx(ocvb.color, dst1, cv.MorphTypes.Open, element)
        Else
            cv.Cv2.MorphologyEx(ocvb.RGBDepth, dst2, cv.MorphTypes.Close, element)
            cv.Cv2.MorphologyEx(ocvb.color, dst1, cv.MorphTypes.Close, element)
        End If
    End Sub
End Class


