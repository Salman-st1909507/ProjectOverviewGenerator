using System.IO;

namespace ProjectOverviewGenerator.Services
{
    public class OutputWriter
    {
        public void WriteFile(string directory, string fileName, string content)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, fileName), content);
        }
    }
}
