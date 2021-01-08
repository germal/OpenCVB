Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.IO
'https://docs.opencv.org/3.1.0/da/d22/tutorial_py_canny.html
Public Class Edges_Basics
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Canny threshold1", 1, 255, 50)
            sliders.setupTrackBar(1, "Canny threshold2", 1, 255, 50)
            sliders.setupTrackBar(2, "Canny Aperture", 3, 7, 3)
        End If

        task.desc = "Show canny edge detection with varying thresholds"
        label1 = "Canny using L1 Norm"
        label2 = "Canny using L2 Norm"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim threshold1 As Integer = sliders.trackbar(0).Value
        Dim threshold2 As Integer = sliders.trackbar(1).Value
        Dim aperture = If(sliders.trackbar(2).Value Mod 2, sliders.trackbar(2).Value, sliders.trackbar(2).Value + 1)

        Dim input = src
        If input.Channels = 3 Then input = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        dst1 = input.Canny(threshold1, threshold2, aperture, False)
        dst2 = input.Canny(threshold1, threshold2, aperture, True)
    End Sub
End Class



Public Class Edges_DepthAndColor
    Inherits VBparent
    Dim shadow As Depth_Holes
    Dim canny As Edges_Basics
    Dim dilate As DilateErode_Basics
    Public Sub New()
        initParent()
        dilate = New DilateErode_Basics()
        dilate.radio.check(2).Checked = True

        canny = New Edges_Basics()
        canny.sliders.trackbar(0).Value = 100
        canny.sliders.trackbar(1).Value = 100

        shadow = New Depth_Holes()

        task.desc = "Find all the edges in an image include Canny from the grayscale image and edges of depth shadow."
        label1 = "Edges in color and depth after dilate"
        label2 = "Edges in color and depth no dilate"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        canny.src = src
        canny.Run()
        shadow.Run()

        dst2 = shadow.dst2
        dst2 += canny.dst1.Threshold(1, 255, cv.ThresholdTypes.Binary)

        dilate.src = dst2
        dilate.Run()
        dilate.dst1.SetTo(0, shadow.holeMask)
        dst1 = dilate.dst1
    End Sub
End Class





'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/laplace_operator/laplace_operator.html
Public Class Edges_Laplacian
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Gaussian Kernel", 1, 32, 7)
            sliders.setupTrackBar(1, "Laplacian Kernel", 1, 32, 5)
        End If
        label2 = "Laplacian of Depth Image"
        task.desc = "Show Laplacian edge detection with varying kernel sizes"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim gaussiankernelSize = If(sliders.trackbar(0).Value Mod 2, sliders.trackbar(0).Value, sliders.trackbar(0).Value - 1)
        Dim laplaciankernelSize = If(sliders.trackbar(1).Value Mod 2, sliders.trackbar(1).Value, sliders.trackbar(1).Value - 1)

        dst1 = src.GaussianBlur(New cv.Size(gaussiankernelSize, gaussiankernelSize), 0, 0)
        dst1 = dst1.Laplacian(cv.MatType.CV_8U, laplaciankernelSize, 1, 0)
        dst1 = dst1.ConvertScaleAbs()

        dst2 = task.RGBDepth.GaussianBlur(New cv.Size(gaussiankernelSize, gaussiankernelSize), 0, 0)
        dst2 = dst2.Laplacian(cv.MatType.CV_8U, laplaciankernelSize, 1, 0)
        dst2 = dst2.ConvertScaleAbs()
    End Sub
End Class



'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edges_Scharr
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Scharr multiplier X100", 1, 500, 50)
        End If
        label2 = "x field + y field in CV_32F format"
        task.desc = "Scharr is most accurate with 3x3 kernel."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim xField = gray.Scharr(cv.MatType.CV_32FC1, 1, 0)
        Dim yField = gray.Scharr(cv.MatType.CV_32FC1, 0, 1)
        cv.Cv2.Add(xField, yField, dst2)
        dst2.ConvertTo(dst1, cv.MatType.CV_8U, sliders.trackbar(0).Value / 100)
    End Sub
End Class



' https://www.learnopencv.com/non-photorealistic-rendering-using-opencv-python-c/
Public Class Edges_Preserving
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 2)
            radio.check(0).Text = "Edge RecurseFilter"
            radio.check(1).Text = "Edge NormconvFilter"
            radio.check(0).Checked = True
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Edge Sigma_s", 0, 200, 10)
            sliders.setupTrackBar(1, "Edge Sigma_r", 1, 100, 40)
        End If
        label2 = "Edge preserving blur for RGB depth image above"
        task.desc = "OpenCV's edge preserving filter."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim sigma_s = sliders.trackbar(0).Value
        Dim sigma_r = sliders.trackbar(1).Value / sliders.trackbar(1).Maximum
        If radio.check(0).Checked Then
            cv.Cv2.EdgePreservingFilter(src, dst1, cv.EdgePreservingMethods.RecursFilter, sigma_s, sigma_r)
        Else
            cv.Cv2.EdgePreservingFilter(src, dst1, cv.EdgePreservingMethods.NormconvFilter, sigma_s, sigma_r)
        End If
        If radio.check(0).Checked Then
            cv.Cv2.EdgePreservingFilter(task.RGBDepth, dst2, cv.EdgePreservingMethods.RecursFilter, sigma_s, sigma_r)
        Else
            cv.Cv2.EdgePreservingFilter(task.RGBDepth, dst2, cv.EdgePreservingMethods.NormconvFilter, sigma_s, sigma_r)
        End If
    End Sub
End Class



Module Edges_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edges_RandomForest_Open(modelFileName As String) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Edges_RandomForest_Close(Edges_RandomForestPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edges_RandomForest_Run(Edges_RandomForestPtr As IntPtr, inputPtr As IntPtr, rows As Integer, cols As Integer) As IntPtr
    End Function
End Module



'  https://docs.opencv.org/3.1.0/d0/da5/tutorial_ximgproc_prediction.html
Public Class Edges_RandomForest_CPP
    Inherits VBparent
    Dim rgbData() As Byte
    Dim EdgesPtr As IntPtr
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Edges RF Threshold", 1, 255, 35)
        End If

        task.desc = "Detect edges using structured forests - Opencv Contrib"
        ReDim rgbData(src.Total * src.ElemSize - 1)
        label2 = "Thresholded Edge Mask (use slider to adjust)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        If ocvb.frameCount < 100 Then ocvb.trueText("On the first call only, it takes a few seconds to load the randomForest model.", 10, 100)

        ' why not do this in the constructor?  Because the message is held up by the lengthy process of loading the model.
        If ocvb.frameCount = 5 Then
            Dim modelInfo = New FileInfo(ocvb.parms.homeDir + "Data/model.yml.gz")
            EdgesPtr = Edges_RandomForest_Open(modelInfo.FullName)
        End If
        If ocvb.frameCount > 5 Then ' the first images are skipped so the message above can be displayed.
            Marshal.Copy(src.Data, rgbData, 0, rgbData.Length)
            Dim handleRGB = GCHandle.Alloc(rgbData, GCHandleType.Pinned)
            Dim gray8u = Edges_RandomForest_Run(EdgesPtr, handleRGB.AddrOfPinnedObject(), src.Rows, src.Cols)
            handleRGB.Free() ' free the pinned memory...

            dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, gray8u).Threshold(sliders.trackbar(0).Value, 255, cv.ThresholdTypes.Binary)
        End If
    End Sub
    Public Sub Close()
        Edges_RandomForest_Close(EdgesPtr)
    End Sub
End Class






Public Class Edges_LeftView
    Inherits VBparent
    Dim red As LeftRightView_Basics
    Dim sobel As Edges_Sobel
    Public Sub New()
        initParent()
        red = New LeftRightView_Basics()
        sobel = New Edges_Sobel()
        sobel.sliders.trackbar(0).Value = 5

        task.desc = "Find the edges in the LeftViewimages."
        label1 = "Edges in Left Image"
        label2 = "Edges in Right Image (except on Kinect)"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        red.Run()
        Dim leftView = red.dst1
        sobel.src = red.dst2
        sobel.Run()
        dst2 = sobel.dst1.Clone()

        sobel.src = leftView
        sobel.Run()
        dst1 = sobel.dst1
    End Sub
End Class



Public Class Edges_ResizeAdd
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Border Vertical in Pixels", 1, 20, 5)
            sliders.setupTrackBar(1, "Border Horizontal in Pixels", 1, 20, 5)
            sliders.setupTrackBar(2, "Threshold for Pixel Difference", 1, 50, 16)
        End If
        task.desc = "Find edges using a resize, subtract, and threshold."
        label1 = "Edges found with just resizing"
        label2 = "Found edges added to grayscale image source."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim gray = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim newFrame = gray(New cv.Range(sliders.trackbar(0).Value, gray.Rows - sliders.trackbar(0).Value),
                            New cv.Range(sliders.trackbar(1).Value, gray.Cols - sliders.trackbar(1).Value))
        newFrame = newFrame.Resize(gray.Size())
        cv.Cv2.Absdiff(gray, newFrame, dst1)
        dst1 = dst1.Threshold(sliders.trackbar(2).Value, 255, cv.ThresholdTypes.Binary)
        cv.Cv2.Add(gray, dst1, dst2)
    End Sub
End Class




Public Class Edges_DCTfrequency
    Inherits VBparent
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Remove Frequencies < x", 0, 100, 32)
            sliders.setupTrackBar(1, "Threshold after Removal", 1, 255, 20)
        End If

        label2 = "Mask for the isolated frequencies"
        task.desc = "Find edges by removing all the highest frequencies."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim gray = task.RGBDepth.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim frequencies As New cv.Mat
        Dim src32f As New cv.Mat
        gray.ConvertTo(src32f, cv.MatType.CV_32F, 1 / 255)
        cv.Cv2.Dct(src32f, frequencies, cv.DctFlags.None)

        Dim roi As New cv.Rect(0, 0, sliders.trackbar(0).Value, src32f.Height)
        If roi.Width > 0 Then frequencies(roi).SetTo(0)
        label1 = "Highest " + CStr(sliders.trackbar(0).Value) + " frequencies removed from RGBDepth"

        cv.Cv2.Dct(frequencies, src32f, cv.DctFlags.Inverse)
        src32f.ConvertTo(dst1, cv.MatType.CV_8UC1, 255)
        dst2 = dst1.Threshold(sliders.trackbar(1).Value, 255, cv.ThresholdTypes.Binary)
    End Sub
End Class






Module Edges_Deriche_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edges_Deriche_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Edges_Deriche_Close(Edges_DerichePtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Edges_Deriche_Run(Edges_DerichePtr As IntPtr, rgbPtr As IntPtr, rows As Integer, cols As Integer, alpha As Single, omega As Single) As IntPtr
    End Function
End Module




' https://github.com/opencv/opencv_contrib/blob/master/modules/ximgproc/samples/dericheSample.py
Public Class Edges_Deriche_CPP
    Inherits VBparent
    Dim Edges_Deriche As IntPtr
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Deriche Alpha", 1, 400, 100)
            sliders.setupTrackBar(1, "Deriche Omega", 1, 1000, 100)
        End If
        Edges_Deriche = Edges_Deriche_Open()
        label2 = "Image enhanced with Deriche results"
        task.desc = "Edge detection using the Deriche X and Y gradients - Painterly"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim srcData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, srcData, 0, srcData.Length)
        Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
        Dim alpha = sliders.trackbar(0).Value / 100
        Dim omega = sliders.trackbar(1).Value / 1000
        Dim imagePtr = Edges_Deriche_Run(Edges_Deriche, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, alpha, omega)
        handleSrc.Free()

        If imagePtr <> 0 Then
            Dim dstData(src.Total * src.ElemSize() - 1) As Byte
            Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
            dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC3, dstData)
        End If
        cv.Cv2.BitwiseOr(src, dst1, dst2)
    End Sub
    Public Sub Close()
        Edges_Deriche_Close(Edges_Deriche)
    End Sub
End Class







'https://docs.opencv.org/2.4/doc/tutorials/imgproc/imgtrans/sobel_derivatives/sobel_derivatives.html
Public Class Edges_Sobel
    Inherits VBparent
    Public grayX As cv.Mat
    Public grayY As cv.Mat
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Sobel kernel Size", 1, 32, 3)
        End If
        task.desc = "Show Sobel edge detection with varying kernel sizes"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static ksizeSlider = findSlider("Sobel kernel Size")
        Dim kernelSize = If(ksizeSlider.Value Mod 2, ksizeSlider.Value, ksizeSlider.Value - 1)
        dst1 = New cv.Mat(src.Rows, src.Cols, src.Type)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        grayX = src.Sobel(cv.MatType.CV_32F, 1, 0, kernelSize)
        Dim abs_grayX = grayX.ConvertScaleAbs()
        grayY = src.Sobel(cv.MatType.CV_32F, 0, 1, kernelSize)
        Dim abs_grayY = grayY.ConvertScaleAbs()
        cv.Cv2.AddWeighted(abs_grayX, 0.5, abs_grayY, 0.5, 0, dst1)
    End Sub
End Class







Public Class Edges_Palette
    Inherits VBparent
    Dim edges As Edges_Basics
    Dim palette As Palette_Basics
    Public Sub New()
        initParent()
        edges = New Edges_Basics
        palette = New Palette_Basics
        task.desc = "Use palette to help canny find more edges"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        edges.src = src
        edges.Run()
        dst1 = edges.dst1.Clone

        palette.src = src
        palette.Run()
        edges.src = palette.dst1
        edges.Run()
        dst2 = edges.dst1.Clone
    End Sub
End Class








Public Class Edges_DCTinput
    Inherits VBparent
    Dim edges As Edges_Basics
    Dim dct As DCT_FeatureLess
    Public Sub New()
        initParent()
        edges = New Edges_Basics
        dct = New DCT_FeatureLess
        task.desc = "Use the featureless regions to enhance the edge detection"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        edges.src = src
        edges.Run()
        dst1 = edges.dst1.Clone

        dct.src = src
        dct.Run()
        edges.src = src.SetTo(cv.Scalar.White, dct.dst1)
        edges.Run()
        dst2 = edges.dst1
    End Sub
End Class








Public Class Edges_BinarizedCanny
    Inherits VBparent
    Dim edges As Edges_Basics
    Dim binarize As Binarize_Recurse
    Dim mats As Mat_4Click
    Public Sub New()
        initParent()
        mats = New Mat_4Click
        binarize = New Binarize_Recurse
        edges = New Edges_Basics
        ocvb.quadrantIndex = QUAD3
        label1 = "Edges between halves, lightest, darkest, and the combo"
        task.desc = "Collect edges from binarized images"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        binarize.src = src
        binarize.Run()

        edges.src = binarize.mats.mat(0) ' the light and dark halves
        edges.Run()
        mats.mat(0) = edges.dst1.Clone
        mats.mat(3) = edges.dst1.Clone

        edges.src = binarize.mats.mat(1) ' the lightest of the light half
        edges.Run()
        mats.mat(1) = edges.dst1.Clone
        cv.Cv2.BitwiseOr(mats.mat(1), mats.mat(3), mats.mat(3))

        edges.src = binarize.mats.mat(3) ' the darkest of the dark half
        edges.Run()
        mats.mat(2) = edges.dst1.Clone
        cv.Cv2.BitwiseOr(mats.mat(2), mats.mat(3), mats.mat(3))

        mats.Run()
        dst1 = mats.dst1
        If mats.dst2.Channels = 3 Then
            label2 = "Combo of first 3 below.  Click quadrants in dst1."
            dst2 = mats.mat(3)
        Else
            dst2 = mats.dst2
        End If
    End Sub
End Class








Public Class Edges_BinarizedSobel
    Inherits VBparent
    Dim edges As Edges_Sobel
    Dim binarize As Binarize_Recurse
    Public mats As Mat_4Click
    Public Sub New()
        initParent()
        mats = New Mat_4Click

        binarize = New Binarize_Recurse

        edges = New Edges_Sobel
        ocvb.quadrantIndex = QUAD3

        Dim kernelSlider = findSlider("Sobel kernel Size")
        kernelSlider.Value = 5

        label1 = "Edges between halves, lightest, darkest, and the combo"
        task.desc = "Collect Sobel edges from binarized images"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        binarize.src = src
        binarize.Run()

        edges.src = binarize.mats.mat(0) ' the light and dark halves
        edges.Run()
        mats.mat(0) = edges.dst1.Clone
        mats.mat(3) = edges.dst1.Clone

        edges.src = binarize.mats.mat(1) ' the lightest of the light half
        edges.Run()
        mats.mat(1) = edges.dst1.Clone
        cv.Cv2.BitwiseOr(mats.mat(1), mats.mat(3), mats.mat(3))

        edges.src = binarize.mats.mat(3) ' the darkest of the dark half
        edges.Run()
        mats.mat(2) = edges.dst1.Clone
        cv.Cv2.BitwiseOr(mats.mat(2), mats.mat(3), mats.mat(3))

        mats.Run()
        dst1 = mats.dst1
        If mats.dst2.Channels = 3 Then
            label2 = "Combo of first 3 below.  Click quadrants in dst1."
            dst2 = mats.mat(3)
        Else
            dst2 = mats.dst2
        End If
    End Sub
End Class