
Public Class Form1
    Public mywrapper As New httpwrapper 'Create a new httpwrapper class instance
    Dim accounthandler As New account 'Create a new account class instance


    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        If accounthandler.neologin("user", "password", mywrapper) = True Then
            MsgBox("logged in")
        End If
    End Sub

    Private Sub GroupBox1_Enter(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GroupBox1.Enter

    End Sub
End Class
