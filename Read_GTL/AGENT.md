# AGENT.md — GTL Text Editor (GTL Editor)

## Project Overview
- **Application:** GTL Text Editor (`GTL Editor.sln`, `Read_GTL`)
- **Stack:** C# / .NET Windows Forms desktop application
- **Target Ecosystem:** Companion tool for **Cyberboard Designer** (created by Dale L. Larson), a digital board game and tabletop wargame design system.
- **Primary Purpose:** Inspects, edits, and batches text and numerical annotations onto tiles stored in Cyberboard Game Tile Library (`.gtl`) binary files.

---

## Key Architecture & Source Files
- **`GTL_File.cs` (Binary I/O & Tile Data Model):**
  - Parses, modifies, and serializes Cyberboard `GTLB` binary files (version `0.3+`).
  - Manages multi-resolution tile structures per counter:
    - `FullTile`: Full-size bitmap counter/tile.
    - `HalfTile`: Half-size bitmap counter/tile.
    - `SmallTile`: Representative color / mini tile.
  - Handles 16-bit, 24-bit, and 32-bit bitmap headers, color tables, and raw DIB (Device-Independent Bitmap) byte conversions.
- **`GTL Text Editor.cs` (Main WinForms UI & GDI+ Rendering):**
  - Handles interactive canvas placement (drag-and-drop and scrollbar X/Y positioning) and high-magnification zoom preview.
  - Executes GDI+ text stamping with 360° rotation (slider + `0°`, `90°`, `180°`, `270°` quick presets), font family/size/style (`Bold`/`Italic`), foreground color, and text rendering hints (`SingleBitPerPixel` vs. `AntiAlias`).
  - Runs batch auto-increment/sequencing across tile ranges (unit IDs, combat factors, movement allowances, step counters).
  - Implements interactive color sampling via `Eyedropper.cur` and Windows Registry MRU (Most Recently Used) file tracking.
- **`PositionDataDto` (Templates & Presets):**
  - JSON/DTO serializable model storing text coordinates, typography, color, and rotation settings for repeatable layouts across tiles and `.gtl` libraries.
- **`gtlTileDisplay` (Tile Strip & Viewport Control):**
  - Custom visual thumbnail browser for selecting and navigating individual tiles within a loaded library.

---

## Critical Invariants & Guardrails
1. **Binary Format Integrity (`GTLB` v0.3+):**
   - Never alter byte offsets, header lengths, color table padding, or DIB stride alignment in `GTL_File.cs` unless explicitly verified against Cyberboard's binary specification.
   - Always keep `FullTile`, `HalfTile`, and `SmallTile` synchronized when committing edits so Cyberboard Designer loads zoomed-out and mini-map views without corruption.
2. **Pixel & Color Accuracy:**
   - Respect transparency/mask colors used by Cyberboard `.gtl` bitmaps when stamping text or sampling with the eyedropper.
   - Preserve exact `PixelFormat` (16-bit, 24-bit, 32-bit) and DIB row padding during `Bitmap` <-> raw byte round-trips.
3. **GDI+ Resource Management:**
   - Always wrap `Graphics`, `Font`, `Brush`, `Pen`, `Matrix`, and temporary `Bitmap` instances in `using` blocks to prevent GDI handle leaks during batch stamping or zoom redraws.
   - Ensure rotation transforms translate around the intended anchor/pivot point cleanly before resetting the `Graphics` transform matrix.
4. **Preset Backward Compatibility (`PositionDataDto`):**
   - When adding properties to `PositionDataDto`, provide sensible default values so existing saved JSON preset files deserialize without breaking.

---

## Agent Workflow Rules
- **File Isolation:** Keep binary serialization logic strictly inside `GTL_File.cs` and data transfer schemas inside `PositionDataDto`; do not leak raw stream parsing into WinForms event handlers.
- **WinForms Designer Safety:** Do not manually rewrite `*.Designer.cs` layout code unless specifically instructed; keep business and rendering logic in the main code-behind files.
- **Verification:** Before concluding changes, verify that the solution builds cleanly (`dotnet build` or MSBuild on `GTL Editor.sln`) with zero new compiler warnings and confirm no unintended changes were made to `.gtl` binary read/write paths.

---

## Release & Installer Packaging Guidelines
- **Incrementing Version Policy:** Whenever an `.msi` installer is built or published, its version number must increment cleanly on each release across `AssemblyInfo.cs`, `About.cs`, and `Package.wxs`.
- **MSI Naming Convention:** The installer output filename in `GTLEditorInstaller.wixproj` must strictly follow the `GTL Text Editor - <version>.msi` naming convention (e.g., `GTL Text Editor - 1.9.msi`), without superfluous `.0.0` extensions.
- **Git Hygiene:** Build artifacts (`bin/`, `obj/`, `.vs/`, `*.user`) and `.msi` binary installer packages must remain in `.gitignore` and never be committed into regular Git branch history.