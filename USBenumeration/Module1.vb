Imports System.Management
Imports System.IO
Module Module1
    Sub Main()
        Console.WriteLine("Enumerating USB devices")
        Dim info As ManagementObject
        Dim search As ManagementObjectSearcher
        Dim Name As String
        search = New ManagementObjectSearcher("SELECT * From Win32_PnPEntity")
        Dim sw As New StreamWriter("c:\Temp\USBlist.txt")
        For Each info In search.Get()
            Name = CType(info("Caption"), String) ' Get the name of the device.'
            sw.WriteLine(Name)
        Next
        sw.Close()
    End Sub
End Module
