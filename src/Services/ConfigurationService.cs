using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectOverviewGenerator.Models;

namespace ProjectOverviewGenerator.Services
{
    /// <summary>
    /// Service for loading and validating scan configuration from JSON files.
    /// </summary>
    public class ConfigurationService
    {
        /// <summary>
        /// Loads configuration from a JSON file.
        /// </summary>
        public static ScanConfig LoadConfig(string configFilePath)
        {
            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException($"Configuration file not found: {configFilePath}");
            }

            try
            {
                var json = File.ReadAllText(configFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var config = JsonSerializer.Deserialize<ScanConfig>(json, options);

                if (config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration.");
                }

                ValidateConfig(config);
                return config;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Error parsing configuration JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates the configuration for completeness and correctness.
        /// </summary>
        private static void ValidateConfig(ScanConfig config)
        {
            if (config.Categories == null || config.Categories.Count == 0)
            {
                throw new InvalidOperationException("Configuration must define at least one category.");
            }

            if (config.GeneratedFiles == null || config.GeneratedFiles.Count == 0)
            {
                throw new InvalidOperationException("Configuration must define at least one generated file.");
            }

            var validCategoryNames = new HashSet<string>();
            foreach (var category in config.Categories)
            {
                if (string.IsNullOrWhiteSpace(category.Name))
                {
                    throw new InvalidOperationException("All categories must have a non-empty name.");
                }

                if (category.Extensions == null || category.Extensions.Count == 0)
                {
                    throw new InvalidOperationException($"Category '{category.Name}' must define at least one file extension.");
                }

                validCategoryNames.Add(category.Name);
            }

            foreach (var file in config.GeneratedFiles)
            {
                if (string.IsNullOrWhiteSpace(file.Name))
                {
                    throw new InvalidOperationException("All generated files must have a non-empty name.");
                }

                if (file.IncludedCategories == null || file.IncludedCategories.Count == 0)
                {
                    throw new InvalidOperationException($"Generated file '{file.Name}' must include at least one category.");
                }

                foreach (var catName in file.IncludedCategories)
                {
                    if (!validCategoryNames.Contains(catName))
                    {
                        throw new InvalidOperationException(
                            $"Generated file '{file.Name}' references unknown category: {catName}");
                    }
                }
            }
        }

        /// <summary>
        /// Saves configuration to a JSON file.
        /// </summary>
        public static void SaveConfig(ScanConfig config, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving configuration to {filePath}: {ex.Message}", ex);
            }
        }
    }
}
