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
        ocvb.result1.Row(line1) = ocvb.color.Row(line1)
        ocvb.result1.Row(line2) = ocvb.color.Row(line2)

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
