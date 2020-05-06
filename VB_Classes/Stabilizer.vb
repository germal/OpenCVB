Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Stabilizer_BriskFeatures
    Inherits VB_Class
    Dim brisk As BRISK_Basics
    Dim stabilizer As Stabilizer_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        stabilizer = New Stabilizer_Basics(ocvb, callerName)
        stabilizer.externalUse = True

        brisk = New BRISK_Basics(ocvb, callerName)
        brisk.externalUse = True
        brisk.sliders.TrackBar1.Value = 10

        ocvb.desc = "Stabilize the video stream using BRISK features (not GoodFeaturesToTrack)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.color.CopyTo(brisk.src)
        brisk.Run(ocvb)
        stabilizer.features = brisk.features ' supply the features to track with Optical Flow
        stabilizer.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        brisk.Dispose()
        stabilizer.Dispose()
    End Sub
End Class





Public Class Stabilizer_HarrisFeatures
    Inherits VB_Class
    Dim harris As Harris_Detector_CPP
    Dim stabilizer As Stabilizer_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        stabilizer = New Stabilizer_Basics(ocvb, callerName)
        stabilizer.externalUse = True

        harris = New Harris_Detector_CPP(ocvb, callerName)
        harris.externalUse = True

        ocvb.desc = "Stabilize the video stream using Harris detector features"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        harris.Run(ocvb)
        stabilizer.features = harris.FeaturePoints ' supply the features to track with Optical Flow
        stabilizer.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        harris.Dispose()
        stabilizer.Dispose()
    End Sub
End Class






' https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
Public Class Stabilizer_Basics
    Inherits VB_Class
    Public good As Features_GoodFeatures
    Public features As New List(Of cv.Point2f)
    Public lastFrame As cv.Mat
    Public externalUse As Boolean
    Public borderCrop = 30
    Dim sumScale As cv.Mat, sScale As cv.Mat, features1 As cv.Mat
    Dim errScale As cv.Mat, qScale As cv.Mat, rScale As cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        good = New Features_GoodFeatures(ocvb, callerName)
        good.externalUse = True

        ocvb.desc = "Stabilize video with a Kalman filter.  Shake camera to see image edges appear.  This is not really working!"
        ocvb.label1 = "Stabilized Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim vert_Border = borderCrop * ocvb.color.Rows / ocvb.color.Cols
        If ocvb.frameCount = 0 Then
            errScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 1)
            qScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.004)
            rScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.5)
            sumScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
            sScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
        End If
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If externalUse Then
            features1 = New cv.Mat(features.Count, 1, cv.MatType.CV_32FC2, features.ToArray)
        Else
            good.gray = gray.Clone()
            good.Run(ocvb)
            features = good.goodFeatures
        End If
        features1 = New cv.Mat(features.Count, 1, cv.MatType.CV_32FC2, features.ToArray)
        If lastFrame IsNot Nothing Then
            Dim features2 = New cv.Mat
            Dim status As New cv.Mat
            Dim err As New cv.Mat
            Dim winSize As New cv.Size(3, 3)
            cv.Cv2.CalcOpticalFlowPyrLK(gray, lastFrame, features1, features2, status, err, winSize, 3, term, cv.OpticalFlowFlags.None)
            features = New List(Of cv.Point2f)
            Dim lastFeatures As New List(Of cv.Point2f)
            For i = 0 To status.Rows - 1
                If status.Get(Of Byte)(i, 0) Then
                    Dim pt1 = features1.Get(Of cv.Point2f)(i, 0)
                    Dim pt2 = features2.Get(Of cv.Point2f)(i, 0)
                    Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                    If length < 10 Then
                        features.Add(pt1)
                        lastFeatures.Add(pt2)
                    End If
                End If
            Next
            Dim affine = cv.Cv2.GetAffineTransform(features.ToArray, lastFeatures.ToArray)

            Dim dx = affine.Get(Of Double)(0, 2)
            Dim dy = affine.Get(Of Double)(1, 2)
            Dim da = Math.Atan2(affine.Get(Of Double)(1, 0), affine.Get(Of Double)(0, 0))
            Dim ds_x = affine.Get(Of Double)(0, 0) / Math.Cos(da)
            Dim ds_y = affine.Get(Of Double)(1, 1) / Math.Cos(da)
            Dim saveDX = dx, saveDY = dy, saveDA = da

            Dim text = "Original dx = " + Format(dx, "#0.00") + vbNewLine + " dy = " + Format(dy, "#0.00") + vbNewLine + " da = " + Format(da, "#0.00")
            ocvb.putText(New ActiveClass.TrueType(text, 10, 50, RESULT1))

            Dim sx = ds_x, sy = ds_y

            Dim delta As New cv.Mat(5, 1, cv.MatType.CV_64F, New Double() {ds_x, ds_y, da, dx, dy})
            cv.Cv2.Add(sumScale, delta, sumScale)

            Dim diff As New cv.Mat
            cv.Cv2.Subtract(sScale, sumScale, diff)

            da += diff.Get(Of Double)(2, 0)
            dx += diff.Get(Of Double)(3, 0)
            dy += diff.Get(Of Double)(4, 0)
            If Math.Abs(dx) > 50 Then dx = saveDX
            If Math.Abs(dy) > 50 Then dy = saveDY
            If Math.Abs(da) > 50 Then da = saveDA

            text = "dx = " + Format(dx, "#0.00") + vbNewLine + " dy = " + Format(dy, "#0.00") + vbNewLine + " da = " + Format(da, "#0.00")
            ocvb.putText(New ActiveClass.TrueType(text, 10, 100, RESULT1))

            Dim smoothedMat = New cv.Mat(2, 3, cv.MatType.CV_64F)
            smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 2, dx)
            smoothedMat.Set(Of Double)(1, 2, dy)

            Dim smoothedFrame = ocvb.color.WarpAffine(smoothedMat, ocvb.color.Size())
            smoothedFrame = smoothedFrame(New cv.Range(vert_Border, smoothedFrame.Rows - vert_Border), New cv.Range(borderCrop, smoothedFrame.Cols - borderCrop))
            ocvb.result1 = smoothedFrame.Resize(ocvb.color.Size())

            For i = 0 To features.Count - 1
                ocvb.color.Circle(features.ElementAt(i), 3, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
            Next
        End If
        lastFrame = gray.Clone()
    End Sub
    Public Sub MyDispose()
        good.Dispose()
    End Sub
End Class






' https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
Module Stabilizer_Basics_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Stabilizer_Basics_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Stabilizer_Basics_Close(sPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Stabilizer_Basics_Run(sPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function
End Module
Public Class Stabilizer_Basics_CPP
    Inherits VB_Class
    Dim srcData() As Byte
    Dim handleSrc As GCHandle
    Dim sPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        ReDim srcData(ocvb.color.Total * ocvb.color.ElemSize - 1)
        sPtr = Stabilizer_Basics_Open()
        ocvb.desc = "Use the C++ version of code available on web.  This algorithm is not working.  Only small movements work."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.putText(New ActiveClass.TrueType("this algorithm is not stable.", 10, 100, RESULT1))
        'Marshal.Copy(ocvb.color.Data, srcData, 0, srcData.Length)
        'handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        'Dim imagePtr = Stabilizer_Basics_Run(sPtr, handleSrc.AddrOfPinnedObject(), ocvb.color.Rows, ocvb.color.Cols)
        'handleSrc.Free() ' free the pinned memory...

        'If imagePtr <> 0 Then
        '    Dim dstData(ocvb.color.Total * ocvb.color.ElemSize - 1) As Byte
        '    Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
        '    ocvb.result1 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8UC3, dstData)
        'End If
    End Sub
    Public Sub MyDispose()
        Stabilizer_Basics_Close(sPtr)
    End Sub
End Class






' https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
Public Class Stabilizer_SideBySide
    Inherits VB_Class
    Dim original As Stabilizer_Basics_CPP
    Dim basics As Stabilizer_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        original = New Stabilizer_Basics_CPP(ocvb, callerName)
        basics = New Stabilizer_Basics(ocvb, callerName)
        ocvb.desc = "Run both the original and the VB.Net version of the video stabilizer.  Neither is working properly."
        ocvb.label1 = "Stabilizer_Basic (VB.Net)"
        ocvb.label2 = "Stabilizer_Basic_CPP (C++)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        original.Run(ocvb)
        ocvb.result2 = ocvb.result1.Clone()
        basics.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        original.Dispose()
        basics.Dispose()
    End Sub
End Class
