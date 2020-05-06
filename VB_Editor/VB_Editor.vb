Imports System.IO
Imports System.Text.RegularExpressions
Module VB_EditorMain
    Private Function makeChange(line As String) As String
        line = Mid(line, 1, InStr(line, "ocvb, ") + 5) + " callerName)"
        Return line
    End Function
    Sub Main()
        ' Regular expression are great but can be too complicated.  This app is just a simpler way to make global changes that 
        ' would normally be accomplished with regular expressions.
        Dim VBcodeDir As New DirectoryInfo(CurDir() + "/../../VB_Classes")
        Dim fileEntries As String() = Directory.GetFiles(VBcodeDir.FullName)

        Dim changeLines As Integer
        Dim changeFiles As Integer
        For Each fileName In fileEntries
            Dim nextFile As New System.IO.StreamReader(fileName)
            Dim saveChangeLines = changeLines
            While nextFile.Peek() <> -1
                Dim line As String
                line = Trim(nextFile.ReadLine())
                If line.Contains(" = New ") And line.Contains("(ocvb, """) Then
                    Console.WriteLine(line)
                    Console.WriteLine("Change to: " + makeChange(line))
                    changeLines += 1
                End If
            End While
            If saveChangeLines <> changeLines Then changeFiles += 1
        Next

        Dim response = InputBox("You must respond 'Yes' to make the changes.", "Make Changes?", "")
        If response = "Yes" Then

        End If
        'Dim FilesInfo As New FileInfo(VBcodeDir.FullName + "/../Data/FileNames.txt")
        'dim sw = New StreamWriter(FilesInfo.FullName)
        'For i = 0 To fileNames.Count - 1
        '    sw.WriteLine(fileNames.Item(i))
        'Next
        'sw.Close()
        Console.WriteLine(CStr(changeLines) + " matching lines found in " + CStr(changeFiles) + " files")
    End Sub
End Module
