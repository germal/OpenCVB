Imports Numpy
Imports py = Python.Runtime
Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class TTtext
    Public Const RESULT1 = 2
    Public Const RESULT2 = 3
    Public text As String

    Public picTag As Int32
    Public x As Int32
    Public y As Int32
    Public Sub New(_text As String, _x As Int32, _y As Int32, Optional _picTag As Int32 = RESULT1)
        text = _text
        x = _x
        y = _y
        picTag = _picTag
    End Sub
    Public Sub New(_text As String, _x As Int32, _y As Int32)
        text = _text
        x = _x
        y = _y
        picTag = RESULT1
    End Sub
End Class
Public Class ocvbClass : Implements IDisposable
    Public check As New OptionsCheckbox
    Public combo As New OptionsCombo
    Public radio As New OptionsRadioButtons
    Public radio1 As New OptionsRadioButtons
    Public sliders As New OptionsSliders
    Public pyStream As PyStream_Basics = Nothing
    Public standalone As Boolean
    Public src As New cv.Mat
    Public dst1 As New cv.Mat
    Public dst2 As New cv.Mat
    Public label1 As String
    Public label2 As String
    Public msRNG As New System.Random
    Public caller As String = ""
    Dim algorithm As Object
    Public scalarColors(255) As cv.Scalar
    Public rColors(255) As cv.Vec3b
    Public Const RESULT1 = 2 ' 0=rgb 1=depth 2=result1 3=Result2
    Public Const RESULT2 = 3 ' 0=rgb 1=depth 2=result1 3=Result2

    Public Sub setCaller(ocvb As AlgorithmData)
        If ocvb.caller = "" Then
            standalone = True
            ocvb.caller = Me.GetType.Name
        Else
            standalone = False
        End If
        caller += Me.GetType.Name
    End Sub
    Public Function validateRect(r As cv.Rect) As cv.Rect
        If r.Width < 0 Then r.Width = 1
        If r.Height < 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X > src.Width Then r.X = src.Width
        If r.Y > src.Height Then r.Y = src.Height
        If r.X + r.Width > src.Width Then r.Width = src.Width - r.X
        If r.Y + r.Height > src.Height Then r.Height = src.Height - r.Y
        Return r
    End Function
    Public Function validatePoint2f(p As cv.Point2f) As cv.Point2f
        If p.X < 0 Then p.X = 0
        If p.Y < 0 Then p.Y = 0
        If p.X > dst1.Width Then p.X = dst1.Width - 1
        If p.Y > dst1.Height Then p.Y = dst1.Height - 1
        Return p
    End Function
    Public Function MatToNumPyFloat(mat As cv.Mat) As NDarray
        Dim array(mat.Total - 1) As Single
        Marshal.Copy(mat.Data, array, 0, array.Length)
        Dim ndarray = Numpy.np.asarray(Of Single)(array)
        Return ndarray
    End Function
    Public Sub NumPyFloatToMat(array As NDarray, ByRef mat As cv.Mat)
        Marshal.Copy(array.GetData(Of Single), 0, mat.Data, mat.Total)
    End Sub
    Public Sub New()
        src = New cv.Mat(colorRows, colorCols, cv.MatType.CV_8UC3, 0)
        dst1 = New cv.Mat(colorRows, colorCols, cv.MatType.CV_8UC3, 0)
        dst2 = New cv.Mat(colorRows, colorCols, cv.MatType.CV_8UC3, 0)
        algorithm = Me
        label1 = Me.GetType.Name
        For i = 0 To rColors.Length - 1
            rColors(i) = New cv.Vec3b(msRNG.Next(100, 255), msRNG.Next(100, 255), msRNG.Next(100, 255))
            scalarColors(i) = New cv.Scalar(rColors(i).Item0, rColors(i).Item1, rColors(i).Item2)
        Next
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
        If standalone Then src = ocvb.color
        If ocvb.drawRect.Width <> 0 Then ocvb.drawRect = validateRect(ocvb.drawRect)
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
    Public Sub Dispose() Implements IDisposable.Dispose
        On Error Resume Next
        Dim proc = Process.GetProcessesByName("python")
        For i = 0 To proc.Count - 1
            proc(i).Kill()
        Next i
        If pyStream IsNot Nothing Then pyStream.Dispose()
        Console.WriteLine("The following System.MissingMemberException is harmless if algorithm has no 'Close' method.")
        algorithm.Close()  ' Close any unmanaged classes...
        sliders.Dispose()
        check.Dispose()
        radio1.Dispose()
        combo.Dispose()
    End Sub
End Class
