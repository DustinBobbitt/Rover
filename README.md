# Rover

Windows desktop utility for **bulk tool remapping** in **Biesse-style `.cid` / `.bpp`** files and common **G-code** outputs. Scan a folder, edit an old-to-new mapping table, then apply (optionally with **dry run** and **`.bak` backups**).

## Who it is for

- Shops moving programs between setups or posts where **tool names or T/H/D numbers** need to line up with a new convention.
- Anyone who prefers a **small offline tool** over hand-editing hundreds of CAM or NC files.

## Supported file types

| Area | Extensions | Behavior summary |
|------|------------|------------------|
| CID | `.cid` | `TOOLNAME=` / `ToolName=` / `Tool=` (case-insensitive); value token is remapped. |
| BPP | `.bpp` | Lines with `@ BG`, `@ ROUTG`, or `' ROUTG`; tool field at fixed indices (BG: 35, ROUTG: 49 after `:`). |
| G-code | `.tap`, `.iso`, `.nc`, `.cnc`, `.mpf`, `.spf`, `.ngc`, `.gcode` | `T` / `H` / `D` when the **numeric token** matches a mapping key (including `T=` / `H=` / `D=`, compact forms, and fused `G43H` / `G41D` / `G42D`). |

## Safe workflow (use these)

- **Dry run** - Reports what would change without writing files.
- **Create .bak backups** - Copies each file to `filename.ext.bak` before overwrite (skipped in dry run).
- **Confirm apply** - Final yes/no before processing; extra **machine safety** text appears when G-code is enabled.
- **JSON mapping** - Save/load mappings to repeat the same rename set.

## Machine safety (G-code)

Rover only edits text. If **`H`** (length offset / `G43`) or **`D`** (cutter comp) no longer matches the **control offset page**, the program can run with **wrong Z** or comp. That can cause **tool breakage**, **damage to spoil boards or fixtures**, or **machine limits** - physical motion, not an app crash.

Before running edited G-code on a machine: **backups**, **dry run**, verify in **CAM or simulation**, check **offsets on the control**, and **prove out** before production.

## Text encoding

Files are read and written with **UTF-8** (no BOM). That is appropriate for **ASCII / UTF-8** NC and typical CAM exports. If a legacy file uses a **non-UTF-8 code page** (some older Windows-only pipelines), characters outside ASCII may **round-trip incorrectly** after a save. In doubt, work on a **copy**, use **backups**, and confirm in your CAM or editor.

## Build and run

Prerequisites: **.NET 8 SDK**, **Windows** (WinForms).

From the project directory:

```powershell
dotnet build
dotnet run
```

Release build:

```powershell
dotnet build -c Release
```

## Publish (optional single-file EXE)

```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
```

Output (typical):

- `bin\Release\net8.0-windows\win-x64\publish\Rover.exe`

## Assets

- `Resources\app.ico` - application icon (referenced by the project file).
- `Resources\Rover-icon.png` - source graphic if you need to regenerate the `.ico`.

## Limitations

- Errors are shown in a **modal dialog** only; there is no exportable log file.
- Processing uses the **UI thread** with `Application.DoEvents()` for progress; very large trees may feel sluggish.
- G-code handling is **regex-based**, not a full ISO parser; unusual dialects or text inside comments can still surprise you - use dry run and verify output.

## Repo hygiene (if you use a parent workspace)

If your disk layout has a sibling folder such as `_SmokeHarnessTmp` **outside** this project, **do not add it to the public repo**. This repo's `.gitignore` ignores a local `SmokeHarness\` folder **inside** the project if you create one.
