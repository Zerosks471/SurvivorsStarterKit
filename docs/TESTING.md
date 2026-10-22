# Testing a Build

## One-time setup

| Step | What to do |
|---|---|
| 1 | Install the .NET SDK that matches your Godot: .NET 9 for Godot 4.7 (`winget install Microsoft.DotNet.SDK.9`), .NET 8 for Godot 4.6. Check with `dotnet --list-sdks`. |
| 2 | Open the project in Godot 4.7.2 .NET. Accept the upgrade from 4.6. |
| 3 | If Godot asks for an FBX2glTF path, use the download link in that dialog. Do not switch to the built-in FBX importer. |
| 4 | Press Build (hammer icon, top right). Wait for it to finish. |
| 5 | Editor > Manage Export Templates > Download and Install |

## Make the build

Pick one:

- Double-click `BUILD-WINDOWS.bat` in the project folder. Edit the `GODOT` path at the top first if Godot isn't in Downloads.
- Or in the editor: Project > Export > Windows Desktop > Export Project.

The game lands in `builds/windows/`. Run `EchoCrypt.console.exe` to see errors in a console window, or `EchoCrypt.exe` without one.

To share the build, zip the whole `builds/windows` folder. The `.exe` won't run without the files next to it.

## Test checklist

Fill in the Result column after each test.

| # | Test | How | Pass when | Result |
|---|---|---|---|---|
| 1 | Game starts | Run the exe | Player and floor show, enemies walk in | |
| 2 | Overlay | Press F3 | FPS and enemy count show at top left | |
| 3 | Shooting | Watch the player | Bullets fly at the nearest enemy and hit it | |
| 4 | Lifesteal | Let enemies get close | Enemies near you take damage, your HP refills | |
| 5 | Spirit Water | Wait 5 s | Water pools appear and damage enemies inside | |
| 6 | Floating spheres | Watch for 5 s | Spheres orbit, hit enemies, hide, come back | |
| 7 | Level-up | Kill enemies | Upgrade cards appear, picking one resumes play | |
| 8 | Boss | Press B | A large skeleton spawns | |
| 9 | Stress 200 | Press F4, wait for 200 enemies | FPS at or above 60 | |
| 10 | Stress 500 | Keep waiting to 500 | FPS at or above 60 | |
| 11 | Hitches | Play 5 min in stress mode | No freezes longer than a blink | |

## Common export errors

| Error | Fix |
|---|---|
| `.NET Sdk not found. The required version is '9.0.x'` | Install the .NET 9 SDK, then open a new terminal |
| `script 'res://Scripts/GameManager.cs' is not compiling` | Usually the missing SDK above. Otherwise press Build in the editor and read the red errors. |
| `No export template found` | Editor > Manage Export Templates > Download and Install |
| `Detected another project.godot at res://...` | A second copy of the project is inside this one. Move it out. |

## When something fails

Note the test number, what you saw, and the FPS from the overlay. If you ran the console exe, copy any red error lines.

Known gaps, not bugs yet: no death or game over, HP can hit 0 and play continues. Upgrade text is in French.
