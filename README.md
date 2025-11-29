# Project Overview Generator

A sophisticated .NET 8.0 console application that scans a codebase and generates category-based markdown documentation of your project architecture. It intelligently extracts types, methods, properties, API endpoints, and more from C# and TypeScript source files.

## Features

### Core Capabilities

- **Multi-Language Support**: Parse C# and TypeScript files with intelligent regex-based extraction
- **Category-Based Organization**: Define custom categories and generate separate documentation files for each
- **API Endpoint Extraction**: Automatically extracts API routes, HTTP methods, controllers, and handlers
- **Type Metadata Extraction**: Captures namespaces, inheritance, interfaces, methods, properties, and attributes
- **Flexible Configuration**: Comprehensive JSON configuration for fine-grained control
- **Error Reporting**: Lists unparsed files and provides detailed error messages

### Configuration Features

- **Path-Based Filtering**: Include/exclude directories using glob patterns
- **Pattern Matching**: Match files by name patterns and exclude by patterns
- **Extension Filtering**: Specify which file extensions belong to each category
- **Custom Categories**: Define unlimited custom categories with rich matching rules
- **Output File Mapping**: Map categories to output files for organized documentation
- **Global Exclusions**: Project-wide exclusions (bin, obj, node_modules, etc.)
- **Markdown Formatting**: Configurable header levels and code block styles

## Quick Start

### 1. Build the Project

```bash
cd project-overview-generator
dotnet build ProjectOverviewGenerator.csproj
```

### 2. Create Configuration

Copy `ai-scan-config.example.json` to `ai-scan-config.json` in your target project root and customize:

```bash
copy ai-scan-config.example.json "C:\path\to\your\project\ai-scan-config.json"
```

### 3. Run the Generator

```bash
dotnet run -- "C:\path\to\your\project"
```

The tool will generate markdown files in `PROJECT_OVERVIEW/` directory.

## Configuration Guide

### Basic Structure

```json
{
  "projectStructure": { ... },
  "categories": [ ... ],
  "generatedFiles": [ ... ],
  "markdown": { ... }
}
```

### Project Structure (Global Settings)

```json
{
  "projectStructure": {
    "description": "Global project-level settings",
    "excludedPaths": ["*/bin", "*/obj", ".git", "node_modules"]
  }
}
```

### Categories (Define what to include)

```json
{
  "categories": [
    {
      "name": "models",
      "description": "Data models, entities, and DTOs",
      "paths": ["*/Models", "*/Entities", "*/Domain/Models"],
      "patterns": ["Model", "Entity", "ViewModel", "Dto"],
      "excludedPatterns": ["Program", "Startup"],
      "excludedPaths": [],
      "extensions": ["*.cs"]
    },
    {
      "name": "services",
      "description": "Business logic services",
      "paths": ["*/Services"],
      "patterns": ["Service"],
      "excludedPatterns": ["ServiceProvider"],
      "extensions": ["*.cs"]
    }
  ]
}
```

### Category Matching Rules

Files match a category if ALL of the following are true:

1. **Extension**: File extension matches one in `extensions`
2. **Path**: File path contains one of the patterns in `paths` (if specified)
3. **Pattern**: File name contains one of the patterns in `patterns` (if specified)
4. **Not Excluded**: File path doesn't contain `excludedPaths` patterns
5. **Not Excluded Pattern**: File name doesn't contain `excludedPatterns`

### Generated Files (Map categories to output)

```json
{
  "generatedFiles": [
    {
      "name": "Domain.md",
      "description": "Domain models and entities",
      "includedCategories": ["models", "repositories"]
    },
    {
      "name": "API.md",
      "description": "API endpoints and controllers",
      "includedCategories": ["controllers", "api"]
    },
    {
      "name": "ProjectArchitecture.md",
      "description": "Complete overview",
      "includedCategories": [
        "models",
        "repositories",
        "services",
        "controllers",
        "api"
      ]
    }
  ]
}
```

### Markdown Configuration

```json
{
  "markdown": {
    "headerLevel": 2,
    "codeBlockStyle": "fenced"
  }
}
```

- **headerLevel**: Base header level (1=H1, 2=H2, etc.)
- **codeBlockStyle**: "fenced" for ``` or "indented" for 4-space indentation

## Example Configuration

See `ai-scan-config.example.json` for a complete, production-ready configuration with 7 predefined categories and 5 output files.

## Output Format

### Category Files

Each generated file includes:

- **Excluded Paths**: List of globally excluded paths
- **Directory Structure**: Full tree of all files and folders
- **Types Overview**: All types (classes, interfaces, enums) grouped by category
  - Namespace/Module
  - Base types and interfaces
  - Methods and properties
- **API Endpoints** (if present): Table of all API routes in the file

### Example Output

```markdown
## Project Architecture Overview

### Excluded Paths

- \*/bin
- \*/obj

### Directory Structure
```

root/
├── Models/
│ ├── User.cs
│ └── Product.cs
├── Services/
│ └── UserService.cs
...

```

### Models
- **User** (class)
  - Namespace/Module: `MyApp.Models`
  - Implements: IEntity
  - Members:
    - property: `Id` : Guid
    - method: `Validate()` : bool

### API Endpoints
| Route | HTTP Method | Controller | Handler | DTOs |
|-------|-------------|------------|---------|------|
| /api/users | GET | UserController | GetUsers | User |
```

## Architecture

### Core Services

- **WorkspaceScanner**: Recursively scans directories for C# and TypeScript files
- **FileParserEngine**: Dispatches files to language-specific parsers
- **CsFileParser** / **TsFileParser**: Extract types, methods, and API endpoints
- **CategoryMatchingEngine**: Matches files to categories based on config rules
- **MarkdownGenerator**: Generates markdown output from parsed metadata
- **ConfigurationService**: Loads and validates configuration files
- **OutputWriter**: Writes markdown files to disk

### Data Models

- **FileMetadata**: Parsed file information with types and endpoints
- **TypeMetadata**: Class/interface/enum with members and inheritance
- **MemberMetadata**: Method or property details
- **ApiEndpointMetadata**: Route, HTTP method, controller, handler
- **ScanConfig**: Complete configuration structure

## Extending the Tool

### Adding a New Parser

1. Create a class implementing `IFileParser`
2. Implement `CanParse(extension)`, `Parse()`, and `GetSupportedExtensions()`
3. Register in `Program.cs`: `parsers.Add(new MyLanguageParser())`

```csharp
public class PythonFileParser : IFileParser
{
    public bool CanParse(string extension) => extension.ToLower() == ".py";
    public List<string> GetSupportedExtensions() => new() { ".py" };
    public FileMetadata Parse(string filePath, string fileContent) { ... }
}
```

### Adding a New Category

Simply add to `categories` in your `ai-scan-config.json`:

```json
{
  "name": "middleware",
  "description": "Middleware and filters",
  "paths": ["*/Middleware"],
  "patterns": ["Middleware"],
  "extensions": ["*.cs"]
}
```

Then reference it in `generatedFiles`:

```json
{
  "name": "Middleware.md",
  "includedCategories": ["middleware"]
}
```

## Requirements

- .NET 8.0 or later
- C# source files (`.cs`)
- TypeScript source files (`.ts`)

## Build & Release

### Build

```bash
dotnet build ProjectOverviewGenerator.csproj
```

### Run Tests (if applicable)

```bash
dotnet test
```

### Package

```bash
dotnet publish -c Release -o ./publish
```

## Troubleshooting

### Configuration not found

- Ensure `ai-scan-config.json` exists in the target project root
- Check file path: `<project>\ai-scan-config.json`

### No files found

- Verify the project has `.cs` or `.ts` files
- Check `excludedPaths` aren't filtering out all files
- Ensure category `extensions` match your file types

### Categories not matching

- Verify `paths` patterns (glob-like: `*/Services`)
- Check `patterns` against actual file names
- Ensure `excludedPatterns` aren't blocking files
- Review `extensions` for correct format (e.g., `*.cs`)

## Performance Notes

- Large projects (1000+ files) may take 10-30 seconds
- Regex parsing is the bottleneck for very large source files
- Consider using `excludedPaths` to skip unnecessary directories

## License

TBD

## Contact

For issues, questions, or contributions, please open an issue or submit a pull request.
