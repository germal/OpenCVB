Imports cv = OpenCvSharp
Public Class KLT_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim check As New OptionsCheckbox
    Dim oldGray As New cv.Mat
    Public points() As cv.Point2f
    Public status As New cv.Mat
    Public externalUse As Boolean
    Public outputMat As New cv.Mat
    Dim term As New cv.TermCriteria(cv.CriteriaType.Eps + cv.CriteriaType.Count, 10, 1.0)
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "KLT - MaxCorners", 1, 200, 100)
        sliders.setupTrackBar2(ocvb, "KLT - qualityLevel", 1, 100, 1) ' low quality!  We want lots of points.
        sliders.setupTrackBar3(ocvb, "KLT - minDistance", 1, 100, 7)
        sliders.setupTrackBar4(ocvb, "KLT - BlockSize", 1, 100, 7)
        If ocvb.parms.ShowOptions Then sliders.show()

        check.Setup(ocvb, 2)
        check.Box(0).Text = "KLT - Night Mode"
        check.Box(1).Text = "KLT - delete all Points"
        If ocvb.parms.ShowOptions Then check.show()

        ReDim points(0)
        ocvb.desc = "Track movement with Kanada-Lucas-Tomasi algorithm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim maxCorners = sliders.TrackBar1.Value
        Dim qualityLevel = sliders.TrackBar2.Value / 100
        Dim minDistance = sliders.TrackBar3.Value
        Dim blockSize = sliders.TrackBar4.Value
        Dim winSize As New cv.Size(3, 3)
        Dim subPixWinSize As New cv.Size(10, 10)
        Dim nightMode = check.Box(0).Checked

        If nightMode Then ocvb.result1.SetTo(0) Else ocvb.color.CopyTo(ocvb.result1)

        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If ocvb.frameCount = 0 Or points.Length <= 2 Then
            points = cv.Cv2.GoodFeaturesToTrack(gray, maxCorners, qualityLevel, minDistance, New cv.Mat, blockSize, False, 0)
            If points.Length > 0 Then
                points = cv.Cv2.CornerSubPix(gray, points, subPixWinSize, New cv.Size(-1, -1), term)
            End If
        ElseIf points.Length > 0 Then
            Dim err As New cv.Mat
            ' convert the point2f vector to an inputarray (cv.Mat)
            Dim inputMat = New cv.Mat(points.Length, 1, cv.MatType.CV_32FC2, points)
            outputMat = inputMat.Clone()
            cv.Cv2.CalcOpticalFlowPyrLK(oldGray, gray, inputMat, outputMat, status, err, winSize, 3, term, cv.OpticalFlowFlags.None)

            ocvb.result2.SetTo(0)
            For i = 0 To outputMat.Rows - 1
                Dim pt = outputMat.At(Of cv.Point2f)(i)
                If pt.X >= 0 And pt.X <= ocvb.color.Cols And pt.Y >= 0 And pt.Y <= ocvb.color.Rows Then
                    If status.At(Of Byte)(i) Then
                        ocvb.result1.Circle(pt, 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                        ocvb.result2.Circle(pt, 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                    End If
                Else
                    status.Set(Of Byte)(i, 0) ' this point is not visible!
                End If
            Next

            If externalUse = False Then
                Dim k As Int32
                For i = 0 To points.Length - 1
                    If status.At(Of Byte)(i) Then
                        points(k) = outputMat.At(Of cv.Point2f)(i)
                        k += 1
                    End If
                Next
                ReDim Preserve points(k)
            End If
        End If

        ' if they want to autoInitialize when there are no points left...
        If check.Box(1).Checked Then
            ReDim points(0) ' just delete all points and start again.
            check.Box(1).Checked = False
        End If
        oldGray = gray.Clone()
        ocvb.label1 = "KLT Basics - " + CStr(points.Length) + " points"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        check.Dispose()
    End Sub
End Class




Public Class KLT_OpticalFlow : Implements IDisposable
    Dim klt As KLT_Basics
    Dim lastpoints As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        klt = New KLT_Basics(ocvb)
        klt.externalUse = True ' we will compress the points file below.
        ocvb.desc = "KLT optical flow"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        klt.Run(ocvb)
        If ocvb.frameCount > 0 And klt.outputMat.Rows > 0 Then
            Dim k As Int32
            For i = 0 To klt.outputMat.Rows - 1
                If klt.status.At(Of Byte)(i) Then
                    Dim pt1 = lastpoints.Get(Of cv.Point2f)(i)
                    Dim pt2 = klt.outputMat.At(Of cv.Point2f)(i)
                    ocvb.result1.Line(pt1, pt2, cv.Scalar.Blue, 2, cv.LineTypes.AntiAlias)
                    ocvb.result2.Line(pt1, pt2, cv.Scalar.Blue, 2, cv.LineTypes.AntiAlias)
                    If k < klt.points.Length Then
                        klt.points(k) = klt.outputMat.At(Of cv.Point2f)(i)
                        k += 1
                    End If
                End If
            Next
            ReDim Preserve klt.points(k)
        End If
        lastpoints = New cv.Mat(klt.points.Length, 1, cv.MatType.CV_32SC2, klt.points).Clone()
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        klt.Dispose()
    End Sub
End Class