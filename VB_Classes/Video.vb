Imports cv = OpenCvSharp
Imports System.IO
' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_Basics
    Inherits VBparent
    Public srcVideo As String
    Public image As New cv.Mat
    Public captureVideo As New cv.VideoCapture
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)

        ocvb.parms.openFileDialogRequested = True
        ocvb.parms.openFileInitialDirectory = ocvb.homeDir + "/Data/"
        ocvb.parms.openFileDialogName = GetSetting("OpenCVB", "VideoFileName", "VideoFileName", ocvb.homeDir + "Data\CarsDrivingUnderBridge.mp4")
        ocvb.parms.openFileFilter = "video files (*.mp4)|*.mp4|All files (*.*)|*.*"
        ocvb.parms.openFileFilterIndex = 1
        ocvb.parms.openFileDialogTitle = "Select a video file for input"
        ocvb.parms.initialStartSetting = False

        Dim fileInfo = New FileInfo(ocvb.parms.openFileDialogName)
        srcVideo = fileInfo.FullName

        captureVideo = New cv.VideoCapture(fileInfo.FullName)
        label1 = fileInfo.Name
        desc = "Show a video file"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        Dim fileInfo = New FileInfo(ocvb.parms.openFileDialogName)
        If srcVideo <> ocvb.parms.openFileDialogName Then
            If fileInfo.Exists = False Then
                ocvb.trueText(New TTtext("File not found: " + fileInfo.FullName, 10, 125))
                Exit Sub
            End If
            srcVideo = ocvb.parms.openFileDialogName
            captureVideo = New cv.VideoCapture(ocvb.parms.openFileDialogName)
        End If
        captureVideo.Read(image)
        If image.Empty() Then
            captureVideo.Dispose()
            captureVideo = New cv.VideoCapture(FileInfo.FullName)
            captureVideo.Read(image)
        End If

        ocvb.parms.openFileSliderPercent = captureVideo.PosFrames / captureVideo.FrameCount
        If image.Empty() = False Then dst1 = image.Resize(ocvb.color.Size())
    End Sub
End Class






' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_CarCounting
    Inherits VBparent
    Dim flow As Font_FlowText
    Dim video As Video_Basics
    Dim bgSub As BGSubtract_MOG
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        bgSub = New BGSubtract_MOG(ocvb)

        video = New Video_Basics(ocvb)

        flow = New Font_FlowText(ocvb)

        desc = "Count cars in a video file"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        video.Run(ocvb)
        If video.dst1.Empty() = False And video.image.Empty() = False Then
            dst1.SetTo(0)
            bgSub.src = video.image
            bgSub.Run(ocvb)
            Dim videoImage = bgSub.dst1
            dst2 = video.dst1

            ' there are 5 lanes of traffic so setup 5 regions
            ' NOTE: if long shadows are present this approach will not work without provision for the width of a car.  Needs more sample data.
            Dim activeHeight = 30
            Dim finishLine = bgSub.dst1.Height - activeHeight * 8
            Static activeState(5) As Boolean
            Static carCount As Int32
            For i = 1 To activeState.Length - 1
                Dim lane = New cv.Rect(Choose(i, 230, 460, 680, 900, 1110), finishLine, 40, activeHeight)
                Dim cellCount = videoImage(lane).CountNonZero()
                If cellCount Then
                    activeState(i) = True
                    videoImage.Rectangle(lane, cv.Scalar.Red, -1)
                    dst2.Rectangle(lane, cv.Scalar.Red, -1)
                End If
                If cellCount = 0 And activeState(i) = True Then
                    activeState(i) = False
                    carCount += 1
                End If
                dst2.Rectangle(lane, cv.Scalar.White, 2)
            Next

            Dim tmp = videoImage.Resize(src.Size())
            flow.msgs.Add("  Cars " + CStr(carCount))
            flow.Run(ocvb)
            cv.Cv2.BitwiseOr(dst1, tmp.CvtColor(cv.ColorConversionCodes.GRAY2BGR), dst1)
        End If
    End Sub
End Class




' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_CarCComp
    Inherits VBparent
    Dim cc As CComp_Basics
    Dim video As Video_Basics
    Dim bgSub As BGSubtract_MOG
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        bgSub = New BGSubtract_MOG(ocvb)

        cc = New CComp_Basics(ocvb)

        video = New Video_Basics(ocvb)

        desc = "Outline cars with a rectangle"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        video.Run(ocvb)
        If video.dst1.Empty() = False Then
            bgSub.src = video.dst1
            bgSub.Run(ocvb)
            cc.src = bgSub.dst1
            cc.Run(ocvb)
            dst1 = cc.dst1
            dst2 = cc.dst2
        End If
    End Sub
End Class




' https://stackoverflow.com/questions/47706339/car-counting-and-classification-using-emgucv-and-vb-net
Public Class Video_MinRect
    Inherits VBparent
    Public video As Video_Basics
    Public bgSub As BGSubtract_MOG
    Public contours As cv.Point()()
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        video = New Video_Basics(ocvb)
        video.srcVideo = ocvb.homeDir + "Data/CarsDrivingUnderBridge.mp4"
        video.Run(ocvb)

        bgSub = New BGSubtract_MOG(ocvb)
        desc = "Find area of car outline - example of using minAreaRect"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        video.Run(ocvb)
        If video.dst1.Empty() = False Then
            bgSub.src = video.dst1
            bgSub.Run(ocvb)

            contours = cv.Cv2.FindContoursAsArray(bgSub.dst1, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
            dst1 = bgSub.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            If standalone Then
                For i = 0 To contours.Length - 1
                    Dim minRect = cv.Cv2.MinAreaRect(contours(i))
                    drawRotatedRectangle(minRect, dst1, cv.Scalar.Red)
                Next
            End If
            dst2 = video.dst1
        End If
    End Sub
End Class





Public Class Video_MinCircle
    Inherits VBparent
    Dim input As Video_MinRect
    Public Sub New(ocvb As VBocvb)
        setCaller(ocvb)
        input = New Video_MinRect(ocvb)
        desc = "Find area of car outline - example of using MinEnclosingCircle"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        input.Run(ocvb)
        dst1 = input.dst1
        dst2 = input.dst2

        Dim center As New cv.Point2f
        Dim radius As Single
        If input.contours IsNot Nothing Then
            For i = 0 To input.contours.Length - 1
                cv.Cv2.MinEnclosingCircle(input.contours(i), center, radius)
                dst1.Circle(center, radius, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
            Next
        End If
    End Sub
End Class
