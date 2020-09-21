Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
Public Class Stabilizer_Basics
    Inherits VBparent
    Public good As Features_GoodFeatures
    Public inputFeat As New List(Of cv.Point2f)
    Public borderCrop = 30
    Dim sumScale As cv.Mat, sScale As cv.Mat, features1 As cv.Mat
    Dim errScale As cv.Mat, qScale As cv.Mat, rScale As cv.Mat
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        good = New Features_GoodFeatures(ocvb)

        desc = "Stabilize video with a Kalman filter.  Shake camera to see image edges appear.  This is not really working!"
        label1 = "Stabilized Image"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim vert_Border = borderCrop * src.Rows / src.Cols
        If ocvb.frameCount = 0 Then
            errScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 1)
            qScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.004)
            rScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.5)
            sumScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
            sScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
        End If

        dst1 = src

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If inputFeat Is Nothing Then
            good.src = src
            good.Run(ocvb)
            inputFeat = good.goodFeatures
        End If
        features1 = New cv.Mat(inputFeat.Count, 1, cv.MatType.CV_32FC2, inputFeat.ToArray)

        Static lastFrame As cv.Mat = src.Clone()
        If ocvb.frameCount > 0 Then
            Dim features2 = New cv.Mat
            Dim status As New cv.Mat
            Dim err As New cv.Mat
            Dim winSize As New cv.Size(3, 3)
            cv.Cv2.CalcOpticalFlowPyrLK(src, lastFrame, features1, features2, status, err, winSize, 3, term, cv.OpticalFlowFlags.None)
            lastFrame = src.Clone()

            Dim commonPoints = New List(Of cv.Point2f)
            Dim lastFeatures As New List(Of cv.Point2f)
            For i = 0 To status.Rows - 1
                If status.Get(Of Byte)(i, 0) Then
                    Dim pt1 = features1.Get(Of cv.Point2f)(i, 0)
                    Dim pt2 = features2.Get(Of cv.Point2f)(i, 0)
                    Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                    If length < 10 Then
                        commonPoints.Add(pt1)
                        lastFeatures.Add(pt2)
                    End If
                End If
            Next
            Dim affine = cv.Cv2.GetAffineTransform(commonPoints.ToArray, lastFeatures.ToArray)

            Dim dx = affine.Get(Of Double)(0, 2)
            Dim dy = affine.Get(Of Double)(1, 2)
            Dim da = Math.Atan2(affine.Get(Of Double)(1, 0), affine.Get(Of Double)(0, 0))
            Dim ds_x = affine.Get(Of Double)(0, 0) / Math.Cos(da)
            Dim ds_y = affine.Get(Of Double)(1, 1) / Math.Cos(da)
            Dim saveDX = dx, saveDY = dy, saveDA = da

            Dim text = "Original dx = " + Format(dx, "#0.00") + vbNewLine + " dy = " + Format(dy, "#0.00") + vbNewLine + " da = " + Format(da, "#0.00")
            ocvb.trueText(text)

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
            ocvb.trueText(text, 10, 100)

            Dim smoothedMat = New cv.Mat(2, 3, cv.MatType.CV_64F)
            smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 2, dx)
            smoothedMat.Set(Of Double)(1, 2, dy)

            Dim smoothedFrame = ocvb.color.WarpAffine(smoothedMat, src.Size())
            smoothedFrame = smoothedFrame(New cv.Range(vert_Border, smoothedFrame.Rows - vert_Border), New cv.Range(borderCrop, smoothedFrame.Cols - borderCrop))
            dst2 = smoothedFrame.Resize(src.Size())

            For i = 0 To commonPoints.Count - 1
                dst1.Circle(commonPoints.ElementAt(i), 5, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
                dst1.Circle(lastFeatures.ElementAt(i), 3, cv.Scalar.Blue, -1, cv.LineTypes.AntiAlias)
            Next
        End If
        inputFeat = Nothing ' show that we consumed the current set of features.
    End Sub
End Class






Public Class Stabilizer_BriskFeatures
    Inherits VBparent
    Dim brisk As BRISK_Basics
    Dim stabilizer As Stabilizer_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        stabilizer = New Stabilizer_Basics(ocvb)

        brisk = New BRISK_Basics(ocvb)
        brisk.sliders.trackbar(0).Value = 10

        desc = "Stabilize the video stream using BRISK features (not GoodFeaturesToTrack)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        src.CopyTo(brisk.src)
        brisk.Run(ocvb)
        stabilizer.inputFeat = brisk.features ' supply the features to track with Optical Flow
        stabilizer.src = src
        stabilizer.Run(ocvb)
        dst1 = stabilizer.dst1
        dst2 = stabilizer.dst2
    End Sub
End Class





Public Class Stabilizer_HarrisFeatures
    Inherits VBparent
    Dim harris As Harris_Detector_CPP
    Dim stabilizer As Stabilizer_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        stabilizer = New Stabilizer_Basics(ocvb)

        harris = New Harris_Detector_CPP(ocvb)

        desc = "Stabilize the video stream using Harris detector features"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        harris.src = src
        harris.Run(ocvb)
        stabilizer.inputFeat = harris.FeaturePoints ' supply the features to track with Optical Flow
        stabilizer.src = src
        stabilizer.Run(ocvb)
        dst1 = stabilizer.dst1
        dst2 = stabilizer.dst2
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
    Public Function Stabilizer_Basics_Run(sPtr As IntPtr, rgbPtr As IntPtr, rows As integer, cols As integer) As IntPtr
    End Function
End Module
Public Class Stabilizer_Basics_CPP
    Inherits VBparent
    Dim srcData() As Byte
    Dim handleSrc As GCHandle
    Dim sPtr As IntPtr
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        ReDim srcData(src.Total * src.ElemSize - 1)
        sPtr = Stabilizer_Basics_Open()
        desc = "Use the C++ version of code available on web.  This algorithm is not working.  Only small movements work.  Needs more work."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        ocvb.trueText("this algorithm is not stable.", 10, 100)
        'Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        'handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        'Dim imagePtr = Stabilizer_Basics_Run(sPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        'handleSrc.Free() ' free the pinned memory...

        'If imagePtr <> 0 Then
        '    Dim dstData(src.Total * src.ElemSize - 1) As Byte
        '    Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
        '    dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, dstData)
        'End If
    End Sub
    Public Sub Close()
        Stabilizer_Basics_Close(sPtr)
    End Sub
End Class






' https://github.com/Lakshya-Kejriwal/Real-Time-Video-Stabilization
Public Class Stabilizer_SideBySide
    Inherits VBparent
    Dim original As Stabilizer_Basics
    Dim basics As Stabilizer_HarrisFeatures
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        original = New Stabilizer_Basics(ocvb)
        basics = New Stabilizer_HarrisFeatures(ocvb)
        desc = "Run both the original and the VB.Net version of the video stabilizer.  Neither is working properly."
        label1 = "Stabilizer_Basic (VB.Net)"
        label2 = "Stabilizer_HarrisFeatures"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        original.src = src
        original.Run(ocvb)
        dst1 = original.dst1

        basics.src = src
        basics.Run(ocvb)
        dst2 = basics.dst1
    End Sub
End Class

