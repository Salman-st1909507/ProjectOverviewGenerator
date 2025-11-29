using System.Collections.Generic;
using System.Linq;
using ProjectOverviewGenerator.Models;

namespace ProjectOverviewGenerator.Services
{
    /// <summary>
    /// Deprecated: Use CategoryMatchingEngine instead.
    /// This class is kept for backwards compatibility only.
    /// </summary>
    public class ClassificationEngine
    {
        private readonly ScanConfig _config;

        public ClassificationEngine(ScanConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Deprecated: Use CategoryMatchingEngine.MatchFileToCategories() instead.
        /// </summary>
        public string Classify(string filePath)
        {
            return "Uncategorized";
        }
    }
}
