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
    Public Sub New()
        algorithm = Me
        label1 = Me.GetType.Name
    End Sub
    Public Sub NextFrame(ocvb As AlgorithmData)
        algorithm.Run(ocvb)
        If standalone Then
            If dst1.Width <> 0 Then ocvb.result1 = dst1
            If dst2.Width <> 0 Then ocvb.result2 = dst2
            ocvb.label1 = label1
            ocvb.label2 = label2
        End If
        If standalone Then
        End If
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
