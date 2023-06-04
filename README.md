## About PSDX
**PSDX**, short for **PSX-SDX**, is a **PlayStation Save Data Editor** that allows you to read and edit the content of game save data files.
The main goal of the project is to build save data files that contain any game state (e.g. unlocked levels and defeated bosses, number of lives obtained, and so on) you would like to play.

If you have ever played *Crash Bandicoot 2*, the image below will give you an idea of what you can do with PSDX:

![Crash Bandicoot 2 save screen](https://github.com/giuse94/tests/assets/59248203/095f9ade-8eb1-46e9-b08a-124b4338e4c1)

### Current status
The project is in an early stage of development and has two main limitations:
1. It is implemented as a C# library without user interface, so a basic knowledge of the C# programming language is required to use the project.
2. Only one specific game is currently supported: the European version of *Crash Bandicoot 2* (SCES-00967).

A user interface and support for more games are planned, but they will not come anytime soon.
If you would like to contribute to the project, any help is welcome.

## Quick start guide
### Get the source code
No DLL for the library is currently provided, so it is necessary to build the source code from scratch.
To do this, you first need to [download this repository](https://github.com/giuse94/PSDX/archive/refs/heads/main.zip) or clone it with Git: `git clone https://github.com/giuse94/PSDX.git`.
If you opted for the download, unzip the folder (extract its content) in a folder of your choice.

### Get the .NET 7 SDK
Since this is a .NET 7 library, you need to download and install the [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).

### Create a C# project
You can create any C# project type, but a console application is the easiest way to test things out.
Once you have created your project, reference PSDX by adding the following lines in your `.csproj` file (replace "path\to\PSDX.csproj" with the actual path of the file):
```
<ItemGroup>
    <ProjectReference Include="path\to\PSDX.csproj" />
</ItemGroup>
```

You could also build the library and add its DLL to your project, but it would be pointless since you have access to the source code.

### Get a game save data file
Now you need a *Crash Bandicoot 2* save data file in the Single Save Data (.mcs) format.
The easiest way to obtain such a file is to use an emulator, like [DuckStation](https://github.com/stenzek/duckstation/).
This file will be referred to as "cb2.mcs" in the next section.

### Use PSDX to edit the file
Now you are ready to edit the save data file using PSDX.
The snippet below shows the typical usage of the library:
```c#
using PSDX;

// Open the save data file and pass it to PSDX.
using var fs = new FileStream("cb2.mcs", FileMode.Open);
var cb2 = new CrashBandicoot2SaveData(fs);
// Edit the file.
cb2.SetLives(100);
cb2.SetAkuAkuMasks(2);
// Save the changes to another file.
using var fs2 = new FileStream("cb2edited.mcs", FileMode.Create);
cb2.GetStream().CopyTo(fs2);
```

Beware that most functions throw exceptions if their arguments are wrong, so if you don't trust the input,  use `try-catch` blocks wisely.

Run the program to create your custom game save data file.

### Have fun
Load the file created in the previous section in the emulator and play the game the way you like.

## Acknowledgments
This project would not have been possible without the help of:
- [DuckStation](https://github.com/stenzek/duckstation/), the emulator that I used to test the game.
- The [tonyhax repository](https://github.com/socram8888/tonyhax) where I found the checksum algorithm for *Crash Bandicoot 2*.
- This [wiki](https://www.psdevwiki.com/ps3/PS1_Savedata) that contains information about the format of the PlayStation memory card.
