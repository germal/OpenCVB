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
    Public dst As New cv.Mat
    Public dst1 As cv.Mat
    Public dst2 As New cv.Mat
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
    End Sub
    Public Sub Finish(ocvb As AlgorithmData)
        If standalone And dst.Width <> 0 Then ocvb.result1 = dst
        If standalone And dst2.Width <> 0 Then ocvb.result2 = dst2
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        On Error Resume Next
        Dim proc = Process.GetProcessesByName("python")
        For i = 0 To proc.Count - 1
            proc(i).Kill()
        Next i
        If pyStream IsNot Nothing Then pyStream.Dispose()
        If algorithm.GetProperty("MyDispose") IsNot Nothing Then algorithm.MyDispose()  ' dispose of any managed and unmanaged classes.
        sliders.Dispose()
        sliders1.Dispose()
        sliders2.Dispose()
        sliders3.Dispose()
        radio1.Dispose()
        videoOptions.Dispose()
    End Sub
End Class
