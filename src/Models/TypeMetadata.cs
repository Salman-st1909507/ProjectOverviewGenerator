using System.Collections.Generic;

namespace ProjectOverviewGenerator.Models
{
    /// <summary>
    /// Represents metadata for a type (class, interface, enum, etc.).
    /// </summary>
    public class TypeMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty; // class, interface, enum, record, etc.
        public string NamespaceOrModule { get; set; } = string.Empty;
        public List<string> BaseTypes { get; set; } = new List<string>();
        public List<string> ImplementedInterfaces { get; set; } = new List<string>();
        public List<AttributeMetadata> Attributes { get; set; } = new List<AttributeMetadata>();
        public List<MemberMetadata> Members { get; set; } = new List<MemberMetadata>();
    }
}
