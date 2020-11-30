Imports System.Threading
Imports System.IO
Imports SharpAvi
Imports SharpAvi.Codecs
Imports SharpAvi.Output
Public Class appCapture
    Dim picCount As Integer
    Dim myLock As New Mutex(True, "myLock")
    Dim startTime As DateTime
    Dim duration As Integer
    Dim codec As FourCC
    Dim writer As AviWriter
    Dim videoStream As IAviVideoStream
    Dim screenThread As Thread
    Dim threadStop As Boolean
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If IsNumeric(TextBox1.Text) = False Then TextBox1.Text = "5"
        If Button1.Text = "Start" Then
            duration = CInt(TextBox1.Text)
            startTime = Now()
            Button1.Text = "Stop"
            Timer1.Interval = 1000 * duration
            Timer1.Enabled = True

            threadStop = False
            writer = New AviWriter(fileName.Text)
            videoStream = writer.AddMotionJpegVideoStream(Width, Height)
            screenThread = New Thread(AddressOf RecordScreen)
            screenThread.Name = "RecordStream"
            screenThread.IsBackground = True
            screenThread.Start()
        Else
            threadStop = True
            screenThread.Join()
            writer.Close()
            Timer1.Enabled = False

            Button1.Text = "Start"
            Dim diff = Now().Subtract(startTime)
            Me.Text = "Captured " + CStr(picCount) + " frames in " + Format(diff.TotalMilliseconds / 1000, "#0.0") + " seconds.  FPS = " + Format(picCount / diff.TotalSeconds, "#0.0")
        End If
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        threadStop = True
    End Sub
    Private Sub appCapture_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        codec = KnownFourCCs.Codecs.MotionJpeg
        Dim fileinfo As New FileInfo(Application.StartupPath + "/../../../Video/appCapture.avi")
        fileName.Text = fileinfo.FullName
    End Sub
    Private Sub RecordScreen()
        Dim w = Width
        Dim h = Height
        Dim buffer(w * h * 4) As Byte
        Dim bmp = New Bitmap(w, h)
        Dim videoWriteTask As Task = Nothing
        While 1
            Application.DoEvents()
            Dim g = Graphics.FromImage(bmp)
            g.CopyFromScreen(Point.Empty, Point.Empty, New Drawing.Size(w, h), CopyPixelOperation.SourceCopy)
            videoWriteTask?.Wait()
            videoWriteTask = videoStream.WriteFrameAsync(True, buffer, 0, buffer.Length)
            picCount += 1
            If threadStop = True Then Exit While
        End While
        videoWriteTask?.Wait()
    End Sub
End Class
