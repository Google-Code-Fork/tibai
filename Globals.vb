Imports Tibia
Imports Tibia.Objects
Imports Tibia.Packets
Imports Tibia.Util
Module Globals
    'client related
    Public Maps(9) As Integer 'the map file addresses in tibia's memory
    Public Handle As System.IntPtr 'used to identify the client for reading memory
    'packet related
    Public dataOffset As Integer = 8 'apparently there are 8 leading 0's in the packet
    'tibia memory related
    Public ReadOnly FirstMapAddy As Integer = &H6467B0 '8.5 address for the first map
    Public ReadOnly MapOffset As Integer = &H14 '8.5 the offset for the first tile in the map file
    Public ReadOnly MapFileOffset As Integer = &HA8 '8.5 the offset between mapfiles
    'map specifications
    Public ReadOnly MapX As Integer = 1792 'The true X dimension of the tibia map
    Public ReadOnly MapY As Integer = 2048 'the true Y dimension of the tibia map
    Public ReadOnly MinX As Integer = 31744 'the lowest X value possible in tibia
    Public ReadOnly MinY As Integer = 30976 'the lowest Y value possible in tibia
    'Stores directory information for use when accessing files
    Public ReadOnly MapsDir As New System.IO.DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\tibia\automap\") ' the location of the map files
    Public ReadOnly MapInfoDir As String = MapsDir.FullName & "MapInfo\"
    'The macrotile queues
    Public Mappable As MiningQueue
    'The data mining thread
    Public DataMiner As System.Threading.Thread
    'Flags
    Public DebugMap As Boolean = False 'enables drawing on the minimap
    Public DebugColor As Byte = &HD2
    'Special Tile Types
    Public PrivateDoor As Byte = 1
    Public SafeZone As Byte = 2

    'major objects
    Public myClient As Client
    Public myPlayer As Player
    Public myEvents As EventHandler
    Public myCartographer As Cartographer
    Public myMovement As Movement
    Public myController As Controller
End Module
