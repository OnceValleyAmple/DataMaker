using System.IO.Compression;

namespace DataMaker.Refactored.Services;

public sealed class ZipService
{
    public void Create(string sourceDirectory, string zipPath)
    {
        var parent = Path.GetDirectoryName(zipPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        if (File.Exists(zipPath))
            File.Delete(zipPath);

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        foreach (var file in Directory.EnumerateFiles(
                     sourceDirectory,
                     "*",
                     SearchOption.TopDirectoryOnly))
        {
            if (string.Equals(
                    Path.GetFileName(file),
                    "solution.exe",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            archive.CreateEntryFromFile(
                file,
                Path.GetFileName(file),
                CompressionLevel.Optimal);
        }
    }
}
