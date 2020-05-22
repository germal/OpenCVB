Imports cv = OpenCvSharp
Public Class ocvbClass : Implements IDisposable
    Public caller As String
    Public check As New OptionsCheckbox
    Public radio As New OptionsRadioButtons
    Public radio1 As New OptionsRadioButtons
    Public sliders As New OptionsSliders
    Public sliders1 As New OptionsSliders
    Public sliders2 As New OptionsSliders
    Public sliders3 As New OptionsSliders
    Public videoOptions As New OptionsVideoName
    Public pyStream As PyStream_Basics = Nothing
    Public standalone As Boolean
    Public src As New cv.Mat
    Public dst1 As New cv.Mat
    Public dst2 As New cv.Mat
    Public label1 As String
    Public label2 As String
    Public myRNG As New cv.RNG
    Dim algorithm As Object
    Public Sub setCaller(callerRaw As String)
        If callerRaw = "" Or callerRaw = Me.GetType.Name Then
            standalone = True
            caller = Me.GetType.Name
        Else
            standalone = False
            caller = callerRaw + "/" + Me.GetType.Name
        End If
    End Sub
    Public Function validateRect(r As cv.Rect) As cv.Rect
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X > colorCols Then r.X = colorCols
        If r.Y > colorRows Then r.Y = colorRows
        If r.X + r.Width > colorCols Then r.Width = colorCols - r.X
        If r.Y + r.Height > colorRows Then r.Height = colorRows - r.Y
        Return r
    End Function
    Public Sub New()
        dst1 = New cv.Mat(colorRows, colorCols, cv.MatType.CV_8UC3, 0)
        dst2 = New cv.Mat(colorRows, colorCols, cv.MatType.CV_8UC3, 0)
        algorithm = Me
        label1 = Me.GetType.Name
    End Sub
    Private Sub MakeSureImage8uC3(ByRef src As cv.Mat)
        If src.Type = cv.MatType.CV_32F Then
            ' it must be a 1 channel 32f image so convert it to 8-bit and let it get converted to RGB below
            src = src.Normalize(0, 255, cv.NormTypes.MinMax)
            src.ConvertTo(src, cv.MatType.CV_8UC1)
        End If
        If src.Channels = 1 And src.Type = cv.MatType.CV_8UC1 Then
            src = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If
    End Sub
    Public Sub NextFrame(ocvb As AlgorithmData)
        If ocvb.drawRect.Width <> 0 Then ocvb.drawRect = validateRect(ocvb.drawRect)
        If standalone Then src = ocvb.color
        algorithm.Run(ocvb)
        If standalone Then
            If dst1.Width <> 0 Then
                ocvb.result1 = dst1
                MakeSureImage8uC3(ocvb.result1)
            End If
            If dst2.Width <> 0 Then
                ocvb.result2 = dst2
                MakeSureImage8uC3(ocvb.result2)
            End If
            ocvb.label1 = label1
            ocvb.label2 = label2
            ocvb.frameCount += 1
        End If
    End Sub
    Public Sub vtkInstructions(ocvb As AlgorithmData)
        ocvb.putText(New ActiveClass.TrueType("VTK support is disabled. " + vbCrLf + "Enable VTK with the following steps:" + vbCrLf + vbCrLf +
                                             "Step 1) Run 'PrepareVTK.bat' in <OpenCVB_Home>" + vbCrLf +
                                             "Step 2) Build VTK for both Debug and Release" + vbCrLf +
                                             "Step 3) Build OpenCV for both Debug and Release" + vbCrLf +
                                             "Step 4) Edit mainVTK.cpp (project VTKDataExample) and modify the first line", 10, 125))
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        On Error Resume Next
        Dim proc = Process.GetProcessesByName("python")
        For i = 0 To proc.Count - 1
            proc(i).Kill()
        Next i
        If pyStream IsNot Nothing Then pyStream.Dispose()
        If algorithm.GetProperty("Close") IsNot Nothing Then algorithm.Close()  ' Close any unmanaged classes...
        sliders.Dispose()
        sliders1.Dispose()
        sliders2.Dispose()
        sliders3.Dispose()
        check.Dispose()
        radio1.Dispose()
        videoOptions.Dispose()
    End Sub
End Class
