using System.Collections.Generic;
using ProjectOverviewGenerator.Models;
using ProjectOverviewGenerator.Interfaces;

namespace ProjectOverviewGenerator.Services.Parsers
{
    public class CsFileParser : IFileParser
    {
        public bool CanParse(string extension) => extension.ToLower() == ".cs";

        public List<string> GetSupportedExtensions() => new List<string> { ".cs" };

        public FileMetadata Parse(string filePath, string fileContent)
        {
            var types = new System.Collections.Generic.List<TypeMetadata>();
            var apiEndpoints = new System.Collections.Generic.List<ApiEndpointMetadata>();

            // Regex for class, interface, enum, record
            var typeRegex = new System.Text.RegularExpressions.Regex(@"(public |internal |protected |private |static |abstract |sealed |partial )*(class|interface|enum|record)\s+(\w+)(\s*:\s*([\w, <>]+))?", System.Text.RegularExpressions.RegexOptions.Multiline);
            var matches = typeRegex.Matches(fileContent);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var kind = match.Groups[2].Value;
                var name = match.Groups[3].Value;
                var baseTypes = match.Groups[5].Success ? match.Groups[5].Value.Split(',').Select(s => s.Trim()).ToList() : new System.Collections.Generic.List<string>();

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

                // Members: methods, properties, fields
                var members = new System.Collections.Generic.List<MemberMetadata>();

                // Extract members with full signatures
                var lines = body.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith("/*"))
                        continue;

                    // Skip non-member lines
                    if (!line.StartsWith("public") && !line.StartsWith("private") && !line.StartsWith("protected") && !line.StartsWith("internal"))
                        continue;

                    var fullSignature = line;

                    // For methods, continue to next line if it ends with (
                    if (line.Contains("(") && !line.Contains(")"))
                    {
                        int j = i + 1;
                        while (j < lines.Length && !lines[j].Trim().Contains(")"))
                        {
                            fullSignature += " " + lines[j].Trim();
                            j++;
                        }
                        if (j < lines.Length)
                        {
                            var closeLine = lines[j].Trim();
                            var endParenIdx = closeLine.IndexOf(")");
                            if (endParenIdx != -1)
                            {
                                fullSignature += " " + closeLine.Substring(0, endParenIdx + 1);
                            }
                        }
                    }

                    // Extract member type and name
                    var isMethod = fullSignature.Contains("(") && fullSignature.Contains(")");

                    // Extract the last word before ( or { or ; as the member name
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(fullSignature, @"\s(\w+)\s*(\(|\{|;|=|$)");
                    if (!nameMatch.Success)
                        continue;

                    var memberName = nameMatch.Groups[1].Value;

                    // Skip if it's a class name or constructor pattern
                    if (memberName == name || memberName[0] == '_')
                    {
                        // Skip internal members starting with _
                        if (memberName[0] == '_' && !memberName.StartsWith("_Ready") && !memberName.StartsWith("_Process") && !memberName.StartsWith("_Input"))
                            continue;
                    }

                    // Extract return type
                    var typeMatch = System.Text.RegularExpressions.Regex.Match(fullSignature, @"\s([\w<>\[\]]+)\s+\w+\s*(\(|\{|;|=)");
                    var returnType = typeMatch.Success ? typeMatch.Groups[1].Value : "unknown";

                    members.Add(new MemberMetadata
                    {
                        Name = memberName,
                        MemberType = isMethod ? "method" : "property/field",
                        ReturnType = returnType,
                        Signature = fullSignature.TrimEnd('{', ';', '=').Trim(),
                        Attributes = new System.Collections.Generic.List<AttributeMetadata>(),
                        Parameters = new System.Collections.Generic.List<ParameterMetadata>()
                    });

                    // API endpoint extraction for methods
                    if (isMethod)
                    {
                        var httpAttrMatch = System.Text.RegularExpressions.Regex.Match(
                            fullSignature,
                            @"\[(Http(Get|Post|Put|Delete|Patch|Options|Head))\(\s*""([^""]*)""\s*\)\]"
                        );
                        if (!httpAttrMatch.Success)
                        {
                            httpAttrMatch = System.Text.RegularExpressions.Regex.Match(fullSignature, @"\[(Http(Get|Post|Put|Delete|Patch|Options|Head))\(\s*'([^']*)'\s*\)\]");
                        }
                        if (httpAttrMatch.Success)
                        {
                            apiEndpoints.Add(new ApiEndpointMetadata
                            {
                                Route = httpAttrMatch.Groups[3].Value,
                                HttpMethod = httpAttrMatch.Groups[2].Value.ToUpper(),
                                Controller = name,
                                Handler = memberName,
                                Dtos = new System.Collections.Generic.List<string>()
                            });
                        }
                    }
                }

                types.Add(new TypeMetadata
                {
                    Name = name,
                    Kind = kind,
                    NamespaceOrModule = "",
                    BaseTypes = baseTypes,
                    ImplementedInterfaces = new System.Collections.Generic.List<string>(),
                    Attributes = new System.Collections.Generic.List<AttributeMetadata>(),
                    Members = members
                });
            }

            return new FileMetadata
            {
                FilePath = filePath,
                Extension = ".cs",
                Types = types,
                ApiEndpoints = apiEndpoints
            };
        }
    }

}
