# Project Overview Generator: System Design & Implementation Plan

## 1. System Architecture Blueprint

### 1.1 Component/Module Diagram

```
+-------------------------------------------------------------+
|                  Project Overview Generator                 |
+-------------------------------------------------------------+
|                                                             |
|  [1] ConfigurationProvider                                  |
|      |                                                      |
|  [2] WorkspaceScanner                                       |
|      |                                                      |
|  [3] FileParserEngine <-------------------+                 |
|      |                                     |                |
|      +-- IFileParser (interface)           |                |
|      |    |                                |                |
|      |    +-- CsFileParser (.cs)           |                |
|      |    +-- TsFileParser (.ts)           |                |
|      |    +-- [Extensible: New Parsers]    |                |
|      |                                     |                |
|  [4] ClassificationEngine                  |                |
|      |                                     |                |
|  [5] MetadataModel (Domain)                |                |
|      |                                     |                |
|  [6] MarkdownGenerator                     |                |
|      |                                     |                |
|  [7] OutputWriter (PROJECT_OVERVIEW/)      |                |
+-------------------------------------------------------------+
```

### 1.2 Responsibilities

- **ConfigurationProvider**: Loads and validates config file, exposes settings to all modules.
- **WorkspaceScanner**: Recursively scans directories, applies include/exclude rules, yields files for parsing.
- **FileParserEngine**: Orchestrates parsing, delegates to appropriate IFileParser implementation based on file type.
- **IFileParser**: Interface for all file parsers; contract for extracting metadata.
- **ClassificationEngine**: Applies classification rules (directory, name, pattern, custom) to parsed metadata.
- **MetadataModel**: Domain models for files, types, APIs, etc.
- **MarkdownGenerator**: Produces markdown content for each required and user-defined output.
- **OutputWriter**: Writes markdown files to PROJECT_OVERVIEW, ensures required files always exist.

### 1.3 Extensibility Points

- **IFileParser**: Add new file type support by implementing this interface.
- **ClassificationEngine**: Plug in new classification strategies.
- **MarkdownGenerator**: Add new markdown sections or output formats.

### 1.4 End-to-End Data Flow

1. **Startup**: Load config via ConfigurationProvider.
2. **Scan**: WorkspaceScanner traverses root directory, applies include/exclude rules.
3. **Parse**: FileParserEngine selects appropriate IFileParser for each file, extracts metadata.
4. **Classify**: ClassificationEngine assigns categories to files/types.
5. **Generate**: MarkdownGenerator creates markdown content per spec.
6. **Write**: OutputWriter saves all output under PROJECT_OVERVIEW.

---

## 2. Domain & Metadata Model

### 2.1 File Metadata Abstraction

```csharp
public class FileMetadata
{
    public string FilePath { get; set; }
    public string RelativePath { get; set; }
    public string Extension { get; set; }
    public string Category { get; set; }
    public List<TypeMetadata> Types { get; set; }
}
```

### 2.2 Type/Class/Method/Property Models

```csharp
public class TypeMetadata
{
    public string Name { get; set; }
    public string Kind { get; set; } // class, interface, enum, record, etc.
    public string NamespaceOrModule { get; set; }
    public List<string> BaseTypes { get; set; }
    public List<string> ImplementedInterfaces { get; set; }
    public List<AttributeMetadata> Attributes { get; set; }
    public List<MemberMetadata> Members { get; set; }
}

public class MemberMetadata
{
    public string Name { get; set; }
    public string MemberType { get; set; } // property, method, constructor, etc.
    public string ReturnType { get; set; }
    public List<AttributeMetadata> Attributes { get; set; }
    public List<ParameterMetadata> Parameters { get; set; }
}

public class AttributeMetadata
{
    public string Name { get; set; }
    public Dictionary<string, string> Arguments { get; set; }
}

public class ParameterMetadata
{
    public string Name { get; set; }
    public string Type { get; set; }
}
```

### 2.3 API Endpoint Model

```csharp
public class ApiEndpointMetadata
{
    public string Route { get; set; }
    public string HttpMethod { get; set; }
    public string Controller { get; set; }
    public string Handler { get; set; }
    public List<string> Dtos { get; set; }
}
```

### 2.4 Config Schema (Sample JSON)

```json
{
  "outputFiles": {
    "architecture": "ProjectArchitecture.md",
    "api": "ApiEndpoints.md"
  },
  "includeExtensions": [".cs", ".ts"],
  "excludePaths": ["bin/", "obj/", "node_modules/"],
  "classificationRules": [
    { "type": "directory", "pattern": "Services", "category": "Services" },
    { "type": "name", "pattern": "*ViewModel.cs", "category": "View Models" }
  ],
  "customCategories": ["Repositories", "DTOs"],
  "activeParsers": ["cs", "ts"],
  "markdown": {
    "headerLevel": 2,
    "codeBlockStyle": "fenced"
  }
}
```

---

## 3. File Parsing Strategy

### 3.1 IFileParser Interface

```csharp
public interface IFileParser
{
    bool CanParse(string extension);
    FileMetadata Parse(string filePath, string fileContent);
}
```

### 3.2 .cs and .ts Parsers

- **CsFileParser**: Parses C# files using Roslyn or regex for structure, extracts types, members, attributes, API routes.
- **TsFileParser**: Parses TypeScript files using a TypeScript parser or regex, extracts classes, interfaces, decorators, etc.

### 3.3 Adding New Extensions

- Implement IFileParser for the new file type.
- Register the parser in FileParserEngine.
- Add extension to config.

---

## 4. Processing Workflow

1. **Directory Scanning**: WorkspaceScanner walks the directory tree, applies config rules.
2. **Parsing**: For each file, FileParserEngine selects parser, extracts FileMetadata.
3. **Classification**: ClassificationEngine assigns categories using config rules.
4. **Metadata Aggregation**: All metadata is collected into a central model.
5. **Markdown Generation**: MarkdownGenerator creates output for each required/user-defined file.
6. **Output Writing**: OutputWriter writes all markdown files to PROJECT_OVERVIEW.

---

## 5. Markdown Output Specs

### 5.1 Example Layouts

#### Project Architecture Overview

```
# Project Architecture Overview

## Directory Structure

<pre>
root/
├── Services/
│   └── UserService.cs
├── Controllers/
│   └── UserController.cs
├── Models/
│   └── UserViewModel.cs
...
</pre>

## Classified Types

### Services

- **UserService**
  - Namespace: MyApp.Services
  - Methods: GetUser, CreateUser
  - Attributes: [Injectable]

### View Models

- **UserViewModel**
  - Properties: Id, Name, Email
```

#### API Endpoints Overview

```
# API Endpoints Overview

| Route         | HTTP Method | Controller      | Handler      | DTOs         |
|---------------|------------|-----------------|--------------|--------------|
| /api/users    | GET        | UserController  | GetUsers     | UserDto      |
| /api/users    | POST       | UserController  | CreateUser   | CreateUserDto|
```

### 5.2 Formatting Rules

- Use clear headers, tables, and code blocks.
- All files in `PROJECT_OVERVIEW/`.
- Required files: `ProjectArchitecture.md`, `ApiEndpoints.md` (names can be customized in config, but must exist).
- User-defined files: Generated as per config.

---

## 6. Extensibility & Maintenance

### 6.1 Adding New File Type Support

- Implement `IFileParser` for the new type.
- Register in FileParserEngine.
- Add extension to config.

### 6.2 Adding New Markdown Sections

- Extend MarkdownGenerator with new section logic.
- Add section to config if user-customizable.

### 6.3 Integrating New Architectures/Patterns

- Update ClassificationEngine with new rules.
- Add new categories in config.

---

## Summary

This design provides a robust, extensible, and maintainable foundation for a .NET 8.0 console application that generates AI-optimized project overviews. It enforces clean separation of concerns, supports multi-language parsing, and is fully driven by user configuration. All outputs are markdown files under `PROJECT_OVERVIEW/`, with required and user-defined sections, ensuring both AI agents and human developers can quickly understand and navigate any codebase.

---

_Next step: Begin implementation scaffolding for the main components as described above._
