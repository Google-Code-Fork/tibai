Imports Tibia.Util.Memory
Imports System.Threading
Imports System.IO
'Manages all data for movement related purposes
Public Class MapData
    'The MapData class manages the storage and retrieval of geographically significant
    'data. Information such as monster locations, item locations, pathfinder information
    'etc might be managed with this class

    'Arrays used to store information about the map
    Private MDOs(9) As MDatObj 'a cache of macro file objects 
    Private MDOsPointer As Integer 'the pointer to the most recently loaded MFO
    'Protection Zone Lock information
    Private isPZlocked As Boolean = False 'Default is false, if the player is PZLocked safety zones are ignored
    Private PZLockedSetTime As DateTime
    Private PZLockDuration As TimeSpan

    Public Sub New()
        For i As Integer = 0 To UBound(Maps)
            Maps(i) = FirstMapAddy + (i * (&H20000 + MapFileOffset))
        Next
        CreateMapDirectories()
        'UpdateMacroTiles()
    End Sub
    Public Function GetTileCost(ByVal Tx As Integer, ByVal Ty As Integer, ByVal Tz As Integer, ByVal blockSpecial As Boolean) As Byte
        'Returns the movement cost of a given tile- default is same as unexplored
        'Tries to reset isPZLocked
        If isPZlocked Then
            If PZLockedSetTime.Ticks + PZLockDuration.Ticks < Now.Ticks Then isPZlocked = False
        End If

        Dim mapInfo As IO.FileInfo = New FileInfo(MapsDir.FullName & NameBase(Tx, Ty, Tz) & ".map")
        'if the map file exists
        Dim isload, inmem, toffset As Integer
        isload = isLoaded(mapInfo.Name) '   'ensure the mDat is loaded
        inmem = InMemory(Tx, Ty, Tz)
        toffset = GetTileOffset(Ty) + (GetTileOffset(Tx) * 256)
        If mapInfo.Exists = True Then
            '   'if the tile is illegal
            If isIllegal(MDOs(isload).TibMap.Name, toffset, inmem) And blockSpecial Then
                '   '   return a blocking value
                Return &HFF

                '   'if the tile is legal
            Else
                '   '   'if the tile is in tibia's memory
                If Not inmem = -1 Then
                    '   '   '   'return value from tibias memory
                    GetTileCost = ReadByte(Handle, Maps(inmem) + MapOffset + &H10000 + toffset)
                    'checks to see if it should write a color to tibia's memory
                    If DebugMap And GetTileCost < 255 Then
                        WriteByte(Handle, Maps(inmem) + MapOffset + toffset, DebugColor)
                    End If

                    Return GetTileCost
                    '   '   'if the tile is not in tibias memory
                Else
                    '   '   '   'return the value from the mapfile
                    Return MDOs(isload).MovCostMap(toffset)
                End If
            End If
        Else
            'if the tile is in tibias memory
            If Not inmem = -1 Then
                '   'return the value from tibias memory
                GetTileCost = ReadByte(Handle, Maps(inmem) + MapOffset + &H10000 + toffset)
                'checks to see if it should write a color to tibia's memory
                If DebugMap And GetTileCost < 255 Then
                    WriteByte(Handle, Maps(inmem) + MapOffset + toffset, DebugColor)
                End If

                Return GetTileCost
                'if the tile is not in tibias memory
            Else
                '   'return an unexplored value
                Return &HFA
            End If

        End If

    End Function
    Public Function GetNodeId(ByVal TibMapName As String, ByVal fOffset As Integer, ByVal inMem As Integer) As Integer
        'returns the id of the node in that file

        Dim isload As Integer
        'checks to see if the file is loaded
        isload = isLoaded(TibMapName)
        'if no map has been initialized then return false
        If MDOs(isload).NodeMap Is Nothing Then Return 0

        'read the value and return the correct response
        Dim myTile As Byte = MDOs(isload).NodeMap(fOffset)

        Return myTile
    End Function
    Public Function GetTileID(ByVal Tx As Integer, ByVal Ty As Integer, ByVal Tz As Integer) As Byte
        'Returns the movement cost of a given tile- default is same as unexplored

        Dim mapInfo As IO.FileInfo = New FileInfo(MapsDir.FullName & NameBase(Tx, Ty, Tz) & ".map")
        Dim isload, inmem, toffset As Integer
        isload = isLoaded(mapInfo.Name)
        inmem = InMemory(Tx, Ty, Tz)
        toffset = GetTileBase(Ty) + (GetTileBase(Tx) * 256)
        'if tile is in tibia memory 
        If Not inmem = -1 Then
            '   'return value in tibias memory
            Return ReadByte(Handle, Maps(inmem) + MapOffset + toffset)
            'if tile is not in tibias memory
        Else
            '   'if map file exists
            If mapInfo.Exists = True Then
                '   '   'return the value from the mdat
                Return MDOs(isload).MiniMap(toffset)
                '   'if map file does not exist
            Else
                '   '   'return the unexplored default
                Return 0
                '   'end if
            End If

            'end if
        End If

    End Function
    Private Function GetTileBase(ByVal TileXorY As Integer) As Integer
        'Returns the base of a coordinate for use in identifying the map file
        Return CInt(Math.Truncate(TileXorY / 256))
    End Function
    Public Function GetTileOffset(ByVal TileXorY As Integer) As Integer
        'gets the offset to find the tile in the map file
        Return TileXorY - ((CInt(Math.Truncate(TileXorY / 256))) * 256)
    End Function
    Private Function InMemory(ByVal TileX As Integer, ByVal TileY As Integer, ByVal TileZ As Integer) As Integer
        'Checks tibias memory for the map file
        '-1 for not present or 0 - 9 if present
        InMemory = -1
        For i As Integer = 0 To 9
            If ReadByte(Handle, Maps(i)) = GetTileBase(TileX) And ReadByte(Handle, Maps(i) + 4) = GetTileBase(TileY) And ReadByte(Handle, Maps(i) + 8) = TileZ Then
                InMemory = i
                Exit For
            End If
        Next
    End Function
    Private Function isLoaded(ByRef filename As String) As Integer
        ' = -1 if not loaded 0 - 9 if it is loaded
        'now auto-loads files
        isLoaded = -1
        For i As Integer = 0 To UBound(MDOs)
            If Not MDOs(i) Is Nothing Then
                If filename = MDOs(i).TibMap.Name Then
                    isLoaded = i
                    Exit For
                End If
            End If
        Next
        If isLoaded = -1 Then
            'save the previous mdo
            If Not MDOs(MDOsPointer) Is Nothing Then
                'then save the mdo
                MDOs(MDOsPointer).Save()
            End If
            'clear the mdo
            MDOs(MDOsPointer) = Nothing
            'load the new MDO
            MDOs(MDOsPointer) = New MDatObj(filename)
            isLoaded = MDOsPointer
            'advances the pointer
            If MDOsPointer = MDOs.Count - 1 Then MDOsPointer = 0 Else MDOsPointer += 1
        End If
    End Function
    Protected Overrides Sub Finalize()

        ' saves the files stored in memory
        For i As Integer = 0 To UBound(MDOs)
            If Not MDOs(i) Is Nothing Then
                MDOs(i).Save()
            End If
        Next

    End Sub

    'Data Related Methods
    Private Sub CreateMapDirectories()
        'Creates the directories that hold map data from TibA.I.
        If Not My.Computer.FileSystem.DirectoryExists(MapInfoDir) Then My.Computer.FileSystem.CreateDirectory(MapInfoDir)
    End Sub

    'mDat Methods
    Private Function isIllegal(ByVal MDOMapName As String, ByVal fOffset As Integer, ByVal inMem As Integer) As Boolean
        'returns true if the tile is illegal to walk on

        Dim isload As Integer
        'checks to see if the file is loaded
        isload = isLoaded(MDOMapName)
        'if no map has been initialized then
        If MDOs(isload).SpecialMap Is Nothing Then
            Return False
        End If

        'read the value and return the correct response
        'if its a special tile then tibia's memory has to be updated so that 
        'tibia's pathfinder doesn't freak out in unexplored areas
        Dim myTile As Byte = MDOs(isload).SpecialMap(fOffset)
        If myTile < 3 And myTile > 0 Then
            'its a door
            If inMem > -1 Then
                'Writes a blocking value to tibia's memory
                WriteByte(Handle, Maps(inMem) + MapOffset + fOffset + &H10000, &HFF)
            End If
            Return True
        ElseIf myTile > 1 And isPZlocked = True Then
            'its a Protection zone and the character was blocked recently
            If inMem > -1 Then
                'Writes a blocking value to tibia's memory
                WriteByte(Handle, Maps(inMem) + MapOffset + fOffset + &H10000, &HFF)
            End If
            Return True
        ElseIf myTile = 0 Then
            'is nothing
            Return False
        End If
    End Function
    Public Sub MarkAsSpecial(ByVal Tx As Integer, ByVal Ty As Integer, ByVal Tz As Integer, ByVal type As Byte)
        'Called when a tile has  been identified as being a 
        'non-qualified step, it adds the specific type of tile to the map

        'identify the MDO
        Dim mapName As String = NameBase(Tx, Ty, Tz) & ".map"
        'load the mdo
        Dim mapPos As Integer = isLoaded(mapName)
        'add the tile to the list
        MDOs(mapPos).MarkSpecialTile(GetTileOffset(Tx), GetTileOffset(Ty), type)
        'Changes the tile in tibia's memory to assist tibia's automap
        Dim inmem As Integer = InMemory(Tx, Ty, Tz)
        If inmem > -1 Then
            'gets the file position of the tile
            Dim tOffset As Integer
            tOffset = GetTileOffset(Ty) + (GetTileOffset(Tx) * 256)
            'Writes a blocking value to tibia's memory
            Dim test As Integer = Maps(9)
            WriteByte(Handle, Maps(inmem) + MapOffset + tOffset + &H10000, &HFF)
        End If
    End Sub
    Public Function GetmTile(ByVal mapname As String, ByVal tileid As Integer) As mTile
        'loads the mdat if necessary
        Dim mapPos As Integer = isLoaded(mapname)
        'returns the mTile object
        Return MDOs(mapPos).MacroTiles(tileid)
    End Function
    Public Function isConnected(ByRef mtile1 As mTile, ByVal node1 As Integer, ByRef mtile2 As mTile, ByVal node2 As Integer) As Boolean
        'determines if mtile1.node1 has a connection to mtile2.node2
        For Each c In mtile1.Connections
            If c.SourceNode = node1 And c.TargetTile = mtile2.id.mTileNumber And c.TargetNode = node2 Then Return True
        Next
    End Function


    Public Sub MarkNodeId(ByVal tileX As Integer, ByVal tileY As Integer, ByVal tileZ As Integer, ByVal id As Byte)
        'Called when a tile needs to be branded with a nodeid in the node map of an mdo
        'identify the MDO
        Dim mapName As String = NameBase(tileX, tileY, tileZ) & ".map"
        'load the mdo
        Dim mapPos As Integer = isLoaded(mapName)
        'add the tile to the list
        MDOs(mapPos).MarkNodeID(GetTileOffset(tileX), GetTileOffset(tileY), id)
    End Sub

    'miscellaneous - should be reviewed
    Public Function NameBase(ByVal x As Integer, ByVal y As Integer, ByVal z As Integer) As String
        Dim zStr As String
        If z < 10 Then zStr = "0" & CStr(z) Else zStr = CStr(z)
        NameBase = CStr(GetTileBase(x) & GetTileBase(y) & zStr)
    End Function
    Public Sub SetPZLock(ByVal Duration As Long)
        'turns on a pzlock for a set amount of time, this could be improved
        isPZlocked = True
        PZLockDuration = New TimeSpan(Duration * TimeSpan.TicksPerSecond)
        PZLockedSetTime = Now
    End Sub
    Public Function GetMapFileAssessment() As Integer(,)
        'loop through every mapfile and record tiles with identical color and movement cost
        'Objects used to read and write to various files
        Dim binReader As System.IO.BinaryReader 'used to load a map file into tibiaAI's memory
        Dim fStream As System.IO.FileStream 'used to load a map file into tibiaAI's memory

        Dim MapData(1000, 2) As Integer '0=id,1=cost,2=number
        Dim tilecount As Integer 'number of different tiles found
        Dim aryFi As System.IO.FileInfo() = MapsDir.GetFiles("*.map")
        Dim bytearray() As Byte
        Dim mcost, tileid As Byte
        Dim isfound As Integer

        'for each file in the mapfolder
        For Each file As System.IO.FileInfo In aryFi
            'fills the bytearray variable with the map files data
            fStream = New System.IO.FileStream(file.FullName, IO.FileMode.Open)
            binReader = New System.IO.BinaryReader(fStream)
            bytearray = binReader.ReadBytes(CInt(binReader.BaseStream.Length))
            binReader.Close()
            fStream.Close()
            'for each tile in the map file
            For a As Integer = 0 To &HFFFF
                '       get the mcost and tileid
                tileid = bytearray(a) 'gets the color of the tile
                mcost = bytearray(a + &H10000) 'gets the movement cost of the tile
                '       check the list to see if its added
                isfound = -1
                For i As Integer = 0 To tilecount
                    If tileid = MapData(i, 0) And mcost = MapData(i, 1) Then
                        isfound = i 'found it
                        Exit For
                    End If
                Next
                If Not isfound = -1 Then
                    '       if it is already on the list then increment the number
                    MapData(isfound, 2) += 1
                Else
                    '       if its not added then add it to the list
                    MapData(tilecount, 0) = tileid
                    MapData(tilecount, 1) = mcost
                    '       increment tilecount
                    tilecount += 1
                End If
            Next

        Next

        'add the found items to the return variable
        Dim ReturnVar(tilecount - 1, 2) As Integer
        For a As Integer = 0 To tilecount
            ReturnVar(tilecount, 0) = MapData(a, 0)
            ReturnVar(tilecount, 1) = MapData(a, 1)
            ReturnVar(tilecount, 2) = MapData(a, 2)
        Next
        Return ReturnVar
    End Function
    Public Sub SchedulePlayersmTile()
        Dim myMapName As String
        Dim myTileID As Integer
        'gets the mapname
        myMapName = NameBase(myPlayer.X, myPlayer.Y, myPlayer.Z) & ".map"
        'calculates the tileid
        myTileID = CInt(Math.Truncate((myPlayer.Y - MinY) / 16))
        myTileID -= CInt(Math.Truncate((myPlayer.Y - MinY) / 256) * 16)
        myTileID = myTileID * 16
        myTileID += CInt(Math.Truncate((myPlayer.X - MinX) / 16))
        myTileID -= CInt(Math.Truncate((myPlayer.X - MinX) / 256) * 16)
        'gets the mtile
        Dim mymtileid As mTile.tileID = GetmTile(myMapName, myTileID).id
        'sets flags to false so that the dataminer wont skip it
        mymtileid.MDO.MacroTiles(mymtileid.tileID).Mapped = False
        mymtileid.MDO.MacroTiles(mymtileid.tileID).Discovered = False
        'enqueues the tile
        Mappable.Enqueue(mymtileid)

    End Sub

    '12712407.map@60db = TileColor
    '12712407.map@160db = TileMovementCost
    '12712407.map@160db = 32608, 31963,7
    'xxxyyyzz.map@Nxxyy
    '127 * 256 + hex[60](96) = 32608
    '124 * 256 + hex[db](219) = 31963

    'Minimap info
    'Map Data is stored in 10 locations in tibia's memory
    'to check if a tile is contained within its memory its necessary to check
    'the 10 locations for the appropriate file coordinates
    '6dec58 = 32365, 31784
End Class
