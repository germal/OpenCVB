Imports cv = OpenCvSharp
Imports OpenCvSharp.XPhoto
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Public Class xPhoto_Bm3dDenoise
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Denoise image with block matching and filtering."
        label1 = "Bm3dDenoising"
        label2 = "Difference from Input"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.EqualizeHist(src, src)
        CvXPhoto.Bm3dDenoising(src, dst1)
        cv.Cv2.Subtract(dst1, src, dst2)
        Dim minVal As Double, maxVal As Double
        dst2.MinMaxLoc(minVal, maxVal)
        label2 = "Diff from input - max change=" + CStr(maxVal)
        dst2 = dst2.Normalize(0, 255, cv.NormTypes.MinMax)
    End Sub
End Class





Public Class xPhoto_Bm3dDenoiseDepthImage
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Denoise the depth image with block matching and filtering."
        label2 = "Difference from Input"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        cv.Cv2.EqualizeHist(gray, gray)
        CvXPhoto.Bm3dDenoising(gray, dst1)
        cv.Cv2.Subtract(dst1, gray, dst2)
        Dim minVal As Double, maxVal As Double
        dst2.MinMaxLoc(minVal, maxVal)
        label2 = "Diff from input - max change=" + CStr(maxVal)
        dst2 = dst2.Normalize(0, 255, cv.NormTypes.MinMax)
    End Sub
End Class




Module xPhoto_OilPaint_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_OilPaint_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub xPhoto_OilPaint_Close(xPhoto_OilPaint_Ptr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function xPhoto_OilPaint_Run(xPhoto_OilPaint_Ptr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32,
                                       size As Int32, dynRatio As Int32, colorCode As Int32) As IntPtr
    End Function
End Module



' https://github.com/opencv/opencv_contrib/blob/master/modules/xphoto/samples/oil.cpp
Public Class xPhoto_OilPaint_CPP
    Inherits ocvbClass
    Dim xPhoto_OilPaint As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.setupTrackBar1(ocvb, caller, "xPhoto Dynamic Ratio", 1, 127, 7)
        sliders.setupTrackBar2("xPhoto Block Size", 1, 100, 3)

        radio.Setup(ocvb, caller, 5)
        radio.check(0).Text = "BGR2GRAY"
        radio.check(1).Text = "BGR2HSV"
        radio.check(2).Text = "BGR2YUV  "
        radio.check(3).Text = "BGR2XYZ"
        radio.check(4).Text = "BGR2Lab"
        radio.check(0).Checked = True

        Application.DoEvents() ' because the rest of initialization takes so long, let the show() above take effect.
        xPhoto_OilPaint = xPhoto_OilPaint_Open()
        ocvb.desc = "Use the xPhoto Oil Painting transform - Painterly Effect"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim colorCode As Int32 = cv.ColorConversionCodes.BGR2GRAY
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                colorCode = Choose(i + 1, cv.ColorConversionCodes.BGR2GRAY, cv.ColorConversionCodes.BGR2HSV, cv.ColorConversionCodes.BGR2YUV,
                                   cv.ColorConversionCodes.BGR2XYZ, cv.ColorConversionCodes.BGR2Lab)
                Exit For
            End If
        Next

        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = xPhoto_OilPaint_Run(xPhoto_OilPaint, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                           sliders.TrackBar2.Value, sliders.TrackBar1.Value, colorCode)
        handleSrc.Free()

        If imagePtr <> 0 Then dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, imagePtr)
    End Sub
    Public Sub Close()
        xPhoto_OilPaint_Close(xPhoto_OilPaint)
    End Sub
End Class

