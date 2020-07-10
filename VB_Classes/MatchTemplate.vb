Imports cv = OpenCvSharp
Public Class MatchTemplate_Basics
    Inherits ocvbClass
    Dim flow As Font_FlowText
    Public sample1 As cv.Mat
    Public sample2 As cv.Mat
    Public matchText As String = ""
    Public correlationMat As New cv.Mat
    Public reportFreq = 10 ' report the results every x number of iterations.
    Public matchOption As cv.TemplateMatchModes
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        flow = New Font_FlowText(ocvb)
        flow.result1or2 = RESULT1

        radio.Setup(ocvb, caller, 6)
        radio.check(0).Text = "CCoeff"
        radio.check(1).Text = "CCoeffNormed"
        radio.check(2).Text = "CCorr"
        radio.check(3).Text = "CCorrNormed"
        radio.check(4).Text = "SqDiff"
        radio.check(5).Text = "SqDiffNormed"
        radio.check(1).Checked = True
        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "Sample Size", 2, 10000, 100)
        ocvb.desc = "Find correlation coefficient for 2 random series.  Should be near zero except for small sample size."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        if standalone Then
            sample1 = New cv.Mat(New cv.Size(sliders.sliders(0).Value, 1), cv.MatType.CV_32FC1)
            sample2 = New cv.Mat(New cv.Size(sliders.sliders(0).Value, 1), cv.MatType.CV_32FC1)
            cv.Cv2.Randn(sample1, 100, 25)
            cv.Cv2.Randn(sample2, 0, 25)
        Else
            sliders.Visible = False
        End If

        matchOption = cv.TemplateMatchModes.CCoeffNormed
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                matchOption = Choose(i + 1, cv.TemplateMatchModes.CCoeff, cv.TemplateMatchModes.CCoeffNormed, cv.TemplateMatchModes.CCorr,
                                            cv.TemplateMatchModes.CCorrNormed, cv.TemplateMatchModes.SqDiff, cv.TemplateMatchModes.SqDiffNormed)
                matchText = Choose(i + 1, "CCoeff", "CCoeffNormed", "CCorr", "CCorrNormed", "SqDiff", "SqDiffNormed")
                Exit For
            End If
        Next
        cv.Cv2.MatchTemplate(sample1, sample2, correlationMat, matchOption)
        If ocvb.frameCount Mod reportFreq = 0 Then
            Dim correlation = correlationMat.Get(Of Single)(0, 0)
            label1 = "Correlation = " + Format(correlation, "#,##0.000")
            if standalone Then
                label1 = matchText + " for " + CStr(sample1.Cols) + " samples = " + Format(correlation, "#,##0.00")
                flow.msgs.Add(matchText + " = " + Format(correlation, "#,##0.00"))
                flow.Run(ocvb)
            End If
        End If
    End Sub
End Class




Public Class MatchTemplate_RowCorrelation
    Inherits ocvbClass
    Dim corr As MatchTemplate_Basics
    Dim flow As Font_FlowText
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        flow = New Font_FlowText(ocvb)
        flow.result1or2 = RESULT1

        corr = New MatchTemplate_Basics(ocvb)
        corr.sliders.Visible = False

        ocvb.desc = "Find correlation coefficients for 2 random rows in the RGB image to show variability"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim line1 = msRNG.Next(0, src.Height - 1)
        Dim line2 = msRNG.Next(0, src.Height - 1)

        corr.sample1 = src.Row(line1)
        corr.sample2 = src.Row(line2 + 1)
        corr.Run(ocvb)
        Dim correlation = corr.correlationMat.Get(Of Single)(0, 0)
        flow.msgs.Add(corr.matchText + " between lines " + CStr(line1) + " and line " + CStr(line2) + " = " + Format(correlation, "#,##0.00"))
        flow.Run(ocvb)

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
    Inherits ocvbClass
    Public saveTemplate As cv.Mat
    Public saveRect As cv.Rect
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        radio.Setup(ocvb, caller, 6)
        For i = 0 To radio.check.Count - 1
            radio.check(i).Text = Choose(i + 1, "SQDIFF", "SQDIFF NORMED", "TM CCORR", "TM CCORR NORMED", "TM COEFF", "TM COEFF NORMED")
        Next
        radio.check(5).Checked = True
        If standalone Then ocvb.drawRect = New cv.Rect(100, 100, 50, 50) ' arbitrary template to match

        label1 = "Probabilities (draw rectangle to test again)"
        label2 = "White is input, Red circle centers highest probability"
        ocvb.desc = "Find the requested template in an image.  Tracker Algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.drawRect.Width > 0 And ocvb.drawRect.Height > 0 Then
            If ocvb.drawRect.X + ocvb.drawRect.Width >= src.Width Then ocvb.drawRect.Width = src.Width - ocvb.drawRect.X
            If ocvb.drawRect.Y + ocvb.drawRect.Height >= src.Height Then ocvb.drawRect.Height = src.Height - ocvb.drawRect.Y
            saveRect = ocvb.drawRect
            saveTemplate = src(ocvb.drawRect).Clone()
            ocvb.drawRectClear = True
        End If
        Dim matchMethod As cv.TemplateMatchModes
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
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
    Inherits ocvbClass
    Dim entropy As Entropy_Highest_MT
    Dim match As MatchTemplate_DrawRect
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        match = New MatchTemplate_DrawRect(ocvb)

        entropy = New Entropy_Highest_MT(ocvb)

        ocvb.parms.ShowOptions = False ' we won't need the options...

        label1 = "Probabilities that the template matches image"
        label2 = "Red is the best template to match (highest entropy)"
        ocvb.desc = "Track an object - one with the highest entropy - using OpenCV's matchtemplate.  Tracker Algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount Mod 30 = 0 Then
            entropy.src = src
            entropy.Run(ocvb)
            ocvb.drawRect = entropy.bestContrast
        End If

        match.src = src
        match.Run(ocvb)
        dst1 = match.dst1
        dst2 = match.dst2
    End Sub
End Class
