Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://github.com/opencv/opencv_contrib/blob/master/modules/bgsegm/samples/bgfg.cpp
Public Class BGSubtract_Basics_CPP
    Inherits VBparent
    Dim bgfs As IntPtr
    Public currMethod As Int32 = -1
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        radio.Setup(ocvb, caller, 7)
        radio.check(0).Text = "GMG"
        radio.check(1).Text = "CNT - Counting"
        radio.check(2).Text = "KNN"
        radio.check(3).Text = "MOG"
        radio.check(4).Text = "MOG2"
        radio.check(5).Text = "GSOC"
        radio.check(6).Text = "LSBP"
        radio.check(4).Checked = True ' mog2 appears to be the best...
        desc = "Demonstrate all the different background subtraction algorithms in OpenCV - some only available in C++"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                If currMethod = i Then
                    Exit For
                Else
                    If ocvb.frameCount > 0 Then BGSubtract_BGFG_Close(bgfs)
                    currMethod = i
                    label1 = "Method = " + radio.check(i).Text
                    bgfs = BGSubtract_BGFG_Open(currMethod)
                End If
            End If
        Next
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim imagePtr = BGSubtract_BGFG_Run(bgfs, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, src.Channels)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(src.Total - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
        End If
    End Sub
    Public Sub Close()
        BGSubtract_BGFG_Close(bgfs)
    End Sub
End Class





Public Class BGSubtract_MotionDetect_MT
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Correlation Threshold", 0, 1000, 980)
        radio.Setup(ocvb, caller, 6)
        For i = 0 To radio.check.Count - 1
            radio.check(i).Text = CStr(2 ^ i) + " threads"
        Next
        radio.check(0).Text = "1 thread"
        radio.check(5).Checked = True
        label2 = "Only Motion Added"
        desc = "Detect Motion for use with background subtraction"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.frameCount = 0 Then src.CopyTo(dst2)
        Dim threadData As New cv.Vec3i
        Dim width = src.Width, height = src.Height
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                threadData = Choose(i + 1, New cv.Vec3i(1, width, height), New cv.Vec3i(2, width / 2, height), New cv.Vec3i(4, width / 2, height / 2),
                                           New cv.Vec3i(8, width / 4, height / 2), New cv.Vec3i(16, width / 4, height / 4), New cv.Vec3i(32, width / 8, height / 4))
                Exit For
            End If
        Next
        Dim threadCount = threadData(0)
        width = threadData(1)
        height = threadData(2)
        Dim taskArray(threadCount - 1) As Task
        Dim xfactor = CInt(src.Width / width)
        Dim yfactor = Math.Max(CInt(src.Height / height), CInt(src.Width / width))
        Static correlationSlider = findSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        dst1.SetTo(0)
        For i = 0 To threadCount - 1
            Dim section = i
            taskArray(i) = Task.Factory.StartNew(
                Sub()
                    Dim roi = New cv.Rect((section Mod xfactor) * width, height * Math.Floor(section / yfactor), width, height)
                    Dim correlation As New cv.Mat
                    cv.Cv2.MatchTemplate(src(roi), dst2(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                    If CCthreshold > correlation.Get(Of Single)(0, 0) Then
                        src(roi).CopyTo(dst1(roi))
                        src(roi).CopyTo(dst2(roi))
                    End If
                End Sub)
        Next
        Task.WaitAll(taskArray)
    End Sub
End Class




Public Class BGSubtract_Basics_MT
    Inherits VBparent
    Dim grid As Thread_Grid
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        grid = New Thread_Grid(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Correlation Threshold", 0, 1000, 980)

        label2 = "Only Motion Added"
        desc = "Detect Motion in the color image"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        grid.Run(ocvb)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src.EmptyClone.SetTo(0)
        If ocvb.frameCount = 0 Then dst2 = src.Clone()
        Static correlationSlider = findSlider("Correlation Threshold")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)
        dst1.SetTo(0)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
        Sub(roi)
            Dim correlation As New cv.Mat
            cv.Cv2.MatchTemplate(src(roi), dst2(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
            If correlation.Get(Of Single)(0, 0) < CCthreshold Then src(roi).CopyTo(dst2(roi))
            src(roi).CopyTo(dst1(roi))
        End Sub)
    End Sub
End Class



Public Class BGSubtract_Depth_MT
    Inherits VBparent
    Dim shadow As Depth_Holes
    Dim bgsub As BGSubtract_Basics_MT
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        bgsub = New BGSubtract_Basics_MT(ocvb)
        shadow = New Depth_Holes(ocvb)
        desc = "Detect Motion in the depth image - needs more work"
        label1 = "Depth data input"
        label2 = "Accumulated depth image"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        shadow.Run(ocvb) ' get where depth is zero
        bgsub.src = ocvb.RGBDepth
        bgsub.Run(ocvb)
        dst1 = bgsub.src
        dst2 = bgsub.dst2
        dst2.SetTo(0, shadow.holeMask)
    End Sub
End Class



Public Class BGSubtract_MOG
    Inherits VBparent
    Dim MOG As cv.BackgroundSubtractorMOG
    Public gray As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "MOG Learn Rate", 0, 1000, 10)

        MOG = cv.BackgroundSubtractorMOG.Create()
        desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Channels = 3 Then
            gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            gray = src
        End If
        Static learnRateSlider = findSlider("MOG Learn Rate")
        MOG.Apply(gray, gray, learnRateSlider.Value / 1000)
        dst1 = gray
    End Sub
End Class



Public Class BGSubtract_MOG2
    Inherits VBparent
    Public gray As New cv.Mat
    Dim MOG2 As cv.BackgroundSubtractorMOG2
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "MOG Learn Rate", 0, 1000, 10)

        MOG2 = cv.BackgroundSubtractorMOG2.Create()
        desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static learnRateSlider = findSlider("MOG Learn Rate")
        MOG2.Apply(src, dst1, learnRateSlider.Value / 1000)
    End Sub
End Class



Public Class BGSubtract_GMG_KNN
    Inherits VBparent
    Dim gmg As cv.BackgroundSubtractorGMG
    Dim knn As cv.BackgroundSubtractorKNN
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Learn Rate", 1, 1000, 1)

        gmg = cv.BackgroundSubtractorGMG.Create()
        knn = cv.BackgroundSubtractorKNN.Create()
        desc = "GMG and KNN API's to subtract background"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.frameCount < 120 Then
            ocvb.trueText("Waiting to get sufficient frames to learn background.  frameCount = " + CStr(ocvb.frameCount))
        Else
            ocvb.trueText("")
        End If

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static learnRateSlider = findSlider("Learn Rate")
        gmg.Apply(dst1, dst1, learnRateSlider.Value / 1000)
        knn.Apply(dst1, dst1, learnRateSlider.Value / 1000)
    End Sub
End Class





Public Class BGSubtract_MOG_RGBDepth
    Inherits VBparent
    Public gray As New cv.Mat
    Dim MOGDepth As cv.BackgroundSubtractorMOG
    Dim MOGRGB As cv.BackgroundSubtractorMOG
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "MOG Learn Rate x1000", 0, 1000, 10)

        MOGDepth = cv.BackgroundSubtractorMOG.Create()
        MOGRGB = cv.BackgroundSubtractorMOG.Create()
        label1 = "Unstable depth"
        label1 = "Unstable color"
        desc = "Isolate motion in both depth and color data using a mixture of Gaussians"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        gray = ocvb.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static learnRateSlider = findSlider("Learn Rate")
        MOGDepth.Apply(gray, gray, learnRateSlider.Value / 1000)
        dst1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        MOGRGB.Apply(src, src, learnRateSlider.Value / 1000)
    End Sub
End Class



Public Class BGSubtract_MOG_Retina
    Inherits VBparent
    Dim bgSub As BGSubtract_MOG
    Dim retina As Retina_Basics_CPP
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        bgSub = New BGSubtract_MOG(ocvb)
        Static bgSubLearnRate = findSlider("MOG Learn Rate")
        bgSubLearnRate.Value = 100

        retina = New Retina_Basics_CPP(ocvb)

        label1 = "MOG results of depth motion"
        label2 = "Difference from retina depth motion."
        desc = "Use the bio-inspired retina algorithm to create a background/foreground using depth."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        retina.src = ocvb.RGBDepth
        retina.Run(ocvb)
        bgSub.src = retina.dst2.Clone()
        bgSub.Run(ocvb)
        dst1 = bgSub.dst1
        cv.Cv2.Subtract(bgSub.dst1, retina.dst2, dst2)
    End Sub
End Class




Public Class BGSubtract_DepthOrColorMotion
    Inherits VBparent
    Public motion As Diff_UnstableDepthAndColor
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        motion = New Diff_UnstableDepthAndColor(ocvb)
        desc = "Detect motion with both depth and color changes"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        motion.src = src.Clone()
        motion.Run(ocvb)
        dst1 = motion.dst1
        dst2 = motion.dst2
        Dim mask = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs()
        cv.Cv2.BitwiseNot(mask, mask)
        src.CopyTo(dst2, mask)
        label2 = "Image with instability filled with color data"
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





Public Class BGSubtract_Video
    Inherits VBparent
    Dim bgfg As BGSubtract_Basics_CPP
    Dim video As Video_Basics
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        bgfg = New BGSubtract_Basics_CPP(ocvb)

        video = New Video_Basics(ocvb)
        video.srcVideo = ocvb.HomeDir + "Data/vtest.avi"
        desc = "Demonstrate all background subtraction algorithms in OpenCV using a video instead of camera."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        video.Run(ocvb)
        dst2 = video.dst1
        bgfg.src = dst2
        bgfg.Run(ocvb)
        dst1 = bgfg.dst1
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





Public Class BGSubtract_Synthetic_CPP
    Inherits VBparent
    Dim synthPtr As IntPtr
    Dim amplitude As Double, magnitude As Double, waveSpeed As Double, objectSpeed As Double
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Synthetic Amplitude x100", 1, 400, 200)
        sliders.setupTrackBar(1, "Synthetic Magnitude", 1, 40, 20)
        sliders.setupTrackBar(2, "Synthetic Wavespeed x100", 1, 400, 20)
        sliders.setupTrackBar(3, "Synthetic ObjectSpeed", 1, 20, 15)
        label1 = "Synthetic background/foreground image."
        desc = "Generate a synthetic input to background subtraction method - Painterly"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.frameCount < 10 Then Exit Sub ' darker images at the start?
        If amplitude <> sliders.trackbar(0).Value Or magnitude <> sliders.trackbar(1).Value Or waveSpeed <> sliders.trackbar(2).Value Or
            objectSpeed <> sliders.trackbar(3).Value Then

            If ocvb.frameCount <> 0 Then BGSubtract_Synthetic_Close(synthPtr)

            amplitude = sliders.trackbar(0).Value
            magnitude = sliders.trackbar(1).Value
            waveSpeed = sliders.trackbar(2).Value
            objectSpeed = sliders.trackbar(3).Value

            Dim srcData(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, srcData, 0, srcData.Length)
            Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)

            synthPtr = BGSubtract_Synthetic_Open(handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                                ocvb.HomeDir + "Data/baboon.jpg",
                                                amplitude / 100, magnitude, waveSpeed / 100, objectSpeed)
            handleSrc.Free()
        End If
        Dim imagePtr = BGSubtract_Synthetic_Run(synthPtr)
        If imagePtr <> 0 Then dst1 = New cv.Mat(dst1.Rows, dst1.Cols, cv.MatType.CV_8UC3, imagePtr)
    End Sub
    Public Sub Close()
        BGSubtract_Synthetic_Close(synthPtr)
    End Sub
End Class






Public Class BGSubtract_Synthetic
    Inherits VBparent
    Dim bgfg As BGSubtract_Basics_CPP
    Dim synth As BGSubtract_Synthetic_CPP
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        bgfg = New BGSubtract_Basics_CPP(ocvb)

        synth = New BGSubtract_Synthetic_CPP(ocvb)
        desc = "Demonstrate background subtraction algorithms with synthetic images - Painterly"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        synth.src = src
        synth.Run(ocvb)
        dst2 = synth.dst1
        bgfg.src = dst2
        bgfg.Run(ocvb)
        dst1 = bgfg.dst1
    End Sub
End Class
