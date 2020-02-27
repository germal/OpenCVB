Imports cv = OpenCvSharp
' https://github.com/IntelRealSense/librealsense/tree/master/examples/motion
Public Class IMU_Basics : Implements IDisposable
    Dim sliders As New OptionsSliders
    Dim lastTimeStamp As Double
    Dim flow As Font_FlowText
    Public theta As cv.Point3f ' this is the description - x, y, and z - of the axes centered in the camera.
    Public gyroAngle As cv.Point3f ' this is the orientation of the gyro.
    Public externalUse As Boolean
    Public Sub New(ocvb As AlgorithmData)
        sliders.setupTrackBar1(ocvb, "IMU_Basics: Alpha x 1000", 0, 1000, 980)
        If ocvb.parms.ShowOptions Then sliders.Show()

        flow = New Font_FlowText(ocvb)
        flow.externalUse = True
        flow.result1or2 = RESULT1

        ocvb.desc = "Read and display the IMU coordinates"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Present Then
            Dim alpha As Double = sliders.TrackBar1.Value / 1000
            If ocvb.frameCount = 0 Then
                lastTimeStamp = ocvb.parms.IMU_TimeStamp
            Else
                gyroAngle = ocvb.parms.imuGyro
                Dim dt_gyro = (ocvb.parms.IMU_TimeStamp - lastTimeStamp) / 1000
                If ocvb.parms.cameraIndex <> D400Cam Then dt_gyro /= 1000 ' different units in the timestamp?
                lastTimeStamp = ocvb.parms.IMU_TimeStamp
                gyroAngle = gyroAngle * dt_gyro
                theta += New cv.Point3f(-gyroAngle.Z, -gyroAngle.Y, gyroAngle.X)
            End If

            ' NOTE: Initialize the angle around the y-axis to zero.
            Dim accelAngle = New cv.Point3f(Math.Atan2(ocvb.parms.imuAccel.X, Math.Sqrt(ocvb.parms.imuAccel.Y * ocvb.parms.imuAccel.Y + ocvb.parms.imuAccel.Z * ocvb.parms.imuAccel.Z)), 0,
                                                Math.Atan2(ocvb.parms.imuAccel.Y, ocvb.parms.imuAccel.Z))
            If ocvb.frameCount = 0 Then
                theta = accelAngle
            Else
                ' Apply the Complementary Filter:
                '  - high-pass filter = theta * alpha: allows short-duration signals to pass while filtering steady signals (trying to cancel drift)
                '  - low-pass filter = accel * (1 - alpha): lets the long-term changes through, filtering out short term fluctuations
                theta.X = theta.X * alpha + accelAngle.X * (1 - alpha)
                theta.Z = theta.Z * alpha + accelAngle.Z * (1 - alpha)
            End If
            If externalUse = False Then
                flow.msgs.Add("ts = " + CStr(ocvb.parms.IMU_TimeStamp) + "Gravity (m/sec^2) x = " + Format(ocvb.parms.imuAccel.X, "#0.000") + " y = " + Format(ocvb.parms.imuAccel.Y, "#0.000") +
                                  " z = " + Format(ocvb.parms.imuAccel.Z, "#0.000") + vbTab + "Motion (rads/sec) pitch = " + Format(ocvb.parms.imuGyro.X, "#0.000") + vbTab +
                                  " Yaw = " + Format(ocvb.parms.imuGyro.Y, "#0.000") + vbTab + " Roll = " + Format(ocvb.parms.imuGyro.Z, "#0.000"))
            End If
            ocvb.label1 = "theta.x " + Format(theta.X, "#0.000") + " y " + Format(theta.Y, "#0.000") + " z " + Format(theta.Z, "#0.000")
        Else
            If ocvb.frameCount = 0 Then flow.msgs.Add("No IMU present on this RealSense device")
        End If
        flow.Run(ocvb)
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        flow.Dispose()
        sliders.Dispose()
    End Sub
End Class






Public Class IMU_Stabilizer : Implements IDisposable
    Dim kalman As Kalman_Basics
    Dim check As New OptionsCheckbox
    Public Sub New(ocvb As AlgorithmData)
        kalman = New Kalman_Basics(ocvb)
        kalman.externalUse = True

        check.Setup(ocvb, 1)
        check.Box(0).Text = "Turn on/off Kalman filtering of IMU data."
        If ocvb.parms.ShowOptions Then check.Show()

        kalman.plot.sliders.Hide()
        kalman.kPlot.sliders.Hide()

        ocvb.desc = "Stabilize the image with the IMU data."
        ocvb.label1 = "IMU Stabilize (Move Camera + Select Kalman)"
        ocvb.label2 = "Difference from Color Image"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Present Then
            Static savedKalmanCheck = check.Box(0).Checked
            If savedKalmanCheck <> check.Box(0).Checked Then
                kalman.restartRequested = True
                savedKalmanCheck = check.Box(0).Checked
            End If
            Dim borderCrop = 5
            Dim vert_Border = borderCrop * ocvb.color.Rows / ocvb.color.Cols
            Dim dx = ocvb.parms.imuGyro.X
            Dim dy = ocvb.parms.imuGyro.Y
            Dim da = ocvb.parms.imuGyro.Z
            Dim sx = 1 ' assume no scaling is taking place.
            Dim sy = 1 ' assume no scaling is taking place.

            If ocvb.frameCount > 1 And check.Box(0).Checked Then
                kalman.inputReal = New cv.Point3f(dx, dy, da)
                kalman.Run(ocvb)
                dx = kalman.statePoint.X
                dy = kalman.statePoint.Y
                da = kalman.statePoint.Z
            End If

            Dim smoothedMat = New cv.Mat(2, 3, cv.MatType.CV_64F)
            smoothedMat.Set(Of Double)(0, 0, sx * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 1, sx * -Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 0, sy * Math.Sin(da))
            smoothedMat.Set(Of Double)(1, 1, sy * Math.Cos(da))
            smoothedMat.Set(Of Double)(0, 2, dx)
            smoothedMat.Set(Of Double)(1, 2, dy)

            Dim smoothedFrame = ocvb.color.WarpAffine(smoothedMat, ocvb.color.Size())
            smoothedFrame = smoothedFrame(New cv.Range(borderCrop, smoothedFrame.Rows - borderCrop), New cv.Range(borderCrop, smoothedFrame.Cols - borderCrop))
            ocvb.result1 = smoothedFrame.Resize(ocvb.color.Size())
            cv.Cv2.Subtract(ocvb.color, ocvb.result1, ocvb.result2)

            ocvb.result1(New cv.Rect(10, 95, 50, 50)).SetTo(0)
            Dim Text = "dx = " + Format(dx, "#0.00") + vbNewLine + "dy = " + Format(dy, "#0.00") + vbNewLine + "da = " + Format(da, "#0.00")
            ocvb.putText(New ActiveClass.TrueType(Text, 10, 100, RESULT1))
        Else
            ocvb.putText(New ActiveClass.TrueType("No IMU present on this RealSense device", 20, 100))
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        kalman.Dispose()
        check.Dispose()
    End Sub
End Class






Public Class IMU_Magnetometer : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Get the IMU_Magnetometer values from the IMU (if available)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Magnetometer = New cv.Point3f Then
            ocvb.putText(New ActiveClass.TrueType("The IMU for this camera does not have IMU_Magnetometer readings.", 10, 125))
        Else
            ocvb.putText(New ActiveClass.TrueType("Uncalibrated IMU Magnetometer reading:  x = " + CStr(ocvb.parms.IMU_Magnetometer.X), 10, 60))
            ocvb.putText(New ActiveClass.TrueType("Uncalibrated IMU Magnetometer reading:  y = " + CStr(ocvb.parms.IMU_Magnetometer.Y), 10, 80))
            ocvb.putText(New ActiveClass.TrueType("Uncalibrated IMU Magnetometer reading:  z = " + CStr(ocvb.parms.IMU_Magnetometer.Z), 10, 100))
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class IMU_Barometer : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Get the barometric pressure from the IMU (if available)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Barometer = 0 Then
            ocvb.putText(New ActiveClass.TrueType("The IMU for this camera does not have barometric pressure.", 10, 125))
        Else
            ocvb.putText(New ActiveClass.TrueType("Barometric pressure is " + CStr(ocvb.parms.IMU_Barometer) + " hectopascal.", 10, 60))
            ocvb.putText(New ActiveClass.TrueType("Barometric pressure is " + Format(ocvb.parms.IMU_Barometer * 0.02953, "#0.00") + " inches of mercury.", 10, 90))
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class




Public Class IMU_Temperature : Implements IDisposable
    Public Sub New(ocvb As AlgorithmData)
        ocvb.desc = "Get the temperature of the IMU (if available)"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Present Then
            ocvb.putText(New ActiveClass.TrueType("IMU Temperature is " + Format(ocvb.parms.IMU_Temperature, "#0.00") + " degrees Celsius.", 10, 60))
            ocvb.putText(New ActiveClass.TrueType("IMU Temperature is " + Format(ocvb.parms.IMU_Temperature * 9 / 5 + 32, "#0.00") + " degrees Fahrenheit.", 10, 80))
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class








Public Class IMU_TimeStamp : Implements IDisposable
    Dim flow As Font_FlowText
    Public Sub New(ocvb As AlgorithmData)
        flow = New Font_FlowText(ocvb)
        flow.externalUse = True
        flow.result1or2 = RESULT1

        ocvb.desc = "Get the timestamp from the IMU"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.IMU_Present Then
            flow.msgs.Add("Timestamp = " + Format(ocvb.parms.IMU_TimeStamp / 1000, "##0.0000000000000") + " seconds")
            flow.Run(ocvb)
        End If
    End Sub
    Public Sub Dispose() Implements IDisposable.Dispose
        flow.Dispose()
    End Sub
End Class