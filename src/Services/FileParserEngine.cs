using System.Collections.Generic;
using System.Linq;
using ProjectOverviewGenerator.Models;
using ProjectOverviewGenerator.Interfaces;

namespace ProjectOverviewGenerator.Services
{
    /// <summary>
    /// Engine for parsing files using appropriate language-specific parsers.
    /// </summary>
    public class FileParserEngine
    {
        private readonly List<IFileParser> _parsers;

        public FileParserEngine(IEnumerable<IFileParser> parsers)
        {
            _parsers = parsers.ToList();
        }

        /// <summary>
        /// Parses a single file and returns its metadata.
        /// </summary>
        public FileMetadata? ParseFile(string filePath, string fileContent)
        {
            var ext = System.IO.Path.GetExtension(filePath);
            var parser = _parsers.FirstOrDefault(p => p.CanParse(ext));
            return parser?.Parse(filePath, fileContent);
        }

        /// <summary>
        /// Gets the list of supported file extensions.
        /// </summary>
        public List<string> GetSupportedExtensions()
        {
            return _parsers
                .SelectMany(p => p.GetSupportedExtensions())
                .Distinct()
                .ToList();
        }
    }
}
