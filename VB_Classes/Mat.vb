Imports cv = OpenCvSharp
Public Class Mat_Repeat : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use the repeat method to replicate data."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim small = ocvb.color.Resize(New cv.Size(ocvb.color.Cols / 10, ocvb.color.Rows / 10))
        ocvb.result1 = small.Repeat(10, 10)
        small = ocvb.depthRGB.Resize(New cv.Size(ocvb.color.Cols / 10, ocvb.color.Rows / 10))
        ocvb.result2 = small.Repeat(10, 10)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class



Public Class Mat_PointToMat : Implements IDisposable
    Dim mask As Random_Points
    Public Sub New(ocvb As AlgorithmData)
        mask = New Random_Points(ocvb)
        ocvb.desc = "Convert pointf3 into a mat of points."
        ocvb.label1 = "Random_Points points (original)"
        ocvb.label2 = "Random_Points points after format change"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        mask.Run(ocvb) ' generates a set of points
        Dim rows = mask.Points.Length
        Dim pMat = New cv.Mat(rows, 1, cv.MatType.CV_32SC2, mask.Points)
        Dim indexer = pMat.GetGenericIndexer(Of cv.Vec2i)()
        ocvb.result2.SetTo(0)
        Dim white = New cv.Vec3b(255, 255, 255)
        For i = 0 To rows - 1
            ocvb.result2.Set(Of cv.Vec3b)(indexer(i).Item1, indexer(i).Item0, white)
        Next
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        mask.Dispose()
    End Sub
End Class



Public Class Mat_MatToPoint : Implements IDisposable
    Dim mask As Random_Points
    Public Sub New(ocvb As AlgorithmData)
        mask = New Random_Points(ocvb)
        ocvb.desc = "Convert a mat into a vector of points."
        ocvb.label1 = "Reconstructed RGB Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim points(ocvb.color.Rows * ocvb.color.Cols - 1) As cv.Vec3b
        Dim vec As New cv.Vec3b
        Dim index As Int32 = 0
        Dim m3b As New cv.MatOfByte3(ocvb.color)
        Dim indexer = m3b.GetGenericIndexer(Of cv.Vec3b)()
        For y = 0 To ocvb.color.Rows - 1
            For x = 0 To ocvb.color.Cols - 1
                vec = indexer(y, x)
                points(index) = New cv.Vec3b(vec.Item0, vec.Item1, vec.Item2)
                index += 1
            Next
        Next
        ocvb.result1 = New cv.Mat(ocvb.color.Rows, ocvb.color.Cols, cv.MatType.CV_8UC3, points)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        mask.Dispose()
    End Sub
End Class



Public Class Mat_Transpose : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Transpose a Mat and show results."
        ocvb.label1 = "Color Image Transposed"
        ocvb.label2 = "Color Image Transposed Twice"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim transpose = ocvb.color.T()
        Dim newSize = New cv.Size(ocvb.color.Cols, ocvb.color.Rows)
        ocvb.result1 = transpose.Resize(newSize)
        transpose = ocvb.result1.T()
        ocvb.result2 = transpose.Resize(newSize)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class



' https://csharp.hotexamples.com/examples/OpenCvSharp/Mat/-/php-mat-class-examples.html#0x95f170f4714e3258c220a78eacceeee99591440b9885a2997bbbc6b3aebdcf1c-19,,37,
Public Class Mat_Tricks : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Show some Mat tricks."
        ocvb.label1 = "Image squeezed into square Mat"
        ocvb.label2 = "Mat transposed around the diagonal"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mat = ocvb.color.Resize(New cv.Size(200, 200))
        ocvb.result1.SetTo(0)
        Dim x = 40
        Dim y = 80
        ocvb.result1(x, x + mat.Width, y, y + mat.Height) = mat

        ocvb.result2.SetTo(0)
        x = 20
        y = 40
        ocvb.result2(x, x + mat.Width, y, y + mat.Height) = mat.T
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class Mat_4to1 : Implements IDisposable
    Dim mat1 As cv.Mat
    Dim mat2 As cv.Mat
    Dim mat3 As cv.Mat
    Dim mat4 As cv.Mat
    Public mat() As cv.Mat = {mat1, mat2, mat3, mat4}
    Public externalUse As Boolean
    Public noLines As Boolean ' if they want lines or not...
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = ""
        mat1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC3, 0)
        mat2 = mat1.Clone()
        mat3 = mat1.Clone()
        mat4 = mat1.Clone()
        mat = {mat1, mat2, mat3, mat4}
        ocvb.desc = "Use one Mat for up to 4 images"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static nSize = New cv.Size(ocvb.color.Width / 2, ocvb.color.Height / 2)
        Static roiTopLeft = New cv.Rect(0, 0, nSize.Width, nSize.Height)
        Static roiTopRight = New cv.Rect(nSize.Width, 0, nSize.Width, nSize.Height)
        Static roibotLeft = New cv.Rect(0, nSize.Height, nSize.Width, nSize.Height)
        Static roibotRight = New cv.Rect(nSize.Width, nSize.Height, nSize.Width, nSize.Height)
        If externalUse = False Then
            mat1 = ocvb.color
            mat2 = ocvb.depthRGB
            mat3 = ocvb.redLeft.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            mat4 = ocvb.redRight.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            mat = {mat1, mat2, mat3, mat4}
        End If
        If mat(0).Channels <> ocvb.result2.Channels Then ocvb.result2 = New cv.Mat(ocvb.result2.Size(), mat(0).Type, 0)
        For i = 0 To 3
            Dim roi = Choose(i + 1, roiTopLeft, roiTopRight, roibotLeft, roibotRight)
            ocvb.result2(roi) = mat(i).Resize(nSize)
        Next
        If noLines = False Then
            ocvb.result2.Line(New cv.Point(0, ocvb.result2.Height / 2), New cv.Point(ocvb.result2.Width, ocvb.result2.Height / 2), cv.Scalar.White, 2)
            ocvb.result2.Line(New cv.Point(ocvb.result2.Width / 2, 0), New cv.Point(ocvb.result2.Width / 2, ocvb.result2.Height), cv.Scalar.White, 2)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class Mat_2to1 : Implements IDisposable
    Dim mat1 As cv.Mat
    Dim mat2 As cv.Mat
    Public mat() = {mat1, mat2}
    Public externalUse As Boolean
    Public noLines As Boolean ' if they want lines or not...
    Public Sub New(ocvb As AlgorithmData)
        ocvb.label1 = ""
        mat1 = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8UC3, 0)
        mat2 = mat1.Clone()
        mat = {mat1, mat2}
        ocvb.desc = "Use one Mat for up to 2 images"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static nSize = New cv.Size(ocvb.color.Width, ocvb.color.Height / 2)
        Static roiTop = New cv.Rect(0, 0, nSize.Width, nSize.Height)
        Static roibot = New cv.Rect(0, nSize.Height, nSize.Width, nSize.Height)
        If externalUse = False Then
            mat1 = ocvb.color
            mat2 = ocvb.depthRGB
            mat = {mat1, mat2}
        End If
        For i = 0 To 1
            Dim roi = Choose(i + 1, roiTop, roibot)
            ocvb.result2(roi) = mat(i).Resize(nSize)
        Next
        If noLines = False Then
            ocvb.result2.Line(New cv.Point(0, ocvb.result2.Height / 2), New cv.Point(ocvb.result2.Width, ocvb.result2.Height / 2), cv.Scalar.White, 2)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class Mat_ImageXYZ_MT : Implements IDisposable
    Dim grid As Thread_Grid
    Public xyDepth As cv.Mat
    Public xyzPlanes() As cv.Mat
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        grid = New Thread_Grid(ocvb)
        grid.sliders.TrackBar1.Value = 32
        grid.sliders.TrackBar2.Value = 32


        xyDepth = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32FC3, 0)
        Dim xyz As New cv.Point3f
        For xyz.Y = 0 To xyDepth.Height - 1
            For xyz.X = 0 To xyDepth.Width - 1
                xyDepth.Set(Of cv.Point3f)(xyz.Y, xyz.X, xyz)
            Next
        Next
        cv.Cv2.Split(xyDepth, xyzPlanes)
        ocvb.desc = "Create a cv.Point3f vector with x, y, and z."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        grid.Run(ocvb)
        Parallel.ForEach(Of cv.Rect)(grid.roiList,
          Sub(roi)
              Dim z As Single
              xyzPlanes(2)(roi).SetTo(0)
              For y = roi.Y To roi.Y + roi.Height - 1
                  For x = roi.X To roi.X + roi.Width - 1
                      z = ocvb.depth.At(Of UInt16)(y, x)
                      If z > 0 Then xyzPlanes(2).Set(Of Single)(y, x, z)
                  Next
              Next
          End Sub)

        If externalUse = False Then cv.Cv2.Merge(xyzPlanes, xyDepth)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        grid.Dispose()
    End Sub
End Class
