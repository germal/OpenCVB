Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Brightness_Clahe ' Contrast Limited Adaptive Histogram Equalization (CLAHE)
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        sliders.setupTrackBar1(ocvb, callerName, "Clip Limit", 1, 100, 10)
        sliders.setupTrackBar2(ocvb, callerName, "Grid Size", 1, 100, 8)
        ocvb.desc = "Show a Contrast Limited Adaptive Histogram Equalization image (CLAHE)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim imgGray = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
        Dim imgClahe = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC1)
        cv.Cv2.CvtColor(ocvb.color, imgGray, cv.ColorConversionCodes.BGR2GRAY)

        Dim claheObj = cv.Cv2.CreateCLAHE()
        ' claheObj.SetTilesGridSize(New cv.Size(sliders.TrackBar1.Value, sliders.TrackBar2.Value))
        ' claheObj.SetClipLimit(sliders.TrackBar1.Value)
        claheObj.TilesGridSize() = New cv.Size(sliders.TrackBar1.Value, sliders.TrackBar2.Value)
        claheObj.ClipLimit = sliders.TrackBar1.Value
        claheObj.Apply(imgGray, imgClahe)

        ocvb.label1 = "GrayScale"
        ocvb.label2 = "CLAHE Result"
        cv.Cv2.CvtColor(imgGray, ocvb.result1, cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.CvtColor(imgClahe, ocvb.result2, cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class



Public Class Brightness_Contrast
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        sliders.setupTrackBar1(ocvb, callerName, "Brightness", 1, 100, 50)
        sliders.setupTrackBar2(ocvb, callerName, "Contrast", 1, 100, 50)
        ocvb.desc = "Show image with vary contrast and brightness."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.color.ConvertTo(ocvb.result1, -1, sliders.TrackBar2.Value / 50, sliders.TrackBar1.Value)
        ocvb.label1 = "Brightness/Contrast"
        ocvb.label2 = ""
    End Sub
End Class



Public Class Brightness_hue
    Inherits VB_Class
    Public hsv_planes(2) As cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        ocvb.desc = "Show hue (Result1) and Saturation (Result2)."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim imghsv = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC3)
        cv.Cv2.CvtColor(ocvb.color, imghsv, cv.ColorConversionCodes.RGB2HSV)
        cv.Cv2.Split(imghsv, hsv_planes)

        ocvb.label1 = "Hue"
        ocvb.label2 = "Saturation"
        cv.Cv2.CvtColor(hsv_planes(0), ocvb.result1, cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.CvtColor(hsv_planes(1), ocvb.result2, cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class



Public Class Brightness_AlphaBeta
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        ocvb.desc = "Use alpha and beta with ConvertScaleAbs."
        sliders.setupTrackBar1(ocvb, callerName, "Brightness Alpha (contrast)", 0, 500, 300)
        sliders.setupTrackBar2(ocvb, callerName, "Brightness Beta (brightness)", -100, 100, 0)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = ocvb.color.ConvertScaleAbs(sliders.TrackBar1.Value / 500, sliders.TrackBar2.Value)
    End Sub
End Class




Public Class Brightness_Gamma
    Inherits VB_Class
    Dim lookupTable(255) As Byte
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        ocvb.desc = "Use gamma with ConvertScaleAbs."
        sliders.setupTrackBar1(ocvb, callerName, "Brightness Gamma correction", 0, 200, 100)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static lastGamma As Int32 = -1
        If lastGamma <> sliders.TrackBar1.Value Then
            lastGamma = sliders.TrackBar1.Value
            For i = 0 To lookupTable.Length - 1
                lookupTable(i) = Math.Pow(i / 255, sliders.TrackBar1.Value / 100) * 255
            Next
        End If
        ocvb.result1 = ocvb.color.LUT(lookupTable)
    End Sub
End Class




Module Brightness_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WhiteBalance_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub WhiteBalance_Close(wPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WhiteBalance_Run(wPtr As IntPtr, rgb As IntPtr, rows As Int32, cols As Int32, thresholdVal As Single) As IntPtr
    End Function
End Module





' https://blog.csdn.net/just_sort/article/details/85982871
Public Class Brightness_WhiteBalance_CPP
    Inherits VB_Class
    Dim wPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        sliders.setupTrackBar1(ocvb, callerName, "White balance threshold X100", 1, 100, 10)

        wPtr = WhiteBalance_Open()
        ocvb.label1 = "Image with auto white balance"
        ocvb.label2 = "White pixels were altered from the original"
        ocvb.desc = "Automate getting the right white balance"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rgbData(ocvb.color.Total * ocvb.color.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(rgbData, GCHandleType.Pinned) ' pin it for the duration...
        Marshal.Copy(ocvb.color.Data, rgbData, 0, rgbData.Length)

        Dim thresholdVal As Single = sliders.TrackBar1.Value / 100
        Dim rgbPtr = WhiteBalance_Run(wPtr, handleSrc.AddrOfPinnedObject(), ocvb.color.Rows, ocvb.color.Cols, thresholdVal)
        handleSrc.Free()

        ocvb.result1 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8UC3, rgbPtr) ' no need to copy.  rgbPtr points to C++ data, not managed.
        Dim diff = ocvb.result1 - ocvb.color
        diff = diff.ToMat().CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.result2 = diff.ToMat().Threshold(1, 255, cv.ThresholdTypes.Binary)
    End Sub
    Public Sub MyDispose()
        WhiteBalance_Close(wPtr)
    End Sub
End Class





' https://blog.csdn.net/just_sort/article/details/85982871
Public Class Brightness_WhiteBalance
    Inherits VB_Class
    Dim hist As Histogram_Basics
    Dim wPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        setCaller(caller)
        hist = New Histogram_Basics(ocvb, callerName)
        hist.bins = 256 * 3
        hist.maxRange = hist.bins
        hist.externalUse = True

        sliders.setupTrackBar1(ocvb, callerName, "White balance threshold X100", 1, 100, 10)

        ocvb.label1 = "Image with auto white balance"
        ocvb.label2 = "White pixels were altered from the original"
        ocvb.desc = "Automate getting the right white balance - faster than the C++ version (in debug mode)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rgb32f As New cv.Mat
        ocvb.color.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim maxInput = New cv.Mat(rgb32f.Rows, rgb32f.Cols * 3, cv.MatType.CV_32F, rgb32f.Data)
        Dim maxVal As Double, minVal As Double
        maxInput.MinMaxLoc(minVal, maxVal)

        Dim planes() = rgb32f.Split()
        Dim sum32f = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32F)
        sum32f = planes(0) + planes(1) + planes(2)
        hist.src = sum32f
        hist.Run(ocvb)

        Dim thresholdVal = sliders.TrackBar1.Value / 100
        Dim sum As Single
        Dim threshold As Int32
        For i = hist.histRaw(0).Rows - 1 To 0 Step -1
            sum += hist.histRaw(0).Get(Of Single)(i, 0)
            If sum > hist.src.Rows * hist.src.Cols * thresholdVal Then
                threshold = i
                Exit For
            End If
        Next

        Dim mask = sum32f.Threshold(threshold, 255, cv.ThresholdTypes.Binary).ConvertScaleAbs(1)

        Dim mean = rgb32f.Mean(mask)
        For i = 0 To rgb32f.Channels - 1
            planes(i) *= maxVal / mean.Item(i)
            planes(i) = planes(i).Threshold(255, 255, cv.ThresholdTypes.Trunc)
        Next

        cv.Cv2.Merge(planes, rgb32f)
        rgb32f.ConvertTo(ocvb.result1, cv.MatType.CV_8UC3)

        Dim diff = ocvb.result1 - ocvb.color
        diff = diff.ToMat().CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.result2 = diff.ToMat().Threshold(1, 255, cv.ThresholdTypes.Binary)
    End Sub
    Public Sub MyDispose()
        WhiteBalance_Close(wPtr)
        hist.Dispose()
    End Sub
End Class