Imports cv = OpenCvSharp
' https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
Public Class IMU_Basics
    Inherits VBparent
    Dim lastTimeStamp As Double
    Dim flow As Font_FlowText
    Public theta As cv.Point3f ' this is the description - x, y, and z - of the axes centered in the camera.
    Public gyroAngle As cv.Point3f ' this is the orientation of the gyro.
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "IMU_Basics: Alpha x 1000", 0, 1000, 980)

        flow = New Font_FlowText(ocvb)

        ocvb.desc = "Read and display the IMU coordinates"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.parms.IMU_Present = False Then
            ocvb.trueText("No IMU present on this device")
            Exit Sub
        End If
        Dim alpha As Double = sliders.trackbar(0).Value / 1000
        If ocvb.frameCount = 0 Then
            lastTimeStamp = ocvb.IMU_TimeStamp
        Else
            gyroAngle = ocvb.IMU_AngularVelocity
            Dim dt_gyro = (ocvb.IMU_TimeStamp - lastTimeStamp) / 1000
            If ocvb.parms.cameraIndex <> VB_Classes.ActiveTask.algParms.D435i Then dt_gyro /= 1000 ' different units in the timestamp?
            lastTimeStamp = ocvb.IMU_TimeStamp
            gyroAngle = gyroAngle * dt_gyro
            theta += New cv.Point3f(-gyroAngle.Z, -gyroAngle.Y, gyroAngle.X)
        End If

        ' NOTE: Initialize the angle around the y-axis to zero.
        Dim accelAngle = New cv.Point3f(Math.Atan2(ocvb.IMU_Acceleration.X, Math.Sqrt(ocvb.IMU_Acceleration.Y * ocvb.IMU_Acceleration.Y + ocvb.IMU_Acceleration.Z * ocvb.IMU_Acceleration.Z)), 0,
                                                    Math.Atan2(ocvb.IMU_Acceleration.Y, ocvb.IMU_Acceleration.Z))
        If ocvb.frameCount = 0 Then
            theta = accelAngle
        Else
            ' Apply the Complementary Filter:
            '  - high-pass filter = theta * alpha: allows short-duration signals to pass while filtering steady signals (trying to cancel drift)
            '  - low-pass filter = accel * (1 - alpha): lets the long-term changes through, filtering out short term fluctuations
            theta.X = theta.X * alpha + accelAngle.X * (1 - alpha)
            theta.Z = theta.Z * alpha + accelAngle.Z * (1 - alpha)
        End If
        If standalone Then
            flow.msgs.Add("ts = " + Format(ocvb.IMU_TimeStamp, "#0.00") + " Acceleration (m/sec^2) x = " + Format(ocvb.IMU_Acceleration.X, "#0.00") +
                                  " y = " + Format(ocvb.IMU_Acceleration.Y, "#0.00") + " z = " + Format(ocvb.IMU_Acceleration.Z, "#0.00") + vbTab +
                                  " Motion (rads/sec) pitch = " + Format(ocvb.IMU_AngularVelocity.X, "#0.00") +
                                  " Yaw = " + Format(ocvb.IMU_AngularVelocity.Y, "#0.00") + " Roll = " + Format(ocvb.IMU_AngularVelocity.Z, "#0.00"))
        End If
        label1 = "theta.x " + Format(theta.X, "#0.000") + " y " + Format(theta.Y, "#0.000") + " z " + Format(theta.Z, "#0.000")
        flow.Run(ocvb)
    End Sub
End Class






Public Class IMU_Stabilizer
    Inherits VBparent
    Dim kalman As Kalman_Basics
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.input(3 - 1)
        ocvb.desc = "Stabilize the image with the IMU data."
        label1 = "IMU Stabilize (Move Camera + Select Kalman)"
        label2 = "Difference from Color Image"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.parms.IMU_Present = False Then
            ocvb.trueText("No IMU present on this device")
            Exit Sub
        End If
        Dim borderCrop = 5
        Dim vert_Border = borderCrop * src.Rows / src.Cols
        Dim dx = ocvb.IMU_AngularVelocity.X
        Dim dy = ocvb.IMU_AngularVelocity.Y
        Dim da = ocvb.IMU_AngularVelocity.Z
        Dim sx = 1 ' assume no scaling is taking place.
        Dim sy = 1 ' assume no scaling is taking place.

        kalman.input = {dx, dy, da}
        kalman.Run(ocvb)
        dx = kalman.output(0)
        dy = kalman.output(1)
        da = kalman.output(2)

        Dim smoothedMat = New cv.Mat(2, 3, cv.MatType.CV_64F)
        smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(da))
        smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(da))
        smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(da))
        smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(da))
        smoothedMat.Set(Of Double)(0, 2, dx)
        smoothedMat.Set(Of Double)(1, 2, dy)

        Dim smoothedFrame = src.WarpAffine(smoothedMat, src.Size())
        smoothedFrame = smoothedFrame(New cv.Range(borderCrop, smoothedFrame.Rows - borderCrop), New cv.Range(borderCrop, smoothedFrame.Cols - borderCrop))
        dst1 = smoothedFrame.Resize(src.Size())
        cv.Cv2.Subtract(src, dst1, dst2)

        dst1(New cv.Rect(10, 95, 50, 50)).SetTo(0)
        Dim Text = "dx = " + Format(dx, "#0.00") + vbNewLine + "dy = " + Format(dy, "#0.00") + vbNewLine + "da = " + Format(da, "#0.00")
        ocvb.trueText(Text)
    End Sub
End Class






Public Class IMU_Magnetometer
    Inherits VBparent
    Public plot As Plot_OverTime
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        plot = New Plot_OverTime(ocvb)
        plot.dst1 = dst2
        plot.maxScale = 10
        plot.minScale = -10

        ocvb.desc = "Get the IMU_Magnetometer values from the IMU (if available)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.IMU_Magnetometer = New cv.Point3f Then
            ocvb.trueText("The IMU for this camera does not have Magnetometer readings.")
        Else
            ocvb.trueText("Uncalibrated IMU Magnetometer reading:  x = " + CStr(ocvb.IMU_Magnetometer.X) + vbCrLf +
                                                  "Uncalibrated IMU Magnetometer reading:  y = " + CStr(ocvb.IMU_Magnetometer.Y) + vbCrLf +
                                                  "Uncalibrated IMU Magnetometer reading:  z = " + CStr(ocvb.IMU_Magnetometer.Z))
            plot.plotData = New cv.Scalar(ocvb.IMU_Magnetometer.X, ocvb.IMU_Magnetometer.Y, ocvb.IMU_Magnetometer.Z)
            plot.Run(ocvb)
            label2 = "x (blue) = " + Format(plot.plotData.Item(0), "#0.00") + " y (green) = " + Format(plot.plotData.Item(1), "#0.00") +
                          " z (red) = " + Format(plot.plotData.Item(2), "#0.00")
        End If
    End Sub
End Class




Public Class IMU_Barometer
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Get the barometric pressure from the IMU (if available)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.IMU_Barometer = 0 Then
            ocvb.trueText("The IMU for this camera does not have barometric pressure.")
        Else
            ocvb.trueText("Barometric pressure is " + CStr(ocvb.IMU_Barometer) + " hectopascal." + vbCrLf +
                                                  "Barometric pressure is " + Format(ocvb.IMU_Barometer * 0.02953, "#0.00") + " inches of mercury.")
        End If
    End Sub
End Class




Public Class IMU_Temperature
    Inherits VBparent
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        ocvb.desc = "Get the temperature of the IMU (if available)"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.parms.IMU_Present = False Then
            ocvb.trueText("No IMU present on this device")
        Else
            ocvb.trueText("IMU Temperature is " + Format(ocvb.IMU_Temperature, "#0.00") + " degrees Celsius." + vbCrLf +
                                                  "IMU Temperature is " + Format(ocvb.IMU_Temperature * 9 / 5 + 32, "#0.00") + " degrees Fahrenheit.")
        End If
    End Sub
End Class




Public Class IMU_FrameTime
    Inherits VBparent
    Public plot As Plot_OverTime
    Public CPUInterval As Double
    Public IMUtoCaptureEstimate As Double
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        plot = New Plot_OverTime(ocvb)
        plot.dst1 = dst2
        plot.maxScale = 150
        plot.minScale = 0
        plot.backColor = cv.Scalar.Aquamarine
        plot.plotCount = 4

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Minimum IMU to Capture time (ms)", 1, 10, 2)
        sliders.setupTrackBar(1, "Number of Plot Values", 5, 30, 20)

        label2 = "IMU (blue) Host (green) Latency est. (red) - all in ms"
        ocvb.desc = "Use the IMU timestamp to estimate the delay from IMU capture to image capture.  Just an estimate!"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.parms.IMU_Present = False Then
            ocvb.trueText("No IMU present on this device")
            Exit Sub
        End If

        Static IMUanchor As Integer = ocvb.IMU_FrameTime
        Static histogramIMU(plot.maxScale) As Integer
        ' there can be some errant times at startup.
        If ocvb.IMU_FrameTime > plot.maxScale Or ocvb.IMU_FrameTime < 0 Then Exit Sub ' skip the crazy values.
        Static imuTotalTime As Double
        imuTotalTime += ocvb.IMU_FrameTime
        If imuTotalTime = 0 Then
            Static allZeroCount As Integer
            allZeroCount += 1
            If allZeroCount > 20 Then
                ocvb.trueText("Is IMU present?  No IMU FrameTimes")
                allZeroCount = Integer.MinValue ' don't show message again.
            End If
            Exit Sub ' if the IMU frametime was 0, then no new IMU data was generated (or it is unsupported!)
        End If

        Dim maxval = Integer.MinValue
        For i = 0 To histogramIMU.Count - 1
            If maxval < histogramIMU(i) Then
                maxval = histogramIMU(i)
                IMUanchor = i
            End If
        Next

        Dim imuFrameTime = CInt(ocvb.IMU_FrameTime)
        If IMUanchor <> 0 Then imuFrameTime = imuFrameTime Mod IMUanchor
        Dim minDelay = sliders.trackbar(0).Value
        IMUtoCaptureEstimate = IMUanchor - imuFrameTime + minDelay
        If IMUtoCaptureEstimate > IMUanchor Then IMUtoCaptureEstimate -= IMUanchor
        If IMUtoCaptureEstimate < minDelay Then IMUtoCaptureEstimate = minDelay

        Static sampledIMUFrameTime = ocvb.IMU_FrameTime
        If ocvb.frameCount Mod 10 = 0 Then sampledIMUFrameTime = ocvb.IMU_FrameTime

        histogramIMU(CInt(ocvb.IMU_FrameTime)) += 1

        If standalone Then
            Dim output = "IMU_TimeStamp (ms) " + Format(ocvb.IMU_TimeStamp, "00") + vbCrLf +
                        "CPU TimeStamp (ms) " + Format(ocvb.CPU_TimeStamp, "00") + vbCrLf +
                        "IMU Frametime (ms, sampled) " + Format(sampledIMUFrameTime, "000.00") +
                        " IMUanchor = " + Format(IMUanchor, "00") +
                        " latest = " + Format(ocvb.IMU_FrameTime, "00.00") + vbCrLf +
                        "IMUtoCapture (ms, sampled, in red) " + Format(IMUtoCaptureEstimate, "00") + vbCrLf + vbCrLf +
                        "IMU Frame Time = Blue" + vbCrLf +
                        "Host Frame Time = Green" + vbCrLf +
                        "IMU Total Delay = Red" + vbCrLf +
                        "IMU Anchor Frame Time = White (IMU Frame Time that occurs most often" + vbCrLf + vbCrLf + vbCrLf

            plot.plotData = New cv.Scalar(ocvb.IMU_FrameTime, ocvb.CPU_FrameTime, IMUtoCaptureEstimate, IMUanchor)
            plot.Run(ocvb)

            If plot.maxScale - plot.minScale > histogramIMU.Count Then ReDim histogramIMU(plot.maxScale - plot.minScale)

            Dim plotLastX = sliders.trackbar(1).Value
            If plot.lastXdelta.Count > plotLastX Then
                For i = 0 To plot.plotCount - 1
                    output += "Last " + CStr(plotLastX) + Choose(i + 1, " IMU FrameTime", " Host Frame Time", " IMUtoCapture ms", " IMU Center time") + vbTab
                    For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                        output += Format(plot.lastXdelta.Item(j).Item(i), "00") + ", "
                    Next
                    output += vbCrLf
                Next
            End If
            ocvb.trueText(output)
        End If
    End Sub
End Class





Public Class IMU_HostFrameTimes
    Inherits VBparent
    Public plot As Plot_OverTime
    Public CPUInterval As Double
    Public HostInterruptDelayEstimate As Double
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        plot = New Plot_OverTime(ocvb)
        plot.dst1 = dst2
        plot.maxScale = 150
        plot.minScale = 0
        plot.backColor = cv.Scalar.Aquamarine
        plot.plotCount = 4

        sliders.Setup(ocvb, caller)
        sliders.setupTrackBar(0, "Minimum Host interrupt delay (ms)", 1, 10, 4)
        sliders.setupTrackBar(1, "Number of Plot Values", 5, 30, 25)

        label2 = "IMU (blue) Host (green) Latency est. (red) - all in ms"
        ocvb.desc = "Use the Host timestamp to estimate the delay from image capture to host interrupt.  Just an estimate!"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.parms.IMU_Present = False Then
            ocvb.trueText("No IMU present on this device")
            Exit Sub
        End If

        Static CPUanchor As Integer = ocvb.CPU_FrameTime
        Static hist(plot.maxScale) As Integer
        ' there can be some errant times at startup.
        If ocvb.CPU_FrameTime > plot.maxScale Or ocvb.CPU_FrameTime < 0 Then Exit Sub ' skip the crazy values.

        Dim maxval = Integer.MinValue
        For i = 0 To hist.Count - 1
            If maxval < hist(i) Then
                maxval = hist(i)
                CPUanchor = i
            End If
        Next

        Dim cpuFrameTime = CInt(ocvb.CPU_FrameTime)
        If CPUanchor <> 0 Then cpuFrameTime = cpuFrameTime Mod CPUanchor
        Dim minDelay = sliders.trackbar(0).Value
        HostInterruptDelayEstimate = CPUanchor - cpuFrameTime + minDelay
        If HostInterruptDelayEstimate > CPUanchor Then HostInterruptDelayEstimate -= CPUanchor
        If HostInterruptDelayEstimate < 0 Then HostInterruptDelayEstimate = minDelay

        Static sampledCPUFrameTime = ocvb.CPU_FrameTime
        If ocvb.frameCount Mod 10 = 0 Then sampledCPUFrameTime = ocvb.CPU_FrameTime

        hist(CInt(ocvb.CPU_FrameTime)) += 1

        If standalone Then
            Dim output = "IMU_TimeStamp (ms) " + Format(ocvb.IMU_TimeStamp, "00") + vbCrLf +
                         "CPU TimeStamp (ms) " + Format(ocvb.CPU_TimeStamp, "00") + vbCrLf +
                         "Host Frametime (ms, sampled) " + Format(sampledCPUFrameTime, "000.00") +
                         " CPUanchor = " + Format(CPUanchor, "00") +
                         " latest = " + Format(ocvb.CPU_FrameTime, "00.00") + vbCrLf +
                         "Host Interrupt Delay (ms, sampled, in red) " + Format(HostInterruptDelayEstimate, "00") + vbCrLf + vbCrLf +
                         "Blue" + vbTab + "IMU Frame Time" + vbCrLf +
                         "Green" + vbTab + "Host Frame Time" + vbCrLf +
                         "Red" + vbTab + "Host Total Delay (latency)" + vbCrLf +
                         "White" + vbTab + "Host Anchor Frame Time (Host Frame Time that occurs most often" + vbCrLf + vbCrLf + vbCrLf

            plot.plotData = New cv.Scalar(ocvb.IMU_FrameTime, ocvb.CPU_FrameTime, HostInterruptDelayEstimate, CPUanchor)
            plot.Run(ocvb)

            If plot.maxScale - plot.minScale > hist.Count Then ReDim hist(plot.maxScale - plot.minScale)

            Dim plotLastX = sliders.trackbar(1).Value
            If plot.lastXdelta.Count > plotLastX Then
                For i = 0 To plot.plotCount - 1
                    output += "Last " + CStr(plotLastX) + Choose(i + 1, " IMU FrameTime", " Host Frametime", " Host Delay ms", " CPUanchor FT") + vbTab
                    For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                        output += Format(plot.lastXdelta.Item(j).Item(i), "00") + ", "
                    Next
                    output += vbCrLf
                Next
            End If
            ocvb.trueText(output)
        End If
    End Sub
End Class




Public Class IMU_TotalDelay
    Inherits VBparent
    Dim host As IMU_HostFrameTimes
    Dim imu As IMU_FrameTime
    Dim plot As Plot_OverTime
    Dim kalman As Kalman_Single
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)

        host = New IMU_HostFrameTimes(ocvb)
        imu = New IMU_FrameTime(ocvb)
        kalman = New Kalman_Single(ocvb)

        plot = New Plot_OverTime(ocvb)
        plot.dst1 = dst2
        plot.maxScale = 50
        plot.minScale = 0
        plot.plotCount = 4

        label1 = "Timing data - total (white) right image"
        label2 = "IMU (blue) Host (green) Latency est. (red) - all in ms"
        ocvb.desc = "Estimate time from IMU capture to host processing to allow predicting effect of camera motion."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.parms.IMU_Present = False Then
            ocvb.trueText("No IMU present on this device")
            Exit Sub
        End If

        host.Run(ocvb)
        imu.Run(ocvb)
        Dim totaldelay = host.HostInterruptDelayEstimate + imu.IMUtoCaptureEstimate

        kalman.inputReal = totaldelay
        kalman.Run(ocvb)

        Static sampledCPUDelay = host.HostInterruptDelayEstimate
        Static sampledIMUDelay = imu.IMUtoCaptureEstimate
        Static sampledTotalDelay = totaldelay
        Static sampledSmooth = kalman.stateResult
        If ocvb.frameCount Mod 10 = 0 Then
            sampledCPUDelay = host.HostInterruptDelayEstimate
            sampledIMUDelay = imu.IMUtoCaptureEstimate
            sampledTotalDelay = totaldelay
            sampledSmooth = kalman.stateResult
        End If

        Dim output = "Estimated host delay (ms, sampled) " + Format(sampledCPUDelay, "00") + vbCrLf +
                     "Estimated IMU delay (ms, sampled) " + Format(sampledIMUDelay, "00") + vbCrLf +
                     "Estimated Total delay (ms, sampled) " + Format(sampledTotalDelay, "00") + vbCrLf +
                     "Estimated Total delay Smoothed (ms, sampled, in White) " + Format(sampledSmooth, "00") + vbCrLf + vbCrLf +
                     "Blue" + vbTab + "IMU Frame Time" + vbCrLf +
                     "Green" + vbTab + "Host Frame Time" + vbCrLf +
                     "Red" + vbTab + "Host+IMU Total Delay (latency)" + vbCrLf +
                     "White" + vbTab + "Host+IMU Anchor Frame Time (Host Frame Time that occurs most often)" + vbCrLf + vbCrLf + vbCrLf

        plot.plotData = New cv.Scalar(imu.IMUtoCaptureEstimate, host.HostInterruptDelayEstimate, totaldelay, kalman.stateResult)
        plot.Run(ocvb)

        Dim plotLastX = 25
        If plot.lastXdelta.Count > plotLastX Then
            For i = 0 To plot.plotCount - 1
                output += "Last " + CStr(plotLastX) + Choose(i + 1, " IMU Delay ", " Host Delay", " Total Delay ms", " Smoothed Total") + vbTab
                For j = plot.lastXdelta.Count - plotLastX - 1 To plot.lastXdelta.Count - 1
                    output += Format(plot.lastXdelta.Item(j).Item(i), "00") + ", "
                Next
                output += vbCrLf
            Next
        End If
        ocvb.trueText(output)
    End Sub
End Class








Public Class IMU_IsCameraLevel
    Inherits VBparent
    Public angleX As Single ' in radians.
    Public angleY As Single ' in radians.
    Public angleZ As Single ' in radians.
    Public cameraLevel As Boolean
    Dim flow As Font_FlowText
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        If standalone Then flow = New Font_FlowText(ocvb)
        sliders.Setup(ocvb, caller, 1)
        sliders.setupTrackBar(0, "Threshold in degrees X10", 1, 100, 20) ' default is a 20/10 or 2 degrees from 0...
        ocvb.desc = "Answer the question: Is the camera level?"
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.parms.IMU_Present = False Then
            ocvb.trueText("No IMU present on this device")
            Exit Sub
        End If

        Dim gx = ocvb.IMU_Acceleration.X
        Dim gy = ocvb.IMU_Acceleration.Y
        Dim gz = ocvb.IMU_Acceleration.Z

        angleX = (Math.Atan2(gy, gx) + cv.Cv2.PI / 2) * 57.2958
        angleY = (Math.Atan2(gx, gy) - cv.Cv2.PI / 2) * 57.2958
        angleZ = (Math.Atan2(gy, gz) + cv.Cv2.PI / 2) * 57.2958

        Dim degreesThreshold = sliders.trackbar(0).Value / 10 ' 0-100 --> 0-10 degrees
        If Math.Abs(angleX) > degreesThreshold Or Math.Abs(angleZ) > degreesThreshold Then cameraLevel = False Else cameraLevel = True
        If standalone Then
            flow.msgs.Add(" Angle X = " + Format(angleX, "0.00") + " degrees" +
                          " Angle Y = " + Format(angleY, "0.00") + " degrees" +
                          " Angle Z = " + Format(angleZ, "0.00") + " degrees" +
                          If(cameraLevel, " - Camera is level", " - Camera is NOT level"))
            flow.Run(ocvb)
        End If
    End Sub
End Class






Public Class IMU_GVector
    Inherits VBparent
    Dim kalman As Kalman_Basics
    Public angleX As Single ' in radians.
    Public angleY As Single ' in radians.
    Public angleZ As Single ' in radians.
    Public Sub New(ocvb As VBocvb)
        initParent(ocvb)
        kalman = New Kalman_Basics(ocvb)
        ReDim kalman.input(6 - 1)
        ocvb.desc = "Find the angle of tilt for the camera with respect to gravity."
    End Sub
    Public Sub Run(ocvb As VBocvb)
        If ocvb.parms.IMU_Present = False Then
            ocvb.trueText("No IMU present on this device")
            Exit Sub
        End If

        Dim gx = ocvb.IMU_Acceleration.X
        Dim gy = ocvb.IMU_Acceleration.Y
        Dim gz = ocvb.IMU_Acceleration.Z

        angleX = Math.Atan2(gy, gx) + cv.Cv2.PI / 2
        angleY = Math.Atan2(gx, gy) - cv.Cv2.PI / 2
        angleZ = Math.Atan2(gy, gz) + cv.Cv2.PI / 2

        kalman.input = {gx, gy, gz, angleX, angleY, angleZ}

        If kalman.check.Box(0).Checked Then
            kalman.Run(ocvb)
            gx = kalman.output(0)
            gy = kalman.output(1)
            gz = kalman.output(2)

            angleX = kalman.output(3)
            angleY = kalman.output(4)
            angleZ = kalman.output(5)
        End If

        If standalone Then
            Dim outStr As String = "Acceleration and their angles are smoothed with a Kalman filters:" + vbCrLf + vbCrLf
            outStr = "IMU Acceleration in X-direction = " + vbTab + vbTab + Format(gx, "#0.0000") + vbCrLf
            outStr += "IMU Acceleration in Y-direction = " + vbTab + vbTab + Format(gy, "#0.0000") + vbCrLf
            outStr += "IMU Acceleration in Z-direction = " + vbTab + vbTab + Format(gz, "#0.0000") + vbCrLf + vbCrLf
            outStr += "X-axis Angle from horizontal (in degrees) = " + vbTab + Format(angleX * 57.2958, "#0.0000") + vbCrLf
            outStr += "Y-axis Angle from horizontal (in degrees) = " + vbTab + Format(angleY * 57.2958, "#0.0000") + vbCrLf
            outStr += "Z-axis Angle from horizontal (in degrees) = " + vbTab + Format(angleZ * 57.2958, "#0.0000") + vbCrLf + vbCrLf
            ' if there is any significant acceleration other than gravity, it will be detected here.
            If Math.Abs(Math.Sqrt(gx * gx + gy * gy + gz * gz) - 9.807) > 0.05 Then
                outStr += "Camera appears to be moving because the gravity vector is not 9.8.  Results may not be valid." + vbCrLf
            Else
                outStr += vbCrLf
            End If

            ' validate the result
            outStr += vbCrLf + "sqrt (" + vbTab + Format(gx, "#0.0000") + "*" + Format(gx, "#0.0000") + vbTab +
                            vbTab + Format(gy, "#0.0000") + "*" + Format(gy, "#0.0000") + vbTab +
                            vbTab + Format(gz, "#0.0000") + "*" + Format(gz, "#0.0000") + " ) = " + vbTab +
                            vbTab + Format(Math.Sqrt(gx * gx + gy * gy + gz * gz), "#0.0000") + vbCrLf +
                            "Should be close to the earth's gravitational constant of 9.807 (or the camera was moving.)"

            ocvb.trueText(outStr)
        End If
    End Sub
End Class
