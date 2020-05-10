Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module Harris_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Features_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Harris_Features_Close(Harris_FeaturesPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Features_Run(Harris_FeaturesPtr As IntPtr, inputPtr As IntPtr, rows As Int32, cols As Int32, threshold As Single,
                                        neighborhood As Int16, aperture As Int16, HarrisParm As Single) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Detector_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Harris_Detector_Close(Harris_FeaturesPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Harris_Detector_Run(Harris_FeaturesPtr As IntPtr, inputPtr As IntPtr, rows As Int32, cols As Int32, qualityLevel As Double,
                                        count As IntPtr) As IntPtr
    End Function
End Module



' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class Harris_Features_CPP
    Inherits ocvbClass
        Dim srcData() As Byte
    Dim Harris_Features As IntPtr
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Harris Threshold", 1, 100, 1)
        sliders.setupTrackBar2(ocvb, caller, "Harris Neighborhood", 1, 41, 21)
        sliders.setupTrackBar3(ocvb, caller,"Harris aperture", 1, 33, 21)
        sliders.setupTrackBar4(ocvb, caller,  "Harris Parameter", 1, 100, 1)
        
        ocvb.desc = "Use Harris feature detectors to identify interesting points."

        ReDim srcData(ocvb.color.Total - 1)
        Harris_Features = Harris_Features_Open()
        ocvb.label2 = "RGB overlaid with Harris result"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Marshal.Copy(gray.Data, srcData, 0, srcData.Length)
        Dim threshold = sliders.TrackBar1.Value / 10000
        Dim neighborhood = sliders.TrackBar2.Value
        If neighborhood Mod 2 = 0 Then neighborhood += 1
        Dim aperture = sliders.TrackBar3.Value
        If aperture Mod 2 = 0 Then aperture += 1
        Dim HarrisParm = sliders.TrackBar4.Value / 100
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = Harris_Features_Run(Harris_Features, handleSrc.AddrOfPinnedObject(), ocvb.color.Rows, ocvb.color.Cols, threshold,
                                           neighborhood, aperture, HarrisParm)

        handleSrc.Free() ' free the pinned memory...
        Dim dstData(ocvb.color.Total - 1) As Single
        Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
        Dim gray32f = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_32F, dstData)
        gray32f.ConvertTo(ocvb.result1, cv.MatType.CV_8U)
        ocvb.result1 = ocvb.result1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.AddWeighted(ocvb.result1, 0.5, ocvb.color, 0.5, 0, ocvb.result2)
    End Sub
    Public Sub Close()
        Harris_Features_Close(Harris_Features)
    End Sub
End Class




' https://github.com/PacktPublishing/OpenCV3-Computer-Vision-Application-Programming-Cookbook-Third-Edition/blob/master/Chapter08/harrisDetector.h
Public Class Harris_Detector_CPP
    Inherits ocvbClass
        Dim srcData() As Byte
    Dim ptCount(1) As Int32
    Dim Harris_Detector As IntPtr
    Public FeaturePoints As New List(Of cv.Point2f)
        Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Harris qualityLevel", 1, 100, 2)
        
        ocvb.desc = "Use Harris detector to identify interesting points."

        ReDim srcData(ocvb.color.Total - 1)
        Harris_Detector = Harris_Detector_Open()
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Marshal.Copy(gray.Data, srcData, 0, srcData.Length)
        Dim qualityLevel = sliders.TrackBar1.Value / 100

        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned) 
        Dim handleCount = GCHandle.Alloc(ptCount, GCHandleType.Pinned) 
        Dim ptPtr = Harris_Detector_Run(Harris_Detector, handleSrc.AddrOfPinnedObject(), ocvb.color.Rows, ocvb.color.Cols, qualityLevel, handleCount.AddrOfPinnedObject())
        handleSrc.Free()
        handleCount.Free()
        If ptCount(0) > 1 And ptPtr <> 0 Then
            Dim pts((ptCount(0) - 1) * 2 - 1) As Int32
            Marshal.Copy(ptPtr, pts, 0, ptCount(0))
            Dim ptMat = New cv.Mat(ptCount(0), 2, cv.MatType.CV_32S, pts)
            if standalone Then ocvb.color.CopyTo(ocvb.result1)
            FeaturePoints.Clear()
            For i = 0 To ptMat.Rows - 1
                FeaturePoints.Add(New cv.Point2f(ptMat.Get(of Int32)(i, 0), ptMat.Get(of Int32)(i, 1)))
                if standalone Then ocvb.result1.Circle(FeaturePoints(i), 3, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
    Public Sub Close()
        Harris_Detector_Close(Harris_Detector)
    End Sub
End Class

