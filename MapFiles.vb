Imports Tibia.Memory
Imports System.Threading
'This class manages threads for the mapfiles class
Public Class MapFilesController
    Private NextThread As Integer = -1
    Private isAvailable As Boolean = True
    Private myMapfiles As MapFiles

    Public Sub New(ByVal Hndl As System.IntPtr)
        WaitforControl()
        myMapfiles = New MapFiles(Hndl)
        ReleaseControl()
    End Sub
    Public Function GetTileCost(ByVal Tx As Integer, ByVal Ty As Integer, ByVal Tz As Integer, ByVal color As Byte, ByVal blockSpecial As Boolean) As Byte
        WaitforControl()
        GetTileCost = myMapfiles.GetTileCost(Tx, Ty, Tz, color, blockSpecial)
        ReleaseControl()
    End Function
    Public Function GetTileID(ByVal Tx As Integer, ByVal Ty As Integer, ByVal Tz As Integer) As Byte
        WaitforControl()
        GetTileID = myMapfiles.GetTileID(Tx, Ty, Tz)
        ReleaseControl()
    End Function
    Public Sub AddTiletoList(ByVal Tx As Integer, ByVal Ty As Integer, ByVal Tz As Integer, ByVal type As Byte)
        WaitforControl()
        myMapfiles.AddTiletoList(Tx, Ty, Tz, type)
        ReleaseControl()
    End Sub
    Public Sub SetPZLock(ByVal Duration As Long)
        WaitforControl()
        myMapfiles.SetPZLock(Duration)
        ReleaseControl()
    End Sub
    Public Function GetMapFileAssessment() As Integer(,)
        WaitforControl()
        GetMapFileAssessment = myMapfiles.GetMapFileAssessment
        ReleaseControl()
    End Function

    'Control Subs- Ensures obect is acted upon by one thread at a time
    'a call to these two subs exist within each of the methods above

    Private Sub WaitforControl()
        'new threads pause here if another thread is using the object
        If NextThread = -1 Then NextThread = Thread.CurrentThread.ManagedThreadId
        Do While isAvailable = False Or Not Thread.CurrentThread.ManagedThreadId = NextThread
            'the first one in takes becomes the next one out
            If NextThread = -1 Then NextThread = Thread.CurrentThread.ManagedThreadId
            Thread.Sleep(100)
        Loop
        'The next thread position opens up and the object becomes unavailable
        isAvailable = False
        NextThread = -1

    End Sub
    Private Sub ReleaseControl()
        'the active thread releases control when it's done
        isAvailable = True
    End Sub
End Class

'Controlled class
Public Class MapFiles
    Private Maps() As Integer = New Integer(9) {&H63A6C8, &H65A770, &H67A818, &H69A8C0, &H6BA968, &H6DAA10, &H6FAAB8, &H71AB60, &H73AC08, &H75ACB0} '8.31 the map files loaded in tibia's memory
    Private MapOffset As Integer = &H14 '8.31 the location of a tile within a mapfile
    Private Handle As System.IntPtr 'used to identify the client for reading memory
    Private mapsLoaded(9)() As Byte 'the map files in a byte array
    Private SmapsLoaded(9)() As Byte 'contains extra map tile details
    Private mapsDetail(9) As String 'the file name of the map in mapsloaded
    Private SmapsDetail(9) As String 'the file name of the map in smapsloaded
    Private mapsPointer As Integer 'the position of the currently loaded map
    Private SmapsPointer As Integer 'the position of the currently loaded special map
    Private binReader As System.IO.BinaryReader 'used to load a map file into tibiaAI's memory
    Private binWriter As System.IO.BinaryWriter 'used to write to special maps
    Private fStream As System.IO.FileStream 'used to load a map file into tibiaAI's memory
    Private MapsDir As New System.IO.DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\tibia\automap\") ' the location of the map files
    Private isPZlocked As Boolean = False 'Default is false, if the player is PZLocked safety zones are ignored
    Private PZLockedSetTime As DateTime
    Private PZLockDuration As TimeSpan

    Public Sub New(ByVal Hndl As System.IntPtr)
        Handle = Hndl

    End Sub
    Public Function GetTileCost(ByVal Tx As Integer, ByVal Ty As Integer, ByVal Tz As Integer, ByVal color As Byte, ByVal blockSpecial As Boolean) As Byte
        'Returns the movement cost of a given tile- default is same as unexplored
        'Tries to reset isPZLocked
        If isPZlocked Then
            If PZLockedSetTime.Ticks + PZLockDuration.Ticks < Now.Ticks Then isPZlocked = False
        End If
        GetTileCost = &HFA 'same as unexplored
        'get the file x and y and the offset within the file
        Dim baseX, baseY, tOffset, inMem, isload As Integer
        Dim zStr, MapFilename, sMapFilename As String
        baseX = GetTileBase(Tx) 'gets the mapfiles first 3 digits
        baseY = GetTileBase(Ty) 'gets the mapfiles second 3 digits
        'gets the file position of the tile
        tOffset = GetTileOffset(Ty) + (GetTileOffset(Tx) * 256)
        'creates a 2 char string from Tz used in finding the map file
        If Tz < 10 Then zStr = "0" & CStr(Tz) Else zStr = CStr(Tz)
        'creates the mapfile name
        MapFilename = MapsDir.FullName & baseX & baseY & zStr
        sMapFilename = MapFilename & ".smap"
        MapFilename = MapFilename & ".map"

        'determine if the file is stored in tibia's memory
        inMem = InMemory(Tx, Ty, Tz)
        If blockSpecial = True Then
            If isSpecial(sMapFilename, tOffset, inMem) = True Then
                Return &HFF
                Exit Function
            End If
        End If
        If inMem > -1 Then
            'Gets the movement cost from tibia's memory
            GetTileCost = ReadByte(Handle, Maps(inMem) + MapOffset + &H10000 + tOffset)
            'changes the color of the tile in the map (used to debug)
            'WriteByte(Handle, Maps(inMem) + MapOffset + tOffset, color)
        Else
            'if not in memory check the map file
            'if the map file exists
            If My.Computer.FileSystem.FileExists(MapFilename) Then
                'loads the map into memory for possibly repeated use
                ' *note: There are 10 slots for map files. The map pointer is
                ' *used to rotate between the 10 spots so that the oldest one
                ' *is replaced. This way if several paths are used in the same
                ' *area (which is highly likely) I don't have to keep loading
                ' *maps. (this feature could cause a problem if tibia updates
                ' *the maps with new data after they are loaded into memory)
                isload = isLoaded(MapFilename, False)
                If isload = -1 Then
                    'if not already loaded then load the map file into one of the load slots
                    fStream = New System.IO.FileStream(MapFilename, IO.FileMode.Open)
                    binReader = New System.IO.BinaryReader(fStream)
                    mapsLoaded(mapsPointer) = binReader.ReadBytes(CInt(binReader.BaseStream.Length))
                    mapsDetail(mapsPointer) = MapFilename
                    fStream.Flush()
                    fStream.Close()
                    binReader.Close()
                    'advance the pointer
                    If mapsPointer = 9 Then mapsPointer = 0 Else mapsPointer += 1
                End If
                isload = isLoaded(MapFilename, False)
                GetTileCost = mapsLoaded(isload)(tOffset + &H10000)
            End If
        End If
    End Function
    Public Function GetTileID(ByVal Tx As Integer, ByVal Ty As Integer, ByVal Tz As Integer) As Byte
        'Returns the movement cost of a given tile- default is same as unexplored
        GetTileID = 0 'same as unexplored
        'get the file x and y and the offset within the file
        Dim baseX, baseY, tOffset, inMem, isload As Integer
        Dim zStr, MapFilename As String
        baseX = GetTileBase(Tx) 'gets the mapfiles first 3 digits
        baseY = GetTileBase(Ty) 'gets the mapfiles second 3 digits
        'gets the file position of the tile
        tOffset = GetTileOffset(Ty) + (GetTileOffset(Tx) * 256)


        'determine if the file is stored in tibia's memory
        inMem = InMemory(Tx, Ty, Tz)
        If inMem > -1 Then
            'Gets the movement cost from tibia's memory
            GetTileID = ReadByte(Handle, Maps(inMem) + MapOffset + tOffset)
            'changes the color of the tile in the map (used to debug)
            'WriteByte(Handle, Maps(inMem) + MapOffset + tOffset, color)
        Else
            'if not in memory check the map file
            'creates a 2 char string from Tz used in finding the map file
            If Tz < 10 Then zStr = "0" & CStr(Tz) Else zStr = CStr(Tz)
            'creates the mapfile name
            MapFilename = MapsDir.FullName & baseX & baseY & zStr & ".map"
            'if the map file exists
            If My.Computer.FileSystem.FileExists(MapFilename) Then
                'loads the map into memory for possibly repeated use
                ' *note: There are 10 slots for map files. The map pointer is
                ' *used to rotate between the 10 spots so that the oldest one
                ' *is replaced. This way if several paths are used in the same
                ' *area (which is highly likely) I don't have to keep loading
                ' *maps. (this feature could cause a problem if tibia updates
                ' *the maps with new data after they are loaded into memory)
                isload = isLoaded(MapFilename, False)
                If isload = -1 Then
                    'if not already loaded then load the map file into one of the load slots
                    fStream = New System.IO.FileStream(MapFilename, IO.FileMode.Open)
                    binReader = New System.IO.BinaryReader(fStream)
                    mapsLoaded(mapsPointer) = binReader.ReadBytes(CInt(binReader.BaseStream.Length))
                    mapsDetail(mapsPointer) = MapFilename
                    fStream.Flush()
                    fStream.Close()
                    binReader.Close()
                    'advance the pointer
                    If mapsPointer = 9 Then mapsPointer = 0 Else mapsPointer += 1
                End If
                isload = isLoaded(MapFilename, False)
                GetTileID = mapsLoaded(isload)(tOffset + &H10000)
            End If
        End If
    End Function
    Private Function GetTileBase(ByVal TileXorY As Integer) As Integer
        'Returns the base of a coordinate for use in identifying the map file
        Return CInt(Math.Truncate(TileXorY / 256))
    End Function
    Private Function GetTileOffset(ByVal TileXorY As Integer) As Integer
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
    Private Function isLoaded(ByRef filepath As String, ByVal isSpecial As Boolean) As Integer
        ' = -1 if not loaded 0 - 9 if it is loaded
        isLoaded = -1
        If isSpecial Then 'checks for special maps
            For i As Integer = 0 To UBound(SmapsDetail)
                If filepath = SmapsDetail(i) Then
                    isLoaded = i
                    Exit For
                End If
            Next
        Else 'checks for regular maps
            For i As Integer = 0 To UBound(mapsDetail)
                If filepath = mapsDetail(i) Then
                    isLoaded = i
                    Exit For
                End If
            Next
        End If
    End Function
    Protected Overrides Sub Finalize()

        ' saves the files stored in memory
        For i As Integer = 0 To UBound(SmapsDetail)
            If SmapsDetail(i) <> "" Then
                saveSFile(i)
            End If
        Next

    End Sub
    Private Sub saveSFile(ByVal fLoc As Integer)
        'this sub saves the special file created at fLoc so that new data is
        'recorded
        'opens or creates and opens the file
        fStream = New System.IO.FileStream(SmapsDetail(fLoc), IO.FileMode.Open, IO.FileAccess.ReadWrite)
        'writes to the filestream
        binWriter = New System.IO.BinaryWriter(fStream)
        binWriter.Write(SmapsLoaded(fLoc), 0, &HFFFF)
        'closes the filestream
        fStream.Flush()
        fStream.Close()
        binWriter.Close()

    End Sub
    Private Function isSpecial(ByVal fName As String, ByVal fOffset As Integer, ByVal inMem As Integer) As Boolean
        'returns true if the tile was flagged as special

        Dim isload As Integer
        'checks to see if the file is loaded
        isload = isLoaded(fName, True)
        If isload = -1 Then 'its not loaded
            'if the file exists
            If My.Computer.FileSystem.FileExists(fName) Then
                'saves the previous smap
                If SmapsDetail(SmapsPointer) <> "" Then
                    saveSFile(SmapsPointer)
                End If

                'load the map file
                fStream = New System.IO.FileStream(fName, IO.FileMode.Open, IO.FileAccess.ReadWrite)
                binReader = New System.IO.BinaryReader(fStream)
                SmapsLoaded(SmapsPointer) = binReader.ReadBytes(CInt(binReader.BaseStream.Length))
                SmapsDetail(SmapsPointer) = fName
                'close the stream
                fStream.Flush()
                fStream.Close()
                binReader.Close()
                isload = SmapsPointer
                'advance the pointer
                If SmapsPointer = 9 Then SmapsPointer = 0 Else SmapsPointer += 1

            Else
                'return false
                Return False
            End If
        End If
        'read the value and return the correct response
        'if its a special tile then tibia's memory has to be updated so that 
        'tibia's pathfinder doesn't freak out in unexplored areas
        Dim a As Byte = SmapsLoaded(isload)(fOffset)
        If a < 3 And a > 0 Then
            'its a door
            If inMem > -1 Then
                'Writes a blocking value to tibia's memory
                Dim test As Integer = Maps(9)
                WriteByte(Handle, Maps(inMem) + MapOffset + fOffset + &H10000, &HFF)
            End If
            Return True
        ElseIf a > 1 And isPZlocked = True Then
            'its a Protection zone and the character was blocked recently
            If inMem > -1 Then
                'Writes a blocking value to tibia's memory
                WriteByte(Handle, Maps(inMem) + MapOffset + fOffset + &H10000, &HFF)
            End If
            Return True
        ElseIf a = 0 Then
            'is nothing
            Return False
        End If
    End Function
    Public Sub AddTiletoList(ByVal Tx As Integer, ByVal Ty As Integer, ByVal Tz As Integer, ByVal type As Byte)
        'Called when the eventhandler identifies a tile as being a 
        'non-qualified step, it adds the specific type of tile to the map

        'gets the special mapfile's name
        Dim baseX, baseY, tOffset, isload As Integer
        Dim zStr, sMapFilename As String
        Dim EmptyFile(&HFFFF) As Byte
        baseX = GetTileBase(Tx) 'gets the mapfiles first 3 digits
        baseY = GetTileBase(Ty) 'gets the mapfiles second 3 digits
        'gets the file position of the tile
        tOffset = GetTileOffset(Ty) + (GetTileOffset(Tx) * 256)
        If Tz < 10 Then zStr = "0" & CStr(Tz) Else zStr = CStr(Tz)
        'creates the mapfile name
        sMapFilename = MapsDir.FullName & baseX & baseY & zStr & ".smap"
        'Changes the tile in tibia's memory to assist tibia's automap
        Dim inmem As Integer = InMemory(Tx, Ty, Tz)
        If inMem > -1 Then
            'Writes a blocking value to tibia's memory
            Dim test As Integer = Maps(9)
            WriteByte(Handle, Maps(inmem) + MapOffset + tOffset + &H10000, &HFF)
        End If
        'checks to see if the file is loaded
        isload = isLoaded(sMapFilename, True)
        'if the file is not loaded then
        If isload = -1 Then
            'saves the previous smap
            If SmapsDetail(SmapsPointer) <> "" Then
                saveSFile(SmapsPointer)
            End If

            'if this file doesn't exist
            If Not My.Computer.FileSystem.FileExists(sMapFilename) Then
                'open a new file to write to
                fStream = New System.IO.FileStream(sMapFilename, IO.FileMode.OpenOrCreate, IO.FileAccess.ReadWrite, IO.FileShare.None)
                'fills the file with blank data
                binWriter = New System.IO.BinaryWriter(fStream)
                binWriter.Write(EmptyFile, 0, &HFFFF)
                'closes the stream
                fStream.Flush()
                fStream.Close()
                binWriter.Close()
            End If
            'load the file
            fStream = New System.IO.FileStream(sMapFilename, IO.FileMode.Open, IO.FileAccess.ReadWrite, IO.FileShare.None)
            binReader = New System.IO.BinaryReader(fStream)
            SmapsLoaded(SmapsPointer) = binReader.ReadBytes(CInt(binReader.BaseStream.Length))
            SmapsDetail(SmapsPointer) = sMapFilename
            'close the stream
            fStream.Flush()
            fStream.Close()
            binReader.Close()
            isload = SmapsPointer
            'advance the pointer
            If SmapsPointer = 9 Then SmapsPointer = 0 Else SmapsPointer += 1
        End If
        'add the tile to the list
        '1 is a private door 3 is a safezone
        '1+3=4(both present),1+1=2(ignored),3+3=6(ignored)
        If SmapsLoaded(isload)(tOffset) + type = 4 Then
            SmapsLoaded(isload)(tOffset) = 2
        Else
            SmapsLoaded(isload)(tOffset) = type
        End If

    End Sub
    Public Sub SetPZLock(ByVal Duration As Long)
        'turns on a pzlock for a set amount of time, this could be improved
        isPZlocked = True
        PZLockDuration = New TimeSpan(Duration * TimeSpan.TicksPerSecond)
        PZLockedSetTime = Now
    End Sub
    Public Function GetMapFileAssessment() As Integer(,)
        'loop through every mapfile and record tiles with identical color and movement cost

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
