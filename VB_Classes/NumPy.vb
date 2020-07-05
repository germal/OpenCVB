Imports Numpy
Imports System.Text
Imports cv = OpenCvSharp
Imports py = Python.Runtime

' https://docs.scipy.org/doc/scipy/reference/tutorial/fft.html
' https://github.com/SciSharp/Numpy.NET
Public Class NumPy_Test
    Inherits ocvbClass
    Public Sub New(ocvb As AlgorithmData)
        setCaller(ocvb)
        ocvb.desc = "Test Numpy interface for FFT"
    End Sub
    Public Sub Run(ocvb As AlgorithmData)
        If ocvb.parms.NumPyEnabled Then
            'Using py.Py.GIL() ' for explanation see http://pythonnet.github.io/ 
            Dim test = np.random.randn(64, 1000)
                Dim x = np.array(Of Double)({1.0, 2.0, 1.0, -1.0, 1.5})
                Dim y = np.fft.fft_(x)
                Dim sb = New StringBuilder().AppendFormat("Original input = {0:N}" + vbCrLf + vbCrLf, x)
                sb.AppendFormat("FFT output of above 1-dimensional vector" + vbCrLf + "{0:N}", y)
                Dim inverse = np.fft.ifft(y)
                sb.AppendFormat(vbCrLf + vbCrLf + "Inverse FFT output" + "{0:N}" + vbCrLf + vbCrLf + "Should reflect original input above", inverse)
                ocvb.putText(New oTrueType(sb.ToString, 10, 60, RESULT1))
            'End Using
        Else
            ocvb.putText(New oTrueType("Enable Embedded NumPy in the OptionsDialog", 10, 60, RESULT1))
        End If
    End Sub
End Class
