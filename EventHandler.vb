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
    Dim WithEvents client As Tibia.Objects.Client
    Dim WithEvents proxy As Tibia.Packets.Proxy

    Public Sub New(ByRef con As Controller)
        'sets up the global variables
        myController = con
        ReDim myPath.Path(10)

        'Initializes the client and proxy objects
        client = myController.myClient
        proxy = client.IO.Proxy
    End Sub

    Public Function ReceivedMoveOutgoingPacketEvent(ByVal packets As OutgoingPacket) As Boolean Handles proxy.ReceivedMoveOutgoingPacket
        Dim myNetMsg As New Tibia.Packets.NetworkMessage
        packets.ToNetworkMessage(myNetMsg)
        RecordMovement(myNetMsg.Data)
        Return True
    End Function
    Public Function ReceivedAutoWalkOutgoingPacketEvent(ByVal packets As OutgoingPacket) As Boolean Handles proxy.ReceivedAutoWalkOutgoingPacket
        Dim myNetMsg As New Tibia.Packets.NetworkMessage
        packets.ToNetworkMessage(myNetMsg)
        RecordMovement(myNetMsg.Data)
        Return True
    End Function
    Public Function ReceivedTextMessageIncomingPacketEvent(ByVal packets As Tibia.Packets.IncomingPacket) As Boolean Handles proxy.ReceivedTextMessageIncomingPacket
        'This function handles events raised when the server sends a status 
        'message


        Dim p As Tibia.Packets.Incoming.TextMessagePacket
        p = CType(packets, Tibia.Packets.Incoming.TextMessagePacket)


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
                myController.myPathfinder.myMapData.SetPZLock(60) '1 = 1 second

                tileType = 3
            End If
            'flags a tile as special
            myController.myPathfinder.myMapData.AddTiletoList(tempx, tempy, tempz, tileType)

        End If


        Return True
    End Function

    Public Function PlayerLoginEvent() As Boolean Handles proxy.PlayerLogin
        myController.myPlayer = myController.myClient.GetPlayer
        myController.myMovement = New Movement(myController.myClient, myController.myPlayer, myController)
        Return True
    End Function

    Private Sub RecordMovement(ByVal data() As Byte)
        'This function handles outgoing movement events
        'First it records the originating location
        myPath.StartLocation_X = myController.myPlayer.X
        myPath.StartLocation_Y = myController.myPlayer.Y
        myPath.StartLocation_Z = myController.myPlayer.Z
        myPath.PathLength = 1 'sets the path length to 1 by default
        'fills the packet with path data based on the type of packet
        '100 = multimove packet
        If data(0) = 100 Then
            myPath.PathLength = data(1) - 1
            For i As Integer = 2 To UBound(data)
                myPath.Path(i - 2) = data(i)
            Next
        ElseIf data(0) = 101 Then
            myPath.Path(0) = 3 'up
        ElseIf data(0) = 102 Then
            myPath.Path(0) = 1 'right
        ElseIf data(0) = 103 Then
            myPath.Path(0) = 7 'down
        ElseIf data(0) = 104 Then
            myPath.Path(0) = 5 'left
        ElseIf data(0) = 106 Then
            myPath.Path(0) = 2 'up-right
        ElseIf data(0) = 107 Then
            myPath.Path(0) = 8 'down-right
        ElseIf data(0) = 108 Then
            myPath.Path(0) = 6 'down-left
        ElseIf data(0) = 109 Then
            myPath.Path(0) = 4 'up-left
        End If
    End Sub
End Class
