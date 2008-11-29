TibiaAPI Readme

Quick User Guide:

1) Create a blank project, name it and save it.

2) Right click the Bolded project name, select Add Existing Item. Select these three files: 
	-TibiaAPI.dll
	-TibiaAPI_Inject.dll
	-packet.dll

3) Then click the dll files in the Solution Explorer, and change the property Copy to Output to "Copy if Newer".

4) Right click the References folder and click Add Reference.

5) Choose Browse, and select TibiaAPI.dll from your Project folder. You don't need to do this for Packet.dll or TibiaAPI_Inject.dll (in fact you cannot, because it is not .NET) because TibiaAPI.dll provides the interface for Packet.dll and TibiaAPI_Inject.dll.

6) Now, in Form1.cs, at the top of the code, under the using System; et al., add:
        In C# Use
	using Tibia;
	using Tibia.Objects;

        In VB Use
        Imports Tibia
        Imports Tibia.Objects
And you are good to go! If you want some more help, ask, or look at the many examples using TibiaAPI!