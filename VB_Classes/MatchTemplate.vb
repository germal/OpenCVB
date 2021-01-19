Imports cv = OpenCvSharp
Imports System.Threading
Public Class MatchTemplate_Basics
    Inherits VBparent
    Dim flow As Font_FlowText
    Public sample As cv.Mat
    Public searchMat As cv.Mat
    Public matchText As String = ""
    Public correlationMat As New cv.Mat
    Public correlation As Single
    Public matchOption As cv.TemplateMatchModes
    Public Sub New()
        initParent()
        flow = New Font_FlowText()

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 6)
            radio.check(0).Text = "CCoeff"
            radio.check(1).Text = "CCoeffNormed"
            radio.check(2).Text = "CCorr"
            radio.check(3).Text = "CCorrNormed"
            radio.check(4).Text = "SqDiff"
            radio.check(5).Text = "SqDiffNormed"
            radio.check(1).Checked = True
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Sample Size", 2, 10000, 100)
        End If
        task.desc = "Find correlation coefficient for 2 random series.  Should be near zero except for small sample size."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static sampleSlider = findSlider("Sample Size")
        If standalone Or task.intermediateReview = caller Then
            sample = New cv.Mat(New cv.Size(CInt(sampleSlider.Value), 1), cv.MatType.CV_32FC1)
            searchMat = New cv.Mat(New cv.Size(CInt(sampleSlider.Value), 1), cv.MatType.CV_32FC1)
            cv.Cv2.Randn(sample, 100, 25)
            cv.Cv2.Randn(searchMat, 0, 25)
        End If

        matchOption = cv.TemplateMatchModes.CCoeffNormed
        Static frm = findfrm("MatchTemplate_Basics Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                matchOption = Choose(i + 1, cv.TemplateMatchModes.CCoeff, cv.TemplateMatchModes.CCoeffNormed, cv.TemplateMatchModes.CCorr,
                                            cv.TemplateMatchModes.CCorrNormed, cv.TemplateMatchModes.SqDiff, cv.TemplateMatchModes.SqDiffNormed)
                matchText = Choose(i + 1, "CCoeff", "CCoeffNormed", "CCorr", "CCorrNormed", "SqDiff", "SqDiffNormed")
                Exit For
            End If
        Next
        cv.Cv2.MatchTemplate(sample, searchMat, correlationMat, matchOption)
        correlation = correlationMat.Get(Of Single)(0, 0)
        label1 = "Correlation = " + Format(correlation, "#,##0.000")
        If standalone Or task.intermediateReview = caller Then
            dst1.SetTo(0)
            label1 = matchText + " for " + CStr(sample.Cols) + " samples = " + Format(correlation, "#,##0.00")
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

        task.desc = "Find correlation coefficients for 2 random rows in the RGB image to show variability"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim line1 = msRNG.Next(0, src.Height - 1)
        Dim line2 = msRNG.Next(0, src.Height - 1)

        corr.sample = src.Row(line1)
        corr.searchMat = src.Row(line2 + 1)
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
    Dim match As MatchTemplate_Basics
    Public Sub New()
        initParent()
        If standalone Then task.drawRect = New cv.Rect(100, 100, 50, 50) ' arbitrary template to match

        match = New MatchTemplate_Basics

        label1 = "Probabilities (draw rectangle to test again)"
        label2 = "White is input, Red circle centers highest probability"
        task.desc = "Find the requested template in an image.  Tracker Algorithm"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then
            If task.drawRect.X + task.drawRect.Width >= src.Width Then task.drawRect.Width = src.Width - task.drawRect.X
            If task.drawRect.Y + task.drawRect.Height >= src.Height Then task.drawRect.Height = src.Height - task.drawRect.Y
            saveRect = task.drawRect
            saveTemplate = src(task.drawRect).Clone()
            task.drawRectClear = True
        End If

        match.sample = saveTemplate
        match.searchMat = src
        match.Run()
        dst1 = match.correlationMat
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
        task.desc = "Track an object - one with the highest entropy - using OpenCV's matchtemplate.  Tracker Algorithm"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount Mod 30 = 0 Then
            entropy.src = src
            entropy.Run()
            task.drawRect = entropy.bestContrast
        End If

        match.src = src
        match.Run()
        dst1 = match.dst1
        dst2 = match.dst2
    End Sub
End Class












Public Class MatchTemplate_Movement
    Inherits VBparent
    Dim grid As Thread_Grid
    Public Sub New()
        initParent()
        grid = New Thread_Grid

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Correlation Threshold", 0, 1000, 970)
        End If

        task.desc = "Detect Motion in the color image"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        grid.Run()
        dst1 = src.Clone
        If dst1.Channels = 3 Then dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If ocvb.frameCount = 0 Then dst2 = dst1.Clone()
        Static correlationSlider = findSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        Dim updateCount As Integer
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim correlation As New cv.Mat
            cv.Cv2.MatchTemplate(dst1(roi), dst2(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
            If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                Interlocked.Increment(updateCount)
                dst1(roi).CopyTo(dst2(roi))
            Else
                dst1(roi).SetTo(0)
            End If
        End Sub)
        dst1.SetTo(255, grid.gridMask)
        label1 = CStr(updateCount) + " segments (of " + CStr(grid.roiList.Count) + ") that were updated"
        label2 = CStr(grid.roiList.Count - updateCount) + " segments out of " + CStr(grid.roiList.Count) + " had > " + Format(correlationSlider.value / 1000, "0.0%") + " correlation"
    End Sub
End Class