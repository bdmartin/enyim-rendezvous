// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Enyim.Caching.Rendezvous.HashProfiler.Reporting;

public sealed class JsonReportWriter : IReportWriter
{
    public void Write(ProfileReport report, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        var path = Path.Combine(outputDir, "profile-results.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(report.Results, options);
        File.WriteAllText(path, json);

        Console.WriteLine($"JSON report written to: {path}");
    }
}
