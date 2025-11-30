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

            // Regex for class, interface, enum, type alias
            var typeRegex = new System.Text.RegularExpressions.Regex(@"(export\s+)?(abstract\s+)?(class|interface|enum|type)\s+(\w+)(\s+extends\s+([\w, ]+))?(\s+implements\s+([\w, ]+))?", System.Text.RegularExpressions.RegexOptions.Multiline);
            var matches = typeRegex.Matches(fileContent);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var kind = match.Groups[3].Value;
                var name = match.Groups[4].Value;
                var baseTypes = match.Groups[6].Success ? match.Groups[6].Value.Split(',').Select(s => s.Trim()).ToList() : new System.Collections.Generic.List<string>();
                var interfaces = match.Groups[8].Success ? match.Groups[8].Value.Split(',').Select(s => s.Trim()).ToList() : new System.Collections.Generic.List<string>();

                // For type aliases, extract the full definition
                if (kind == "type")
                {
                    var typeDefMatch = System.Text.RegularExpressions.Regex.Match(
                        fileContent.Substring(match.Index),
                        @"type\s+\w+\s*=\s*([^;]+);",
                        System.Text.RegularExpressions.RegexOptions.Singleline
                    );
                    if (typeDefMatch.Success)
                    {
                        types.Add(new TypeMetadata
                        {
                            Name = name,
                            Kind = "type",
                            NamespaceOrModule = typeDefMatch.Groups[1].Value.Trim(),
                            BaseTypes = new System.Collections.Generic.List<string>(),
                            ImplementedInterfaces = new System.Collections.Generic.List<string>(),
                            Attributes = new System.Collections.Generic.List<AttributeMetadata>(),
                            Members = new System.Collections.Generic.List<MemberMetadata>()
                        });
                    }
                    continue;
                }

                // Find body of the type (for classes and interfaces)
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

                // Parse members line by line for better accuracy
                var members = new System.Collections.Generic.List<MemberMetadata>();
                var lines = body.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                
                int braceDepth = 0; // Track nested braces to skip method/constructor bodies
                int angleDepth = 0; // Track angle brackets to skip type parameter content
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    
                    // Count braces and angle brackets in this line
                    int openBraces = line.Count(c => c == '{');
                    int closeBraces = line.Count(c => c == '}');
                    int openAngles = line.Count(c => c == '<');
                    int closeAngles = line.Count(c => c == '>');
                    
                    // Update angle bracket depth (for type parameters)
                    angleDepth += openAngles - closeAngles;
                    if (angleDepth < 0) angleDepth = 0; // Reset if negative
                    
                    // If we're inside a method/constructor body, skip this line but update depth
                    if (braceDepth > 0)
                    {
                        braceDepth += openBraces - closeBraces;
                        continue;
                    }
                    
                    // If we're inside type parameters (e.g., signal<Record<string, {...}>>), skip property-like lines
                    if (angleDepth > 0 && line.Contains(":"))
                    {
                        continue;
                    }
                    
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("*"))
                        continue;

                    // Match: readonly property (e.g., readonly Check = Check;)
                    var readonlyMatch = System.Text.RegularExpressions.Regex.Match(
                        line,
                        @"^readonly\s+(\w+)\s*=\s*(.+?);?"
                    );
                    if (readonlyMatch.Success)
                    {
                        members.Add(new MemberMetadata
                        {
                            Name = readonlyMatch.Groups[1].Value,
                            MemberType = "readonly",
                            ReturnType = readonlyMatch.Groups[2].Value.TrimEnd(';'),
                            Signature = line.TrimEnd(';'),
                            Attributes = new System.Collections.Generic.List<AttributeMetadata>(),
                            Parameters = new System.Collections.Generic.List<ParameterMetadata>()
                        });
                        continue;
                    }

                    // Match: Signal/Computed fields (e.g., requests: Signal<Request[]>)
                    var signalMatch = System.Text.RegularExpressions.Regex.Match(
                        line,
                        @"^(\w+)\s*:\s*(Signal|WritableSignal|Computed)<(.+?)>\s*;?"
                    );
                    if (signalMatch.Success)
                    {
                        members.Add(new MemberMetadata
                        {
                            Name = signalMatch.Groups[1].Value,
                            MemberType = "signal",
                            ReturnType = $"{signalMatch.Groups[2].Value}<{signalMatch.Groups[3].Value}>",
                            Signature = line.TrimEnd(';'),
                            Attributes = new System.Collections.Generic.List<AttributeMetadata>(),
                            Parameters = new System.Collections.Generic.List<ParameterMetadata>()
                        });
                        continue;
                    }

                    // Match: Signal initialization (e.g., selectedRequest = signal<Request | null>(null))
                    var signalInitMatch = System.Text.RegularExpressions.Regex.Match(
                        line,
                        @"^(\w+)\s*=\s*(signal|computed)\s*<(.+?)>\s*\("
                    );
                    if (signalInitMatch.Success)
                    {
                        // Exclude 'default' (switch statement cases artifact)
                        if (signalInitMatch.Groups[1].Value != "default")
                        {
                            members.Add(new MemberMetadata
                            {
                                Name = signalInitMatch.Groups[1].Value,
                                MemberType = signalInitMatch.Groups[2].Value,
                                ReturnType = signalInitMatch.Groups[3].Value,
                                Signature = line.TrimEnd(';'),
                                Attributes = new System.Collections.Generic.List<AttributeMetadata>(),
                                Parameters = new System.Collections.Generic.List<ParameterMetadata>()
                            });
                        }
                        continue;
                    }

                    // Match: Constructor
                    var constructorMatch = System.Text.RegularExpressions.Regex.Match(
                        line,
                        @"^constructor\s*\("
                    );
                    if (constructorMatch.Success)
                    {
                        var constructorSig = line;
                        // Continue to next lines if constructor spans multiple lines
                        int j = i + 1;
                        while (j < lines.Length && !lines[j].Trim().Contains("{"))
                        {
                            constructorSig += " " + lines[j].Trim();
                            j++;
                        }
                        
                        // Extract just the signature (parameter list), not the body
                        int openParenIdx = constructorSig.IndexOf('(');
                        int closeParenIdx = constructorSig.IndexOf(')', openParenIdx);
                        if (closeParenIdx > openParenIdx)
                        {
                            constructorSig = constructorSig.Substring(0, closeParenIdx + 1);
                        }
                        
                        members.Add(new MemberMetadata
                        {
                            Name = "constructor",
                            MemberType = "constructor",
                            ReturnType = "",
                            Signature = constructorSig.Trim(),
                            Attributes = new System.Collections.Generic.List<AttributeMetadata>(),
                            Parameters = new System.Collections.Generic.List<ParameterMetadata>()
                        });
                        
                        // Enter constructor body - start tracking depth
                        braceDepth = openBraces - closeBraces;
                        continue;
                    }

                    // Match: Methods (e.g., approveRequest(requestId: string): void)
                    var methodMatch = System.Text.RegularExpressions.Regex.Match(
                        line,
                        @"^(public\s+|private\s+|protected\s+|async\s+)*(\w+)\s*\(([^)]*)\)\s*:\s*(\w+)"
                    );
                    if (methodMatch.Success)
                    {
                        members.Add(new MemberMetadata
                        {
                            Name = methodMatch.Groups[2].Value,
                            MemberType = "method",
                            ReturnType = methodMatch.Groups[4].Value,
                            Signature = $"{methodMatch.Groups[2].Value}({methodMatch.Groups[3].Value}): {methodMatch.Groups[4].Value}",
                            Attributes = new System.Collections.Generic.List<AttributeMetadata>(),
                            Parameters = new System.Collections.Generic.List<ParameterMetadata>()
                        });
                        
                        // Enter method body - start tracking depth
                        braceDepth = openBraces - closeBraces;
                        continue;
                    }

                    // Match: Regular properties with type (e.g., name: string;)
                    // Must end with semicolon to avoid matching object literal properties or parameters
                    var propertyMatch = System.Text.RegularExpressions.Regex.Match(
                        line,
                        @"^(public\s+|private\s+|protected\s+)?(\w+)\s*:\s*([\w<>\[\]|]+)\s*;$"
                    );
                    if (propertyMatch.Success && !line.Contains("(") && !line.Contains("{") && !line.Contains("["))
                    {
                        // Exclude 'default' (switch statement cases) and 'return' (return statements)
                        var propName = propertyMatch.Groups[2].Value;
                        if (propName != "default" && propName != "return")
                        {
                            members.Add(new MemberMetadata
                            {
                                Name = propName,
                                MemberType = "property",
                                ReturnType = propertyMatch.Groups[3].Value,
                                Signature = line.TrimEnd(';'),
                                Attributes = new System.Collections.Generic.List<AttributeMetadata>(),
                                Parameters = new System.Collections.Generic.List<ParameterMetadata>()
                            });
                        }
                        continue;
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

            return new FileMetadata
            {
                FilePath = filePath,
                Extension = ".ts",
                Types = types,
                ApiEndpoints = apiEndpoints
            };
        }
    }
}
