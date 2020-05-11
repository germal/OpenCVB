Imports cv = OpenCvSharp
Public Class Remap_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Use remap to reflect an image in 4 directions."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim map_x = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32F)
        Dim map_y = New cv.Mat(ocvb.color.Size(), cv.MatType.CV_32F)
        Static Dim direction = 0

        ocvb.label1 = Choose(direction + 1, "Remap_Basics - original", "Remap veritically", "Remap horizontally",
                                            "Remap horizontally and vertically")

        Dim src As cv.Mat = ocvb.color
        ' build a map for use with remap!
        For j = 0 To map_x.Rows - 1
            For i = 0 To map_x.Cols - 1
                Select Case direction
                    Case 0 ' leave the original unmapped!
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

        If direction <> 0 Then cv.Cv2.Remap(ocvb.color, dst1, map_x, map_y) Else dst1 = ocvb.color

        If ocvb.frameCount Mod 30 = 0 Then
            direction += 1
            direction = direction Mod 4
        End If
    End Sub
End Class




Public Class Remap_Flip
    Inherits ocvbClass
    Public direction = 0
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Use flip to remap an image."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.label1 = Choose(direction + 1, "Remap_Flip - original", "Remap_Flip - flip horizontal", "Remap_Flip - flip veritical",
                                            "Remap_Flip - flip horizontal and vertical")

        Select Case direction
            Case 0 ' do nothing!
                ocvb.color.CopyTo(dst1)
            Case 1 ' flip vertically  
                cv.Cv2.Flip(ocvb.color, dst1, cv.FlipMode.Y)
            Case 2 ' flip horizontally
                cv.Cv2.Flip(ocvb.color, dst1, cv.FlipMode.X)
            Case 3 ' flip horizontally and vertically
                cv.Cv2.Flip(ocvb.color, dst1, cv.FlipMode.XY)
        End Select
        If ocvb.frameCount Mod 100 = 0 Then
            direction += 1
            direction = direction Mod 4
        End If
    End Sub
End Class


