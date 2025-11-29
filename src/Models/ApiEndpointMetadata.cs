using System.Collections.Generic;

namespace ProjectOverviewGenerator.Models
{
    /// <summary>
    /// Represents metadata for an API endpoint.
    /// </summary>
    public class ApiEndpointMetadata
    {
        public string Route { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Handler { get; set; } = string.Empty;
        public List<string> Dtos { get; set; } = new List<string>();
    }
}
