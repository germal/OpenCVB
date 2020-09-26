#If USE_NUMPY Then
'Imports Numpy
'Imports System.Text
'Imports cv = OpenCvSharp
'Imports epy = Python.Runtime
'Imports System.Dynamic

'' https://docs.scipy.org/doc/scipy/reference/tutorial/fft.html
'' https://github.com/SciSharp/Numpy.NET
'Public Class NumPy_FFT
'    Inherits VBparent
'    Public Sub New(ocvb As VBocvb)
'        initParent(ocvb)
'        ocvb.desc = "Test Numpy interface for FFT"
'    End Sub
'    Public Sub Run(ocvb As VBocvb)
'        If ocvb.parms.NumPyEnabled Then
'            Dim test = np.random.randn(64, 1000)
'            Dim x = np.array(Of Double)({1.0, 2.0, 1.0, -1.0, 1.5})
'            Dim y = np.fft.fft_(x)
'            Dim sb = New StringBuilder().AppendFormat("Original input = {0:N}" + vbCrLf + vbCrLf, x)
'            sb.AppendFormat("FFT output of above 1-dimensional vector" + vbCrLf + "{0:N}", y)
'            Dim inverse = np.fft.ifft(y)
'            sb.AppendFormat(vbCrLf + vbCrLf + "Inverse FFT output" + "{0:N}" + vbCrLf + vbCrLf + "Should reflect original input above", inverse)
'            ocvb.trueText(sb.ToString)
'        Else
'            ocvb.trueText("Enable Embedded NumPy in the OptionsDialog")
'        End If
'    End Sub
'End Class



'' http://pythonnet.github.io/
'' https://github.com/pythonnet/pythonnet
'Public Class NumPy_EmbeddedTest_CS
'    Inherits VBparent
'    Dim embed = New CS_Classes.NumPy_EmbeddedTest
'    Public Sub New(ocvb As VBocvb)
'        initParent(ocvb)
'        ocvb.desc = "Run an embedded Python script"
'    End Sub
'    Public Sub Run(ocvb As VBocvb)
'        If ocvb.parms.NumPyEnabled Then
'            embed.Run()
'        Else
'            ocvb.trueText("Enable Embedded NumPy/Python in the Global OptionsDialog")
'        End If
'    End Sub
'End Class






'' http://pythonnet.github.io/
'' https://github.com/pythonnet/pythonnet
'Public Class NumPy_EmbeddedMat_CS
'    Inherits VBparent
'    Dim embed = New CS_Classes.NumPy_EmbeddedMat
'    Public Sub New(ocvb As VBocvb)
'        initParent(ocvb)
'        ocvb.desc = "Run an embedded Python script to display an image"
'    End Sub
'    Public Sub Run(ocvb As VBocvb)
'        If ocvb.parms.NumPyEnabled Then
'            'Dim cmd = "import ctypes # An included library with Python install." + vbCrLf + "import sys" + vbCrLf + "def Mbox(title, text, style):" + vbCrLf + vbTab +
'            '          "return ctypes.windll.user32.MessageBoxW(0, text, title, style)" + vbCrLf + "Mbox('NumPy_Embedded testing', 'test', 1)"
'            Dim cmd = "from time import time,ctime" ' + vbCrLf + "Console.Writeline('Today is',ctime(time()))" + vbCrLf
'            embed.Run(src, cmd)
'        Else
'            ocvb.trueText("Enable Embedded NumPy/Python in the Global OptionsDialog")
'        End If
'    End Sub
'End Class






'' http://pythonnet.github.io/
'' https://github.com/pythonnet/pythonnet
'Public Class NumPy_CmdLine
'    Inherits VBparent
'    Dim embed = New CS_Classes.NumPy_Cmds
'    Public Sub New(ocvb As VBocvb)
'        initParent(ocvb)
'        ocvb.desc = "Run an embedded Python string of commands"
'    End Sub
'    Public Sub Run(ocvb As VBocvb)
'        If ocvb.parms.NumPyEnabled Then
'            embed.Run()
'        Else
'            ocvb.trueText("Enable Embedded NumPy/Python in the Global OptionsDialog")
'        End If
'    End Sub
'End Class






'' http://pythonnet.github.io/
'' https://github.com/pythonnet/pythonnet
'Public Class NumPy_EmbeddedCmd
'    Inherits VBparent
'    Dim embed = New CS_Classes.NumPy_EmbeddedCmd
'    Public Sub New(ocvb As VBocvb)
'        initParent(ocvb)
'        ocvb.desc = "Install a package on the embedded Python with Pip"
'    End Sub
'    Public Sub Run(ocvb As VBocvb)
'        If ocvb.parms.NumPyEnabled Then
'            embed.Run("from code import InteractiveConsole" + vbCrLf + "class Console(InteractiveConsole):" + vbCrLf + vbTab +
'                      "def __init__(*args): InteractiveConsole.__init__(*args)" + vbCrLf + "a = 0" + vbCrLf + "code = 'a = 1; print(a)'" + vbCrLf +
'                      "console = Console()" + vbCrLf + "console.runcode(code)" + vbCrLf + "print(a)")
'        Else
'            ocvb.trueText("Enable Embedded NumPy/Python in the Global OptionsDialog")
'        End If
'    End Sub
'End Class
#end if