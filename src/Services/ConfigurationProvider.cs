using System;
using System.IO;
using System.Text.Json;
using ProjectOverviewGenerator.Models;

namespace ProjectOverviewGenerator.Services
{
    public class ConfigurationProvider
    {
        public ScanConfig Config { get; private set; }

        public ConfigurationProvider(string configPath)
        {
            if (!File.Exists(configPath))
                throw new FileNotFoundException($"Config file not found: {configPath}");

            var json = File.ReadAllText(configPath);
            Config = JsonSerializer.Deserialize<ScanConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (Config == null)
                throw new Exception("Failed to parse configuration file.");
        }
    }
}
