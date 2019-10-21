Imports cv = OpenCvSharp
Public Class Remap_Basics : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use remap to reflect an image in 4 directions."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim map_x = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32F)
        Dim map_y = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32F)
        Static Dim direction = 0

        Dim src As cv.Mat = ocvb.color
        ' build a map for use with remap!
        For j = 0 To map_x.Rows - 1
            For i = 0 To map_x.Cols - 1
                Select Case direction
                    Case 0
                        If i > src.Cols * 0.25 And i < src.Cols * 0.75 And j > src.Rows * 0.25 And j < src.Rows * 0.75 Then
                            map_x.Set(Of Single)(j, i, 2 * (i - src.Cols * 0.25) + 0.5)
                            map_y.Set(Of Single)(j, i, 2 * (j - src.Rows * 0.25) + 0.5)
                        Else
                            map_x.Set(Of Single)(j, i, 0)
                            map_y.Set(Of Single)(j, i, 0)
                        End If
                    Case 1
                        map_x.Set(Of Single)(j, i, i)
                        map_y.Set(Of Single)(j, i, src.Rows - j)
                    Case 2
                        map_x.Set(Of Single)(j, i, src.Cols - i)
                        map_y.Set(Of Single)(j, i, j)
                    Case 3
                        map_x.Set(Of Single)(j, i, src.Cols - i)
                        map_y.Set(Of Single)(j, i, src.Rows - j)
                End Select
            Next
        Next

        cv.Cv2.Remap(ocvb.color, ocvb.result1, map_x, map_y)

        If ocvb.frameCount Mod 10 = 0 Then
            direction += 1
            direction = direction Mod 4
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class Remap_Flip : Implements IDisposable
    Public direction = 0
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Use flip to remap an image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Select Case direction
            Case 0 ' flip horizontally
                cv.Cv2.Flip(ocvb.color, ocvb.result1, cv.FlipMode.Y)
            Case 1 ' flip vertically
                cv.Cv2.Flip(ocvb.color, ocvb.result1, cv.FlipMode.X)
            Case 2 ' flip horizontally and vertically
                cv.Cv2.Flip(ocvb.color, ocvb.result1, cv.FlipMode.XY)
            Case 3 ' do nothing!
                ocvb.result1.CopyTo(ocvb.result1)
        End Select
        If ocvb.frameCount Mod 10 = 0 Then
            direction += 1
            direction = direction Mod 4
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

