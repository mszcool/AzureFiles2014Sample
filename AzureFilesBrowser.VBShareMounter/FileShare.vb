'
' MIT License 
' Copyright (c) 2014 Microsoft. All rights reserved.
'
' Permission is hereby granted, free of charge, to any person 
' obtaining a copy of this software and associated documentation 
' files (the "Software"), to deal in the Software without restriction, including 
' without limitation the rights to use, copy, modify, merge, publish, distribute, 
' sublicense, and/or sell copies of the Software, and to permit persons to 
' whom the Software is furnished to do so, subject to the following conditions: 
' 
' The above copyright notice and this permission notice shall be included 
' in all copies or substantial portions of the Software. 
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
' EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
' MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
' IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
' DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
' ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
' DEALINGS IN THE SOFTWARE.
'

Imports System.Runtime.InteropServices

Public Class FileShare

    Private Declare Function WNetAddConnection2 Lib "mpr.dll" Alias "WNetAddConnection2A" _
                                (ByRef lpNetResource As NETRESOURCE, _
                                 ByVal lpPassword As String, _
                                 ByVal lpUserName As String, _
                                 ByVal dwFlags As Integer) As Integer

    Private Declare Function WNetCancelConnection2 Lib "mpr.dll" Alias "WNetCancelConnection2A" _
                                (ByVal lpName As String, _
                                 ByVal dwFlags As Integer, _
                                 ByVal fForce As Integer) As Integer

    Private Const ForceDisconnect As Integer = 1
    Private Const RESOURCETYPE_DISK As Long = &H1

    <StructLayout(LayoutKind.Sequential)> _
    Private Structure NETRESOURCE
        Public dwScope As Integer
        Public dwType As Integer
        Public dwDisplayType As Integer
        Public dwUsage As Integer
        Public lpLocalName As String
        Public lpRemoteName As String
        Public lpComment As String
        Public lpProvider As String
    End Structure

    'Function to map drive/connect to drive
    Public Shared Sub MountShare(ByVal shareName As String, ByVal driveLetterAndColon As String, ByVal userName As String, ByVal password As String)

        If Not String.IsNullOrEmpty(driveLetterAndColon) Then
            UnmountShare(driveLetterAndColon, False)
        End If

        Dim nr As NETRESOURCE
        nr = New NETRESOURCE
        nr.lpRemoteName = shareName
        nr.lpLocalName = driveLetterAndColon

        nr.dwType = RESOURCETYPE_DISK

        Dim result As Integer
        result = WNetAddConnection2(nr, password, userName, 0)

        If result <> 0 Then
            Throw New Exception(String.Format("Unable to mount the file share due to error code {0}", result))
        End If

    End Sub

    'Function to disconnect from drive
    Public Shared Sub UnmountShare(ByVal driveLetterAndColon As String, Optional ByVal throwException As Boolean = True)
        Dim rc As Integer
        rc = WNetCancelConnection2(driveLetterAndColon, 0, ForceDisconnect)

        If rc <> 0 Then
            Dim msg As String = String.Format("Unable to cancel file share connection, return code {0}", rc)
            If throwException Then
                Throw New Exception(msg)
            Else
                Trace.TraceError(msg)
            End If
        End If

    End Sub

End Class
