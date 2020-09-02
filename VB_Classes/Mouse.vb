Imports cv = OpenCvSharp
Public Class Mouse_Basics
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        label1 = "Move the mouse below to show mouse tracking."
        desc = "Test the mousePoint interface"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Static lastPoint = New cv.Point
        ' only display mouse movement in the lower left image (pic.tag = 2)
        If lastPoint = ocvb.mousePoint Or ocvb.mousePicTag <> 2 Then Exit Sub
        lastPoint = ocvb.mousePoint
        Dim radius = 10
        Static colorIndex As Int32
        Dim nextColor = scalarColors(colorIndex)
        Dim nextPt = ocvb.mousePoint
        dst1.Circle(nextPt, radius, nextColor, -1, cv.LineTypes.AntiAlias)
        colorIndex += 1
        If colorIndex >= scalarColors.Count Then colorIndex = 0
    End Sub
End Class



Public Class Mouse_LeftClick
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        label1 = "Left click and drag to draw a rectangle"
        desc = "Demonstrate what the left-click enables"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.trueText(New TTtext("Left-click and drag to select a region in any of the images." + vbCrLf +
                                 "The selected area is presented to ocvbClass in ocvb.drawRect." + vbCrLf +
                                 "In this example, the selected region from the RGB image will be resized to fit in the Result2 image to the right." + vbCrLf +
                                 "Double-click an image to remove the selected region.", 10, 50))

        If ocvb.drawRect.Width <> 0 And ocvb.drawRect.Height <> 0 Then dst2 = src(ocvb.drawRect).Resize(dst2.Size())
    End Sub
End Class




Public Class Mouse_RightClick
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        label1 = "Right click and drag to draw a rectangle"
        desc = "Demonstrate what the right-click enables"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.trueText(New TTtext("Right-click and drag to select a region in one of the images." + vbCrLf +
                                 "The selected image data will be opened in a spreadsheet.  Give it a try!" + vbCrLf +
                                 "Double-click an image to remove the selected region.", 10, 50))
        If ocvb.drawRect.Width <> 0 And ocvb.drawRect.Height <> 0 Then dst2 = src(ocvb.drawRect).Resize(dst2.Size())
    End Sub
End Class

