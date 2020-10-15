#if USE_NUMPY Then
Imports Numpy
#End If
Imports System.Windows.Forms
Imports cv = OpenCvSharp
Imports System.IO
Public Class TTtext
    Public text As String
    Public picTag = 2
    Public x As Integer
    Public y As Integer
    Private Sub setup(_text As String, _x As Integer, _y As Integer, camPicIndex As Integer)
        text = _text
        x = _x
        y = _y
        picTag = camPicIndex
    End Sub
    Public Sub New(_text As String, _x As Integer, _y As Integer, camPicIndex As Integer)
        setup(_text, _x, _y, camPicIndex)
    End Sub
    Public Sub New(_text As String, _x As Integer, _y As Integer)
        setup(_text, _x, _y, 2)
    End Sub
End Class
Public Class VBparent : Implements IDisposable
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
    Dim algorithm As Object
    Public fontsize As Single
    Public resFactor As Single ' resolution is often a factor in sizing tasks.
    Public caller As String
    Public topCameraPoint As cv.Point
    Public sideCameraPoint As cv.Point
    Public Const MAXZ_DEFAULT = 4
    Public maxZ As Single = MAXZ_DEFAULT
    Public desc As String
    Dim callStack = ""
    Public Sub initParent(ocvb As VBocvb)
        If ocvb.callTrace.Count = 0 Then
            standalone = True
            ocvb.callTrace.Clear()
            ocvb.callTrace.Add(callStack)
        Else
            standalone = False
            If ocvb.callTrace.Contains(callStack) = False Then ocvb.callTrace.Add(callStack)
        End If

        src = New cv.Mat(ocvb.color.Size, cv.MatType.CV_8UC3, 0)
        dst1 = New cv.Mat(ocvb.color.Size, cv.MatType.CV_8UC3, 0)
        dst2 = New cv.Mat(ocvb.color.Size, cv.MatType.CV_8UC3, 0)
        Select Case ocvb.color.Width
            Case 320
                fontsize = ocvb.color.Width / 1280
                resFactor = 0.1
            Case 640
                fontsize = ocvb.color.Width / 1280
                resFactor = 0.3
            Case 1280
                fontsize = 1
                resFactor = 1
        End Select

        topCameraPoint = New cv.Point(CInt(src.Height), CInt(src.Height))
        sideCameraPoint = New cv.Point(CInt((src.Width - src.Height) / 2), CInt(src.Height - (src.Width - src.Height) / 2))
    End Sub
    Public Const QUAD0 = 0 ' there are 4 images to the user interface when using Mat_4to1.
    Public Const QUAD1 = 1
    Public Const QUAD2 = 2
    Public Const QUAD3 = 3
    Public Sub setQuadrant(ocvb As VBocvb)
        If ocvb.mouseClickFlag Then
            Dim pt = ocvb.mouseClickPoint
            If pt.Y < src.Height / 2 Then
                If pt.X < src.Width / 2 Then ocvb.quadrantIndex = QUAD0 Else ocvb.quadrantIndex = QUAD1
            Else
                If pt.X < src.Width / 2 Then ocvb.quadrantIndex = QUAD2 Else ocvb.quadrantIndex = QUAD3
            End If
        End If
    End Sub
    Public Function findCheckBox(opt As String) As CheckBox
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" CheckBox Options") Then
                        For i = 0 To frm.Box.length - 1
                            If frm.box(i).text.contains(opt) Then Return frm.box(i)
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findCheckBox failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                MsgBox("A checkbox was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request in '" + vbCrLf + vbCrLf + "'" + caller + "'")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Public Function findRadio(opt As String) As RadioButton
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" Radio Options") Then
                        For i = 0 To frm.check.length - 1
                            If frm.check(i).text.contains(opt) Then Return frm.check(i)
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findRadio failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                MsgBox("A findRadio was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request in '" + vbCrLf + vbCrLf + "'" + caller + "'")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Public Sub hideForm(title As String)
        For Each frm In Application.OpenForms
            If frm.text = title Then
                frm.hide
                Exit Sub
            End If
        Next
    End Sub
    Public Function findForm(title As String) As Object
        For Each frm In Application.OpenForms
            If frm.text = title Then Return frm
        Next
        Return Nothing
    End Function
    Public Function findSlider(opt As String) As TrackBar
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" Slider Options") Then
                        For i = 0 To frm.trackbar.length - 1
                            If frm.sLabels(i).text.contains(opt) Then Return frm.trackbar(i)
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findSlider failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                MsgBox("A slider was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request in '" + vbCrLf + vbCrLf + "'" + caller + "'")
                Exit While
            End If
        End While

        Return Nothing
    End Function
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
#If USE_NUMPY Then
    Public Function MatToNumPyFloat(mat As cv.Mat) As NDarray
        Dim array(mat.Total - 1) As Single
        Marshal.Copy(mat.Data, array, 0, array.Length)
        Dim ndarray = Numpy.np.asarray(Of Single)(array)
        Return ndarray
    End Function
    Public Sub NumPyFloatToMat(array As NDarray, ByRef mat As cv.Mat)
        Marshal.Copy(array.GetData(Of Single), 0, mat.Data, mat.Total)
    End Sub
#End If
    Public Sub New()
        algorithm = Me
        caller = Me.GetType.Name
        label1 = caller
        Dim stackTrace = Environment.StackTrace
        Dim lines() = stackTrace.Split(vbCrLf)
        For i = 0 To lines.Count - 1
            lines(i) = Trim(lines(i))
            Dim offset = InStr(lines(i), "VB_Classes.")
            If offset > 0 Then
                Dim partLine = Mid(lines(i), offset + 11)
                If partLine.StartsWith("algorithmList.createAlgorithm") Then Exit For
                Dim split() = partLine.Split("\")
                partLine = Mid(partLine, 1, InStr(partLine, ".") - 1)
                If Not (partLine.StartsWith("VBparent") Or partLine.StartsWith("ActiveTask")) Then
                    callStack = partLine + "\" + callStack
                End If
            End If
        Next
    End Sub
    Private Function MakeSureImage8uC3(ByVal input As cv.Mat) As cv.Mat
        If input.Type = cv.MatType.CV_32F Then
            ' it must be a 1 channel 32f image so convert it to 8-bit and let it get converted to RGB below
            input = input.Normalize(0, 255, cv.NormTypes.MinMax)
            input.ConvertTo(input, cv.MatType.CV_8UC1)
        End If
        If input.Channels = 1 And input.Type = cv.MatType.CV_8UC1 Then
            input = input.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If
        Return input
    End Function
    Public Sub NextFrame(ocvb As VBocvb)
        If standalone Then src = ocvb.color
        If src.Width <> dst1.Width Or src.Width <> dst2.Width Then
            dst1 = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
            dst2 = New cv.Mat(src.Size(), cv.MatType.CV_8UC3)
        End If
        If ocvb.drawRect.Width <> 0 Then ocvb.drawRect = validateRect(ocvb.drawRect)
        algorithm.Run(ocvb)
        If standalone And src.Width > 0 Then
            'If dst1.Width = ocvb.result.Width Then
            '    ocvb.result = dst1.Clone()
            'Else
            If dst1.Width <> src.Width Then dst1 = dst1.Resize(New cv.Size(src.Width, src.Height))
            If dst2.Width <> src.Width Then dst2 = dst2.Resize(New cv.Size(src.Width, src.Height))
            If ocvb.result.Width <> dst1.Width * 2 Or ocvb.result.Height <> dst1.Height Then
                ocvb.result = New cv.Mat(New cv.Size(dst1.Width * 2, dst1.Height), cv.MatType.CV_8UC3)
            End If
            ocvb.result(New cv.Rect(0, 0, src.Width, src.Height)) = MakeSureImage8uC3(dst1)
            ocvb.result(New cv.Rect(src.Width, 0, src.Width, src.Height)) = MakeSureImage8uC3(dst2)
            'End If
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
        Console.WriteLine("The following System.MissingMemberException indicates the algorithm has no 'Close' method (harmless notification).")
        algorithm.Close()  ' Close any unmanaged classes...
        sliders.Dispose()
        check.Dispose()
        radio.Dispose()
        radio1.Dispose()
        combo.Dispose()
    End Sub
End Class
