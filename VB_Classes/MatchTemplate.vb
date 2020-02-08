Imports cv = OpenCvSharp
Public Class MatchTemplate_Correlation : Implements IDisposable
    Public sliders As New OptionsSliders
    Dim flow As Font_FlowText
    Dim radio As New OptionsRadioButtons
    Public sample1 As cv.Mat
    Public sample2 As cv.Mat
    Public externalUse As Boolean
    Public matchText As String = ""
    Public correlationMat As New cv.Mat
    Public reportFreq = 10 ' report the results every x number of iterations.
    Public Sub New(ocvb As AlgorithmData)
        flow = New Font_FlowText(ocvb)
        flow.externalUse = True
        flow.result1or2 = RESULT2

        radio.Setup(ocvb, 6)
        radio.check(0).Text = "CCoeff"
        radio.check(1).Text = "CCoeffNormed"
        radio.check(2).Text = "CCorr"
        radio.check(3).Text = "CCorrNormed"
        radio.check(4).Text = "SqDiff"
        radio.check(5).Text = "SqDiffNormed"
        radio.check(1).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()
        sliders.setupTrackBar1(ocvb, "Sample Size", 2, 10000, 100)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.label2 = "Log of correlation results"
        ocvb.desc = "Find correlation coefficient for 2 random series.  Should be near zero except for small sample size."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then
            sample1 = New cv.Mat(New cv.Size(sliders.TrackBar1.Value, 1), cv.MatType.CV_32FC1)
            sample2 = New cv.Mat(New cv.Size(sliders.TrackBar1.Value, 1), cv.MatType.CV_32FC1)
            cv.Cv2.Randn(sample1, 100, 25)
            cv.Cv2.Randn(sample2, 0, 25)
        Else
            sliders.Visible = False
        End If

        Dim matchOption = cv.TemplateMatchModes.CCoeffNormed
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                matchOption = Choose(i + 1, cv.TemplateMatchModes.CCoeff, cv.TemplateMatchModes.CCoeffNormed, cv.TemplateMatchModes.CCorr,
                                            cv.TemplateMatchModes.CCorrNormed, cv.TemplateMatchModes.SqDiff, cv.TemplateMatchModes.SqDiffNormed)
                matchText = Choose(i + 1, "CCoeff", "CCoeffNormed", "CCorr", "CCorrNormed", "SqDiff", "SqDiffNormed")
            End If
        Next
        cv.Cv2.MatchTemplate(sample1, sample2, correlationMat, matchOption)
        If ocvb.frameCount Mod reportFreq = 0 Then
            Dim correlation = correlationMat.At(Of Single)(0, 0)
            ocvb.label1 = "Correlation = " + Format(correlation, "#,##0.000")
            If externalUse = False Then
                ocvb.label1 = matchText + " for " + CStr(sample1.Rows) + " samples each = " + Format(correlation, "#,##0.00")
                flow.msgs.Add(matchText + " = " + Format(correlation, "#,##0.00"))
                flow.Run(ocvb)
            End If
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
    End Sub
End Class




Public Class MatchTemplate_RowCorrelation : Implements IDisposable
    Dim mathCor As MatchTemplate_Correlation
    Dim flow As Font_FlowText
    Public Sub New(ocvb As AlgorithmData)
        flow = New Font_FlowText(ocvb)
        flow.externalUse = True
        flow.result1or2 = RESULT2

        mathCor = New MatchTemplate_Correlation(ocvb)
        mathCor.externalUse = True
        mathCor.sliders.Visible = False

        ocvb.desc = "Find correlation coefficients for 2 random rows in the RGB image to show variability"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim line1 = ocvb.ms_rng.Next(0, ocvb.color.Height - 1)
        Dim line2 = ocvb.ms_rng.Next(0, ocvb.color.Height - 1)

        ocvb.result1.SetTo(0)
        Dim nextLine = ocvb.result1.Row(line1)
        nextLine = ocvb.color.Row(line1)
        nextLine = ocvb.result1.Row(line2)
        nextLine = ocvb.color.Row(line2)

        mathCor.sample1 = ocvb.color.Row(line1).Clone()
        mathCor.sample2 = ocvb.color.Row(line2 + 1).Clone()
        mathCor.Run(ocvb)
        Dim correlation = mathCor.correlationMat.At(Of Single)(0, 0)
        flow.msgs.Add(mathCor.matchText + " between lines " + CStr(line1) + " and line " + CStr(line2) + " = " + Format(correlation, "#,##0.00"))
        flow.Run(ocvb)

        Static minCorrelation = Single.PositiveInfinity
        Static maxCorrelation = Single.NegativeInfinity
        If correlation < minCorrelation Then minCorrelation = correlation
        If correlation > maxCorrelation Then maxCorrelation = correlation
        ocvb.label1 = "Min = " + Format(minCorrelation, "#,##0.00") + " max = " + Format(maxCorrelation, "#,##0.0000")
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        mathCor.Dispose()
        flow.Dispose()
    End Sub
End Class





Public Class MatchTemplate_Basics : Implements IDisposable
    Dim radio As New OptionsRadioButtons
    Public Sub New(ocvb As AlgorithmData)
        radio.Setup(ocvb, 6)
        For i = 0 To radio.check.Count - 1
            radio.check(i).Text = Choose(i + 1, "SQDIFF", "SQDIFF NORMED", "TM CCORR", "TM CCORR NORMED", "TM COEFF", "TM COEFF NORMED")
        Next
        radio.check(5).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()

        ocvb.drawRect = New cv.Rect(100, 100, 50, 50) ' arbitrary template to match

        ocvb.label1 = "MatchTemplate probabilities"
        ocvb.label2 = "White is input, Red is highest probability"
        ocvb.desc = "Find the requested template in an image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static saveTemplate As cv.Mat
        Static saveRect As cv.Rect
        If ocvb.drawRect.Width > 0 And ocvb.drawRect.Height > 0 Then
            saveRect = ocvb.drawRect
            saveTemplate = ocvb.color(ocvb.drawRect).Clone()
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
        cv.Cv2.MatchTemplate(ocvb.color, saveTemplate, ocvb.result1, matchMethod)
        ocvb.result2 = ocvb.color
        saveTemplate.CopyTo(ocvb.result2(saveRect))
        ocvb.result2.Rectangle(saveRect, cv.Scalar.White, 1)
        Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point
        ocvb.result1.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)
        ocvb.result2.Circle(maxLoc.X + saveRect.Width / 2, maxLoc.Y + saveRect.Height / 2, 20, cv.Scalar.Red, 3, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        radio.Dispose()
    End Sub
End Class





Public Class MatchTemplate_BestTemplate : Implements IDisposable
    Dim grid As Thread_Grid
    Dim entropies(0) As Entropy_Basics
    Dim match As MatchTemplate_Basics
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32
        grid.externalUse = True

        match = New MatchTemplate_Basics(ocvb)

        ocvb.parms.ShowOptions = False ' we won't need the options...

        ocvb.label2 = "White is highest entropy (input). Red is best match."
        ocvb.desc = "Find the best object to track in the image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static bestContrast As cv.Rect
        If ocvb.frameCount Mod 100 = 0 Then
            grid.Run(ocvb)
            If entropies.Length <> grid.roiList.Count Then
                ReDim entropies(grid.roiList.Count - 1)
                For i = 0 To entropies.Length - 1
                    entropies(i) = New Entropy_Basics(ocvb)
                    entropies(i).externalUse = True
                Next
            End If

            Parallel.For(0, grid.roiList.Count - 1,
             Sub(i)
                 entropies(i).src = ocvb.color(grid.roiList(i))
                 entropies(i).Run(ocvb)
             End Sub)

            Dim maxEntropy As Single
            Dim maxIndex As Int32
            For i = 0 To entropies.Count - 1
                If entropies(i).entropy > maxEntropy Then
                    maxEntropy = entropies(i).entropy
                    maxIndex = i
                End If
            Next

            ocvb.result2 = ocvb.color.Clone()
            bestContrast = grid.roiList(maxIndex)
            ocvb.drawRect = bestContrast
        End If

        match.Run(ocvb)
        ocvb.label1 = "MatchTemplate probabilities"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
    End Sub
End Class