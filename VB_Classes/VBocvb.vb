Imports cv = OpenCvSharp

Public Class VBocvb
    ' all the items here are used to communicate to/from the host user interface.  Other variables common to all algorithms should be ocvbClass.vb
    Public color As cv.Mat
    Public RGBDepth As cv.Mat
    Public result As New cv.Mat
    Public pointCloud As cv.Mat
    Public depth16 As cv.Mat
    Public leftView As cv.Mat
    Public rightView As cv.Mat

    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public frameCount As Integer = 0
    Public label1 As String
    Public label2 As String
    Public quadrantIndex As Integer = 0
    Public parms As ActiveTask.algParms

    Public mouseClickFlag As Boolean
    Public mouseClickPoint As cv.Point
    Public mousePicTag As Integer ' which image was the mouse in?
    Public mousePoint As cv.Point ' trace any mouse movements using this.

    Public PythonFileName As String
    Public TTtextData As List(Of TTtext)

    Public algorithmIndex As Integer
    Public parentRoot As String
    Public parentAlgorithm As String
    Public callTrace As New List(Of String)
    Public callObjects As New List(Of VBparent)

    Public transformationMatrix() As Single
    Public fixedColors(255) As cv.Scalar

    Public openFileDialogRequested As Boolean
    Public openFileInitialDirectory As String
    Public openFileFilter As String
    Public openFileFilterIndex As Integer
    Public openFileDialogName As String
    Public openFileDialogTitle As String
    Public openFileSliderPercent As Single
    Public fileStarted As Boolean
    Public initialStartSetting As Boolean

    Public IMU_Barometer As Single
    Public IMU_Magnetometer As cv.Point3f
    Public IMU_Temperature As Single
    Public IMU_TimeStamp As Double
    Public IMU_Rotation As System.Numerics.Quaternion
    Public IMU_Translation As cv.Point3f
    Public IMU_Acceleration As cv.Point3f
    Public IMU_Velocity As cv.Point3f
    Public IMU_AngularAcceleration As cv.Point3f
    Public IMU_AngularVelocity As cv.Point3f
    Public IMU_FrameTime As Double
    Public CPU_TimeStamp As Double
    Public CPU_FrameTime As Double
    Public scalarColors(255) As cv.Scalar
    Public vecColors(255) As cv.Vec3b
    Public desc As String
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
    Public intermediateReview As String
    Public intermediateObject As VBparent

    Public mainLocation As cv.Rect
    Public optionsOffset As Integer
    Public Sub New(resolution As cv.Size, parms As ActiveTask.algParms, location As cv.Rect, pointcloudWidth As Integer, pointcloudHeight As Integer)
        color = New cv.Mat(resolution.Height, resolution.Width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        RGBDepth = New cv.Mat(color.Size(), cv.MatType.CV_8UC3, cv.Scalar.All(0))
        pointCloud = New cv.Mat(pointcloudHeight, pointcloudWidth, cv.MatType.CV_32FC3, cv.Scalar.All(0))
        result = New cv.Mat(color.Height, color.Width * 2, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        TTtextData = New List(Of TTtext)

        Select Case color.Width
            Case 320
                fontSize = color.Width / pointCloud.Width
                dotSize = 3
                lineSize = 1
                resfactor = 0.1
                resolutionIndex = 1
            Case 640
                fontSize = color.Width / pointCloud.Width
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
        TTtextData.Add(str)
    End Sub
    Public Sub trueText(text As String, pt As cv.Point, Optional picTag As Integer = 2)
        Dim str As New TTtext(text, pt.X, pt.Y, picTag)
        TTtextData.Add(str)
    End Sub
End Class
