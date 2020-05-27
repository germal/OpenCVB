Imports cv = OpenCvSharp
Public Class Mouse_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Test the mousePoint interface"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static lastPoint = New cv.Point
        ' we are only interested in mouse movements in Result1 (tag = 2)
        If lastPoint = ocvb.mousePoint Or ocvb.mousePicTag <> 2 Then Exit Sub
        lastPoint = ocvb.mousePoint
        Dim radius = 10
        Static colorIndex As Int32
        Dim nextColor = scalarColors(colorIndex)
        dst1.Circle(ocvb.mousePoint, radius, nextColor, -1, cv.LineTypes.AntiAlias)
        colorIndex += 1
        If colorIndex >= scalarColors.Count Then colorIndex = 0
        ocvb.putText(New ActiveClass.TrueType("Move the mouse through this image to show mouse tracking.", 10, 50))
    End Sub
End Class



Public Class Mouse_LeftClick
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Demonstrate what the left-click enables"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.putText(New ActiveClass.TrueType("Left-click and drag to select a region in any of the images." + vbCrLf +
                                              "The selected area is presented to ocvbClass in ocvb.drawRect." + vbCrLf +
                                              "In this example, the selected region from the RGB image will be resized to fit in the Result2 image to the right." + vbCrLf +
                                              "Double-click an image to remove the selected region.", 10, 50))

        Static zeroRect As New cv.Rect(0, 0, 0, 0)
        If ocvb.drawRect <> zeroRect Then dst2 = src(ocvb.drawRect).Resize(dst2.Size())
    End Sub
End Class




Public Class Mouse_RightClick
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData, ByVal callerRaw As String)
        setCaller(callerRaw)
        ocvb.desc = "Demonstrate what the right-click enables"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.putText(New ActiveClass.TrueType("Right-click and drag to select a region in one of the images." + vbCrLf +
                                              "The selected image data will be opened in a spreadsheet.  Give it a try!" + vbCrLf +
                                              "Double-click an image to remove the selected region.", 10, 50))
        Static zeroRect As New cv.Rect(0, 0, 0, 0)
        If ocvb.drawRect <> zeroRect Then dst2 = src(ocvb.drawRect).Resize(dst2.Size())
    End Sub
End Class

