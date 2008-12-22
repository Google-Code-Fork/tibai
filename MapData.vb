Imports Tibia.Memory
Imports System.Threading
Imports System.IO
'This class manages threads for the mapdata class
Public Class MapDataController
    Private NextThread As Integer = -1
    Private isAvailable As Boolean = True
    Private myMapData As MapData

    Public Sub New(ByVal Hndl As System.IntPtr, ByRef SearchAlg As PathFinder)
        WaitforControl()
        myMapData = New MapData(Hndl, SearchAlg)
        ReleaseControl()
    End Sub
    Public Function GetTileCost(ByVal Tx As Integer, ByVal Ty As Integer, ByVal Tz As Integer, ByVal color As Byte, ByVal blockSpecial As Boolean) As Byte
        WaitforControl()
        GetTileCost = myMapData.GetTileCost(Tx, Ty, Tz, color, blockSpecial)
        ReleaseControl()
    End Function
    Public Function GetTileID(ByVal Tx As Integer, ByVal Ty As Integer, ByVal Tz As Integer) As Byte
        WaitforControl()
        GetTileID = myMapData.GetTileID(Tx, Ty, Tz)
        ReleaseControl()
    End Function
    Public Sub AddTiletoList(ByVal Tx As Integer, ByVal Ty As Integer, ByVal Tz As Integer, ByVal type As Byte)
        WaitforControl()
        myMapData.AddTiletoList(Tx, Ty, Tz, type)
        ReleaseControl()
    End Sub

    Public Sub SetPZLock(ByVal Duration As Long)
        WaitforControl()
        myMapData.SetPZLock(Duration)
        ReleaseControl()
    End Sub
    Public Function GetMapFileAssessment() As Integer(,)
        WaitforControl()
        GetMapFileAssessment = myMapData.GetMapFileAssessment
        ReleaseControl()
    End Function
    'Control Subs- Ensures obect is acted upon by one thread at a time
    'a call to these two subs exist within each of the methods above

    Private Sub WaitforControl()
        'new threads pause here if another thread is using the object
        If NextThread = -1 Then NextThread = Thread.CurrentThread.ManagedThreadId
        Do While isAvailable = False Or Not Thread.CurrentThread.ManagedThreadId = NextThread
            'the first one in becomes the next one out
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

'Manages all data for movement related purposes
Public Class MapData
    'The Pathfinder to use when updating the macronode data files
    Private myPathfinder As PathFinder

    'Necessary client information
    Private Maps() As Integer = New Integer(9) {&H63F588, &H65F630, &H67F6D8, &H69F780, &H6BF828, &H6DF8D0, &H6FF978, &H71FA20, &H73FAC8, &H75FB70} '8.4 the map files loaded in tibia's memory
    Private MapOffset As Integer = &H14 '8.4 the location of a tile within a mapfile in tibia's memory
    Private Handle As System.IntPtr 'used to identify the client for reading memory

    'Arrays used to store information about the map
    Private mapsLoaded(9)() As Byte 'the map files in a byte array
    Private mapsDetail(9) As String 'the file name of the map in mapsloaded
    Private mapsPointer As Integer 'the position of the currently loaded map
    Private SmapsLoaded(9)() As Byte 'contains extra map tile details
    Private SmapsDetail(9) As String 'the file name of the map in smapsloaded
    Private SmapsPointer As Integer 'the position of the currently loaded special map

    'Objects that hold macro file data

    Private Structure MacroFileObj '256 bytes + Hubs
        Dim Map() As Byte
        Dim NameBase As String
        Dim MacroTiles()() As HubObj
    End Structure
    Private Structure HubObj '4 bytes + (number of child hubs * 6 bytes)
        Dim X As UInt16 'The x coordinate of the hub
        Dim Y As UInt16 'The y coordinate of the hub
        Dim ChildHubX() As UInt16 'The array of X coordinates for children hubs
        Dim ChildHubY() As UInt16 'The array of Y coordinates for children hubs
        Dim ChildHubDist() As UInt16 'The array of distances to children hubs from this hub
    End Structure

    'Objects used to read and write to various files
    Private binReader As System.IO.BinaryReader 'used to load a map file into tibiaAI's memory
    Private binWriter As System.IO.BinaryWriter 'used to write to special maps
    Private fStream As System.IO.FileStream 'used to load a map file into tibiaAI's memory

    'Stores directory information for use when accessing files
    Private MapsDir As New System.IO.DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\tibia\automap\") ' the location of the map files
    Private MapInfoDir As String = MapsDir.FullName & "MapInfo\"
    Private MapSFileDir As String = MapInfoDir & "SpecialTiles\"
    Private MacroSearchDir As String = MapInfoDir & "MacroSearch\"

    'Protection Zone Lock information
    Private isPZlocked As Boolean = False 'Default is false, if the player is PZLocked safety zones are ignored
    Private PZLockedSetTime As DateTime
    Private PZLockDuration As TimeSpan

    Public Sub New(ByVal Hndl As System.IntPtr, ByRef SearchAlg As PathFinder)
        Handle = Hndl
        myPathfinder = SearchAlg
        CreateMapDirectories()
        UpdateMacroNodes()
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
        sMapFilename = MapSFileDir & baseX & baseY & zStr & ".smap"
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

    'Data Related Methods
    Private Sub CreateMapDirectories()
        'Creates the directories that hold map data from TibA.I.
        If Not My.Computer.FileSystem.DirectoryExists(MacroSearchDir) Then My.Computer.FileSystem.CreateDirectory(MacroSearchDir)
        If Not My.Computer.FileSystem.DirectoryExists(MapSFileDir) Then My.Computer.FileSystem.CreateDirectory(MapSFileDir)
    End Sub

    'Special Tile Methods
    Private Sub saveSFile(ByVal fLoc As Integer)
        'this sub saves the special file created at fLoc so that new data is
        'recorded
        'opens or creates and opens the file
        fStream = New System.IO.FileStream(SmapsDetail(fLoc), IO.FileMode.Open, IO.FileAccess.ReadWrite)
        'writes to the filestream
        binWriter = New System.IO.BinaryWriter(fStream)
        binWriter.Write(SmapsLoaded(fLoc), 0, &H10000)
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
        sMapFilename = MapSFileDir & baseX & baseY & zStr & ".smap"
        'Changes the tile in tibia's memory to assist tibia's automap
        Dim inmem As Integer = InMemory(Tx, Ty, Tz)
        If inmem > -1 Then
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
                binWriter.Write(EmptyFile, 0, &H10000)
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

    'MacroSearch Related Methods
    Private Sub LoadMacroFile(ByVal X As Integer, ByVal Y As Integer, ByVal Z As Integer)
        'Loads the data in a .mdat file for use in tibia wide searches

    End Sub
    Private Sub SaveMacroFileObj(ByRef MFO As MacroFileObj)
        'Saves a .mdat files and .mmap files in the appropriate format
        Dim ByteArray() As Byte
        Dim NumberofBytes, ByteArrayPos As Integer
        'Compute the size of the file
        NumberofBytes = 256 '1 for each MacroTile 
        For Each MacroTile In MFO.MacroTiles
            For Each Hub In MacroTile
                NumberofBytes += 5 'adds 5 bytes for the hub
                If Not Hub.ChildHubX Is Nothing Then
                    NumberofBytes += UBound(Hub.ChildHubX) * 6 'adds 6 bytes per child
                End If
            Next
        Next
        'resizes the array
        ReDim ByteArray(NumberofBytes - 1)

        'Fills the information into the array
        For i As Integer = 0 To 255
            'The first 256 bytes are the number of hubs in each of
            'the 16x16 tiles of each mapfile
            ByteArray(i) = CByte(MFO.MacroTiles(i).Count)
        Next
        ByteArrayPos += 256 'increments the pointer
        For Each MacroTile In MFO.MacroTiles
            For i As Integer = 0 To UBound(MacroTile)
                'Adds the high byte of this hubs x position
                ByteArray(ByteArrayPos) = CByte(Math.Truncate(MacroTile(i).X / 256))
                ByteArrayPos += 1
                'Adds the low byte of this hubs x position
                ByteArray(ByteArrayPos) = CByte(MacroTile(i).X - (ByteArray(ByteArrayPos - 1) * 256))
                ByteArrayPos += 1
                'Adds the high byte of this hubs y position
                ByteArray(ByteArrayPos) = CByte(Math.Truncate(MacroTile(i).Y / 256))
                ByteArrayPos += 1
                'Adds the low byte of this hubs y position
                ByteArray(ByteArrayPos) = CByte(MacroTile(i).Y - (ByteArray(ByteArrayPos - 1) * 256))
                ByteArrayPos += 1

                'Adds a byte containing the number of children for this hub
                If Not MacroTile(i).ChildHubX Is Nothing Then
                    ByteArray(ByteArrayPos) = CByte(MacroTile(i).ChildHubX.Count)
                End If
                ByteArrayPos += 1
                If Not MacroTile(i).ChildHubX Is Nothing Then
                    For n As Integer = 0 To UBound(MacroTile(i).ChildHubX)
                        'Adds the high byte of this child hubs x position
                        ByteArray(ByteArrayPos) = CByte(Math.Truncate(MacroTile(i).ChildHubX(n) / 256))
                        ByteArrayPos += 1
                        'Adds the low byte of this child hubs x position
                        ByteArray(ByteArrayPos) = CByte(MacroTile(i).ChildHubX(n) - (ByteArray(ByteArrayPos - 1) * 256))
                        ByteArrayPos += 1
                        'Adds the high byte of this child hubs y position
                        ByteArray(ByteArrayPos) = CByte(Math.Truncate(MacroTile(i).ChildHubY(n) / 256))
                        ByteArrayPos += 1
                        'Adds the low byte of this child hubs y position
                        ByteArray(ByteArrayPos) = CByte(MacroTile(i).ChildHubY(n) - (ByteArray(ByteArrayPos - 1) * 256))
                        ByteArrayPos += 1
                        'Adds the high byte of this child hubs distance
                        ByteArray(ByteArrayPos) = CByte(Math.Truncate(MacroTile(i).ChildHubDist(n) / 256))
                        ByteArrayPos += 1
                        'Adds the low byte of this child hubs distance
                        ByteArray(ByteArrayPos) = CByte(MacroTile(i).ChildHubDist(n) - (ByteArray(ByteArrayPos - 1) * 256))
                        ByteArrayPos += 1
                    Next
                End If
            Next
        Next


        'Saves both arrays to their respective files
        Dim Stream As FileStream
        'saves the macro tile data file
        Stream = New FileStream(MacroSearchDir & MFO.NameBase & ".mdat", FileMode.Create, FileAccess.Write)
        Stream.Write(ByteArray, 0, ByteArray.Count)
        Stream.Flush()
        Stream.Close()
        'saves the macro map file
        Stream = New FileStream(MacroSearchDir & MFO.NameBase & ".mmap", FileMode.Create, FileAccess.Write)
        Stream.Write(MFO.Map, 0, MFO.Map.Count)
        Stream.Flush()
        Stream.Close()

    End Sub
    Private Sub UpdateMacroNodes()
        Exit Sub
        'Checks for missing or outdated macro search files and updates them
        Dim Path As String
        Dim MFO As MacroFileObj
        Dim MacroTileNumber, HubID As Integer
        Dim sMapX, sMapY, sMapZ As Integer
        Dim mdatInfo, mapInfo As FileInfo
        Dim zStr As String
        frmMStatus.Show()

        'Gets map size details from the pathfinder object
        Dim MapX As Integer = myPathfinder.MapX
        Dim MapY As Integer = myPathfinder.MapY
        Dim MinX As Integer = myPathfinder.MinX
        Dim MinY As Integer = myPathfinder.MinY

        'Gets a count of how many files need to be updated
        Dim UpdateCount As Integer
        For MacrX As Integer = CInt(MinX / 256) To CInt((MinX + MapX) / 256) - 1
            For MacrY As Integer = CInt(MinY / 256) To CInt((MinY + MapY) / 256) - 1
                For MacrZ As Integer = 0 To 15
                    If MacrZ < 10 Then zStr = "0" & CStr(MacrZ) Else zStr = CStr(MacrZ)
                    MFO.NameBase = CStr(MacrX) & CStr(MacrY) & zStr
                    'gets the map data
                    Path = MapsDir.FullName & "\" & CStr(MacrX) & CStr(MacrY) & zStr & ".map"
                    sMapX = MacrX * 256
                    sMapY = MacrY * 256
                    sMapZ = MacrZ
                    'if the mdat file is missing or out of date then
                    mapInfo = New FileInfo(MapsDir.FullName & MacrX & MacrY & zStr & ".map")
                    mdatInfo = New FileInfo(MacroSearchDir & MacrX & MacrY & zStr & ".mdat")
                    If Not mdatInfo.Exists Or mapInfo.LastWriteTime > mdatInfo.LastWriteTime Then
                        UpdateCount += 1
                    End If
                Next
            Next
        Next



        'for each possible macro search file
        Dim sTime As DateTime = Now
        Dim tSpan As TimeSpan
        Dim timeLeft As Integer
        frmMStatus.Show()
        For MacrX As Integer = CInt(MinX / 256) To CInt((MinX + MapX) / 256) - 1
            For MacrY As Integer = CInt(MinY / 256) To CInt((MinY + MapY) / 256) - 1
                For MacrZ As Integer = 0 To 15
                    'Updates frmMStatus
                    With frmMStatus
                        .lblTask.Text = "Building macro search files. Please Wait."
                        .pbarMStatus.Minimum = 0
                        .pbarMStatus.Maximum = UpdateCount
                        Dim a As Integer = .pbarMStatus.Value
                        tSpan = Now - sTime
                        If .pbarMStatus.Value = 0 Then
                            .lblTimeLeft.Text = "Time Left: Calculating"
                        Else
                            timeLeft = CInt((tSpan.TotalMinutes / .pbarMStatus.Value) * (UpdateCount - .pbarMStatus.Value))
                            If timeLeft > 0 Then
                                .lblTimeLeft.Text = "Time Left: " & CStr(timeLeft) & " minutes"
                            Else
                                .lblTimeLeft.Text = "Time Left: Less than 1 minute"
                            End If
                        End If
                        .Update()
                    End With
                    'converts the z coordinate to a string
                    If MacrZ < 10 Then zStr = "0" & CStr(MacrZ) Else zStr = CStr(MacrZ)
                    MFO.NameBase = CStr(MacrX) & CStr(MacrY) & zStr
                    'gets the map data
                    Path = MapsDir.FullName & "\" & CStr(MacrX) & CStr(MacrY) & zStr & ".map"
                    sMapX = MacrX * 256
                    sMapY = MacrY * 256
                    sMapZ = MacrZ

                    'if the mdat file is missing or out of date then
                    mapInfo = New FileInfo(MapsDir.FullName & MacrX & MacrY & zStr & ".map")
                    mdatInfo = New FileInfo(MacroSearchDir & MacrX & MacrY & zStr & ".mdat")
                    If Not mdatInfo.Exists Or mapInfo.LastWriteTime > mdatInfo.LastWriteTime Then

                        'clears the current MFO data
                        ReDim MFO.MacroTiles(255) '256 16x16 Tiles
                        ReDim MFO.Map(65535) 'The tibia style map that records which tiles are assigned to which hubs

                        For tile As Integer = 0 To UBound(MFO.Map)



                            If MFO.Map(tile) = 0 Then 'its unassigned
                                'Gets the MacroTile number(0-255 proceeding north to south then west to east)
                                MacroTileNumber = CInt(Math.Truncate(Math.Truncate(tile / 256) / 16) * 16)
                                MacroTileNumber += CInt(Math.Truncate((tile - (Math.Truncate(tile / 256) * 256)) / 16))

                                'Gets the hubID for the marknodes sub
                                If MFO.MacroTiles(MacroTileNumber) Is Nothing Then
                                    'if there are no current hubs then the hub id is 1
                                    HubID = 1
                                Else
                                    'otherwise the hubid is one more than the number of current hubs
                                    HubID = MFO.MacroTiles(MacroTileNumber).Count + 1
                                End If
                                'Mark all the tiles

                                If myPathfinder.MarkNodes(tile, MFO.Map, CByte(HubID), MacroTileNumber, sMapX, sMapY, sMapZ, Me) Then
                                    'Dimension the hub
                                    If MFO.MacroTiles(MacroTileNumber) Is Nothing Then
                                        ReDim MFO.MacroTiles(MacroTileNumber)(0)
                                    Else
                                        ReDim Preserve MFO.MacroTiles(MacroTileNumber)(UBound(MFO.MacroTiles(MacroTileNumber)) + 1)
                                    End If
                                    'Record the hubs coordinates
                                    MFO.MacroTiles(MacroTileNumber)(UBound(MFO.MacroTiles(MacroTileNumber))).X = CUShort(Math.Truncate(tile / 256) + sMapX)
                                    MFO.MacroTiles(MacroTileNumber)(UBound(MFO.MacroTiles(MacroTileNumber))).Y = CUShort(tile - ((Math.Truncate(tile / 256)) * 256) + sMapY)
                                End If
                            End If
                        Next
                        'save the macrofileobject
                        SaveMacroFileObj(MFO)
                        'increments the progress bar
                        frmMStatus.pbarMStatus.Increment(1)
                    End If
                Next
            Next
        Next
        'find children

        'Private Structure MacroFileObj
        '    Dim Map() As Byte
        '    Dim MacroTiles()() As HubObj
        'End Structure
        'Private Structure HubObj '4 bytes + (number of child hubs * 6 bytes)
        '    Dim X As UInt16 'The x coordinate of the hub
        '    Dim Y As UInt16 'The y coordinate of the hub
        '    Dim ChildHubX() As UInt16 'The array of X coordinates for children hubs
        '    Dim ChildHubY() As UInt16 'The array of Y coordinates for children hubs
        '    Dim ChildHubDist() As UInt16 'The array of distances to children hubs from this hub
        'End Structure
        frmMStatus.Hide()
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
