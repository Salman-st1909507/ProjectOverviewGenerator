using System.Collections.Generic;
using ProjectOverviewGenerator.Models;

namespace ProjectOverviewGenerator.Services
{
    public class MarkdownGenerator
    {
        /// <summary>
        /// Generates ProjectStructure.md content (directory tree + excluded paths and extensions for AI awareness)
        /// </summary>
        public string GenerateProjectStructure(
            string rootPath,
            ProjectStructureConfig? projectStructureConfig,
            MarkdownConfig? markdownConfig = null)
        {
            int h2 = markdownConfig?.HeaderLevel ?? 2;
            int h3 = h2 + 1;
            string codeBlockStart = markdownConfig?.CodeBlockStyle == "fenced" ? "```" : "<pre>";
            string codeBlockEnd = markdownConfig?.CodeBlockStyle == "fenced" ? "```" : "</pre>";

            var md = $"{new string('#', h2)} Project Structure\n\n";

            // Directory tree (respecting exclusions)
            var tree = BuildFullDirectoryTree(rootPath, projectStructureConfig);
            md += $"{new string('#', h3)} Directory Structure\n\n";
            md += codeBlockStart + "\n" + tree + "\n" + codeBlockEnd + "\n\n";

            // Excluded Paths (for AI awareness)
            if (projectStructureConfig?.ExcludedPaths != null && projectStructureConfig.ExcludedPaths.Count > 0)
            {
                md += $"{new string('#', h3)} Excluded Paths\n\n";
                md += "The following paths are **excluded** from the project structure analysis:\n\n";
                foreach (var path in projectStructureConfig.ExcludedPaths)
                {
                    md += $"- `{path}`\n";
                }
                md += "\n";
            }

            // Excluded Extensions (for AI awareness)
            if (projectStructureConfig?.ExcludedExtensions != null && projectStructureConfig.ExcludedExtensions.Count > 0)
            {
                md += $"{new string('#', h3)} Excluded Extensions\n\n";
                md += "The following file extensions are **excluded** from the analysis:\n\n";
                foreach (var ext in projectStructureConfig.ExcludedExtensions)
                {
                    md += $"- `{ext}`\n";
                }
                md += "\n";
            }

            return md;
        }

        /// <summary>
        /// Generates ApiEndpoints.md content (API endpoints table only)
        /// </summary>
        public string GenerateApiEndpoints(
            IEnumerable<FileMetadata> allFiles,
            CategoryConfig? apiEndpointsConfig,
            MarkdownConfig? markdownConfig = null)
        {
            int h2 = markdownConfig?.HeaderLevel ?? 2;

            var md = $"{new string('#', h2)} API Endpoints\n\n";

            // Filter endpoints by apiEndpointsConfig if provided
            var allApiEndpoints = allFiles.SelectMany(f => f.ApiEndpoints ?? new List<ApiEndpointMetadata>()).ToList();

            if (apiEndpointsConfig != null && (apiEndpointsConfig.Paths.Count > 0 || apiEndpointsConfig.Extensions.Count > 0))
            {
                // Filter endpoints from files matching apiEndpointsConfig
                var matchingFiles = allFiles
                    .Where(f => MatchesApiEndpointConfig(f, apiEndpointsConfig))
                    .ToList();
                allApiEndpoints = matchingFiles.SelectMany(f => f.ApiEndpoints ?? new List<ApiEndpointMetadata>()).ToList();
            }

            // API endpoints table
            md += "| Route | HTTP Method | Controller | Handler | Request DTO | Response DTO |\n";
            md += "|-------|-------------|-----------|---------|-------------|---------------|\n";

            if (allApiEndpoints.Count == 0)
            {
                md += "| _No endpoints found_ | | | | | |\n";
            }
            else
            {
                foreach (var endpoint in allApiEndpoints.OrderBy(e => e.Route))
                {
                    var requestDto = endpoint.Dtos?.FirstOrDefault(d => d.Contains("Request")) ?? "";
                    var responseDto = endpoint.Dtos?.FirstOrDefault(d => !d.Contains("Request")) ?? "";
                    md += $"| `{endpoint.Route}` | {endpoint.HttpMethod} | {endpoint.Controller} | {endpoint.Handler} | {requestDto} | {responseDto} |\n";
                }
            }

            md += "\n";
            return md;
        }

        /// <summary>
        /// Generates category-specific content (types and members only, NO project structure)
        /// </summary>
        public string GenerateCategory(
            IEnumerable<FileMetadata> categoryFiles,
            List<string> includedCategories,
            MarkdownConfig? markdownConfig = null)
        {
            int h2 = markdownConfig?.HeaderLevel ?? 2;
            int h3 = h2 + 1;

            var md = "";

            // Group types by matched category
            var typesByCategory = categoryFiles
                .SelectMany(f => f.Types.Select(t => (Categories: f.MatchedCategories, Type: t)))
                .SelectMany(x => x.Categories.Where(c => includedCategories.Contains(c))
                    .Select(c => (Category: c, Type: x.Type)))
                .GroupBy(x => x.Category)
                .OrderBy(g => g.Key)
                .ToList();

            // For each category, output types
            foreach (var categoryGroup in typesByCategory)
            {
                md += $"{new string('#', h3)} {categoryGroup.Key}\n\n";

                // Group types by file
                var typesByFile = categoryGroup
                    .GroupBy(x => categoryFiles.First(f => f.Types.Contains(x.Type)).FilePath)
                    .OrderBy(g => Path.GetFileName(g.Key))
                    .ToList();

                foreach (var fileGroup in typesByFile)
                {
                    md += $"**File:** `{Path.GetFileName(fileGroup.Key)}`\n\n";

                    foreach (var item in fileGroup)
                    {
                        var type = item.Type;
                        md += $"- **{type.Name}** ({type.Kind})\n";

                        if (!string.IsNullOrEmpty(type.NamespaceOrModule))
                            md += $"  - Namespace: `{type.NamespaceOrModule}`\n";

                        if (type.BaseTypes != null && type.BaseTypes.Count > 0)
                            md += $"  - Base Types: {string.Join(", ", type.BaseTypes.Select(b => $"`{b}`"))}\n";

                        if (type.ImplementedInterfaces != null && type.ImplementedInterfaces.Count > 0)
                            md += $"  - Implements: {string.Join(", ", type.ImplementedInterfaces.Select(i => $"`{i}`"))}\n";

                        if (type.Members != null && type.Members.Count > 0)
                        {
                            md += "  - Members:\n";
                            foreach (var member in type.Members.OrderBy(m => m.Name))
                            {
                                // Use full signature if available, otherwise format return type
                                if (!string.IsNullOrEmpty(member.Signature))
                                {
                                    md += $"    - `{member.Signature}`\n";
                                }
                                else
                                {
                                    var returnType = !string.IsNullOrEmpty(member.ReturnType) ? $" : {member.ReturnType}" : "";
                                    md += $"    - `{member.MemberType}` {member.Name}{returnType}\n";
                                }
                            }
                        }
                        md += "\n";
                    }
                }

                md += "\n";
            }

            return md;
        }

        private bool MatchesApiEndpointConfig(FileMetadata file, CategoryConfig config)
        {
            var fileName = Path.GetFileName(file.FilePath);
            var relPath = file.RelativePath ?? fileName;

            // Check extensions
            if (config.Extensions != null && config.Extensions.Count > 0)
            {
                var ext = Path.GetExtension(file.FilePath);
                if (!config.Extensions.Any(e => e.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            // Check paths
            if (config.Paths != null && config.Paths.Count > 0)
            {
                var pathMatches = config.Paths.Any(p =>
                    relPath.Replace("\\", "/").Contains(p.Replace("\\", "/").TrimEnd('*')));
                if (!pathMatches)
                    return false;
            }

            // Check patterns
            if (config.Patterns != null && config.Patterns.Count > 0)
            {
                var patternMatches = config.Patterns.Any(p => fileName.Contains(p, StringComparison.OrdinalIgnoreCase));
                if (!patternMatches)
                    return false;
            }

            // Check excluded patterns
            if (config.ExcludedPatterns != null && config.ExcludedPatterns.Count > 0)
            {
                if (config.ExcludedPatterns.Any(p => fileName.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            return true;
        }

        private string BuildFullDirectoryTree(string rootPath, ProjectStructureConfig? config)
        {
            var root = new TreeNode(Path.GetFileName(rootPath));
            BuildTreeRecursive(root, rootPath, config);
            return RenderTree(root, "");
        }

        private void BuildTreeRecursive(TreeNode node, string dirPath, ProjectStructureConfig? config)
        {
            try
            {
                var dirs = Directory.GetDirectories(dirPath).OrderBy(d => d).ToList();
                var files = Directory.GetFiles(dirPath).OrderBy(f => f).ToList();

                foreach (var d in dirs)
                {
                    var dirName = Path.GetFileName(d);

                    // Skip excluded paths
                    if (ShouldExcludePath(dirName, config))
                        continue;

                    var child = new TreeNode(dirName);
                    node.Children[child.Name] = child;
                    BuildTreeRecursive(child, d, config);
                }

                foreach (var f in files)
                {
                    var fileName = Path.GetFileName(f);

                    // Skip excluded extensions
                    if (ShouldExcludeExtension(fileName, config))
                        continue;

                    var child = new TreeNode(fileName);
                    node.Children[child.Name] = child;
                }
            }
            catch { /* Skip inaccessible directories */ }
        }

        private bool ShouldExcludePath(string dirName, ProjectStructureConfig? config)
        {
            if (config?.ExcludedPaths == null || config.ExcludedPaths.Count == 0)
                return false;

            return config.ExcludedPaths.Any(pattern =>
            {
                // Handle patterns like "*/bin", ".git", "node_modules"
                var patternToMatch = pattern.TrimStart('*', '/');
                return dirName.Equals(patternToMatch, StringComparison.OrdinalIgnoreCase) ||
                       dirName.Contains(patternToMatch, StringComparison.OrdinalIgnoreCase);
            });
        }

        private bool ShouldExcludeExtension(string fileName, ProjectStructureConfig? config)
        {
            if (config?.ExcludedExtensions == null || config.ExcludedExtensions.Count == 0)
                return false;

            var fileExt = Path.GetExtension(fileName);
            return config.ExcludedExtensions.Any(ext =>
                fileExt.Equals(ext, StringComparison.OrdinalIgnoreCase));
        }

        private string RenderTree(TreeNode node, string indent)
        {
            var lines = new List<string>();
            var children = node.Children.Values.ToList();

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                var isLast = i == children.Count - 1;
                var prefix = indent + (node.Name == "" ? "" : (isLast ? "└── " : "├── "));
                lines.Add(prefix + child.Name);
                var nextIndent = indent + (node.Name == "" ? "" : (isLast ? "    " : "│   "));
                lines.Add(RenderTree(child, nextIndent));
            }

            return string.Join("\n", lines.Where(l => !string.IsNullOrEmpty(l)));
        }

        private class TreeNode
        {
            public string Name;
            public Dictionary<string, TreeNode> Children = new();
            public TreeNode(string name) { Name = name; }
        }
    }
}
