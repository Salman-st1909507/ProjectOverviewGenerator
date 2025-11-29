using System.Collections.Generic;

namespace ProjectOverviewGenerator.Models
{
    /// <summary>
    /// Represents metadata for a type member (method, property, field, etc.).
    /// </summary>
    public class MemberMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string MemberType { get; set; } = string.Empty; // property, method, constructor, etc.
        public string ReturnType { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty; // Full signature line (e.g., "public void DoSomething(int x)")
        public List<AttributeMetadata> Attributes { get; set; } = new List<AttributeMetadata>();
        public List<ParameterMetadata> Parameters { get; set; } = new List<ParameterMetadata>();
    }
}
