Imports Tibia
Imports Tibia.Objects
Imports Tibia.Packets
Imports Tibia.Util
Public Class EventHandler
    Public Structure CurrentPath
        'holds details about the short term(<10 steps) path that was last 
        'submitted
        Dim StartLocation_X As Integer 'Where the character was
        Dim StartLocation_Y As Integer 'when then path was sent
        Dim StartLocation_Z As Integer 'to the server
        Dim Path() As Byte 'The path submitted to the server
        Dim PathLength As Integer 'The number of steps in the path
    End Structure
    Public myPath As CurrentPath 'Contains info on the last movement packet sent
    Public myController As Controller 'the central class that contains all relevant objects

    Public Sub New(ByRef con As Controller)
        'sets up the global variables
        myController = con
        ReDim myPath.Path(10)

        'If the proxy is initialized
        If Not myController.myClient.Proxy Is Nothing Then
            'Starts Event Handlers
            myController.myClient.Proxy.OnLogIn = New Proxy.ProxyNotification(AddressOf OnLogInEvent)
            myController.myClient.Proxy.ReceivedPacketFromClient = New Proxy.PacketListener(AddressOf PacketFromClientEvent)
            myController.myClient.Proxy.ReceivedStatusMessagePacket = New Proxy.PacketListener(AddressOf StatusMessageEvent)
        End If
    End Sub
    Public Function OnLogInEvent(ByVal msg As String) As Boolean
        If myController.myClient.LoggedIn Then
            myController.myPlayer = myController.myClient.GetPlayer
            myController.myMotion = New Movement(myController.myClient, myController.myPlayer)
        Else
            Exit Function
        End If
        Return True
    End Function
    Public Function PacketFromClientEvent(ByVal packets As Packet) As Boolean
        'This function handles events raised when the client sends a packet

        'Exits the sub if not logged in
        If Not myController.myClient.LoggedIn Then Exit Function
        'This part looks for movement packets
        If packets.Data(2) > 99 And packets.Data(2) < 105 Then
            'First it records the originating location
            myPath.StartLocation_X = myController.myPlayer.X
            myPath.StartLocation_Y = myController.myPlayer.Y
            myPath.StartLocation_Z = myController.myPlayer.Z
            myPath.PathLength = 1 'sets the path length to 1 by default
            'fills the packet with path data based on the type of packet
            '100 = multimove packet
            '101 = up, 102 = right, 103 = down, 104 = left
            If packets.Data(2) = 100 Then
                myPath.PathLength = packets.Data(3) - 1
                For i As Integer = 4 To UBound(packets.Data)
                    myPath.Path(i - 4) = packets.Data(i)
                Next
            ElseIf packets.Data(2) = 101 Then
                myPath.Path(0) = 3 'up
            ElseIf packets.Data(2) = 102 Then
                myPath.Path(0) = 1 'right
            ElseIf packets.Data(2) = 103 Then
                myPath.Path(0) = 7 'down
            ElseIf packets.Data(2) = 104 Then
                myPath.Path(0) = 5 'left
            End If
        End If
        Return True
    End Function
    Public Function StatusMessageEvent(ByVal packets As Packet) As Boolean
        'This function handles events raised when the server sends a status 
        'message

        'Exits the sub if not logged in
        If Not myController.myClient.LoggedIn Then Exit Function

        Dim p As New Tibia.Packets.StatusMessagePacket(myController.myClient, packets.Data)

        'Checks for messages associated with invalid moves
        If p.Message = "You are not invited." Or p.Message = "Characters who attacked other players may not enter a protection zone." Then

            'finds the failed step from the mypath object
            Dim tempx, tempy, tempz As Integer
            tempx = myPath.StartLocation_X
            tempy = myPath.StartLocation_Y
            tempz = myPath.StartLocation_Z
            For i As Integer = 0 To myPath.PathLength - 1
                'if the current location is found it adds the next step and 
                'exits the for-next loop
                If myController.myPlayer.X = tempx And myController.myPlayer.Y = tempy Then
                    'applies the step on the x coordinate
                    If myPath.Path(i) < 3 Or myPath.Path(i) = 8 Then tempx += 1
                    If myPath.Path(i) > 3 And myPath.Path(i) < 7 Then tempx -= 1
                    'applies the step on the y coordinate
                    If myPath.Path(i) > 1 And myPath.Path(i) < 5 Then tempy -= 1
                    If myPath.Path(i) > 5 Then tempy += 1
                    Exit For
                End If

                'applies the step on the x coordinate
                If myPath.Path(i) < 3 Or myPath.Path(i) = 8 Then tempx += 1
                If myPath.Path(i) > 3 And myPath.Path(i) < 7 Then tempx -= 1
                'applies the step on the y coordinate
                If myPath.Path(i) > 1 And myPath.Path(i) < 5 Then tempy -= 1
                If myPath.Path(i) > 5 Then tempy += 1

            Next
            Dim tileType As Byte
            If p.Message = "You are not invited." Then
                'its a private door
                tileType = 1
            Else
                'its a PZ lock
                'sets a 60 second PZLocktimer if the character is PZLocked
                myController.myMotion.myPathfinder.myMapData.SetPZLock(60) '1 = 1 second

                tileType = 3
            End If
            'flags a tile as special
            myController.myMotion.myPathfinder.myMapData.AddTiletoList(tempx, tempy, tempz, tileType)

        End If


        Return True
    End Function

End Class
