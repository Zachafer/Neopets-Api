'Vb.Net Account Handler
'Uses  wrapper (class)


Imports System.Web 'Used for urlencode
Public Class account
    Public LastLoginResult As String 'Used to hold the last login info/error info. Usefull for giving more detail on why logins fail

    Public Function neologin(ByVal strusername As String, ByVal strpassword As String, ByVal thewrapper As httpwrapper) As Boolean
        'Login to neopets via a neopets.com account (http://www.neopets.com/login/index.phtml)
        Dim htmlholder As String 'A string to hold our login pages source code

        htmlholder = thewrapper.Request("GET", "http://www.neopets.com/login/index.phtml") 'First connect to neopets login page to set initial cookies(security measure)



        'Url encode username and password (otherwise a error will show with if the user has symbols in there username/pass)

        htmlholder = thewrapper.Request("POST", "http://www.neopets.com/login/index.phtml?password_popup=1?destination=&username=" & strusername & "&password=" & strpassword, thewrapper.LastPage) ' Now do a http post rewquest sending all the needed form data to login
        Form1.text1.Text = htmlholder
        If InStr(1, htmlholder, "badpassword") Then 'Check html contents for invalid logins
            LastLoginResult = ("Invalid Password/Username...")

            Return False
            Exit Function
        ElseIf InStr(1, htmlholder, "this account has been ") Then 'Check html contents for frozen account
            Return False
            LastLoginResult = ("This account has been frozen... :(")
            Exit Function
        ElseIf InStr(1, htmlholder, "index") Then 'Check html contents for valid login
            Return True
            LastLoginResult = ("success")

        Else
            Return False 'Html does nto contain any of the above so some error must of occured..
            LastLoginResult = ("Unknown Error...")
            Exit Function
        End If

        'If we got here we are logged in
    End Function
    Public Function URLEncode(ByVal StringToEncode As String, Optional ByVal _
       UsePlusRatherThanHexForSpace As Boolean = False) As String

        Dim TempAns As String
        Dim CurChr As Integer
        CurChr = 1
        Do Until CurChr - 1 = Len(StringToEncode)
            Select Case Asc(Mid(StringToEncode, CurChr, 1))
                Case 48 To 57, 65 To 90, 97 To 122
                    TempAns = TempAns & Mid(StringToEncode, CurChr, 1)
                Case 32
                    If UsePlusRatherThanHexForSpace = True Then
                        TempAns = TempAns & "+"
                    Else
                        TempAns = TempAns & "%" & Hex(32)
                    End If
                Case Else
                    TempAns = TempAns & "%" & _
                         Format(Hex(Asc(Mid(StringToEncode, _
                         CurChr, 1))), "00")
            End Select

            CurChr = CurChr + 1
        Loop

        URLEncode = TempAns
    End Function



End Class
