using System.Collections.Generic;
using ProjectOverviewGenerator.Models;

namespace ProjectOverviewGenerator.Interfaces
{
    /// <summary>
    /// Interface for language-specific file parsers.
    /// </summary>
    public interface IFileParser
    {
        /// <summary>
        /// Determines if this parser can handle the given file extension.
        /// </summary>
        bool CanParse(string extension);

        /// <summary>
        /// Parses the file content and returns metadata.
        /// </summary>
        FileMetadata Parse(string filePath, string fileContent);

        /// <summary>
        /// Gets the list of file extensions this parser supports.
        /// </summary>
        List<string> GetSupportedExtensions();
    }
}
