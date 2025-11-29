using System.Collections.Generic;

namespace ProjectOverviewGenerator.Models
{
    /// <summary>
    /// Represents metadata for an attribute/decorator.
    /// </summary>
    public class AttributeMetadata
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, string> Arguments { get; set; } = new Dictionary<string, string>();
    }
}
