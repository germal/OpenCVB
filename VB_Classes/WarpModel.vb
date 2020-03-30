Imports cv = OpenCvSharp
Imports System.IO
Imports System.Runtime.InteropServices
Public Class WarpModel_Input : Implements IDisposable
    Dim radio As New OptionsRadioButtons
    Public rgb(3 - 1) As cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        radio.Setup(ocvb, 6)
        radio.check(0).Text = "building.jpg"
        radio.check(1).Text = "church.jpg"
        radio.check(2).Text = "emir.jpg"
        radio.check(3).Text = "Painting.jpg"
        radio.check(4).Text = "railroad.jpg"
        radio.check(5).Text = "river.jpg"
        radio.check(0).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()

        ocvb.desc = "Import the misaligned input."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim img As New cv.Mat
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                Dim photo As New FileInfo(ocvb.parms.HomeDir + "Data\Prokudin\" + radio.check(i).Text)
                img = cv.Cv2.ImRead(photo.FullName, cv.ImreadModes.Grayscale)
                ocvb.label1 = photo.Name + " - red image"
                ocvb.label2 = photo.Name + " - Misaligned merge"
            End If
        Next
        Dim r() = {New cv.Rect(0, 0, img.Width, img.Height / 3), New cv.Rect(0, img.Height / 3, img.Width, img.Height / 3), New cv.Rect(0, 2 * img.Height / 3, img.Width, img.Height / 3)}
        For i = 0 To r.Count - 1
            rgb(i) = img(r(i))
        Next
        Dim merged As New cv.Mat
        cv.Cv2.Merge(rgb, merged)
        ocvb.result1(r(0)) = rgb(0).CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        ocvb.result2(r(0)) = merged
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        radio.Dispose()
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


Public Class WarpModel_FindTransformECC_CPP : Implements IDisposable
    Public radio As New OptionsRadioButtons
    Public input As WarpModel_Input
    Dim cPtr As IntPtr
    Public warpMatrix() As Double
    Public src1 As New cv.Mat
    Public src2 As New cv.Mat
    Public externalUse As Boolean
    Public warpMode As Integer
    Public aligned As New cv.Mat
    Public Sub New(ocvb As AlgorithmData)
        cPtr = WarpModel_Open()

        radio.Setup(ocvb, 4)
        radio.check(0).Text = "Motion_Translation"
        radio.check(1).Text = "Motion_Euclidean"
        radio.check(2).Text = "Motion_Affine"
        radio.check(3).Text = "Motion_Homography"
        radio.check(0).Checked = True
        If ocvb.parms.ShowOptions Then radio.Show()

        input = New WarpModel_Input(ocvb)

        ocvb.desc = "Use FindTransformECC to align 2 images"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If externalUse = False Then input.Run(ocvb)

        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then warpMode = i
        Next

        If externalUse = False Then src1 = input.rgb(0)
        Dim src1Data(src1.Total * src1.ElemSize) As Byte
        Marshal.Copy(src1.Data, src1Data, 0, src1Data.Length - 1)

        If externalUse = False Then src2 = input.rgb(1)
        Dim src2Data(src1.Total * src1.ElemSize) As Byte
        Marshal.Copy(src1.Data, src2Data, 0, src2Data.Length - 1)

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

        If warpMode <> 3 Then
            Dim warpMat = New cv.Mat(2, 3, cv.MatType.CV_32F, warpMatrix)
            cv.Cv2.WarpAffine(src2, aligned, warpMat, src1.Size(), cv.InterpolationFlags.Linear + cv.InterpolationFlags.WarpInverseMap)
        Else
            Dim warpMat = New cv.Mat(3, 3, cv.MatType.CV_32F, warpMatrix)
            cv.Cv2.WarpPerspective(src2, aligned, warpMat, src1.Size(), cv.InterpolationFlags.Linear + cv.InterpolationFlags.WarpInverseMap)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        WarpModel_Close(cPtr)
        radio.Dispose()
        input.Dispose()
    End Sub
End Class







Public Class WarpModel_AlignInput : Implements IDisposable
    Dim ecc As WarpModel_FindTransformECC_CPP
    Public Sub New(ocvb As AlgorithmData)
        ecc = New WarpModel_FindTransformECC_CPP(ocvb)

        ocvb.desc = "Align the RGB inputs from the Prokudin examples."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim aligned() = {New cv.Mat, New cv.Mat}

        For i = 0 To 1
            ecc.src1 = Choose(i + 1, ecc.input.rgb(0), ecc.input.rgb(0))
            ecc.src2 = Choose(i + 1, ecc.input.rgb(1), ecc.input.rgb(2))
            ecc.Run(ocvb)
            aligned(i) = ecc.aligned.Clone()
        Next

        Dim mergeInput() = {ecc.input.rgb(0), aligned(1), aligned(0)} ' green and blue were aligned to the original red 
        Dim merged As New cv.Mat
        cv.Cv2.Merge(mergeInput, merged)
        ocvb.result1(New cv.Rect(0, 0, merged.Width, merged.Height)) = merged
        ocvb.label1 = "Aligned image"
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
