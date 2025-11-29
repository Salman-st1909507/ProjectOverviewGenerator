using ProjectOverviewGenerator.Models;
using ProjectOverviewGenerator.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ProjectOverviewGenerator.Services.Parsers
{
    public class GdFileParser : IFileParser
    {
        public bool CanParse(string extension) => extension.ToLower() == ".gd";

        public List<string> GetSupportedExtensions() => new List<string> { ".gd" };

        public FileMetadata Parse(string filePath, string fileContent)
        {
            var types = new List<TypeMetadata>();
            // Simple regex for class_name and extends
            var classNameMatch = Regex.Match(fileContent, @"class_name\s+(\w+)");
            var extendsMatch = Regex.Match(fileContent, @"extends\s+(\w+)");
            var className = classNameMatch.Success ? classNameMatch.Groups[1].Value : System.IO.Path.GetFileNameWithoutExtension(filePath);
            var baseType = extendsMatch.Success ? extendsMatch.Groups[1].Value : null;
            // Members: func and var
            var members = new List<MemberMetadata>();
            var funcRegex = new Regex(@"func\s+(\w+)\s*\(([^)]*)\)", RegexOptions.Multiline);
            foreach (Match m in funcRegex.Matches(fileContent))
            {
                members.Add(new MemberMetadata
                {
                    Name = m.Groups[1].Value,
                    MemberType = "method",
                    ReturnType = "",
                    Attributes = new List<AttributeMetadata>(),
                    Parameters = new List<ParameterMetadata>()
                });
            }
            var varRegex = new Regex(@"var\s+(\w+)", RegexOptions.Multiline);
            foreach (Match m in varRegex.Matches(fileContent))
            {
                members.Add(new MemberMetadata
                {
                    Name = m.Groups[1].Value,
                    MemberType = "property",
                    ReturnType = "",
                    Attributes = new List<AttributeMetadata>(),
                    Parameters = new List<ParameterMetadata>()
                });
            }
            types.Add(new TypeMetadata
            {
                Name = className,
                Kind = "gdscript_class",
                NamespaceOrModule = "",
                BaseTypes = baseType != null ? new List<string> { baseType } : new List<string>(),
                ImplementedInterfaces = new List<string>(),
                Attributes = new List<AttributeMetadata>(),
                Members = members
            });
            return new FileMetadata
            {
                FilePath = filePath,
                Extension = ".gd",
                Types = types
            };
        }
    }
}
