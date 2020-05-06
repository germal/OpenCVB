Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_demo.cpp
Public Class Clone_Normal
    Inherits VB_Class
    Public colorChangeValues As cv.Vec3f
    Public illuminationChangeValues As cv.Vec2f
    Public textureFlatteningValues As cv.Vec2f
    Public cloneSpec As Int32 ' 0 is colorchange, 1 is illuminationchange, 2 is textureflattening
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        ocvb.desc = "Clone a portion of one image into another.  Draw on any image to change selected area."
        ocvb.label1 = "Clone result - draw anywhere to clone a region"
        ocvb.label2 = "Clone Region Mask"
        ocvb.drawRect = New cv.Rect(ocvb.color.Width / 4, ocvb.color.Height / 4, ocvb.color.Width / 2, ocvb.color.Height / 2)
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim mask As New cv.Mat(ocvb.color.Size(), cv.MatType.CV_8U, 0)
        If ocvb.drawRect = New cv.Rect Then
            mask.SetTo(255)
        Else
            cv.Cv2.Rectangle(mask, ocvb.drawRect, cv.Scalar.White, -1)
        End If
        ocvb.result2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)

        Select Case cloneSpec
            Case 0
                cv.Cv2.ColorChange(ocvb.color, mask, ocvb.result1, colorChangeValues(0), colorChangeValues(1), colorChangeValues(2))
            Case 1
                cv.Cv2.IlluminationChange(ocvb.color, mask, ocvb.result1, illuminationChangeValues(0), illuminationChangeValues(1))
            Case 2
                cv.Cv2.TextureFlattening(ocvb.color, mask, ocvb.result1, textureFlatteningValues(0), textureFlatteningValues(1))
        End Select
    End Sub
    Public Sub MyDispose()
    End Sub
End Class




Public Class Clone_ColorChange
    Inherits VB_Class
        Dim clone As Clone_Normal
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        clone = New Clone_Normal(ocvb, "Clone_ColorChange")
        ocvb.desc = "Clone a portion of one image into another controlling rgb.  Draw on any image to change selected area."

        sliders.setupTrackBar1(ocvb, callerName, "Color Change - Red", 5, 25, 15)
        sliders.setupTrackBar2(ocvb, callerName, "Color Change - Green", 5, 25, 5)
        sliders.setupTrackBar3(ocvb, callerName,"Color Change - Blue", 5, 25, 5)
            End Sub
    Public Sub Run(ocvb As AlgorithmData)
        clone.cloneSpec = 0
        clone.colorChangeValues = New cv.Point3f(sliders.TrackBar1.Value / 10, sliders.TrackBar2.Value / 10, sliders.TrackBar1.Value / 10)
        clone.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        clone.Dispose()
            End Sub
End Class




Public Class Clone_IlluminationChange
    Inherits VB_Class
        Dim clone As Clone_Normal
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        clone = New Clone_Normal(ocvb, "Clone_IlluminationChange")
        ocvb.desc = "Clone a portion of one image into another controlling illumination.  Draw on any image to change selected area."

        sliders.setupTrackBar1(ocvb, callerName, "Alpha", 0, 20, 2)
        sliders.setupTrackBar2(ocvb, callerName, "Beta", 0, 20, 2)
            End Sub
    Public Sub Run(ocvb As AlgorithmData)
        clone.cloneSpec = 1
        clone.illuminationChangeValues = New cv.Vec2f(sliders.TrackBar1.Value / 10, sliders.TrackBar2.Value / 10)
        clone.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        clone.Dispose()
            End Sub
End Class





Public Class Clone_TextureFlattening
    Inherits VB_Class
        Dim clone As Clone_Normal
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        clone = New Clone_Normal(ocvb, "Clone_TextureFlattening")
        ocvb.desc = "Clone a portion of one image into another controlling texture.  Draw on any image to change selected area."

        sliders.setupTrackBar1(ocvb, callerName, "Low Threshold", 0, 100, 10)
        sliders.setupTrackBar2(ocvb, callerName, "High Threshold", 0, 100, 50)
            End Sub
    Public Sub Run(ocvb As AlgorithmData)
        clone.cloneSpec = 2
        clone.textureFlatteningValues = New cv.Vec2f(sliders.TrackBar1.Value, sliders.TrackBar2.Value)
        clone.Run(ocvb)
    End Sub
    Public Sub MyDispose()
        clone.Dispose()
            End Sub
End Class





' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_gui.cpp
' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_demo.cpp
' https://www.learnopencv.com/seamless-cloning-using-opencv-python-cpp/
' https://github.com/opencv/opencv/blob/master/samples/cpp/cloning_demo.cpp
Public Class Clone_Eagle
    Inherits VB_Class
    Dim sourceImage As cv.Mat
    Dim mask As cv.Mat
    Dim srcROI As cv.Rect
    Dim maskROI As cv.Rect
    Dim pt As cv.Point
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        radio.Setup(ocvb, callerName,3)
        radio.check(0).Text = "Seamless - Mixed Clone"
        radio.check(1).Text = "Seamless - MonochromeTransfer Clone"
        radio.check(2).Text = "Seamless - Normal Clone"
        radio.check(2).Checked = True
        
        sourceImage = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/CloneSource.png")
        srcROI = New cv.Rect(0, 40, sourceImage.Width, sourceImage.Height)

        mask = cv.Cv2.ImRead(ocvb.parms.HomeDir + "Data/Clonemask.png")
        maskROI = New cv.Rect(srcROI.Width, 40, mask.Width, mask.Height)

        ocvb.result2(srcROI) = sourceImage
        ocvb.result2(maskROI) = mask

        pt = New cv.Point(ocvb.color.Width / 2, ocvb.color.Height / 2)
        ocvb.desc = "Clone an eagle into the video stream."
        ocvb.label1 = "Move Eagle by clicking in any location."
        ocvb.label2 = "Source image and source mask."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        ocvb.result1 = ocvb.color.Clone()
        If ocvb.mouseClickFlag Then
            pt = ocvb.mouseClickPoint ' pt corresponds to the center of the source image.  Roi can't be outside image boundary.
            If pt.X + srcROI.Width / 2 >= ocvb.color.Width Then pt.X = ocvb.color.Width - srcROI.Width / 2
            If pt.X - srcROI.Width / 2 < 0 Then pt.X = srcROI.Width / 2
            If pt.Y + srcROI.Height >= ocvb.color.Height Then pt.Y = ocvb.color.Height - srcROI.Height / 2
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
        cv.Cv2.SeamlessClone(sourceImage, ocvb.result1, mask, pt, ocvb.result1, cloneFlag)
    End Sub
    Public Sub MyDispose()
        radio.Dispose()
    End Sub
End Class




' https://www.csharpcodi.com/csharp-examples/OpenCvSharp.Cv2.SeamlessClone(OpenCvSharp.InputArray,%20OpenCvSharp.InputArray,%20OpenCvSharp.InputArray,%20OpenCvSharp.Point,%20OpenCvSharp.OutputArray,%20OpenCvSharp.SeamlessCloneMethods)/
Public Class Clone_Seamless
    Inherits VB_Class
    Public Sub New(ocvb As AlgorithmData, ByVal caller As String)
                If caller = "" Then callerName = Me.GetType.Name Else callerName = caller + "-->" + Me.GetType.Name
        radio.Setup(ocvb, callerName,3)
        radio.check(0).Text = "Seamless Normal Clone"
        radio.check(1).Text = "Seamless Mono Clone"
        radio.check(2).Text = "Seamless Mixed Clone"
        radio.check(0).Checked = True
                ocvb.label1 = "Mask for Clone"
        ocvb.label2 = "Results for SeamlessClone"
        ocvb.desc = "Use the seamlessclone API to merge color and depth..."
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        Dim center As New cv.Point(ocvb.color.Width / 2, ocvb.color.Height / 2)
        Dim radius = 100
        ocvb.result1.SetTo(0)
        If ocvb.drawRect = New cv.Rect Then
            ocvb.result1.SetTo(255)
        Else
            cv.Cv2.Rectangle(ocvb.result1, ocvb.drawRect, cv.Scalar.White, -1)
            ' ocvb.result1.Circle(center.X, center.Y, radius, cv.Scalar.White, -1)
        End If

        Dim style = cv.SeamlessCloneMethods.NormalClone
        For i = 0 To radio.check.Count - 1
            If radio.check(i).Checked Then
                style = Choose(i + 1, cv.SeamlessCloneMethods.NormalClone, cv.SeamlessCloneMethods.MonochromeTransfer, cv.SeamlessCloneMethods.MixedClone)
                Exit For
            End If
        Next
        ocvb.result2 = ocvb.color.Clone()
        cv.Cv2.SeamlessClone(ocvb.RGBDepth, ocvb.color, ocvb.result1, center, ocvb.result2, style)
        ocvb.result2.Circle(center, radius, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
    End Sub
    Public Sub MyDispose()
        radio.Dispose()
    End Sub
End Class