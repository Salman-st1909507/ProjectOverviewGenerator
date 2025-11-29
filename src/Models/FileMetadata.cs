using System.Collections.Generic;

namespace ProjectOverviewGenerator.Models
{
    /// <summary>
    /// Represents metadata for a single source file.
    /// </summary>
    public class FileMetadata
    {
        /// <summary>
        /// Full file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Relative path from the root directory.
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// File extension (e.g., ".cs", ".ts").
        /// </summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// Category name(s) this file belongs to (comma-separated if multiple).
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// List of types (classes, interfaces, etc.) defined in this file.
        /// </summary>
        public List<TypeMetadata> Types { get; set; } = new List<TypeMetadata>();

        /// <summary>
        /// List of API endpoints defined in this file.
        /// </summary>
        public List<ApiEndpointMetadata> ApiEndpoints { get; set; } = new List<ApiEndpointMetadata>();

        /// <summary>
        /// List of categories this file matches (as determined by CategoryConfig rules).
        /// </summary>
        public List<string> MatchedCategories { get; set; } = new List<string>();
    }
}
