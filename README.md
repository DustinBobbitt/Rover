# CID Tool Renamer

Desktop WinForms utility for scanning `.cid` files, mapping tool names, and applying replacements in bulk.

## Current Purpose

This project helps migrate CID/CAM files between machines that use different tool naming conventions.

It currently targets entries in this format:

- `TOOLNAME=9/16END`
- `ToolName = T05`
- case-insensitive on `ToolName`

The value after `=` is parsed as the tool token and can be remapped.

## Tech Stack

- C#
- .NET 6 Windows (`net6.0-windows`)
- WinForms
- No external NuGet dependencies

## Implemented Features

- Browse/select a root folder
- Optional recursive scan (`Include subfolders`)
- Scan `.cid` files and collect unique tool names from `TOOLNAME=...`
- Editable Old Name -> New Name mapping grid
- Save mappings to JSON
- Load mappings from JSON (merge existing + new keys)
- Apply mappings across all discovered `.cid` files
- Optional `.bak` backup creation
- Optional dry run mode (count changes without writing files)
- Progress bar and status text during processing
- Basic error aggregation (read/write/copy errors shown at completion)

## Parser/Replacement Details

Core regex in `MainForm.cs`:

- `@"(ToolName\s*=\s*)([^\s;,\r\n]+)"`

Behavior:

- Group 1 preserves original prefix exactly (keyword casing and spacing)
- Group 2 is the tool token/value
- Replacement only changes Group 2 when a mapping exists
- Mapping key lookup is case-sensitive on the tool value

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

- `bin\Release\net6.0-windows\win-x64\publish\CidToolRenamer.exe`

## Process History / Handoff Notes

This section summarizes the prior Cursor session and this session for future agents.

### Prior Session (from exported log)

1. Started from spec for a CID tool renamer with WinForms UI and mapping workflow.
2. Implemented base app with scanning, mapping grid, JSON load/save, apply, backup, dry-run, and status/progress.
3. Initial parser targeted `Tool=` style entries.
4. User clarified real format is `TOOLNAME=`.
5. Regex and behavior were updated to parse and replace `TOOLNAME=` entries instead.
6. User requested launching/publishing as EXE; assistant responses discussed `dotnet publish` commands and expected output path.

### This Session

1. Repository initialization and first push:
   - Initialized git repository in this project directory.
   - Added `.gitignore` for `bin/`, `obj/`, `.vs/`.
   - Created initial commit and pushed `main` to:
     - `https://github.com/DustinBobbitt/CAD-Tool-Name-Changer`
2. Added this `README.md` with complete handoff and expansion guidance.

## Known Gaps / Improvement Opportunities

- Confirmation message in apply flow still says `Tool=` text; update wording to `TOOLNAME=`.
- File discovery currently uses `*.cid` pattern. Validate behavior for uppercase `.CID` on all environments; if needed, use a case-insensitive extension filter.
- Error reporting is modal-only; could be improved with exportable log file.
- Processing is synchronous with `Application.DoEvents()`; could move to async/background worker for large batches.

## Planned Expansion (More File Types)

To expand beyond CID files, refactor to a parser strategy model:

1. Introduce a file-type configuration list:
   - extension(s)
   - regex pattern(s)
   - keyword semantics (`TOOLNAME`, `TOOL`, etc.)
2. Create a common parser interface (for scan and replace):
   - `ExtractTools(text) -> IEnumerable<string>`
   - `ApplyMapping(text, map) -> newText`
3. Add UI support:
   - checkbox list or dropdown for target file types
   - per-type settings if needed
4. Add tests with representative sample files per format.
5. Keep current `TOOLNAME` parser as default implementation.

## Suggested Next Agent Tasks

1. Update user-facing apply confirmation/status copy from `Tool=` to `TOOLNAME=`.
2. Add optional dual support for both `TOOLNAME=` and `Tool=` in a backward-compatible mode.
3. Add small test corpus and automated parser tests.
4. Add release notes section and versioning strategy.

