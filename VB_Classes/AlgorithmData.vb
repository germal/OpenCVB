Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class AlgorithmData
    Public name As String
    Public desc As String
    Public optionsTop As Int32
    Public optionsLeft As Int32
    Public color As New cv.Mat
    Public depth As New cv.Mat
    Public disparity As New cv.Mat
    Public depthRGB As New cv.Mat
    Public pointCloud As New cv.Mat
    Public redLeft As New cv.Mat
    Public redRight As New cv.Mat
    Public result1 As New cv.Mat
    Public result2 As New cv.Mat
    Public drawRect As cv.Rect ' filled in if the user draws on any of the images.
    Public drawRectClear As Boolean ' used to remove the drawing rectangle when it has been used to initialize a camshift or mean shift.
    Public mousePoint As cv.Point ' trace any mouse movements using this.
    Public mouseClickFlag As Boolean
    Public mouseClickPoint As cv.Point
    Public mousePicTag As Int32 ' which image was the mouse in?
    Public rng = New cv.RNG()
    Public ms_rng As System.Random
    Public rColors(255) As cv.Vec3b
    Public label1 As String
    Public label2 As String
    Public frameCount As Int32 = 0
    Public PythonFileName As String
    Public PythonExe As String
    Public TTtextData(3) As List(Of ActiveClass.TrueType)
    Public fontName As String
    Public fontSize As Int32
    Public bestOpenCVFont = cv.HersheyFonts.HersheyComplex
    Public bestOpenCVFontSize = 1.5
    Public openGLWidth = 1500
    Public openGLHeight = 1200
    Public parms As ActiveClass.algorithmParameters
    Public Sub New(parms As ActiveClass.algorithmParameters)
        optionsTop = parms.mainFormLoc.Y + parms.mainFormHeight
        optionsLeft = parms.mainFormLoc.X
        If parms.width < 1000 Then bestOpenCVFontSize = 0.5 ' this is better for the smaller resolutions.
        color = New cv.Mat(parms.height, parms.width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        depth = New cv.Mat(parms.height, parms.width, cv.MatType.CV_16U, cv.Scalar.All(0))
        depthRGB = New cv.Mat(parms.height, parms.width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        result1 = New cv.Mat(parms.height, parms.width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        result2 = New cv.Mat(parms.height, parms.width, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        label1 = ""
        label2 = ""
        rng = New cv.RNG
        ms_rng = New System.Random
        For i = 0 To rColors.Length - 1
            rColors(i) = New cv.Vec3b(rng.uniform(0, 255), rng.uniform(0, 255), rng.uniform(0, 255))
            colorScalar(i) = New cv.Scalar(rng.uniform(0, 255), rng.uniform(0, 255), rng.uniform(0, 255))
        Next
        For i = 0 To TTtextData.Count - 1
            TTtextData(i) = New List(Of ActiveClass.TrueType)
        Next
        fontSize = GetSetting("OpenCVB", "FontSize", "FontSize", 12)
        fontName = GetSetting("OpenCVB", "FontName", "FontName", "Tahoma")
    End Sub
    Public Sub vtkInstructions()
        putText(New ActiveClass.TrueType("VTK support is disabled.  Here are the steps needed to enable VTK.", 10, 125))
        putText(New ActiveClass.TrueType("Step 1) build VTK with <OpenCVB_Home_Directory>/VTK/Build/VTK.sln", 10, 145))
        putText(New ActiveClass.TrueType("Step 2) Reconfigure OpenCV with CMake - With_VTK and VTK_Dir.", 10, 185))
        putText(New ActiveClass.TrueType("Step 3) Add the VTK_Apps/DataExample to OpenCVB.sln'", 10, 205))
        putText(New ActiveClass.TrueType("Step 4) Update the Project Dependencies for VB_Classes to include the VTK_Apps/DataExample project", 10, 225))
        putText(New ActiveClass.TrueType("Step 5) Rebuild OpenCVB and it will find VTK and OpenCV's VIZ DLL", 10, 245))
    End Sub
    Public Sub putText(tt As ActiveClass.TrueType)
        TTtextData(tt.picTag).Add(tt)
    End Sub
End Class
