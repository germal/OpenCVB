Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO

Module Retina_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Retina_Basics_Open(rows As Int32, cols As Int32, useLogSampling As Boolean, samplingFactor As Single) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Retina_Basics_Close(RetinaPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Retina_Basics_Run(RetinaPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32, magno As IntPtr, useLogSampling As Int32) As IntPtr
    End Function
End Module

'https://docs.opencv.org/3.4/d3/d86/tutorial_bioinspired_retina_model.html
Public Class Retina_Basics_CPP
    Inherits ocvbClass
    Dim Retina As IntPtr
    Dim startInfo As New ProcessStartInfo
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Retina Sample Factor", 1, 10, 2)

        check.Setup(ocvb, caller, 2)
        check.Box(0).Text = "Use log sampling"
        check.Box(1).Text = "Open resulting xml file"
        ocvb.desc = "Use the bio-inspired retina algorithm to adjust color and monitor motion."
        ocvb.label1 = "Retina Parvo"
        ocvb.label2 = "Retina Magno"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If check.Box(1).Checked Then
            check.Box(1).Checked = False
            Dim fileinfo = New FileInfo(CurDir() + "/RetinaDefaultParameters.xml")
            If fileinfo.Exists Then
                FileCopy(CurDir() + "/RetinaDefaultParameters.xml", ocvb.parms.HomeDir + "data/RetinaDefaultParameters.xml")
                startInfo.FileName = "wordpad.exe"
                startInfo.Arguments = ocvb.parms.HomeDir + "Data/RetinaDefaultParameters.xml"
                Process.Start(startInfo)
            Else
                MsgBox("RetinaDefaultParameters.xml should have been created but was not found.  OpenCV error?")
            End If
        End If
        Static useLogSampling As Int32 = check.Box(0).Checked
        Static samplingFactor As Single = -1 ' force open
        if standalone Then src = ocvb.color
        If useLogSampling <> check.Box(0).Checked Or samplingFactor <> sliders.TrackBar1.Value Then
            If Retina <> 0 Then Retina_Basics_Close(Retina)
            useLogSampling = check.Box(0).Checked
            samplingFactor = sliders.TrackBar1.Value
            Retina = Retina_Basics_Open(src.Rows, src.Cols, useLogSampling, samplingFactor)
        End If
        Dim magnoData(src.Total - 1) As Byte
        Dim handleMagno = GCHandle.Alloc(magnoData, GCHandleType.Pinned)

        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim magnoPtr = Retina_Basics_Run(Retina, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, handleMagno.AddrOfPinnedObject(), useLogSampling)
        handleSrc.Free()

        If magnoPtr <> 0 Then
            Dim nextFactor = samplingFactor
            If useLogSampling = False Then nextFactor = 1
            Dim parvoData(src.Total * src.ElemSize / (nextFactor * nextFactor) - 1) As Byte
            Marshal.Copy(magnoPtr, parvoData, 0, parvoData.Length)
            Dim parvo = New cv.Mat(src.Rows / nextFactor, src.Cols / nextFactor, cv.MatType.CV_8UC3, parvoData)
            ocvb.result1 = parvo.Resize(src.Size())

            Dim magno = New cv.Mat(src.Rows / nextFactor, src.Cols / nextFactor, cv.MatType.CV_8U, magnoData)
            ocvb.result2 = magno.Resize(src.Size())
        End If
    End Sub
    Public Sub MyDispose()
        Retina_Basics_Close(Retina)
            End Sub
End Class






Public Class Retina_Depth
    Inherits ocvbClass
    Dim retina As Retina_Basics_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
                setCaller(callerRaw)
        retina = New Retina_Basics_CPP(ocvb, caller)

        ocvb.desc = "Use the bio-inspired retina algorithm with the depth data."
        ocvb.label1 = "Last result || current result"
        ocvb.label2 = "Current depth motion result"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        retina.src = ocvb.RGBDepth
        retina.Run(ocvb)
        Static lastMotion As New cv.Mat
        If lastMotion.Width = 0 Then lastMotion = ocvb.result2
        cv.Cv2.BitwiseOr(lastMotion, ocvb.result2, ocvb.result1)
        lastMotion = ocvb.result2
    End Sub
    Public Sub MyDispose()
        retina.Dispose()
    End Sub
End Class