Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module MinTriangle_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub MinTriangle_Run(inputPtr As IntPtr, numberOfPoints As Int32, outputTriangle As IntPtr)
    End Sub
End Module

Public Class Area_MinTriangle_CPP
    Inherits ocvbClass
    Dim numberOfPoints As Int32
    Public srcPoints() As cv.Point2f
    Public srcData() As Byte
    Public dstData() As Byte
    Public triangle As cv.Mat
    Private Sub setup(ocvb As AlgorithmData)
        numberOfPoints = sliders.trackbar(0).Value
        ReDim srcPoints(numberOfPoints)
        ReDim srcData(numberOfPoints * Marshal.SizeOf(numberOfPoints) * 2 - 1) ' input is a list of points.
        ReDim dstData(3 * Marshal.SizeOf(numberOfPoints) * 2 - 1) ' minTriangle returns 3 points
    End Sub
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Area Number of Points", 1, 30, 5)
        sliders.setupTrackBar(1, "Area size", 10, 300, 200)
        setup(ocvb)
        ocvb.desc = "Find minimum containing triangle for a set of points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static pointCountSlider = findSlider("Area Number of Points")
        Static sizeSlider = findSlider("Area size")
        If numberOfPoints <> pointCountSlider.Value Then setup(ocvb)
        Dim squareWidth = sizeSlider.Value / 2

        dst1.SetTo(0)
        For i = 0 To srcPoints.Length - 1
            srcPoints(i).X = msRNG.Next(src.Width / 2 - squareWidth, src.Width / 2 + squareWidth)
            srcPoints(i).Y = msRNG.Next(src.Height / 2 - squareWidth, src.Height / 2 + squareWidth)
            dst1.Circle(srcPoints(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        Dim input As New cv.Mat(numberOfPoints, 1, cv.MatType.CV_32FC2, srcPoints)
        Marshal.Copy(input.Data, srcData, 0, srcData.Length)
        Dim srcHandle = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim dstHandle = GCHandle.Alloc(dstData, GCHandleType.Pinned)
        MinTriangle_Run(srcHandle.AddrOfPinnedObject(), numberOfPoints, dstHandle.AddrOfPinnedObject)
        srcHandle.Free() ' free the pinned memory...
        dstHandle.Free()
        triangle = New cv.Mat(3, 1, cv.MatType.CV_32FC2, dstData)

        For i = 0 To 2
            Dim p1 = triangle.Get(Of cv.Point2f)(i)
            Dim p2 = triangle.Get(Of cv.Point2f)((i + 1) Mod 3)
            dst1.Line(p1, p2, cv.Scalar.White, 2, cv.LineTypes.AntiAlias)
        Next
    End Sub
End Class




Public Class Area_MinRect
    Inherits ocvbClass
    Dim numberOfPoints As Integer
    Public srcPoints() As cv.Point2f
    Public minRect As cv.RotatedRect
    Private Sub setup(ocvb As AlgorithmData, _numberOfPoints As Integer)
        numberOfPoints = _numberOfPoints
        ReDim srcPoints(numberOfPoints)
    End Sub
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Area Number of Points", 1, 30, 5)
        sliders.setupTrackBar(1, "Area size", 10, 300, 200)

        setup(ocvb, sliders.trackbar(0).Value)

        ocvb.desc = "Find minimum containing rectangle for a set of points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static pointCountSlider = findSlider("Area Number of Points")
        Static sizeSlider = findSlider("Area size")
        If numberOfPoints <> pointCountSlider.Value Then setup(ocvb, pointCountSlider.value)
        Dim squareWidth = sizeSlider.Value / 2

        dst1.SetTo(0)
        For i = 0 To srcPoints.Length - 1
            srcPoints(i).X = msRNG.Next(src.Width / 2 - squareWidth, src.Width / 2 + squareWidth)
            srcPoints(i).Y = msRNG.Next(src.Height / 2 - squareWidth, src.Height / 2 + squareWidth)
            dst1.Circle(srcPoints(i), 3, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        minRect = cv.Cv2.MinAreaRect(srcPoints)
        drawRotatedRectangle(minRect, dst1, cv.Scalar.Yellow)
    End Sub
End Class





Public Class Area_MinMotionRect
    Inherits ocvbClass
    Dim input As BGSubtract_MOG
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        input = New BGSubtract_MOG(ocvb)
        input.sliders.trackbar(0).Value = 100 ' low threshold to maximize motion
        ocvb.desc = "Use minRectArea to encompass detected motion"
        label1 = "MinRectArea of MOG motion"
    End Sub

    Private Function motionRectangles(gray As cv.Mat, rColors() As cv.Vec3b) As cv.Mat
        Dim contours As cv.Point()()
        contours = cv.Cv2.FindContoursAsArray(gray, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)

        For i = 0 To contours.Length - 1
            Dim minRect = cv.Cv2.MinAreaRect(contours(i))
            Dim nextColor = New cv.Scalar(rColors(i Mod 255).Item0, rColors(i Mod 255).Item1, rColors(i Mod 255).Item2)
            drawRotatedRectangle(minRect, gray, nextColor)
        Next
        Return gray
    End Function
    Public Sub Run(ocvb As AlgorithmData)
        input.src = src
        input.Run(ocvb)
        Dim gray As cv.Mat
        If input.dst1.Channels = 1 Then gray = input.dst1 Else gray = input.dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = motionRectangles(gray, rColors)
        dst1.SetTo(cv.Scalar.All(255), gray)
    End Sub
End Class







Public Class Area_FindNonZero
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        label1 = "Non-zero original points"
        label2 = "Coordinates of non-zero points"
        ocvb.desc = "Use FindNonZero API to get coordinates of non-zero points."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim gray = New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
        Dim srcPoints(10 - 1) As cv.Point ' doesn't really matter how many there are.
        For i = 0 To srcPoints.Length - 1
            srcPoints(i).X = msRNG.Next(0, src.Width)
            srcPoints(i).Y = msRNG.Next(0, src.Height)
            gray.Set(Of Byte)(srcPoints(i).Y, srcPoints(i).X, 255)
        Next

        Dim nonzero = gray.FindNonZero()

        dst1 = gray.EmptyClone().SetTo(0)
        ' mark the points so they are visible...
        For i = 0 To srcPoints.Length - 1
            dst1.Circle(srcPoints(i), 5, cv.Scalar.White, -1, cv.LineTypes.AntiAlias)
        Next

        Dim outstr As String = "Coordinates of the non-zero points (ordered by row - top to bottom): " + vbCrLf + vbCrLf
        For i = 0 To srcPoints.Length - 1
            Dim pt = nonzero.Get(Of cv.Point)(0, i)
            outstr += "X = " + vbTab + CStr(pt.X) + vbTab + " y = " + vbTab + CStr(pt.Y) + vbCrLf
        Next
        ocvb.putText(New TTtext(outstr, 10, 50, RESULT2))
    End Sub
End Class
