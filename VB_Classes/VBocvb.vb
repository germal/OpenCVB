Imports cv = OpenCvSharp

Public Class VBocvb
    Public frameCount As Integer = 0
    Public quadrantIndex As Integer = 0
    Public parms As ActiveTask.algParms

    Public algorithmIndex As Integer
    Public parentRoot As String
    Public parentAlgorithm As String

    Public fixedColors(255) As cv.Scalar

    Public scalarColors(255) As cv.Scalar
    Public vecColors(255) As cv.Vec3b
    Public topCameraPoint As cv.Point
    Public sideCameraPoint As cv.Point
    Public fontSize As Single
    Public dotSize As Integer
    Public lineSize As Integer
    Public resfactor As Single ' resolution is often a factor in sizing tasks.
    Public resolutionIndex As Integer
    Public Const MAXZ_DEFAULT = 4
    Public maxZ As Single = MAXZ_DEFAULT
    Public pixelsPerMeterH As Single
    Public pixelsPerMeterV As Single
    Public hFov As Single
    Public vFov As Single
    Public angleX As Single  ' rotation angle in radians around x-axis to align with gravity
    Public angleY As Single  ' this angle is only used manually - no IMU connection.
    Public angleZ As Single  ' rotation angle in radians around z-axis to align with gravity
    Public cz As Single
    Public sz As Single
    Public gMat As cv.Mat
    Public useIMU As Boolean = True
    Public imuXAxis As Boolean
    Public imuZAxis As Boolean

    Public intermediateObject As VBparent

    Public label1 As String
    Public label2 As String

    Public pythonTaskName As String
    Public Sub New(_task As ActiveTask)
        Select Case _task.color.Width
            Case 320
                fontSize = _task.color.Width / _task.pointCloud.Width
                dotSize = 3
                lineSize = 1
                resfactor = 0.1
                resolutionIndex = 1
            Case 640
                fontSize = _task.color.Width / _task.pointCloud.Width
                dotSize = 7
                lineSize = 2
                resfactor = 0.3
                resolutionIndex = 2
            Case 1280
                fontSize = 1
                dotSize = 15
                lineSize = 4
                resfactor = 1
                resolutionIndex = 3
        End Select
    End Sub
    Public Sub trueText(text As String, Optional x As Integer = 10, Optional y As Integer = 40, Optional picTag As Integer = 2)
        Dim str As New TTtext(text, x, y, picTag)
        task.TTtextData.Add(str)
    End Sub
    Public Sub trueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New TTtext(text, pt.X, pt.Y, picTag)
        task.TTtextData.Add(str)
    End Sub
End Class
