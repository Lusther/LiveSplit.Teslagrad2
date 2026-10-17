# How game builds and offsets work

This page explains, from scratch, how the component finds the values it reads in the game's memory, why that breaks on game updates, and how to support a new build. For the short checklist, see [CONTRIBUTING.md](../CONTRIBUTING.md#supporting-a-new-game-build).

## 1. The big picture

A LiveSplit autosplitter does not talk to the game. It reads the game's memory from the outside (the same way Cheat Engine does) and watches values such as "is Blink unlocked?". When a value goes from `false` to `true`, it splits.

To read a value you need its **address** in memory. Every time the developers recompile the game, the compiler may move things around, so some addresses change. Everything below is about finding the new ones reliably.

## 2. Steam vocabulary: app, depot, build, manifest, branch

| Term | What it is | Teslagrad 2 |
|---|---|---|
| **App** | The product on the Steam store | `1698220` |
| **Depot** | A set of files inside the app (games often have one per OS, language or DLC) | `1698221` = the Windows game files |
| **Build** | One upload by the developers; every update gets a new build id | `17572578`, `20406723`, `23225880`, ... |
| **Manifest** | The exact file list of one depot in one build (every file, its size and hash) | e.g. `7358339606444643465` = depot 1698221 in build 20406723 |
| **Branch** (beta) | A named pointer to a build that players pick in *Properties → Betas* | `public`, `old_version_for_speedrunning` |

An old build that no branch points to anymore still exists on Steam's servers through its **manifest**. With the manifest id, the exact files of that build can be downloaded again.

[SteamDB](https://steamdb.info) keeps the history of every manifest. The page `https://steamdb.info/patchnotes/<build id>/` lists, for depot 1698221, a change like `2038588255437308694 › 7358339606444643465`: the second id is the manifest introduced by that build.

[DepotDownloader](https://github.com/SteamRE/DepotDownloader) is an open-source command-line Steam client that downloads a depot from a branch (`-branch`) or from a specific manifest (`-manifest`). With `-filelist` it only fetches the listed files (about 50 MB instead of the whole game). It needs a Steam account that owns the game; `-remember-password` stores a login token so later runs don't ask again.

```powershell
# filelist.txt
GameAssembly.dll
regex:.*global-metadata\.dat
```

```powershell
# Current build of a branch
DepotDownloader.exe -app 1698220 -depot 1698221 -branch old_version_for_speedrunning -filelist filelist.txt -dir .\build -username <steam account name> -remember-password
# Any past build, by manifest id
DepotDownloader.exe -app 1698220 -depot 1698221 -manifest 7358339606444643465 -filelist filelist.txt -dir .\build -username <steam account name> -remember-password
```

The Steam account name is the login name, not the display name (it is listed as `AccountName` in `Steam\config\loginusers.vdf`).

## 3. How a Unity IL2CPP game is built

Teslagrad 2 is written in **C#** with Unity. Unity can ship C# in two ways: **Mono** (C# stays as bytecode run by a virtual machine) or **IL2CPP**, which Teslagrad 2 uses: the C# is converted to C++ and compiled into native machine code. The result is two files:

- **`GameAssembly.dll`**: all the game code, as machine code. Class and field names are mostly gone.
- **`Teslagrad 2_Data\il2cpp_data\Metadata\global-metadata.dat`**: the names. Every class, its fields, their types, and the offset of each field inside an object.

Neither file is readable alone: the DLL has the code without names, the metadata has names without saying where things are in the DLL.

## 4. Reading a pointer path

A line from `Teslagrad2Memory.cs`:

```csharp
new DeepPointer("GameAssembly.dll", save, 0xB8, 0x10, 0x44)   // save = 0x2775B40 on build 23225880
```

Read it as a chain of "go there, read the address stored there, add the next number":

```
GameAssembly.dll start address (decided by Windows when the game starts)
  + 0x2775B40  → "Game_TypeInfo": a slot in the DLL holding a pointer to the
                  description of the C# class `Game` (an Il2CppClass)
  read         → address of the Il2CppClass of `Game`
  + 0xB8       → in every Il2CppClass, offset 0xB8 = pointer to the class's static fields
  read         → address of Game's static fields
  + 0x10       → static field `activeSlot` (the current save, a SaveDataSlot object)
  read         → address of the SaveDataSlot object
  + 0x44       → field `blinkUnlocked`, a bool  ← the value we want
```

The numbers don't all change for the same reasons:

| Number | Meaning | Changes when |
|---|---|---|
| `0x2775B40` | Position of `Game_TypeInfo` inside the DLL | **Almost every build**: any added code shifts it |
| `0xB8` | Layout of IL2CPP's own class structure | Only on a Unity version upgrade |
| `0x10`, `0x44` | Position of fields in the game's C# classes | Only if the developers add or remove fields in those classes |

That is why each build in `Teslagrad2Constants.Builds` stores four base addresses, one per class the component starts from, while the rest of the pointer paths stays in `Teslagrad2Memory.cs`:

| Constructor argument | IL2CPP class | Used for |
|---|---|---|
| `saveDataSlot` | `Game` (static `activeSlot`) | Skills, bosses, scrolls, time spent |
| `sceneHandler` | `PlayerSceneHandler` | Current scene, Elenor fight |
| `saveDataFile` | `SaveDataFile` | Number of save slots |
| `player` | `Player` (static `instance`) | Lumina's position |

### The player position: leaving C# for Unity's native memory

The position is not a C# field: `Player.Position` returns `instance.transform.position`, and `transform.position` is an *internal call* implemented in C++ inside `UnityPlayer.dll`. The path therefore crosses into Unity's native objects, whose layout was read from `UnityPlayer.dll` 2021.3.45f2 with Ghidra:

```
GameAssembly.dll + Player_TypeInfo
  + 0xB8 → static fields, + 0x0 → Player.instance          (C# object)
  + 0x10 → m_CachedPtr: the native C++ object behind any UnityEngine.Object
  + 0x30 → its GameObject
  + 0x30 → the GameObject's component array, + 0x8 → component 0, always the Transform
Transform
  + 0x38 → TransformHierarchy,  + 0x40 → index of this transform in it
TransformHierarchy
  + 0x18 → array of { position, rotation, scale }, 0x30 bytes per transform
  + 0x20 → array of parent indices, -1 for a root
```

The array holds *local* positions: like `Transform::GetPosition`, `Teslagrad2Memory.ReadPlayerPosition` applies the scale, rotation and position of every ancestor to get the world position (Lumina sits under a neutral parent, but the game can move her under another one).

These native offsets do not depend on the game build, only on the Unity version: they need checking again only if `UnityPlayer.dll` changes version (file properties, "Product version").

Example: from build 20406723 to 23225880, the four base addresses all moved by exactly +8 bytes. These `_TypeInfo` slots are stored next to each other, so most likely one 8-byte pointer was added before them, shifting everything that follows.

## 5. What Il2CppDumper does

[Il2CppDumper](https://github.com/Perfare/Il2CppDumper) reads `GameAssembly.dll` and `global-metadata.dat` together and matches them, putting the names back on the code.

```powershell
Il2CppDumper.exe "<game>\GameAssembly.dll" "<game>\Teslagrad 2_Data\il2cpp_data\Metadata\global-metadata.dat" .\dump
```

Two of its output files matter here:

- **`script.json`**: the address of every named item in the DLL. The base addresses come from entries like:
  ```json
  { "Address": 41376576, "Name": "Game_TypeInfo", "Signature": "Game_c*" }
  ```
  `41376576` is `0x2775B40`. Addresses are relative to the start of the DLL (an *RVA*), which is why the code writes `"GameAssembly.dll", 0x2775B40`: "DLL start + this".
- **`dump.cs`**: the game's classes rebuilt as C#-like text, with the offset of every field:
  ```csharp
  public static class Game {
      private static SaveDataSlot activeSlot; // 0x10
  ...
  public class SaveDataSlot {
      private bool <blinkUnlocked>k__BackingField; // 0x44
  ```
  This is where the offsets in the pointer paths (`0x10`, `0x44`, ...) come from.

## 6. Identifying a build: the PE timestamp

Every Windows `.exe`/`.dll` starts with a **PE header**, a small fixed structure Windows reads to load the file:

```
0x00           "MZ"                     every exe/dll starts with this
0x3C           e_lfanew                 where the PE header starts
e_lfanew+0x00  "PE\0\0"                 signature
e_lfanew+0x04  Machine, NumberOfSections
e_lfanew+0x08  TimeDateStamp (4 bytes)  when the linker produced this file
...
e_lfanew+0x50  SizeOfImage              size of the DLL once loaded (LiveSplit's ModuleMemorySize)
```

`TimeDateStamp` is a Unix time: `0x6A0376AF` = 2026-05-12 18:51:27 UTC, when build 23225880 was compiled. It is different for every compilation, unlike `SizeOfImage`: public builds 20406723 and 23225880 both have a size of 48156672, which is why the component used to read the wrong addresses on 23225880.

Windows copies the header into memory when it loads the DLL, so the component reads it from the game process (`Teslagrad2Memory.ReadModuleTimestamp`):

```csharp
int peHeaderOffset = Game.ReadValue<int>(module.BaseAddress + 0x3C);
return Game.ReadValue<uint>(module.BaseAddress + peHeaderOffset + 0x8);
```

It then looks the timestamp up in `Teslagrad2Constants.Builds`. A match gives the build id, its branch and its four base addresses; no match means an unknown build, and the component refuses to hook (and logs it) instead of reading wrong values. The branch and build id are shown in the `Teslagrad2_Version` LiveSplit variable (e.g. `public (build 23225880)`), and the build id alone in `Teslagrad2_Build`.

## 7. What `tools/Update-Offsets.ps1` does

It turns the files of a build into one line of `Teslagrad2Constants.cs`:

1. **Reads `script.json`** and finds `Game_TypeInfo`, `PlayerSceneHandler_TypeInfo`, `SaveDataFile_TypeInfo` and `Player_TypeInfo`: the four base addresses.
2. **Reads the PE header of `GameAssembly.dll`** on disk: timestamp and size.
3. **Reads Steam's app manifest** (`steamapps\appmanifest_1698220.acf`, where Steam records what is installed): build id and branch. For files downloaded with DepotDownloader, pass `-Build` and `-Branch` instead.
4. **Checks `dump.cs`** for every field offset `Teslagrad2Memory.cs` relies on (`activeSlot = 0x10`, `blinkUnlocked = 0x44`, `scrollsPickedUp = 0x80`, ...). If one moved or disappeared it reports it and exits with code 1: `Teslagrad2Memory.cs` then needs a manual fix too.
5. **Writes the entry**, replacing the line of that build id if it exists, or adding it at the top:
   ```csharp
   new GameBuild(23225880, "public", 0x6A0376AF, 48156672, 0x2775B40, 0x273C238, 0x274A470, 0x273B718),
   ```
   Without `-Apply` it only prints what it would change.

```powershell
.\tools\Update-Offsets.ps1 -DumpDir .\dump -GameDir "<game>"            # preview
.\tools\Update-Offsets.ps1 -DumpDir .\dump -GameDir "<game>" -Apply     # write
.\tools\Update-Offsets.ps1 -DumpDir .\dump -GameDir .\build -Build 20406723 -Branch public -Apply   # downloaded build
```

## 8. The whole routine for a game update

```
Steam updates the game
   │
   ├─ 1. Get the files      installed by Steam, or DepotDownloader -branch / -manifest
   ├─ 2. Il2CppDumper       GameAssembly.dll + global-metadata.dat → script.json + dump.cs
   ├─ 3. Update-Offsets.ps1 adds the GameBuild entry, checks the field offsets
   ├─ 4. Build and test in LiveSplit
   └─ 5. Pull request and release
```

Previous builds stay in the list, so runners on older versions (such as the speedrun branch) keep working.
