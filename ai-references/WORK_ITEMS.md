# Work Items: Project Overview Generator - Architectural Alignment

**Generated**: November 29, 2025  
**Priority**: Critical - Architectural Refactoring Required  
**Impact**: Fixes core output file generation to match documented requirements

---

## Executive Summary

The current implementation generates correct **component outputs** (parsed types, API endpoints, directory tree) but with **incorrect orchestration**:

-  **CURRENT**: Each category file contains duplicate project structure (tree + excluded paths)
-  **REQUIRED**: ProjectStructure.md as standalone file, ApiEndpoints.md with dedicated config, Category files without duplication

This document outlines work items to align implementation with requirements.

---

## Architecture Overview (Required State)

### Output File Types

| File Type | Purpose | Generation Frequency | Config Section | Content |
|-----------|---------|----------------------|-----------------|---------|
| **ProjectStructure.md** | Directory tree & excluded paths overview | Once (always) | \projectStructure\ | Directory tree + Excluded paths list |
| **ApiEndpoints.md** | All API endpoints matching criteria | Once (always) | \piEndpoints\ (NEW) | API endpoints table only |
| **Category Files** (User-defined) | Types from specified categories | Per generatedFile | \generatedFiles\ | Category-specific types + members ONLY (no tree) |

---

## Work Item Summary

### WORK ITEM 1: Update Configuration Models
- Add dedicated \ApiEndpointsConfig\ property to \ScanConfig.cs\
- Type: \CategoryConfig? ApiEndpoints\
- Rationale: Separates API endpoint extraction rules from category matching
- Files: \src/Models/ScanConfig.cs\

### WORK ITEM 2: Update Example Configuration
- Add \piEndpoints\ section to \i-scan-config.example.json\
- Include all matching rules: paths, patterns, excludedPatterns, excludedPaths, extensions
- Files: \i-scan-config.example.json\

### WORK ITEM 3: Refactor MarkdownGenerator
- Split into three methods:
  - \GenerateProjectStructure()\ - Tree + excluded paths only
  - \GenerateApiEndpoints()\ - API table only
  - \GenerateCategory()\ - Category types only
- Eliminate duplication of project structure
- Files: \src/Services/MarkdownGenerator.cs\

### WORK ITEM 4: Update Program.cs Pipeline
- Phase 1: Generate ProjectStructure.md (always)
- Phase 2: Generate ApiEndpoints.md (if configured)
- Phase 3: Generate category files (without project structure)
- Files: \Program.cs\

### WORK ITEM 5: CategoryMatchingEngine Updates
- Apply apiEndpoints rules to filter API endpoints
- Use same matching logic as categories
- Files: \src/Services/CategoryMatchingEngine.cs\

### WORK ITEM 6: API Endpoint Extraction Logic
- Update extraction to respect apiEndpoints config
- Filter endpoints by path, pattern, extension
- Files: \src/Services/MarkdownGenerator.cs\

### WORK ITEM 7: Unit Tests
- Add tests for three generation methods
- Add integration test for complete pipeline
- Verify no duplication, correct content per file type
- Files: Test project

### WORK ITEM 8: Update Documentation
- Update README.md with three file types explanation
- Update IMPLEMENTATION_SUMMARY.md with architecture
- Reference this work items document
- Files: \README.md\, \i-references/IMPLEMENTATION_SUMMARY.md\

---

## Success Criteria

### Functional Requirements
- [ ] ProjectStructure.md contains ONLY directory tree + excluded paths
- [ ] ApiEndpoints.md contains ONLY API endpoints table
- [ ] Category files contain ONLY category-specific types (no project structure)
- [ ] No duplication of project structure across files
- [ ] All three file types generated in correct order
- [ ] ApiEndpoints config section supports same matching rules as categories

### Code Quality
- [ ] All code builds without errors
- [ ] All new tests pass
- [ ] Code follows project conventions
- [ ] Comprehensive documentation updated

### Testing
- [ ] Unit tests for all three generation methods
- [ ] Integration test for complete pipeline
- [ ] Manual test on sample project confirms correct output
- [ ] Test coverage  80% for MarkdownGenerator

---

## Implementation Order

**Recommended sequence** (respects dependencies):

1. WORK ITEM 1: Update ScanConfig.cs (foundation)
2. WORK ITEM 2: Update ai-scan-config.example.json (demonstrates new structure)
3. WORK ITEM 3: Refactor MarkdownGenerator (core logic)
4. WORK ITEM 4: Update Program.cs pipeline (orchestration)
5. WORK ITEM 5: Update CategoryMatchingEngine (filtering)
6. WORK ITEM 6: API endpoint extraction logic (filtering)
7. WORK ITEM 7: Add unit tests (validation)
8. WORK ITEM 8: Update documentation (communication)

---

## Estimated Effort

| Work Item | Complexity | Estimated Hours |
|-----------|-----------|-----------------|
| 1. Config Models | Low | 1 |
| 2. Example Config | Low | 1 |
| 3. MarkdownGenerator Refactor | High | 4 |
| 4. Program.cs Pipeline | Medium | 3 |
| 5. CategoryMatchingEngine Updates | Low | 1 |
| 6. API Extraction Logic | Medium | 2 |
| 7. Unit Tests | Medium | 3 |
| 8. Documentation | Low | 2 |
| **TOTAL** | | **17 hours** |

---

## Status: Ready for Implementation

**Priority**: Critical - Architectural Alignment  
**Next Step**: Begin implementation of WORK ITEM 1
