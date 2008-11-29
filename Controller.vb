Imports Tibia
Imports Tibia.Objects
Imports Tibia.Packets
Imports Tibia.Util
Public Class Controller
    Public myClient As Client = ClientChooser.ShowBox()
    Public myPlayer As Player
    Public myEvents As tibAI.EventHandler
    Public myMotion As Movement
    Public Sub New()
        'Terminates if a client is not selected
        If myClient Is Nothing Then
            MsgBox("No client selected!")
            Application.Exit()
            Exit Sub
        End If
        'If the selected client is not logged in it starts the proxy 
        'and event handler
        If myClient.LoggedIn = False Then
            myClient.StartProxy()
            myEvents = New tibAI.EventHandler(Me)
        End If
    End Sub
    Public Sub RouteRepeat()
        If myMotion Is Nothing Then Exit Sub
        For i As Integer = 0 To 5
            myMotion.GotoXYZ(32229, 31771, 7)
            myMotion.GotoXYZ(32606, 31963, 7)
        Next i
    End Sub
    Public Sub GetXYZ()
        If myPlayer Is Nothing Then Exit Sub
        MsgBox(myPlayer.X & ", " & myPlayer.Y & ", " & myPlayer.Z)
    End Sub
    Public Sub Test()
        Dim WPQueue(11, 2) As Integer
        For i As Integer = 0 To 5
            WPQueue((i * 2), 0) = 32229
            WPQueue((i * 2), 1) = 31771
            WPQueue((i * 2), 2) = 7
            WPQueue((i * 2) + 1, 0) = 32606
            WPQueue((i * 2) + 1, 1) = 31963
            WPQueue((i * 2) + 1, 2) = 7

            'WPQueue((i * 2), 0) = 32229
            'WPQueue((i * 2), 1) = 31771
            'WPQueue((i * 2), 2) = 7
            'WPQueue((i * 2) + 1, 0) = 32339
            'WPQueue((i * 2) + 1, 1) = 31751
            'WPQueue((i * 2) + 1, 2) = 7
        Next
        myMotion.FollowWaypoints(WPQueue)
    End Sub
 

End Class
