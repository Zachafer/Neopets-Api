Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.IO.Compression
Imports System.Net.Sockets

Public Class httpwrapper
    'Httpwrapper Credits to glurak and whoever converted this to vb.net
    'This is not my work except for some slight chagnges and tweaks where i see fit
    Implements ICloneable

    Private TCP_Client As TcpClient
    Private colCookies As Dictionary(Of String, String) = New Dictionary(Of String, String)
    Public strCookies As String = String.Empty
    Public LastPage As String = String.Empty

    Private pUseProxy As Boolean = False
    Private pProxyAddress As String = String.Empty
    Private pProxyPort As Integer = 80

    Public Const constHeaderUserAgent As String = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-GB; rv:1.9.2.8) Gecko/20100722 Firefox/3.6.8"

    Public headerAccept As String = "text/html,application/xhtml+xml,application/xml,0.9,*/*;q=0.8"
    Public headerUserAgent As String = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-GB; rv:1.9.2.8) Gecko/20100722 Firefox/3.6.8"
    Public headerAcceptLanguage As String = "en-us,en;q=0.5"
    Public headerAcceptCharset As String = "ISO-8859-1,utf-8;q=0.7,*;q=0.7"
    Public alternativePostdataSeparator As String = "µ"
    Public intMaxSendTime As Integer = 3000
    Public intMaxReceiveTime As Integer = 7500
    Public ExceptionCatcher As ExceptionCatcherSub = Nothing

    Public Delegate Sub ExceptionCatcherSub(ByVal sender As Object, ByVal Ex As Exception)

    Public Sub New()
        'Do nothing :)
    End Sub

    Public Sub New(ByVal ExCatcher As ExceptionCatcherSub)
        ExceptionCatcher = ExCatcher
    End Sub

    Public Function Request(ByVal Method As String, ByVal URL As String, Optional ByVal Referer As String = "") As String
        Try
            Dim Host As String = String.Empty
            Dim ReqHeaders As String = String.Empty

            Call PrepRequest(Method, URL, Referer, strCookies, headerUserAgent, Host, ReqHeaders)

            TCP_Client = New TcpClient
            TCP_Client.SendTimeout = intMaxSendTime
            TCP_Client.ReceiveTimeout = intMaxReceiveTime
            If pUseProxy Then
                TCP_Client.Connect(Host, pProxyPort)
            Else
                TCP_Client.Connect(Host, 80)
            End If
            Dim headers As Byte() = Encoding.ASCII.GetBytes(ReqHeaders)
            Dim ns As NetworkStream = TCP_Client.GetStream()
            ns.Write(headers, 0, headers.Length)
            Dim sr As StreamReader = New StreamReader(ns, Encoding.Default)
            Dim strReadHTML As String = sr.ReadToEnd()
            sr.Close()
            ns.Close()
            TCP_Client.Close()

            If strReadHTML.IndexOf(vbCrLf & vbCrLf) > 0 Then
                Dim strParts() As String = Split(strReadHTML, vbCrLf & vbCrLf, 2)
                strCookies = ParseCookies(strParts(0))
                If strParts(0).Contains("Content-Encoding") And Not Method = "HEAD" Then
                    strParts(1) = DecompressGzip(strParts(1))
                End If

                Return Join(strParts, vbCrLf & vbCrLf)
            Else

                Return strReadHTML
            End If
        Catch ex As Exception
            ExceptionHandler(ex)
            Return "-1" 'Error just return -1
        End Try
    End Function

    Private Function PrepRequest(ByVal Method As String, ByVal URL As String, ByVal Referer As String, ByVal strCookiesToUse As String, ByVal strUserAgentToUse As String, ByRef strHost As String, ByRef strReqHeaders As String)
        Dim Host As String = String.Empty
        Dim strFile As String = String.Empty
        Dim strPost As String = String.Empty
        Dim intPos As Integer = 0
        Dim strReqContent As String = String.Empty

        If Referer = String.Empty Then
            Referer = LastPage
        ElseIf Referer = "" Then
            Referer = String.Empty
        End If

        If URL.ToLower().Contains("http://") Then
            Host = URL.Substring(7)
        Else
            Host = URL
        End If

        If Host.Contains("/") Then
            intPos = Host.IndexOf("/", 0)
            strFile = Host.Substring(intPos)
            Host = Host.Substring(0, intPos)
        Else
            strFile = "/"
        End If

        If Method = "POST" Then
            intPos = strFile.IndexOf(alternativePostdataSeparator)
            If intPos <> -1 Then
                strPost = strFile.Substring(intPos + 1)
                strFile = strFile.Substring(0, intPos)
            Else
                intPos = strFile.IndexOf("?")
                If intPos <> -1 Then
                    strPost = strFile.Substring(intPos + 1)
                    strFile = strFile.Substring(0, intPos)
                Else
                    strPost = ""
                End If
            End If
        ElseIf Not Method = "HEAD" Then
            Method = "GET"
        End If

        If pUseProxy Then
            strFile = "http://" & Host & strFile
            Host = pProxyAddress
        End If

        LastPage = URL

        If Method = "POST" Then
            strReqContent = "Content-Type: application/x-www-form-urlencoded" & vbCrLf & "Content-Length: " & strPost.Length.ToString() & vbCrLf & vbCrLf & strPost
        Else
            strReqContent = vbCrLf
        End If

        If strUserAgentToUse = String.Empty Then
            strUserAgentToUse = constHeaderUserAgent
        End If

        strReqHeaders = Method & " " & strFile & " HTTP/1.1" & vbCrLf & _
        "Host: " & Host & vbCrLf & _
        "User-Agent: " & headerUserAgent & vbCrLf & _
        "Accept: " & headerAccept & vbCrLf & _
        "Accept-Language: " & headerAcceptLanguage & vbCrLf & _
        "Accept-Encoding: gzip, deflate" & vbCrLf & _
        "Accept-Charset: " & headerAcceptCharset & vbCrLf & _
        "Connection: close" & vbCrLf & _
        If(Not Referer = String.Empty, "Referer: " & Referer & vbCrLf, "") & _
        "Cookie: " & strCookies & vbCrLf & _
        strReqContent

        strHost = Host
        Return Nothing
    End Function
    Private Function DecompressGzip(ByVal Compressed As String) As String
        Dim memStream As MemoryStream = New MemoryStream(Encoding.Default.GetBytes(Compressed))
        Dim decompressStream As GZipStream = New GZipStream(memStream, CompressionMode.Decompress)

        Dim endBytes As Byte()
        ReDim endBytes(4)
        Dim intPosition As Integer = memStream.Length - 4
        memStream.Position = intPosition
        memStream.Read(endBytes, 0, 4)
        memStream.Position = 0
        Dim buffer As Byte()
        ReDim buffer(BitConverter.ToInt32(endBytes, 0) + 100)
        Dim intOffset As Integer = 0
        Dim intTotal As Integer = 0
        While (True)
            Dim intO As Integer = decompressStream.Read(buffer, intOffset, 100)
            If intO = 0 Then
                Exit While
            End If
            intOffset += intO
            intTotal += intO
        End While
        Return Encoding.Default.GetString(buffer)
    End Function

    Public Property UseProxy() As Boolean
        Get
            Return pUseProxy
        End Get
        Set(ByVal value As Boolean)
            pUseProxy = value
        End Set
    End Property

    Public Property ProxyAddress() As String
        Get
            Return pProxyAddress & ":" & pProxyPort
        End Get
        Set(ByVal value As String)
            Dim strParts() As String = Split(value, ":", 2)
            If UBound(strParts) = 1 Then
                pProxyAddress = strParts(0)
                pProxyPort = Val(strParts(1))
            End If
        End Set
    End Property

    Public Function StripHeaders(ByVal strSource As String) As String
        Return Split(strSource, vbCrLf & vbCrLf, 2)(1)
    End Function

    Public Function getimage(ByVal strURL As String, Optional ByVal strReferer As String = "") As Bitmap
        Try
            Dim strSource As String = String.Empty
            strSource = Request("GET", strURL, strReferer)
            Dim memStream As MemoryStream = New MemoryStream(Encoding.Default.GetBytes(StripHeaders(strSource)))
            Dim bMap As Bitmap = New Bitmap(memStream)
            Return bMap
        Catch ex As Exception
            'Return an empty image.. An empty image is better than no image, right :P ?
            ExceptionHandler(ex)
            Return New Bitmap(0, 0)
        End Try

    End Function

    Public Sub ClearCookies()
        colCookies.Clear()
        strCookies = String.Empty
    End Sub

    Public Sub AppendCookies(ByVal strCooksToAppend As String)
        Dim strCooks() As String = Split(strCooksToAppend, ";")
        For Each strCook As String In strCooks
            If strCook.IndexOf("=") > -1 Then
                strCookies = ParseCookies("set-cookie: " & strCook & ";")
            End If
        Next
    End Sub

    Public Sub SetCookies(ByVal strCooksToSet As String)
        ClearCookies()
        AppendCookies(strCooksToSet)
    End Sub

    Public Function GetCookies() As String
        Dim strCooks As String = String.Empty
        Try
            For Each colItem As KeyValuePair(Of String, String) In colCookies
                strCooks &= colItem.Value.ToString() & "; "
            Next
            Return strCooks.Substring(0, strCookies.Length - 1)
        Catch ex As Exception
            'Return the cookie string already saved if paring the collection fails. Some cookies are better than none, right :D ?
            ExceptionHandler(ex)
            Return strCookies
        End Try
    End Function

    Public Function ParseCookies(ByVal strHeaders As String) As String
        Try
            Dim ParseCooks As String = String.Empty
            Dim regMatches As MatchCollection
            Dim regX As Regex = New Regex("set-cookie:\s*([^=]+)=([^;]+);", RegexOptions.IgnoreCase)
            regMatches = regX.Matches(strHeaders)
            If regMatches.Count > 0 Then
                For Each regM As Match In regMatches
                    If colCookies.ContainsKey(regM.Groups(1).ToString()) Then
                        colCookies.Remove(regM.Groups(1).ToString())
                    End If
                    colCookies.Add(regM.Groups(1).ToString(), regM.Groups(1).ToString() & "=" & regM.Groups(2).ToString())
                Next
            End If
            For Each colItem As KeyValuePair(Of String, String) In colCookies
                ParseCooks &= colItem.Value.ToString() & "; "
            Next
            Return ParseCooks
        Catch ex As Exception
            'Return the cookie string already saved if paring the collection fails. Some cookies are better than none, right :D ?
            ExceptionHandler(ex)
            Return strCookies
        End Try
    End Function

    Private Sub ExceptionHandler(ByVal Ex As Exception)
        If Not ExceptionCatcher Is Nothing Then
            ExceptionCatcher.Invoke(Me, Ex)
        End If
    End Sub

    Public Function Clone() As Object Implements System.ICloneable.Clone
        Return DirectCast(MemberwiseClone(), httpwrapper)
    End Function
End Class