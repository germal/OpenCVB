'Imports cv = OpenCvSharp
'Public Class MFD_Basics
'    Inherits VBparent
'    Public motion As Motion_Basics
'    Public stableRGB As cv.Mat
'    Public Sub New()
'        initParent()
'        motion = New Motion_Basics
'        If findfrm(caller + " Radio Options") Is Nothing Then
'            radio.Setup(caller, 2)
'            radio.check(0).Text = "Use motion-filtered pixel values"
'            radio.check(1).Text = "Use original (unchanged) pixels"
'            radio.check(0).Checked = True
'        End If
'        label1 = "Motion-filtered image"
'        task.desc = "Motion-Filtered basics - default input is rsc"
'    End Sub
'    Public Sub Run()
'        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

'        motion.src = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        motion.Run()
'        label2 = motion.label2
'        dst2 = If(motion.dst2.Channels = 1, motion.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR), motion.dst2.Clone)

'        Dim radioVal As Integer
'        Static frm As OptionsRadioButtons = findfrm(caller + " Radio Options")
'        For radioVal = 0 To frm.check.Count - 1
'            If frm.check(radioVal).Checked Then Exit For
'        Next

'        If motion.resetAll Or stableRGB Is Nothing Or radioVal = 1 Then
'            stableRGB = src.Clone
'        Else
'            For Each rect In motion.intersect.enclosingRects
'                dst2.Rectangle(rect, cv.Scalar.Yellow, 2)
'                If rect.Width And rect.Height Then src(rect).CopyTo(stableRGB(rect))
'            Next
'        End If

'        dst1 = stableRGB.Clone
'    End Sub
'End Class






'Public Class Motion_FilteredDepth
'    Inherits VBparent
'    Public motion As Motion_Basics
'    Dim filteredRGB As Motion_FilteredRGB
'    Public stableDepth As cv.Mat
'    Public Sub New()
'        initParent()
'        motion = New Motion_Basics
'        filteredRGB = New Motion_FilteredRGB
'        label1 = "Motion-filtered depth data"
'        task.desc = "Stabilize the depth image but update any areas with motion"
'    End Sub
'    Public Sub Run()
'        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

'        motion.src = task.color.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        motion.Run()
'        label2 = motion.label2
'        dst2 = If(motion.dst2.Channels = 1, motion.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR), motion.dst2.Clone)

'        Dim radioVal As Integer
'        Static frm As OptionsRadioButtons = findfrm("Motion_FilteredRGB Radio Options")
'        For radioVal = 0 To frm.check.Count - 1
'            If frm.check(radioVal).Checked Then Exit For
'        Next

'        If motion.resetAll Or stableDepth Is Nothing Or radioVal = 1 Then
'            stableDepth = task.depth32f.Clone
'        Else
'            For Each rect In motion.intersect.enclosingRects
'                dst2.Rectangle(rect, cv.Scalar.Yellow, 2)
'                If rect.Width And rect.Height Then task.depth32f(rect).CopyTo(stableDepth(rect))
'            Next
'        End If

'        dst1 = stableDepth
'    End Sub
'End Class