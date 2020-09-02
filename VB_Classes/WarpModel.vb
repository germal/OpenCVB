Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
' https://github.com/ycui11/-Colorizing-Prokudin-Gorskii-images-of-the-Russian-Empire
' https://github.com/petraohlin/Colorizing-the-Prokudin-Gorskii-Collection
Public Class WarpModel_Input
    Inherits VBparent
    Public rgb(3 - 1) As cv.Mat
    Public gradient(3 - 1) As cv.Mat
    Dim sobel As Edges_Sobel
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        radio.Setup(ocvb, caller, 12)
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

        check.Setup(ocvb, caller, 1)
        check.Box(0).Text = "Use Gradient in WarpInput"

        sobel = New Edges_Sobel(ocvb)
        desc = "Import the misaligned input."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim img As New cv.Mat
        For i = 0 To radio.check.Count - 1
            Dim nextRadio = radio.check(i)
            If nextRadio.Checked Then
                Dim photo As New FileInfo(ocvb.homeDir + "Data\Prokudin\" + nextRadio.Text)
                img = cv.Cv2.ImRead(photo.FullName, cv.ImreadModes.Grayscale)
                label1 = photo.Name + " - red image"
                label2 = photo.Name + " - Naively aligned merge"
                Exit For
            End If
        Next
        Dim r() = {New cv.Rect(0, 0, img.Width, img.Height / 3), New cv.Rect(0, img.Height / 3, img.Width, img.Height / 3), New cv.Rect(0, 2 * img.Height / 3, img.Width, img.Height / 3)}
        For i = 0 To r.Count - 1
            If check.Box(0).Checked Then
                sobel.src = img(r(i))
                sobel.Run(ocvb)
                gradient(i) = sobel.dst1.Clone()
            End If
            rgb(i) = img(r(i))
        Next
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
    Public Function WarpModel_Run(WarpModelPtr As IntPtr, src1Ptr As IntPtr, src2Ptr As IntPtr, rows As Int32, cols As Int32, channels As Int32, warpMode As Integer) As IntPtr
    End Function
End Module





' https://www.learnopencv.com/image-alignment-ecc-in-opencv-c-python/
Public Class WarpModel_FindTransformECC_CPP
    Inherits VBparent
    Public input As WarpModel_Input
    Dim cPtr As IntPtr
    Public warpMatrix() As Single
    Public src1 As New cv.Mat
    Public src2 As New cv.Mat
    Public rgb1 As New cv.Mat
    Public rgb2 As New cv.Mat
    Public warpMode As Integer
    Public aligned As New cv.Mat
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        cPtr = WarpModel_Open()

        radio.Setup(ocvb, caller,4)
        radio.check(0).Text = "Motion_Translation (fastest)"
        radio.check(1).Text = "Motion_Euclidean"
        radio.check(2).Text = "Motion_Affine (very slow - Use CPP_Classes in Release Mode)"
        radio.check(3).Text = "Motion_Homography (even slower - Use CPP_Classes in Release Mode)"
        radio.check(0).Checked = True

        input = New WarpModel_Input(ocvb)

        desc = "Use FindTransformECC to align 2 images"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        input.src = src
        input.Run(ocvb)
        dst1 = input.dst1

        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then warpMode = i
        Next

        If input.check.Box(0).Checked Then
            src1 = input.gradient(0)
            src2 = input.gradient(1)
        Else
            src1 = input.rgb(0)
            src2 = input.rgb(1)
        End If

        If radio.check(2).Checked Or radio.check(3).Checked Then
            src1 = src1.Resize(New cv.Size(src1.Width / 4, src1.Height / 4))
            src2 = src2.Resize(New cv.Size(src2.Width / 4, src2.Height / 4))
        End If

        Dim src1Data(src1.Total * src1.ElemSize - 1) As Byte
        Dim src2Data(src2.Total * src2.ElemSize - 1) As Byte
        Marshal.Copy(src1.Data, src1Data, 0, src1Data.Length - 1)
        Marshal.Copy(src2.Data, src2Data, 0, src2Data.Length - 1)
        Dim handleSrc1 = GCHandle.Alloc(src1Data, GCHandleType.Pinned)
        Dim handleSrc2 = GCHandle.Alloc(src2Data, GCHandleType.Pinned)

        Dim matPtr = WarpModel_Run(cPtr, handleSrc1.AddrOfPinnedObject(), handleSrc2.AddrOfPinnedObject(), src1.Rows, src1.Cols, 1, warpMode)

        handleSrc1.Free()
        handleSrc2.Free()

        If warpMode <> 3 Then
            ReDim warpMatrix(2 * 3 - 1)
        Else
            ReDim warpMatrix(3 * 3 - 1)
        End If
        Marshal.Copy(matPtr, warpMatrix, 0, warpMatrix.Length)

        rgb1 = input.rgb(0)
        rgb2 = input.rgb(1)
        If warpMode <> 3 Then
            Dim warpMat = New cv.Mat(2, 3, cv.MatType.CV_32F, warpMatrix)
            cv.Cv2.WarpAffine(rgb2, aligned, warpMat, rgb1.Size(), cv.InterpolationFlags.Linear + cv.InterpolationFlags.WarpInverseMap)
        Else
            Dim warpMat = New cv.Mat(3, 3, cv.MatType.CV_32F, warpMatrix)
            cv.Cv2.WarpPerspective(rgb2, aligned, warpMat, rgb1.Size(), cv.InterpolationFlags.Linear + cv.InterpolationFlags.WarpInverseMap)
        End If
        Dim outStr = "The warp matrix is:" + vbCrLf
        For i = 0 To warpMatrix.Length - 1
            If i Mod 3 = 0 Then outStr += vbCrLf
            outStr += Format(warpMatrix(i), "#0.000") + vbTab
        Next

        If radio.check(2).Checked Or radio.check(3).Checked Then
            outStr += vbCrLf + "NOTE: input resized for performance." + vbCrLf + "Results are probably distorted." + vbCrLf + "Gradients may give better results."
        End If
        ocvb.trueText(New TTtext(outStr, aligned.Width + 10, 220))
    End Sub
    Public Sub Close()
        WarpModel_Close(cPtr)
    End Sub
End Class







' https://www.learnopencv.com/image-alignment-ecc-in-opencv-c-python/
Public Class WarpModel_AlignImages
    Inherits VBparent
    Dim ecc As WarpModel_FindTransformECC_CPP
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        ecc = New WarpModel_FindTransformECC_CPP(ocvb)

        desc = "Align the RGB inputs raw images from the Prokudin examples."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim aligned() = {New cv.Mat, New cv.Mat}
        For i = 0 To 1
            If ecc.input.check.Box(0).Checked Then
                ecc.src1 = Choose(i + 1, ecc.input.gradient(0), ecc.input.gradient(0))
                ecc.src2 = Choose(i + 1, ecc.input.gradient(1), ecc.input.gradient(2))
            Else
                ecc.src1 = Choose(i + 1, ecc.input.rgb(0), ecc.input.rgb(0))
                ecc.src2 = Choose(i + 1, ecc.input.rgb(1), ecc.input.rgb(2))
            End If
            ecc.src = src
            ecc.Run(ocvb)
            aligned(i) = ecc.aligned.Clone()
        Next

        Dim mergeInput() = {ecc.input.rgb(0), aligned(1), aligned(0)} ' green and blue were aligned to the original red
        Dim merged As New cv.Mat
        cv.Cv2.Merge(mergeInput, merged)
        dst1(New cv.Rect(0, 0, merged.Width, merged.Height)) = merged
        label1 = "Aligned image"
        ocvb.trueText(New TTtext("Note small displacement of" + vbCrLf + "the image when gradient is used." + vbCrLf +
                                              "Other than that, images look the same." + vbCrLf +
                                              "Displacement increases with Sobel" + vbCrLf + "kernel size", merged.Width + 10, 100))
    End Sub
End Class

