using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectOverviewGenerator.Models;

namespace ProjectOverviewGenerator.Services
{
    /// <summary>
    /// Scans a workspace directory and enumerates all relevant source files.
    /// </summary>
    public class WorkspaceScanner
    {
        /// <summary>
        /// Scans the root path and returns all source files that should be processed.
        /// Only includes C# and TypeScript files, excluding binary directories.
        /// </summary>
        public List<string> Scan(string rootPath)
        {
            var files = new List<string>();

            // Extensions we support: C# and TypeScript
            var supportedExtensions = new[] { ".cs", ".ts" };

            try
            {
                var allFiles = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories);

                foreach (var file in allFiles)
                {
                    var ext = Path.GetExtension(file);
                    if (!supportedExtensions.Contains(ext.ToLower()))
                        continue;

                    // Exclude binary and cache directories
                    var normalizedPath = file.Replace("\\", "/");
                    if (normalizedPath.Contains("/bin/") ||
                        normalizedPath.Contains("/obj/") ||
                        normalizedPath.Contains("/node_modules/") ||
                        normalizedPath.Contains("/.git/"))
                        continue;

                    files.Add(file);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Console.WriteLine($"Warning: Access denied to {rootPath}: {ex.Message}");
            }

            return files;
        }
    }
}
