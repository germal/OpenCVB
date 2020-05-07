Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/lkdemo.cpp
Public Class KLT_Basics
    Inherits ocvbClass
    Public gray As New cv.Mat
    Public prevGray As New cv.Mat
    Public inputPoints() As cv.Point2f
    Public status As New cv.Mat
    Public externalUse As Boolean
    Public outputMat As New cv.Mat
    Public circleColor = cv.Scalar.Red
    Dim term As New cv.TermCriteria(cv.CriteriaType.Eps + cv.CriteriaType.Count, 10, 1.0)
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "KLT - MaxCorners", 1, 200, 100)
        sliders.setupTrackBar2(ocvb, caller, "KLT - qualityLevel", 1, 100, 1) ' low quality!  We want lots of points.
        sliders.setupTrackBar3(ocvb, caller, "KLT - minDistance", 1, 100, 7)
        sliders.setupTrackBar4(ocvb, caller, "KLT - BlockSize", 1, 100, 7)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "KLT - Night Mode"
        check.Box(1).Text = "KLT - delete all Points"

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

        gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If ocvb.frameCount = 0 Or inputPoints Is Nothing Then
            ocvb.result2.SetTo(0)
            inputPoints = cv.Cv2.GoodFeaturesToTrack(gray, maxCorners, qualityLevel, minDistance, New cv.Mat, blockSize, False, 0)
            If inputPoints.Length > 0 Then
                inputPoints = cv.Cv2.CornerSubPix(gray, inputPoints, subPixWinSize, New cv.Size(-1, -1), term)
            End If
        ElseIf inputPoints.Length > 0 Then
            Dim err As New cv.Mat
            ' convert the point2f vector to an inputarray (cv.Mat)
            Dim inputMat = New cv.Mat(inputPoints.Length, 1, cv.MatType.CV_32FC2, inputPoints)
            outputMat = inputMat.Clone()
            cv.Cv2.CalcOpticalFlowPyrLK(prevGray, gray, inputMat, outputMat, status, err, winSize, 3, term, cv.OpticalFlowFlags.None)

            For i = 0 To outputMat.Rows - 1
                Dim pt = outputMat.Get(Of cv.Point2f)(i)
                If pt.X >= 0 And pt.X <= ocvb.color.Cols And pt.Y >= 0 And pt.Y <= ocvb.color.Rows Then
                    If status.Get(Of Byte)(i) Then
                        ocvb.result1.Circle(pt, 3, circleColor, -1, cv.LineTypes.AntiAlias)
                    End If
                Else
                    status.Set(Of Byte)(i, 0) ' this point is not visible!
                End If
            Next

            Dim k As Int32
            For i = 0 To inputPoints.Length - 1
                If status.Get(Of Byte)(i) Then
                    inputPoints(k) = outputMat.Get(Of cv.Point2f)(i)
                    k += 1
                End If
            Next
            ReDim Preserve inputPoints(k - 1)
        End If

        If check.Box(1).Checked Or ocvb.frameCount Mod 25 = 0 Then
            inputPoints = Nothing ' just delete all points and start again.
            check.Box(1).Checked = False
        End If
        prevGray = gray.Clone()
        ocvb.label1 = "KLT Basics - " + If(inputPoints Is Nothing, "0", CStr(inputPoints.Length)) + " points"
    End Sub
End Class



' https://github.com/opencv/opencv/blob/master/samples/python/lk_track.py
Public Class KLT_OpticalFlow
    Inherits ocvbClass
    Dim klt As KLT_Basics
    Dim lastpoints() As cv.Point2f
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        klt = New KLT_Basics(ocvb, caller)
        klt.externalUse = True ' we will compress the points file below.
        ocvb.desc = "KLT optical flow"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        klt.Run(ocvb)
        If ocvb.frameCount > 0 And lastpoints IsNot Nothing And klt.inputPoints IsNot Nothing Then
            For i = 0 To klt.inputPoints.Length - 1
                If klt.status.Get(Of Byte)(i) And i < lastpoints.Length And i < klt.inputPoints.Length Then
                    ocvb.result1.Line(lastpoints(i), klt.inputPoints(i), cv.Scalar.Yellow, 2, cv.LineTypes.AntiAlias)
                    ocvb.result2.Line(lastpoints(i), klt.inputPoints(i), cv.Scalar.Yellow, 2, cv.LineTypes.AntiAlias)
                End If
            Next
        End If
        lastpoints = klt.inputPoints
    End Sub
    Public Sub MyDispose()
        klt.Dispose()
    End Sub
End Class
