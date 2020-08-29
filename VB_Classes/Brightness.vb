Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Brightness_Clahe ' Contrast Limited Adaptive Histogram Equalization (CLAHE)
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Clip Limit", 1, 100, 10)
        sliders.setupTrackBar(1, "Grid Size", 1, 100, 8)
        setDescription(ocvb, "Show a Contrast Limited Adaptive Histogram Equalization image (CLAHE)")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src
        Dim claheObj = cv.Cv2.CreateCLAHE()
        claheObj.TilesGridSize() = New cv.Size(sliders.trackbar(0).Value, sliders.trackbar(1).Value)
        claheObj.ClipLimit = sliders.trackbar(0).Value
        claheObj.Apply(src, dst2)

        label1 = "GrayScale"
        label2 = "CLAHE Result"
    End Sub
End Class



Public Class Brightness_Hue
    Inherits ocvbClass
    Public hsv_planes(2) As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        setDescription(ocvb, "Show hue (Result1) and Saturation (Result2).")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim imghsv = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        cv.Cv2.CvtColor(src, imghsv, cv.ColorConversionCodes.RGB2HSV)
        cv.Cv2.Split(imghsv, hsv_planes)

        label1 = "Hue"
        label2 = "Saturation"
        cv.Cv2.CvtColor(hsv_planes(0), dst1, cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.CvtColor(hsv_planes(1), dst2, cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class



Public Class Brightness_AlphaBeta
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        setDescription(ocvb, "Use alpha and beta with ConvertScaleAbs.")
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Brightness Alpha (contrast)", 0, 500, 300)
        sliders.setupTrackBar(1, "Brightness Beta (brightness)", -100, 100, 0)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1 = src.ConvertScaleAbs(sliders.trackbar(0).Value / 500, sliders.trackbar(1).Value)
    End Sub
End Class




Public Class Brightness_Gamma
    Inherits ocvbClass
    Dim lookupTable(255) As Byte
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        setDescription(ocvb, "Use gamma with ConvertScaleAbs.")
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Brightness Gamma correction", 0, 200, 100)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static lastGamma As Int32 = -1
        If lastGamma <> sliders.trackbar(0).Value Then
            lastGamma = sliders.trackbar(0).Value
            For i = 0 To lookupTable.Length - 1
                lookupTable(i) = Math.Pow(i / 255, sliders.trackbar(0).Value / 100) * 255
            Next
        End If
        dst1 = src.LUT(lookupTable)
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
    Inherits ocvbClass
    Dim wPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "White balance threshold X100", 1, 100, 10)

        wPtr = WhiteBalance_Open()
        label1 = "Image with auto white balance"
        label2 = "White pixels were altered from the original"
        setDescription(ocvb, "Automate getting the right white balance")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rgbData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(rgbData, GCHandleType.Pinned) ' pin it for the duration...
        Marshal.Copy(src.Data, rgbData, 0, rgbData.Length)

        Dim thresholdVal As Single = sliders.trackbar(0).Value / 100
        Dim rgbPtr = WhiteBalance_Run(wPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, thresholdVal)
        handleSrc.Free()

        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, rgbPtr) ' no need to copy.  rgbPtr points to C++ data, not managed.
        Dim diff = dst1 - src
        diff = diff.ToMat().CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst2 = diff.ToMat().Threshold(1, 255, cv.ThresholdTypes.Binary)
    End Sub
    Public Sub Close()
        WhiteBalance_Close(wPtr)
    End Sub
End Class





' https://blog.csdn.net/just_sort/article/details/85982871
Public Class Brightness_WhiteBalance
    Inherits ocvbClass
    Dim hist As Histogram_Basics
    Dim wPtr As IntPtr
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        hist = New Histogram_Basics(ocvb)
        hist.bins = 256 * 3
        hist.maxRange = hist.bins
        If standalone = False Then hist.sliders.Visible = False

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "White balance threshold X100", 1, 100, 10)

        label1 = "Image with auto white balance"
        setDescription(ocvb, "Automate getting the right white balance - faster than the C++ version (in debug mode)")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim rgb32f As New cv.Mat
        src.ConvertTo(rgb32f, cv.MatType.CV_32FC3)
        Dim maxVal As Double, minVal As Double
        rgb32f.MinMaxLoc(minVal, maxVal)

        Dim planes() = rgb32f.Split()
        Dim sum32f = New cv.Mat(src.Size(), cv.MatType.CV_32F)
        sum32f = planes(0) + planes(1) + planes(2)
        hist.src = sum32f
        hist.Run(ocvb)

        Dim thresholdVal = sliders.trackbar(0).Value / 100
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
        rgb32f.ConvertTo(dst1, cv.MatType.CV_8UC3)
    End Sub
    Public Sub Close()
        WhiteBalance_Close(wPtr)
    End Sub
End Class






' https://blog.csdn.net/just_sort/article/details/85982871
Public Class Brightness_ChangeMask
    Inherits ocvbClass
    Dim white As Brightness_WhiteBalance
    Dim whiteCPP As Brightness_WhiteBalance_CPP
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        white = New Brightness_WhiteBalance(ocvb)
        If standalone = False Then white.sliders.Visible = False
        whiteCPP = New Brightness_WhiteBalance_CPP(ocvb)
        If standalone = False Then whiteCPP.sliders.Visible = False

        setDescription(ocvb, "Create a mask for the changed pixels after white balance")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static countdown = 60
        Static whiteFlag As Boolean
        If countdown = 0 Then
            countdown = 60
            whiteFlag = Not whiteFlag
        End If
        countdown -= 1

        If whiteFlag Then
            white.src = src
            white.Run(ocvb)
            dst1 = white.dst1
            label1 = "White balanced image - VB version"
            label2 = "Mask of changed pixels - VB version"
        Else
            whiteCPP.src = src
            whiteCPP.Run(ocvb)
            dst1 = whiteCPP.dst1
            label1 = "White balanced image - C++ version"
            label2 = "Mask of changed pixels - C++ version"
        End If
        Dim diff = dst1 - src
        dst2 = diff.ToMat().CvtColor(cv.ColorConversionCodes.BGR2GRAY).Threshold(1, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class





' https://blog.csdn.net/just_sort/article/details/85982871
Public Class Brightness_PlotHist
    Inherits ocvbClass
    Dim white As Brightness_ChangeMask
    Public hist1 As Histogram_KalmanSmoothed
    Public hist2 As Histogram_KalmanSmoothed
    Dim mat2to1 As Mat_2to1
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        white = New Brightness_ChangeMask(ocvb)

        hist1 = New Histogram_KalmanSmoothed(ocvb)
        hist1.sliders.Visible = False
        hist1.plotHist.sliders.Visible = False

        hist2 = New Histogram_KalmanSmoothed(ocvb)
        hist2.sliders.Visible = False
        hist2.plotHist.sliders.Visible = False

        mat2to1 = New Mat_2to1(ocvb)

        setDescription(ocvb, "Plot the histogram of the before and after white balancing")
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        hist1.src = src
        hist1.Run(ocvb)
        mat2to1.mat(0) = hist1.dst1

        white.src = src
        white.Run(ocvb)
        dst1 = white.dst1
        label1 = white.label1

        hist2.src = dst1
        hist2.Run(ocvb)
        mat2to1.mat(1) = hist2.dst1

        mat2to1.Run(ocvb)
        dst2 = mat2to1.dst1
        label2 = "The top is before white balance"
    End Sub
End Class