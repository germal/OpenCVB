Imports cv = OpenCvSharp
Public Class MatchTemplate_Basics
    Inherits VBparent
    Dim flow As Font_FlowText
    Public sample1 As cv.Mat
    Public sample2 As cv.Mat
    Public matchText As String = ""
    Public correlationMat As New cv.Mat
    Public matchOption As cv.TemplateMatchModes
    Public Sub New()
        initParent()
        flow = New Font_FlowText()

        radio.Setup(caller, 6)
        radio.check(0).Text = "CCoeff"
        radio.check(1).Text = "CCoeffNormed"
        radio.check(2).Text = "CCorr"
        radio.check(3).Text = "CCorrNormed"
        radio.check(4).Text = "SqDiff"
        radio.check(5).Text = "SqDiffNormed"
        radio.check(1).Checked = True

        sliders.Setup(caller)
        sliders.setupTrackBar(0, "Sample Size", 2, 10000, 100)
        ocvb.desc = "Find correlation coefficient for 2 random series.  Should be near zero except for small sample size."
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If standalone Then
            sample1 = New cv.Mat(New cv.Size(sliders.trackbar(0).Value, 1), cv.MatType.CV_32FC1)
            sample2 = New cv.Mat(New cv.Size(sliders.trackbar(0).Value, 1), cv.MatType.CV_32FC1)
            cv.Cv2.Randn(sample1, 100, 25)
            cv.Cv2.Randn(sample2, 0, 25)
        End If

        matchOption = cv.TemplateMatchModes.CCoeffNormed
        Static frm = findForm("MatchTemplate_Basics Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                matchOption = Choose(i + 1, cv.TemplateMatchModes.CCoeff, cv.TemplateMatchModes.CCoeffNormed, cv.TemplateMatchModes.CCorr,
                                            cv.TemplateMatchModes.CCorrNormed, cv.TemplateMatchModes.SqDiff, cv.TemplateMatchModes.SqDiffNormed)
                matchText = Choose(i + 1, "CCoeff", "CCoeffNormed", "CCorr", "CCorrNormed", "SqDiff", "SqDiffNormed")
                Exit For
            End If
        Next
        cv.Cv2.MatchTemplate(sample1, sample2, correlationMat, matchOption)
        Dim correlation = correlationMat.Get(Of Single)(0, 0)
        label1 = "Correlation = " + Format(correlation, "#,##0.000")
        If standalone Then
            dst1.SetTo(0)
            label1 = matchText + " for " + CStr(sample1.Cols) + " samples = " + Format(correlation, "#,##0.00")
            flow.msgs.Add(matchText + " = " + Format(correlation, "#,##0.00"))
            flow.Run()
        End If
    End Sub
End Class




Public Class MatchTemplate_RowCorrelation
    Inherits VBparent
    Dim corr As MatchTemplate_Basics
    Dim flow As Font_FlowText
    Public Sub New()
        initParent()
        flow = New Font_FlowText()

        corr = New MatchTemplate_Basics()
        hideForm("MatchTemplate_Basics Slider Options")

        ocvb.desc = "Find correlation coefficients for 2 random rows in the RGB image to show variability"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim line1 = msRNG.Next(0, src.Height - 1)
        Dim line2 = msRNG.Next(0, src.Height - 1)

        corr.sample1 = src.Row(line1)
        corr.sample2 = src.Row(line2 + 1)
        corr.Run()
        Dim correlation = corr.correlationMat.Get(Of Single)(0, 0)
        flow.msgs.Add(corr.matchText + " between lines " + CStr(line1) + " and line " + CStr(line2) + " = " + Format(correlation, "#,##0.00"))
        flow.Run()

        Static minCorrelation As Single
        Static maxCorrelation As Single

        Static saveCorrType = corr.matchOption
        If ocvb.frameCount = 0 Or saveCorrType <> corr.matchOption Then
            minCorrelation = Single.PositiveInfinity
            maxCorrelation = Single.NegativeInfinity
            saveCorrType = corr.matchOption
        End If

        If correlation < minCorrelation Then minCorrelation = correlation
        If correlation > maxCorrelation Then maxCorrelation = correlation
        label1 = "Min = " + Format(minCorrelation, "#,##0.00") + " max = " + Format(maxCorrelation, "#,##0.0000")
    End Sub
End Class





Public Class MatchTemplate_DrawRect
    Inherits VBparent
    Public saveTemplate As cv.Mat
    Public saveRect As cv.Rect
    Public Sub New()
        initParent()
        radio.Setup(caller, 6)
        Static frm = findForm("MatchTemplate_DrawRect Radio Options")
        For i = 0 To frm.check.length - 1
            frm.check(i).Text = Choose(i + 1, "SQDIFF", "SQDIFF NORMED", "TM CCORR", "TM CCORR NORMED", "TM COEFF", "TM COEFF NORMED")
        Next
        radio.check(5).Checked = True
        If standalone Then ocvb.task.drawRect = New cv.Rect(100, 100, 50, 50) ' arbitrary template to match

        label1 = "Probabilities (draw rectangle to test again)"
        label2 = "White is input, Red circle centers highest probability"
        ocvb.desc = "Find the requested template in an image.  Tracker Algorithm"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.task.drawRect.Width > 0 And ocvb.task.drawRect.Height > 0 Then
            If ocvb.task.drawRect.X + ocvb.task.drawRect.Width >= src.Width Then ocvb.task.drawRect.Width = src.Width - ocvb.task.drawRect.X
            If ocvb.task.drawRect.Y + ocvb.task.drawRect.Height >= src.Height Then ocvb.task.drawRect.Height = src.Height - ocvb.task.drawRect.Y
            saveRect = ocvb.task.drawRect
            saveTemplate = src(ocvb.task.drawRect).Clone()
            ocvb.task.drawRectClear = True
        End If
        Dim matchMethod As cv.TemplateMatchModes
        Static frm = findForm("MatchTemplate_DrawRect Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                matchMethod = Choose(i + 1, cv.TemplateMatchModes.SqDiff, cv.TemplateMatchModes.SqDiffNormed, cv.TemplateMatchModes.CCorr,
                                          cv.TemplateMatchModes.CCorrNormed, cv.TemplateMatchModes.CCoeff, cv.TemplateMatchModes.CCoeffNormed)
                Exit For
            End If
        Next
        cv.Cv2.MatchTemplate(src, saveTemplate, dst1, matchMethod)
        dst2 = src
        dst2.Rectangle(saveRect, cv.Scalar.White, 1)
        Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point
        dst1.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)
        dst2.Circle(maxLoc.X + saveRect.Width / 2, maxLoc.Y + saveRect.Height / 2, 20, cv.Scalar.Red, 3, cv.LineTypes.AntiAlias)
    End Sub
End Class





Public Class MatchTemplate_BestEntropy_MT
    Inherits VBparent
    Dim entropy As Entropy_Highest_MT
    Dim match As MatchTemplate_DrawRect
    Public Sub New()
        initParent()

        match = New MatchTemplate_DrawRect()

        entropy = New Entropy_Highest_MT()

        label1 = "Probabilities that the template matches image"
        label2 = "Red is the best template to match (highest entropy)"
        ocvb.desc = "Track an object - one with the highest entropy - using OpenCV's matchtemplate.  Tracker Algorithm"
    End Sub
    Public Sub Run()
		If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount Mod 30 = 0 Then
            entropy.src = src
            entropy.Run()
            ocvb.task.drawRect = entropy.bestContrast
        End If

        match.src = src
        match.Run()
        dst1 = match.dst1
        dst2 = match.dst2
    End Sub
End Class

