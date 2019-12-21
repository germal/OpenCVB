Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO
'https://docs.opencv.org/3.1.0/da/d22/tutorial_py_canny.html
Public Class Edges_Canny : Implements IDisposable
    Public sliders As New OptionsSliders
    Public src As cv.Mat
    Public dst As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Canny threshold1", 1, 255, 50)
        sliders.setupTrackBar2(ocvb, "Canny threshold2", 1, 255, 50)
        sliders.setupTrackBar3(ocvb, "Canny Aperture", 3, 7, 3)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Show canny edge detection with varying thresholds"
        ocvb.label1 = "Canny using L1 Norm"
        ocvb.label2 = "Canny using L2 Norm"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim threshold1 As Int32 = sliders.TrackBar1.Value
        Dim threshold2 As Int32 = sliders.TrackBar2.Value
        Dim aperture = sliders.TrackBar3.Value
        If aperture Mod 2 = 0 Then aperture += 1
        If externalUse = False Then
            Static useRegularSrc As Boolean = False
            If src Is Nothing Or useRegularSrc Then
                useRegularSrc = True
                src = ocvb.color.Clone
            End If
            Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst = gray.Canny(threshold1, threshold2, aperture, False)
            If useRegularSrc = True Then
                ocvb.result1 = dst.Clone()
                ocvb.result2 = gray.Canny(threshold1, threshold2, aperture, True)
            End If
        Else
            Dim gray = ocvb.color.Clone.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst = gray.Canny(threshold1, threshold2, aperture, False)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Edges_CannyAndShadow : Implements IDisposable
    Dim shadow As Depth_Shadow
    Dim canny As Edges_Canny
    Dim dilate As DilateErode_Basics
    Public Sub New(ocvb As AlgorithmData)
        dilate = New DilateErode_Basics(ocvb)
        dilate.radio.check(2).Checked = True
        dilate.externalUse = True

        canny = New Edges_Canny(ocvb)
        canny.sliders.TrackBar1.Value = 100
        canny.sliders.TrackBar2.Value = 100
        canny.externalUse = True

        shadow = New Depth_Shadow(ocvb)
        shadow.externalUse = True

        ocvb.desc = "Find all the edges in an image include Canny from the grayscale image and edges of depth shadow."
        ocvb.label1 = "Edges in color and depth after dilate"
        ocvb.label2 = "Edges in color and depth no dilate"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        canny.Run(ocvb)
        shadow.Run(ocvb)

        ocvb.result2.SetTo(0)
        ocvb.result2 = shadow.borderMask
        ocvb.result2 += canny.dst.Threshold(1, 255, cv.ThresholdTypes.Binary)

        dilate.src = ocvb.result2
        dilate.Run(ocvb)
        ocvb.result1.SetTo(0, shadow.holeMask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        canny.Dispose()
        shadow.Dispose()
        dilate.Dispose()
    End Sub
End Class


'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Edges_Laplacian : Implements IDisposable
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Gaussian Kernel", 1, 32, 7)
        sliders.setupTrackBar2(ocvb, "Laplacian Kernel", 1, 32, 5)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Show Laplacian edge detection with varying kernel sizes"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gaussiankernelSize As Int32 = sliders.TrackBar1.Value
        If gaussiankernelSize Mod 2 = 0 Then gaussiankernelSize -= 1 ' kernel size must be odd
        Dim laplaciankernelSize As Int32 = sliders.TrackBar2.Value
        If laplaciankernelSize Mod 2 = 0 Then laplaciankernelSize -= 1 ' kernel size must be odd
        Dim gray As New cv.Mat()
        Dim dst As New cv.Mat()
        Dim abs_dst As New cv.Mat()
        cv.Cv2.GaussianBlur(ocvb.color, ocvb.result1, New cv.Size(gaussiankernelSize, gaussiankernelSize), 0, 0)
        cv.Cv2.CvtColor(ocvb.result1, gray, cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.Laplacian(gray, dst, cv.MatType.CV_8U, laplaciankernelSize, 1, 0)
        cv.Cv2.ConvertScaleAbs(dst, abs_dst)
        cv.Cv2.CvtColor(abs_dst, ocvb.result1, cv.ColorConversionCodes.GRAY2BGR)

        cv.Cv2.GaussianBlur(ocvb.depthRGB, ocvb.result2, New cv.Size(gaussiankernelSize, gaussiankernelSize), 0, 0)
        cv.Cv2.CvtColor(ocvb.result2, gray, cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.Laplacian(gray, dst, cv.MatType.CV_8U, laplaciankernelSize, 1, 0)
        cv.Cv2.ConvertScaleAbs(dst, abs_dst)
        cv.Cv2.CvtColor(abs_dst, ocvb.result2, cv.ColorConversionCodes.GRAY2BGR)
        ocvb.label2 = "Laplacian of Depth Image"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edges_Scharr : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Scharr is more accurate with 3x3 kernel."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim xField = gray.Scharr(cv.MatType.CV_32FC1, 1, 0)
        Dim yField = gray.Scharr(cv.MatType.CV_32FC1, 0, 1)
        Dim xyField As New cv.Mat
        cv.Cv2.Add(xField, yField, xyField)
        xyField.ConvertTo(gray, cv.MatType.CV_8U, 0.5)
        ocvb.result1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class



' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class Edges_Preserving : Implements IDisposable
    Dim radio As New OptionsRadioButtons
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        radio.Setup(ocvb, 2)
        radio.check(0).Text = "Edge RecurseFilter"
        radio.check(1).Text = "Edge NormconvFilter"
        radio.check(0).Checked = True
        If ocvb.parms.ShowOptions Then radio.show()

        sliders.setupTrackBar1(ocvb, "Edge Sigma_s", 0, 200, 10)
        sliders.setupTrackBar2(ocvb, "Edge Sigma_r", 1, 100, 40)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "OpenCV's edge preserving filter."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim sigma_s = sliders.TrackBar1.Value
        Dim sigma_r = sliders.TrackBar2.Value / sliders.TrackBar2.Maximum
        If radio.check(0).Checked Then
            cv.Cv2.EdgePreservingFilter(ocvb.color, ocvb.result1, cv.EdgePreservingMethods.RecursFilter, sigma_s, sigma_r)
        Else
            cv.Cv2.EdgePreservingFilter(ocvb.color, ocvb.result1, cv.EdgePreservingMethods.NormconvFilter, sigma_s, sigma_r)
        End If
        If radio.check(0).Checked Then
            cv.Cv2.EdgePreservingFilter(ocvb.depthRGB, ocvb.result2, cv.EdgePreservingMethods.RecursFilter, sigma_s, sigma_r)
        Else
            cv.Cv2.EdgePreservingFilter(ocvb.depthRGB, ocvb.result2, cv.EdgePreservingMethods.NormconvFilter, sigma_s, sigma_r)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        radio.Dispose()
        sliders.Dispose()
    End Sub
End Class



Module Edges_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edges_RandomForest_Open(modelFileName As String) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Edges_RandomForest_Close(Edges_RandomForestPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edges_RandomForest_Run(Edges_RandomForestPtr As IntPtr, inputPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function
End Module



'  https://docs.opencv.org/3.1.0/d0/da5/tutorial_ximgproc_prediction.html
Public Class Edges_RandomForest_CPP : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim rgbData() As Byte
    Dim EdgesPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Edges RF Threshold", 1, 255, 35)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Detect edges using structured forests - Opencv Contrib"
        ocvb.label1 = "Detected Edges"

        ReDim rgbData(ocvb.color.Total * ocvb.color.ElemSize - 1)
        ocvb.label2 = "Thresholded Edge Mask (use slider to adjust)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.testAllRunning Then
            ocvb.putText(New ActiveClass.TrueType("When 'Test All' is running, the database load can take longer than the test time", 10, 100, RESULT2))
            ocvb.putText(New ActiveClass.TrueType("This is the only test that is not run during a 'Test All' run.", 10, 140, RESULT2))
            Exit Sub
        End If
        If ocvb.frameCount < 10 Then
            ocvb.putText(New ActiveClass.TrueType("On the first call only, it takes a few seconds to load the randomForest model.", 10, 100, RESULT2))
            ocvb.putText(New ActiveClass.TrueType("If running 'Test All' and the duration of each test < load time, it will finish loading before continuing to the next algorithm.", 10, 140, RESULT2))
        End If
        If ocvb.frameCount = 5 Then
            Dim modelInfo = New FileInfo(ocvb.parms.HomeDir + "Data/model.yml.gz")
            EdgesPtr = Edges_RandomForest_Open(modelInfo.FullName)
        End If
        If ocvb.frameCount > 5 Then ' the first images are skipped so the message above can be displayed.
            Marshal.Copy(ocvb.color.Data, rgbData, 0, rgbData.Length)
            Dim handleRGB = GCHandle.Alloc(rgbData, GCHandleType.Pinned)
            Dim gray8u = Edges_RandomForest_Run(EdgesPtr, handleRGB.AddrOfPinnedObject(), ocvb.color.Rows, ocvb.color.Cols)
            handleRGB.Free() ' free the pinned memory...

            Dim dstData(ocvb.color.Total - 1) As Byte
            Marshal.Copy(gray8u, dstData, 0, dstData.Length)
            ocvb.result1 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8U, dstData)
            ocvb.result1 = ocvb.result1.Threshold(sliders.TrackBar1.Value, 255, cv.ThresholdTypes.Binary)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        Edges_RandomForest_Close(EdgesPtr)
        sliders.Dispose()
    End Sub
End Class



'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edges_Sobel : Implements IDisposable
    Public sliders As New OptionsSliders
    Public src As cv.Mat
    Public grayX As cv.Mat
    Public grayY As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Sobel kernel Size", 1, 32, 3)
        If ocvb.parms.ShowOptions Then sliders.show()
        ocvb.desc = "Show Sobel edge detection with varying kernel sizes"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim kernelSize As Int32 = sliders.TrackBar1.Value
        If kernelSize Mod 2 = 0 Then kernelSize -= 1 ' kernel size must be odd
        Dim grad As New cv.Mat()
        If externalUse = False Then
            src = ocvb.color
        End If
        Dim gray As New cv.Mat
        If src.Channels = 3 Then gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY) Else gray = src.Clone()
        grayX = gray.Sobel(cv.MatType.CV_16U, 1, 0, kernelSize)
        Dim abs_grayX = grayX.ConvertScaleAbs()
        grayY = gray.Sobel(cv.MatType.CV_16U, 0, 1, kernelSize)
        Dim abs_grayY = grayY.ConvertScaleAbs()
        cv.Cv2.AddWeighted(abs_grayX, 0.5, abs_grayY, 0.5, 0, grad)
        ocvb.result1 = grad.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class



Public Class Edges_Infrared : Implements IDisposable
    Dim red As InfraRed_Basics
    Dim sobel As Edges_Sobel
    Public Sub New(ocvb As AlgorithmData)
        red = New InfraRed_Basics(ocvb)
        sobel = New Edges_Sobel(ocvb)
        sobel.externalUse = True
        sobel.sliders.TrackBar1.Value = 5

        ocvb.desc = "Find the edges in the infrared images."
        ocvb.label1 = "Edges in Left Infrared Image"
        ocvb.label2 = "Edges in Right Infrared Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        red.Run(ocvb)
        Dim leftView = ocvb.result1
        sobel.src = ocvb.result2
        sobel.Run(ocvb)
        ocvb.result2 = ocvb.result1

        sobel.src = leftView
        sobel.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        red.Dispose()
        sobel.Dispose()
    End Sub
End Class



Public Class Edges_ResizeAdd : Implements IDisposable
    Public sliders As New OptionsSliders
    Public gray As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Border Vertical in Pixels", 1, 20, 5)
        sliders.setupTrackBar2(ocvb, "Border Horizontal in Pixels", 1, 20, 5)
        sliders.setupTrackBar3(ocvb, "Threshold for Pixel Difference", 1, 50, 16)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Find edges using a resize, subtract, and threshold."
        ocvb.label1 = ""
        ocvb.label2 = ""
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim newFrame = gray(New cv.Range(sliders.TrackBar1.Value, gray.Rows - sliders.TrackBar1.Value), New cv.Range(sliders.TrackBar2.Value, gray.Cols - sliders.TrackBar2.Value))
        newFrame = newFrame.Resize(gray.Size())
        cv.Cv2.Absdiff(gray, newFrame, ocvb.result1)
        ocvb.result1 = ocvb.result1.Threshold(sliders.TrackBar3.Value, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.Add(gray, ocvb.result1, ocvb.result2)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Edges_DCTfrequency : Implements IDisposable
    Public sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Remove Frequencies < x", 0, 100, 32)
        sliders.setupTrackBar2(ocvb, "Threshold after Removal", 1, 255, 20)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Find edges by removing all the highest frequencies."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.depthRGB.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim frequencies As New cv.Mat
        Dim src32f As New cv.Mat
        gray.ConvertTo(src32f, cv.MatType.CV_32F, 1 / 255)
        cv.Cv2.Dct(src32f, frequencies, cv.DctFlags.None)

        Dim roi As New cv.Rect(0, 0, sliders.TrackBar1.Value, src32f.Height)
        If roi.Width > 0 Then frequencies(roi).SetTo(0)
        ocvb.label1 = "Highest " + CStr(sliders.TrackBar1.Value) + " frequencies removed from depthRGB"

        cv.Cv2.Dct(frequencies, src32f, cv.DctFlags.Inverse)
        src32f.ConvertTo(ocvb.result1, cv.MatType.CV_8UC1, 255)
        ocvb.result2 = ocvb.result1.Threshold(sliders.TrackBar2.Value, 255, cv.ThresholdTypes.Binary)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
    End Sub
End Class




Public Class Edges_InfraredDots : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim red As InfraRed_Basics
    Dim sobel As Edges_Sobel
    Public Sub New(ocvb As AlgorithmData)
        red = New InfraRed_Basics(ocvb)
        sobel = New Edges_Sobel(ocvb)
        sobel.externalUse = True
        sobel.sliders.TrackBar1.Value = 5

        sliders.setupTrackBar1(ocvb, "Threshold for sobel edges", 1, 255, 30)
        If ocvb.parms.ShowOptions Then sliders.show()

        ocvb.desc = "Find all the dots in the image - indicating they are within the desired range of the camera."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        red.Run(ocvb)
        sobel.src = ocvb.result1
        sobel.Run(ocvb)
        ocvb.result2 = ocvb.result1.Threshold(sliders.TrackBar1.Value, 255, cv.ThresholdTypes.Binary)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        red.Dispose()
        sobel.Dispose()
        sliders.Dispose()
    End Sub
End Class
