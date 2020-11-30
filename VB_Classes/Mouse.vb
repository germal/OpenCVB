Imports cv = OpenCvSharp
Public Class Mouse_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        label1 = "Move the mouse below to show mouse tracking."
        ocvb.desc = "Test the mousePoint interface"
    End Sub
    Public Sub Run()
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static lastPoint = New cv.Point
        ' only display mouse movement in the lower left image (pic.tag = 2)
        If lastPoint = ocvb.task.mousePoint Or ocvb.task.mousePicTag <> 2 Then Exit Sub
        lastPoint = ocvb.task.mousePoint
        Dim radius = 10
        Static colorIndex As Integer
        Dim nextColor = ocvb.scalarColors(colorIndex)
        Dim nextPt = ocvb.task.mousePoint
        dst1.Circle(nextPt, radius, nextColor, -1, cv.LineTypes.AntiAlias)
        colorIndex += 1
        If colorIndex >= ocvb.scalarColors.Count Then colorIndex = 0
    End Sub
End Class



Public Class Mouse_LeftClick
    Inherits VBparent
    Public Sub New()
        initParent()
        label1 = "Left click and drag to draw a rectangle"
        ocvb.desc = "Demonstrate what the left-click enables"
    End Sub
    Public Sub Run()
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        ocvb.trueText("Left-click and drag to select a region in any of the images." + vbCrLf +
                                 "The selected area is presented to ocvbClass in ocvb.task.drawRect." + vbCrLf +
                                 "In this example, the selected region from the RGB image will be resized to fit in the Result2 image to the right." + vbCrLf +
                                 "Double-click an image to remove the selected region.")

        If ocvb.task.drawRect.Width <> 0 And ocvb.task.drawRect.Height <> 0 Then dst2 = src(ocvb.task.drawRect).Resize(dst2.Size())
    End Sub
End Class




Public Class Mouse_RightClick
    Inherits VBparent
    Public Sub New()
        initParent()
        label1 = "Right click and drag to draw a rectangle"
        ocvb.desc = "Demonstrate what the right-click enables"
    End Sub
    Public Sub Run()
        If ocvb.intermediateReview = caller Then ocvb.intermediateObject = Me
        ocvb.trueText("Right-click and drag to select a region in one of the images." + vbCrLf +
                                 "The selected image data will be opened in a spreadsheet.  Give it a try!" + vbCrLf +
                                 "Double-click an image to remove the selected region.")
        If ocvb.task.drawRect.Width <> 0 And ocvb.task.drawRect.Height <> 0 Then dst2 = src(ocvb.task.drawRect).Resize(dst2.Size())
    End Sub
End Class


