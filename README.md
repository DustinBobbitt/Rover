# CID & BPP Tool Renamer

Desktop WinForms utility for scanning `.cid` and `.bpp` files, mapping tool names, and applying replacements in bulk.

## Current Purpose

This project helps migrate CID/CAM and BPP files between machines that use different tool naming conventions.

It targets entries in these formats:

### CID Files
- `TOOLNAME=9/16END`
- `ToolName = T05`
- `Tool = T05`
- Case-insensitive on `ToolName` / `Tool`
- The value after `=` is parsed as the tool token.

### BPP Files (Index-Based)
- Processes lines starting with `@ BG`, `@ ROUTG`, or `' ROUTG`.
- Uses fixed field indices for safe replacement:
  - **BG**: Tool is at index **35** (after `:`)
  - **ROUTG**: Tool is at index **49** (after `:`)
- Replacements preserve CSV structure and quotes.

## Tech Stack

- C#
- .NET 8 Windows (`net8.0-windows`)
- WinForms
- No external NuGet dependencies

## Implemented Features

- Browse/select a root folder
- Optional recursive scan (`Include subfolders`)
- Scan `.cid` and `.bpp` files to collect unique tool names
- Editable Old Name -> New Name mapping grid
- Save mappings to JSON
- Load mappings from JSON (merge existing + new keys)
- Apply mappings across all discovered files
- Optional `.bak` backup creation
- Optional dry run mode (count changes without writing files)
- Progress bar and status text during processing
- Basic error aggregation (read/write/copy errors shown at completion)

## Parser/Replacement Details

### CID Parser
- Regex: `@"(Tool(?:Name)?\s*=\s*)([^\s;,\r\n]+)"`
- Group 1 preserves prefix, Group 2 is the tool value.

### BPP Parser
- Splits fields using CSV-style parsing (respecting quotes).
- Targets specific indices per operation type (`BG`: 35, `ROUTG`: 49).
- Safe for production: only modifies designated fields in specific operation types.

## JSON Mapping Format

Example:

```json
{
  "9/16END": "T916",
  "5MM": "T05",
  "3MM": "T03"
}
```

- Key = original tool token as found in file
- Value = replacement token

## Build and Run

From project folder:

```powershell
dotnet build
dotnet run
```

Publish single-file self-contained EXE:

```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
```

Expected output path:

- `bin\Release\net8.0-windows\win-x64\publish\CidToolRenamer.exe`

## Process History / Handoff Notes

### Prior Session (from exported log)
1. Started from spec for a CID tool renamer with WinForms UI.
2. Implemented base app with scanning, mapping grid, JSON load/save, apply, backup, dry-run, and status/progress.
3. Updated parser to target `TOOLNAME=` entries.

### This Session
1. Expanded support to `.bpp` files using index-based strategy (BG=35, ROUTG=49).
2. Updated CID parser to support both `ToolName=` and `Tool=` keywords.
3. Updated UI title and confirmation messages to reflect dual support.
4. Verified build and updated documentation.

## Known Gaps / Improvement Opportunities
- Error reporting is modal-only; could be improved with exportable log file.
- Processing is synchronous with `Application.DoEvents()`; could move to async/background worker for large batches.

## Suggested Next Agent Tasks
1. Add small test corpus and automated parser tests.
2. Add release notes section and versioning strategy.

