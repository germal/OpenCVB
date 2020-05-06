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
    Inherits VB_Class
    Dim grayData() As Byte
    Dim rows As Int32, cols As Int32, numScales As Int32
    Dim salience As IntPtr
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders = New OptionsSliders
        sliders.setupTrackBar1(ocvb, callerName, "Salience numScales", 1, 6, 1)

        ReDim grayData(ocvb.color.Total - 1)
        rows = ocvb.color.Rows
        cols = ocvb.color.Cols
        salience = Salience_Open()
        ocvb.desc = "Show results of Salience algorithm when using C++"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim grayInput = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim cols = ocvb.color.Width
        Dim rows = ocvb.color.Height
        Dim roi As New cv.Rect(0, 0, cols, rows)
        If ocvb.drawRect.Width > 0 Then
            cols = ocvb.drawRect.Width
            rows = ocvb.drawRect.Height
            roi = ocvb.drawRect
            ocvb.color.CopyTo(ocvb.result1)
        End If

        Dim grayHandle = GCHandle.Alloc(grayData, GCHandleType.Pinned)
        Dim gray As New cv.Mat(rows, cols, cv.MatType.CV_8U, grayData)
        grayHandle.Free()
        grayInput(roi).CopyTo(gray)

        Dim imagePtr = Salience_Run(salience, sliders.TrackBar1.Value, gray.Data, roi.Height, roi.Width)

        Dim dstData(roi.Width * roi.Height - 1) As Byte
        Dim dst As New cv.Mat(rows, cols, cv.MatType.CV_8U, dstData)
        Marshal.Copy(imagePtr, dstData, 0, roi.Height * roi.Width)

        ocvb.color.CopyTo(ocvb.result1)
        ocvb.result1(roi) = dst.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub MyDispose()
        Salience_Close(salience)
    End Sub
End Class



Public Class Salience_Basics_MT
    Inherits VB_Class
    Dim grayData() As Byte
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
        If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        sliders = New OptionsSliders
        sliders.setupTrackBar1(ocvb, callerName, "Salience numScales", 1, 6, 1)
        sliders.setupTrackBar2(ocvb, callerName, "Salience Number of Threads", 1, 100, 36)

        ReDim grayData(ocvb.color.Total - 1)
        ocvb.desc = "Show results of multi-threaded Salience algorithm when using C++.  NOTE: salience is relative."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim numScales = sliders.TrackBar1.Value
        Dim threads = sliders.TrackBar2.Value
        Dim h = CInt(ocvb.color.Height / threads)
        Dim grayHandle = GCHandle.Alloc(grayData, GCHandleType.Pinned)
        Dim gray As New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8U, grayData)

        Parallel.For(0, threads - 1,
            Sub(i)
                Dim roi = New cv.Rect(0, i * h, ocvb.color.Width, Math.Min(h, ocvb.color.Height - i * h))
                If roi.Height <= 0 Then Exit Sub
                Dim grayInput = ocvb.color(roi).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                grayInput.CopyTo(gray(roi))

                Dim salience = Salience_Open()
                Dim imagePtr = Salience_Run(salience, numScales, gray(roi).Data, roi.Height, roi.Width)

                Dim dstData(roi.Width * roi.Height - 1) As Byte
                Dim dst As New cv.Mat(roi.Height, roi.Width, cv.MatType.CV_8U, dstData)
                Marshal.Copy(imagePtr, dstData, 0, roi.Height * roi.Width)

                ocvb.result1(roi) = dst.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
                Salience_Close(salience)
            End Sub)
        grayHandle.Free()
    End Sub
End Class
