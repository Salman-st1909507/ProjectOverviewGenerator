using System.Collections.Generic;
using ProjectOverviewGenerator.Models;
using ProjectOverviewGenerator.Interfaces;

namespace ProjectOverviewGenerator.Services.Parsers
{
    public class TsFileParser : IFileParser
    {
        public bool CanParse(string extension) => extension.ToLower() == ".ts";

        public List<string> GetSupportedExtensions() => new List<string> { ".ts" };

        public FileMetadata Parse(string filePath, string fileContent)
        {
            var types = new System.Collections.Generic.List<TypeMetadata>();
            var apiEndpoints = new System.Collections.Generic.List<ApiEndpointMetadata>();

            // Regex for class, interface, enum
            var typeRegex = new System.Text.RegularExpressions.Regex(@"(export\s+)?(abstract\s+)?(class|interface|enum)\s+(\w+)(\s+extends\s+([\w, ]+))?(\s+implements\s+([\w, ]+))?", System.Text.RegularExpressions.RegexOptions.Multiline);
            var matches = typeRegex.Matches(fileContent);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var kind = match.Groups[3].Value;
                var name = match.Groups[4].Value;
                var baseTypes = match.Groups[6].Success ? match.Groups[6].Value.Split(',').Select(s => s.Trim()).ToList() : new System.Collections.Generic.List<string>();
                var interfaces = match.Groups[8].Success ? match.Groups[8].Value.Split(',').Select(s => s.Trim()).ToList() : new System.Collections.Generic.List<string>();

                // Find body of the type
                int startIdx = match.Index + match.Length;
                int braceIdx = fileContent.IndexOf('{', startIdx);
                if (braceIdx == -1) continue;
                int depth = 1, endIdx = braceIdx + 1;
                while (endIdx < fileContent.Length && depth > 0)
                {
                    if (fileContent[endIdx] == '{') depth++;
                    else if (fileContent[endIdx] == '}') depth--;
                    endIdx++;
                }
                var body = fileContent.Substring(braceIdx + 1, endIdx - braceIdx - 2);

                // Members: methods and properties
                var members = new System.Collections.Generic.List<MemberMetadata>();
                var memberRegex = new System.Text.RegularExpressions.Regex(@"(public |private |protected |readonly |static )*(\w+)\s*(:\s*([\w\<\>\[\]]+))?\s*(=|;|\(|\{)", System.Text.RegularExpressions.RegexOptions.Multiline);
                var memberMatches = memberRegex.Matches(body);
                foreach (System.Text.RegularExpressions.Match mm in memberMatches)
                {
                    var memberName = mm.Groups[2].Value;
                    var type = mm.Groups[4].Success ? mm.Groups[4].Value : "";
                    var isMethod = mm.Groups[5].Value == "(";
                    members.Add(new MemberMetadata
                    {
                        Name = memberName,
                        MemberType = isMethod ? "method" : "property",
                        ReturnType = type,
                        Attributes = new System.Collections.Generic.List<AttributeMetadata>(),
                        Parameters = new System.Collections.Generic.List<ParameterMetadata>()
                    });

                    // API endpoint extraction for NestJS-like decorators
                    if (isMethod)
                    {
                        // Look for @Get(), @Post(), etc. above the method
                        var methodStart = body.LastIndexOf(mm.Value, System.StringComparison.Ordinal);
                        if (methodStart > 0)
                        {
                            var beforeMethod = body.Substring(0, methodStart);
                            var decoratorMatch = System.Text.RegularExpressions.Regex.Match(
                                beforeMethod,
                                @"@(Get|Post|Put|Delete|Patch|Options|Head)\((?:'|""|)?([^'""\)]*)(?:'|""|)?\)$",
                                System.Text.RegularExpressions.RegexOptions.RightToLeft | System.Text.RegularExpressions.RegexOptions.Multiline
                            );
                            if (decoratorMatch.Success)
                            {
                                apiEndpoints.Add(new ApiEndpointMetadata
                                {
                                    Route = decoratorMatch.Groups[2].Value,
                                    HttpMethod = decoratorMatch.Groups[1].Value.ToUpper(),
                                    Controller = name,
                                    Handler = memberName,
                                    Dtos = new System.Collections.Generic.List<string>()
                                });
                            }
                        }
                    }
                }

                types.Add(new TypeMetadata
                {
                    Name = name,
                    Kind = kind,
                    NamespaceOrModule = "",
                    BaseTypes = baseTypes,
                    ImplementedInterfaces = interfaces,
                    Attributes = new System.Collections.Generic.List<AttributeMetadata>(),
                    Members = members
                });
            }

            var fileMeta = new FileMetadata
            {
                FilePath = filePath,
                Extension = ".ts",
                Types = types,
                ApiEndpoints = apiEndpoints
            };
            return fileMeta;
        }
    }
}
