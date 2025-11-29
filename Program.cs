using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectOverviewGenerator.Services;
using ProjectOverviewGenerator.Models;
using ProjectOverviewGenerator.Services.Parsers;
using ProjectOverviewGenerator.Interfaces;

namespace ProjectOverviewGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Project Overview Generator starting...");

                string rootPath = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
                Console.WriteLine($"Scanning: {rootPath}");

                // Load configuration
                string configPath = Path.Combine(rootPath, "ai-scan-config.json");
                if (!File.Exists(configPath))
                {
                    configPath = Path.Combine(rootPath, "ai-scan-config.example.json");
                }

                if (!File.Exists(configPath))
                {
                    throw new FileNotFoundException($"No configuration file found. Expected: ai-scan-config.json or ai-scan-config.example.json");
                }

                ScanConfig config = ConfigurationService.LoadConfig(configPath);
                Console.WriteLine($"Loaded configuration from: {configPath}");

                // Scan workspace
                var scanner = new WorkspaceScanner();
                var allFiles = scanner.Scan(rootPath);
                Console.WriteLine($"Found {allFiles.Count} files");

                // Set up parsers (C# and TypeScript only)
                var parsers = new List<IFileParser>
                {
                    new CsFileParser(),
                    new TsFileParser()
                };
                var parserEngine = new FileParserEngine(parsers);

                // Parse files
                var fileMetadatas = new List<FileMetadata>();
                var unparsedFiles = new List<string>();

                foreach (var filePath in allFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(filePath);
                        var relativePath = Path.GetRelativePath(rootPath, filePath);
                        var metadata = parserEngine.ParseFile(filePath, content);

                        if (metadata != null)
                        {
                            metadata.RelativePath = relativePath;
                            fileMetadatas.Add(metadata);
                        }
                        else
                        {
                            unparsedFiles.Add(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing {filePath}: {ex.Message}");
                        unparsedFiles.Add(filePath);
                    }
                }

                Console.WriteLine($"Successfully parsed {fileMetadatas.Count} files");
                Console.WriteLine($"Unparsed files: {unparsedFiles.Count}");

                // Apply category matching
                var categoryMatcher = new CategoryMatchingEngine(config);
                foreach (var file in fileMetadatas)
                {
                    file.MatchedCategories = categoryMatcher.MatchFileToCategories(file);
                }

                // Log category assignments
                Console.WriteLine("\nCategory Assignments:");
                var categoryGroups = fileMetadatas
                    .GroupBy(f => f.MatchedCategories.FirstOrDefault() ?? "uncategorized")
                    .OrderBy(g => g.Key);

                foreach (var group in categoryGroups)
                {
                    Console.WriteLine($"  [{group.Key}] {group.Count()} files");
                    foreach (var file in group.Take(3))  // Show first 3 files per category
                    {
                        Console.WriteLine($"    - {Path.GetFileName(file.FilePath)}");
                    }
                    if (group.Count() > 3)
                    {
                        Console.WriteLine($"    ... and {group.Count() - 3} more");
                    }
                }

                // Generate output files
                var markdownGen = new MarkdownGenerator();
                var outputWriter = new OutputWriter();
                string overviewDir = Path.Combine(rootPath, "PROJECT_OVERVIEW");
                Directory.CreateDirectory(overviewDir);

                var generatedFiles = new List<string>();

                // PHASE 1: Always generate ProjectStructure.md
                Console.WriteLine("Generating ProjectStructure.md...");
                var projectStructureMarkdown = markdownGen.GenerateProjectStructure(
                    rootPath,
                    config.ProjectStructure,
                    config.Markdown);
                outputWriter.WriteFile(overviewDir, "ProjectStructure.md", projectStructureMarkdown);
                generatedFiles.Add("ProjectStructure.md");

                // PHASE 2: Generate ApiEndpoints.md (if config provides apiEndpoints)
                if (config.ApiEndpoints != null)
                {
                    Console.WriteLine("Generating ApiEndpoints.md...");
                    var apiEndpointsMarkdown = markdownGen.GenerateApiEndpoints(
                        fileMetadatas,
                        config.ApiEndpoints,
                        config.Markdown);
                    outputWriter.WriteFile(overviewDir, "ApiEndpoints.md", apiEndpointsMarkdown);
                    generatedFiles.Add("ApiEndpoints.md");
                }

                // PHASE 3: Generate category-based files (without project structure)
                if (config.GeneratedFiles != null && config.GeneratedFiles.Count > 0)
                {
                    foreach (var fileConfig in config.GeneratedFiles)
                    {
                        var relevantFiles = fileMetadatas
                            .Where(f => f.MatchedCategories.Any(c => fileConfig.IncludedCategories.Contains(c)))
                            .ToList();

                        if (relevantFiles.Count > 0)
                        {
                            Console.WriteLine($"Generating {fileConfig.Name}...");
                            var categoryMarkdown = markdownGen.GenerateCategory(
                                relevantFiles,
                                fileConfig.IncludedCategories,
                                config.Markdown);

                            outputWriter.WriteFile(overviewDir, fileConfig.Name, categoryMarkdown);
                            generatedFiles.Add(fileConfig.Name);
                        }
                    }
                }

                Console.WriteLine($"\nGenerated: {string.Join(", ", generatedFiles)}");
                Console.WriteLine($"Project overview generated in {overviewDir}/");
                Console.WriteLine("Success!");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
                Environment.Exit(1);
            }
        }
    }
}
