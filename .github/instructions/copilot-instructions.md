# Copilot Instructions for ProjectOverviewGenerator

## Project Purpose & Architecture

**What it does**: A .NET console app that scans user workspaces, parses C# and TypeScript code, categorizes files by rules, and generates structured markdown documentation.

**Why the design**: Regex-based parsing (no AST dependencies) + stateless parsers + config-driven rules = lightweight, extensible, zero external parsing dependencies.

**Data flow**: `Program.cs` → ConfigLoad → WorkspaceScan → FileParserEngine (delegates to CsFileParser/TsFileParser) → CategoryMatchingEngine (validates single-category per file) → MarkdownGenerator → OutputWriter → `PROJECT_OVERVIEW/`

## Key Architectural Decisions

1. **Single-Category Constraint** (`CategoryMatchingEngine.cs`, lines 24-50):
   - Each file belongs to exactly ONE category; throws `InvalidOperationException` if file matches multiple categories.
   - This is a validation rule, not a soft guideline—config patterns must NOT overlap.
   - **Real example**: If `combat` category has pattern `Weapon` and `utilities` has pattern `Service`, ensure no file is named like `WeaponService.cs` matching both.

2. **Member Signature Extraction** (`CsFileParser.cs`, lines 43-112):
   - Parses class bodies line-by-line, extracting full method/property/field signatures including modifiers, types, and parameters.
   - Stores in `MemberMetadata.Signature` property; used by `MarkdownGenerator` for output.
   - **Pattern**: `fullSignature = "public void _Process(double delta)"` not just `"_Process : void"`.

3. **Exclusion Filtering** (`MarkdownGenerator.cs`, lines 23-48):
   - Excluded paths/extensions are filtered from directory tree at build-time via `ShouldExcludePath()` and `ShouldExcludeExtension()`.
   - **Not** just documented—actually removed from tree output.
   - Also documented in ProjectStructure.md for AI/developer awareness.

4. **Config-Driven Generation** (`Program.cs`, lines 110-160):
   - Three-phase pipeline:
     1. **PHASE 1**: Always generate `ProjectStructure.md` (directory tree + exclusion documentation).
     2. **PHASE 2**: Generate `ApiEndpoints.md` (if config has `apiEndpoints` section).
     3. **PHASE 3**: Generate category-specific files (e.g., `GameSystems.md`, `Utilities.md`) based on `generatedFiles[]` config.

## Critical Patterns & Conventions

- **Parsers are stateless**: `IFileParser.Parse()` only depends on file path + content; no side effects.
- **Metadata models are schema**: `FileMetadata`, `TypeMetadata`, `MemberMetadata`, `ApiEndpointMetadata`—all represent parsed code structure.
- **Category matching uses rules**: paths (glob), patterns (substring match), excludedPatterns (override), extensions—all applied in `CategoryMatchingEngine.MatchesCategory()`.
- **Markdown output is multi-level**: h2 headers for categories, h3 for types, members indented with backticks.

## Developer Workflows

- **Build**: `dotnet build` or `dotnet build ProjectOverviewGenerator.csproj -c Release`.
- **Run**: `dotnet run -- "C:\path\to\workspace"` (scans workspace, generates `PROJECT_OVERVIEW/` at root).
- **Add Parser**: Implement `IFileParser` in `Services/Parsers/`, register in `Program.cs` line ~47.
- **Fix Category Overlaps**: Audit `ai-scan-config.json` patterns—use `excludedPatterns` to disambiguate.
- **Debug Output**: Check `PROJECT_OVERVIEW/ProjectStructure.md` for what's excluded; check category-specific files for correct member signatures.

## Common Gotchas & Solutions

| Problem | Root Cause | Solution |
|---------|-----------|----------|
| File matches multiple categories | Overlapping patterns in config | Add `excludedPatterns` to one category to disambiguate |
| `.git`, `Library` dirs in tree | Exclusions not applied at tree-build time | Ensure `ProjectStructureConfig.ExcludedPaths` is populated; verify `ShouldExcludePath()` is called in `BuildTreeRecursive()` |
| Member signatures show as `` `method` Name : void `` | Signature extraction failed or fallback was used | Check `CsFileParser` line extraction; verify `MemberMetadata.Signature` is set |
| File marked `uncategorized` | No category rule matched | Check config patterns, paths, extensions; add debugging in `CategoryMatchingEngine.MatchesCategory()` |

## Files to Review for Common Tasks

- **Understand overall flow**: `Program.cs` (orchestration) + `SYSTEM_DESIGN.md` (architectural rationale).
- **Fix categorization**: `CategoryMatchingEngine.cs` (matching logic) + `ai-scan-config.example.json` (test config for Godot project).
- **Improve parsing**: `CsFileParser.cs` (C# member extraction) or `TsFileParser.cs` (TypeScript equivalent).
- **Adjust output format**: `MarkdownGenerator.cs` (all markdown generation methods; note h2/h3 header levels from config).
- **Add new category/output**: Modify `ai-scan-config.json` → add category rule + add entry to `generatedFiles[]`.

---

**Documentation references**: `README.md` (quick start + config guide), `SYSTEM_DESIGN.md` (architecture), `ai-scan-config.example.json` (working example for Godot 3D Rogue-Like game with 33 C# files, 88% categorization, 0 overlaps).
