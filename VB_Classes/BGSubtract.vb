Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://github.com/opencv/opencv_contrib/blob/master/modules/bgsegm/samples/bgfg.cpp
Public Class BGSubtract_Basics_CPP : Implements IDisposable
    Public radio As New OptionsRadioButtons
    Dim bgfs As IntPtr
    Public currMethod As Int32 = -1
    Public src As New cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        radio.Setup(ocvb, 7)
        radio.check(0).Text = "GMG"
        radio.check(1).Text = "CNT - Counting"
        radio.check(2).Text = "KNN"
        radio.check(3).Text = "MOG"
        radio.check(4).Text = "MOG2"
        radio.check(5).Text = "GSOC"
        radio.check(6).Text = "LSBP"
        radio.check(4).Checked = True ' mog2 appears to be the best...
        If ocvb.parms.ShowOptions Then radio.Show()
        ocvb.desc = "Demonstrate all the different background subtraction algorithms in OpenCV - some only available in C++"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                If currMethod = i Then
                    Exit For
                Else
                    If ocvb.frameCount > 0 Then BGSubtract_BGFG_Close(bgfs)
                    currMethod = i
                    ocvb.label1 = "Method = " + radio.check(i).Text
                    bgfs = BGSubtract_BGFG_Open(currMethod)
                End If
            End If
        Next
        If externalUse = False Then src = ocvb.color
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = BGSubtract_BGFG_Run(bgfs, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(src.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            ocvb.result1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        radio.Dispose()
        BGSubtract_BGFG_Close(bgfs)
    End Sub
End Class





Public Class BGSubtract_MotionDetect_MT : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim radio As New OptionsRadioButtons
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Correlation Threshold", 0, 1000, 980)
        If ocvb.parms.ShowOptions Then sliders.Show()
        radio.Setup(ocvb, 6)
        For i = 0 To radio.check.Count - 1
            radio.check(i).Text = CStr(2 ^ i) + " threads"
        Next
        radio.check(0).Text = "1 thread"
        radio.check(5).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()
        ocvb.label2 = "Only Motion Added"
        ocvb.desc = "Detect Motion for use with background subtraction"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1.SetTo(0)
        If ocvb.frameCount = 0 Then ocvb.color.CopyTo(ocvb.result2)
        Dim threadData As New cv.Vec3i
        Dim w = ocvb.color.Width, h = ocvb.color.Height
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                threadData = Choose(i + 1, New cv.Vec3i(1, w, h), New cv.Vec3i(2, w / 2, h), New cv.Vec3i(4, w / 2, h / 2),
                                           New cv.Vec3i(8, w / 4, h / 2), New cv.Vec3i(16, w / 4, h / 4), New cv.Vec3i(32, w / 8, h / 4))
                Exit For
            End If
        Next
        Dim threadCount = threadData(0)
        w = threadData(1)
        h = threadData(2)
        Dim taskArray(threadCount - 1) As Task
        Dim xfactor = CInt(ocvb.color.Width / w)
        Dim yfactor = Math.Max(CInt(ocvb.color.Height / h), CInt(ocvb.color.Width / w))
        Dim CCthreshold = CSng(sliders.TrackBar1.Value / sliders.TrackBar1.Maximum)
        For i = 0 To threadCount - 1
            Dim section = i
            taskArray(i) = Task.Factory.StartNew(
                Sub()
                    Dim roi = New cv.Rect((section Mod xfactor) * w, h * Math.Floor(section / yfactor), w, h)
                    Dim correlation As New cv.Mat
                    cv.Cv2.MatchTemplate(ocvb.color(roi), ocvb.result2(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                    If CCthreshold > correlation.Get(Of Single)(0, 0) Then
                        ocvb.color(roi).CopyTo(ocvb.result1(roi))
                        ocvb.color(roi).CopyTo(ocvb.result2(roi))
                    End If
                End Sub)
        Next
        Task.WaitAll(taskArray)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        radio.Dispose()
    End Sub
End Class




Public Class BGSubtract_Basics_MT : Implements IDisposable
    Public sliders As New OptionsSliders
    Public grid As Thread_Grid
    Dim accum As New cv.Mat
    Public externalUse = False
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)

        sliders.setupTrackBar1(ocvb, "Correlation Threshold", 0, 1000, 980)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.label2 = "Only Motion Added"
        ocvb.desc = "Detect Motion in the color image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        ocvb.result1.SetTo(0)
        If ocvb.frameCount = 0 Then ocvb.color.CopyTo(accum)
        Dim CCthreshold = CSng(sliders.TrackBar1.Value / sliders.TrackBar1.Maximum)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim correlation As New cv.Mat
            cv.Cv2.MatchTemplate(ocvb.color(roi), accum(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
            If CCthreshold > correlation.Get(Of Single)(0, 0) Then
                ocvb.color(roi).CopyTo(ocvb.result1(roi))
                ocvb.color(roi).CopyTo(accum(roi))
            End If
        End Sub)
        If externalUse = False Then accum.CopyTo(ocvb.result2) ' show the accumulated result if this is not some other object using me...
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        grid.Dispose()
    End Sub
End Class



Public Class BGSubtract_Depth_MT : Implements IDisposable
    Dim shadow As Depth_Holes
    Dim bgsub As BGSubtract_Basics_MT
    Public Sub New(ocvb As AlgorithmData)
        bgsub = New BGSubtract_Basics_MT(ocvb)
        bgsub.externalUse = True
        shadow = New Depth_Holes(ocvb)
        shadow.externalUse = True
        ocvb.desc = "Detect Motion in the depth image"
        ocvb.label2 = "Accumulated 3D image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb) ' get where depth is zero
        bgsub.Run(ocvb)

        If ocvb.frameCount = 0 Then ocvb.RGBDepth.CopyTo(ocvb.result2)
        Dim gray = ocvb.result1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim mask = gray.Threshold(1, 255, cv.ThresholdTypes.Binary)
        Dim shadowMask As New cv.Mat
        cv.Cv2.BitwiseAnd(shadow.holeMask, mask, shadowMask)
        mask.SetTo(0, shadow.holeMask)
        ocvb.RGBDepth.CopyTo(ocvb.result2, mask)
        mask = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        cv.Cv2.AddWeighted(ocvb.result1, 0.75, mask, 0.25, 0, ocvb.result1)
        ocvb.result2.SetTo(0, shadowMask)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        shadow.Dispose()
        bgsub.Dispose()
    End Sub
End Class



Public Class BGSubtract_MOG : Implements IDisposable
    Public sliders As New OptionsSliders
    Public src As New cv.Mat
    Public externalUse As Boolean
    Public gray As New cv.Mat
    Dim MOG As cv.BackgroundSubtractorMOG
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "MOG Learn Rate", 0, 1000, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()

        MOG = cv.BackgroundSubtractorMOG.Create()
        ocvb.desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then src = ocvb.color
        If src.Channels = 3 Then
            gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            gray = src
        End If
        MOG.Apply(gray, ocvb.result1, sliders.TrackBar1.Value / 1000)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        MOG.Dispose()
    End Sub
End Class



Public Class BGSubtract_MOG2 : Implements IDisposable
    Public sliders As New OptionsSliders
    Public src As New cv.Mat
    Public externalUse As Boolean
    Public gray As New cv.Mat
    Dim MOG2 As cv.BackgroundSubtractorMOG2
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "MOG Learn Rate", 0, 1000, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()

        MOG2 = cv.BackgroundSubtractorMOG2.Create()
        ocvb.desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse Then
            gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        MOG2.Apply(gray, ocvb.result1, sliders.TrackBar1.Value / 1000)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        MOG2.Dispose()
    End Sub
End Class



Public Class BGSubtract_GMG_KNN : Implements IDisposable
    Public sliders As New OptionsSliders
    Dim gmg As cv.BackgroundSubtractorGMG
    Dim knn As cv.BackgroundSubtractorKNN
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Learn Rate", 1, 1000, 1)
        If ocvb.parms.ShowOptions Then sliders.Show()

        gmg = cv.BackgroundSubtractorGMG.Create()
        knn = cv.BackgroundSubtractorKNN.Create()
        ocvb.desc = "GMG and KNN API's to subtract background"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount < 120 Then
            ocvb.putText(New ActiveClass.TrueType("Waiting to get sufficient frames to learn background.  frameCount = " + CStr(ocvb.frameCount), 10, 60, RESULT2))
        Else
            ocvb.putText(New ActiveClass.TrueType("", 10, 60, RESULT2))
        End If

        Dim gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ocvb.result1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        gmg.Apply(gray, gray, sliders.TrackBar1.Value / 1000)

        knn.Apply(gray, gray, sliders.TrackBar1.Value / 1000)
        ocvb.result2 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        gmg.Dispose()
        knn.Dispose()
    End Sub
End Class





Public Class BGSubtract_MOG_RGBDepth : Implements IDisposable
    Public sliders As New OptionsSliders
    Public gray As New cv.Mat
    Dim MOGDepth As cv.BackgroundSubtractorMOG
    Dim MOGRGB As cv.BackgroundSubtractorMOG
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "MOG Learn Rate x1000", 0, 1000, 10)
        If ocvb.parms.ShowOptions Then sliders.Show()

        MOGDepth = cv.BackgroundSubtractorMOG.Create()
        MOGRGB = cv.BackgroundSubtractorMOG.Create()
        ocvb.label1 = "Unstable depth"
        ocvb.label2 = "Unstable RGB data"
        ocvb.desc = "Subtract background from depth data using a mixture of Gaussians"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        gray = ocvb.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        MOGDepth.Apply(gray, gray, sliders.TrackBar1.Value / 1000)
        ocvb.result1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        MOGRGB.Apply(gray, gray, sliders.TrackBar1.Value / 1000)
        ocvb.result2 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        sliders.Dispose()
        MOGDepth.Dispose()
        MOGRGB.Dispose()
    End Sub
End Class



Public Class BGSubtract_MOG_Retina : Implements IDisposable
    Dim input As BGSubtract_MOG
    Dim retina As Retina_Basics_CPP
    Dim sliders As New OptionsSliders
    Public Sub New(ocvb As AlgorithmData)
        input = New BGSubtract_MOG(ocvb)
        input.externalUse = True
        input.sliders.TrackBar1.Value = 100

        retina = New Retina_Basics_CPP(ocvb)
        retina.externalUse = True

        sliders.setupTrackBar1(ocvb, "Uncertainty threshold", 1, 255, 100)
        If ocvb.parms.ShowOptions Then sliders.Show()

        ocvb.desc = "Use the bio-inspired retina algorithm to create a background/foreground using depth."
        ocvb.label1 = "MOG results of depth motion"
        ocvb.label2 = "Difference from retina depth motion."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        retina.src = ocvb.RGBDepth
        retina.Run(ocvb)
        input.src = ocvb.result2
        input.Run(ocvb)
        cv.Cv2.Subtract(ocvb.result1, ocvb.result2, ocvb.result2)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        input.Dispose()
        retina.Dispose()
        sliders.Dispose()
    End Sub
End Class




Public Class BGSubtract_DepthOrColorMotion : Implements IDisposable
    Dim motion As Diff_UnstableDepthAndColor
    Public Sub New(ocvb As AlgorithmData)
        motion = New Diff_UnstableDepthAndColor(ocvb)
        ocvb.desc = "Detect motion with both depth and color changes"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        motion.Run(ocvb)
        Dim tmp As New cv.Mat
        cv.Cv2.BitwiseNot(ocvb.result1, tmp)
        ocvb.color.CopyTo(ocvb.result2, tmp)
        ocvb.label2 = "Image with motion removed"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        motion.Dispose()
    End Sub
End Class






Module BGSubtract_BGFG_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_BGFG_Open(currMethod As Int32) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub BGSubtract_BGFG_Close(bgfs As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_BGFG_Run(bgfs As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32, channels As Int32) As IntPtr
    End Function
End Module





Public Class BGSubtract_Video : Implements IDisposable
    Dim bgfg As BGSubtract_Basics_CPP
    Dim video As Video_Basics
    Public Sub New(ocvb As AlgorithmData)
        bgfg = New BGSubtract_Basics_CPP(ocvb)
        bgfg.externalUse = True

        video = New Video_Basics(ocvb)
        video.srcVideo = ocvb.parms.HomeDir + "Data/vtest.avi"
        ocvb.desc = "Demonstrate all background subtraction algorithms in OpenCV using a video instead of camera."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        video.Run(ocvb)
        bgfg.src = video.image
        bgfg.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        bgfg.Dispose()
        video.Dispose()
    End Sub
End Class






Module BGSubtract_Synthetic_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_Synthetic_Open(rgbPtr As IntPtr, rows As Int32, cols As Int32, fgFilename As String, amplitude As Double,
                                              magnitude As Double, wavespeed As Double, objectspeed As Double) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub BGSubtract_Synthetic_Close(synthPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function BGSubtract_Synthetic_Run(synthPtr As IntPtr) As IntPtr
    End Function
End Module





Public Class BGSubtract_Synthetic_CPP : Implements IDisposable
    Dim synthPtr As IntPtr
    Dim sliders As New OptionsSliders
    Dim amplitude As Double, magnitude As Double, waveSpeed As Double, objectSpeed As Double
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "Synthetic Amplitude x100", 1, 400, 200)
        sliders.setupTrackBar2(ocvb, "Synthetic Magnitude", 1, 40, 20)
        sliders.setupTrackBar3(ocvb, "Synthetic Wavespeed x100", 1, 400, 20)
        sliders.setupTrackBar4(ocvb, "Synthetic ObjectSpeed", 1, 20, 15)
        If ocvb.parms.ShowOptions Then sliders.Show()
        ocvb.label1 = ""
        ocvb.label2 = "Synthetic background/foreground image."
        ocvb.desc = "Generate a synthetic input to background subtraction method."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If amplitude <> sliders.TrackBar1.Value Or magnitude <> sliders.TrackBar2.Value Or waveSpeed <> sliders.TrackBar3.Value Or
            objectSpeed <> sliders.TrackBar4.Value Then

            If ocvb.frameCount <> 0 Then BGSubtract_Synthetic_Close(synthPtr)

            amplitude = sliders.TrackBar1.Value
            magnitude = sliders.TrackBar2.Value
            waveSpeed = sliders.TrackBar3.Value
            objectSpeed = sliders.TrackBar4.Value

            Dim src = ocvb.color
            Dim srcData(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, srcData, 0, srcData.Length)
            Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)

            synthPtr = BGSubtract_Synthetic_Open(handleSrc.AddrOfPinnedObject(), ocvb.color.Rows, ocvb.color.Cols,
                                                ocvb.parms.HomeDir + "Data/baboon.jpg",
                                                amplitude / 100, magnitude, waveSpeed / 100, objectSpeed)
            handleSrc.Free()
        End If
        Dim imagePtr = BGSubtract_Synthetic_Run(synthPtr)

        If imagePtr <> 0 Then
            Dim dstData(ocvb.result2.Total * ocvb.result2.ElemSize - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            ocvb.result2 = New cv.Mat(ocvb.result2.Rows, ocvb.result2.Cols, cv.MatType.CV_8UC3, dstData)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        BGSubtract_Synthetic_Close(synthPtr)
        sliders.Dispose()
    End Sub
End Class






Public Class BGSubtract_Synthetic : Implements IDisposable
    Dim bgfg As BGSubtract_Basics_CPP
    Dim synth As BGSubtract_Synthetic_CPP
    Public Sub New(ocvb As AlgorithmData)
        bgfg = New BGSubtract_Basics_CPP(ocvb)
        bgfg.externalUse = True

        synth = New BGSubtract_Synthetic_CPP(ocvb)
        ocvb.desc = "Demonstrate background subtraction algorithms with synthetic images."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        synth.Run(ocvb)
        bgfg.src = ocvb.result2.Clone()
        bgfg.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        bgfg.Dispose()
        synth.Dispose()
    End Sub
End Class