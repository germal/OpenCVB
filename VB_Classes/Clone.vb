Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_demo.cpp
Public Class Clone_Basics
    Inherits ocvbClass
    Public colorChangeValues As cv.Vec3f
    Public illuminationChangeValues As cv.Vec2f
    Public textureFlatteningValues As cv.Vec2f
    Public cloneSpec As Int32 ' 0 is colorchange, 1 is illuminationchange, 2 is textureflattening
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)

        label1 = "Clone result - draw anywhere to clone a region"
        label2 = "Clone Region Mask"
        ocvb.desc = "Clone a portion of one image into another.  Draw on any image to change selected area."
        ocvb.drawRect = New cv.Rect(src.Width / 4, src.Height / 4, src.Width / 2, src.Height / 2)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mask As New cv.Mat(src.Size(), cv.MatType.CV_8U, 0)
        If ocvb.drawRect = New cv.Rect Then
            mask.SetTo(255)
        Else
            cv.Cv2.Rectangle(mask, ocvb.drawRect, cv.Scalar.White, -1)
        End If
        dst2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        If standalone And ocvb.frameCount Mod 10 = 0 Then cloneSpec += 1
        Select Case cloneSpec Mod 3
            Case 0
                cv.Cv2.ColorChange(src, mask, dst1, colorChangeValues(0), colorChangeValues(1), colorChangeValues(2))
            Case 1
                cv.Cv2.IlluminationChange(src, mask, dst1, illuminationChangeValues(0), illuminationChangeValues(1))
            Case 2
                cv.Cv2.TextureFlattening(src, mask, dst1, textureFlatteningValues(0), textureFlatteningValues(1))
        End Select
    End Sub
End Class




Public Class Clone_ColorChange
    Inherits ocvbClass
    Dim clone As Clone_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        clone = New Clone_Basics(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Color Change - Red", 5, 25, 15)
        sliders.setupTrackBar(1, "Color Change - Green", 5, 25, 5)
        sliders.setupTrackBar(2, "Color Change - Blue", 5, 25, 5)

        label1 = "Draw anywhere to select different clone region"
        label2 = "Mask used for clone"
        ocvb.desc = "Clone a portion of one image into another controlling rgb.  Draw on any image to change selected area."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        clone.cloneSpec = 0
        clone.colorChangeValues = New cv.Point3f(sliders.trackbar(0).Value / 10, sliders.trackbar(1).Value / 10, sliders.trackbar(0).Value / 10)
        clone.Run(ocvb)
        dst1 = clone.dst1
        dst2 = clone.dst2
    End Sub
End Class




Public Class Clone_IlluminationChange
    Inherits ocvbClass
    Dim clone As Clone_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        clone = New Clone_Basics(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Alpha", 0, 20, 2)
        sliders.setupTrackBar(1, "Beta", 0, 20, 2)

        label1 = "Draw anywhere to select different clone region"
        label2 = "Mask used for clone"
        ocvb.desc = "Clone a portion of one image into another controlling illumination.  Draw on any image to change selected area."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        clone.cloneSpec = 1
        clone.illuminationChangeValues = New cv.Vec2f(sliders.trackbar(0).Value / 10, sliders.trackbar(1).Value / 10)
        clone.Run(ocvb)
        dst1 = clone.dst1
        dst2 = clone.dst2
    End Sub
End Class





Public Class Clone_TextureFlattening
    Inherits ocvbClass
    Dim clone As Clone_Basics
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        clone = New Clone_Basics(ocvb)

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Low Threshold", 0, 100, 10)
        sliders.setupTrackBar(1, "High Threshold", 0, 100, 50)

        label1 = "Draw anywhere to select different clone region"
        label2 = "mask used for clone"
        ocvb.desc = "Clone a portion of one image into another controlling texture.  Draw on any image to change selected area."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        clone.cloneSpec = 2
        clone.textureFlatteningValues = New cv.Vec2f(sliders.trackbar(0).Value, sliders.trackbar(1).Value)
        clone.Run(ocvb)
        dst1 = clone.dst1
        dst2 = clone.dst2
    End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_gui.cpp
' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_demo.cpp
' https://www.learnopencv.com/seamless-cloning-using-opencv-python-cpp/
' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_demo.cpp
Public Class Clone_Eagle
    Inherits ocvbClass
    Dim sourceImage As cv.Mat
    Dim mask As cv.Mat
    Dim srcROI As cv.Rect
    Dim maskROI As cv.Rect
    Dim pt As cv.Point
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        radio.Setup(ocvb, caller, 3)
        radio.check(0).Text = "Seamless - Mixed Clone"
        radio.check(1).Text = "Seamless - MonochromeTransfer Clone"
        radio.check(2).Text = "Seamless - Normal Clone"
        radio.check(2).Checked = True

        sourceImage = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/CloneSource.png")
        srcROI = New cv.Rect(0, 40, sourceImage.Width, sourceImage.Height)

        mask = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/Clonemask.png")
        maskROI = New cv.Rect(srcROI.Width, 40, mask.Width, mask.Height)

        dst2.SetTo(0)
        dst2(srcROI) = sourceImage
        dst2(maskROI) = mask

        pt = New cv.Point(src.Width / 2, src.Height / 2)
        label1 = "Move Eagle by clicking in any location."
        label2 = "Source image and source mask."
        ocvb.desc = "Clone an eagle into the video stream."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        dst1 = src.Clone()
        If ocvb.mouseClickFlag Then
            pt = ocvb.mouseClickPoint ' pt corresponds to the center of the source image.  Roi can't be outside image boundary.
            pt = ocvb.mouseClickPoint * If(ocvb.parms.resolution = resHigh, 2, 1)
            If pt.X + srcROI.Width / 2 >= src.Width Then pt.X = src.Width - srcROI.Width / 2
            If pt.X - srcROI.Width / 2 < 0 Then pt.X = srcROI.Width / 2
            If pt.Y + srcROI.Height >= src.Height Then pt.Y = src.Height - srcROI.Height / 2
            If pt.Y - srcROI.Height < 0 Then pt.Y = srcROI.Height / 2
            ocvb.mouseClickFlag = False
        End If

        Dim cloneFlag As New cv.SeamlessCloneMethods
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                cloneFlag = Choose(i + 1, cv.SeamlessCloneMethods.MixedClone, cv.SeamlessCloneMethods.MonochromeTransfer, cv.SeamlessCloneMethods.NormalClone)
                Exit For
            End If
        Next
        cv.Cv2.SeamlessClone(sourceImage, dst1, mask, pt, dst1, cloneFlag)
    End Sub
End Class




' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.Cv2.SeamlessClone(OpenCvSharp.InputArray,%20OpenCvSharp.InputArray,%20OpenCvSharp.InputArray,%20OpenCvSharp.Point,%20OpenCvSharp.OutputArray,%20OpenCvSharp.SeamlessCloneMethods)/
Public Class Clone_Seamless
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        radio.Setup(ocvb, caller, 3)
        radio.check(0).Text = "Seamless Normal Clone"
        radio.check(1).Text = "Seamless Mono Clone"
        radio.check(2).Text = "Seamless Mixed Clone"
        radio.check(0).Checked = True
        label1 = "Results for SeamlessClone"
        label2 = "Mask for Clone"
        ocvb.desc = "Use the seamlessclone API to merge color and depth..."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim center As New cv.Point(src.Width / 2, src.Height / 2)
        Dim radius = 100
        If ocvb.drawRect = New cv.Rect Then
            dst2.SetTo(0)
            dst2.Circle(center.X, center.Y, radius, cv.Scalar.White, -1)
        Else
            cv.Cv2.Rectangle(dst2, ocvb.drawRect, cv.Scalar.White, -1)
        End If

        Dim style = cv.SeamlessCloneMethods.NormalClone
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                style = Choose(i + 1, cv.SeamlessCloneMethods.NormalClone, cv.SeamlessCloneMethods.MonochromeTransfer, cv.SeamlessCloneMethods.MixedClone)
                Exit For
            End If
        Next
        dst1 = src.Clone()
        cv.Cv2.SeamlessClone(ocvb.RGBDepth, src, dst2, center, dst1, style)
        dst1.Circle(center, radius, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
    End Sub
End Class
