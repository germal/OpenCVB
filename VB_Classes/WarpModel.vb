Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
' https://www.learnopencv.com/image-alignment-ecc-in-opencv-c-python/
Public Class WarpModel_Basics
    Inherits VBparent
    Public warp As WarpModel_Input
    Dim cPtr As IntPtr
    Public warpMatrix() As Single
    Public src2 As New cv.Mat
    Public warpMode As Integer
    Public aligned As New cv.Mat
    Public Sub New()
        initParent()
        cPtr = WarpModel_Open()

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 4)
            radio.check(0).Text = "Motion_Translation (fastest)"
            radio.check(1).Text = "Motion_Euclidean"
            radio.check(2).Text = "Motion_Affine (very slow - Be sure to configure CPP_Classes in Release Mode)"
            radio.check(3).Text = "Motion_Homography (even slower - Use CPP_Classes in Release Mode)"
            radio.check(0).Checked = True
        End If

        warp = New WarpModel_Input()

        label1 = "Src image (align to this image)"
        label2 = "Src2 image aligned to src image"
        task.desc = "Use FindTransformECC to align 2 images"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        warp.src = src
        warp.Run()

        Static frm = findfrm("WarpModel_Basics Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then warpMode = i
        Next

        If warp.check.Box(0).Checked Then
            src = warp.gradient(0)
            src2 = warp.gradient(1)
        Else
            src = warp.rgb(0)
            src2 = warp.rgb(1)
        End If

        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Dim src2Data(src2.Total * src2.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length - 1)
        Marshal.Copy(src2.Data, src2Data, 0, src2Data.Length - 1)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim handleSrc2 = GCHandle.Alloc(src2Data, GCHandleType.Pinned)

        Dim matPtr = WarpModel_Run(cPtr, handleSrc.AddrOfPinnedObject(), handleSrc2.AddrOfPinnedObject(), src.Rows, src.Cols, 1, warpMode)

        handleSrc.Free()
        handleSrc2.Free()

        If warpMode <> 3 Then
            ReDim warpMatrix(2 * 3 - 1)
        Else
            ReDim warpMatrix(3 * 3 - 1)
        End If
        Marshal.Copy(matPtr, warpMatrix, 0, warpMatrix.Length)

        If warpMode <> 3 Then
            Dim warpMat = New cv.Mat(2, 3, cv.MatType.CV_32F, warpMatrix)
            cv.Cv2.WarpAffine(warp.rgb(1), aligned, warpMat, warp.rgb(0).Size(), cv.InterpolationFlags.Linear + cv.InterpolationFlags.WarpInverseMap)
        Else
            Dim warpMat = New cv.Mat(3, 3, cv.MatType.CV_32F, warpMatrix)
            cv.Cv2.WarpPerspective(warp.rgb(1), aligned, warpMat, warp.rgb(0).Size(), cv.InterpolationFlags.Linear + cv.InterpolationFlags.WarpInverseMap)
        End If

        Dim rect As New cv.Rect(0, 0, warp.rgb(0).Width, warp.rgb(0).Height)
        dst1 = New cv.Mat(task.color.Size, cv.MatType.CV_8U, 0)
        dst2 = New cv.Mat(task.color.Size, cv.MatType.CV_8U, 0)
        dst1(rect) = warp.rgb(0)
        dst2(rect) = warp.rgb(1)

        Dim outStr = "The warp matrix is:" + vbCrLf
        For i = 0 To warpMatrix.Length - 1
            If i Mod 3 = 0 Then outStr += vbCrLf
            outStr += Format(warpMatrix(i), "#0.000") + vbTab
        Next

        If radio.check(2).Checked Or radio.check(3).Checked Then
            outStr += vbCrLf + "NOTE: input resized for performance." + vbCrLf + "Results are probably distorted." + vbCrLf + "Gradients may give better results."
        End If
        ocvb.trueText(outStr, aligned.Width + 10, 220)
    End Sub
    Public Sub Close()
        WarpModel_Close(cPtr)
    End Sub
End Class






' https://github.com/ycui11/-Colorizing-Prokudin-Gorskii-images-of-the-Russian-Empire
' https://github.com/petraohlin/Colorizing-the-Prokudin-Gorskii-Collection
Public Class WarpModel_Input
    Inherits VBparent
    Public rgb(3 - 1) As cv.Mat
    Public gradient(3 - 1) As cv.Mat
    Dim sobel As Edges_Sobel
    Public Sub New()
        initParent()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 12)
            radio.check(0).Text = "building.jpg"
            radio.check(1).Text = "church.jpg"
            radio.check(2).Text = "emir.jpg"
            radio.check(3).Text = "Painting.jpg"
            radio.check(4).Text = "railroad.jpg"
            radio.check(5).Text = "river.jpg"
            radio.check(6).Text = "Cliff.jpg"
            radio.check(7).Text = "Column.jpg"
            radio.check(8).Text = "General.jpg"
            radio.check(9).Text = "Girls.jpg"
            radio.check(10).Text = "Tablet.jpg"
            radio.check(11).Text = "Valley.jpg"
            radio.check(9).Checked = True
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Use Gradient in WarpInput"
        End If

        sobel = New Edges_Sobel()
        task.desc = "Import the misaligned input."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim img As New cv.Mat
        Static frm = findfrm("WarpModel_Input Radio Options")
        For i = 0 To frm.check.length - 1
            Dim nextRadio = frm.check(i)
            If nextRadio.Checked Then
                Dim photo As New FileInfo(ocvb.parms.homeDir + "Data\Prokudin\" + nextRadio.Text)
                img = cv.Cv2.ImRead(photo.FullName, cv.ImreadModes.Grayscale)
                label1 = photo.Name + " - red image"
                label2 = photo.Name + " - Naively aligned merge"
                Exit For
            End If
        Next
        Dim r() = {New cv.Rect(0, 0, img.Width, img.Height / 3), New cv.Rect(0, img.Height / 3, img.Width, img.Height / 3),
                   New cv.Rect(0, 2 * img.Height / 3, img.Width, img.Height / 3)}
        For i = 0 To r.Count - 1
            If check.Box(0).Checked Then
                sobel.src = img(r(i))
                sobel.Run()
                gradient(i) = sobel.dst1.Clone()
            End If
            rgb(i) = img(r(i))
        Next

        If src.Width < rgb(0).Width Or src.Height < rgb(0).Height Then
            For i = 0 To rgb.Count - 1
                Dim sz = New cv.Size(src.Width * rgb(i).Height / rgb(i).Width, src.Height)
                r(i) = New cv.Rect(0, 0, sz.Width, sz.Height)
                rgb(i) = rgb(i).Resize(sz)
            Next
        End If
        Dim merged As New cv.Mat
        cv.Cv2.Merge(rgb, merged)
        dst1.SetTo(0)
        dst2.SetTo(0)
        dst1(r(0)) = rgb(0).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2(r(0)) = merged
    End Sub
End Class




Module WarpModel_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WarpModel_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub WarpModel_Close(WarpModelPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function WarpModel_Run(WarpModelPtr As IntPtr, src1Ptr As IntPtr, src2Ptr As IntPtr, rows As Integer, cols As Integer, channels As Integer, warpMode As Integer) As IntPtr
    End Function
End Module







' https://www.learnopencv.com/image-alignment-ecc-in-opencv-c-python/
Public Class WarpModel_AlignImages
    Inherits VBparent
    Dim ecc As WarpModel_Basics
    Public Sub New()
        initParent()
        ecc = New WarpModel_Basics()

        task.desc = "Align the RGB inputs raw images from the Prokudin examples."
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim aligned() = {New cv.Mat, New cv.Mat}
        For i = 0 To 1
            If ecc.warp.check.Box(0).Checked Then
                ecc.src = Choose(i + 1, ecc.warp.gradient(0), ecc.warp.gradient(0))
                ecc.src2 = Choose(i + 1, ecc.warp.gradient(1), ecc.warp.gradient(2))
            Else
                ecc.src = Choose(i + 1, ecc.warp.rgb(0), ecc.warp.rgb(0))
                ecc.src2 = Choose(i + 1, ecc.warp.rgb(1), ecc.warp.rgb(2))
            End If
            ecc.src = src
            ecc.Run()
            aligned(i) = ecc.aligned.Clone()
        Next

        Dim mergeInput() = {ecc.warp.rgb(0), aligned(1), aligned(0)} ' green and blue were aligned to the original red
        Dim merged As New cv.Mat
        cv.Cv2.Merge(mergeInput, merged)
        dst1(New cv.Rect(0, 0, merged.Width, merged.Height)) = merged
        label1 = "Aligned image"
        ocvb.trueText("Note small displacement of" + vbCrLf + "the image when gradient is used." + vbCrLf +
                                              "Other than that, images look the same." + vbCrLf +
                                              "Displacement increases with Sobel" + vbCrLf + "kernel size", merged.Width + 10, 100)
    End Sub
End Class







Public Class WarpModel_Image
    Inherits VBparent
    Dim warp As WarpModel_Basics
    Dim sobel As Edges_Sobel
    Public Sub New()
        initParent()
        sobel = New Edges_Sobel
        warp = New WarpModel_Basics
        task.desc = "Find the Translation and Euclidean warp matrix for the current grayscale image to the previous"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        sobel.src = src
        sobel.Run()
        dst1 = sobel.dst1

        Static lastFrame = dst1.Clone
        ' warp.src1 = 
    End Sub
End Class