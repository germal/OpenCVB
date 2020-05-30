Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://docs.opencv.org/3.0-beta/modules/ml/doc/expectation_maximization.html
' https://github.com/opencv/opencv/blob/master/samples/cpp/em.cpp
Public Class EMax_Basics
    Inherits ocvbClass
    Public samples As cv.Mat
    Public labels As cv.Mat
    Public grid As Thread_Grid
    Public regionCount As Int32
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        grid = New Thread_Grid(ocvb, caller)

        grid.sliders.TrackBar1.Value = ocvb.color.Width / 2
        grid.sliders.TrackBar2.Value = ocvb.color.Height / 2

        sliders.setupTrackBar1(ocvb, caller, "EMax Number of Samples", 1, 200, 100)
        sliders.setupTrackBar2(ocvb, caller, "EMax Prediction Step Size", 1, 20, 5)
        sliders.setupTrackBar3(ocvb, caller, "EMax Sigma (spread)", 1, 100, 30)

        radio.Setup(ocvb, caller, 3)
        radio.check(0).Text = "EMax matrix type Spherical"
        radio.check(1).Text = "EMax matrix type Diagonal"
        radio.check(2).Text = "EMax matrix type Generic"
        radio.check(0).Checked = True

        ocvb.desc = "OpenCV expectation maximization example."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        regionCount = grid.roiList.Count - 1

        If standalone Then
            ocvb.putText(New oTrueType("The EMax ocvbClass fails as a result of a bug in OpenCVSharp.  See code for details." + vbCrLf +
                                                  "The EMax_Basics_CPP works fine and they are functionally identical.", 20, 100, RESULT2))
        End If

        samples = New cv.Mat(sliders.TrackBar1.Value, 2, cv.MatType.CV_32FC1, 0)
        labels = New cv.Mat(sliders.TrackBar1.Value, 1, cv.MatType.CV_32S, 0)
        samples = samples.Reshape(2, 0)
        Dim sigma = sliders.TrackBar3.Value
        For i = 0 To regionCount - 1
            Dim samples_part = samples.RowRange(i * samples.Rows / regionCount, (i + 1) * samples.Rows / regionCount)
            labels.RowRange(i * samples.Rows / regionCount, (i + 1) * samples.Rows / regionCount).SetTo(i)
            Dim x = grid.roiList(i).X + grid.roiList(i).Width / 2
            Dim y = grid.roiList(i).Y + grid.roiList(i).Height / 2
            cv.Cv2.Randn(samples_part, New cv.Scalar(x, y), cv.Scalar.All(sigma))
        Next

        samples = samples.Reshape(1, 0)

        dst1.SetTo(cv.Scalar.Black)
        If standalone Then
            Dim em_model = cv.EM.Create()
            em_model.ClustersNumber = regionCount
            For i = 0 To radio.check.Count - 1
                If radio.check(i).Checked Then
                    em_model.CovarianceMatrixType = Choose(i + 1, cv.EM.Types.CovMatSpherical, cv.EM.Types.CovMatDiagonal, cv.EM.Types.CovMatGeneric)
                End If
            Next
            em_model.TermCriteria = New cv.TermCriteria(cv.CriteriaType.Eps + cv.CriteriaType.Count, 300, 1.0)
            em_model.TrainEM(samples, Nothing, labels, Nothing)

            ' now classify every image pixel based on the samples.
            Dim sample As New cv.Mat(1, 2, cv.MatType.CV_64F, 0)
            For i = 0 To dst1.Rows - 1
                For j = 0 To dst1.Cols - 1
                    sample.Set(Of Double)(0, 0, CSng(j))
                    sample.Set(Of Double)(0, 1, CSng(i))
                    ' remove the " 0 '" to see the error in Predict2.
                    Dim response = 0 ' Math.Round(em_model.Predict2(sample)(1))
                    Dim c = rColors(response)
                    dst1.Circle(New cv.Point(j, i), 1, c, -1)
                Next
            Next
        End If

        ' draw the clustered samples
        For i = 0 To samples.Rows - 1
            Dim pt = New cv.Point(Math.Round(samples.Get(Of Single)(i, 0)), Math.Round(samples.Get(Of Single)(i, 1)))
            dst1.Circle(pt, 4, rColors(labels.Get(Of Int32)(i) + 1), -1, cv.LineTypes.AntiAlias) ' skip the first rColor - it might be used above.
        Next
    End Sub
End Class





Module EMax_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function EMax_Basics_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub EMax_Basics_Close(EMax_BasicsPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function EMax_Basics_Run(EMax_BasicsPtr As IntPtr, samplesPtr As IntPtr, labelsPtr As IntPtr, rows As Int32, cols As Int32,
                                    imgRows As Int32, imgCols As Int32, clusters As Int32, stepSize As Int32, covarianceMatrixType As Int32) As IntPtr
    End Function
End Module

Public Class EMax_Basics_CPP
    Inherits ocvbClass
    Dim emax As EMax_Basics
    Dim EMax_Basics As IntPtr
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        emax = New EMax_Basics(ocvb, caller)

        EMax_Basics = EMax_Basics_Open()
        label2 = "Emax regions around clusters"
        ocvb.desc = "Use EMax - Expectation Maximization - to classify a series of points"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        emax.Run(ocvb)
        dst1 = emax.dst1
        label1 = CStr(emax.sliders.TrackBar1.Value) + " Random samples in " + CStr(emax.regionCount) + " clusters"

        Dim covarianceMatrixType As Int32 = 0
        For i = 0 To 3 - 1
            If emax.radio.check(i).Checked = True Then
                covarianceMatrixType = Choose(i + 1, cv.EM.Types.CovMatSpherical, cv.EM.Types.CovMatDiagonal, cv.EM.Types.CovMatGeneric)
            End If
        Next

        Dim srcData((emax.sliders.TrackBar1.Value - 1) * 2 - 1) As Single
        Dim handleSrc As GCHandle
        handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Marshal.Copy(emax.samples.Data, srcData, 0, srcData.Length)

        Dim labelData(emax.sliders.TrackBar1.Value - 1) As Int32
        Dim handleLabels As GCHandle
        handleLabels = GCHandle.Alloc(labelData, GCHandleType.Pinned)
        Marshal.Copy(emax.labels.Data, labelData, 0, labelData.Length)

        Dim imagePtr = EMax_Basics_Run(EMax_Basics, handleSrc.AddrOfPinnedObject(), handleLabels.AddrOfPinnedObject(), emax.samples.Rows, emax.samples.Cols,
                                       dst1.Rows, dst1.Cols, emax.regionCount, emax.sliders.TrackBar2.Value, covarianceMatrixType)
        handleLabels.Free() ' free the pinned memory...
        handleSrc.Free() ' free the pinned memory...

        If imagePtr <> 0 Then dst2 = New cv.Mat(dst2.Rows, dst2.Cols, cv.MatType.CV_8UC3, imagePtr)

        Dim mask = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst1.CopyTo(dst2, mask)
    End Sub
    Public Sub Close()
        EMax_Basics_Close(EMax_Basics)
    End Sub
End Class

