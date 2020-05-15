Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://github.com/opencv/opencv_contrib/blob/master/modules/bgsegm/samples/bgfg.cpp
Public Class BGSubtract_Basics_CPP
    Inherits ocvbClass
    Dim bgfs As IntPtr
    Public currMethod As Int32 = -1
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        radio.Setup(ocvb, caller, 7)
        radio.check(0).Text = "GMG"
        radio.check(1).Text = "CNT - Counting"
        radio.check(2).Text = "KNN"
        radio.check(3).Text = "MOG"
        radio.check(4).Text = "MOG2"
        radio.check(5).Text = "GSOC"
        radio.check(6).Text = "LSBP"
        radio.check(4).Checked = True ' mog2 appears to be the best...
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
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Correlation Threshold", 0, 1000, 980)
        radio.Setup(ocvb, caller, 6)
        For i = 0 To radio.check.Count - 1
            radio.check(i).Text = CStr(2 ^ i) + " threads"
        Next
        radio.check(0).Text = "1 thread"
        radio.check(5).Checked = True
        label2 = "Only Motion Added"
        ocvb.desc = "Detect Motion for use with background subtraction"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.frameCount = 0 Then ocvb.color.CopyTo(dst2)
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
                    cv.Cv2.MatchTemplate(ocvb.color(roi), dst2(roi), correlation, cv.TemplateMatchModes.CCoeffNormed)
                    If CCthreshold > correlation.Get(Of Single)(0, 0) Then
                        ocvb.color(roi).CopyTo(dst1(roi))
                        ocvb.color(roi).CopyTo(dst2(roi))
                    End If
                End Sub)
        Next
        Task.WaitAll(taskArray)
    End Sub
End Class




Public Class BGSubtract_Basics_MT
    Inherits ocvbClass
    Dim grid As Thread_Grid
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        grid = New Thread_Grid(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "Correlation Threshold", 0, 1000, 980)

        label2 = "Only Motion Added"
        ocvb.desc = "Detect Motion in the color image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = src.EmptyClone.SetTo(0)
        If ocvb.frameCount = 0 Then dst2 = src.Clone()
        Dim CCthreshold = CSng(sliders.TrackBar1.Value / sliders.TrackBar1.Maximum)
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
    Inherits ocvbClass
    Dim shadow As Depth_Holes
    Dim bgsub As BGSubtract_Basics_MT
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        bgsub = New BGSubtract_Basics_MT(ocvb, caller)
        shadow = New Depth_Holes(ocvb, caller)
        ocvb.desc = "Detect Motion in the depth image - more work needed"
        label1 = "Depth data input"
        label2 = "Accumulated depth image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        shadow.Run(ocvb) ' get where depth is zero
        bgsub.src = ocvb.RGBDepth
        bgsub.Run(ocvb)
        dst1 = bgsub.src
        dst2 = bgsub.dst2
        dst2.SetTo(0, shadow.holeMask)
    End Sub
End Class



Public Class BGSubtract_MOG
    Inherits ocvbClass
    Dim MOG As cv.BackgroundSubtractorMOG
    Public gray As New cv.Mat
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "MOG Learn Rate", 0, 1000, 10)

        MOG = cv.BackgroundSubtractorMOG.Create()
        ocvb.desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If src.Channels = 3 Then
            gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            gray = src
        End If
        MOG.Apply(gray, gray, sliders.TrackBar1.Value / 1000)
        dst1 = gray
    End Sub
End Class



Public Class BGSubtract_MOG2
    Inherits ocvbClass
    Public gray As New cv.Mat
    Dim MOG2 As cv.BackgroundSubtractorMOG2
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "MOG Learn Rate", 0, 1000, 10)

        MOG2 = cv.BackgroundSubtractorMOG2.Create()
        ocvb.desc = "Subtract background using a mixture of Gaussians"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If standalone Then
            gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        MOG2.Apply(gray, dst1, sliders.TrackBar1.Value / 1000)
    End Sub
End Class



Public Class BGSubtract_GMG_KNN
    Inherits ocvbClass
    Dim gmg As cv.BackgroundSubtractorGMG
    Dim knn As cv.BackgroundSubtractorKNN
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Learn Rate", 1, 1000, 1)

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

        dst1 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        gmg.Apply(dst1, dst1, sliders.TrackBar1.Value / 1000)
        knn.Apply(dst1, dst1, sliders.TrackBar1.Value / 1000)
    End Sub
End Class





Public Class BGSubtract_MOG_RGBDepth
    Inherits ocvbClass
    Public gray As New cv.Mat
    Dim MOGDepth As cv.BackgroundSubtractorMOG
    Dim MOGRGB As cv.BackgroundSubtractorMOG
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "MOG Learn Rate x1000", 0, 1000, 10)

        MOGDepth = cv.BackgroundSubtractorMOG.Create()
        MOGRGB = cv.BackgroundSubtractorMOG.Create()
        label1 = "Unstable depth"
        label1 = "Unstable color"
        ocvb.desc = "Isolate motion in both depth and color data using a mixture of Gaussians"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        gray = ocvb.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        MOGDepth.Apply(gray, gray, sliders.TrackBar1.Value / 1000)
        dst1 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        gray = ocvb.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        MOGRGB.Apply(gray, gray, sliders.TrackBar1.Value / 1000)
        dst2 = gray.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
    End Sub
End Class



Public Class BGSubtract_MOG_Retina
    Inherits ocvbClass
    Dim mog As BGSubtract_MOG
    Dim retina As Retina_Basics_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        mog = New BGSubtract_MOG(ocvb, caller)
        mog.sliders.TrackBar1.Value = 100

        retina = New Retina_Basics_CPP(ocvb, caller)

        sliders.setupTrackBar1(ocvb, caller, "Uncertainty threshold", 1, 255, 100)

        ocvb.desc = "Use the bio-inspired retina algorithm to create a background/foreground using depth."
        label1 = "MOG results of depth motion"
        label2 = "Difference from retina depth motion."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        retina.src = ocvb.RGBDepth
        retina.Run(ocvb)
        mog.src = retina.dst2.Clone()
        mog.Run(ocvb)
        dst1 = mog.dst1
        cv.Cv2.Subtract(mog.dst1, retina.dst2, dst2)
    End Sub
End Class




Public Class BGSubtract_DepthOrColorMotion
    Inherits ocvbClass
    Public motion As Diff_UnstableDepthAndColor
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        motion = New Diff_UnstableDepthAndColor(ocvb, caller)
        ocvb.desc = "Detect motion with both depth and color changes"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        motion.src = ocvb.color
        motion.Run(ocvb)
        dst1 = motion.dst1
        dst2 = motion.dst2
        Dim mask As New cv.Mat
        cv.Cv2.BitwiseNot(dst1, mask)
        ocvb.color.CopyTo(dst2, mask)
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
    Inherits ocvbClass
    Dim bgfg As BGSubtract_Basics_CPP
    Dim video As Video_Basics
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        bgfg = New BGSubtract_Basics_CPP(ocvb, caller)

        video = New Video_Basics(ocvb, caller)
        video.srcVideo = ocvb.parms.HomeDir + "Data/vtest.avi"
        ocvb.desc = "Demonstrate all background subtraction algorithms in OpenCV using a video instead of camera."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
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
    Inherits ocvbClass
    Dim synthPtr As IntPtr
    Dim amplitude As Double, magnitude As Double, waveSpeed As Double, objectSpeed As Double
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        sliders.setupTrackBar1(ocvb, caller, "Synthetic Amplitude x100", 1, 400, 200)
        sliders.setupTrackBar2(ocvb, caller, "Synthetic Magnitude", 1, 40, 20)
        sliders.setupTrackBar3(ocvb, caller, "Synthetic Wavespeed x100", 1, 400, 20)
        sliders.setupTrackBar4(ocvb, caller, "Synthetic ObjectSpeed", 1, 20, 15)
        label1 = "Synthetic background/foreground image."
        ocvb.desc = "Generate a synthetic input to background subtraction method - Painterly"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If amplitude <> sliders.TrackBar1.Value Or magnitude <> sliders.TrackBar2.Value Or waveSpeed <> sliders.TrackBar3.Value Or
            objectSpeed <> sliders.TrackBar4.Value Then

            If ocvb.frameCount <> 0 Then BGSubtract_Synthetic_Close(synthPtr)

            amplitude = sliders.TrackBar1.Value
            magnitude = sliders.TrackBar2.Value
            waveSpeed = sliders.TrackBar3.Value
            objectSpeed = sliders.TrackBar4.Value

            Dim srcData(src.Total * src.ElemSize - 1) As Byte
            Marshal.Copy(src.Data, srcData, 0, srcData.Length)
            Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)

            synthPtr = BGSubtract_Synthetic_Open(handleSrc.AddrOfPinnedObject(), ocvb.color.Rows, ocvb.color.Cols,
                                                ocvb.parms.HomeDir + "Data/baboon.jpg",
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
    Inherits ocvbClass
    Dim bgfg As BGSubtract_Basics_CPP
    Dim synth As BGSubtract_Synthetic_CPP
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        bgfg = New BGSubtract_Basics_CPP(ocvb, caller)

        synth = New BGSubtract_Synthetic_CPP(ocvb, caller)
        ocvb.desc = "Demonstrate background subtraction algorithms with synthetic images - Painterly"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        synth.src = src
        synth.Run(ocvb)
        dst2 = synth.dst2
        bgfg.src = dst2
        bgfg.Run(ocvb)
        dst1 = bgfg.dst1
    End Sub
End Class
