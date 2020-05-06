Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://docs.opencv.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_Harris
    Inherits VB_Class
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Corner block size", 1, 21, 3)
        sliders.setupTrackBar2(ocvb, callerName, "Corner aperture size", 1, 21, 3)
        sliders.setupTrackBar3(ocvb, callerName,"Corner quality level", 1, 100, 50)
                ocvb.desc = "Find corners using Eigen values and vectors"
        ocvb.label2 = "Corner Eigen values"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static color As New cv.Mat
        Static gray As New cv.Mat
        Static mc As New cv.Mat
        Static minval As Double, maxval As Double

        If ocvb.frameCount Mod 30 = 0 Then
            color = ocvb.color.Clone()
            gray = color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            mc = New cv.Mat(gray.Size(), cv.MatType.CV_32FC1, 0)
            Dim dst As New cv.Mat(gray.Size(), cv.MatType.CV_8U, 0)
            Dim blocksize = sliders.TrackBar1.Value
            If blocksize Mod 2 = 0 Then blocksize += 1
            Dim aperture = sliders.TrackBar2.Value
            If aperture Mod 2 = 0 Then aperture += 1
            cv.Cv2.CornerEigenValsAndVecs(gray, dst, blocksize, aperture, cv.BorderTypes.Default)

            For j = 0 To gray.Rows - 1
                For i = 0 To gray.Cols - 1
                    Dim lambda_1 = dst.Get(Of cv.Vec6f)(j, i)(0)
                    Dim lambda_2 = dst.Get(Of cv.Vec6f)(j, i)(1)
                    mc.Set(Of Single)(j, i, lambda_1 * lambda_2 - 0.04 * Math.Pow(lambda_1 + lambda_2, 2))
                Next
            Next

            mc.MinMaxLoc(minval, maxval)
        End If

        color.CopyTo(ocvb.result1)
        For j = 0 To gray.Rows - 1
            For i = 0 To gray.Cols - 1
                If mc.Get(Of Single)(j, i) > minval + (maxval - minval) * sliders.TrackBar3.Value / sliders.TrackBar3.Maximum Then
                    ocvb.result1.Circle(New cv.Point(i, j), 4, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                    ocvb.result1.Circle(New cv.Point(i, j), 2, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                End If
            Next
        Next

        Dim McNormal As New cv.Mat
        cv.Cv2.Normalize(mc, McNormal, 127, 255, cv.NormTypes.MinMax)
        McNormal.ConvertTo(ocvb.result2, cv.MatType.CV_8U)
    End Sub
    Public Sub MyDispose()
            End Sub
End Class




Public Class Corners_SubPix
    Inherits VB_Class
    Dim good As Features_GoodFeatures
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        good = New Features_GoodFeatures(ocvb, "Corners_SubPix")
        sliders.setupTrackBar1(ocvb, callerName, "SubPix kernel Size", 1, 20, 3)
        
        ocvb.desc = "Use PreCornerDetect to find features in the image."
        ocvb.label1 = "Output of GoodFeatures"
        ocvb.label2 = "Refined good features"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        good.Run(ocvb)
        If good.goodFeatures.Count = 0 Then Exit Sub ' no good features right now...
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim winSize = New cv.Size(sliders.TrackBar1.Value, sliders.TrackBar1.Value)
        cv.Cv2.CornerSubPix(gray, good.goodFeatures, winSize, New cv.Size(-1, -1), term)

        ocvb.color.CopyTo(ocvb.result2)
        Dim p As New cv.Point
        For i = 0 To good.goodFeatures.Count - 1
            p.X = CInt(good.goodFeatures(i).X)
            p.Y = CInt(good.goodFeatures(i).Y)
            cv.Cv2.Circle(ocvb.result2, p, 3, New cv.Scalar(0, 0, 255), -1, cv.LineTypes.AntiAlias)
        Next
    End Sub
    Public Sub MyDispose()
                good.Dispose()
    End Sub
End Class




Public Class Corners_PreCornerDetect
    Inherits VB_Class
        Dim median As Math_Median_CDF
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        median = New Math_Median_CDF(ocvb, "Corners_PreCornerDetect")
        sliders.setupTrackBar1(ocvb, callerName, "kernel Size", 1, 20, 19)
        
        ocvb.desc = "Use PreCornerDetect to find features in the image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim ksize = sliders.TrackBar1.Value
        If ksize Mod 2 = 0 Then ksize += 1
        Dim prob As New cv.Mat
        cv.Cv2.PreCornerDetect(gray, prob, ksize)

        cv.Cv2.Normalize(prob, prob, 0, 255, cv.NormTypes.MinMax)
        prob.ConvertTo(gray, cv.MatType.CV_8U)
        median.src = gray.Clone()
        median.Run(ocvb)
        ocvb.result1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.result2 = gray.Threshold(160, 255, cv.ThresholdTypes.BinaryInv).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.label2 = "median = " + CStr(median.medianVal)
    End Sub
    Public Sub MyDispose()
                median.Dispose()
    End Sub
End Class



Module corners_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Corners_ShiTomasi(grayPtr As IntPtr, dstPtr As IntPtr, rows As Int32, cols As Int32, blocksize As Int32, aperture As Int32)
    End Sub
End Module



' https://docs.opencv.org/2.4/doc/tutorials/features2d/trackingmotion/generic_corner_detector/generic_corner_detector.html
Public Class Corners_ShiTomasi_CPP
    Inherits VB_Class
        Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders.setupTrackBar1(ocvb, callerName, "Corner block size", 1, 21, 3)
        sliders.setupTrackBar2(ocvb, callerName, "Corner aperture size", 1, 21, 3)
        sliders.setupTrackBar3(ocvb, callerName,"Corner quality level", 1, 100, 50)
                ocvb.desc = "Find corners using Eigen values and vectors"
        ocvb.label2 = "Corner Eigen values"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static color As New cv.Mat
        Static minval As Double, maxval As Double
        Dim crows = ocvb.color.Rows, ccols = ocvb.color.Cols
        Dim data(crows * ccols) As Byte
        Static data32f(crows * ccols) As Single
        Static dst As New cv.Mat(crows, ccols, cv.MatType.CV_32FC1, data32f)

        If ocvb.frameCount Mod 30 = 0 Then
            Dim blocksize = sliders.TrackBar1.Value
            If blocksize Mod 2 = 0 Then blocksize += 1
            Dim aperture = sliders.TrackBar2.Value
            If aperture Mod 2 = 0 Then aperture += 1

            color = ocvb.color.Clone()
            Dim gray = color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Dim handle = GCHandle.Alloc(data, GCHandleType.Pinned)
            Dim handle32f = GCHandle.Alloc(data32f, GCHandleType.Pinned)
            Dim tmp As New cv.Mat(crows, ccols, cv.MatType.CV_8U, data)
            gray.CopyTo(tmp)
            Corners_ShiTomasi(tmp.Data, dst.Data, crows, ccols, blocksize, aperture)
            handle.Free()
            handle32f.Free()

            dst.MinMaxLoc(minval, maxval)
        End If

        color.CopyTo(ocvb.result1)
        For j = 0 To crows - 1
            For i = 0 To ccols - 1
                If dst.Get(of Single)(j, i) > minval + (maxval - minval) * sliders.TrackBar3.Value / sliders.TrackBar3.Maximum Then
                    ocvb.result1.Circle(New cv.Point(i, j), 4, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
                    ocvb.result1.Circle(New cv.Point(i, j), 2, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                End If
            Next
        Next

        Dim stNormal As New cv.Mat
        cv.Cv2.Normalize(dst, stNormal, 127, 255, cv.NormTypes.MinMax)
        stNormal.ConvertTo(ocvb.result2, cv.MatType.CV_8U)
    End Sub
    Public Sub MyDispose()
            End Sub
End Class