using System.Collections.Generic;

namespace ProjectOverviewGenerator.Models
{
    /// <summary>
    /// Represents the complete configuration for the project overview generator.
    /// </summary>
    public class ScanConfig
    {
        public ProjectStructureConfig? ProjectStructure { get; set; }
        public CategoryConfig? ApiEndpoints { get; set; }
        public List<CategoryConfig> Categories { get; set; } = new List<CategoryConfig>();
        public List<GeneratedFileConfig> GeneratedFiles { get; set; } = new List<GeneratedFileConfig>();
        public MarkdownConfig? Markdown { get; set; }
    }

    /// <summary>
    /// Represents global project-level settings.
    /// </summary>
    public class ProjectStructureConfig
    {
        public string? Description { get; set; }
        public List<string> ExcludedPaths { get; set; } = new List<string>();
        public List<string> ExcludedExtensions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a category configuration with rich matching rules.
    /// </summary>
    public class CategoryConfig
    {
        /// <summary>
        /// Unique identifier for the category (e.g., "models", "services").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description of the category.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Paths to include (glob patterns). Files under these paths match the category.
        /// </summary>
        public List<string> Paths { get; set; } = new List<string>();

        /// <summary>
        /// File name patterns to match (e.g., "Service", "Model").
        /// Matches if the file name contains any of these patterns.
        /// </summary>
        public List<string> Patterns { get; set; } = new List<string>();

        /// <summary>
        /// File name patterns to exclude. Overrides Patterns if a match is found.
        /// </summary>
        public List<string> ExcludedPatterns { get; set; } = new List<string>();

        /// <summary>
        /// Paths to exclude at the category level. Takes precedence over Paths.
        /// </summary>
        public List<string> ExcludedPaths { get; set; } = new List<string>();

        /// <summary>
        /// File extensions to include (e.g., "*.cs", "*.ts").
        /// </summary>
        public List<string> Extensions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a generated output file configuration.
    /// </summary>
    public class GeneratedFileConfig
    {
        /// <summary>
        /// Output file name (e.g., "Domain.md", "API.md").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description of the file.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// List of category names to include in this file.
        /// Only types matching these categories will be included.
        /// </summary>
        public List<string> IncludedCategories { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents markdown formatting options.
    /// </summary>
    public class MarkdownConfig
    {
        /// <summary>
        /// Base header level (1 for H1, 2 for H2, etc.). Default is 2.
        /// </summary>
        public int HeaderLevel { get; set; } = 2;

        /// <summary>
        /// Code block style: "fenced" for ``` or "indented" for 4-space indentation.
        /// </summary>
        public string CodeBlockStyle { get; set; } = "fenced";
    }
}
