// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

namespace Enyim.Caching.Rendezvous.HashProfiler.Reporting;

public sealed class CsvReportWriter : IReportWriter
{
    public void Write(ProfileReport report, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        WriteDistribution(report, outputDir);
        WriteAvalanche(report, outputDir);
        WriteMonotonicity(report, outputDir);
        WriteCollisions(report, outputDir);

        Console.WriteLine($"CSV reports written to: {outputDir}");
    }

    private static void WriteDistribution(ProfileReport report, string outputDir)
    {
        var path = Path.Combine(outputDir, "distribution.csv");
        using var writer = new StreamWriter(path);
        writer.WriteLine("Algorithm,ChiSquared,PValue,MaxDeviationPercent,StdDev,Passed");
        foreach (var (name, results) in report.Results)
        {
            var d = results.Distribution;
            writer.WriteLine($"{name},{d.ChiSquared:F4},{d.PValue:F6},{d.MaxDeviationPercent:F2},{d.StdDev:F4},{d.Passed}");
        }
    }

    private static void WriteAvalanche(ProfileReport report, string outputDir)
    {
        var path = Path.Combine(outputDir, "avalanche.csv");
        using var writer = new StreamWriter(path);
        writer.WriteLine("Algorithm,MeanBitFlipPercent,StdDev,MinBitsFlipped,MaxBitsFlipped,Passed");
        foreach (var (name, results) in report.Results)
        {
            var a = results.Avalanche;
            writer.WriteLine($"{name},{a.MeanBitFlipPercent:F2},{a.StdDev:F4},{a.MinBitsFlipped},{a.MaxBitsFlipped},{a.Passed}");
        }
    }

    private static void WriteMonotonicity(ProfileReport report, string outputDir)
    {
        var path = Path.Combine(outputDir, "monotonicity.csv");
        using var writer = new StreamWriter(path);
        writer.WriteLine("Algorithm,MovedToNewNode,MovedBetweenExisting,SpuriousMoves,ActualMovePercent,ExpectedMovePercent,Passed");
        foreach (var (name, results) in report.Results)
        {
            var m = results.Monotonicity;
            writer.WriteLine($"{name},{m.MovedToNewNode},{m.MovedBetweenExisting},{m.SpuriousMoves},{m.ActualMovePercent:F2},{m.ExpectedMovePercent:F2},{m.Passed}");
        }
    }

    private static void WriteCollisions(ProfileReport report, string outputDir)
    {
        var path = Path.Combine(outputDir, "collisions.csv");
        using var writer = new StreamWriter(path);
        writer.WriteLine("Algorithm,TotalKeys,UniqueHashes,Collisions,ExpectedCollisions,Passed");
        foreach (var (name, results) in report.Results)
        {
            var c = results.Collision;
            writer.WriteLine($"{name},{c.TotalKeys},{c.UniqueHashes},{c.Collisions},{c.ExpectedCollisions:F2},{c.Passed}");
        }
    }
}
