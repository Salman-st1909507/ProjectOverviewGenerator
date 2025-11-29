using System;
using System.Collections.Generic;
using System.Linq;
using ProjectOverviewGenerator.Models;

namespace ProjectOverviewGenerator.Services
{
    /// <summary>
    /// Service for matching files and types to categories based on configuration rules.
    /// </summary>
    public class CategoryMatchingEngine
    {
        private readonly ScanConfig _config;

        public CategoryMatchingEngine(ScanConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Determines which categories a file matches based on configuration rules.
        /// Enforces single-category rule: each file can only belong to ONE category.
        /// If a file matches multiple categories, an exception is thrown.
        /// </summary>
        public List<string> MatchFileToCategories(FileMetadata file)
        {
            var matchedCategories = new List<string>();

            if (_config?.Categories == null || _config.Categories.Count == 0)
                return matchedCategories;

            foreach (var category in _config.Categories)
            {
                if (MatchesCategory(file, category))
                {
                    matchedCategories.Add(category.Name);
                }
            }

            // Enforce single-category constraint
            if (matchedCategories.Count > 1)
            {
                throw new InvalidOperationException(
                    $"File '{file.FilePath}' matches multiple categories: {string.Join(", ", matchedCategories)}. " +
                    $"Each file must belong to exactly one category. Please review your category configuration rules " +
                    $"to ensure paths, patterns, and exclusions do not overlap.");
            }

            return matchedCategories;
        }

        /// <summary>
        /// Determines if a file matches a specific category.
        /// </summary>
        private bool MatchesCategory(FileMetadata file, CategoryConfig category)
        {
            // Check extensions
            if (category.Extensions != null && category.Extensions.Count > 0)
            {
                var hasValidExtension = category.Extensions.Any(ext =>
                    file.Extension.Equals(ext.Replace("*", ""), StringComparison.OrdinalIgnoreCase));

                if (!hasValidExtension)
                    return false;
            }

            // Check paths (include)
            bool pathMatched = false;
            if (category.Paths != null && category.Paths.Count > 0)
            {
                pathMatched = category.Paths.Any(path =>
                    PathMatchesPattern(file.RelativePath, path));
            }
            else
            {
                pathMatched = true; // No paths specified means all paths match
            }

            if (!pathMatched)
                return false;

            // Check excluded paths
            if (category.ExcludedPaths != null && category.ExcludedPaths.Count > 0)
            {
                bool isExcluded = category.ExcludedPaths.Any(path =>
                    PathMatchesPattern(file.RelativePath, path));

                if (isExcluded)
                    return false;
            }

            // Check file name patterns (include)
            bool patternMatched = false;
            if (category.Patterns != null && category.Patterns.Count > 0)
            {
                patternMatched = category.Patterns.Any(pattern =>
                    file.FilePath.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                patternMatched = true; // No patterns specified means all files match
            }

            // Check excluded patterns
            if (category.ExcludedPatterns != null && category.ExcludedPatterns.Count > 0)
            {
                bool isExcludedByPattern = category.ExcludedPatterns.Any(pattern =>
                    file.FilePath.Contains(pattern, StringComparison.OrdinalIgnoreCase));

                if (isExcludedByPattern)
                    return false;
            }

            return patternMatched;
        }

        /// <summary>
        /// Simple glob-like pattern matching for paths.
        /// Supports wildcards: * for any string, ? for single character.
        /// </summary>
        private bool PathMatchesPattern(string path, string pattern)
        {
            // Normalize separators
            path = path.Replace("\\", "/").ToLower();
            pattern = pattern.Replace("\\", "/").ToLower();

            // Simple wildcard matching
            if (pattern == "*" || pattern == "*/*" || pattern == "**")
                return true;

            if (pattern.StartsWith("*/"))
            {
                // */Services -> matches any directory named Services
                var suffix = pattern.Substring(2);
                return path.Contains("/" + suffix) || path.EndsWith(suffix);
            }

            return path.Contains(pattern);
        }

        /// <summary>
        /// Gets all files that belong to a specific category.
        /// </summary>
        public List<FileMetadata> GetFilesInCategory(IEnumerable<FileMetadata> allFiles, string categoryName)
        {
            return allFiles.Where(f => f.MatchedCategories.Contains(categoryName)).ToList();
        }

        /// <summary>
        /// Gets all types that belong to a specific category.
        /// </summary>
        public List<TypeMetadata> GetTypesInCategory(IEnumerable<FileMetadata> allFiles, string categoryName)
        {
            return allFiles
                .Where(f => f.MatchedCategories.Contains(categoryName))
                .SelectMany(f => f.Types)
                .ToList();
        }

        /// <summary>
        /// Gets all API endpoints that belong to a specific category.
        /// </summary>
        public List<ApiEndpointMetadata> GetApiEndpointsInCategory(IEnumerable<FileMetadata> allFiles, string categoryName)
        {
            return allFiles
                .Where(f => f.MatchedCategories.Contains(categoryName))
                .SelectMany(f => f.ApiEndpoints)
                .ToList();
        }
    }
}
