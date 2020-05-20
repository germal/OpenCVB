Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module Salience_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Salience_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Salience_Run(classPtr As IntPtr, numScales As Int32, grayInput As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Salience_Close(classPtr As IntPtr)
    End Sub
End Module




Public Class Salience_Basics_CPP
    Inherits ocvbClass
    Dim grayData(0) As Byte
    Dim numScales As Int32
    Dim salience As IntPtr
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders = New OptionsSliders
        sliders.setupTrackBar1(ocvb, caller, "Salience numScales", 1, 6, 6)

        salience = Salience_Open()
        ocvb.desc = "Show results of Salience algorithm when using C++"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Total <> grayData.Length Then ReDim grayData(src.Total - 1)
        Dim grayHandle = GCHandle.Alloc(grayData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, grayData, 0, src.Height * src.Width)
        Dim imagePtr = Salience_Run(salience, sliders.TrackBar1.Value, grayHandle.AddrOfPinnedObject, src.Height, src.Width)
        grayHandle.Free()

        dst1 = New cv.Mat(colorRows, colorCols, cv.MatType.CV_8U, imagePtr)
    End Sub
    Public Sub Close()
        Salience_Close(salience)
    End Sub
End Class



'Public Class Salience_Basics_MT
'    Inherits ocvbClass
'    Dim grayData(0) As Byte
'    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
'        setCaller(callerRaw)
'        sliders = New OptionsSliders
'        sliders.setupTrackBar1(ocvb, caller, "Salience numScales", 1, 6, 1)
'        sliders.setupTrackBar2(ocvb, caller, "Salience Number of Threads", 1, 100, 36)

'        ocvb.desc = "Show results of multi-threaded Salience algorithm when using C++.  NOTE: salience is relative."
'    End Sub
'    Public Sub Run(ocvb As AlgorithmData)
'        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        If src.Total <> grayData.Length Then ReDim grayData(src.Total - 1)
'        Dim numScales = sliders.TrackBar1.Value
'        Dim threads = sliders.TrackBar2.Value
'        Dim h = CInt(src.Height / threads)
'        Dim grayHandle = GCHandle.Alloc(grayData, GCHandleType.Pinned)

'        Parallel.For(0, threads,
'            Sub(i)
'                Dim roi = New cv.Rect(0, i * h, src.Width, Math.Min(h, src.Height - i * h))
'                If roi.Height <= 0 Then Exit Sub
'                Dim grayInput = src(roi).CvtColor(cv.ColorConversionCodes.BGR2GRAY)

'                Dim salience = Salience_Open()
'                Dim imagePtr = Salience_Run(salience, numScales, gray(roi).Data, roi.Height, roi.Width)

'                Dim dstData(roi.Width * roi.Height - 1) As Byte
'                Dim dst1 As New cv.Mat(roi.Height, roi.Width, cv.MatType.CV_8U, dstData)
'                Marshal.Copy(imagePtr, dstData, 0, roi.Height * roi.Width)

'                dst1(roi) = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
'                Salience_Close(salience)
'            End Sub)
'        grayHandle.Free()
'    End Sub
'End Class

