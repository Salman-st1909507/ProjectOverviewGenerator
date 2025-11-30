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

                // Extract controller-level Route attribute (before class declaration)
                string controllerRoute = "";
                var preClassContent = fileContent.Substring(0, match.Index);
                var preClassLines = preClassContent.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                
                // Look backward from class declaration for Route attribute
                for (int i = preClassLines.Length - 1; i >= 0 && i >= preClassLines.Length - 10; i--)
                {
                    var attrLine = preClassLines[i].Trim();
                    var controllerRouteMatch = System.Text.RegularExpressions.Regex.Match(
                        attrLine,
                        @"\[Route\(\s*""([^""]*)""\s*\)\]",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );
                    if (controllerRouteMatch.Success)
                    {
                        controllerRoute = controllerRouteMatch.Groups[1].Value;
                        break;
                    }
                }
                
                // If no explicit route, derive from controller name (e.g., AccountController -> Account)
                if (string.IsNullOrEmpty(controllerRoute) && name.EndsWith("Controller"))
                {
                    controllerRoute = name.Substring(0, name.Length - "Controller".Length);
                }

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
                var enumValues = new System.Collections.Generic.List<string>();

                // Extract members with full signatures
                var lines = body.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
                
                // Special handling for interfaces - they have no access modifiers
                bool isInterface = kind == "interface";
                bool isEnum = kind == "enum";
                
                // For enums, extract values instead of members
                if (isEnum)
                {
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//") || trimmed.StartsWith("/*"))
                            continue;
                        
                        // Enum values: Name or Name = Value
                        var enumValueMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"^(\w+)\s*(=\s*\d+)?\s*,?");
                        if (enumValueMatch.Success)
                        {
                            var valueName = enumValueMatch.Groups[1].Value;
                            var valueAssignment = enumValueMatch.Groups[2].Success ? enumValueMatch.Groups[2].Value.Trim() : "";
                            
                            if (!string.IsNullOrEmpty(valueName))
                            {
                                if (!string.IsNullOrEmpty(valueAssignment))
                                {
                                    enumValues.Add($"{valueName} {valueAssignment}");
                                }
                                else
                                {
                                    enumValues.Add(valueName);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // For non-enums, extract members as before
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith("/*"))
                            continue;

                    // For interfaces, look for property/method signatures without access modifiers
                    // For classes/records, require access modifiers
                    bool hasMemberSignature = false;
                    if (isInterface)
                    {
                        // Interface members: properties with { get; }, methods with (), or property/method declarations
                        hasMemberSignature = line.Contains("(") || line.Contains("{ get") || line.Contains("{get") || 
                                           (line.Contains(" ") && (line.Contains(";") || line.EndsWith("}")));
                    }
                    else
                    {
                        // Skip non-member lines for classes
                        hasMemberSignature = line.StartsWith("public") || line.StartsWith("private") || 
                                           line.StartsWith("protected") || line.StartsWith("internal");
                    }
                    
                    if (!hasMemberSignature)
                        continue;

                    // Collect attributes from preceding lines
                    var attributeLines = new System.Collections.Generic.List<string>();
                    int attrIdx = i - 1;
                    while (attrIdx >= 0)
                    {
                        var attrLine = lines[attrIdx].Trim();
                        if (string.IsNullOrWhiteSpace(attrLine) || attrLine.StartsWith("//") || attrLine.StartsWith("/*") || attrLine.StartsWith("///"))
                        {
                            attrIdx--;
                            continue;
                        }
                        if (attrLine.StartsWith("[") && attrLine.EndsWith("]"))
                        {
                            attributeLines.Insert(0, attrLine);
                            attrIdx--;
                        }
                        else
                        {
                            break;
                        }
                    }

                    var fullSignature = line;

                    // For methods, continue to next line if it ends with ( or doesn't have closing )
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
                                // For interfaces, also include the semicolon if present
                                if (isInterface && closeLine.Length > endParenIdx + 1)
                                {
                                    fullSignature += closeLine.Substring(endParenIdx + 1).TrimEnd();
                                }
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

                    // Skip if it's a class name or constructor pattern (but not for interfaces)
                    if (!isInterface && (memberName == name || memberName[0] == '_'))
                    {
                        // Skip internal members starting with _
                        if (memberName[0] == '_' && !memberName.StartsWith("_Ready") && !memberName.StartsWith("_Process") && !memberName.StartsWith("_Input"))
                            continue;
                    }

                    // Extract return type
                    var typeMatch = System.Text.RegularExpressions.Regex.Match(fullSignature, @"\s([\w<>\[\]?]+)\s+\w+\s*(\(|\{|;|=)");
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
                        string? httpMethod = null;
                        string route = "";
                        var dtos = new System.Collections.Generic.List<string>();
                        
                        // Check attributes for HTTP method decorators and routes
                        foreach (var attr in attributeLines)
                        {
                            // Match [HttpGet], [HttpPost("route")], [HttpGet("{id}")], etc.
                            var httpAttrMatch = System.Text.RegularExpressions.Regex.Match(
                                attr,
                                @"\[Http(Get|Post|Put|Delete|Patch|Options|Head)(?:\(\s*""([^""]*)""\s*\))?\]",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                            );
                            
                            if (httpAttrMatch.Success)
                            {
                                httpMethod = httpAttrMatch.Groups[1].Value.ToUpper();
                                // If route is specified in HttpMethod attribute, use it
                                if (httpAttrMatch.Groups[2].Success && !string.IsNullOrEmpty(httpAttrMatch.Groups[2].Value))
                                {
                                    route = httpAttrMatch.Groups[2].Value;
                                }
                            }
                            
                            // Also check for separate [Route("...")] attribute
                            var routeAttrMatch = System.Text.RegularExpressions.Regex.Match(
                                attr,
                                @"\[Route\(\s*""([^""]*)""\s*\)\]",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                            );
                            
                            if (routeAttrMatch.Success)
                            {
                                route = routeAttrMatch.Groups[1].Value;
                            }
                        }
                        
                        // Extract DTOs from method parameters
                        // Pattern 1: [FromForm] TokenCommand command, [FromBody] UpdateDto dto, etc.
                        var paramPattern = @"\[From(?:Form|Body|Query|Route|Header)\]\s+([\w<>]+)\s+\w+";
                        var paramMatches = System.Text.RegularExpressions.Regex.Matches(fullSignature, paramPattern);
                        foreach (System.Text.RegularExpressions.Match paramMatch in paramMatches)
                        {
                            var dtoType = paramMatch.Groups[1].Value;
                            if (!dtos.Contains(dtoType))
                            {
                                dtos.Add(dtoType);
                            }
                        }
                        
                        // Pattern 2: Parameters without [From*] attributes - extract complex types (not primitives)
                        // Match: (Type param, Type2 param2) but skip primitives like Guid, int, string
                        if (dtos.Count == 0) // Only if we didn't find any [From*] parameters
                        {
                            var allParamsPattern = @"\(([^)]*)\)";
                            var allParamsMatch = System.Text.RegularExpressions.Regex.Match(fullSignature, allParamsPattern);
                            if (allParamsMatch.Success)
                            {
                                var paramsString = allParamsMatch.Groups[1].Value;
                                // Split by comma, extract type names
                                var parameters = paramsString.Split(',');
                                foreach (var param in parameters)
                                {
                                    var trimmed = param.Trim();
                                    if (string.IsNullOrEmpty(trimmed)) continue;
                                    
                                    // Extract type (first word before space)
                                    var paramTypeMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"^([\w<>]+)\s+\w+");
                                    if (paramTypeMatch.Success)
                                    {
                                        var paramType = paramTypeMatch.Groups[1].Value;
                                        // Skip primitive types and CancellationToken
                                        var primitives = new[] { "int", "string", "bool", "Guid", "DateTime", "long", "double", "float", "decimal", "CancellationToken" };
                                        if (!primitives.Contains(paramType) && !dtos.Contains(paramType))
                                        {
                                            dtos.Add(paramType);
                                        }
                                    }
                                }
                            }
                        }
                        
                        // Also try to extract return type from Task<IActionResult> or similar
                        // This is a simplified approach - in real scenarios, you'd need to trace the actual return
                        var returnTypeMatch = System.Text.RegularExpressions.Regex.Match(fullSignature, @"Task<(\w+)>");
                        if (returnTypeMatch.Success)
                        {
                            var taskReturn = returnTypeMatch.Groups[1].Value;
                            // Only add if it's not IActionResult (which is generic)
                            if (taskReturn != "IActionResult" && taskReturn != "ActionResult")
                            {
                                if (!dtos.Contains(taskReturn))
                                {
                                    dtos.Add(taskReturn);
                                }
                            }
                        }
                        
                        // If we found an HTTP method, create an endpoint
                        if (httpMethod != null)
                        {
                            // Combine controller route with method route
                            string fullRoute = controllerRoute;
                            if (!string.IsNullOrEmpty(route))
                            {
                                // Add slash if needed
                                if (!string.IsNullOrEmpty(fullRoute) && !fullRoute.EndsWith("/") && !route.StartsWith("/"))
                                {
                                    fullRoute += "/";
                                }
                                fullRoute += route;
                            }
                            else if (!string.IsNullOrEmpty(fullRoute))
                            {
                                // Method has no route, just use controller route with trailing slash
                                fullRoute += "/";
                            }
                            
                            apiEndpoints.Add(new ApiEndpointMetadata
                            {
                                Route = fullRoute,
                                HttpMethod = httpMethod,
                                Controller = name,
                                Handler = memberName,
                                Dtos = dtos,
                                MethodSignature = fullSignature,
                                Attributes = new System.Collections.Generic.List<string>(attributeLines)
                            });
                        }
                    }
                    } // End of for loop for members
                } // End of else block for non-enum types

                types.Add(new TypeMetadata
                {
                    Name = name,
                    Kind = kind,
                    NamespaceOrModule = "",
                    BaseTypes = baseTypes,
                    ImplementedInterfaces = new System.Collections.Generic.List<string>(),
                    Attributes = new System.Collections.Generic.List<AttributeMetadata>(),
                    Members = members,
                    EnumValues = enumValues
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
