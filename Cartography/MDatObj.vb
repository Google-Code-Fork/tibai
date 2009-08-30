Imports System.IO
Public Class MDatObj
    'This object holds all the information and functionality for a 256x256 chunk of 
    'the world of tibia including its .map file. This information is stored between 
    'the tibia generated .map files and the TibA.I. generated .mDat files.

    'tibia map arrays
    Public MiniMap() As Byte 'holds the minimap info from the .map file
    Public MovCostMap() As Byte 'holds the .map movement costs
    'generataed map data arrays
    Public NodeMap() As Byte 'holds the marked macrotile map
    Public SpecialMap() As Byte 'holds the special tile coordinates
    Public MacroTiles() As mTile 'holds macro tile information
    'holds some basic information for this map file
    Public Namebase As String
    Public BaseX As Byte
    Public BaseY As Byte
    Public BaseZ As Byte
    Public TibMap As FileInfo 'the .map file this is based on
    Public mDat As FileInfo 'the .mdat file this object represents
    Public inUse As Boolean 'a flag that is raised while this object is being used by a thread

    Private Sub Initialize(ByVal mInfo As IO.FileInfo)

        'initializes the fileinfo objects
        'the fileinfo object for the tibia mapfile
        TibMap = mInfo
        'the file that stores the object
        mDat = New FileInfo(MapInfoDir & Left(TibMap.Name, 8) & ".mdat")
        'gets the base x,y,z variables
        GetBaseXYZ(TibMap.Name)

        'clears any existing data
        NodeMap = Nothing
        MiniMap = Nothing
        MovCostMap = Nothing
        SpecialMap = Nothing

        Dim fstream As FileStream

        'if the tibia map file exists
        If TibMap.Exists Then
            'loads the tibia map
            fstream = New FileStream(TibMap.FullName, FileMode.Open)
            ReDim MiniMap(&H10000 - 1)
            fstream.Read(MiniMap, 0, &H10000)
            ReDim MovCostMap(&H10000 - 1)
            fstream.Read(MovCostMap, 0, &H10000)
            fstream.Flush()
            fstream.Close()
        End If
        'loads the mdat file if it exists
        Dim mtilenum As Integer
        If mDat.Exists Then
            fstream = New FileStream(mDat.FullName, FileMode.Open)
            'first byte indicates if a node map exists
            If CBool(fstream.ReadByte) Then
                ReDim NodeMap(&H10000)
                fstream.Read(NodeMap, 0, &H10000)
            End If
            'next byte indicates if a special map exists
            If CBool(fstream.ReadByte) Then
                ReDim SpecialMap(&H10000)
                fstream.Read(SpecialMap, 0, &H10000)
            End If
            'next byte indicates if connection data exists

            If CBool(fstream.ReadByte) Then
                'initializes the macrotiles structure and its macronodeobjects
                ReDim MacroTiles(255)
                Dim numCons As Integer
                Dim myCon As mTile.Connection
                For n As Integer = 0 To 255
                    'gets the number of tiles above this one
                    mtilenum = ((CInt(Math.Truncate(((BaseY * 256) - MinY) / 16)) + CInt(Math.Truncate(n / 16))) * CInt(Math.Truncate(MapX / 16)))
                    'adds in the number of tiles to the left of this one
                    mtilenum += CInt(Math.Truncate(((BaseX * 256) - MinX) / 16)) + (n - (CInt(Math.Truncate(n / 16) * 16)))
                    MacroTiles(n) = New mTile(n, Me, mtilenum)
                    MacroTiles(n).Mapped = CBool(fstream.ReadByte)
                    'if its mapped then its also discovered but if its not mapped we assume it needs to be discovered because child data isn't stored unless its mapped
                    MacroTiles(n).Discovered = MacroTiles(n).Mapped
                    If MacroTiles(n).Mapped Then
                        numCons = fstream.ReadByte
                        For i As Integer = 1 To numCons
                            myCon.SourceNode = CByte(fstream.ReadByte)
                            myCon.TargetTile = CInt((fstream.ReadByte * 256) + fstream.ReadByte)
                            myCon.TargetNode = CByte(fstream.ReadByte)
                            MacroTiles(n).Connections.Push(myCon)
                        Next
                    End If
                    If MacroTiles(n).Mapped = False Then
                        Mappable.Enqueue(MacroTiles(n).id)
                    End If
                Next
            Else
                'schedule these mtiles to be updated
                ReDim MacroTiles(255)
                For n As Integer = 0 To 255
                    'gets the number of tiles above this one
                    mtilenum = ((CInt(Math.Truncate(((BaseY * 256) - MinY) / 16)) + CInt(Math.Truncate(n / 16))) * CInt(Math.Truncate(MapX / 16)))
                    'adds in the number of tiles to the left of this one
                    mtilenum += CInt(Math.Truncate(((BaseX * 256) - MinX) / 16)) + (n - (CInt(Math.Truncate(n / 16)) * 16))
                    'create the tile object
                    MacroTiles(n) = New mTile(n, Me, mtilenum)
                    'add the tile to the mappable queue
                    Mappable.Enqueue(MacroTiles(n).id)
                Next

            End If
            fstream.Flush()
            fstream.Close()
        Else
            'schedule these mtiles to be updated
            ReDim MacroTiles(255)
            For n As Integer = 0 To 255
                'gets the number of tiles above this one
                mtilenum = ((CInt(Math.Truncate(((BaseY * 256) - MinY) / 16)) + CInt(Math.Truncate(n / 16))) * CInt(Math.Truncate(MapX / 16)))
                'adds in the number of tiles to the left of this one
                mtilenum += CInt(Math.Truncate(((BaseX * 256) - MinX) / 16)) + (n - (CInt(Math.Truncate(n / 16)) * 16))
                'create the tile object
                If mtilenum < 0 Then
                    Application.DoEvents()
                End If
                MacroTiles(n) = New mTile(n, Me, mtilenum)
                'add the tile to the mappable queue
                Mappable.Enqueue(MacroTiles(n).id)
            Next
        End If

    End Sub
    Public Sub New(ByVal fromMapInfo As IO.FileInfo)
        Initialize(fromMapInfo)
    End Sub
    Public Sub New(ByVal mapFileName As String)
        TibMap = New FileInfo(MapsDir.FullName & mapFileName)
        Initialize(TibMap)
    End Sub
    Public Sub Save()
        'Saves an .mdat file in the appropriate format
        Dim fstream As FileStream
        Dim mycon As mTile.Connection
        fstream = New IO.FileStream(mDat.FullName, FileMode.Create, FileAccess.Write)

        If Not NodeMap Is Nothing Then
            fstream.WriteByte(CByte(True))
            fstream.Write(NodeMap, 0, &H10000)
        Else
            fstream.WriteByte(CByte(False))
        End If
        If Not SpecialMap Is Nothing Then
            fstream.WriteByte(CByte(True))
            fstream.Write(SpecialMap, 0, &H10000)
        Else
            fstream.WriteByte(CByte(False))
        End If
        If Not MacroTiles Is Nothing Then
            fstream.WriteByte(CByte(True))
            For Each mtile In MacroTiles
                If mtile.Mapped Then
                    fstream.WriteByte(CByte(True))
                    fstream.WriteByte(CByte(mtile.Connections.Count))
                    Do While mtile.Connections.Count > 0
                        mycon = mtile.Connections.Pop
                        fstream.WriteByte(mycon.SourceNode)
                        fstream.WriteByte(CByte(Math.Truncate(mycon.TargetTile / 256))) 'highbyte
                        fstream.WriteByte(CByte(mycon.TargetTile - (CInt(Math.Truncate(mycon.TargetTile / 256)) * 256))) 'lowbyte
                        fstream.WriteByte(mycon.TargetNode)
                    Loop
                Else
                    fstream.WriteByte(CByte(False))
                End If
            Next
        Else
            fstream.WriteByte(CByte(False))
        End If

        fstream.Flush()
        fstream.Close()

    End Sub

    Public Sub MarkNodeID(ByVal tileX As Integer, ByVal tileY As Integer, ByVal id As Byte)
        'marks tiles with unique traits such as doors and protection zones
        Dim tOffset As Integer
        'gets the file position of the tile
        tOffset = tileY + (tileX * 256)

        'if the nodemap isnt there initialize it
        If NodeMap Is Nothing Then ReDim NodeMap(65535)
        'brand the tile
        NodeMap(tOffset) = id


    End Sub
    Public Sub GetBaseXYZ(ByVal mapPath As String)
        'Loads the base file coordinates
        BaseX = CByte(Left(mapPath, mapPath.Count - 9))
        BaseY = CByte(Right(Left(mapPath, mapPath.Count - 6), 3))
        BaseZ = CByte(Right(Left(mapPath, mapPath.Count - 4), 2))
    End Sub
    Public Sub MarkSpecialTile(ByVal tx As Integer, ByVal ty As Integer, ByVal type As Byte)
        'marks tiles with unique traits such as doors and protection zones
        Dim tOffset As Integer
        'gets the file position of the tile
        tOffset = ty + (tx * 256)

        'if this special tile list isnt there initialize the list
        If SpecialMap Is Nothing Then ReDim SpecialMap(65535)
        'add the tile to the list
        '1 is a private door 3 is a safezone
        '1+3=4(both present),1+1=2(ignored),3+3=6(ignored)
        If SpecialMap(tOffset) + type = 4 Then
            SpecialMap(tOffset) = 2
        Else
            SpecialMap(tOffset) = type
        End If

    End Sub
    Private Function GetTileType(ByVal tX As Integer, ByVal tY As Integer) As Integer
        'returns the value of the special tile if a special map is loaded
        'otherwise it returns 0
        If SpecialMap Is Nothing Then ' if the map is empty
            Return 0
        Else 'if the map is not empty
            Return SpecialMap(tY + (tX * 256))
        End If
    End Function
End Class
