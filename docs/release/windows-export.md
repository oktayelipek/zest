# Windows export

Zest ships its Windows export configuration in game/export_presets.cfg. Build
the current release from the repository root with:

    powershell -ExecutionPolicy Bypass -File tools/export-windows.ps1

The result is written to dist/Zest.exe together with its Godot runtime files.
The script resolves a Godot 4 .NET executable from -GodotPath, ZEST_GODOT,
or godot4/godot on PATH.

Before distributing a build, run:

    dotnet test Zest.sln --nologo
    dotnet build Zest.sln -c Release --nologo
    powershell -ExecutionPolicy Bypass -File tools/export-windows.ps1 -GodotPath 'C:\path\to\Godot.exe'

Open the exported executable and complete one live day, then save at the next
morning boundary and load it again. The export intentionally uses Godot's
standard unsigned Windows output; code signing and installer packaging remain
release-owner decisions.
